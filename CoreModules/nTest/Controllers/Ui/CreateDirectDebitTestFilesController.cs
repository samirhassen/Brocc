using Newtonsoft.Json;
using NTech.Banking.Autogiro;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.CivicRegNumbers.Se;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web.Mvc;

namespace nTest.Controllers
{
    public class CreateDirectDebitTestFilesController : NController
    {
        [Route("Ui/CreateDirectDebitTestFiles")]
        public ActionResult CreateDirectDebitTestFiles()
        {
            var agSettings = NEnv.AutogiroSettings;

            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.GetEncoding("iso-8859-1").GetBytes(JsonConvert.SerializeObject(new
            {
                currentTime = TimeMachine.SharedInstance.GetCurrentTime(),
                bankGiroNr = agSettings.BankGiroNr.NormalizedValue,
                bgcCustomerNr = agSettings.CustomerNr,
                incomingStatusFileImportFolder = agSettings.IncomingStatusFileImportFolder
            })));

            return View("Index");
        }

        [Route("Api/CreateIncomingDirectDebitStatusChangeFile")]
        [HttpPost]
        public ActionResult CreateIncomingDirectDebitStatusChangeFile(IncomingDirectDebitStatusChangeRequest request)
        {
            string msg;
            BankGiroNumberSe bgNr;
            if (!BankGiroNumberSe.TryParseWithErrorMessage(request.ClientBankGiroNr, out bgNr, out msg))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Invalid ClientBankGiroNr '{request.ClientBankGiroNr}': {msg}");
            }
            var f = AutogiroStatusChangeFileFromBgcBuilder.New(request.BankGiroCustomerNr, bgNr, new Lazy<DateTime>(() => DateTime.Now), true);
            foreach (var i in request.Items)
            {
                BankAccountNumberSe b = null;
                if (!string.IsNullOrWhiteSpace(i.BankAccountNr))
                {
                    if (!BankAccountNumberSe.TryParse(i.BankAccountNr, out b, out msg))
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Invalid BankAccountNr '{i.BankAccountNr}': {msg}");
                    }
                }

                CivicRegNumberSe c = null;
                if (!string.IsNullOrWhiteSpace(i.CivicRegNr))
                {
                    if (!CivicRegNumberSe.TryParse(i.CivicRegNr, out c))
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Invalid CivicRegNr '{i.CivicRegNr}'");
                    }
                }
                f.AddStatusChangeItem(i.PaymentNr, b, c, i.InfoCode, i.CommentCode);
            }

            string copyFailedMessage = null;
            var agSettings = NEnv.AutogiroSettings;
            try
            {
                System.IO.Directory.CreateDirectory(agSettings.IncomingStatusFileImportFolder);
                f.SaveToFolderWithCorrectFilename(new System.IO.DirectoryInfo(agSettings.IncomingStatusFileImportFolder));
            }
            catch (Exception ex)
            {
                copyFailedMessage = $"Copy failed: {ex.ToString()}";
            }

            var fileBytes = f.ToByteArray();
            return Json2(new
            {
                fileAsDataUrl = $"data:text/plain;base64,{Convert.ToBase64String(fileBytes)}",
                fileName = f.GetFileName(),
                copyFailedMessage
            });
        }
    }

    public class IncomingDirectDebitStatusChangeRequest
    {
        public bool CopyToImportFolder { get; set; }

        public string BankGiroCustomerNr { get; set; }
        public string ClientBankGiroNr { get; set; }

        public List<Item> Items { get; set; }
        public class Item
        {
            public string PaymentNr { get; set; }
            public string BankAccountNr { get; set; }
            public string CivicRegNr { get; set; }
            public string CommentCode { get; set; }
            public string InfoCode { get; set; }
        }
    }
}