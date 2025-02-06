using Newtonsoft.Json;
using NTech.Core.Module.Shared.Infrastructure;
using System.Collections.Generic;

namespace nTest.Code
{
    public class EnvironmentResetService
    {
        private readonly INTechEnvironment environment;

        public EnvironmentResetService(INTechEnvironment environment)
        {
            this.environment = environment;
        }

        public List<EnvironmentRestoreJob> GetEnvironmentRestoreJobs()
        {
            var jobsFile = environment.StaticResourceFile("ntech.test.resetenvironmentagent.jobsfile", "resetEnvironmentJobs.json", false);
            if (jobsFile.Exists)
                return JsonConvert.DeserializeObject<List<EnvironmentRestoreJob>>(System.IO.File.ReadAllText(jobsFile.FullName, System.Text.Encoding.UTF8));

            var resetEnvironmentAgentJobName = environment.OptionalSetting("ntech.test.resetenvironmentagentjobname");
            if (resetEnvironmentAgentJobName != null)
                return new List<EnvironmentRestoreJob>
                {
                    new EnvironmentRestoreJob
                    {
                        JobName = resetEnvironmentAgentJobName,
                        DisplayName = "Standard"
                    }
                };

            return new List<EnvironmentRestoreJob>();
        }

        public string ResetEnvironmentAgentJobRunnerPath => environment.OptionalSetting("ntech.test.resetenvironmentjobrunnerpath");
        public string ResetEnvironmentAgentJobServerName => environment.OptionalSetting("ntech.test.resetenvironmentagentjobservername") ?? "localhost";
        public string ResetEnvironmentAgentJobServerConnection => environment.OptionalSetting("ntech.test.resetenvironmentagentjobserverconnection");

        public class EnvironmentRestoreJob
        {
            public string JobName { get; set; }
            public string DisplayName { get; set; }
        }
    }
}