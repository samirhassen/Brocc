using NTech.Core.Module;

namespace nPreCredit.Code.Services
{
    public class ProviderInfoService : IProviderInfoService
    {
        public ProviderInfoModel GetSingle(string providerName)
        {
            return ToProviderInfoModel(NEnv.GetAffiliateModel(providerName, allowMissing: false));
        }

        private ProviderInfoModel ToProviderInfoModel(AffiliateModel a)
        {
            if (a == null)
                return null;

            return new ProviderInfoModel
            {
                ProviderName = a.ProviderName,
                IsMortgageLoanProvider = a.IsMortgageLoanProvider,
                IsSelf = a.IsSelf,
                IsSendingRejectionEmails = a.IsSendingRejectionEmails,
                IsUsingDirectLinkFlow = a.IsUsingDirectLinkFlow,
                IsSendingAdditionalQuestionsEmail = a.IsSendingAdditionalQuestionsEmail
            };
        }
    }

    public interface IProviderInfoService
    {
        ProviderInfoModel GetSingle(string providerName);
    }

    public class ProviderInfoModel
    {
        public string ProviderName { get; set; }
        public bool IsSelf { get; set; }
        public bool IsSendingRejectionEmails { get; set; }
        public bool IsUsingDirectLinkFlow { get; set; }
        public bool IsSendingAdditionalQuestionsEmail { get; set; }
        public bool IsMortgageLoanProvider { get; set; }
    }
}