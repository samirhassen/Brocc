using NTech.Core.Module.Shared.Infrastructure;
using System.IdentityModel.Tokens;

namespace NTech.Services.Infrastructure
{
    public static class NTechSelfRefreshingBearerTokenExtensions
    {
        public static INTechCurrentUserMetadata GetUserMetadata(this NTechSelfRefreshingBearerToken source)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(source.GetToken()) as JwtSecurityToken;
            return new NTechCurrentUserMetadataImpl(jwtToken?.Claims);
        }
    }
}
