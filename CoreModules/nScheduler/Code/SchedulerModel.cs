using Duende.IdentityModel.Client;
using Newtonsoft.Json;
using nScheduler.Code;
using NTech;
using NTech.Core.Module.Shared.Clients;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Xml.Linq;
using System.Xml.XPath;

namespace nScheduler
{
    public enum ScheduleRunnerAgentCode
    {
        //The runner is the sql server agent on the same database server as the scheduler database
        ModuleDbSqlAgent
    }

    public enum JobRunStatus
    {
        Success,
        Error,
        Warning,
        Skipped
    }

    public class ServiceCallModel
    {
        public string Name { get; set; }
        public bool IsManualTriggerAllowed { get; set; }
        public Uri ServiceUrl { get; set; }
        public string ServiceName { get; set; }
        public HashSet<string> Tags { get; set; }
    }

    public class SchedulerModel
    {
        private static IServiceClientSyncConverter syncConverter = new ServiceClientSyncConverterLegacy();

        public ScheduleRunnerAgentCode RunnerAgent { get; set; }
        public List<TimeSlot> Timeslots { get; set; }
        public IDictionary<string, ServiceCallModel> ServiceCalls { get; set; }

        private string GetLockName(string timeslotName)
        {
            return $"ntech.scheduler.timeslot.{timeslotName}";
        }

        public void AddServiceCallIfNotExists(ServiceCallModel serviceCall)
        {
            if (ServiceCalls == null)
            {
                ServiceCalls = new Dictionary<string, ServiceCallModel>();
            }
            if (!ServiceCalls.ContainsKey(serviceCall.Name))
                ServiceCalls.Add(serviceCall.Name, serviceCall);
        }

        public void TriggerTimeslot(string name, NTechSelfRefreshingBearerToken accessToken, IDictionary<string, string> schedulerData, IClock clock, int currentUserId, string informationMetadata)
        {
            var result = TriggerTimeslotWithLock(name, accessToken, schedulerData, clock, currentUserId, informationMetadata);
            NTech.Services.Infrastructure.Eventing.NTechEventHandler.PublishEvent(
                SchedulerEventCode.TriggerTimeSlotCompleted.ToString(),
                JsonConvert.SerializeObject(new
                {
                    timeslotName = name,
                    serviceRuns = result,
                    schedulerData = schedulerData
                }));
        }

        public ServiceRun TriggerServiceDirectly(string serviceName, string accessToken, IDictionary<string, string> schedulerData, IClock clock, int currentUserId, string informationMetadata)
        {
            var service = this.ServiceCalls[serviceName];
            var w = Stopwatch.StartNew();
            using (var db = new SchedulerContext())
            {
                var now = clock.Now;
                var run = new ServiceRun
                {
                    TriggeredById = currentUserId,
                    InformationMetaData = informationMetadata,
                    ChangedById = currentUserId,
                    ChangedDate = now,
                    StartDate = now,
                    JobName = service.Name,
                    TimeSlotName = null
                };
                db.ServiceRuns.Add(run);
                db.SaveChanges();

                try
                {
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    client.SetBearerToken(accessToken);              
                    client.Timeout = TimeSpan.FromHours(4);
                    var result = syncConverter.ToSync(() => client.PostAsJsonAsync(service.ServiceUrl, new { schedulerData = schedulerData }));
                    if (result.IsSuccessStatusCode)
                    {
                        var endDate = clock.Now;
                        run.EndDate = endDate;
                        var r = syncConverter.ToSync(() => result.Content.ReadAsAsync<PartialScheduledJobResult>());
                        if (r != null && r.Errors != null && r.Errors.Count > 0)
                        {
                            run.RuntimeInMs = w.ElapsedMilliseconds;
                            run.EndStatus = JobRunStatus.Error.ToString();
                            run.EndStatusData = JsonConvert.SerializeObject(new { errors = r.Errors, warnings = r.Warnings, schedulerData = schedulerData });
                        }
                        else if (r != null && r.Warnings != null && r.Warnings.Count > 0)
                        {
                            run.RuntimeInMs = w.ElapsedMilliseconds;
                            run.EndStatus = JobRunStatus.Warning.ToString();
                            run.EndStatusData = JsonConvert.SerializeObject(new { warnings = r.Warnings, schedulerData = schedulerData });
                        }
                        else
                        {
                            run.RuntimeInMs = w.ElapsedMilliseconds;
                            run.EndStatus = JobRunStatus.Success.ToString();
                            run.EndStatusData = JsonConvert.SerializeObject(new { schedulerData = schedulerData });
                        }
                    }
                    else
                    {
                        result.EnsureSuccessStatusCode();
                    }
                }
                catch (Exception ex)
                {
                    NLog.Error(ex, "TriggerJob {jobName}", service.Name);
                    var endDate = clock.Now;
                    run.RuntimeInMs = w.ElapsedMilliseconds;
                    run.EndDate = endDate;
                    run.EndStatus = JobRunStatus.Error.ToString();
                    run.EndStatusData = JsonConvert.SerializeObject(new { errors = new[] { ex.Message }, schedulerData = schedulerData });
                }
                db.SaveChanges();
                return run;
            }
        }

