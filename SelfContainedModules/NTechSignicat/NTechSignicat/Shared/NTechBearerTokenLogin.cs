using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTech.Services.Infrastructure
{
    public static class NTechBearerTokenLogin
    {
        private static bool ValidateAudience(IEnumerable<string> audiences, Microsoft.IdentityModel.Tokens.SecurityToken securityToken, Microsoft.IdentityModel.Tokens.TokenValidationParameters validationParameters)
        {
            var t = securityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
            if (t == null)
                return false;
            if (!t.Claims.Any(x => x.Type == "client_id" && x.Value == "nTechSystemUser"))
                return false;
            if (!t.Claims.Any(x => x.Type == "scope" && x.Value == "nTech1"))
                return false;
            return true;
        }

        /// <summary>
        /// Besides this you also need app.UseAuthentication(); in the Configure section
        /// </summary>
        public static void ConfigureAuthenticationServices(IServiceCollection services, INEnv env)
        {
            services
                .AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme,
                    jwtOptions =>
                    {
                        jwtOptions.Authority = env.ServiceRegistry.Internal.ServiceUrl("nUser", "id").ToString();
                        jwtOptions.RequireHttpsMetadata = env.IsProduction;
                        jwtOptions.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                        {
                            AudienceValidator = ValidateAudience
                        };
                    },
                    referenceOptions =>
                    {
                        referenceOptions.ClientId = "nTechSystemUser";
                        referenceOptions.Authority = env.ServiceRegistry.Internal.ServiceUrl("nUser", "id").ToString();
                        referenceOptions.NameClaimType = "ntech.username";
                        referenceOptions.RoleClaimType = "ntech.role";
                    });

            services
                .AddMvcCore(options =>
                {
                    var policy = Microsoft.AspNetCore.Authorization.ScopePolicy.Create("nTech1");
                    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
                })
                .AddAuthorization();
        }
    }
}
