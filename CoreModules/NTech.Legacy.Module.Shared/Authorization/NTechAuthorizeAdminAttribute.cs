namespace NTech.Services.Infrastructure
{
    public class NTechAuthorizeAdminAttribute : NTechAuthorizeAttributeBase
    {
        private static readonly string[] roles = new string[] { "Admin" };
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