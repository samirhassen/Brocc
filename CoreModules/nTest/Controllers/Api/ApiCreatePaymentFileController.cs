using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using nTest.RandomDataSource;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;

namespace nTest.Controllers
{
    [NTechApi]
    [RoutePrefix("Api")]
    public class ApiCreatePaymentFileController : NController
    {
        private class TempFile
        {
            public byte[] Content { get; set; }
            public string FileName { get; set; }
            public string ContentType { get; set; }
            public string Key { get; set; }
        }

        private static RingBuffer<TempFile> FilesCache = new RingBuffer<TempFile>(100);

        [Route("GetUnpaidInvoices")]
        [HttpPost]
        public ActionResult GetUnpaidInvoices()
        {
            var c = new CreditDriverCreditClient();
            var unpaidInvoices = c.GetAllUnpaidNotifications(notificationDate: null);
            var payments = unpaidInvoices
                .GroupBy(x => x.ExpectedPaymentOcrPaymentReference)
                .Select(x => new 
                {
                    UnpaidAmount = x.Sum(y => y.UnpaidAmount),
                    DueDate = x.Min(y => y.DueDate),
                    OcrPaymentReference = x.Key,
                    CreditNrsText = string.Join(",", x.Select(y => y.CreditNr).Distinct())
                })
                .ToList();
            return Json2(new
            {
                unpaidInvoices,
                payments = payments
            });
        }

        [Route("GetCreditOrSavingsPaymentInfo")]
        [HttpPost]
        public ActionResult GetCreditOrSavingsPaymentInfo(string nr)
        {
            if (nr.StartsWith("L"))
            {
                var client = new CreditDriverCreditClient();
                var ns = client.GetNotificationsSummary(nr);
                var cs = client.GetCreditDetails(nr);
                if (ns == null || cs == null)
                    return Json2(new { exists = false });
                else
                    return Json2(new
                    {
                        exists = true,
                        reference = ns.OcrPaymentReference,
                        amount = (ns.TotalUnpaidAmount + cs.Details.NotNotifiedCapitalAmount).ToString(CultureInfo.InvariantCulture),
                        payerName = "Test"
                    });
            }
            else
            {
                var client = new SavingsDriverSavingsClient();
                var details = client.GetSavingsAccountDetails(nr);
                if (details == null)
                    return Json2(new { exists = false });
                else
                    return Json2(new
                    {
                        exists = true,
                        reference = details.Details.OcrDepositReference,
                        amount = 1000m,
                        payerName = "Test"
                    });
            }
        }

        [Route("CreatePaymentFile")]
        [HttpPost]
        public ActionResult CreatePaymentFile()
        {
            CreatePaymentFileRequest request;
            Request.InputStream.Position = 0;
            using (var r = new StreamReader(Request.InputStream))
            {
                request = JsonConvert.DeserializeObject<CreatePaymentFileRequest>(r.ReadToEnd());
            }            

            if (request.FileFormat == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing fileformat");

            if (request.FileFormat != "camt.054.001.02" && request.FileFormat != "bgmax")
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Unsupported fileformat");

            if (request.Payments == null || request.Payments.Count == 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing payments");

            var p = request.Payments
                .Select(x => new TestPaymentFileCreator.Payment
                {
                    Amount = decimal.Parse(x.Amount.Replace(",", ".").Replace(" ", ""), CultureInfo.InvariantCulture),
                    BookkeepingDate = x.BookKeepingDate != null ? DateTime.ParseExact(x.BookKeepingDate, "yyyy-MM-dd", CultureInfo.InvariantCulture) : (request.BookkeepingDate ?? DateTime.Today),
                    OcrReference = x.Reference,
                    PayerName = x.PayerName
                })
                .ToList();

            TempFile tf;
            var creator = new TestPaymentFileCreator();
            var key = Guid.NewGuid().ToString();
            if (request.FileFormat == "camt.054.001.02")
            {
                var paymentFile = creator.Create_Camt_054_001_02File(p, clientIban: request.ClientIban);
                tf = new TempFile
                {
                    Content = Encoding.UTF8.GetBytes(paymentFile.ToString()),
                    ContentType = "application/xml",
                    FileName = $"TestPaymentFile-{DateTime.Now.ToString("yyyyMMddHHmmss")}.xml",
                    Key = key
                };
            }
            else if (request.FileFormat == "bgmax")
            {
                var paymentFileData = creator.Create_BgMax_File(DateTime.Now, p, clientBankGiroNr: request.ClientIban);
                tf = new TempFile
                {
                    Content = paymentFileData,
                    ContentType = "text/plain",
                    FileName = $"TestPaymentFile-{DateTime.Now.ToString("yyyyMMddHHmmss")}_bgmax.txt",
                    Key = key
                };
            }
            else
                throw new NotImplementedException();

            FilesCache.Add(tf);

            return Json2(new
            {
                url = Url.Action("DownloadPaymentFile", new { key = key }),
                fileName = tf.FileName
            });
        }

        [HttpGet]
        [Route("DownloadPaymentFile")]
        public ActionResult DownloadPaymentFile(string key)
        {
            var f = FilesCache.Where(x => x.Key == key).SingleOrDefault();
            if (f == null)
                return HttpNotFound();
            else
                return new FileStreamResult(new MemoryStream(f.Content), f.ContentType) { FileDownloadName = f.FileName };
        }

        public class PaymentModel
        {
            public string Amount { get; set; }
            public string BookKeepingDate { get; set; }
            public string PayerName { get; set; }
            public string Reference { get; set; }
        }

        private class CreatePaymentFileRequest
        {
            public string FileFormat { get; set; }
            public List<PaymentModel> Payments { get; set; }
            public DateTime? BookkeepingDate { get; set; }
            public string ClientIban { get; set; }
        }
    }
}