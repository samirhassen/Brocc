namespace NTech.Services.Infrastructure
{
    public class NTechAuthorizeCreditHighAttribute : NTechAuthorizeAttributeBase
    {
        private static readonly string[] roles = new string[] { "ConsumerCreditFi.High", "ConsumerCredit.High" };
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