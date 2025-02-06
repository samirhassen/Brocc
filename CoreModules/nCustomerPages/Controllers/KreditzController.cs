using NTech.Core.Module.Shared;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared;
using NTech.Services.Infrastructure;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace nCustomerPages.Controllers
{
    public class KreditzController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!NEnv.ClientCfgCore.IsFeatureEnabled("ntech.feature.unsecuredloans.datasharing"))
            {
                filterContext.Result = HttpNotFound();
            }
            base.OnActionExecuting(filterContext);
        }
        
        private static Lazy<FewItemsCache> cache = new Lazy<FewItemsCache>(() => new FewItemsCache());
        private static KreditzApiClient.KreditzSettings Settings =>
            cache.Value.WithCache("settings", TimeSpan.FromMinutes(5), () => KreditzApiClient.GetSettings(NTechEnvironmentLegacy.SharedInstance));

        [Route("api/kz/poll")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> Poll(string caseId)
        {
            ActionResult Result(bool hasAccountData) => new JsonNetActionResult
            {
                Data = new
                {
                    hasAccountData = hasAccountData
                }
            };

            if (caseId == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest, "Missing caseId");

            var settings = Settings;
            var client = new HttpClient();
            var accessToken = await KreditzApiClient.GetCachedAccessTokenAsync(client, settings.ApiClientId, settings.ApiClientSecret, cache.Value);

            var caseDataResult = await KreditzApiClient.FindByCase(client, caseId, accessToken, NEnv.ClientCfgCore);
            return Result(caseDataResult.HasData);
        }

        [Route("api/kz/callback1")]
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Callback1()
        {
            //HandleCallback();
            return new HttpStatusCodeResult(System.Net.HttpStatusCode.OK);
        }

        [Route("api/kz/callback4")]
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Callback4()
        {
            //HandleCallback();
            return new HttpStatusCodeResult(System.Net.HttpStatusCode.OK);
        }

        /* NOTE: Left this in uncommented in case we run into problems with the pure polling solution. Doing this works fine it just has two major issues:
         *       - There is signing and the caseId is exposed to the user in the iframe so an attacker could just post directly here so we need to get it by api anyway to ensure kreditz is the source
         *       - Testing is way easier when there are no callbacks since we can run from localhost and debug the full flow.
         *       - If we really need to use the callbacks we should add ip whitelisting
         *       - Still set these callback endpoints in their ui so we can enable them if needed
         *       
         *       
                private static ConcurrentDictionary<string, KreditzData> AccountDataByCaseId = new ConcurrentDictionary<string, KreditzData>();
                private void HandleCallback()
                {
                    bool HandlResultData(string rawData)
                    { 
                        var parsedData = KreditzApiClient.ParseBasicData(rawData);
                        if (parsedData.CaseId == null || parsedData.CivicRegNr == null)
                            return false; //TODO: Log?

                        if (!NEnv.BaseCivicRegNumberParser.TryParse(parsedData.CivicRegNr, out var parsedCivicRegNr))
                            return false; //TODO: Log?

                        AccountDataByCaseId[parsedData.CaseId] = new KreditzData
                        {
                            CivicRegNr = parsedCivicRegNr.NormalizedValue,
                            RawData = rawData
                        };
                        return true;
                    }

                    if (!Request.ContentType.ToLowerInvariant().Contains("json"))
                    {
                        //TODO: Log somehow
                        return;
                    }
                    Request.InputStream.Position = 0;
                    using (var r = new StreamReader(Request.InputStream))
                    {
                        var rawData = r.ReadToEnd();
                        HandlResultData(rawData);
                    }
                }
                private class KreditzData
                {
                    public string CivicRegNr { get; set; }
                    public string RawData { get; set; }
                }
        */
    }
}