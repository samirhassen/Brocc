using Newtonsoft.Json;
using NTech.Services.Infrastructure.NTechWs;
using nTest.Controllers;
using System.Web.Mvc;

namespace nTest.Code
{
    public static class Cm1KycScreeningTestEndpoint
    {
        public static ActionResult Handle(LoggedRequestController.LoggedRequest request)
        {
            return new RawJsonActionResult
            {
                JsonData = JsonConvert.SerializeObject(new
                {
                    ReturnCode = true,
                    ErrorMessage = (string)null,
                    ScreenResult = new object[] { }
                })
            };
        }
    }
}