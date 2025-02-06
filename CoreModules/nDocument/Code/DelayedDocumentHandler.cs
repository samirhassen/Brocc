using nDocument.Code.Archive;
using nDocument.Controllers;
using System.IO;
using System.Text;
using System.Web.Mvc;

namespace nDocument.Code
{
    public class DelayedDocumentHandler
    {
        public const string PdfTemplateMimeType = "x-ntech/pdftemplate1";
        public const string PdfContextMimeType = "x-ntext/pdfcontext1";
        public const string DelayedPdfType = "NtechDelayedPdfV1";

        public ActionResult HandleDelayedPdf(string archiveKey, ArchiveFetchResult result)
        {
            var contextBytes = ReadStream(result.Content);

            var ap = ArchiveProviderFactory.Create();
            var templateDocumentResult = ap.Fetch(result.OptionalData.DelayedDocumentTemplateArchiveKey);
            var templateBytes = ReadStream(templateDocumentResult.Content);

            var pdfBytes = PdfController.RenderDirectShared(() => templateBytes, Encoding.UTF8.GetString(contextBytes));

            var r = new FileStreamResult(new MemoryStream(pdfBytes), "application/pdf");
            r.FileDownloadName = result.FileName;
            return r;
        }

        private byte[] ReadStream(Stream s)
        {
            var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }
    }
}