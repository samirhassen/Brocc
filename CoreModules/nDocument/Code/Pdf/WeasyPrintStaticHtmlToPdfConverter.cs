using nDocument.Pdf;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;

namespace nDocument.Code.Pdf
{
    /// <summary>
    /// First version is contained in a console application, why we call it outside of nDocument. 
    /// </summary>
    public class WeasyPrintStaticHtmlToPdfConverter : IStaticHtmlToPdfConverter
    {
        // Executable of WeasyPrintRunner. 
        private readonly string executableLocation;

        public WeasyPrintStaticHtmlToPdfConverter(string weasyPrintExeLocation)
        {
            executableLocation = weasyPrintExeLocation;
        }

        public bool TryRenderToTempFile(string templateFilePath, string targetFile, string logFile)
        {
            // Needs to be called with the arguments in citations, why they are escaped. 
            var arguments = $"\"{templateFilePath}\" \"{targetFile}\"";

            try
            {
                var p = new Process
                {
                    StartInfo =
                    {
                        FileName = executableLocation,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    }
                };

                p.Start();

                p.WaitForExit();
                if (!File.Exists(targetFile))
                {
                    throw new Exception("Weasyprint failed to produce a pdf: " + p.StandardError.ReadToEnd());
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex.ToString());
                return false;
            }

            return true;

        }
    }
}