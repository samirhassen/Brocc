using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using NTech.Core.Module;
using System.Security.Claims;

namespace NTech.Core.Host.Startup
{
    public static class BearerTokenLogin
    {
        private static HashSet<string> AllowedBearerTokenClientIds = new HashSet<string>
        {
            "nTechSystemUser", "nBackOfficeUserLogin", "nBackOfficeEmbeddedUserLogin"
        };

        public static bool ValidateAudience(IEnumerable<string> audiences, Microsoft.IdentityModel.Tokens.SecurityToken securityToken, Microsoft.IdentityModel.Tokens.TokenValidationParameters validationParameters)
        {
            var t = securityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
            if (t == null)
                return false;
            if (!t.Claims.Any(x => x.Type == "client_id" && AllowedBearerTokenClientIds.Contains(x.Value)))
                return false;
            if (!t.Claims.Any(x => x.Type == "scope" && x.Value == "nTech1"))
                return false;
            return true;
        }

        public static IServiceCollection AddBearerTokenAuthorizationPolicy(this IServiceCollection source) =>
            source.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .RequireScope("nTech1")
                    .Build();
            });

        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddUserModuleBearerTokenAuthentication(this IServiceCollection source, NEnv env) => source
            .AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
            .AddIdentityServerAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme,
                jwtOptions =>
                {
                    jwtOptions.Authority = env.ServiceRegistry.Internal.ServiceUrl("nUser", "id").ToString();
                    jwtOptions.RequireHttpsMetadata = env.IsProduction;
                    jwtOptions.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        AudienceValidator = BearerTokenLogin.ValidateAudience
                    };

                    jwtOptions.SaveToken = true;
                    jwtOptions.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = (context) =>
                        {
                            //Add the access token to claims                            
                            var token = context.SecurityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
                            if (token == null)
                            {
                                return Task.FromResult(0);
                            }

                            var identity = context.Principal;

                            var subjectClaim = identity.Claims;

                            var newClaimsIdentity = new ClaimsIdentity(
                                context.Scheme.Name,
                                "ntech.username",
                                "ntech.role");
                            foreach (var claim in identity.Claims)
                            {
                                newClaimsIdentity.AddClaim(claim);
                            }

                            // keep the access token for api login
                            if (!newClaimsIdentity.HasClaim(x => x.Type == "access_token"))
                                newClaimsIdentity.AddClaim(new System.Security.Claims.Claim("access_token", token.RawData));

                            context.Principal = new ClaimsPrincipal(newClaimsIdentity);
                            context.Success();
                            return Task.FromResult(0);
                        }
                    };
                },
                referenceOptions =>
                {
                    referenceOptions.ClientId = "nTechSystemUser";
                    referenceOptions.Authority = env.ServiceRegistry.Internal.ServiceUrl("nUser", "id").ToString();
                    referenceOptions.NameClaimType = "ntech.username";
                    referenceOptions.RoleClaimType = "ntech.role";
                    referenceOptions.SaveToken = true;
                });
    }
}