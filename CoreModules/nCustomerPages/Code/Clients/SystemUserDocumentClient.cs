using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;

namespace nCustomerPages.Code
{
    public class SystemUserDocumentClient : AbstractSystemUserServiceClient
    {
        protected override string ServiceName => "nDocument";

        private class ArchiveStoreResult
        {
            public string Key { get; set; }
        }

        public byte[] FetchRawWithFilename(string key, out string contentType, out string filename)
        {
            contentType = null;
            filename = null;
            using (var ms = new MemoryStream())
            {
                var r = Begin()
                    .Get("Archive/Fetch?key=" + key);
                if (r.IsNotFoundStatusCode)
                    return null;
                r
                    .DownloadFile(ms, out contentType, out filename);
                return ms.ToArray();
            }
        }

        public string ArchiveStoreFromUrl(Uri urlToFile, string fileName)
        {
            var client = new System.Net.Http.HttpClient();
            var result = client.GetAsync(urlToFile.ToString()).Result;
            using (var ms = new MemoryStream())
            {
                result.Content.CopyToAsync(ms).Wait();

                return Begin()
                    .PostJson("Archive/Store", new
                    {
                        MimeType = result.Content.Headers.ContentType.ToString(),
                        FileName = fileName,
                        Base64EncodedFileData = Convert.ToBase64String(ms.ToArray())
                    })
                    .ParseJsonAs<ArchiveStoreResult>()
                    .Key;
            }
        }

        public string ArchiveStore(byte[] fileData, string mimeType, string filename)
        {
            return Begin().PostJson("Archive/Store", new
            {
                MimeType = mimeType,
                FileName = filename,
                Base64EncodedFileData = Convert.ToBase64String(fileData)
            }).ParseJsonAs<ArchiveStoreResult>().Key;
        }

        public MemoryStream PdfRenderDirect(string templateName, IDictionary<string, object> context)
        {
            var actualtemplate = Convert.ToBase64String(GetPdfTemplate(templateName));
            var actualContext = JsonConvert.SerializeObject(context);

            var ms = new MemoryStream();
            Begin()
                .PostJson("Pdf/RenderDirect", new
                {
                    template = actualtemplate,
                    context = actualContext
                })
                .CopyToStream(ms);

            ms.Position = 0;
            return ms;
        }

        private byte[] GetPdfTemplate(string templateName)
        {
            return NTechCache.WithCache($"ntech.customerpages.pdftemplatebytes.{templateName}", TimeSpan.FromMinutes(5), () => GetPdfTemplateI(templateName));
        }

        private byte[] GetPdfTemplateI(string templateName)
        {
            var name = templateName;
            if (templateName.EndsWith(".zip"))
                name = templateName.Substring(0, templateName.Length - ".zip".Length);

            var templateZipPath = Path.Combine(NEnv.PdfTemplateFolder.FullName, name + ".zip");
            var templateFolderPath = Path.Combine(NEnv.PdfTemplateFolder.FullName, name);

            if (Directory.Exists(templateFolderPath))
            {
                var fs = new ICSharpCode.SharpZipLib.Zip.FastZip();
                using (var ms = new MemoryStream())
                {
                    fs.CreateZip(ms, templateFolderPath, true, null, null);
                    return ms.ToArray();
                }
            }
            else if (File.Exists(templateZipPath))
            {
                return System.IO.File.ReadAllBytes(templateZipPath);
            }
            else
                throw new Exception($"Pdf template {templateName} does not exist");
        }
    }
}