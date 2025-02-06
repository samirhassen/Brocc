using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;
using System.Linq;
using nGccCustomerApplication.Code;
using Serilog;
using nGccCustomerApplication.Code.ProviderIntegrations;

namespace nGccCustomerApplication.Controllers
{
    [HandleApiError]
    [ProviderApiKeyOrBasicAuthentication]
    public class ExternalProviderApplicationController : NController
    {
        public class ExternalApplicationRequest
        {
            public string ExternalId { get; set; }
            public List<Item> Items { get; set; }

            public class Item
            {
                public string Name { get; set; }
                public string Value { get; set; }
            }
        }

        public class ExternalUpdateStatusRequest
        {
            public string ExternalId { get; set; }
            public string Status { get; set; }
        }

        [Route("api/v1/update-application-status")]
        public ActionResult UpdateApplicationStatus(ExternalUpdateStatusRequest request)
        {
            var provider = GetProvider();

            if (provider == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Not setup properly. Please contact support.");

            var result = provider.Integration.TranslateStatusUpdate(request, provider.Affiliate.ProviderName);
            if (result.Item1)
            {
                var client = new PreCreditClient();
                if (!client.TryExternalProviderEvent(result.Item2, bearerToken: provider.BearerToken))
                {
                    Response.TrySkipIisCustomErrors = true;
                    Response.StatusCode = 400;
                    return Json2(new { success = false });
                }
                else
                {
                    return Json2(new { success = true });
                }
            }
            else
            {
                Response.TrySkipIisCustomErrors = true;
                Response.StatusCode = 400;
                return Json2(new { success = false, errors = result.Item3 });
            }
        }
                
        [Route("api/v1/create-application")]
        public ActionResult CreateApplication()
        {
            List<string> errors = new List<string>();
            int errorStatusCode = 400;
            try
            {
                var p = GetProvider();
                if (p == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Not setup properly. Please contact support.");

                ExternalApplicationRequest request;

                if (p.Affiliate.UsesStandardRequestFormat)
                {
                    StandardIntegration.StandardRequest standardRequest;
                    string failedMessage;
                    if (!TryParseJsonRequestAs(out standardRequest, out failedMessage))
                    {
                        errors.Add(failedMessage);
                        request = null;
                    }
                    else
                    {
                        request = StandardIntegration.TranslateToExternalRequest(standardRequest);
                    }                    
                }
                else
                {
                    string failedMessage;
                    if (!TryParseJsonRequestAs(out request, out failedMessage))
                    {
                        errors.Add(failedMessage);
                    }
                }

                if(!errors.Any())
                {
                    var result = p.Integration.Translate(request);

                    if (result.Item1)
                    {
                        var client = new PreCreditClient();

                        bool? disableAutomation = null;
                        if (NEnv.ProviderNamesWithDisabledAutomation.Contains(p.Affiliate.ProviderName))
                        {
                            disableAutomation = true;
                        }

                        var response = client.CreateCreditApplication(result.Item2, bearerToken: p.BearerToken, disableAutomation: disableAutomation);
                        if (!response.Item1)
                        {
                            throw new Exception("Failed to create application");
                        }
                        else
                        {
                            return Json2(new { success = true, applicationNr = response.Item2.ApplicationNr });
                        }
                    }
                    else
                    {
                        errors.AddRange(result.Item3);
                    }
                }
            }
            catch(Exception ex)
            {
                NLog.Error(ex, "Internal server error");
                errorStatusCode = 500;
                errors.Add("Internal server error");
            }
            Response.TrySkipIisCustomErrors = true;
            Response.StatusCode = errorStatusCode;
            return Json2(new { success = false, errors = errors });
        }

        private class Provider
        {
            public string BearerToken { get; set; }
            public NEnv.Affiliate Affiliate { get; set; }
            public Code.ProviderIntegrations.ProviderIntegrationBase Integration { get; set; }
        }

        private Provider GetProvider()
        {
            var p = new Provider();

            var authResult = ExternalProviderApplicationAuthenticationBase.RequireAuthResult(HttpContext);

            p.Affiliate = NEnv.GetAffiliateModel(authResult.ProviderName);
            if (p.Affiliate == null)
                return null;

            p.BearerToken = authResult.PreCreditBearerToken;

            if (p.Affiliate.UsesStandardRequestFormat)
            {
                p.Integration = new Code.ProviderIntegrations.StandardIntegration(p.Affiliate.ProviderName);
            } else  if (p.Affiliate.ProviderName == "eone")
            {
                p.Integration = new Code.ProviderIntegrations.EoneIntegration();
            }
            else if (p.Affiliate.ProviderName == "etua")
            {
                p.Integration = new Code.ProviderIntegrations.EtuaIntegration();
            }
            else if (p.Affiliate.ProviderName == "salus")
            {
                p.Integration = new Code.ProviderIntegrations.SalusIntegration();
            }
            else if(p.Affiliate.ProviderName == Code.ProviderIntegrations.LendoIntegration.ProviderName)
            {
                p.Integration = new Code.ProviderIntegrations.LendoIntegration();
            }

            if (p.Integration == null)
                return null;
            else
                return p;
        }
    }
}
