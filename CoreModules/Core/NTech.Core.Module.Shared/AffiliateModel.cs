using Newtonsoft.Json;

namespace NTech.Core.Module
{
    public class AffiliateModel : NTech.Banking.PluginApis.CreateApplication.IApplicationAffiliateModel
    {
        public string ProviderName { get; set; }
        public string DisplayToEnduserName { get; set; }
        public string StreetAddress { get; set; }
        public string EnduserContactPhone { get; set; }
        public string EnduserContactEmail { get; set; }
        public string WebsiteAddress { get; set; }
        public bool IsSelf { get; set; }
        public bool IsSendingRejectionEmails { get; set; }
        public bool? HasBrandedAdditionalQuestions { get; set; }
        public string BrandingTag { get; set; }
        public string FallbackCampaignCode { get; set; }
        public bool IsUsingDirectLinkFlow { get; set; }
        public bool IsSendingAdditionalQuestionsEmail { get; set; }
        public bool IsMortgageLoanProvider { get; set; }
        public string MortgageLoanProviderIntegrationName { get; set; }
        public string ProviderToken { get; set; }
        public bool? UseLeads { get; set; }

        public T GetCustomPropertyAnonymous<T>(T templateObject)
        {
            return JsonConvert.DeserializeAnonymousType(JsonConvert.SerializeObject(this), templateObject);
        }
    }
}
