using nCustomer.Code;
using System.IO;
using System.Web.Mvc;

namespace nCustomer.Controllers.Api
{
    [RoutePrefix("Api/ArchiveDocument")]
    public class ArchiveDocumentController : NController
    {
        [HttpGet()]
        [Route("Show")]
        public ActionResult Show(string key, bool setFilename = false)
        {
            var c = new DocumentClient();
            string contentType;
            string filename;
            var b = c.FetchRawWithFilename(key, out contentType, out filename);
            return new FileStreamResult(new MemoryStream(b), contentType)
            {
                FileDownloadName = setFilename ? filename : null
            };
        }
    }
}