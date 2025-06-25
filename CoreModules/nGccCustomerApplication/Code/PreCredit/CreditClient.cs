
namespace nGccCustomerApplication.Code.PreCredit
{
    public class CreditClient: AbstractSystemUserServiceClient
    {
        protected override string ServiceName => "nCredit";

        public class LoginResult
        {
            public bool IsAllowedLogin { get; set; }
            public bool IsTokenExpired { get; set; }
            public CustomerResult Customer { get; set; }
            public class CustomerResult
            {
                public int CustomerId { get; set; }
                public string FirstName { get; set; }
            }
        }

        public LoginResult TryLoginToCustomerPagesWithToken(string token)
        {
            return Begin()
                    .PostJson("Api/CustomerPages/TryLoginWithToken", new
                    {
                        token = token
                    })
                    .ParseJsonAs<LoginResult>();
        }
    }
}