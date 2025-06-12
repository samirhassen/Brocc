using System;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace nSavings.Code
{
    public static class ZipFiles
    {
        /// <summary>
        /// CreateFlatZipFile(Tuple.Create("test1.txt", <...>))
        ///
        /// Will create a zipfile with a single file test1.txt with the included data.
        /// </summary>
        public static MemoryStream CreateFlatZipFile(params Tuple<string, Stream>[] fileNamesAndData)
        {
            //Based on: https://stackoverflow.com/questions/8830386/sharpziplib-create-an-archive-with-an-in-memory-string-and-download-as-an-attach
            var resultStream = new MemoryStream();
            using (var zipStream = new ZipOutputStream(resultStream))
            {
                zipStream.IsStreamOwner = false;
                foreach (var (fileName, fileData) in fileNamesAndData)
                {
                    var entry = new ZipEntry(fileName)
                    {
                        DateTime = DateTime.Now
                    };
                    zipStream.PutNextEntry(entry);
                    StreamUtils.Copy(fileData, zipStream, new byte[4096]);
                    zipStream.CloseEntry();
                }
            }

            resultStream.Position = 0;
            return resultStream;
        }
    }
}