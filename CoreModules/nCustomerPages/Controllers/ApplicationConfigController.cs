using System.Collections.Generic;
using System.Web.Mvc;

namespace nCustomerPages.Controllers
{
    public class ApplicationConfigController : AnonymousBaseController
    {
        [HttpPost]
        [Route("api/application/fetch-config")]
        public ActionResult FetchConfig(List<string> countryNameLanguages = null)
        {
            Dictionary<string, Dictionary<string, string>> countryNameByLanguageCode = null;
            if (countryNameLanguages != null && countryNameLanguages.Count > 0)
            {
                var r = new Dictionary<string, Dictionary<string, string>>();
                foreach (var lang in countryNameLanguages)
                {
                    r[lang] = new Dictionary<string, string>();
                    foreach (var k in ISO3166.GetCountryCodesAndNames(lang))
                        r[lang][k.code] = k.name;
                }
                countryNameByLanguageCode = r;
            }
            return Json2(new
            {
                IsTest = !NEnv.IsProduction,
                CountryNameByLanguageCode = countryNameByLanguageCode
            });
        }
    }
}