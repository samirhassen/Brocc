using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class Files
    {
        public static bool TryParseDataUrl(string dataUrl, out string mimeType, out byte[] binaryData)
        {
            var result = System.Text.RegularExpressions.Regex.Match(dataUrl, @"data:(?<type>.+?);base64,(?<data>.+)");
            if (!result.Success)
            {
                mimeType = null;
                binaryData = null;
                return false;
            }
            else
            {
                mimeType = result.Groups["type"].Value.Trim();
                binaryData = Convert.FromBase64String(result.Groups["data"].Value.Trim());
                return true;
            }
        }

        public static string MoveFileToFolder(string filename, string targetFolder)
        {
            var targetFile = System.IO.Path.Combine(targetFolder, System.IO.Path.GetFileName(filename));
            System.IO.File.Move(filename, targetFile);
            return targetFile;
        }
    }
}
