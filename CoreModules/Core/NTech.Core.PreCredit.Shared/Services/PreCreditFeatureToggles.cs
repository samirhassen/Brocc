using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.PreCredit.Shared.Services
{
    public static class PreCreditFeatureToggles
    {
        public static string UnsecuredLoanStandardWebApplicationName = "ntech.feature.unsecuredloans.webapplication";
        public static bool IsUnsecuredLoanStandardWebApplicationEnabled(IClientConfigurationCore clientConfiguration) => 
            clientConfiguration.IsFeatureEnabled(UnsecuredLoanStandardWebApplicationName);
    }
}
