using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web.Mvc;
using nSavings.DbModel;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Ui
{
    [NTechAuthorizeSavingsMiddle]
    public class OutgoingPaymentsController : NController
    {
        private IDictionary<string, object> GetPending(SavingsContext context)
        {
            var pending = context.OutgoingPaymentHeaders.Select(x => new
                {
                    EventType = x.CreatedByEvent.EventType,
                    ShouldBePaidToCustomerBalance =
                        x.Transactions
                            .Where(y => y.AccountCode == LedgerAccountTypeCode.ShouldBePaidToCustomer.ToString())
                            .Sum(y => (decimal?)y.Amount) ?? 0m,
                    HasPaymentFile = x.OutgoingPaymentFileHeaderId.HasValue
                })
                .Where(x => x.ShouldBePaidToCustomerBalance > 0m && !x.HasPaymentFile)
                .GroupBy(x => x.EventType)
                .Select(x => new
                {
                    EventType = x.Key,
                    Amount = x.Sum(y => (decimal?)y.ShouldBePaidToCustomerBalance) ?? 0m,
                    Count = x.Count()
                })
                .ToList()
                .ToDictionary(x => x.EventType);

            var expectedTypes = new[]
            {
                nameof(BusinessEventType.Withdrawal),
                nameof(BusinessEventType.AccountClosure),
                nameof(BusinessEventType.RepaymentOfUnplacedPayment)
            };
            var unexpectedTypes = pending.Keys.Except(expectedTypes).ToList();

            if (unexpectedTypes.Any())
            {
                throw new Exception(
                    "Outgoing payments contains unexpected types: " + string.Join(", ", unexpectedTypes));
            }

            IDictionary<string, object> result = new ExpandoObject();

            foreach (var type in expectedTypes)
            {
                result[type + "Amount"] = pending.TryGetValue(type, out var amt) ? amt.Amount : 0m;
                result[type + "Count"] = pending.TryGetValue(type, out var cnt) ? cnt.Count : 0;
            }

            result["TotalAmount"] = pending.Values.Sum(x => (decimal?)x.Amount);
            result["TotalCount"] = pending.Values.Aggregate(0, (acc, x) => acc + x.Count);

            return result;
        }

        [HttpGet]
        [Route("Ui/OutgoingPayments/List")]
        public ActionResult Index()
        {
            using (var context = new SavingsContext())
            {
                var result = GetPending(context);

                ViewBag.JsonInitialData = EncodeInitialData(new
                {
                    pending = result,
                    createFileUrl = Url.Action("CreateOutgoingPaymentsBankFile", "ApiOutgoingPayments"),
                    getOutgoingFilesPageUrl = Url.Action("GetOutgoingPaymentFilesPage", "ApiOutgoingPayments")
                });
                return View();
            }
        }
    }
}