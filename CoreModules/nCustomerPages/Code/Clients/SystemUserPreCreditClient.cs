namespace nCustomerPages.Code
{
    public class SystemUserPreCreditClient : AbstractSystemUserServiceClient
    {
        protected override string ServiceName => "nPreCredit";

        public bool HasOrHasEverHadAnApplication(int customerId)
        {
            return Begin().PostJson("Api/UnsecuredLoanStandard/Has-Or-Has-Had-Applications", new
            {
                customerId
            }).ParseJsonAsAnonymousType(new { HasAnyApplications = (bool?)null, HasActiveApplications = (bool?)null })?.HasAnyApplications ?? false;
        }
    }
}