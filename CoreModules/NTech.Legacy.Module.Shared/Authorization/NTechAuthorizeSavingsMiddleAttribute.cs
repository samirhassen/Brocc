namespace NTech.Services.Infrastructure
{
    public class NTechAuthorizeSavingsMiddleAttribute : NTechAuthorizeAttributeBase
    {
        private static readonly string[] roles = new string[] { "ConsumerSavingsFi.Middle", "ConsumerSavings.Middle" };
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