using NTech.Services.Infrastructure;
using System;
using System.Linq;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiTestFindRandomCreditController : NController
    {
        private T PickRandomElement<T>(IOrderedQueryable<T> items)
        {
            var count = items.Count();
            if (count == 0)
                return default(T);

            var r = new Random();
            var skipCount = r.Next(0, count);
            return items.Skip(skipCount).First();
        }

        [HttpPost]
        [Route("Api/Credit/TestFindRandom")]
        public ActionResult FindRandom(string creditType)
        {
            if (NEnv.IsProduction)
                return HttpNotFound();

            using (var context = new CreditContext())
            {
                if (creditType == "debtcol")
                {
                    var debtColStatus = CreditStatus.SentToDebtCollection.ToString();
                    var creditNr = PickRandomElement(context.CreditHeaders.Where(x => x.Status == debtColStatus).Select(x => x.CreditNr).OrderBy(x => x));
                    return Json2(new { creditNr });
                }
                else if (creditType == "overdue")
                {
                    var today = Clock.Today;
                    var normalStatus = CreditStatus.Normal.ToString();
                    var creditNr = PickRandomElement(context.CreditNotificationHeaders.Where(x => x.DueDate < today && x.Credit.Status == normalStatus && !x.ClosedTransactionDate.HasValue).Select(x => x.CreditNr).OrderBy(x => x));
                    return Json2(new { creditNr });
                }
                else if (creditType == "any")
                {
                    var creditNr = PickRandomElement(context.CreditHeaders.Select(x => x.CreditNr).OrderBy(x => x));
                    return Json2(new { creditNr });
                }
                else if (creditType == "noimpairments")
                {
                    var today = Clock.Today;
                    var normalStatus = CreditStatus.Normal.ToString();
                    var impairedCredits = context.CreditNotificationHeaders.Where(x => x.DueDate < today && !x.ClosedTransactionDate.HasValue).Select(x => x.CreditNr).ToList();
                    var creditNr = PickRandomElement(context.CreditHeaders.Where(x => x.Status == normalStatus && !impairedCredits.Contains(x.CreditNr)).Select(x => x.CreditNr).OrderBy(x => x));
                    return Json2(new { creditNr });
                }
                else if (creditType == "withcoapplicant")
                {
                    var normalStatus = CreditStatus.Normal.ToString();
                    var creditNr = PickRandomElement(context.CreditHeaders.Where(x => x.Status == normalStatus && x.NrOfApplicants > 1).Select(x => x.CreditNr).OrderBy(x => x));
                    return Json2(new { creditNr });
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}