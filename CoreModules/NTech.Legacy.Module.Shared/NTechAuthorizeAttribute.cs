namespace NTech.Services.Infrastructure
{
    public class NTechAuthorizeAttribute : NTechAuthorizeAttributeBase
    {
        private readonly string[] _rolesSplit = new string[0];
        private readonly string[] _usersSplit = new string[0];

        protected override string[] GetRoles()
        {
            return _rolesSplit;
        }

        protected override string[] GetUsers()
        {
            return _usersSplit;
        }
    }
}