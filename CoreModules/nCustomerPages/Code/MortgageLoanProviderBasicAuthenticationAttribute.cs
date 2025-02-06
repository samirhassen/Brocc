namespace nCustomerPages.Controllers
{

    public class MortgageLoanProviderBasicAuthenticationAttribute : AbstractProviderBasicAuthenticationAttribute
    {
        public MortgageLoanProviderBasicAuthenticationAttribute()
        {
        }

        public override string ProductName => "MortgageLoan";
        public override string ApiKeyScopeName => "ExternalCreditApplicationApi";
    }
}