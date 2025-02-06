using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace NTech.Core.Host.Controllers
{
    [ApiController]
    public class TestAuthController : Controller
    {
        /// <summary>
        /// Test core
        /// </summary>
        [HttpPost]
        [Route("Api/TestAuth")]
        public JsonResult TestAuth()
        {
            return Json((User?.Identity as ClaimsIdentity).Claims.Select(x => new
            {
                x.Type,
                x.Value
            }));
        }
    }
}
