using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace nCustomerPages.Code
{
    internal static class UserHelper
    {
        public const string CorrelationIdClaimName = "ntech.claims.correlationid";
        public const string BearerTokenClaimName = "ntech.claims.internalapitoken";

        public static ClaimsPrincipal CreateUser(string username, string authenticationType, string correlationId = null, string bearertoken = null, string providerName = null, string role = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new Exception("Missing username");

            var claims = new List<Claim>();
            claims.Add(new Claim("ntech.claims.name", username));

            if (bearertoken != null)
                claims.Add(new Claim(BearerTokenClaimName, bearertoken));

            if (providerName != null)
                claims.Add(new Claim("ntech.claims.providername", providerName));

            if (correlationId != null)
                claims.Add(new Claim(CorrelationIdClaimName, correlationId));

            if (role != null)
                claims.Add(new Claim("ntech.claims.role", role));

            var identity = new ClaimsIdentity(claims, authenticationType, "ntech.claims.name", "ntech.claims.role");

            return new ClaimsPrincipal(identity);
        }
    }
}