        private IList<ServiceRun> TriggerTimeslotWithLock(string name, NTechSelfRefreshingBearerToken accessToken, IDictionary<string, string> schedulerData, IClock clock, int currentUserId, string informationMetadata)
        {
            return SchedulerContext.RunWithExclusiveLock<IList<ServiceRun>>(GetLockName(name), () =>
            {
                var runs = new List<ServiceRun>();
                var ts = Timeslots?.Where(x => x.Name == name)?.SingleOrDefault();
                if (ts == null)
                    throw new Exception($"No such timeslot exists: '{name}'");
                var context = new TimeSlotRunContext
                {
                    TimeSlot = ts,
                    AcccessToken = accessToken,
                    Clock = clock,
                    CurrentUserId = currentUserId,
                    InformationMetadata = informationMetadata,
                    AlertContext = new SchedulerAfterRunAlertMessageContext
                    {
                        TimeslotStartTime = clock.Now.ToString("yyyy-MM-dd HH:mm"),
                        TimeslotName = ts.Name
                    },
                    SchedulerData = schedulerData
                };

                foreach (var job in ts.Items)
                {
                    runs.Add(TriggerJob(job, context));
                }

                if (context?.AlertContext?.TimeslotItems != null)
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            SchedulerMessagingFactory.CreateInstance()?.SendAfterRunAlertMessage(context?.AlertContext);
                        }
                        catch (Exception ex)
                        {
                            NLog.Error(ex, "Failed to send alert");
                        }
                    });
                }
                return runs;
            }, () => { throw new Exception($"The timeslot {name} was already running"); });
        }

        public class TimeSlotRunContext
        {
            public NTechSelfRefreshingBearerToken AcccessToken { get; internal set; }
            public IDictionary<string, string> SchedulerData { get; set; }
            public IDictionary<string, JobRunStatus> JobStatusesByJobName { get; set; } = new Dictionary<string, JobRunStatus>(StringComparer.InvariantCultureIgnoreCase);
            public IDictionary<string, IList<string>> WarningsByJobName { get; set; } = new Dictionary<string, IList<string>>(StringComparer.InvariantCultureIgnoreCase);
            public TimeSlot TimeSlot { get; internal set; }
            public int CurrentUserId { get; set; }
            public string InformationMetadata { get; set; }
            public IClock Clock { get; set; }
            public Code.SchedulerAfterRunAlertMessageContext AlertContext { get; set; }
        }

        private class PartialScheduledJobResult
        {
            //Jobs dont have to implement this but the warning level only works if it is
            public List<string> Warnings { get; set; }
            public List<string> Errors { get; set; }
        }

        private bool IsSkippedBySchedulerData(TimeSlotItem item, TimeSlotRunContext context)
        {
            if (context.SchedulerData != null && context.SchedulerData.ContainsKey("skippedJobNames"))
            {
                var skippedNames = JsonConvert.DeserializeObject<List<string>>(context.SchedulerData["skippedJobNames"]);
                if (skippedNames.Contains(item.ServiceCall?.Name, StringComparer.InvariantCultureIgnoreCase))
                    return true;
            }

            if (context.SchedulerData != null && context.SchedulerData.ContainsKey("onlyRunForTheseServiceNames"))
            {
                var onlyRunForTheseServiceNamesSetting = context.SchedulerData["onlyRunForTheseServiceNames"];
                if (!(onlyRunForTheseServiceNamesSetting?.Contains(item.ServiceCall?.ServiceName) ?? false))
                    return true;
            }

            return false;
        }

        private ServiceRun TriggerJob(TimeSlotItem item, TimeSlotRunContext context)
        {
            var runs = new List<ServiceRun>();

            var w = Stopwatch.StartNew();
            using (var db = new SchedulerContext())
            {
                var now = context.Clock.Now;
                var run = new ServiceRun
                {
                    TriggeredById = context.CurrentUserId,
                    InformationMetaData = context.InformationMetadata,
                    ChangedById = context.CurrentUserId,
                    ChangedDate = now,
                    StartDate = now,
                    JobName = item.ServiceCall.Name,
                    TimeSlotName = context.TimeSlot.Name
                };
                db.ServiceRuns.Add(run);
                db.SaveChanges();

                IList<string> skippedDueToRules;
                var shouldRun = item.TriggerLimitation.ShouldRun(item, context, out skippedDueToRules);
                if (shouldRun && IsSkippedBySchedulerData(item, context))
                {
                    shouldRun = false;
                    if (skippedDueToRules == null)
                        skippedDueToRules = new List<string>();
                    skippedDueToRules.Add("SchedulerDataOverride");
                }

                if (shouldRun)
                {
                    try
                    {
                        var client = new HttpClient();
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                        //client.SetBearerToken(context.AcccessToken.GetToken());

                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue((context.AcccessToken.GetToken()));
                        client.Timeout = TimeSpan.FromHours(4);
                        var result = syncConverter.ToSync(() => client.PostAsJsonAsync(item.ServiceCall.ServiceUrl, new { schedulerData = context.SchedulerData }));
                        if (result.IsSuccessStatusCode)
                        {
                            var endDate = context.Clock.Now;
                            run.EndDate = endDate;
                            var r = syncConverter.ToSync(() => result.Content.ReadAsAsync<PartialScheduledJobResult>());
                            if (r != null && r.Errors != null && r.Errors.Count > 0)
                            {
                                context.JobStatusesByJobName[item.ServiceCall.Name] = JobRunStatus.Error;
                                context.WarningsByJobName[item.ServiceCall.Name] = r.Warnings;
                                run.RuntimeInMs = w.ElapsedMilliseconds;
                                run.EndStatus = JobRunStatus.Error.ToString();
                                run.EndStatusData = JsonConvert.SerializeObject(new { errors = r.Errors, warnings = r.Warnings, schedulerData = context.SchedulerData });
                                context.AlertContext?.AddItem(run.JobName, run.EndStatus, r.Errors);
                            }
                            else if (r != null && r.Warnings != null && r.Warnings.Count > 0)
                            {
                                context.JobStatusesByJobName[item.ServiceCall.Name] = JobRunStatus.Warning;
                                context.WarningsByJobName[item.ServiceCall.Name] = r.Warnings;
                                run.RuntimeInMs = w.ElapsedMilliseconds;
                                run.EndStatus = JobRunStatus.Warning.ToString();
                                run.EndStatusData = JsonConvert.SerializeObject(new { warnings = r.Warnings, schedulerData = context.SchedulerData });
                                context.AlertContext?.AddItem(run.JobName, run.EndStatus, r.Warnings);
                            }
                            else
                            {
                                context.JobStatusesByJobName[item.ServiceCall.Name] = JobRunStatus.Success;
                                run.RuntimeInMs = w.ElapsedMilliseconds;
                                run.EndStatus = JobRunStatus.Success.ToString();
                                run.EndStatusData = JsonConvert.SerializeObject(new { schedulerData = context.SchedulerData });
                            }
                            db.SaveChanges();
                        }
                        else
                        {
                            result.EnsureSuccessStatusCode();
                        }
                    }
                    catch (Exception ex)
                    {
                        NLog.Error(ex, "TriggerJob {jobName}", item.ServiceCall.Name);
                        context.JobStatusesByJobName[item.ServiceCall.Name] = JobRunStatus.Error;
                        var endDate = context.Clock.Now;
                        run.RuntimeInMs = w.ElapsedMilliseconds;
                        run.EndDate = endDate;
                        run.EndStatus = JobRunStatus.Error.ToString();
                        run.EndStatusData = JsonConvert.SerializeObject(new { errors = new[] { ex.Message }, schedulerData = context.SchedulerData });
                        db.SaveChanges();
                        context.AlertContext?.AddItem(run.JobName, run.EndStatus, new[] { ex.Message });
                    }
                }
                else
                {
                    context.JobStatusesByJobName[item.ServiceCall.Name] = JobRunStatus.Skipped;
                    var endDate = context.Clock.Now;
                    run.EndDate = endDate;
                    run.RuntimeInMs = w.ElapsedMilliseconds;
                    run.EndStatus = JobRunStatus.Skipped.ToString();
                    run.EndStatusData = JsonConvert.SerializeObject(new { skippedDueToRules = skippedDueToRules, schedulerData = context.SchedulerData });
                    db.SaveChanges();
                }
                return run;
            }
        }

        public class TimeSlot
        {
            public string Name { get; set; }
            public IList<TimeSlotItem> Items { get; set; }
        }

        public class TimeSlotItem
        {
            public ServiceCallModel ServiceCall { get; internal set; }
            public ITriggerLimitation TriggerLimitation { get; set; }
        }

        public interface ITriggerLimitation
        {
            string GetDescription();
            bool ShouldRun(TimeSlotItem item, TimeSlotRunContext context, out IList<string> skippedDueToRules);
        }

        public class RunIfAnyTriggerLimitation : ITriggerLimitation
        {
            private IList<ITriggerLimitation> triggers;

            public RunIfAnyTriggerLimitation(IEnumerable<ITriggerLimitation> triggers)
            {
                this.triggers = triggers.ToList();
            }

            public string GetDescription()
            {
                return string.Join(" OR ", triggers.Select(x => $"[{x.GetDescription()}]"));
            }

            public bool ShouldRun(TimeSlotItem item, TimeSlotRunContext context, out IList<string> skippedDueToRules)
            {
                var rr = new List<string>();
                var shouldRun = false;
                foreach (var t in triggers)
                {
                    IList<string> s;
                    shouldRun = t.ShouldRun(item, context, out s) || shouldRun; //NOTE: Don't change the order we want all the rules to run to log the rules
                    rr.Add(string.Join(" AND ", s));
                }
                if (!shouldRun)
                    skippedDueToRules = new List<string> { string.Join(" OR ", rr) };
                else
                    skippedDueToRules = null;
                return shouldRun;
            }
        }

        public class SimpleTriggerLimitation : ITriggerLimitation
        {
            private bool isAlwaysRun;
            private ISet<int> onlyOnTheseDaysRule;
            private ISet<int> onlyOnTheseMonthsRule;
            private IList<Tuple<string, ISet<JobRunStatus>>> onlyOnOtherCallStatusRules;

            public SimpleTriggerLimitation(bool isAlwaysRun, ISet<int> onlyOnTheseDaysRule, ISet<int> onlyOnTheseMonthsRule, IList<Tuple<string, ISet<JobRunStatus>>> onlyOnOtherCallStatusRules)
            {
                this.isAlwaysRun = isAlwaysRun;
                this.onlyOnOtherCallStatusRules = onlyOnOtherCallStatusRules;
                this.onlyOnTheseDaysRule = onlyOnTheseDaysRule;
                this.onlyOnTheseMonthsRule = onlyOnTheseMonthsRule;
            }

            public string GetDescription()
            {
                if (isAlwaysRun)
                    return "";
                else
                {
                    var b = new List<string>();

                    if (onlyOnTheseDaysRule != null)
                        b.Add($"{GetOnlyTheseDaysDescription()} ");
                    if (onlyOnTheseMonthsRule != null)
                        b.Add($"{GetOnlyTheseMonthsDescription()} ");
                    if (onlyOnOtherCallStatusRules != null)
                    {
                        foreach (var r in onlyOnOtherCallStatusRules)
                        {
                            b.Add($"{GetStatusRuleName(r)} ");
                        }
                    }
                    return string.Join(" AND ", b);
                }
            }

            private string GetOnlyTheseDaysDescription()
            {
                return $"OnlyTheseDays({string.Join(", ", onlyOnTheseDaysRule)})";
            }

            private string GetOnlyTheseMonthsDescription()
            {
                return $"OnlyTheseMonths({string.Join(", ", onlyOnTheseMonthsRule)})";
            }

            private string GetStatusRuleName(Tuple<string, ISet<JobRunStatus>> r)
            {
                return $"OnlyOnOtherJobStatus({r.Item1}, {string.Join(", ", r.Item2.Select(x => x.ToString()))})";
            }

            public bool ShouldRun(TimeSlotItem item, TimeSlotRunContext context, out IList<string> skippedDueToRules)
            {
                skippedDueToRules = new List<string>();
                if (isAlwaysRun)
                {
                    return true;
                }
                else
                {
                    if (onlyOnTheseDaysRule != null && !onlyOnTheseDaysRule.Contains(context.Clock.Today.Day))
                    {
                        skippedDueToRules.Add(GetOnlyTheseDaysDescription());
                        NLog.Information("Skipped {jobName} due to OnlyTheseDaysRule", item.ServiceCall.Name);
                        return false;
                    }
                    if (onlyOnTheseMonthsRule != null && !onlyOnTheseMonthsRule.Contains(context.Clock.Today.Month))
                    {
                        skippedDueToRules.Add(GetOnlyTheseMonthsDescription());
                        NLog.Information("Skipped {jobName} due to OnlyTheseMonthsRule", item.ServiceCall.Name);
                        return false;
                    }
                    if (onlyOnOtherCallStatusRules != null)
                    {
                        foreach (var r in onlyOnOtherCallStatusRules)
                        {
                            if (!context.JobStatusesByJobName.ContainsKey(r.Item1))
                            {
                                skippedDueToRules.Add(GetStatusRuleName(r));
                                NLog.Information("Skipped {jobName} due to OnlyOnOtherJobStatusRules. Other job did not run at all", item.ServiceCall.Name);
                                return false;
                            }
                            var otherJobStatus = context.JobStatusesByJobName[r.Item1];
                            if (!r.Item2.Contains(otherJobStatus))
                            {
                                skippedDueToRules.Add(GetStatusRuleName(r));
                                NLog.Information("Skipped {jobName} due to OnlyOnOtherJobStatusRules. Other job did not finish with allowed status", item.ServiceCall.Name);
                                return false;
                            }
                        }
                    }

                    return true;
                }
            }
        }

        public static SchedulerModel Parse(XDocument d, ServiceRegistry sr, Func<string, bool> isFeatureEnabled, string clientCountry)
        {
            //Trim whitepace
            foreach (var e in d.Descendants().ToList())
            {
                foreach (var a in e.Attributes().ToList())
                {
                    a.Value = a.Value?.Trim();
                }
                if (!e.HasElements && !string.IsNullOrWhiteSpace(e.Value))
                {
                    e.Value = e.Value?.Trim();
                }
            }

            var agent = d.XPathSelectElement("/SchedulerSetup/ScheduleRunner")?.Value;
            if (agent != ScheduleRunnerAgentCode.ModuleDbSqlAgent.ToString())
                throw new Exception("Invalid ScheduleRunner: " + agent);

            //Parse all services
            var servicesBase = d
                .XPathSelectElements("/SchedulerSetup/ServiceCalls/ServiceCall")
                .Select(x =>
                {
                    var name = x?.Attribute("name")?.Value;
                    var jobUrlElement = x.XPathSelectElement("ServiceUrl");
                    var urlServiceName = jobUrlElement?.Attribute("service")?.Value;
                    var urlRelative = jobUrlElement?.Value;
                    if (urlServiceName == null || urlRelative == null)
                        throw new Exception($"Missing ServiceUrl for ServiceCall '{name}'");

                    return new
                    {
                        Name = name,
                        IsManualTriggerAllowed = (x?.Attribute("allowManualTrigger")?.Value ?? "").ToLowerInvariant().Trim() == "true",
                        FeatureToggle = (x?.Attribute("featureToggle")?.Value ?? "").ToLowerInvariant().Trim(),
                        ClientCountry = (x?.Attribute("clientCountry")?.Value ?? "").Trim(),
                        UrlServiceName = urlServiceName,
                        UrlRelative = urlRelative,
                        Tags = (x.XPathSelectElements("Tag")?.Select(y => y?.Value)?.ToList()) ?? new List<string>()
                    };
                });

            var services = servicesBase
                .Where(x => sr.ContainsService(x.UrlServiceName)
                    && (string.IsNullOrWhiteSpace(x.FeatureToggle) || isFeatureEnabled(x.FeatureToggle))
                    && (string.IsNullOrWhiteSpace(x.ClientCountry) || clientCountry.EqualsIgnoreCase(x.ClientCountry)))
                .Select(x => new ServiceCallModel
                {
                    Name = x.Name,
                    IsManualTriggerAllowed = x.IsManualTriggerAllowed,
                    ServiceUrl = sr.CreateServiceUri(x.UrlServiceName, x.UrlRelative),
                    ServiceName = x.UrlServiceName,
                    Tags = new HashSet<string>(x.Tags)
                })
                .ToDictionary(x => x.Name, x => x, StringComparer.InvariantCultureIgnoreCase);

            var skippedServiceCalls = servicesBase
                .Where(x => !services.Keys.Contains(x.Name))
                .ToDictionary(x => x.Name);

            if (skippedServiceCalls.Any())
            {
                NLog.Information($"Scheduler skipped services '{string.Join(", ", skippedServiceCalls.Keys)}' since those service are not deployed to this environment");
            }

            var timeSlots = d.XPathSelectElements("/SchedulerSetup/TimeSlots/TimeSlot").Select(x =>
            {
                var items = new List<TimeSlotItem>();
                var timeSlotName = x.Attribute("name")?.Value;
                if (timeSlotName == null)
                    throw new Exception("Missing timeslot name");

                foreach (var timeSlotItemElement in x.XPathSelectElements("TimeSlotItem"))
                {
                    var serviceCallName = timeSlotItemElement?.Attribute("serviceCallName")?.Value;
                    if (skippedServiceCalls.ContainsKey(serviceCallName))
                    {
                        continue;
                    }
                    if (serviceCallName == null)
                        throw new Exception("Missing serviceCallName");

                    var triggerRules = timeSlotItemElement.XPathSelectElements("TriggerRule");
                    if (triggerRules == null || !triggerRules.Any())
                        throw new Exception("Missing TriggerRule elements");

                    var triggers = new List<SimpleTriggerLimitation>();
                    foreach (var triggerRule in triggerRules)
                    {
                        SimpleTriggerLimitation trigger;
                        var triggerValue = triggerRule?.Value;
                        if (triggerValue == "AlwaysRun")
                        {
                            trigger = new SimpleTriggerLimitation(true, null, null, null);
                        }
                        else
                        {
                            ISet<int> onlyOnTheseDaysRule = null;
                            ISet<int> onlyOnTheseMonthsRule = null;
                            IList<Tuple<string, ISet<JobRunStatus>>> onlyOnOtherCallStatusRule = null;

                            var onlyOnTheseDays = triggerRule.XPathSelectElement("OnlyTheseDays")?.Value;
                            if (onlyOnTheseDays != null)
                                onlyOnTheseDaysRule = new HashSet<int>(onlyOnTheseDays.Split(',').Select(y => y.Trim()).Where(y => y.Length > 0).Select(y => int.Parse(y)));

                            var onlyOnTheseMonths = triggerRule.XPathSelectElement("OnlyTheseMonths")?.Value;
                            if (onlyOnTheseMonths != null)
                                onlyOnTheseMonthsRule = new HashSet<int>(onlyOnTheseMonths.Split(',').Select(y => y.Trim()).Where(y => y.Length > 0).Select(y => int.Parse(y)));

                            foreach (var onlyOnOtherJobStatusElement in triggerRule?.XPathSelectElements("OnlyOnOtherCallStatus"))
                            {
                                var jn = onlyOnOtherJobStatusElement.Attribute("serviceCallName")?.Value;
                                if (jn == null)
                                    throw new Exception("Missing serviceCallName");
                                var statuses = onlyOnOtherJobStatusElement
                                    .Attribute("allowedStatuses")
                                    ?.Value?.Split(',')
                                    ?.Select(y => y.Trim())
                                    ?.Where(y => y.Length > 0)
                                    ?.Select(y => (JobRunStatus)Enum.Parse(typeof(JobRunStatus), y))
                                    ?.ToList();

                                if (statuses == null || statuses.Count == 0)
                                    throw new Exception("Missing or empty allowedStatuses");

                                if (onlyOnOtherCallStatusRule == null)
                                    onlyOnOtherCallStatusRule = new List<Tuple<string, ISet<JobRunStatus>>>();
                                onlyOnOtherCallStatusRule.Add(Tuple.Create(jn, (ISet<JobRunStatus>)new HashSet<JobRunStatus>(statuses)));
                            }

                            if (onlyOnTheseDaysRule == null && onlyOnOtherCallStatusRule == null && onlyOnTheseMonthsRule == null)
                                throw new Exception("No TriggerRules");

                            trigger = new SimpleTriggerLimitation(false, onlyOnTheseDaysRule, onlyOnTheseMonthsRule, onlyOnOtherCallStatusRule);
                        }
                        triggers.Add(trigger);
                    }

                    if (triggers.Count == 0)
                        throw new Exception("No Triggers");

                    if (!services.ContainsKey(serviceCallName))
                    {
                        throw new Exception($"TimeSlotItem references serviceCallName={serviceCallName} that is not declared in Services");
                    }
                    items.Add(new TimeSlotItem
                    {
                        ServiceCall = services[serviceCallName],
                        TriggerLimitation = triggers.Count == 1 ? triggers[0] : (ITriggerLimitation)new RunIfAnyTriggerLimitation(triggers.AsEnumerable<ITriggerLimitation>())
                    });
                }

                return new TimeSlot
                {
                    Name = timeSlotName,
                    Items = items
                };
            }).ToList();

            //TODO: Check that jobstatus check point to jobnames that exist and are scheduled before in the same timeslot
            return new SchedulerModel
            {
                RunnerAgent = ScheduleRunnerAgentCode.ModuleDbSqlAgent,
                Timeslots = timeSlots,
                ServiceCalls = services
            };
        }
    }
}