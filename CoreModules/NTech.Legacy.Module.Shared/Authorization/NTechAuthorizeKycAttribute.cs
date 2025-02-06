namespace NTech.Services.Infrastructure
{
    public class NTechAuthorizeKycAttribute : NTechAuthorizeAttributeBase
    {
        //TODO: Make this use ntech.group instead?
        private static readonly string[] roles = new string[] {
            "ConsumerCreditFi.Middle", "ConsumerCredit.Middle",
            "MortgageLoan.Middle",
            "ConsumerSavingsFi.Middle", "ConsumerSavings.Middle"};
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