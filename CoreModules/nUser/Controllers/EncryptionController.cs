using System.Web.Mvc;

namespace nUser.Controllers
{
    public class EncryptionController : NController
    {
        [HttpPost]
        public ActionResult KeySet()
        {
            return Json2(NEnv.EncryptionKeys);
        }
    }
}