using Newtonsoft.Json;
using NTech.Core.PreCredit.Shared.Services;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.Eventing;
using Serilog;
using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api")]
    public class CreditApplicationProviderController : NController
    {
        [Route("creditapplication/create")]
        [HttpPost]
        public ActionResult Create(LegacyUnsecuredLoanApplicationRequest request, bool? disableAutomation, bool? skipHideFromManualUserLists)
        {
            if (NEnv.IsMortgageLoansEnabled || NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsCompanyLoansEnabled)
                return HttpNotFound();

            try
            {
                string failedMessage;
                string applicationNr;
                if (!this.Service.Resolve<BalanziaFiUnsecuredLoanApplicationCreationService>().TryCreateBalanziaFiLikeApplication(request, disableAutomation, skipHideFromManualUserLists, this.NTechUser, out failedMessage, out applicationNr))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
                else
                    return Json(new { ApplicationNr = applicationNr });
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to create application");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Route("creditapplication/external-provider-event")]
        [HttpPost]
        public ActionResult ProviderEvent(string providerApplicationId, string providerName, string eventName, bool? disableAutomation)
        {
            if (NEnv.IsMortgageLoansEnabled)
                return HttpNotFound();

            try
            {
                if (string.IsNullOrWhiteSpace(providerApplicationId) || string.IsNullOrWhiteSpace(eventName) || string.IsNullOrWhiteSpace(providerName))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing providerApplicationId or eventName or providerName");
                }

                var u = this.User?.Identity as System.Security.Claims.ClaimsIdentity;
                if (u?.FindFirst("ntech.isprovider")?.Value == "true")
                {
                    disableAutomation = null; //only system users are allowed to opt out of automation
                    var loggedInPn = u?.FindFirst("ntech.providername")?.Value;
                    if (loggedInPn != providerName)
                    {
                        throw new Exception("Providers can only add applications for themselves");
                    }
                }
                else if (u?.FindFirst("ntech.issystemuser")?.Value != "true")
                {
                    throw new NotImplementedException();
                }

                string applicationNr;
                using (var c = new PreCreditContext())
                {
                    var app = c
                        .CreditApplicationHeaders
                        .Where(x => x.Items.Any(y => y.GroupName == "application" && y.Name == "providerApplicationId" && y.Value == providerApplicationId))
                        .Select(x => new
                        {
                            x.ProviderName,
                            x.ApplicationNr,
                            x.IsActive
                        })
                        .SingleOrDefault();

                    if (app == null || providerName != app.ProviderName)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such application");
                    }

                    applicationNr = app.ApplicationNr;
                }

                NTechEventHandler.PublishEvent(PreCreditEventCode.CreditApplicationExternalProviderEvent.ToString(), JsonConvert.SerializeObject(new
                {
                    applicationNr = applicationNr,
                    providerName = providerName,
                    eventName = eventName,
                    disableAutomation = disableAutomation
                }));

                return Json(new { });
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to handle provider event");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }
    }
}
