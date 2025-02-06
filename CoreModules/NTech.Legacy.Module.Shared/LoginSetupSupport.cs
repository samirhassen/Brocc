using IdentityServer3.AccessTokenValidation;
using Microsoft.Owin;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Newtonsoft.Json;
using Owin;
using System;
using System.Threading.Tasks;
using System.Web;

namespace NTech.Services.Infrastructure
{
    public static class LoginSetupSupport
    {
        public enum LoginMode
        {
            OnlyUsers,
            OnlyApi,
            BothUsersAndApi
        }

        /// <summary>
        /// IIS will destroy the body god knows what reason. This enabled catching that in the final stages and putting it back.
        /// </summary>
        public const string NTech401JsonItemName = "ntech_401_json_body";

        public static void SetupLogin(IAppBuilder app, string serviceName, LoginMode loginMode, bool isProduction, NTechServiceRegistry serviceRegistry, ClientConfiguration cfg)
        {
            var allowUserLogin = loginMode == LoginMode.BothUsersAndApi || loginMode == LoginMode.OnlyUsers;
            var allowApiLogin = loginMode == LoginMode.BothUsersAndApi || loginMode == LoginMode.OnlyApi;

            Func<IOwinRequest, bool> isApiRequest = (request) =>
            {
                if (request == null)
                    throw new ArgumentNullException("request");
                var isApi = new bool?();

                if (!isApi.HasValue && request.Headers != null)
                {
                    if (request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        isApi = true;
                    else if (request.Headers["X-Ntech-Api-Call"] == "1")
                        isApi = true;
                    else if (request.Headers["X-Ntech-Api-Call"] == "0")
                        isApi = false; //To allow turning this off from outside if it misfires
                }
                if (!isApi.HasValue && request.Path.ToString().StartsWith("/api", StringComparison.OrdinalIgnoreCase) && !request.Path.ToString().Equals("/api/docs", StringComparison.OrdinalIgnoreCase))
                {
                    isApi = true;
                }

                return isApi ?? false;
            };

            //When requests are forwarded using the ui gateway we want the user to be redirected to login even though
            //they are GETing an api endpoint. Things like downloading from the document archive need this.
            bool IsUserInvokedApiRequest(IOwinRequest request) => (request.Query != null && (request.Method ?? "").EqualsIgnoreCase("get") && request.Query["apiureq"] == "1");

            Func<IOwinRequest, bool> isRedirectToAuthorizeRequested = (request) =>
            {
                //This allows consumers who expose report or file like get api methods directly to endusers
                //to override the api behaviour of just returning 401 and instead redirect to login
                return request.Method?.ToLowerInvariant() == "get"
                    && request.Query != null
                    && request.Query["RedirectToAuthorize"]?.ToLowerInvariant() == "true";
            };

            const string NtechAuthTypeName = "NtechCookies";
            string nTechCookieName = string.Format("NTechV2.{0}.{1}", cfg.ClientName, isProduction ? "P" : "T");

            if (allowUserLogin)
            {
                //http://stackoverflow.com/questions/25663773/the-owin-authentication-pipeline-and-how-to-use-katana-middleware-correctly
                app.SetDefaultSignInAsAuthenticationType(NtechAuthTypeName);
            }

            if (allowApiLogin)
            {
                app.UseIdentityServerBearerTokenAuthentication(new IdentityServerBearerTokenAuthenticationOptions
                {
                    ClientId = "nTechSystemUser",
                    Authority = serviceRegistry.Internal.ServiceUrl("nUser", "id").ToString(),
                    NameClaimType = "ntech.username",
                    RoleClaimType = "ntech.role",
                    RequiredScopes = new[] { "nTech1" },
                    DelayLoadMetadata = true
                });
            }

            if (allowUserLogin)
            {
                app.UseIdentityServerBearerTokenAuthentication(new IdentityServerBearerTokenAuthenticationOptions
                {
                    ClientId = "nBackOfficeEmbeddedUserLogin",
                    Authority = serviceRegistry.Internal.ServiceUrl("nUser", "id").ToString(),
                    NameClaimType = "ntech.username",
                    RoleClaimType = "ntech.role",
                    RequiredScopes = new[] { "nTech1" },
                    DelayLoadMetadata = true,
                    AuthenticationType = "nBackOfficeEmbeddedUserLogin"
                });

                app.UseCookieAuthentication(new CookieAuthenticationOptions
                {
                    AuthenticationType = NtechAuthTypeName,
                    CookieManager = new SystemWebCookieManager(SameSiteMode.Lax), //Matches the samesite setting of the dotnet session cookie
                    CookieName = nTechCookieName
                });

                app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
                {
                    Authority = serviceRegistry.External.ServiceUrl("nUser", "id").ToString(),
                    ClientId = "nBackOfficeUserLogin",
                    Scope = "openid nTech1",
                    ResponseType = "id_token token",
                    RedirectUri = serviceRegistry.External.ServiceRootUri(serviceName).ToString(),
                    SignInAsAuthenticationType = NtechAuthTypeName,
                    UseTokenLifetime = true,
                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        SecurityTokenValidated = async n =>
                        {
                            var nid = new System.Security.Claims.ClaimsIdentity(
                                    n.AuthenticationTicket.Identity.AuthenticationType,
                                    "ntech.username",
                                    "ntech.role");

                            nid.AddClaims(n.AuthenticationTicket.Identity.Claims);

                            // keep the id_token for logout
                            if (!nid.HasClaim(x => x.Type == "id_token"))
                                nid.AddClaim(new System.Security.Claims.Claim("id_token", n.ProtocolMessage.IdToken));

                            // keep the access token for api login
                            if (!nid.HasClaim(x => x.Type == "access_token"))
                                nid.AddClaim(new System.Security.Claims.Claim("access_token", n.ProtocolMessage.AccessToken));

                            // keep track of access token expiration
                            if (!nid.HasClaim(x => x.Type == "expires_at"))
                                nid.AddClaim(new System.Security.Claims.Claim("expires_at", DateTime.Now.AddSeconds(int.Parse(n.ProtocolMessage.ExpiresIn)).ToUniversalTime().ToString("R")));

                            var newTicket = new AuthenticationTicket(
                                nid,
                                n.AuthenticationTicket.Properties);

                            newTicket.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(int.Parse(n.ProtocolMessage.ExpiresIn));

                            n.AuthenticationTicket = newTicket;

                            await Task.FromResult(0);
                        },
                        RedirectToIdentityProvider = n =>
                        {
                            if (n.Request != null && isApiRequest(n.Request) && !isRedirectToAuthorizeRequested(n.Request) && !IsUserInvokedApiRequest(n.Request))
                            {
                                var jsonBody = JsonConvert.SerializeObject(new
                                {
                                    errorMessage = "Unauthorized",
                                    errorCode = "unauthorized"
                                });
                                HttpContext.Current.Items[NTech401JsonItemName] = jsonBody;
                                n.Response.StatusCode = 401;
                                n.Response.ContentType = "application/json";
                                n.Response.Write(jsonBody);
                                n.HandleResponse();
                                return Task.FromResult(0);
                            }
                            else if (n.ProtocolMessage.RequestType == Microsoft.IdentityModel.Protocols.OpenIdConnectRequestType.LogoutRequest)
                            {
                                var idTokenHint = n.OwinContext.Authentication.User.FindFirst("id_token");

                                if (idTokenHint != null)
                                {
                                    n.ProtocolMessage.IdTokenHint = idTokenHint.Value;
                                }
                            }

                            return Task.FromResult(0);
                        },
                        AuthenticationFailed = n =>
                        {
                            return Task.FromResult(0);
                        },
                        AuthorizationCodeReceived = n =>
                        {
                            return Task.FromResult(0);
                        },
                        MessageReceived = n =>
                        {
                            return Task.FromResult(0);
                        },
                        SecurityTokenReceived = n =>
                        {
                            return Task.FromResult(0);
                        }
                    }
                });
            }
        }
    }

    //From: http://katanaproject.codeplex.com/wikipage?title=System.Web%20response%20cookie%20integration%20issues
    internal class SystemWebCookieManager : ICookieManager
    {
        private readonly SameSiteMode? sameSiteMode;

        public SystemWebCookieManager()
        {
            this.sameSiteMode = null;
        }

        public SystemWebCookieManager(SameSiteMode sameSiteMode)
        {
            this.sameSiteMode = sameSiteMode;
        }

        public string GetRequestCookie(IOwinContext context, string key)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var webContext = context.Get<HttpContextBase>(typeof(HttpContextBase).FullName);
            var cookie = webContext.Request.Cookies[key];
            return cookie == null ? null : cookie.Value;
        }

        public void AppendResponseCookie(IOwinContext context, string key, string value, CookieOptions options)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            var webContext = context.Get<HttpContextBase>(typeof(HttpContextBase).FullName);

            bool domainHasValue = !string.IsNullOrEmpty(options.Domain);
            bool pathHasValue = !string.IsNullOrEmpty(options.Path);
            bool expiresHasValue = options.Expires.HasValue;

            var cookie = new HttpCookie(key, value);
            if (domainHasValue)
            {
                cookie.Domain = options.Domain;
            }
            if (pathHasValue)
            {
                cookie.Path = options.Path;
            }
            if (expiresHasValue)
            {
                cookie.Expires = options.Expires.Value;
            }
            if (options.Secure)
            {
                cookie.Secure = true;
            }
            if (options.HttpOnly)
            {
                cookie.HttpOnly = true;
            }
            if (sameSiteMode.HasValue)
            {
                cookie.SameSite = sameSiteMode.Value;
            }

            webContext.Response.AppendCookie(cookie);
        }

        public void DeleteCookie(IOwinContext context, string key, CookieOptions options)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            AppendResponseCookie(
                context,
                key,
                string.Empty,
                new CookieOptions
                {
                    Path = options.Path,
                    Domain = options.Domain,
                    Expires = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                });
        }
    }
}
