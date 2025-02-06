using NTech.Services.Infrastructure;
using System;
using System.IO;

namespace nWindowsAuthIdentityServer
{
    internal static class NEnv
    {
        public static bool IsProduction
        {
            get
            {
                var s = Req("ntech.isproduction");
                return s.Trim().ToLower() == "true";
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

        public static NTechServiceRegistry ServiceRegistry
        {
            get
            {
                return NTechCache.WithCache(
                    "a4b92591-347f-4934-9e95-ef16908e4c49",
                    TimeSpan.FromMinutes(5),
                    () => NTechEnvironment.Instance.ServiceRegistry);
            }
        }

        public static Tuple<string, string> AutomationUsernameAndPassword => Tuple.Create(Req("ntech.automationuser.username"), Req("ntech.automationuser.password"));

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

        public static bool IsDebugPageEnabled
        {
            get
            {
                return (Opt("ntech.identityserver.windows.debugpage.enable") ?? "false").ToLowerInvariant() == "true";
            }
        }

        public static bool IsVerboseLoggingEnabled
        {
            get
            {
                return (Opt("ntech.isverboseloggingenabled") ?? "false") == "true";
            }
        }

        public static string CurrentServiceName => "nWindowsAuthIdentityServer";

        private static string Opt(string n)
        {
            return NTechEnvironment.Instance.Setting(n, false);
        }

        private static string Req(string n)
        {
            return NTechEnvironment.Instance.Setting(n, true);
        }
    }
}