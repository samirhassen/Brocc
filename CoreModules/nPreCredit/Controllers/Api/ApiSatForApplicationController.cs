using nPreCredit.Code.Services;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorizeCreditMiddle]
    [RoutePrefix("api")]
    public partial class ApiSatForApplicationController : NController
    {
        [Route("FetchSatReportForApplication")]
        [HttpPost]
        public ActionResult FetchSatReportForApplication(int creditReportId, IList<string> requestedCreditReportFields)
        {
            try
            {
                var c = Service.Resolve<CreditReportService>();
                var result = c.GetCreditReportById(creditReportId, requestedCreditReportFields);

                return Json2(new
                {
                    satReport = result == null ? null : new
                    {
                        result.RequestDate,
                        result.CustomerId,
                        result.CreditReportId,
                        result.ProviderName,
                        result.Items
                    }
                });
            }
            catch(NTechCoreWebserviceException ex)
            {
                if (ex.ErrorCode == "noSuchCreditReportExists")
                    return Json2(null);
                else
                    throw;
            }
        }
    }
}