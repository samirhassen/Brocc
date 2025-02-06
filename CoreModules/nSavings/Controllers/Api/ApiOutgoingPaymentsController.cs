using nSavings.DbModel.BusinessEvents;
using NTech.Services.Infrastructure;
using System.Linq;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechApi]
    public class ApiOutgoingPaymentsController : NController
    {
        [HttpPost]
        [Route("Api/OutgoingPayments/GetFilesPage")]
        public ActionResult GetOutgoingPaymentFilesPage(int pageSize, int pageNr = 0)
        {
            using (var context = new SavingsContext())
            {
                var baseResult = context
                    .OutgoingPaymentFileHeaders;

                var totalCount = baseResult.Count();
                var currentPage = baseResult
                    .OrderByDescending(x => x.CreatedByBusinessEventId)
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .ToList()
                    .Select(x => new
                    {
                        x.TransactionDate,
                        x.FileArchiveKey,
                        PaymentsCount = x.Payments.Count,
                        UserId = x.ChangedById,
                        PaymentsAmount = -(x
                            .Payments
                            .SelectMany(y => y.Transactions)
                            .Where(y => y.AccountCode == LedgerAccountTypeCode.ShouldBePaidToCustomer.ToString() && y.BusinessEventId == x.CreatedByBusinessEventId)
                            .Sum(y => (decimal?)y.Amount) ?? 0m)
                    })
                    .ToList()
                    .Select(x => new
                    {
                        x.TransactionDate,
                        x.PaymentsAmount,
                        x.PaymentsCount,
                        x.UserId,
                        UserDisplayName = GetUserDisplayNameByUserId(x.UserId.ToString()),
                        x.FileArchiveKey,
                        ArchiveDocumentUrl = Url.Action("ArchiveDocument", "ApiArchiveDocument", new { key = x.FileArchiveKey, setFileDownloadName = true }),
                    })
                    .ToList();

                var nrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);

                return Json2(new
                {
                    CurrentPageNr = pageNr,
                    TotalNrOfPages = nrOfPages,
                    Page = currentPage.ToList()
                });
            }
        }

        [HttpPost]
        [Route("Api/OutgoingPayments/CreateBankFile")]
        public ActionResult CreateOutgoingPaymentsBankFile()
        {
            using (var context = new SavingsContext())
            {
                var documentClient = new Code.DocumentClient();
                var mgr = new NewOutgoingPaymentFileBusinessEventManager(CurrentUserId, InformationMetadata);
                var file = mgr.Create(context);

                context.SaveChanges();

                return Json2(new { outgoingPaymentFileHeaderId = file.Id });
            }
        }

        [HttpPost]
        [Route("Api/OutgoingPayments/FetchPayments")]
        public ActionResult FetchPayments(int outgoingPaymentFileHeaderId)
        {
            using (var context = new SavingsContext())
            {
                var result = context
                    .OutgoingPaymentHeaders
                    .Where(x => x.OutgoingPaymentFileHeaderId == outgoingPaymentFileHeaderId)
                    .Select(x => new
                    {
                        x.Id,
                        EventTypePaymentSource = x.CreatedByEvent.EventType,
                        x.OutgoingPaymentFile.TransactionDate,
                        x.OutgoingPaymentFile.BookKeepingDate,
                        PaidToCustomerTransactions = x
                            .Transactions
                            .Where(y => y.BusinessEventId == x.OutgoingPaymentFile.CreatedByBusinessEventId && y.AccountCode == LedgerAccountTypeCode.ShouldBePaidToCustomer.ToString())
                    })
                    .Where(x => x.PaidToCustomerTransactions.Any())
                    .Select(x => new
                    {
                        x.Id,
                        x.EventTypePaymentSource,
                        x.TransactionDate,
                        x.BookKeepingDate,
                        PaidToCustomerAmount = -x.PaidToCustomerTransactions.Sum(y => y.Amount),
                        SavingsAccount = x.PaidToCustomerTransactions.Select(y => y.SavingsAccount).FirstOrDefault(),
                    })
                    .Select(x => new
                    {
                        x.Id,
                        x.EventTypePaymentSource,
                        x.SavingsAccount.SavingsAccountNr,
                        x.TransactionDate,
                        x.BookKeepingDate,
                        x.PaidToCustomerAmount
                    })
                    .OrderBy(x => x.Id)
                    .ToList();

                return Json2(new { Payments = result });
            }
        }
    }
}