namespace NTech.Services.Infrastructure
{
    public class NTechAuthorizeCreditLowAttribute : NTechAuthorizeAttributeBase
    {
        private static readonly string[] roles = new string[] { "ConsumerCreditFi.Low", "ConsumerCredit.Low" };
        private static readonly string[] users = new string[] { };

        protected override string[] GetRoles()
        {
            return roles;
        }

        protected override string[] GetUsers()
        {
            return users;
        }
    }
}