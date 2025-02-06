using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.IO;

namespace NTech.Services.Infrastructure
{
    public class PdfTemplateReader
    {
        private readonly IPdfTemplateReaderHelper helper;
        private static FewItemsCache cache = new FewItemsCache();

        public PdfTemplateReader(IPdfTemplateReaderHelper helper)
        {
            this.helper = helper;
        }

        public byte[] GetPdfTemplate(string templateName, string clientCountryTwoLetterIsoCode, bool isTemplateCachingDisabled)
        {
            return isTemplateCachingDisabled
                ? GetPdfTemplateI(templateName, clientCountryTwoLetterIsoCode)
                : cache.WithCache($"ntech.shared.pdftemplatebytes.{templateName}", TimeSpan.FromMinutes(5), () => GetPdfTemplateI(templateName, clientCountryTwoLetterIsoCode));
        }

        private byte[] GetPdfTemplateI(string templateName, string clientCountryTwoLetterIsoCode)
        {
            var clientPdfTemplateFolder = GetClientPdfTemplateFolder(false);
            if (clientPdfTemplateFolder.Exists)
            {
                if (TryGetPdfTemplateZipFileFromFolder(templateName, clientPdfTemplateFolder.FullName, out var zipFile))
                    return zipFile;
            }

            if (TryGetPdfTemplateZipFileFromFolder(templateName, GetSharedPdfTemplateFolder(clientCountryTwoLetterIsoCode).FullName, out var zipFile2))
                return zipFile2;

            throw new Exception($"Pdf template {templateName} does not exist");
        }

        private bool TryGetPdfTemplateZipFileFromFolder(string templateName, string templateFolder, out byte[] zipFile)
        {
            var name = templateName;
            if (templateName.EndsWith(".zip"))
                name = templateName.Substring(0, templateName.Length - ".zip".Length);

            var templateZipPath = Path.Combine(templateFolder, name + ".zip");
            var templateFolderPath = Path.Combine(templateFolder, name);

            if (Directory.Exists(templateFolderPath))
            {
                zipFile =  helper.CreateZipFileFromFolder(templateFolderPath);
                return true;
            }
            else if (File.Exists(templateZipPath))
            {
                zipFile = System.IO.File.ReadAllBytes(templateZipPath);
                return true;
            }
            else
            {
                zipFile = null;
                return false;
            }
        }

        private DirectoryInfo GetClientPdfTemplateFolder(bool mustExist)
            => helper.ClientResourceDirectory("ntech.pdf.templatefolder", "PdfTemplates", mustExist);

        private DirectoryInfo GetSharedPdfTemplateFolder(string clientCountryTwoLetterIsoCode)
        {
            var sharedResourcesFolder = helper.ClientResourceDirectory("ntech.client.sharedresourcesfolder", "Shared", mustExist: true);
            return new DirectoryInfo(Path.Combine(sharedResourcesFolder.FullName, $"PdfTemplates/{clientCountryTwoLetterIsoCode}"));
        }
    }
}
