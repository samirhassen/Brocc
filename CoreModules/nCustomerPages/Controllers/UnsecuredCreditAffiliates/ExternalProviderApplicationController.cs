using nCustomerPages.Code;
using nCustomerPages.Controllers.UnsecuredCreditAffiliates.ProviderIntegrations;
using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCustomerPages.Controllers.UnsecuredCreditAffiliates
{
    [HandleApiError]
    [ProviderBasicAuthentication]
    public class ExternalProviderApplicationController : Controller
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

        [Route("api/v1/create-application")]
        public ActionResult CreateApplication()
        {
            if (!NEnv.IsUnsecuredLoansEnabled)
                return HttpNotFound();

            List<string> errors = new List<string>();
            int errorStatusCode = 400;
            try
            {
                var user = ProviderBasicAuthenticationAttribute.RequireAuthResult(this.HttpContext);

                var p = GetProvider(user);
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
                    request = null;
                    errors.Add("This endpoint only supports standard format requests");
                }

                if (!errors.Any())
                {
                    var result = p.Integration.Translate(request);

                    if (result.Item1)
                    {
                        var client = new PreCreditClient(() => p.BearerToken);

                        bool? disableAutomation = null;
                        if (NEnv.ProviderNamesWithDisabledAutomation.Contains(p.Affiliate.ProviderName))
                        {
                            disableAutomation = true;
                        }

                        var response = client.CreateCreditApplication(result.Item2, disableAutomation);
                        return Json2(new { success = true, applicationNr = response.ApplicationNr });

                    }
                    else
                    {
                        errors.AddRange(result.Item3);
                    }
                }
            }
            catch (Exception ex)
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
            public ProviderIntegrationBase Integration { get; set; }
        }

        private Provider GetProvider(ApiKeyOrBearerTokenAuthHelper.AuthResult user)
        {
            var p = new Provider();

            var providerName = user.ProviderName;
            p.BearerToken = user.PreCreditBearerToken;
            if (p.BearerToken == null)
                throw new Exception("Should not be possible. Check ProviderBasicAuthenticationAttribute");

            p.Affiliate = NEnv.GetAffiliateModel(providerName);
            if (p.Affiliate == null)
                return null;

            if (p.Affiliate.UsesStandardRequestFormat)
            {
                p.Integration = new StandardIntegration(p.Affiliate.ProviderName);
            }

            if (p.Integration == null)
                return null;
            else
                return p;
        }

        protected ActionResult Json2(object data, int? customHttpStatusCode = null)
        {
            return new JsonNetActionResult
            {
                Data = data,
                CustomHttpStatusCode = customHttpStatusCode
            };
        }

        protected bool TryParseJsonRequestAs<TRequest>(out TRequest result, out string failedMessage) where TRequest : class
        {
            Request.InputStream.Position = 0;
            using (var r = new StreamReader(Request.InputStream))
            {
                var requestString = r.ReadToEnd();
                try
                {
                    result = JsonConvert.DeserializeObject<TRequest>(requestString);
                    failedMessage = null;
                    return true;
                }
                catch (JsonReaderException ex)
                {
                    failedMessage = ex.Message;
                    result = null;
                    return false;
                }
            }
        }
    }
}
