using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace nUser
{
    internal static class NEnv
    {
        public static string CurrentServiceName
        {
            get
            {
                return "nUser";
            }
        }
        public static DirectoryInfo LogFolder
        {
            get
            {
                var v = Opt("ntech.logfolder");
                if (v == null)
                    return null;
                return new DirectoryInfo(v);
            }
        }

        public static bool IsProfilerEnabled
        {
            get
            {
                return (Opt("ntech.isprofilerenabled") ?? "false").ToLowerInvariant() == "true";
            }
        }

        public static ClientConfiguration ClientCfg
        {
            get
            {
                return NTechCache.WithCache("nUser.ClientCfg", TimeSpan.FromMinutes(15), () => ClientConfiguration.CreateUsingNTechEnvironment());
            }
        }

        public static bool IsProduction
        {
            get
            {
                var s = Req("ntech.isproduction");
                return s.Trim().ToLower() == "true";
            }
        }

        public static bool IsMortgageLoansEnabled
        {
            get
            {
                return ClientCfg.IsFeatureEnabled("ntech.feature.mortgageloans");
            }
        }

        public static bool UseExternalUrlAsPublicOrigin
        {
            get
            {
                return (Opt("ntech.identityserver.publicorigin.useexternalurl") ?? "false").Trim().ToLowerInvariant() == "true";
            }
        }

        public static System.Security.Cryptography.X509Certificates.X509Certificate2 IdentityServerCertificate
        {
            get
            {
                var s = Req("ntech.identityserver.certificate");
                if (s.StartsWith("file:"))
                {

                    var c = System.IO.File.ReadAllBytes(s.Substring("file:".Length));
                    return new System.Security.Cryptography.X509Certificates.X509Certificate2(c);
                }
                else if (s.StartsWith("filewithpw:"))
                {
                    var fileAndPw = s.Substring("filewithpw:".Length);
                    var i = fileAndPw.IndexOf(';');
                    var file = fileAndPw.Substring(0, i);
                    var pw = fileAndPw.Substring(i + 1);
                    var c = System.IO.File.ReadAllBytes(file);

                    //If this fails with the error 'The system cannot find the file specified.' check that the app pool has Load User Profile = True

                    return new System.Security.Cryptography.X509Certificates.X509Certificate2(c, pw);
                }
                else if (s.StartsWith("thumbprint:"))
                {
                    var thumbprint = s.Substring("thumbprint:".Length);
                    using (var store = new System.Security.Cryptography.X509Certificates.X509Store(System.Security.Cryptography.X509Certificates.StoreName.My, System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine))
                    {
                        store.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadOnly);
                        var certs = store.Certificates.Find(System.Security.Cryptography.X509Certificates.X509FindType.FindByThumbprint, thumbprint, false);

                        if (certs.Count == 0)
                        {
                            throw new Exception("Could not find certificate pointed to by thumbprint. Make sure it's in the store: My, location: LocalMachine");
                        }

                        store.Close();

                        return certs[0];
                    }
                }
                else
                {
                    throw new Exception("The appsetting 'ntech.identityserver.certificate' is incorrectly used. Refer to the documentation.");
                }
            }
        }

        public class GoogleLoginSetup
        {
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
        }

        public static GoogleLoginSetup GoogleLogin
        {
            get
            {
                var s = Opt("ntech.identityserver.googlelogin.enabled");

                if ((s ?? "false").ToLowerInvariant() != "true")
                {
                    return null;
                }

                var clientId = Req("ntech.identityserver.googlelogin.clientid");
                var clientSecret = Req("ntech.identityserver.googlelogin.clientsecret");

                return new NEnv.GoogleLoginSetup
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                };
            }
        }

        public class WindowsLoginSetup
        {
            public string Url { get; set; } //Typically to nWindowsAuthIdentityServer
        }

        public static WindowsLoginSetup WindowsLogin
        {
            get
            {
                var s = Opt("ntech.identityserver.windowslogin.enabled");

                if ((s ?? "false").ToLowerInvariant() != "true")
                {
                    return null;
                }

                var url = Req("ntech.identityserver.windowslogin.url");

                return new WindowsLoginSetup { Url = url };
            }
        }

        public class AzureAdLoginSetup
        {
            /// <summary>
            /// The azure portal has something called OpenID Connect metadata document
            /// Let's say that is: https://login.microsoftonline.com/[guid]/v2.0/.well-known/openid-configuration
            /// The authority is https://login.microsoftonline.com/[guid]/v2.0
            /// </summary>
            public string Authority { get; set; }

            /// <summary>
            /// Application (client) ID from azure
            /// This will be some guid
            /// </summary>
            public string ApplicationClientId { get; set; }
        }

        /// <summary>
        /// You need to create an application under the azure and turn on token and id token
        /// You also need to add redirect https://[nuser]/id/azure-ad or http://localhost:2635/id/azure-ad for localhost
        /// </summary>
        public static AzureAdLoginSetup AzureAdLogin
        {
            get
            {
                var s = Opt("ntech.identityserver.azureadlogin.enabled");

                if ((s ?? "false").ToLowerInvariant() != "true")
                {
                    return null;
                }

                return new AzureAdLoginSetup
                {
                    Authority = Req("ntech.identityserver.azureadlogin.authority"),
                    ApplicationClientId = Req("ntech.identityserver.azureadlogin.applicationclientid")
                };
            }
        }



        public static bool IsUsernamePasswordLoginEnabled
        {
            get
            {
                //BEWARE: Dont allow this in production unless actual password management is enabled
                return !IsProduction && (Opt("ntech.identityserver.usernamepassword.enabled") ?? "false").ToLowerInvariant() == "true";
            }
        }

        public static string ConsentText
        {
            get
            {
                return Opt("ntech.identityserver.consenttext") ?? "I approve that my name is used to identify me in the Näktergal system.";
            }
        }

        public static NTechServiceRegistry ServiceRegistry
        {
            get
            {
                return NTechCache.WithCache(
                    "dcba56a0-5cf3-4f92-bf4f-6f675a3f642d",
                    TimeSpan.FromMinutes(5),
                    () => NTechEnvironment.Instance.ServiceRegistry);
            }
        }

        public static Tuple<string, string> AutomationUsernameAndPassword => Tuple.Create(Req("ntech.automationuser.username"), Req("ntech.automationuser.password"));

        public static bool IsVerboseLoggingEnabled
        {
            get
            {
                return (Opt("ntech.isverboseloggingenabled") ?? "false") == "true";
            }
        }

        public class EncryptionKeySet
        {
            public string CurrentKeyName { get; set; }

            public List<KeyItem> AllKeys { get; set; }

            public string GetKey(string name)
            {
                return AllKeys.Single(x => x.Name == name).Key;
            }

            public class KeyItem
            {
                public string Name { get; set; }
                public string Key { get; set; }
            }
        }

        public static EncryptionKeySet EncryptionKeys
        {
            get
            {
                return NTechCache.WithCache(
                    "ntech.nuser.encryptionkeys",
                    TimeSpan.FromMinutes(5),
                    () => JsonConvert.DeserializeObject<EncryptionKeySet>(File.ReadAllText(
                        E.StaticResourceFile("ntech.encryption.keysfile", "encryptionkeys.txt", true).FullName)));
            }
        }

        public static int AccessTokenLifeTimeSeconds
        {
            get
            {
                return int.Parse(Opt("ntech.user.accesstokenlifetimeseconds") ?? (3600 * 10).ToString()); //Default 10h
            }
        }

        private static NTechEnvironment E
        {
            get
            {
                return NTechEnvironment.Instance;
            }
        }

        private static string Opt(string n)
        {
            return E.Setting(n, false);
        }

        private static string Req(string n)
        {
            return E.Setting(n, true);
        }
    }
}