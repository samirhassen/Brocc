namespace NTech.Services.Infrastructure
{
    public class NTechAuthorizeCreditEconomyAttribute : NTechAuthorizeAttributeBase
    {
        private static readonly string[] roles = new string[] { "ConsumerCreditFi.Economy", "ConsumerCredit.Economy" };
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