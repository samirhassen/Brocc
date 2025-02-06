using NTech.Services.Infrastructure;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api")]
    public partial class ApiApplicationCustomerIdsByCreditNrsController : NController
    {
        [Route("GetCreditApplicationCustomerIdsByCreditNrs")]
        [HttpPost]
        public ActionResult GetCreditApplicationCustomerIdsByCreditNrs(List<string> creditNrs)
        {
            using (var context = new PreCreditContext())
            {
                var cs = (creditNrs ?? new List<string>()).Distinct().ToList();


                var applicationListItems = context
                    .ComplexApplicationListItems
                    .Where(x => x.ItemName == "creditNr" && x.ListName == "Application" && cs.Contains(x.ItemValue))
                    .Select(x => new
                    {
                        ApplicationNr = x.ApplicationNr,
                        CreditNr = x.ItemValue
                    })
                    .ToList();


                if (applicationListItems.GroupBy(x => x.CreditNr).Any(x => x.Count() > 1))
                {
                    NLog.Error("There are several applications with the same creditnr");
                    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
                }

                var appNr = applicationListItems.First().ApplicationNr;

                var customerIds = context
                    .ComplexApplicationListItems
                    .Where(x => x.ApplicationNr == appNr && x.ItemName == "customerId" && x.ListName == "Applicant")
                     .Select(x => new
                     {
                         CustomerId = x.ItemValue,
                         ApplicantNr = x.Nr
                     })
                    .ToList();

                return Json2(customerIds);

            }
        }
    }
}