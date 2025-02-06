using System.Collections.Generic;

namespace nCustomer.Code.Services.Aml.Cm1
{
    public class Cm1KycSettings
    {
        public bool Disabled { get; set; }
        public string Endpoint { get; set; }
        public string ClientCertificateFilePath { get; set; }
        public string ClientCertificateFilePassword { get; set; }
        public string ClientCertificateThumbprint { get; set; }
        public string XIdentifier { get; set; }
        public string DebugLogFolder { get; set; }
        public int QualityCutoff { get; set; }
        public bool ForceDisableScreenOnly { get; set; }
    }

    public class Cm1FtpSettings : Cm1FtpCommandSettings
    {
        public bool Enabled { get; set; }
        public string HostName { get; set; }
        public string UserName { get; set; }
        public string PrivateKeyPathway { get; set; }
        public string PrivateKeyPassword { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
    }

    public class Cm1FtpCommandSettings
    {
        // We scan one or multiple folders in the ftp for the files we seek. 
        public List<string> FoldersToScan { get; set; }
        public string FileNamePattern { get; set; }
    }
}