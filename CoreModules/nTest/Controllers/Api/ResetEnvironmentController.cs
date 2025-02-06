using NTech.Legacy.Module.Shared;
using NTech.Services.Infrastructure;
using nTest.Code;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web.Mvc;

namespace nTest.Controllers.Api
{
    [NTechApi]
    public class ResetEnvironmentController : Controller
    {
        private static string currentResetId = null; //Used to allow knowing if the app pool has restarted

        [Route("Api/ResetEnvironment/Get-Id")]
        [HttpPost]
        public ActionResult GetCurrentResetId()
        {
            //When reset it becomes null the app pool has restarted. If it becomes some other values than the Start one something is really, really broken
            return new JsonNetActionResult
            {
                Data = new
                {
                    currentResetId
                }
            };
        }

        [Route("Api/ResetEnvironment/Start")]
        [HttpPost]
        public ActionResult StartResetEnvironment(string jobName)
        {
            var resetService = new EnvironmentResetService(NTechEnvironmentLegacy.SharedInstance);

            var resetJobs = resetService.GetEnvironmentRestoreJobs();
            if(!resetJobs.Any(x => x.JobName == jobName))
                return new JsonNetActionResult
                {
                    Data = new
                    {
                        errorCode = "invalidJobName",
                        errorMessage = "Invalid job name"
                    },
                    CustomHttpStatusCode = 400,
                    CustomStatusDescription = "Invalid job name"
                };

            var serverName = resetService.ResetEnvironmentAgentJobServerName;
            var jobRunner = resetService.ResetEnvironmentAgentJobRunnerPath;
            var connectionString = resetService.ResetEnvironmentAgentJobServerConnection;
            if (jobName is null || jobRunner is null)
            {
                return new JsonNetActionResult
                {
                    Data = new
                    {
                        errorCode = "missingAppsettings",
                        errorMessage = "Missing appsettings"
                    },
                    CustomHttpStatusCode = 400,
                    CustomStatusDescription = "Missing appsettings"
                };
            }

            var arguments = $"{true} \"{serverName}\" \"{jobName}\"";
            if (connectionString != null)
            {
                arguments += $" \"{connectionString}\"";
            }

            var p = new Process
            {
                StartInfo =
                {
                    FileName = $@"{jobRunner}",
                    Arguments = arguments,
                    UseShellExecute = true,
                    RedirectStandardOutput = false,
                    CreateNoWindow = true
                }
            };

            currentResetId = Guid.NewGuid().ToString();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                //Allow time for the ui to start waiting for the app pool to go down
                Thread.Sleep(TimeSpan.FromSeconds(5));
                p.Start();
            });

            return new JsonNetActionResult
            {
                Data = new
                {
                    currentResetId
                }
            };
        }
    }
}