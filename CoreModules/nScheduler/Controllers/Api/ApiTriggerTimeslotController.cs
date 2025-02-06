using NTech.Legacy.Module.Shared;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Web.Mvc;

namespace nScheduler.Controllers
{
    [NTechAuthorizeAdmin]
    [RoutePrefix("Api")]
    public class ApiTriggerTimeslotController : NController
    {
        private Lazy<NTechSelfRefreshingBearerToken> serviceTriggerSystemUser = new Lazy<NTechSelfRefreshingBearerToken>(
            () =>
            {
                var user = NEnv.AutomationUser;
                return NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistryNormal, user.Username, user.Password);
            });

        [Route("TriggerServiceManually")]
        [HttpPost]
        public ActionResult TriggerServiceManually(string serviceName)
        {
            try
            {
                var model = NEnv.SchedulerModel;
                var accessToken = CurrentUserAccessToken; //NOTE: Called as the current user as they are only calling a single service so better if logged who.
                if (!model.ServiceCalls.ContainsKey(serviceName))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such service");
                var serviceCall = model.ServiceCalls[serviceName];
                if (!serviceCall.IsManualTriggerAllowed)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Manual trigger is not allowed");

                var result = model.TriggerServiceDirectly(serviceName, accessToken, new Dictionary<string, string>(), this.Clock, this.CurrentUserId, this.InformationMetadata);
                var jobStatus = Api.ApiFetchServiceRunsController.CreateLastJobRunStatus(serviceCall, result, this.GetUserDisplayNameByUserId);

                return Json2(jobStatus);
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Error in TriggerServiceManually for {serviceName}", serviceName);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Route("TriggerTimeslot")]
        [HttpPost]
        public ActionResult TriggerTimeslot(string name, IDictionary<string, string> schedulerData)
        {
            try
            {
                var model = NEnv.SchedulerModel;
                model.TriggerTimeslot(name, serviceTriggerSystemUser.Value, schedulerData, this.Clock, this.CurrentUserId, this.InformationMetadata);
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Error in TriggerTimeslot for {jobName}", name);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Route("TriggerTimeslotByName/{name}")]
        [HttpPost]
        public ActionResult TriggerTimeslot2(string name, IDictionary<string, string> schedulerData = null)
        {
            try
            {
                var model = NEnv.SchedulerModel;
                model.TriggerTimeslot(name, serviceTriggerSystemUser.Value, schedulerData, this.Clock, this.CurrentUserId, this.InformationMetadata);
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Error in TriggerTimeslot for {jobName}", name);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Route("TriggerTimeslotByNameDryRun/{name}")]
        [HttpPost]
        public ActionResult TriggerTimeslotByNameDryRun(string name, IDictionary<string, string> schedulerData = null)
        {
            try
            {
                var sleepSeconds = int.Parse(NTechEnvironmentLegacy.SharedInstance.OptionalSetting("ntech.scheduler.dryrun.sleepseconds") ?? "0");
                NLog.Information($"TriggerTimeslotByNameDryRun triggered for: {name}. Sleeptime = {sleepSeconds} seconds.");
                var model = NEnv.SchedulerModel; //NOTE: Dont remove. This api should not actually trigger anything but we want it to parse the schedule to make sure we find out if it's broken.
                if (sleepSeconds > 0)
                    Thread.Sleep(sleepSeconds);
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Error in TriggerTimeslotByNameDryRun for {jobName}", name);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }
    }
}