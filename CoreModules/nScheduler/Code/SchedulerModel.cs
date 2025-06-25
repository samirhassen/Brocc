using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json;
using nScheduler.Code;
using NTech;
using NTech.Core.Module.Shared.Clients;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Eventing;
using Serilog;

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
        private static readonly IServiceClientSyncConverter SyncConverter = new ServiceClientSyncConverterLegacy();

        public ScheduleRunnerAgentCode RunnerAgent { get; set; }
        public List<TimeSlot> Timeslots { get; set; }
        public IDictionary<string, ServiceCallModel> ServiceCalls { get; set; }

        private static string GetLockName(string timeslotName)
        {
            return $"ntech.scheduler.timeslot.{timeslotName}";
        }

        public void AddServiceCallIfNotExists(ServiceCallModel serviceCall)
        {
            ServiceCalls ??= new Dictionary<string, ServiceCallModel>();

            if (!ServiceCalls.ContainsKey(serviceCall.Name))
                ServiceCalls.Add(serviceCall.Name, serviceCall);
        }

        public void TriggerTimeslot(string name, NTechSelfRefreshingBearerToken accessToken,
            IDictionary<string, string> schedulerData, IClock clock, int currentUserId, string informationMetadata)
        {
            var result = TriggerTimeslotWithLock(name, accessToken, schedulerData, clock, currentUserId,
                informationMetadata);
            NTechEventHandler.PublishEvent(
                nameof(SchedulerEventCode.TriggerTimeSlotCompleted),
                JsonConvert.SerializeObject(new
                {
                    timeslotName = name,
                    serviceRuns = result,
                    schedulerData = schedulerData
                }));
        }

        public ServiceRun TriggerServiceDirectly(string serviceName, string accessToken,
            IDictionary<string, string> schedulerData, IClock clock, int currentUserId, string informationMetadata)
        {
            var service = ServiceCalls[serviceName];
            var w = Stopwatch.StartNew();
            using var db = new SchedulerContext();
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
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                client.SetBearerToken(accessToken);
                client.Timeout = TimeSpan.FromHours(4);
                var result = SyncConverter.ToSync(() =>
                    client.PostAsJsonAsync(service.ServiceUrl, new { schedulerData = schedulerData }));
                if (result.IsSuccessStatusCode)
                {
                    var endDate = clock.Now;
                    run.EndDate = endDate;
                    var r = SyncConverter.ToSync(() => result.Content.ReadAsAsync<PartialScheduledJobResult>());
                    if (r?.Errors is { Count: > 0 })
                    {
                        run.RuntimeInMs = w.ElapsedMilliseconds;
                        run.EndStatus = nameof(JobRunStatus.Error);
                        run.EndStatusData = JsonConvert.SerializeObject(new
                            { errors = r.Errors, warnings = r.Warnings, schedulerData = schedulerData });
                    }
                    else if (r?.Warnings is { Count: > 0 })
                    {
                        run.RuntimeInMs = w.ElapsedMilliseconds;
                        run.EndStatus = nameof(JobRunStatus.Warning);
                        run.EndStatusData = JsonConvert.SerializeObject(new
                            { warnings = r.Warnings, schedulerData = schedulerData });
                    }
                    else
                    {
                        run.RuntimeInMs = w.ElapsedMilliseconds;
                        run.EndStatus = nameof(JobRunStatus.Success);
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
                run.EndStatus = nameof(JobRunStatus.Error);
                run.EndStatusData = JsonConvert.SerializeObject(new
                    { errors = new[] { ex.Message }, schedulerData = schedulerData });
            }

            db.SaveChanges();
            return run;
        }

        private IList<ServiceRun> TriggerTimeslotWithLock(string name, NTechSelfRefreshingBearerToken accessToken,
            IDictionary<string, string> schedulerData, IClock clock, int currentUserId, string informationMetadata)
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
                    AccessToken = accessToken,
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

                if (context.AlertContext?.TimeslotItems != null)
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
            }, () => throw new Exception($"The timeslot {name} was already running"));
        }

        public class TimeSlotRunContext
        {
            public NTechSelfRefreshingBearerToken AccessToken { get; internal set; }
            public IDictionary<string, string> SchedulerData { get; set; }

            public IDictionary<string, JobRunStatus> JobStatusesByJobName { get; set; } =
                new Dictionary<string, JobRunStatus>(StringComparer.InvariantCultureIgnoreCase);

            public IDictionary<string, IList<string>> WarningsByJobName { get; set; } =
                new Dictionary<string, IList<string>>(StringComparer.InvariantCultureIgnoreCase);

            public TimeSlot TimeSlot { get; internal set; }
            public int CurrentUserId { get; set; }
            public string InformationMetadata { get; set; }
            public IClock Clock { get; set; }
            public SchedulerAfterRunAlertMessageContext AlertContext { get; set; }
        }

        private class PartialScheduledJobResult
        {
            //Jobs dont have to implement this but the warning level only works if it is
            public List<string> Warnings { get; set; }
            public List<string> Errors { get; set; }
        }

        private static bool IsSkippedBySchedulerData(TimeSlotItem item, TimeSlotRunContext context)
        {
            if (context.SchedulerData != null && context.SchedulerData.TryGetValue("skippedJobNames", out var value))
            {
                var skippedNames =
                    JsonConvert.DeserializeObject<List<string>>(value);
                if (skippedNames.Contains(item.ServiceCall?.Name, StringComparer.InvariantCultureIgnoreCase))
                    return true;
            }

            if (context.SchedulerData == null || !context.SchedulerData.TryGetValue("onlyRunForTheseServiceNames",
                    out var onlyRunForTheseServiceNamesSetting)) return false;

            if (!(onlyRunForTheseServiceNamesSetting?.Contains(item.ServiceCall?.ServiceName) ?? false))
                return true;

            return false;
        }

        private static ServiceRun TriggerJob(TimeSlotItem item, TimeSlotRunContext context)
        {
            var runs = new List<ServiceRun>();

            var w = Stopwatch.StartNew();
            using var db = new SchedulerContext();
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

            var shouldRun = item.TriggerLimitation.ShouldRun(item, context, out var skippedDueToRules);
            if (shouldRun && IsSkippedBySchedulerData(item, context))
            {
                shouldRun = false;
                skippedDueToRules ??= new List<string>();
                skippedDueToRules.Add("SchedulerDataOverride");
            }

            if (shouldRun)
            {
                try
                {
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                    client.SetBearerToken(context.AccessToken.GetToken());
                    client.Timeout = TimeSpan.FromHours(4);
                    var result = SyncConverter.ToSync(() => client.PostAsJsonAsync(item.ServiceCall.ServiceUrl,
                        new { schedulerData = context.SchedulerData }));
                    if (result.IsSuccessStatusCode)
                    {
                        var endDate = context.Clock.Now;
                        run.EndDate = endDate;
                        var r = SyncConverter.ToSync(() => result.Content.ReadAsAsync<PartialScheduledJobResult>());
                        if (r?.Errors is { Count: > 0 })
                        {
                            context.JobStatusesByJobName[item.ServiceCall.Name] = JobRunStatus.Error;
                            context.WarningsByJobName[item.ServiceCall.Name] = r.Warnings;
                            run.RuntimeInMs = w.ElapsedMilliseconds;
                            run.EndStatus = nameof(JobRunStatus.Error);
                            run.EndStatusData = JsonConvert.SerializeObject(new
                            {
                                errors = r.Errors, warnings = r.Warnings, schedulerData = context.SchedulerData
                            });
                            context.AlertContext?.AddItem(run.JobName, run.EndStatus, r.Errors);
                        }
                        else if (r?.Warnings is { Count: > 0 })
                        {
                            context.JobStatusesByJobName[item.ServiceCall.Name] = JobRunStatus.Warning;
                            context.WarningsByJobName[item.ServiceCall.Name] = r.Warnings;
                            run.RuntimeInMs = w.ElapsedMilliseconds;
                            run.EndStatus = nameof(JobRunStatus.Warning);
                            run.EndStatusData = JsonConvert.SerializeObject(new
                                { warnings = r.Warnings, schedulerData = context.SchedulerData });
                            context.AlertContext?.AddItem(run.JobName, run.EndStatus, r.Warnings);
                        }
                        else
                        {
                            context.JobStatusesByJobName[item.ServiceCall.Name] = JobRunStatus.Success;
                            run.RuntimeInMs = w.ElapsedMilliseconds;
                            run.EndStatus = nameof(JobRunStatus.Success);
                            run.EndStatusData =
                                JsonConvert.SerializeObject(new { schedulerData = context.SchedulerData });
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
                    run.EndStatus = nameof(JobRunStatus.Error);
                    run.EndStatusData = JsonConvert.SerializeObject(new
                        { errors = new[] { ex.Message }, schedulerData = context.SchedulerData });
                    db.SaveChanges();
                    context.AlertContext?.AddItem(run.JobName, run.EndStatus, [ex.Message]);
                }
            }
            else
            {
                context.JobStatusesByJobName[item.ServiceCall.Name] = JobRunStatus.Skipped;
                var endDate = context.Clock.Now;
                run.EndDate = endDate;
                run.RuntimeInMs = w.ElapsedMilliseconds;
                run.EndStatus = nameof(JobRunStatus.Skipped);
                run.EndStatusData = JsonConvert.SerializeObject(new
                    { skippedDueToRules = skippedDueToRules, schedulerData = context.SchedulerData });
                db.SaveChanges();
            }

            return run;
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

        public class RunIfAnyTriggerLimitation(in IEnumerable<ITriggerLimitation> triggers) : ITriggerLimitation
        {
            private readonly IList<ITriggerLimitation> _triggers = triggers.ToList();

            public string GetDescription()
            {
                return string.Join(" OR ", _triggers.Select(x => $"[{x.GetDescription()}]"));
            }

            public bool ShouldRun(TimeSlotItem item, TimeSlotRunContext context, out IList<string> skippedDueToRules)
            {
                var rr = new List<string>();
                var shouldRun = false;
                foreach (var t in _triggers)
                {
                    shouldRun = t.ShouldRun(item, context, out var s) ||
                                shouldRun; //NOTE: Don't change the order we want all the rules to run to log the rules
                    rr.Add(string.Join(" AND ", s));
                }

                skippedDueToRules = !shouldRun ? new List<string> { string.Join(" OR ", rr) } : null;
                return shouldRun;
            }
        }

        public class SimpleTriggerLimitation(
            in bool isAlwaysRun,
            in ISet<int> onlyOnTheseDaysRule,
            in ISet<int> onlyOnTheseMonthsRule,
            in IList<Tuple<string, ISet<JobRunStatus>>> onlyOnOtherCallStatusRules)
            : ITriggerLimitation
        {
            private readonly bool _isAlwaysRun = isAlwaysRun;
            private readonly ISet<int> _onlyOnTheseDaysRule = onlyOnTheseDaysRule;
            private readonly ISet<int> _onlyOnTheseMonthsRule = onlyOnTheseMonthsRule;

            private readonly IList<Tuple<string, ISet<JobRunStatus>>> _onlyOnOtherCallStatusRules =
                onlyOnOtherCallStatusRules;

            public string GetDescription()
            {
                if (_isAlwaysRun)
                    return "";
                var b = new List<string>();

                if (_onlyOnTheseDaysRule != null)
                    b.Add($"{GetOnlyTheseDaysDescription()} ");
                if (_onlyOnTheseMonthsRule != null)
                    b.Add($"{GetOnlyTheseMonthsDescription()} ");
                if (_onlyOnOtherCallStatusRules != null)
                {
                    b.AddRange(_onlyOnOtherCallStatusRules.Select(r => $"{GetStatusRuleName(r)} "));
                }

                return string.Join(" AND ", b);
            }

            private string GetOnlyTheseDaysDescription()
            {
                return $"OnlyTheseDays({string.Join(", ", _onlyOnTheseDaysRule)})";
            }

            private string GetOnlyTheseMonthsDescription()
            {
                return $"OnlyTheseMonths({string.Join(", ", _onlyOnTheseMonthsRule)})";
            }

            private static string GetStatusRuleName(Tuple<string, ISet<JobRunStatus>> r)
            {
                return $"OnlyOnOtherJobStatus({r.Item1}, {string.Join(", ", r.Item2.Select(x => x.ToString()))})";
            }

            public bool ShouldRun(TimeSlotItem item, TimeSlotRunContext context, out IList<string> skippedDueToRules)
            {
                skippedDueToRules = new List<string>();
                if (_isAlwaysRun) return true;

                if (_onlyOnTheseDaysRule != null && !_onlyOnTheseDaysRule.Contains(context.Clock.Today.Day))
                {
                    skippedDueToRules.Add(GetOnlyTheseDaysDescription());
                    NLog.Information("Skipped {jobName} due to OnlyTheseDaysRule", item.ServiceCall.Name);
                    return false;
                }

                if (_onlyOnTheseMonthsRule != null && !_onlyOnTheseMonthsRule.Contains(context.Clock.Today.Month))
                {
                    skippedDueToRules.Add(GetOnlyTheseMonthsDescription());
                    NLog.Information("Skipped {jobName} due to OnlyTheseMonthsRule", item.ServiceCall.Name);
                    return false;
                }

                if (_onlyOnOtherCallStatusRules == null) return true;

                foreach (var r in _onlyOnOtherCallStatusRules)
                {
                    if (!context.JobStatusesByJobName.TryGetValue(r.Item1, out var otherJobStatus))
                    {
                        skippedDueToRules.Add(GetStatusRuleName(r));
                        NLog.Information(
                            "Skipped {jobName} due to OnlyOnOtherJobStatusRules. Other job did not run at all",
                            item.ServiceCall.Name);
                        return false;
                    }

                    if (r.Item2.Contains(otherJobStatus)) continue;

                    skippedDueToRules.Add(GetStatusRuleName(r));
                    NLog.Information(
                        "Skipped {jobName} due to OnlyOnOtherJobStatusRules. Other job did not finish with allowed status",
                        item.ServiceCall.Name);
                    return false;
                }

                return true;
            }
        }

        public static SchedulerModel Parse(XDocument d, ServiceRegistry sr, Func<string, bool> isFeatureEnabled,
            string clientCountry)
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
            if (agent != nameof(ScheduleRunnerAgentCode.ModuleDbSqlAgent))
                throw new Exception("Invalid ScheduleRunner: " + agent);

            //Parse all services
            var servicesBase = d
                .XPathSelectElements("/SchedulerSetup/ServiceCalls/ServiceCall")
                .Select(sc =>
                {
                    var name = sc?.Attribute("name")?.Value;
                    var jobUrlElement = sc.XPathSelectElement("ServiceUrl");
                    var urlServiceName = jobUrlElement?.Attribute("service")?.Value;
                    var urlRelative = jobUrlElement?.Value;
                    if (urlServiceName == null || urlRelative == null)
                        throw new Exception($"Missing ServiceUrl for ServiceCall '{name}'");

                    var parameters = sc.XPathSelectElements("ServiceParameter")
                        .Where(sp =>
                            !string.IsNullOrWhiteSpace(sp.Value) &&
                            !string.IsNullOrWhiteSpace(sp.Attribute("name")?.Value))
                        .ToDictionary(sp => sp.Attribute("name")?.Value, sp => sp.Value);

                    return new
                    {
                        Name = name,
                        IsManualTriggerAllowed =
                            (sc.Attribute("allowManualTrigger")?.Value ?? "").ToLowerInvariant().Trim() == "true",
                        FeatureToggle = (sc.Attribute("featureToggle")?.Value ?? "").ToLowerInvariant().Trim(),
                        ClientCountry = (sc.Attribute("clientCountry")?.Value ?? "").Trim(),
                        UrlServiceName = urlServiceName,
                        UrlRelative = urlRelative,
                        Parameters = parameters,
                        Tags = (sc.XPathSelectElements("Tag")?.Select(y => y?.Value)?.ToList()) ?? new List<string>()
                    };
                })
                .ToList();

            var services = servicesBase
                .Where(x => sr.ContainsService(x.UrlServiceName)
                            && (string.IsNullOrWhiteSpace(x.FeatureToggle) || isFeatureEnabled(x.FeatureToggle))
                            && (string.IsNullOrWhiteSpace(x.ClientCountry) ||
                                clientCountry.EqualsIgnoreCase(x.ClientCountry)))
                .Select(x => new ServiceCallModel
                {
                    Name = x.Name,
                    IsManualTriggerAllowed = x.IsManualTriggerAllowed,
                    ServiceUrl = sr.CreateServiceUri(x.UrlServiceName, x.UrlRelative, x.Parameters),
                    ServiceName = x.UrlServiceName,
                    Tags = [..x.Tags]
                })
                .ToDictionary(x => x.Name, x => x, StringComparer.InvariantCultureIgnoreCase);

            var skippedServiceCalls = servicesBase
                .Where(x => !services.Keys.Contains(x.Name))
                .ToDictionary(x => x.Name);

            if (skippedServiceCalls.Any())
            {
                NLog.Information(
                    $"Scheduler skipped services '{string.Join(", ", skippedServiceCalls.Keys)}' since those service are not deployed to this environment");
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
                    if (serviceCallName == null)
                        throw new Exception("Missing serviceCallName");

                    if (skippedServiceCalls.ContainsKey(serviceCallName))
                    {
                        continue;
                    }

                    var triggerRules = timeSlotItemElement?.XPathSelectElements("TriggerRule")?.ToList();
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
                                onlyOnTheseDaysRule = new HashSet<int>(onlyOnTheseDays.Split(',').Select(y => y.Trim())
                                    .Where(y => y.Length > 0).Select(int.Parse));

                            var onlyOnTheseMonths = triggerRule.XPathSelectElement("OnlyTheseMonths")?.Value;
                            if (onlyOnTheseMonths != null)
                                onlyOnTheseMonthsRule = new HashSet<int>(onlyOnTheseMonths.Split(',')
                                    .Select(y => y.Trim()).Where(y => y.Length > 0).Select(int.Parse));

                            foreach (var onlyOnOtherJobStatusElement in triggerRule?.XPathSelectElements(
                                         "OnlyOnOtherCallStatus"))
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

                                onlyOnOtherCallStatusRule ??= new List<Tuple<string, ISet<JobRunStatus>>>();
                                onlyOnOtherCallStatusRule.Add(Tuple.Create(jn,
                                    (ISet<JobRunStatus>)new HashSet<JobRunStatus>(statuses)));
                            }

                            if (onlyOnTheseDaysRule == null && onlyOnOtherCallStatusRule == null &&
                                onlyOnTheseMonthsRule == null)
                                throw new Exception("No TriggerRules");

                            trigger = new SimpleTriggerLimitation(false, onlyOnTheseDaysRule, onlyOnTheseMonthsRule,
                                onlyOnOtherCallStatusRule);
                        }

                        triggers.Add(trigger);
                    }

                    if (triggers.Count == 0)
                        throw new Exception("No Triggers");

                    if (!services.TryGetValue(serviceCallName, out var service))
                    {
                        throw new Exception(
                            $"TimeSlotItem references serviceCallName={serviceCallName} that is not declared in Services");
                    }

                    items.Add(new TimeSlotItem
                    {
                        ServiceCall = service,
                        TriggerLimitation = triggers.Count == 1
                            ? triggers[0]
                            : new RunIfAnyTriggerLimitation(
                                triggers.AsEnumerable<ITriggerLimitation>())
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