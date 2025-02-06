using NTech.Core.Module;
using System;
using System.Text.RegularExpressions;

namespace nCredit.Code.EInvoiceFi
{
    public class EInvoiceFiSettingsFile
    {
        private readonly NTechSimpleSettingsCore settings;

        public EInvoiceFiSettingsFile(NTechSimpleSettingsCore settings)
        {
            this.settings = settings;
        }

        public string Protocol
        {
            get
            {
                return (this.settings.Opt("protocol") ?? "sftp").ToLowerInvariant();
            }
        }

        public string Host
        {
            get
            {
                return this.settings.Req("host");
            }
        }

        public string Username
        {
            get
            {
                return this.settings.Req("username");
            }
        }

        public string Password
        {
            get
            {
                return this.settings.Req("password");
            }
        }

        public int Port
        {
            get
            {
                return int.Parse(this.settings.Opt("port") ?? "22");
            }
        }

        public string RemoteDirectory
        {
            get
            {
                return this.settings.Req("remotedirectory");
            }
        }

        public Regex RemoteFilenamePattern
        {
            get
            {
                return new Regex(this.settings.Req("remotefilenamepattern"));
            }
        }

        public int SkipRecentlyWrittenMinutes
        {
            get
            {
                return int.Parse(this.settings.Opt("skiprecentlywrittenminutes") ?? "10");
            }
        }

        public bool AllowDuplicateMessageIds
        {
            get
            {
                return (this.settings.Opt("allowduplicatemessageids") ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}