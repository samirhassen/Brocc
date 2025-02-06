namespace NTech.Services.Infrastructure
{
    public class NTechAuthorizeHighAttribute : NTechAuthorizeAttributeBase
    {
        //TODO: Make this use ntech.group instead?
        private static readonly string[] roles = new string[] {
            "ConsumerCreditFi.High", "ConsumerCredit.High",
            "MortgageLoan.High",
            "ConsumerSavingsFi.High", "ConsumerSavings.High"
            };
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