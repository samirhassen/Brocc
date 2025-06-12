using System;
using System.Linq;
using System.Web.Mvc;
using nSavings.Code;
using nSavings.DbModel;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Api
{
    [NTechApi]
    public class ApiTestFindRandomSavingsAccountController : NController
    {
        private static T PickRandomElement<T>(IOrderedQueryable<T> items)
        {
            var count = items.Count();
            if (count == 0)
                return default;

            var r = new Random();
            var skipCount = r.Next(0, count);
            return items.Skip(skipCount).FirstOrDefault();
        }

        [HttpPost]
        [Route("Api/SavingsAccount/TestFindRandom")]
        public ActionResult FindRandom(string mustHaveStatus, string mustContainBusinessEventType)
        {
            if (NEnv.IsProduction)
                return HttpNotFound();

            using (var context = new SavingsContext())
            {
                var accounts = context.SavingsAccountHeaders.AsQueryable();

                if (!string.IsNullOrWhiteSpace(mustHaveStatus))
                {
                    accounts = accounts.Where(x => x.Status == mustHaveStatus);
                }

                if (!string.IsNullOrWhiteSpace(mustContainBusinessEventType))
                {
                    accounts = accounts.Where(x =>
                        x.Transactions.Any(y => y.BusinessEvent.EventType == mustContainBusinessEventType));
                }

                var savingsAccountNr = PickRandomElement(accounts.Select(x => x.SavingsAccountNr).OrderBy(x => x));

                return Json2(new { savingsAccountNr = savingsAccountNr });
            }
        }
    }
}