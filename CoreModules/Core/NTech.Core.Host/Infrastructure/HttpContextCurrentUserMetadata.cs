using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.Host.Infrastructure
{
    public class HttpContextCurrentUserMetadata : NTechCurrentUserMetadataImpl
    {
        public HttpContextCurrentUserMetadata(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor?.HttpContext?.User?.Identity)
        {

        }
    }
}
