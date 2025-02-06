using Microsoft.AspNetCore.Mvc;
using NTech.Core.Module;

namespace NTech.Core.Host.Controllers
{
    [ApiController]
    [NTechRequireFeatures(RequireFeaturesAll = new[] { "ntech.feature.helpsearch" })]
    public class AiHelpController : Controller
    {
        private readonly IHttpClientFactory httpClientFactory;

        public AiHelpController(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        [Route("Api/Help/Query")]
        [HttpPost]
        public async Task<ActionResult> GetQuery(AiHelpRequest request)
        {
            if (NEnv.SharedInstance.IsProduction)
            {
                throw new Exception("This feature is not available in production."); //Currently only available as dev demo feature
            }

            var accessCode = NEnv.SharedInstance.OptionalSetting("ntech.ai.accessCode");
            if(accessCode == null)
                return new StatusCodeResult(400);

            var url = NEnv.SharedInstance.OptionalSetting("ntech.ai.customUrl") ?? "https://ntech-ai.azurewebsites.net";
            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(url);
            var result = await client.PostAsJsonAsync($"api/backoffice-help-query?code={accessCode}", request);

            if (result.IsSuccessStatusCode)
            {
                var json = await result.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            else
                return new StatusCodeResult(500);
        }
    }

    public class AiHelpRequest
    {
        public string OngoingQueryId { get; set; }
        public string NewQuery { get; set; }
    }
}
