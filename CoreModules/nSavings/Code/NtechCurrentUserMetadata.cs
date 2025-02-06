using System;

namespace nSavings.Code
{
    public interface INtechCurrentUserMetadata
    {
        /// <summary>
        /// All other calls will blow up if this is false
        /// </summary>
        bool ContextHasUser { get; }

        int UserId { get; }
        string InformationMetadata { get; }
        bool IsSystemUser { get; }
        string AuthenticationLevel { get; }
        string AccessToken { get; }
    }

    public class NtechCurrentUserMetadata : INtechCurrentUserMetadata
    {
        private readonly System.Security.Principal.IIdentity identity;

        public NtechCurrentUserMetadata(System.Security.Principal.IIdentity identity)
        {
            this.identity = identity;
        }

        private T WithClaims<T>(Func<System.Security.Claims.ClaimsIdentity, T> f)
        {
            var c = this.identity as System.Security.Claims.ClaimsIdentity;
            if (c == null)
                return default(T);
            else
                return f(c);
        }

        private string GetClaim(string claimName, bool isRequired)
        {
            var v = WithClaims(x => x.FindFirst(claimName)?.Value);
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
    }
}