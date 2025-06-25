using nCredit.Code;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech;
using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.BankAccounts.Se;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using NTech.Banking.Shared.BankAccounts.Fi;

namespace nCredit.Controllers
{
    [NTechAuthorize()]
    [NTechApi]
    [RoutePrefix("Api/CustomerPages")]
    public class ApiCustomerPagesController : NController
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!filterContext.ActionDescriptor.ActionName.IsOneOf("TryLoginWithToken"))
            {
                var customerId = filterContext.ActionParameters.FirstOrDefault(x => x.Key == "customerId").Value;
                if (customerId == null)
                    filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
                base.OnActionExecuting(filterContext);
            }
        }

        [HttpPost]
        [Route("TryLoginWithToken")]
        public ActionResult TryLoginWithToken(string token)
        {
            using (var context = new CreditContext())
            {
                var t = context.OneTimeTokens.SingleOrDefault(x => x.TokenType == "CustomerPagesLogin" && x.Token == token);

                //TODO: Log to for example audit that this customer logged in. Log attempts that fail too.

                var result = new
                {
                    IsAllowedLogin = t != null && !t.IsExpired(),
                    IsTokenExpired = t != null && t.IsExpired(),
                    Customer = t != null && !t.IsExpired()
                    ? JsonConvert.DeserializeAnonymousType(t.TokenExtraData, new { CustomerId = 0, FirstName = "" })
                    : null
                };

                if (t != null)
                {
                    t.RemovedBy = CurrentUserId;
                    t.RemovedDate = Clock.Now;
                    t.ChangedById = CurrentUserId;
                    t.ChangedDate = t.RemovedDate.Value;
                    t.InformationMetaData = InformationMetadata;

                    context.SaveChanges();
                }

                return Json2(result);
            }
        }

        public class CustomerFacingCreditModel
        {
            public string CreditNr { get; set; }
            public int CustomerApplicantNr { get; set; }
            public DateTimeOffset StartDate { get; set; }
            public string Status { get; set; }
            public DateTime? StatusDate { get; set; }
            public decimal CurrentCapitalDebtAmount { get; set; }
            public decimal CurrentTotalInterestRatePercent { get; set; }
            public decimal MonthlyPaymentAmount { get; set; }
            public decimal? MonthlyAmortizationAmount { get; set; }
            public IEnumerable<int> CustomerIds { get; set; }
            public string DirectDebitBankAccountNr { get; set; }
            public string IsDirectDebitActive { get; set; }
            public string KycQuestionsJsonDocumentArchiveKey { get; set; }
            public int? RepaymentTimeInMonths { get; set; }
        }

        public static IQueryable<CustomerFacingCreditModel> GetCustomerFacingCreditModels(CreditContextExtended context, int customerId)
        {
            return context
                .CreditHeaders
                .Where(x => x.CreditCustomers.Any(y => y.CustomerId == customerId))
                .Select(x => new
                {
                    x.CreditNr,
                    x.StartDate,
                    CustomerIds = x.CreditCustomers.Select(y => y.CustomerId),
                    ApplicantNr = x.CreditCustomers.Where(y => y.CustomerId == customerId).Select(y => y.ApplicantNr).FirstOrDefault(),
                    LatestStatusItem = x
                        .DatedCreditStrings
                        .Where(y => y.Name == DatedCreditStringCode.CreditStatus.ToString())
                        .OrderByDescending(y => y.BusinessEventId)
                        .FirstOrDefault(),
                    DirectDebitBankAccountNr = x
                        .DatedCreditStrings
                        .Where(y => y.Name == DatedCreditStringCode.DirectDebitBankAccountNr.ToString())
                        .OrderByDescending(y => y.BusinessEventId)
                        .Select(y => y.Value)
                        .FirstOrDefault(),
                    KycQuestionsJsonDocumentArchiveKey = x
                        .DatedCreditStrings
                        .Where(y => y.Name == DatedCreditStringCode.KycQuestionsJsonDocumentArchiveKey.ToString())
                        .OrderByDescending(y => y.BusinessEventId)
                        .Select(y => y.Value)
                        .FirstOrDefault(),
                    IsDirectDebitActive = x
                        .DatedCreditStrings
                        .Where(y => y.Name == DatedCreditStringCode.IsDirectDebitActive.ToString())
                        .OrderByDescending(y => y.BusinessEventId)
                        .Select(y => y.Value)
                        .FirstOrDefault(),
                    MarginInterestRate = x
                        .DatedCreditValues
                        .Where(y => y.Name == DatedCreditValueCode.MarginInterestRate.ToString())
                        .OrderByDescending(y => y.BusinessEventId)
                        .Select(y => (decimal?)y.Value)
                        .FirstOrDefault(),
                    ReferenceInterestRate = x
                        .DatedCreditValues
                        .Where(y => y.Name == DatedCreditValueCode.ReferenceInterestRate.ToString())
                        .OrderByDescending(y => y.BusinessEventId)
                        .Select(y => (decimal?)y.Value)
                        .FirstOrDefault(),
                    NotificationFee = x
                        .DatedCreditValues
                        .Where(y => y.Name == DatedCreditValueCode.NotificationFee.ToString())
                        .OrderByDescending(y => y.BusinessEventId)
                        .Select(y => (decimal?)y.Value)
                        .FirstOrDefault(),
                    AnnuityAmount = x
                        .DatedCreditValues
                        .Where(y => y.Name == DatedCreditValueCode.AnnuityAmount.ToString())
                        .OrderByDescending(y => y.BusinessEventId)
                        .Select(y => (decimal?)y.Value)
                        .FirstOrDefault(),
                    CapitalDebt = x
                        .Transactions
                        .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                        .Sum(y => (decimal?)y.Amount),
                    MonthlyAmortizationAmount = x
                        .DatedCreditValues
                        .Where(y => y.Name == DatedCreditValueCode.MonthlyAmortizationAmount.ToString())
                        .OrderByDescending(y => y.BusinessEventId)
                        .Select(y => (decimal?)y.Value)
                        .FirstOrDefault()
                })
                .Select(x => new CustomerFacingCreditModel
                {
                    CreditNr = x.CreditNr,
                    CustomerApplicantNr = x.ApplicantNr,
                    StartDate = x.StartDate,
                    Status = x.LatestStatusItem.Value,
                    StatusDate = x.LatestStatusItem.TransactionDate,
                    MonthlyPaymentAmount = (x.AnnuityAmount ?? 0m) + (x.NotificationFee ?? 0m),
                    CurrentTotalInterestRatePercent = (x.MarginInterestRate ?? 0m) + (x.ReferenceInterestRate ?? 0m),
                    CurrentCapitalDebtAmount = x.CapitalDebt ?? 0m,
                    MonthlyAmortizationAmount = x.MonthlyAmortizationAmount,
                    CustomerIds = x.CustomerIds,
                    IsDirectDebitActive = x.IsDirectDebitActive,
                    DirectDebitBankAccountNr = x.DirectDebitBankAccountNr,
                    KycQuestionsJsonDocumentArchiveKey = x.KycQuestionsJsonDocumentArchiveKey
                });
        }

        [HttpPost]
        [Route("Credits")]
        public ActionResult Credits(int? customerId)
        {
            if (!customerId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            }
            using (var context = CreateCreditContext())
            {
                var credits = GetCustomerFacingCreditModels(context, customerId.Value)
                    .OrderByDescending(x => x.StartDate)
                    .ToList()
                    .Select(x => new
                    {
                        x.CreditNr,
                        x.Status,
                        x.StatusDate,
                        x.StartDate,
                        x.CurrentTotalInterestRatePercent,
                        x.CurrentCapitalDebtAmount,
                        x.MonthlyPaymentAmount
                    });
                return Json2(new { Credits = credits });
            }
        }

        [HttpPost]
        [Route("CreditDetails")]
        public ActionResult CreditDetails(int? customerId, string creditNr, int? maxTransactionsCount, int? startBeforeTransactionId)
        {
            return CreditDetailsI(customerId, creditNr, maxTransactionsCount, startBeforeTransactionId);
        }

        [HttpPost]
        [Route("EventOrderedCreditTransactions")]
        public ActionResult EventOrderedCreditTransactions(int? customerId, string creditNr, int? maxCountTransactions, int? startBeforeTransactionId)
        {
            if (string.IsNullOrWhiteSpace(creditNr))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing creditNr");
            }
            if (!customerId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            }
            using (var context = new CreditContext())
            {
                var transactions = GetCapitalTransactionsOrderedByEvent(context, customerId.Value, creditNr, maxCountTransactions, startBeforeTransactionId);
                return Json2(new
                {
                    Transactions = transactions
                });
            }
        }

        public class OpenNotificationDocument
        {
            public string DocumentType { get; set; }
            public string DocumentId { get; set; }
        }

        public class OpenNotificationModel
        {
            public int Id { get; set; }
            public string CreditNr { get; set; }
            public DateTime DueDate { get; set; }
            public bool IsOverdue { get; set; }
            public decimal TotalUnpaidNotifiedAmount { get; set; }
            public string OcrPaymentReference { get; set; }
            public string PaymentIban { get; set; }
            public string PaymentBankGiro { get; set; }
            public List<OpenNotificationDocument> Documents { get; set; }
            public decimal InitialNotifiedAmount { get; set; }
            public DateTime? LatestPaymentDate { get; set; }
            public bool IsOpen { get; set; }
        }

        public static List<OpenNotificationModel> GetOpenNotifications(int customerId, IClock clock, ICreditContextExtended context, IBankAccountNumber incomingPaymentBankAccountNr, PaymentOrderService paymentOrderService)
        {            
            var credits = context
                .CreditHeadersQueryable
                .Where(x => x.CreditCustomers.Any(y => y.CustomerId == customerId))
                .OrderByDescending(x => x.CreatedByBusinessEventId)
                .Select(x => new
                {
                    x.CreditNr,
                    ApplicantNr = x
                        .CreditCustomers
                        .Where(y => y.CustomerId == customerId)
                        .Select(y => y.ApplicantNr)
                        .FirstOrDefault(),
                    OcrPaymentReference = x
                        .DatedCreditStrings
                        .Where(y => y.Name == DatedCreditStringCode.OcrPaymentReference.ToString())
                        .OrderByDescending(y => y.BusinessEventId)
                        .Select(y => y.Value)
                        .FirstOrDefault(),
                    x.Notifications
                })
                .Select(x => new
                {
                    x.CreditNr,
                    x.OcrPaymentReference,
                    RemindersOnOpenNotifications = x
                        .Notifications
                        .Where(y => !y.ClosedTransactionDate.HasValue)
                        .SelectMany(y => y.Reminders.Select(z => new
                        {
                            NotificationId = y.Id,
                            ReminderId = z.Id,
                            z.ReminderNumber,
                            PdfArchiveKey = z.Documents.Where(zz => zz.ApplicantNr == x.ApplicantNr).Select(zz => zz.ArchiveKey).FirstOrDefault()
                        })),
                    OpenNotificationArchiveKeys = x
                        .Notifications
                        .Where(y => !y.ClosedTransactionDate.HasValue)
                        .Select(y => new { NotificationId = y.Id, y.PdfArchiveKey })
                })
                .ToList()
                .ToDictionary(x => x.CreditNr);

            var remindersByNotificationId = credits
                .SelectMany(x => x.Value.RemindersOnOpenNotifications)
                .GroupBy(x => x.NotificationId)
                .ToDictionary(x => x.Key, x => x.ToList());

            var notificationsByCreditNr = CreditNotificationDomainModel.CreateForSeveralCredits(new HashSet<string>(credits.Keys), context, 
                paymentOrderService.GetPaymentOrderItems(), onlyFetchOpen: false);

            return credits
                .Where(x => notificationsByCreditNr.ContainsKey(x.Key))
                .SelectMany(x => notificationsByCreditNr[x.Key].Select(y => y.Value).Select(y =>
                {
                    var documents = new List<OpenNotificationDocument>();

                    var hasNotificationPdfArchiveKey = x
                        .Value
                        .OpenNotificationArchiveKeys
                        .Any(z => z.NotificationId == y.NotificationId && z.PdfArchiveKey != null);

                    if (hasNotificationPdfArchiveKey)
                        documents.Add(new OpenNotificationDocument { DocumentType = "Notification", DocumentId = y.NotificationId.ToString() });

                    var reminders = remindersByNotificationId.Opt(y.NotificationId);
                    if (reminders != null)
                    {
                        foreach (var r in reminders.Where(z => z.PdfArchiveKey != null).OrderBy(z => z.ReminderNumber))
                        {
                            documents.Add(new OpenNotificationDocument { DocumentType = $"Reminder{r.ReminderNumber}", DocumentId = r.ReminderId.ToString() });
                        }
                    }

                    return new OpenNotificationModel
                    {
                        Id = y.NotificationId,
                        CreditNr = x.Value.CreditNr,
                        DueDate = y.DueDate,
                        IsOverdue = y.GetNrOfPassedDueDatesWithoutFullPaymentSinceNotification(clock.Today) > 0,
                        TotalUnpaidNotifiedAmount = y.GetRemainingBalance(clock.Today),
                        OcrPaymentReference = x.Value.OcrPaymentReference,
                        PaymentIban = NEnv.ClientCfg.Country.BaseCountry == "FI" ? ((IBANFi)incomingPaymentBankAccountNr).NormalizedValue : null,
                        PaymentBankGiro = NEnv.ClientCfg.Country.BaseCountry == "SE" ? ((BankGiroNumberSe)incomingPaymentBankAccountNr).NormalizedValue : null,
                        Documents = documents,
                        InitialNotifiedAmount = y.GetInitialAmount(clock.Today),
                        LatestPaymentDate = y.GetLastPaymentTransactionDate(clock.Today),
                        IsOpen = y.IsOpen(clock.Today)
                    };
                }
                ))
                .OrderByDescending(y => y.DueDate)
                .ThenByDescending(y => y.Id)
                .ToList();
        }

        [HttpPost]
        [Route("OpenNotifications")]
        public ActionResult OpenNotifications(int? customerId)
        {
            if (!customerId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            }
            using (var context = CreateCreditContext())
            {
                return Json2(new
                {
                    Notifications = GetOpenNotifications(customerId.Value, Clock, context, Service.PaymentAccount.GetIncomingPaymentBankAccountNr(), Service.PaymentOrder)
                });
            }
        }

        public static Tuple<string, string, byte[]> GetCreditDocumentContentTypeNameAndData(int customerId, string documentType, string documentId, IDocumentClient documentClient)
        {
            string archiveKey = null;
            using (var context = new CreditContext())
            {
                if (documentType.Equals("Notification", StringComparison.OrdinalIgnoreCase))
                {
                    var notificationId = int.Parse(documentId);
                    archiveKey = context
                        .CreditNotificationHeaders
                        .Where(x => x.Credit.CreditCustomers.Any(y => y.CustomerId == customerId) && x.Id == notificationId)
                        .Select(x => x.PdfArchiveKey)
                        .FirstOrDefault();
                }
                else if (documentType.StartsWith("Reminder", StringComparison.OrdinalIgnoreCase))
                {
                    var reminderId = int.Parse(documentId);
                    archiveKey = context
                        .Documents
                        .Where(x => x.DocumentType.StartsWith("Reminder") && x.CreditReminderHeaderId == reminderId && x.Reminder.Credit.CreditCustomers.Any(y => y.CustomerId == customerId))
                        .Select(x => x.ArchiveKey)
                        .FirstOrDefault();
                }
                else if (documentType.IsOneOfIgnoreCase("ProxyAuthorization", "MortgageLoanDenuntiation", "MortgageLoanLagenhetsutdrag", "MortgageLoanCustomerAmortizationPlan", "InitialAgreement"))
                {
                    //Credit level shared documents
                    var idParsed = int.Parse(documentId);
                    archiveKey = context
                        .Documents
                        .Where(x => x.DocumentType == documentType && x.Id == idParsed && x.Credit.CreditCustomers.Any(y => y.CustomerId == customerId))
                        .OrderByDescending(x => x.Id)
                        .Select(x => x.ArchiveKey)
                        .FirstOrDefault();
                }
                else
                    return null;
            }
            if (archiveKey == null)
            {
                return null;
            }

            var fetchResult = documentClient.TryFetchRaw(archiveKey);
                        
            if (fetchResult.IsSuccess)
            {
                return Tuple.Create(fetchResult.ContentType, fetchResult.FileName, fetchResult.FileData);
            }
            else
                return null;
        }

        /// <summary>
        /// We do this rather than using the archive keys directly to enable customer locking the documents
        /// to prevent the user from just changing the url and accessing other users documents
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("CreditDocument")]
        public ActionResult CreditDocument(int? customerId, string documentType, string documentId)
        {
            if (!customerId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            }
            if (string.IsNullOrWhiteSpace(documentType))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing documentType");
            }
            if (string.IsNullOrWhiteSpace(documentId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing documentId");
            }

            var d = GetCreditDocumentContentTypeNameAndData(customerId.Value, documentType, documentId, Service.DocumentClientHttpContext);
            if (d != null)
            {
                var r = new FileStreamResult(new MemoryStream(d.Item3), d.Item1);
                r.FileDownloadName = d.Item2;
                return r;
            }
            else
                return HttpNotFound();
        }

        private ActionResult CreditDetailsI(int? customerId, string creditNr, int? maxTransactionsCount, int? startBeforeTransactionId)
        {
            if (!customerId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            }
            if (string.IsNullOrWhiteSpace(creditNr))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing creditNr");
            }
            using (var context = CreateCreditContext())
            {
                var creditDetails = GetCustomerFacingCreditModels(context, customerId.Value)
                    .Where(X => X.CreditNr == creditNr)
                    .FirstOrDefault();

                var model = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, NEnv.EnvSettings);

                int nrOfRemainingPayments;
                int? repaymentTimeInMonths = null;
                var isMortgageLoan = model.GetCreditType() == CreditType.MortgageLoan;
                var date = Clock.Today;

                if (ApiCreditDetailsController.TryGetNrOfRemainingPayments(Clock.Today, NEnv.NotificationProcessSettings.GetByCreditType(model.GetCreditType()), model.GetAmortizationModel(date),
                    model.GetNotNotifiedCapitalBalance(date),
                    model.GetInterestRatePercent(date),
                    model.GetNotificationFee(date),
                    null,
                    out nrOfRemainingPayments,
                    out _))
                {
                    repaymentTimeInMonths = nrOfRemainingPayments;
                }

                creditDetails.RepaymentTimeInMonths = repaymentTimeInMonths;

                if (creditDetails == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such credit");
                }

                var transactions = GetCapitalTransactionsOrderedByEvent(context, customerId.Value, creditNr, maxTransactionsCount, startBeforeTransactionId);

                return Json2(new
                {
                    Details = new
                    {
                        creditDetails.CreditNr,
                        creditDetails.StartDate,
                        creditDetails.Status,
                        creditDetails.StatusDate,
                        creditDetails.MonthlyPaymentAmount,
                        creditDetails.CurrentCapitalDebtAmount,
                        creditDetails.CurrentTotalInterestRatePercent,
                        creditDetails.RepaymentTimeInMonths
                    },
                    Transactions = transactions
                });
            }
        }

        public class CreditCapitalTransactionCustomerPagesModel
        {
            public long Id { get; set; }
            public int CreatedByEventId { get; set; }
            public string BusinessEventType { get; set; }
            public DateTime? TransactionDate { get; set; }
            public decimal? Amount { get; set; }
            public decimal? BalanceAfterAmount { get; set; }
        }

        public static IList<CreditCapitalTransactionCustomerPagesModel> GetCapitalTransactionsOrderedByEvent(CreditContext context, int customerId, string creditNr, int? maxCountTransactions, int? startBeforeTransactionId)
        {
            if (maxCountTransactions.HasValue && maxCountTransactions.Value <= 0)
                return new List<CreditCapitalTransactionCustomerPagesModel>(); //Allows callers to opt out of fetching transactions

            var transactionsBase = context
                .CreditHeaders
                .Where(x => x.CreditCustomers.Any(y => y.CustomerId == customerId) && x.CreditNr == creditNr)
                .SelectMany(x => x.Transactions.Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString()));

            IQueryable<AccountTransaction> transactionsPre;
            if (startBeforeTransactionId.HasValue)
                transactionsPre = transactionsBase.Where(x => x.Id < startBeforeTransactionId.Value);
            else
                transactionsPre = transactionsBase;

            var transactions = transactionsPre
                .OrderByDescending(y => y.BusinessEventId)
                .ThenByDescending(y => y.Id)
                .Select(x => new CreditCapitalTransactionCustomerPagesModel
                {
                    Id = x.Id,
                    CreatedByEventId = x.BusinessEvent.Id,
                    BusinessEventType = x.BusinessEvent.EventType,
                    Amount = x.Amount,
                    TransactionDate = x.TransactionDate,
                    BalanceAfterAmount = (transactionsBase
                        .Where(y => y.BusinessEventId <= x.BusinessEventId && y.Id <= x.Id)
                        .Sum(y => (decimal?)y.Amount) ?? 0m)
                });

            if (maxCountTransactions.HasValue)
                transactions = transactions.Take(maxCountTransactions.Value);

            return transactions.ToList();
        }

        public class CreditsAccountDocumentsResult
        {
            public List<Document> Documents { get; set; }

            public class Document
            {
                public string CreditNr { get; set; }
                public string DocumentType { get; set; }
                public DateTimeOffset DocumentDate { get; set; }
                public string ArchiveKey { get; set; }
            }
        }

        [HttpPost]
        [Route("GetDocuments")]
        public ActionResult GetDocuments(int? customerId)
        {
            if (!customerId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing customerId");
            }

            var creditsAccountDocumentsResult = new CreditsAccountDocumentsResult
            {
                Documents = new List<CreditsAccountDocumentsResult.Document>()
            };

            Action<string, DateTimeOffset, string, string> addDocument = (creditNr, documentDate, documentType, archiveKey) =>
                 {
                     creditsAccountDocumentsResult.Documents.Add(
                        new CreditsAccountDocumentsResult.Document() { CreditNr = creditNr, DocumentDate = documentDate, DocumentType = documentType, ArchiveKey = archiveKey });
                 };

            #region Get InitialAgreement documents

            using (var context = CreateCreditContext())
            {
                var creditCustomers = context
                    .CreditCustomers.Where(x => x.CustomerId == customerId)
                    .ToList()
                     .Select(x => new
                     {
                         x.CreditNr,
                         x.ApplicantNr
                     });

                var models = CreditDomainModel.PreFetchForCredits(context, creditCustomers.Select(x => x.CreditNr).Distinct().ToArray(), NEnv.EnvSettings);
                foreach (var item in models.Values)
                {
                    var fountdcreditCustomer = creditCustomers.Where(x => x.CreditNr == item.CreditNr).FirstOrDefault();
                    var archiveKey = item.GetSignedInitialAgreementArchiveKey(Clock.Today, fountdcreditCustomer.ApplicantNr);
                    var startDate = item.GetStartDate();
                    if (!string.IsNullOrWhiteSpace(archiveKey))
                        addDocument(item.CreditNr, startDate, "InitialAgreement", archiveKey);
                }

                #endregion Get InitialAgreement documents

                #region Get AdditionalLoanAgreement documents

                var creditCustomersAdditionalLoanAgreement = context
                   .CreditCustomers.Where(x => x.CustomerId == customerId)
                   .SelectMany(x =>
                    x
                        .Credit
                        .Documents.Where(y => y.ApplicantNr == x.ApplicantNr && y.DocumentType == "AdditionalLoanAgreement")
                        .Select(t => new { t.ArchiveKey, t.ChangedDate, t.CreditNr })
                     )
                   .ToList();

                foreach (var item in creditCustomersAdditionalLoanAgreement)
                {
                    addDocument(item.CreditNr, item.ChangedDate, "AdditionalLoanAgreement", item.ArchiveKey);
                }

                #endregion Get AdditionalLoanAgreement documents

                #region Get ChangeTermAgreement documents

                var creditChangeTermAgreement = context
                    .CreditCustomers
                    .Where(x => x.CustomerId == customerId)
                    .SelectMany(x =>
                        x
                            .Credit
                            .TermsChanges
                            .SelectMany(y => y
                                .Items
                                .Where(t => t.ApplicantNr == x.ApplicantNr && t.Name == "SignedAgreementDocumentArchiveKey")
                            .Select(t => new { t.Value, t.CreatedByEvent.TransactionDate, t.CreditTermsChange.CreditNr })
                ))
                .ToList();

                foreach (var item in creditChangeTermAgreement)
                {
                    addDocument(item.CreditNr, item.TransactionDate, "ChangeTermAgreement", item.Value);
                }

                #endregion Get ChangeTermAgreement documents

                creditsAccountDocumentsResult.Documents = creditsAccountDocumentsResult.Documents.OrderByDescending(x => x.DocumentDate).ThenBy(x => x.CreditNr).ToList();
                return Json2(creditsAccountDocumentsResult);
            }
        }
    }
}