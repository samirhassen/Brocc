namespace nPreCredit.Code.AffiliateReporting
{
    public abstract class AffiliateReportingEventModelBase
    {
        public string ApplicationNr { get; set; }
        public string ProviderName { get; set; }
        public string ProviderApplicationId { get; set; }
    }
}