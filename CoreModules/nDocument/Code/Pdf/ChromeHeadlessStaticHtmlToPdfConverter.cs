using ICSharpCode.SharpZipLib.Zip;
using NTech.Services.Infrastructure;
using System;
using System.IO;

namespace nDocument.Pdf
{
    public class ChromeHeadlessStaticHtmlToPdfConverter : IStaticHtmlToPdfConverter
    {
        private readonly Uri serviceUrl;

        public ChromeHeadlessStaticHtmlToPdfConverter(Uri serviceUrl)
        {
            this.serviceUrl = serviceUrl;
        }

        public bool TryRenderToTempFile(string templateFilePath, string targetFile, string logFile)
        {
            using (var source = new MemoryStream())
            using (var target = File.Create(targetFile))
            {
                new FastZip().CreateZip(source, Path.GetDirectoryName(templateFilePath), true, null, null);

                string _; string __;
                NHttp
                    .Begin(serviceUrl, null, timeout: TimeSpan.FromMinutes(30))
                    .PostJson("api/pdf/render-single", new
                    {
                        templateZipFileAsBase64 = Convert.ToBase64String(source.ToArray()),
                        fileInZipFileToRender = Path.GetFileName(templateFilePath),
                        sourceDebugContext = "nDocument.TryRenderToTempFile"
                    })
                    .DownloadFile(target, out _, out __);

                target.Position = 0;

                return true;
            }
        }
    }
}