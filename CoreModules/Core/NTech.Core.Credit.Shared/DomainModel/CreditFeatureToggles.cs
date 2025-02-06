using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.Credit.Shared.DomainModel
{
    public static class CreditFeatureToggles
    {
        public static string AgreementNr = "ntech.feature.agreementnr";
        public static string CoNotification = "ntech.feature.conotification";

        public static bool IsCoNotificationEnabled(IClientConfigurationCore clientConfig) => clientConfig.IsFeatureEnabled(CoNotification);
        public static bool IsAgreementNrEnabled(IClientConfigurationCore clientConfig) => clientConfig.IsFeatureEnabled(AgreementNr);
    }
}
