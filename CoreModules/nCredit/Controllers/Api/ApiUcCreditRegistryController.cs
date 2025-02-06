using NTech.Services.Infrastructure;
using System;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("Api")]
    public class ApiUcCreditRegistryController : NController
    {
        [Route("UcCreditRegistry/ReportCreditChanges")]
        [HttpPost]
        public ActionResult ReportCreditChanges()
        {
            if (NEnv.ClientCfg.Country.BaseCountry != "SE")
                throw new Exception("Only supported in country SE");
            return CreditContext.RunWithExclusiveLock("ntech.ncredit.uccreditregistry.reportchanges", () =>
            {
                this.Service.UcCreditRegistryService.ReportCreditsChangedSinceLastReport(this.GetCurrentUserMetadata());
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }, () => new HttpStatusCodeResult(HttpStatusCode.BadRequest, "The job was already running"));
        }
    }
}