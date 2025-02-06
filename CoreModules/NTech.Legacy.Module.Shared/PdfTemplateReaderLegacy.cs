using System;
using System.IO;

namespace NTech.Services.Infrastructure
{
    public static class PdfTemplateReaderLegacy
    {
        public static byte[] GetPdfTemplate(string templateName, string clientCountryTwoLetterIsoCode, Func<string, byte[]> createZipFileFromFolder, bool isTemplateCachingDisabled) =>
            GetReader(createZipFileFromFolder).GetPdfTemplate(templateName, clientCountryTwoLetterIsoCode, isTemplateCachingDisabled);

        private class LegacyHelper : IPdfTemplateReaderHelper
        {
            private readonly Func<string, byte[]> createZipFileFromFolder;

            public LegacyHelper(Func<string, byte[]> createZipFileFromFolder)
            {
                this.createZipFileFromFolder = createZipFileFromFolder;
            }

            public DirectoryInfo ClientResourceDirectory(string settingName, string resourceFolderRelativePath, bool mustExist) =>
                NTechEnvironment.Instance.ClientResourceDirectory(settingName, resourceFolderRelativePath, mustExist);

            public byte[] CreateZipFileFromFolder(string path) => createZipFileFromFolder(path);
        }

        public static PdfTemplateReader GetReader(Func<string, byte[]> createZipFileFromFolder) => new PdfTemplateReader(new LegacyHelper(createZipFileFromFolder));
    }
}
