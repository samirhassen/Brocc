using System;

namespace nDocument.Pdf
{
    public class PrinceXmlStaticHtmlToPdfConverter : IStaticHtmlToPdfConverter
    {
        private readonly string princeExePath;
        private Action<Prince> setLicense;

        public PrinceXmlStaticHtmlToPdfConverter(string princeExePath, string licenseKey = null, string licenseFilePath = null)
        {
            this.princeExePath = princeExePath;
            if (!string.IsNullOrWhiteSpace(licenseKey))
            {
                setLicense = p => p.SetLicensekey(licenseKey);
            }
            else if (!string.IsNullOrWhiteSpace(licenseFilePath))
            {
                setLicense = p => p.SetLicenseFile(licenseFilePath);
            }
            else
            {
                setLicense = _ => { };
            }
        }

        public bool TryRenderToTempFile(string templateFilePath, string targetFile, string logFile)
        {
            var prn = new Prince(princeExePath);

            prn.SetLog(logFile);
            prn.SetEmbedFonts(true);
            prn.SetCompress(true);
            setLicense(prn);

            var pdfProfile = NEnv.PrinceXmlPdfProfile;

            if (NEnv.PrinceXmlPdfProfile != null)
                prn.SetOptions($"--pdf-profile=\"{pdfProfile}\"");

            return prn.Convert(
                templateFilePath,
                targetFile);
        }
    }
}
