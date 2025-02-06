using System;
using System.Text;

namespace NTech.Legacy.Module.Shared.Infrastructure
{
    public class AuthorizationHeader
    {
        public enum HeaderTypeCode
        {
            Basic,
            Bearer
        }
        public HeaderTypeCode HeaderType { get; set; }
        public string BasicAuthUserName { get; set; }
        public string BasicAuthPassword { get; set; }
        public string BearerToken { get; set; }
        public string RawHeaderValue { get; set; }

        public static bool TryParseHeader(string rawHeaderValue, out AuthorizationHeader header)
        {
            header = null;
            var normalizedHeader = rawHeaderValue?.NormalizeNullOrWhitespace();
            if (normalizedHeader == null)
                return false;

            if (normalizedHeader.IndexOf("basic", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                try
                {
                    var cred = ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(normalizedHeader.Substring(6))).Split(':');
                    var userName = cred[0]?.NormalizeNullOrWhitespace();
                    var password = cred[1]?.NormalizeNullOrWhitespace();
                    if (userName != null && password != null)
                    {
                        header = new AuthorizationHeader
                        {
                            BasicAuthUserName = userName,
                            BasicAuthPassword = password,
                            HeaderType = HeaderTypeCode.Basic,
                            RawHeaderValue = normalizedHeader
                        };
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }
            else if (normalizedHeader.IndexOf("bearer", StringComparison.InvariantCultureIgnoreCase) >= 0 && normalizedHeader.Length > 7)
            {
                try
                {
                    var bearerToken = normalizedHeader.Substring(7).NormalizeNullOrWhitespace();
                    if (bearerToken != null)
                    {
                        header = new AuthorizationHeader
                        {
                            BearerToken = bearerToken,
                            HeaderType = HeaderTypeCode.Bearer,
                            RawHeaderValue = normalizedHeader
                        };
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }
}
