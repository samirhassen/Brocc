using nCredit.DbModel.BusinessEvents;
using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System.Linq;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiOutgoingPaymentsController : NController
    {
        [HttpPost]
        [Route("Api/OutgoingPayments/GetFilesPage")]
        public ActionResult GetOutgoingPaymentFilesPage(int pageSize, int pageNr = 0)
        {
            using (var context = new CreditContext())
            {
                var baseResult = context
                    .OutgoingPaymentFileHeaders;

                var totalCount = baseResult.Count();
                var currentPage = baseResult
                    .OrderByDescending(x => x.Timestamp)
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
                            .Where(y => y.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString() && y.BusinessEventId == x.CreatedByBusinessEventId)
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
            using (var context = new CreditContextExtended(GetCurrentUserMetadata(), Clock))
            {
                var mgr = Service.NewOutgoingPaymentFile;
                var files = mgr.Create(context);

                context.SaveChanges();

                foreach (var f in files)
                {
                    NTech.Services.Infrastructure.Eventing.NTechEventHandler.PublishEvent(
                        CreditEventCode.OutgoingCreditPaymentFileCreated.ToString(),
                        JsonConvert.SerializeObject(new { outgoingPaymentFileHeaderId = f.Id }));
                }


                return Json2(new { outgoingPaymentFileHeaderId = files.First().Id, outgoingPaymentFileHeaderIds = files.Select(x => x.Id).ToList() });
            }
        }

        [HttpPost]
        [Route("Api/OutgoingPayments/FetchPayments")]
        public ActionResult FetchPayments(int outgoingPaymentFileHeaderId)
        {
            var result = this.Service.OutgoingPayments.FetchPayments(outgoingPaymentFileHeaderId);
            return Json2(new { Payments = result });
        }
    }
}