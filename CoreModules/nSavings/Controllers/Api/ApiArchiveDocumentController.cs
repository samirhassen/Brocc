using System.IO;
using System.Web.Mvc;
using nSavings.Code;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Api
{
    [NTechApi]
    public class ApiArchiveDocumentController : NController
    {
        [HttpGet, Route("Api/ArchiveDocument")]
        public ActionResult ArchiveDocument(string key, bool setFileDownloadName = false)
        {
            var c = new DocumentClient();
            if (!c.TryFetchRaw(key, out var contentType, out var fileName, out var b))
            {
                return HttpNotFound();
            }

            var result = new FileStreamResult(new MemoryStream(b), contentType);
            if (setFileDownloadName && !string.IsNullOrWhiteSpace(fileName))
            {
                result.FileDownloadName = fileName;
            }

            return result;
        }
    }
}