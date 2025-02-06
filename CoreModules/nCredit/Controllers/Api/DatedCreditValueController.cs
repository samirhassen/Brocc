using NTech.Services.Infrastructure;
using System.Linq;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class DatedCreditValueController : NController
    {
        [HttpPost]
        [Route("Api/Credit/FetchDatedCreditValueItems")]
        public ActionResult FetchDatedCreditValueItems(string creditNr, string name)
        {
            using (var context = new CreditContext())
            {
                var datedCreditValues = context
                .DatedCreditValues
                .Where(x => x.CreditNr == creditNr && x.Name == name)
                .GroupBy(x => x.TransactionDate)
                .Select(x => x.OrderByDescending(y => y.BusinessEventId).FirstOrDefault())
                .Select(x => new
                {
                    x.TransactionDate,
                    x.Value
                })
                .ToList();

                return Json2(datedCreditValues);
            }
        }
    }
}