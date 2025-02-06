using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

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

        public NTechCurrentUserMetadataImpl(System.Security.Principal.IIdentity identity)
            : this((identity as System.Security.Claims.ClaimsIdentity)?.Claims)

        {

        }

        public NTechCurrentUserMetadataImpl(IEnumerable<Claim> claims)
        {
            this.claims = new Lazy<List<Claim>>(() =>
            {
                if (claims == null)
                {
                    return null;
                }
                return claims.ToList();
            });
        }

        private T WithClaims<T>(Func<List<Claim>, T> f)
        {
            if (claims.Value == null)
                return default(T);
            else
                return f(claims.Value);
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
            get
            {
                return WithClaims(x => new object()) == null;
            }
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

        public string InformationMetadata
        {
            get
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    providerUserId = UserId,
                    providerAuthenticationLevel = AuthenticationLevel,
                    isSigned = false
                });
            }
        }

        public bool IsSystemUser
        {
            get
            {

                return (GetClaim("ntech.issystemuser", false) ?? "false").ToLowerInvariant() == "true";
            }
        }

        public string AuthenticationLevel => GetClaim("ntech.authenticationlevel", true);

        public string AccessToken => GetClaim("access_token", false);

        public bool IsProvider => (GetClaim("ntech.isprovider", false) ?? "false").ToLowerInvariant() == "true";

        public string ProviderName => GetClaim("ntech.providername", false);
    }
}