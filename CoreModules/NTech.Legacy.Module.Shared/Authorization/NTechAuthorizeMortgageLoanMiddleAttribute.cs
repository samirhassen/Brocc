namespace NTech.Services.Infrastructure
{
    public class NTechAuthorizeMortgageLoanMiddleAttribute : NTechAuthorizeAttributeBase
    {
        private static readonly string[] roles = new string[] { "MortgageLoan.Middle" };
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