namespace NTech.Services.Infrastructure
{
    public class NTechAuthorizeCreditMiddleAttribute : NTechAuthorizeAttributeBase
    {
        private static readonly string[] roles = new string[] { "ConsumerCreditFi.Middle", "ConsumerCredit.Middle" };
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