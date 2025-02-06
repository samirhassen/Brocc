using NTech.Services.Infrastructure;
using System.IO;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechApi]
    public class ApiArchiveDocumentController : NController
    {
        [Route("Api/ArchiveDocument")]
        [HttpGet()]
        public ActionResult ArchiveDocument(string key, bool setFileDownloadName = false)
        {
            var c = new Code.DocumentClient();
            string contentType;
            string fileName;
            byte[] b;
            if (!c.TryFetchRaw(key, out contentType, out fileName, out b))
            {
                return HttpNotFound();
            }

            var result = new FileStreamResult(new MemoryStream(b), contentType.ToString());
            if (setFileDownloadName && !string.IsNullOrWhiteSpace(fileName))
            {
                result.FileDownloadName = fileName;
            }
            return result;
        }
    }
}