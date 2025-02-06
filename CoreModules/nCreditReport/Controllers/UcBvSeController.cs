using nCreditReport.Code;
using nCreditReport.Code.PropertyValuation.UcBvSe;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace nCreditReport.Controllers
{
    public class UcBvSeController : Controller
    {
        private static Lazy<UcBvSeService> Service = new Lazy<UcBvSeService>(() =>
        {
            var settings = NEnv.UcbvSeSettings;
            return new UcBvSeService(settings);
        });


        [Route("Api/UcBvSe/SokAddress")]
        [HttpPost]
        public async Task<ActionResult> SokAddress(string adress, string postnr, string postort, string kommun)
        {
            var result = await Service.Value.SokAdress(adress, postnr, postort, kommun);

            return new JsonNetActionResult
            {
                Data = result
            };
        }

        [Route("Api/UcBvSe/HamtaObjektInfo2")]
        [HttpPost]
        public async Task<ActionResult> HamtaObjektInfo2(string id)
        {
            var result = await Service.Value.HamtaObjektInfo(id);

            return new JsonNetActionResult
            {
                Data = result
            };
        }


        [Route("Api/UcBvSe/HamtaLagenhet")]
        [HttpPost]
        public async Task<ActionResult> HamtaLagenhet(string id, string lghNr)
        {
            var result = await Service.Value.HamtaLagenhet(id, lghNr);

            return new JsonNetActionResult
            {
                Data = result
            };
        }

        [Route("Api/UcBvSe/VarderaBostadsratt")]
        [HttpPost]
        public async Task<ActionResult> VarderaBostadsratt(string id, string lghNr, int? area, bool? includeValuationPdf, bool? includeSeBrfArsredovisningPdf)
        {
            var result = await Service.Value.VarderaBostadsratt(id, lghNr, area);

            if (!result.IsError() && result.Data.Varde.HasValue)
            {
                var documentClient = new DocumentClient();
                var resultObject = JObject.FromObject(result);
                if (includeValuationPdf.GetValueOrDefault())
                {
                    var pdfResult = await Service.Value.HamtaVarderingsPdf2(result.TransId);
                    if (pdfResult.IsOk)
                    {
                        var archiveKey = documentClient.ArchiveStore(pdfResult.Result, "application/pdf", $"Valuation-{result.TransId}.pdf");


                        resultObject.AddOrReplaceJsonProperty("ValuationPdfArchiveKey", new JValue(archiveKey), true);
                    }
                }
                if (includeSeBrfArsredovisningPdf.GetValueOrDefault())
                {
                    try
                    {
                        var pdfResult = await Service.Value.HamtaArsredovisningsPDF2(result.TransId);
                        if (pdfResult.IsOk)
                        {
                            var archiveKey = documentClient.ArchiveStore(pdfResult.Result, "application/pdf", $"Valuation-{result.TransId}.pdf");
                            resultObject.AddOrReplaceJsonProperty("SeBrfArsredovisningPdfArchiveKey", new JValue(archiveKey), true);
                        }
                    }
                    catch (Exception ex)
                    {
                        //Ignored since the api doesnt seem to have a way of indicating when this is missing which it surely can be.
                        NLog.Warning(ex, $"Failed HamtaArsredovisningsPDF2 for object={id}");
                    }
                }
                return new JsonNetActionResult
                {
                    Data = resultObject
                };
            }
            else
            {
                return new JsonNetActionResult
                {
                    Data = result
                };
            }
        }

        [Route("Api/UcBvSe/VarderaSmahus")]
        [HttpPost]
        public async Task<ActionResult> VarderaSmahus(string id, bool? includeValuationPdf, bool? includeInskrivningJson)
        {
            var result = await Service.Value.VarderaSmahus(id);

            if (!result.IsError() && result.Data.Varde.HasValue)
            {
                var documentClient = new DocumentClient();
                var resultObject = JObject.FromObject(result);
                if (includeValuationPdf.GetValueOrDefault())
                {
                    var pdfResult = await Service.Value.HamtaVarderingsPdf2(result.TransId);
                    if (pdfResult.IsOk)
                    {
                        var archiveKey = documentClient.ArchiveStore(pdfResult.Result, "application/pdf", $"Valuation-{result.TransId}.pdf");
                        resultObject.AddOrReplaceJsonProperty("ValuationPdfArchiveKey", new JValue(archiveKey), true);
                    }
                }
                if (includeInskrivningJson.GetValueOrDefault())
                {
                    var inskrivningResult = await Service.Value.Inskrivning(id);

                    if (inskrivningResult.Felkod == 0)
                    {
                        var data = Encoding.UTF8.GetBytes(inskrivningResult.Data.ToString(Formatting.None));
                        inskrivningResult.Data.ToString(Formatting.None);
                        var archiveKey = documentClient.ArchiveStore(data, "application/json", $"Inskrivning-{id}.json");
                        resultObject.AddOrReplaceJsonProperty("InskrivningJsonArchiveKey", new JValue(archiveKey), true);
                    }
                }
                return new JsonNetActionResult
                {
                    Data = resultObject
                };
            }
            else
            {
                return new JsonNetActionResult
                {
                    Data = result
                };
            }
        }
    }
}