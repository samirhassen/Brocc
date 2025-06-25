using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IdentityServer3.Core.Configuration;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using IdentityServer3.Core.Services.Default;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security.WsFederation;
using Owin;

namespace nUser.Code
{
    public class IdentityServerSetup
    {
        public static IEnumerable<Scope> GetScopes()
        {
            var scopes = new List<Scope>
            {
                new Scope
                {
                    Enabled = true,
                    Name = "roles",
                    Type = ScopeType.Identity,
                    Claims = new List<ScopeClaim>
                    {
                        new ScopeClaim("role"),
                        new ScopeClaim("ntech.permission")
                    },
                    IncludeAllClaimsForUser = true
                },
                new Scope
                {
                    Enabled = true,
                    DisplayName = "Näktergal Financial Systems",
                    Required = true,
                    Name = "nTech1",
                    Description = NEnv.ConsentText,
                    Type = ScopeType.Resource,
                    Claims = new List<ScopeClaim>
                    {
                        new ScopeClaim("role"),
                        new ScopeClaim("ntech.permission")
                    },
                    IncludeAllClaimsForUser = true
                },
                new Scope
                {
                    Enabled = true,
                    DisplayName = "Näktergal Financial Systems - PreCredit",
                    Name = "nTechPreCredit1",
                    Type = ScopeType.Resource,
                    IncludeAllClaimsForUser = true
                },
                StandardScopes.OpenId
            };

            return scopes;
        }

        public static IEnumerable<Client> GetClients()
        {
            return new[]
            {
                new Client
                {
                    ClientName = "nBackOffice User Login",
                    ClientId = "nBackOfficeUserLogin",
                    Flow = Flows.Implicit,
                    RequireConsent = true,
                    RedirectUris = AllowRedirectToEntityServiceRegistry().ToList(),
                    PostLogoutRedirectUris = new List<string>
                    {
                        NEnv.ServiceRegistry.External.ServiceUrl("nBackOffice", "Secure/LoggedOut").ToString()
                    },
                    AllowedScopes = new List<string>
                    {
                        "openid",
                        "nTech1",
                        "nTechPreCredit1"
                    },
                    EnableLocalLogin = NEnv.IsUsernamePasswordLoginEnabled,
                    AccessTokenLifetime = NEnv.AccessTokenLifeTimeSeconds
                },
                new Client
                {
                    ClientName = "Embedded BackOffice User Login",
                    ClientId = "nBackOfficeEmbeddedUserLogin",
                    Flow = Flows.Implicit,
                    RequireConsent = false,
                    RedirectUris = AllowRedirectToEntityServiceRegistry(isEmbeddedBackOffice: true).ToList(),
                    PostLogoutRedirectUris = new List<string>
                    {
                        NEnv.ServiceRegistry.External.ServiceUrl("nBackOffice", "Secure/LoggedOut").ToString()
                    },
                    AllowedScopes = new List<string>
                    {
                        "openid",
                        "nTech1",
                        "nTechPreCredit1"
                    },
                    EnableLocalLogin = NEnv.IsUsernamePasswordLoginEnabled,
                    AccessTokenLifetime = NEnv.AccessTokenLifeTimeSeconds,
                    AllowedCorsOrigins = GetCorsOrigins("nBackOffice")
                },
                new Client
                {
                    ClientName = "nTechSystemUser",
                    ClientId = "nTechSystemUser",
                    Flow = Flows.ResourceOwner,
                    ClientSecrets = new List<Secret>
                    {
                        //NOTE: It's a violation of oauth2 to require a client secret so identityserver is broken here
                        //      Public clients cannot maintain a secret anyway so this is useless, the username/password is the only protection here
                        //      We set this particular secret just to make it slightly less retarded for people trying to call us.
                        new Secret("nTechSystemUser".Sha256())
                    },
                    AllowedScopes = new List<string>
                    {
                        "nTech1",
                        "nTechPreCredit1",
                    },
                    EnableLocalLogin = true,
                    AccessTokenLifetime = 3600 //1h
                },
            };
        }

        private static IEnumerable<string> AllowRedirectToEntityServiceRegistry(bool isEmbeddedBackOffice = false)
        {
            foreach (var s in NEnv.ServiceRegistry.External)
            {
                var uri = new Uri(s.Value).ToString();
                yield return uri;
                yield return
                    uri.Substring(0, uri.Length - 1); //Trailing slash that randomly gets appened by the auth framework
            }

            if (isEmbeddedBackOffice)
            {
                var uri = NEnv.ServiceRegistry.External.ServiceUrl("nBackOffice", "s/login-complete").ToString();
                yield return uri;
                yield return uri.Substring(0, uri.Length - 1);
            }
        }

        private static List<string> GetCorsOrigins(params string[] serviceNames)
        {
            var serviceRegistry = NEnv.ServiceRegistry;
            return serviceNames
                .Select(x => serviceRegistry.External.ServiceRootUri(x).GetLeftPart(UriPartial.Authority))
                .ToList();
        }

        private static string CreateTemporaryEmbeddedViewsFolder()
        {
            var p = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(p);

            File.WriteAllText(Path.Combine(p, "_authorizeresponse.html"), ReadEmbeddedFile("_authorizeresponse.html"));
            File.WriteAllText(Path.Combine(p, "_layout.html"), ReadEmbeddedFile("_layout.html"));
            File.WriteAllText(Path.Combine(p, "_consent.html"), ReadEmbeddedFile("_consent.html"));

            return p;

            string ReadEmbeddedFile(string name)
            {
                using (var s = Assembly.GetExecutingAssembly()
                           .GetManifestResourceStream($"nUser.EmbeddedFileSystemIdentityServerViews.{name}"))
                using (var r = new StreamReader(s, Encoding.UTF8))
                {
                    return r.ReadToEnd();
                }
            }
        }

