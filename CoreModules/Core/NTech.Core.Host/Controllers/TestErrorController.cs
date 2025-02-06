using Microsoft.AspNetCore.Mvc;

namespace NTech.Core.Host.Controllers
{
    [ApiController]
    public class TestErrorController : Controller
    {
        /// <summary>
        /// Test core
        /// </summary>
        [Route("Api/TestError")]
        [HttpPost]
        public JsonResult TestError()
        {
            throw new Exception("Testing errorhandler");
        }
    }
}
