using NTech.Core.Customer.Shared;
using NTech.Core.Customer.Shared.Models;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.Customer
{
    public class CustomerEnvSettings : ICustomerEnvSettings
    {
        private readonly NEnv env;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly FewItemsCache cache;

        public CustomerEnvSettings(NEnv env, IClientConfigurationCore clientConfiguration)
        {
            this.env = env;
            this.clientConfiguration = clientConfiguration;
            cache = new FewItemsCache();
        }

        private IClientConfigurationCore ClientCfg => clientConfiguration;
        private string Opt(string name) => env.OptionalSetting(name);
        private string Req(string name) => env.RequiredSetting(name);

        public bool IsProduction
        {
            get
            {
                var s = Req("ntech.isproduction");
                return s.Trim().ToLower() == "true";
            }
        }

        public Dictionary<string, KycQuestionsTemplate> DefaultKycQuestionsSets
        {
            get
            {
                var f = env.ClientResourceFile("ntech.kyc.ui.questionsset", "kyc-questions.json", mustExist: false);
                return cache.WithCache("0ad9b3b1-b7a2-4a62-8652-2438a64aaf4c", TimeSpan.FromMinutes(15), () => f.Exists
                    ? KycQuestionsTemplate.ParseDefaultSetting(File.ReadAllText(f.FullName))
                    : new Dictionary<string, KycQuestionsTemplate>()
                );
            }
        }

        public bool IsTemplateCacheDisabled => string.Equals((Opt("ntech.document.disabletemplatecache") ?? "false"), "true", StringComparison.InvariantCultureIgnoreCase);
        public string RelativeKycLogFolder => env.OptionalSetting("ntech.customer.kyc.queryitemslogfolder");
        public string LogFolder => Opt("ntech.logfolder");
    }
}
