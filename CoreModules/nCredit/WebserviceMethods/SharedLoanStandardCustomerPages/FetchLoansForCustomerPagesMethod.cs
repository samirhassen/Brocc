using nCredit.Code;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech;
using NTech.Banking.BankAccounts;
using NTech.Core.Credit.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCredit.WebserviceMethods.SharedLoanStandardCustomerPages
{
    public class FetchLoansForCustomerPagesMethod : TypedWebserviceMethod<FetchLoansForCustomerPagesMethod.Request, FetchLoansForCustomerPagesMethod.Response>
    {
        public override string Path => "LoanStandard/CustomerPages/Fetch-Loans";

        public override bool IsEnabled => (NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsStandardMortgageLoansEnabled) && NEnv.ClientCfg.Country.BaseCountry == "SE";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var today = requestContext.Clock().Today;

            var ocrNumberParser = NEnv.BaseOcrNumberParser;
            var incomingPaymentAccountNr = requestContext.Service().PaymentAccount.GetIncomingPaymentBankAccountNr();

            var nextNotificationDate = GetNextNotificationDate(today);            

            using (var context = new CreditContextExtended(requestContext.CurrentUserMetadata(), CoreClock.SharedInstance))
            {
                var credits = LoadCredits(request, today, ocrNumberParser, incomingPaymentAccountNr, nextNotificationDate, context, requestContext.Service().PaymentOrder);

                var activeCredits = credits.Where(x => x.Status == CreditStatus.Normal.ToString()).ToList();
                var inactiveCredits = credits.Where(x => x.Status != CreditStatus.Normal.ToString()).ToList();

                if (request.IncludeInactiveLoans ?? false)
                {
                    return new Response
                    {
                        ActiveCredits = activeCredits,
                        InactiveCredits = inactiveCredits
                    };
                }

                else
                {
                    return new Response
                    {
                        ActiveCredits = activeCredits
                    };
                }
            }
        }

        private static List<Response.CreditModel> LoadCredits(Request request, DateTime today, OcrNumberParser ocrNumberParser, IBankAccountNumber incomingPaymnentBankAccountNr, 
            DateTime? nextNotificationDate, CreditContextExtended context, PaymentOrderService paymentOrderService)
        {
            var dbCredits = context
                .CreditHeaders
                .Where(x => x.CreditCustomers.Any(y =>
                    y.CustomerId == request.CustomerId.Value))
                .Select(x => new
                {
                    x.CreatedByEvent,
                    x.CreditNr,
                    x.Status,
                    x.Notifications,
                    x.Reminders,
                    ApplicantNr = x
                        .CreditCustomers
                        .Where(y => y.CustomerId == request.CustomerId.Value)
                        .Select(y => y.ApplicantNr)
                        .FirstOrDefault(),
                    Customers = x.CreditCustomers.Select(y => new { y.ApplicantNr, y.CustomerId }),
                    CollateralId = x.CollateralHeaderId,
                    CollateralItems = x
                        .Collateral
                        .Items
                        .Where(y => !y.RemovedByBusinessEventId.HasValue && SharedCollateralItemNames.Contains(y.ItemName))
                        .GroupBy(y => y.ItemName)
                        .Select(y => y.OrderByDescending(z => z.Id).FirstOrDefault()),
                    EndDate = x
                        .DatedCreditStrings
                        .Where(str => str.CreditNr == x.CreditNr && x.Status != CreditStatus.Normal.ToString() && str.Name == "CreditStatus")
                        .OrderByDescending(z => z.TransactionDate)
                        .Select(d => (DateTime?)d.TransactionDate).FirstOrDefault()
                })
                .Select(x => new
                {
                    CreatedByEventId = x.CreatedByEvent.Id,
                    StartDate = x.CreatedByEvent.TransactionDate,
                    NotificationDocuments = x.Notifications.Select(y => new { NotificationId = y.Id, y.PdfArchiveKey }),
                    Reminders = x.Reminders.Select(y => new
                    {
                        y.ReminderNumber,
                        y.NotificationId,
                        y.ReminderDate,
                        ArchiveKey = y.Documents.Where(z => z.ApplicantNr == x.ApplicantNr).Select(z => z.ArchiveKey).FirstOrDefault()
                    }),
                    x.CreditNr,
                    x.Status,
                    x.EndDate,
                    x.Customers,
                    x.CollateralId,
                    x.CollateralItems
                })
                .ToList();

            var creditDomainModelsByCreditNr =
                CreditDomainModel.PreFetchForCredits(context, dbCredits.Select(x => x.CreditNr).ToArray(), NEnv.EnvSettings);

            var notificationDomainModelsByCreditNr =
                CreditNotificationDomainModel.CreateForSeveralCredits(dbCredits.Select(x => x.CreditNr).ToHashSet(), context, paymentOrderService.GetPaymentOrderItems(), onlyFetchOpen: false);

            var activeCredits = dbCredits.OrderBy(x => x.CreatedByEventId).Select(dbCredit =>
            {
                var creditDomainModel = creditDomainModelsByCreditNr[dbCredit.CreditNr];

                var pdfArchiveKeyByNotificationId = dbCredit
                    .NotificationDocuments
                    .ToDictionary(y => y.NotificationId, y => y.PdfArchiveKey);

                var remindersByNotificationId = dbCredit
                    .Reminders
                    .GroupBy(y => y.NotificationId)
                    .ToDictionary(y => y.Key, y => y);

                var notificationDomainModels = (notificationDomainModelsByCreditNr
                        .Opt(dbCredit.CreditNr)
                        ?.Select(y => y.Value)
                        ?.ToList() ?? new List<CreditNotificationDomainModel>());

                var notifications = notificationDomainModels
                    .Select(y => new Response.NotificationModel
                    {
                        NotificationId = y.NotificationId,
                        NotificationDate = y.NotificationDate,
                        IsOpen = y.IsOpen(today),
                        ClosedDate = y.GetClosedDate(today),
                        DueDate = y.DueDate,
                        IsOverdue = today > y.DueDate,
                        InitialAmount = y.GetInitialAmount(today),
                        BalanceAmount = y.GetRemainingBalance(today),
                        PaymentBankGiroNr = incomingPaymnentBankAccountNr.FormatFor(null),
                        PaymentBankGiroNrDisplay = incomingPaymnentBankAccountNr.FormatFor("display"),
                        OcrPaymentReference = ocrNumberParser.Parse(y.OcrPaymentReference).NormalForm,
                        OcrPaymentReferenceDisplay = ocrNumberParser.Parse(y.OcrPaymentReference).DisplayForm,
                        PdfArchiveKey = pdfArchiveKeyByNotificationId.Opt(y.NotificationId),
                        Reminders = remindersByNotificationId.Opt(y.NotificationId)?.Select(z => new Response.ReminderModel
                        {
                            ReminderNumber = z.ReminderNumber,
                            ReminderDate = z.ReminderDate,
                            ArchiveKey = z.ArchiveKey
                        }).ToList()
                    })
                    .ToList();

                Response.MortgageLoanSpecifics mortgageLoanSpecifics = null;
                Response.UnsecuredLoanSpecifics unsecuredLoanSpecifics = null;

                if (creditDomainModel.GetCreditType() == CreditType.MortgageLoan)
                {
                    var amortizationExceptionUntilDate = creditDomainModel.GetDatedCreditDate(today, DatedCreditDateCode.AmortizationExceptionUntilDate, null);
                    var monthlyAmortizationAmount = creditDomainModel.GetDatedCreditValueOpt(today, DatedCreditValueCode.MonthlyAmortizationAmount);
                    var exceptionAmortizationAmount = creditDomainModel.GetDatedCreditValueOpt(today, DatedCreditValueCode.ExceptionAmortizationAmount);
                    var amortizationExceptionReasons = creditDomainModel.GetAmortizationExceptionReasons(today);
                    var isAmortizationExceptionActive = exceptionAmortizationAmount.HasValue && amortizationExceptionUntilDate.HasValue
                        && amortizationExceptionUntilDate.Value >= today;

                    mortgageLoanSpecifics = new Response.MortgageLoanSpecifics
                    {
                        MortgageLoanInterestRebindMonthCount = (int?)creditDomainModel.GetDatedCreditValueOpt(today, DatedCreditValueCode.MortgageLoanInterestRebindMonthCount),
                        MortgageLoanNextInterestRebindDate = creditDomainModel.GetDatedCreditDate(today, DatedCreditDateCode.MortgageLoanNextInterestRebindDate, null),
                        CollateralId = dbCredit.CollateralId.Value,
                        CollateralStringItems = dbCredit.CollateralItems.ToDictionary(x => x.ItemName, x => x.StringValue)
                    };
                }
                if (creditDomainModel.GetCreditType() == CreditType.UnsecuredLoan)
                {
                    /*
                     The second branch of this - when using a fixed amortization - does not really have a well defined 
                     monthly amount. Now we just show the amortization but maybe the estimated interest of the next notification should be added.
                     */
                    var monthlyPaymentExcludingFee = creditDomainModel.GetAmortizationModel(today)
                        .UsingCurrentAnnuityOrFixedMonthlyCapital(today, x => x, x => x);
                    unsecuredLoanSpecifics = new Response.UnsecuredLoanSpecifics
                    {
                        MonthlyPaymentExcludingFee = monthlyPaymentExcludingFee,
                        MonthlyPaymentIncludingFee = monthlyPaymentExcludingFee + creditDomainModel.GetNotificationFee(today),
                    };
                }
                var singlePaymentLoanRepaymentDays = creditDomainModel.GetSinglePaymentLoanRepaymentDays();
                return new
                {
                    Customers = dbCredit.Customers.OrderBy(x => x.ApplicantNr).ToList(),
                    Model = new Response.CreditModel
                    {
                        CreditNr = dbCredit.CreditNr,
                        StartDate = dbCredit.StartDate,
                        Status = dbCredit.Status,
                        EndDate = dbCredit.EndDate,
                        CapitalBalance = creditDomainModel.GetBalance(CreditDomainModel.AmountType.Capital, today),
                        IsDirectDebitActive = creditDomainModel.GetIsDirectDebitActive(today),
                        Notifications = notifications,
                        NextNotificationDate = singlePaymentLoanRepaymentDays.HasValue 
                            ? (notifications.Any() ? null : new DateTime?(today))
                            : nextNotificationDate,
                        CurrentInterestRatePercent = creditDomainModel.GetInterestRatePercent(today),
                        MortgageLoan = mortgageLoanSpecifics,
                        UnsecuredLoan = unsecuredLoanSpecifics,
                        SinglePaymentLoanRepaymentDays = singlePaymentLoanRepaymentDays
                    }
                };
            })
            .ToList();

            if (request.IncludeCustomerPersonalData.GetValueOrDefault())
            {
                var allCustomerIds = activeCredits.SelectMany(x => x.Customers.Select(y => y.CustomerId)).ToHashSet();
                var customerData = new CreditCustomerClient().BulkFetchPropertiesByCustomerIdsD(allCustomerIds, "firstName", "lastName");
                foreach (var credit in activeCredits)
                {
                    credit.Model.ApplicantsPersonalData = credit.Customers.Select(x => new Response.ApplicantPersonalDataModel
                    {
                        ApplicantNr = x.ApplicantNr,
                        FirstName = customerData?.Opt(x.CustomerId)?.Opt("firstName"),
                        LastName = customerData?.Opt(x.CustomerId)?.Opt("lastName")
                    }).ToList();
                }
            }

            return activeCredits.Select(x => x.Model).ToList();
        }

        private DateTime? GetNextNotificationDate(DateTime today)
        {
            DateTime? nextNotificationDate = null;
            //NOTE: We could probably support per loan due dates also. You would need to compute the next notificaiton date
            //      by running NotificationService.GetNotificationDueDateOrSkipReason for each date starting from today until 
            //      it suggests sending a notification
            if (!NEnv.HasPerLoanDueDay)
            {
                var settings = NEnv.NotificationProcessSettings.GetByCreditType(NEnv.ClientCreditType);
                //today - 1 since we want it to pick up if there is a notification today
                nextNotificationDate = Dates.GetNextDateWithDayNrAfterDate(settings.NotificationNotificationDay, today.AddDays(-1));
            }

            return nextNotificationDate;
        }

        private static HashSet<string> SharedCollateralItemNames = new HashSet<string>
        {
            "objectTypeCode", "objectAddressMunicipality", "seBrfName", "seBrfApartmentNr", "objectId"
        };

        public class Request
        {
            [Required]
            public int? CustomerId { get; set; }

            public bool? IncludeCustomerPersonalData { get; set; }
            public bool? IncludeInactiveLoans { get; set; }
        }

        public class Response
        {
            public List<CreditModel> ActiveCredits { get; set; }
            public List<CreditModel> InactiveCredits { get; set; }

            public class CreditModel
            {
                public string CreditNr { get; set; }
                public DateTime StartDate { get; set; }
                public string Status { get; set; }
                public DateTime? EndDate { get; set; }
                public decimal CapitalBalance { get; set; }
                public decimal CurrentInterestRatePercent { get; set; }
                public bool IsDirectDebitActive { get; set; }
                public List<NotificationModel> Notifications { get; set; }
                public DateTime? NextNotificationDate { get; set; }
                public List<ApplicantPersonalDataModel> ApplicantsPersonalData { get; set; }
                public MortgageLoanSpecifics MortgageLoan { get; set; }
                public UnsecuredLoanSpecifics UnsecuredLoan { get; set; }
                public int? SinglePaymentLoanRepaymentDays { get; set; }
            }

            public class MortgageLoanSpecifics
            {
                public DateTime? MortgageLoanNextInterestRebindDate { get; set; }
                public int? MortgageLoanInterestRebindMonthCount { get; set; }
                public int CollateralId { get; set; }
                public Dictionary<string, string> CollateralStringItems { get; set; }
            }

            public class UnsecuredLoanSpecifics
            {
                public decimal MonthlyPaymentExcludingFee { get; set; }
                public decimal MonthlyPaymentIncludingFee { get; set; }
            }

            public class ReminderModel
            {
                public int ReminderNumber { get; set; }
                public DateTime ReminderDate { get; set; }
                public string ArchiveKey { get; set; }
            }

            public class NotificationModel
            {
                public int NotificationId { get; set; }
                public DateTime NotificationDate { get; set; }
                public bool IsOpen { get; set; }
                public DateTime? ClosedDate { get; set; }
                public DateTime DueDate { get; set; }
                public bool IsOverdue { get; set; }
                public decimal InitialAmount { get; set; }
                public decimal BalanceAmount { get; set; }
                public string PaymentBankGiroNr { get; set; }
                public string PaymentBankGiroNrDisplay { get; set; }
                public string OcrPaymentReference { get; set; }
                public string OcrPaymentReferenceDisplay { get; set; }
                public string PdfArchiveKey { get; set; }
                public List<ReminderModel> Reminders { get; set; }
            }

            public class ApplicantPersonalDataModel
            {
                public int ApplicantNr { get; set; }
                public string FirstName { get; set; }
                public string LastName { get; set; }
            }
        }
    }
}