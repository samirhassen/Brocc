using System.IO;

namespace NTech.Services.Infrastructure
{
    public interface IPdfTemplateReaderHelper
    {
        // NTechEnvironment.Instance.ClientResourceDirectory
        DirectoryInfo ClientResourceDirectory(string settingName, string resourceFolderRelativePath, bool mustExist);

        /*
         createZipFileFromFolder because we dont want to take a shared dependancy on sharpzip:
                x =>
                {
                    var fs = new ICSharpCode.SharpZipLib.Zip.FastZip();
                    using (var ms = new MemoryStream())
                    {
                        fs.CreateZip(ms, x, true, null, null);
                        return ms.ToArray();
                    }
                }

         */
        byte[] CreateZipFileFromFolder(string path);
    }
}
