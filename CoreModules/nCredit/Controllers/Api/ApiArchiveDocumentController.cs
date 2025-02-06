using Microsoft.Ajax.Utilities;
using NTech.Services.Infrastructure;
using System.IO;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiArchiveDocumentController : NController
    {

        [Route("Api/ArchiveDocument")]
        [HttpGet()]
        public ActionResult ArchiveDocument(string key, bool setFileDownloadName = false)
        {
            var c = Service.DocumentClientHttpContext;
            var fetchResult = c.TryFetchRaw(key);
            if (!fetchResult.IsSuccess)
            {
                return HttpNotFound();
            }

            var result = new FileStreamResult(new MemoryStream(fetchResult.FileData), fetchResult.ContentType.ToString());
            if (setFileDownloadName && !string.IsNullOrWhiteSpace(fetchResult.FileName))
            {
                result.FileDownloadName = fetchResult.FileName;
            }
            return result;
        }
    }
}