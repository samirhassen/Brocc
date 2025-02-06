using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace nCustomerPages.Code
{
    public static class AffiliateTrackingModel
    {
        public class ExternalApplicationVariable
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public static List<ExternalApplicationVariable> GetExternalApplicationVariablesFromString(string data)
        {
            if (data == null)
                return null;
            return JsonConvert.DeserializeAnonymousType(data, new { externalApplicationVariables = (List<ExternalApplicationVariable>)null })?.externalApplicationVariables;
        }

        public static string ExternalApplicationVariablesToString(List<ExternalApplicationVariable> externalApplicationVariables)
        {
            if (externalApplicationVariables == null || externalApplicationVariables.Count == 0)
                return null;
            return JsonConvert.SerializeObject(new { externalApplicationVariables });
        }

        public class Settings
        {
            public bool IsEnabled { get; set; }
            public HashSet<string> ExternalVariables { get; set; }
            public string GtmTag1 { get; set; }
            public string GtmTag2 { get; set; }
            public string CookiesScriptTemplate { get; set; }
        }

        public static Settings CreateSettings(NTech.Services.Infrastructure.NTechSimpleSettings s)
        {
            return new Settings
            {
                IsEnabled = s.OptBool("isenabled"),
                ExternalVariables = new HashSet<string>(s.Req("externalvariables").Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x))),
                GtmTag1 = s.Req("gtmtag1"),
                GtmTag2 = s.Req("gtmtag2"),
                CookiesScriptTemplate = s.Req("cookiescript")
            };
        }

        public static List<ExternalApplicationVariable> ExtractExternalApplicationVariablesFromRequest(HttpRequestBase request)
        {
            var result = new List<ExternalApplicationVariable>();
            foreach (var v in NEnv.AllTrackedExternalVariables)
            {
                var q = request.QueryString[v];
                if (!string.IsNullOrWhiteSpace(q))
                    result.Add(new ExternalApplicationVariable { Name = v, Value = q });
            }
            return result;
        }

        public static void SetupLandingPageOnNewAccountOpened(System.Web.Mvc.ControllerBase b, int customerId, string newAccountNr)
        {
            bool hasAffiliateTracking = false;
            if (!string.IsNullOrWhiteSpace(newAccountNr))
            {
                var s = NEnv.SavingsAffiliateTrackingModelSettings;
                if (s.IsEnabled)
                {
                    var sc = new CustomerLockedSavingsClient(customerId);
                    var externalVariables = sc.GetExternalVariables(newAccountNr);
                    if (externalVariables != null && externalVariables.Count > 0)
                    {
                        var cookieScript = s.CookiesScriptTemplate;
                        cookieScript = cookieScript.Replace("{{applicationId}}", newAccountNr);
                        foreach (var e in externalVariables)
                        {
                            cookieScript = cookieScript.Replace("{{externalVariable_" + e.Name + "}}", e.Value);
                        }
                        b.ViewBag.AffiliateCookieScripts = cookieScript;
                        b.ViewBag.AffiliateGtmTag1 = s.GtmTag1;
                        b.ViewBag.AffiliateGtmTag2 = s.GtmTag2;
                        hasAffiliateTracking = true;
                    }
                }
            }
            b.ViewBag.HasAffiliateTracking = hasAffiliateTracking;
        }
    }
}