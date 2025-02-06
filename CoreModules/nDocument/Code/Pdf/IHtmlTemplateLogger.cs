using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace nDocument.Pdf
{
    public interface IHtmlTemplateLogger
    {
        void OnCompiledTemplatePath(string templateCorrelationId, string templatePath);
        void OnRenderToFileBegin(string templateCorrelationId, string contextCorrelationId);
        void OnRenderedHtmlFile(string templateCorrelationId, string contextCorrelationId, string renderedHtmlFileName, Dictionary<string, object> context, TimeSpan timeTaken);
        void OnRenderedPdfFile(string templateCorrelationId, string contextCorrelationId, string renderedPdfFileName, string pdfProductionLogFileName, TimeSpan timeTaken);
        void OnRenderToFileEnd(string templateCorrelationId, string contextCorrelationId);
    }

    public class FileSystemHtmlTemplateLogger : IHtmlTemplateLogger
    {
        private string GetTemplateLogPath(string templateCorrelationId)
        {
            return Path.Combine(NEnv.LogFolder.FullName, $"nDocument/PdfCreationLogs/{templateCorrelationId}");
        }

        public void OnCompiledTemplatePath(string templateCorrelationId, string templatePath)
        {
            var d = GetTemplateLogPath(templateCorrelationId);
            Directory.CreateDirectory(d);

            var ht = Path.Combine(d, "html");
            if (!Directory.Exists(ht))
            {
                Directory.CreateDirectory(ht);
                CopyFilesRecursively(new DirectoryInfo(templatePath), new DirectoryInfo(ht));
            }
        }

        public void OnRenderedHtmlFile(string templateCorrelationId, string contextCorrelationId, string renderedHtmlFileName, Dictionary<string, object> context, TimeSpan timeTaken)
        {
            var d = GetTemplateLogPath(templateCorrelationId);

            var ht = Path.Combine(d, "rendered-html");
            Directory.CreateDirectory(ht);
            File.Copy(renderedHtmlFileName, Path.Combine(ht, $"{contextCorrelationId}.html"));
            var perfLogFile = Path.Combine(ht, "html-render-performancelog-ms.txt");
            File.AppendAllLines(perfLogFile, new[] { $"{contextCorrelationId}: {(int)Math.Round(timeTaken.TotalMilliseconds)}" });

            var ct = Path.Combine(d, "context-data");
            Directory.CreateDirectory(ct);
            File.WriteAllText(Path.Combine(ct, $"{contextCorrelationId}.json"), JsonConvert.SerializeObject(context));
        }

        public void OnRenderedPdfFile(string templateCorrelationId, string contextCorrelationId, string renderedPdfFileName, string pdfProductionLogFileName, TimeSpan timeTaken)
        {
            var d = GetTemplateLogPath(templateCorrelationId);
            var ht = Path.Combine(d, "rendered-pdf");
            Directory.CreateDirectory(ht);

            File.Copy(renderedPdfFileName, Path.Combine(ht, $"{contextCorrelationId}.pdf"));

            var perfLogFile = Path.Combine(ht, "pdf-render-performancelog-ms.txt");

            File.AppendAllLines(perfLogFile, new[] { $"{contextCorrelationId}: {(int)Math.Round(timeTaken.TotalMilliseconds)}" });
        }

        public void OnRenderToFileBegin(string templateCorrelationId, string contextCorrelationId)
        {

        }

        public void OnRenderToFileEnd(string templateCorrelationId, string contextCorrelationId)
        {

        }

        private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name));
        }
    }

    public class DoNothingHtmlTemplateLogger : IHtmlTemplateLogger
    {
        public void OnCompiledTemplatePath(string templateCorrelationId, string templatePath)
        {

        }

        public void OnRenderedHtmlFile(string templateCorrelationId, string contextCorrelationId, string renderedHtmlFileName, Dictionary<string, object> context, TimeSpan timeTaken)
        {

        }

        public void OnRenderedPdfFile(string templateCorrelationId, string contextCorrelationId, string renderedPdfFileName, string pdfProductionLogFileName, TimeSpan timeTaken)
        {

        }

        public void OnRenderToFileBegin(string templateCorrelationId, string contextCorrelationId)
        {

        }

        public void OnRenderToFileEnd(string templateCorrelationId, string contextCorrelationId)
        {

        }
    }
}
