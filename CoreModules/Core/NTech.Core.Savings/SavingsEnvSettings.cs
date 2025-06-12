using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.Shared.BankAccounts.Fi;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Savings.Shared;

namespace NTech.Core.Savings
{
    public class SavingsEnvSettings : ISavingsEnvSettings
    {
        private readonly NEnv env;
        private readonly IClientConfigurationCore clientConfiguration;

        public SavingsEnvSettings(NEnv env, IClientConfigurationCore clientConfiguration)
        {
            this.env = env;
            this.clientConfiguration = clientConfiguration;
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

        public bool IsTemplateCacheDisabled => string.Equals((Opt("ntech.document.disabletemplatecache") ?? "false"), "true", StringComparison.InvariantCultureIgnoreCase);

        public decimal MaxAllowedSavingsCustomerBalance
        {
            get
            {
                var v = Opt("ntech.savings.maxallowedsavingscustomerbalance");
                if (v != null)
                    return decimal.Parse(v, System.Globalization.CultureInfo.InvariantCulture);
                else if (ClientCfg.Country.BaseCurrency == "EUR")
                    return 100000m;
                else
                    throw new NotImplementedException();
            }
        }

        public string OutgoingPaymentFileCustomerMessagePattern => Opt("ntech.savings.outgoingpayments.customermessagepattern");

        public IBANFi OutgoingPaymentIban
        {
            get
            {
                return IBANFi.Parse(Req("ntech.savings.outgoingpaymentiban"));
            }
        }
    }
}