        private static string GetPublicOrigin()
        {
            if (NEnv.UseExternalUrlAsPublicOrigin)
                return NEnv.ServiceRegistry.External.ServiceRootUri("nUser").ToString();
            return null;
        }

        public static void RegisterStartup(IAppBuilder app)
        {
            var factory = new IdentityServerServiceFactory()
                .UseInMemoryClients(GetClients())
                .UseInMemoryScopes(GetScopes());

            var userService = new NTechUserService();
            factory.UserService = new Registration<IUserService>(resolver => userService);

            factory.ConsentService = new Registration<IConsentService>(resolver =>
                new NTechConsentService(resolver.Resolve<IConsentStore>()));

            var viewOptions = new DefaultViewServiceOptions
            {
                CacheViews = true,
                CustomViewDirectory = CreateTemporaryEmbeddedViewsFolder()
            };

            factory.ConfigureDefaultViewService(viewOptions);

            var options = new IdentityServerOptions
            {
                SiteName = "Brocc Identity Provider",
                SigningCertificate = NEnv.IdentityServerCertificate,
                Factory = factory,
                RequireSsl = NEnv.IsProduction,
                EnableWelcomePage = !NEnv.IsProduction,
                AuthenticationOptions = new AuthenticationOptions
                {
                    IdentityProviders = ConfigureIdentityProviders,
                    EnablePostSignOutAutoRedirect = true,
                    EnableLocalLogin = true
                },
                PublicOrigin = GetPublicOrigin(),
                Endpoints = new EndpointOptions
                {
                    EnableCspReportEndpoint = false
                }
            };
            if (NEnv.IsVerboseLoggingEnabled)
            {
                options.LoggingOptions = new LoggingOptions
                {
                    EnableKatanaLogging = true,
                    EnableWebApiDiagnostics = true,
                    WebApiDiagnosticsIsVerbose = true,
                    EnableHttpLogging = true
                };
            }

            app.Map("/id", idsrvApp => { idsrvApp.UseIdentityServer(options); });
        }

        private static void ConfigureIdentityProviders(IAppBuilder app, string signInAsType)
        {
            var googleLogin = NEnv.GoogleLogin;
            if (googleLogin != null)
            {
                //NOTE: Google+ API needs to be enabled on the account that hosts the client or this will not work (access denied)
                //Setup accounts here: https://console.developers.google.com
                app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions
                {
                    AuthenticationType = "Google",
                    Caption = "Sign-in with Google",
                    SignInAsAuthenticationType = signInAsType,
                    ClientId = googleLogin.ClientId,
                    ClientSecret = googleLogin.ClientSecret
                });
            }

            var windowsLogin = NEnv.WindowsLogin;
            if (windowsLogin != null)
            {
                var windowsAuth = new WsFederationAuthenticationOptions
                {
                    AuthenticationType = "Windows",
                    Caption = "Windows",
                    SignInAsAuthenticationType = signInAsType,
                    MetadataAddress = windowsLogin.Url,
                    Wtrealm = "urn:idsrv3",
                    Notifications = new WsFederationAuthenticationNotifications
                    {
                        RedirectToIdentityProvider = notification =>
                        {
                            if (notification.ProtocolMessage.IsSignOutMessage)
                            {
                                //Can't _really_ logout of windows auth, just set the notification to handled.
                                notification.HandleResponse();
                            }

                            return Task.FromResult(0);
                        }
                    }
                };
                app.UseWsFederationAuthentication(windowsAuth);
            }

            var azureAdLogin = NEnv.AzureAdLogin;
            if (azureAdLogin != null)
            {
                //https://docs.microsoft.com/en-us/azure/active-directory/develop/tutorial-v2-asp-webapp
                //Need to enable ID Tokens and tokens under Implicit grant and hybrid flows in the azure portal
                app.UseOpenIdConnectAuthentication(
                    new OpenIdConnectAuthenticationOptions
                    {
                        // Sets the client ID, authority, and redirect URI as obtained from Web.config
                        ClientId = azureAdLogin.ApplicationClientId,
                        Caption = "Sign-in with Microsoft",
                        Authority = azureAdLogin.Authority,
                        RedirectUri = NEnv.ServiceRegistry.External.ServiceUrl("nUser", "id/azure-ad").ToString(),
                        // PostLogoutRedirectUri is the page that users will be redirected to after sign-out. In this case, it's using the home page
                        PostLogoutRedirectUri = NEnv.ServiceRegistry.External
                            .ServiceUrl("nBackOffice", "Secure/LoggedOut").ToString(),
                        Scope = "openid profile email", //"  https://graph.microsoft.com/.default",
                        ResponseType = "id_token", //" token",
                        // ValidateIssuer set to false to allow personal and work accounts from any organization to sign in to your application
                        // To only allow users from a single organization, set ValidateIssuer to true and the 'tenant' setting in Web.config to the tenant name
                        // To allow users from only a list of specific organizations, set ValidateIssuer to true and use the ValidIssuers parameter
                        TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true
                        },

                        //NOTE: These are read by the token validator callback in the constructor so the order of these matter. Pure insanity: https://github.com/IdentityServer/IdentityServer3/issues/1176#issuecomment-94942148
                        AuthenticationType = "AzureAd",
                        SignInAsAuthenticationType = signInAsType,
                    });
            }
        }
    }
}