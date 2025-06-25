using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Newtonsoft.Json;

namespace NTech.Core.Module.Shared.Infrastructure
{
    public interface INTechCurrentUserMetadata
    {
        bool ContextHasUser { get; }
        int UserId { get; }
        string InformationMetadata { get; }
        bool IsSystemUser { get; }
        string AuthenticationLevel { get; }
        string AccessToken { get; }
        bool IsProvider { get; }
        string ProviderName { get; }
    }

    public class NTechCurrentUserMetadataImpl : INTechCurrentUserMetadata
    {
        private readonly Lazy<List<Claim>> claims;

        public NTechCurrentUserMetadataImpl(IIdentity identity)
            : this((identity as ClaimsIdentity)?.Claims)

        {
        }

        public NTechCurrentUserMetadataImpl(IEnumerable<Claim> claims)
        {
            this.claims = new Lazy<List<Claim>>(() => claims?.ToList());
        }

        private T WithClaims<T>(Func<List<Claim>, T> f)
        {
            return claims.Value == null ? default : f(claims.Value);
        }

        private string GetClaim(string claimName, bool isRequired)
        {
            var v = WithClaims(x => x.FirstOrDefault(y => y.Type == claimName))?.Value;
            if (string.IsNullOrWhiteSpace(v) && isRequired)
                throw new Exception($"Missing claim {claimName}");
            return v?.Trim();
        }

        public bool ContextHasUser
        {
            get { return WithClaims(x => new object()) == null; }
        }

        public int UserId
        {
            get
            {
                var userIdRaw = GetClaim("ntech.userid", true);
                return int.Parse(userIdRaw);
            }
        }

        public int? OptionalUserId
        {
            get
            {
                var userIdRaw = GetClaim("ntech.userid", false);
                return userIdRaw == null ? new int?() : int.Parse(userIdRaw);
            }
        }

        public string InformationMetadata =>
            JsonConvert.SerializeObject(new
            {
                providerUserId = UserId,
                providerAuthenticationLevel = AuthenticationLevel,
                isSigned = false
            });

        public bool IsSystemUser => (GetClaim("ntech.issystemuser", false) ?? "false").ToLowerInvariant() == "true";

        public string AuthenticationLevel => GetClaim("ntech.authenticationlevel", true);

        public string AccessToken => GetClaim("access_token", false);

        public bool IsProvider => (GetClaim("ntech.isprovider", false) ?? "false").ToLowerInvariant() == "true";

        public string ProviderName => GetClaim("ntech.providername", false);
    }
}