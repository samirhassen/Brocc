using nPreCredit.Code;
using System.IO;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    public partial class CreditManagementController
    {
        [Route("ArchiveDocument")]
        [HttpGet()]
        public ActionResult ArchiveDocument(string key)
        {
            var c = new nDocumentClient();
            string contentType;
            var b = c.FetchRaw(key, out contentType);

            NTech.Services.Infrastructure.NTechHttpHardening.AllowIFrameEmbeddingForThisContext(this.HttpContext);

            return new FileStreamResult(new MemoryStream(b), contentType);
        }
    }
}