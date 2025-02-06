namespace nCustomerPages.Controllers
{
    public class ConsumerCreditStandardProviderBasicAuthenticationAttribute : AbstractProviderBasicAuthenticationAttribute
    {
        public ConsumerCreditStandardProviderBasicAuthenticationAttribute()
        {
        }

        public override string ProductName => "ConsumerCreditStandard";
        public override string ApiKeyScopeName => "ExternalCreditApplicationApi";
    }
}