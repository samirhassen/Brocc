using nCredit.Code;
using nCredit.DomainModel;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure.Email;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class CreditSettlementSuggestionBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly ICreditEnvSettings envSettings;
        private readonly PaymentAccountService paymentAccountService;
        private readonly CreditContextFactory creditContextFactory;
        private readonly ILoggingService loggingService;
        private readonly SwedishMortgageLoanRseService rseService;
        private readonly PaymentOrderService paymentOrderService;
        private readonly INTechEmailServiceFactory emailServiceFactory;
        private readonly ICustomerClient customerClient;

        public CreditSettlementSuggestionBusinessEventManager(INTechCurrentUserMetadata currentUser, ICoreClock clock, IClientConfigurationCore clientConfiguration, ICreditEnvSettings envSettings,
            PaymentAccountService paymentAccountService, CreditContextFactory creditContextFactory, ILoggingService loggingService,
            SwedishMortgageLoanRseService rseService, PaymentOrderService paymentOrderService, INTechEmailServiceFactory emailServiceFactory, ICustomerClient customerClient)
            : base(currentUser, clock, clientConfiguration)
        {
            this.envSettings = envSettings;
            this.paymentAccountService = paymentAccountService;
            this.creditContextFactory = creditContextFactory;
            this.loggingService = loggingService;
            this.rseService = rseService;
            this.paymentOrderService = paymentOrderService;
            this.emailServiceFactory = emailServiceFactory;
            this.customerClient = customerClient;
        }

        public PendingCreditSettlementSuggestionData GetPendingSettlementIfAny(string creditNr, ICreditContextExtended context)
        {
            var result = context
                .CreditSettlementOfferHeadersQueryable
                .Where(x => x.CreditNr == creditNr && !x.CommitedByEventId.HasValue && !x.CancelledByEventId.HasValue)
                .OrderByDescending(x => x.Timestamp)
                .Select(x => new { Value = x, x.Items }) //NOTE: This we do this instead of Include("Items") since code shared between ef legacy and core cannot use Include
                .FirstOrDefault();

            var h = result?.Value;

            if (h == null)
                return null;
            var settlementAmount = decimal.Parse(h.Items.Single(x => x.Name == CreditSettlementOfferItem.CreditSettlementOfferItemCode.SettlementAmount.ToString()).Value, NumberStyles.Number, CultureInfo.InvariantCulture);
            var settlementDate = h.ExpectedSettlementDate;

            return new PendingCreditSettlementSuggestionData
            {
                id = h.Id,
                creditNr = h.CreditNr,
                autoExpireDate = h.AutoExpireDate,
                settlementAmount = settlementAmount,
                settlementDate = settlementDate
            };
        }

        private decimal CalculateSwedishRse(string creditNr, decimal comparisonInterestRatePercent)
        {
            var rseResult = rseService.CalculateRseForCredit(new RseForCreditRequest
            {
                CreditNr = creditNr,
                ComparisonInterestRatePercent = comparisonInterestRatePercent
            });
            return rseResult.HasRse ? rseResult.Rse.RseAmount : 0m;
        }

        public CreateAndSendCreditSettlementResponse CreateAndSendSuggestion(CreateAndSendCreditSettlementRequest request)
        {
            PendingCreditSettlementSuggestionData offer;
            if (!TryCreateAndSendSettlementSuggestion(request.CreditNr, request.SettlementDate, request.SwedishRseEstimatedAmount, request.SwedishRseInterestRatePercent, out var warningMessage, out offer,
                request.Email))
                throw new NTechCoreWebserviceException(warningMessage) {  IsUserFacing = true, ErrorHttpStatusCode = 400 };
            else
            {
                return new CreateAndSendCreditSettlementResponse
                { 
                    PendingOffer = offer,
                    UserWarningMessage = warningMessage
                };
            }
        }        

        public ComputeCreditSuggestionResponse ComputeSuggestion(ComputeCreditSuggestionRequest request)
        {
            string failedMessage;
            CreditSettlementSuggestionData suggestion;
            if (!TryComputeSettlementSuggestion(request.CreditNr, request.SettlementDate, out failedMessage, out suggestion, swedishRseInterestRatePercent: request.SwedishRseInterestRatePercent))
                throw new NTechCoreWebserviceException(failedMessage) { IsUserFacing = true, ErrorHttpStatusCode = 400 };
            else
                return new ComputeCreditSuggestionResponse
                {
                    Suggestion = suggestion
                };
        }

        public CancelCreditSettlementSuggestionResponse CancelPendingSuggestion(CancelCreditSettlementSuggestionRequest request)
        {
            string failedMessage;
            using (var context = creditContextFactory.CreateContext())
            {
                var isOk = TryCancelSettlementSuggestion(context, request.Id, true, out failedMessage, out var _);

                context.SaveChanges();

                if (!isOk)
                    throw new NTechCoreWebserviceException(failedMessage) { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                else
                    return new CancelCreditSettlementSuggestionResponse { };
            }
        }

        public CreditSettlementFetchInitialDataResponse FetchInitialData(CreditSettlementFetchInitialDataRequest request)
        {            
            HistoricalCreditModel model;
            string notificationEmail = null;
            var hasEmailProvider = emailServiceFactory.HasEmailProvider;

            using (var context = creditContextFactory.CreateContext())
            {
                model = AmortizationPlan.GetHistoricalCreditModel(request.CreditNr, context, envSettings.IsMortgageLoansEnabled);
                var pendingOffer = GetPendingSettlementIfAny(request.CreditNr, context);
                if (hasEmailProvider && request.IncludeNotificationEmail == true)
                {
                    var credit = context
                       .CreditHeadersQueryable
                       .Where(x => x.CreditNr == request.CreditNr)
                       .Select(x => new
                       {
                           CustomerId = x.CreditCustomers.Where(y => y.ApplicantNr == 1).Select(y => y.CustomerId).FirstOrDefault(),
                           x.CreditType
                       })
                       .Single();

                    notificationEmail = customerClient.BulkFetchPropertiesByCustomerIdsD(new HashSet<int> { credit.CustomerId }, "email")?.Opt(credit.CustomerId)?.Opt("email");
                }
                return new CreditSettlementFetchInitialDataResponse
                {
                    CreditNr = request.CreditNr,
                    CreditStatus = model.Status,
                    PendingOffer = pendingOffer,
                    NotificationEmail = notificationEmail,
                    HasEmailProvider = hasEmailProvider
                };
            }
        }

        public bool TryComputeSettlementSuggestion(string creditNr, DateTime? settlementDate, out string failedMessage, out CreditSettlementSuggestionData suggestion, bool ignoreExistingOffer = false,
            decimal? swedishRseInterestRatePercent = null,
            decimal? forceSwedishRseAmount = null)
        {
            if (!settlementDate.HasValue)
            {
                failedMessage = "Missing settlementDate";
                suggestion = null;
                return false;
            }

            if (string.IsNullOrWhiteSpace(creditNr))
            {
                failedMessage = "Missing creditNr";
                suggestion = null;
                return false;
            }

            using (var context = creditContextFactory.CreateContext())
            {
                if (!ignoreExistingOffer && context.CreditSettlementOfferHeadersQueryable.Any(x => x.CreditNr == creditNr && !x.CancelledByEventId.HasValue && !x.CommitedByEventId.HasValue))
                {
                    failedMessage = "There is already an active settlement offer. Cancel that first.";
                    suggestion = null;
                    return false;
                }
                var credit = CreditPaymentPlacementModel.LoadSingle(creditNr, context, envSettings, ClientCfg, paymentOrderService);
                if (credit.GetCreditStatus() != CreditStatus.Normal)
                {
                    failedMessage = $"Credit has status: {credit.GetCreditStatus()}";
                    suggestion = null;
                    return false;
                }

                var totalSettlementBalance = 0m;

                var notifiedCapitalBalance = credit
                    .GetOpenNotifications()
                    .Aggregate(0m, (acc, x) => acc + x.GetRemainingBalance(CreditDomainModel.AmountType.Capital));
                var notNotifiedCapitalBalance = credit.GetNotNotifiedCapitalBalance();
                totalSettlementBalance += (notifiedCapitalBalance + notNotifiedCapitalBalance);

                var notifiedInterestBalance = credit
                    .GetOpenNotifications()
                    .Aggregate(0m, (acc, x) => acc + x.GetRemainingBalance(CreditDomainModel.AmountType.Interest));
                int nrOfInterestDays;
                var notNotifiedInterestBalance = credit.ComputeNotNotifiedInterestUntil(settlementDate.Value, out nrOfInterestDays);
                totalSettlementBalance += (notifiedInterestBalance + notNotifiedInterestBalance);

                decimal partOfNotifiedInterestBalanceThatIsAfterSettlementDate = 0;
                var futureNotificationDueDate = credit.GetOpenNotifications().OrderByDescending(x => x.DueDate > settlementDate).Max(x => (DateTime?)x.DueDate);
                if (futureNotificationDueDate.HasValue)
                {
                    var futureInterestAmount = credit.ComputeInterestAmountIgnoringInterestFromDate(Clock.Today, Tuple.Create(settlementDate.Value.AddDays(1), futureNotificationDueDate.Value));

                    //Min here used to handle the case if they already paid the interst. Then we cant handle it this way
                    partOfNotifiedInterestBalanceThatIsAfterSettlementDate = Math.Min(futureInterestAmount, notifiedInterestBalance);

                    totalSettlementBalance -= partOfNotifiedInterestBalanceThatIsAfterSettlementDate;
                }

                var otherPaymentOrderItems = paymentOrderService
                    .GetPaymentOrderItems()
                    .Where(x => !x.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.Capital) && !x.IsCreditDomainModelAmountType(CreditDomainModel.AmountType.Interest))
                    .ToList();

                var notifiedOtherBalance = credit
                    .GetOpenNotifications()
                    .SelectMany(x => otherPaymentOrderItems.Select(y => x.GetRemainingBalance(y)))
                    .Aggregate(0m, (acc, x) => acc + x);
                totalSettlementBalance += notifiedOtherBalance;

                decimal? swedishRseEstimatedAmount = null;
                if (envSettings.IsStandardMortgageLoansEnabled && ClientCfg.Country.BaseCountry == "SE" && (swedishRseInterestRatePercent.HasValue || forceSwedishRseAmount.HasValue))
                {
                    swedishRseEstimatedAmount = forceSwedishRseAmount ?? CalculateSwedishRse(creditNr, swedishRseInterestRatePercent.Value);
                    totalSettlementBalance += swedishRseEstimatedAmount.Value;
                }

                var ocrPaymentReference = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, envSettings).GetOcrPaymentReference(Clock.Today);

                failedMessage = null;
                suggestion = new CreditSettlementSuggestionData
                {
                    creditNr = creditNr,
                    ocrPaymentReference = ocrPaymentReference,
                    settlementDate = settlementDate.Value,
                    notifiedCapitalBalance = notifiedCapitalBalance,
                    notNotifiedCapitalBalance = notNotifiedCapitalBalance,
                    totalCapitalBalance = notifiedCapitalBalance + notNotifiedCapitalBalance,
                    notifiedInterestBalance = notifiedInterestBalance,
                    notNotifiedInterestBalance = notNotifiedInterestBalance,
                    partOfNotifiedInterestBalanceThatIsAfterSettlementDate = partOfNotifiedInterestBalanceThatIsAfterSettlementDate,
                    nrOfInterestDaysInNotNotifiedInterestBalance = nrOfInterestDays,
                    totalInterestBalance = notifiedInterestBalance + notNotifiedInterestBalance - partOfNotifiedInterestBalanceThatIsAfterSettlementDate,
                    notifiedOtherBalance = notifiedOtherBalance,
                    swedishRse = swedishRseEstimatedAmount.HasValue ? new CreditSettlementSuggestionSwedishRseData
                    {
                        estimatedAmount = swedishRseEstimatedAmount,
                        interestRatePercent = swedishRseInterestRatePercent,
                    } : null,
                    totalOtherBalance = notifiedOtherBalance,
                    totalSettlementBalance = totalSettlementBalance,
                    willSendSuggestion = true
                };

                return true;
            }
        }

        public bool TryCreateAndSendSettlementSuggestion(string creditNr,
            DateTime? settlementDate, decimal? swedishRseEstimatedAmount, decimal? swedishRseInterestRatePercent,
            out string warningMessage, out PendingCreditSettlementSuggestionData offer, string notificationEmail)
        {
            var didEmailFail = false;

            if (!TryComputeSettlementSuggestion(creditNr, settlementDate, out warningMessage, out var suggestion,
                swedishRseInterestRatePercent: swedishRseInterestRatePercent,
                forceSwedishRseAmount: swedishRseEstimatedAmount))
            {
                offer = null;
                return false;
            }

            using (var context = creditContextFactory.CreateContext())
            {
                var credit = context
                    .CreditHeadersQueryable
                    .Where(x => x.CreditNr == creditNr)
                    .Select(x => new
                    {
                        CustomerId = x.CreditCustomers.Where(y => y.ApplicantNr == 1).Select(y => y.CustomerId).FirstOrDefault(),
                        x.CreditType
                    })
                    .Single();

                var evt = AddBusinessEvent(BusinessEventType.StartedCreditSettlementOffer, context);
                var isMortgageLoan = credit.CreditType == CreditType.MortgageLoan.ToString();

                var h = new CreditSettlementOfferHeader
                {
                    CreatedByEvent = evt,
                    ChangedDate = Clock.Now,
                    ChangedById = this.UserId,
                    CreditNr = creditNr,
                    InformationMetaData = this.InformationMetadata,
                    ExpectedSettlementDate = suggestion.settlementDate,
                    AutoExpireDate = isMortgageLoan
                        ? new DateTime?()
                        : suggestion.settlementDate.AddDays(envSettings.CreditSettlementOfferGraceDays ?? 10)
                };
                context.AddCreditSettlementOfferHeaders(h);

                void AddOfferItem(CreditSettlementOfferItem.CreditSettlementOfferItemCode code, string value)
                {
                    context.AddCreditSettlementOfferItems(new CreditSettlementOfferItem
                    {
                        ChangedById = UserId,
                        ChangedDate = Clock.Now,
                        CreatedByEvent = evt,
                        CreditSettlementOffer = h,
                        InformationMetaData = InformationMetadata,
                        Name = code.ToString(),
                        Value = value
                    });
                }

                AddOfferItem(CreditSettlementOfferItem.CreditSettlementOfferItemCode.SettlementAmount, suggestion.totalSettlementBalance.ToString(CultureInfo.InvariantCulture));
                if (swedishRseEstimatedAmount.HasValue)
                {
                    AddOfferItem(CreditSettlementOfferItem.CreditSettlementOfferItemCode.SwedishRseEstimatedAmount, swedishRseEstimatedAmount.Value.ToString(CultureInfo.InvariantCulture));
                }
                if (swedishRseInterestRatePercent.HasValue)
                {
                    AddOfferItem(CreditSettlementOfferItem.CreditSettlementOfferItemCode.SwedishRseInterestRatePercent, swedishRseInterestRatePercent.Value.ToString(CultureInfo.InvariantCulture));
                }

                string emailComment;
                if (!string.IsNullOrWhiteSpace(notificationEmail) && emailServiceFactory.HasEmailProvider)
                {
                    var incomingPaymentBankAccountNr = paymentAccountService.GetIncomingPaymentBankAccountNr();
                    try
                    {
                        var emailContext = new Dictionary<string, string>
                            {
                                { "settlementDate", suggestion.settlementDate.ToString("d", PrintFormattingCulture) },
                                { "settlementAmount", suggestion.totalSettlementBalance.ToString("C", PrintFormattingCulture) },
                                { "payToOcrPaymentReference", OcrNumberParser.Parse(suggestion.ocrPaymentReference, ClientCfg.Country.BaseCountry).DisplayForm },
                                { "payToIban", paymentAccountService.FormatIncomingBankAccountNrForDisplay(incomingPaymentBankAccountNr) }
                            };
                        if (swedishRseEstimatedAmount.HasValue)
                        {
                            emailContext["swedishRseEstimatedAmount"] = swedishRseEstimatedAmount.Value.ToString("C", PrintFormattingCulture);
                        }
                        emailServiceFactory.CreateEmailService().SendTemplateEmail(
                            new List<string>() { notificationEmail },
                            "credit-settlement-suggestion",
                            emailContext,
                            BusinessEventType.StartedCreditSettlementOffer.ToString());

                        emailComment = " and emailed it to the customer.";
                    }
                    catch (Exception ex)
                    {
                        loggingService.Warning(ex, "Could not send email on settlement suggestion");
                        emailComment = " but could not email it to the customer."; //NOTE: We save anyway here since they are likely to have the customer on the phone and we dont want it to be impossible to save the offer just because the customers email doesn't work
                        didEmailFail = true;
                    }
                }
                else
                    emailComment = null;

                AddComment(string.Format(CommentFormattingCulture, "Created settlement offer for {0:C} if payment arrives by {1:d} {2}", suggestion.totalSettlementBalance, suggestion.settlementDate, emailComment), BusinessEventType.StartedCreditSettlementOffer, context, creditNr: creditNr);

                context.SaveChanges();

                offer = new PendingCreditSettlementSuggestionData
                {
                    id = h.Id,
                    creditNr = h.CreditNr,
                    autoExpireDate = h.AutoExpireDate,
                    settlementAmount = suggestion.totalSettlementBalance,
                    settlementDate = suggestion.settlementDate
                };

                warningMessage = didEmailFail
                    ? "Settlement suggestion created but email could not be sent."
                    : null;

                return true;
            }
        }

        public bool TryCancelSettlementSuggestion(ICreditContextExtended context, int? id, bool isManual, out string failedMessage, out string creditNr)
        {
            if (!id.HasValue)
            {
                failedMessage = "id missing";
                creditNr = null;
                return false;
            }
            var h = context.CreditSettlementOfferHeadersQueryable.SingleOrDefault(x => x.Id == id.Value);

            if (h == null)
            {
                failedMessage = "no such settlement offer exists";
                creditNr = null;
                return false;
            }

            if (h.CancelledByEventId.HasValue || h.CommitedByEventId.HasValue)
            {
                failedMessage = "settlement offer is not active";
                creditNr = null;
                return false;
            }

            CancelSettlementSuggestion(context, h, isManual);

            creditNr = h.CreditNr;
            failedMessage = null;

            return true;
        }

        public void AutoCancelOldPendingExpired()
        {
            var today = Clock.Today.Date;
            using (var context = creditContextFactory.CreateContext())
            {
                var expiredOffers = context
                    .CreditSettlementOfferHeadersQueryable
                    .Where(x =>
                        !x.CommitedByEventId.HasValue
                        && !x.CancelledByEventId.HasValue
                        && (x.AutoExpireDate.HasValue && x.AutoExpireDate.Value <= today))
                    .ToList();
                foreach (var offer in expiredOffers)
                {
                    CancelSettlementSuggestion(context, offer, false);
                }
                context.SaveChanges();
            }
        }

        private void CancelSettlementSuggestion(ICreditContextExtended context, CreditSettlementOfferHeader h, bool isManual)
        {
            var evt = AddBusinessEvent(BusinessEventType.CancelledCreditSettlementOffer, context);

            h.CancelledByEvent = evt;
            h.ChangedById = UserId;
            h.ChangedDate = Clock.Now;

            AddComment($"Cancelled settlement offer {(isManual ? "manually" : "automatically")}", BusinessEventType.CancelledCreditSettlementOffer, context, creditNr: h.CreditNr);
        }
    }

    public class ComputeCreditSuggestionRequest
    {
        [Required]
        public string CreditNr { get; set; }
        [Required]
        public DateTime? SettlementDate { get; set; }
        public decimal? SwedishRseInterestRatePercent { get; set; }
    }

    public class ComputeCreditSuggestionResponse
    {
        public CreditSettlementSuggestionData Suggestion { get; set; }
    }

    public class CreditSettlementSuggestionData
    {
        public string creditNr { get; set; }
        public string ocrPaymentReference { get; set; }
        public DateTime settlementDate { get; set; }
        public decimal notifiedCapitalBalance { get; set; }
        public decimal notNotifiedCapitalBalance { get; set; }
        public decimal totalCapitalBalance { get; set; }
        public decimal notifiedInterestBalance { get; set; }
        public decimal notNotifiedInterestBalance { get; set; }
        public decimal partOfNotifiedInterestBalanceThatIsAfterSettlementDate { get; set; }
        public int nrOfInterestDaysInNotNotifiedInterestBalance { get; set; }
        public decimal totalInterestBalance { get; set; }
        public decimal notifiedOtherBalance { get; set; }
        public decimal totalOtherBalance { get; set; }
        public decimal totalSettlementBalance { get; set; }
        public bool willSendSuggestion { get; set; }
        public CreditSettlementSuggestionSwedishRseData swedishRse { get; set; }
    }

    public class PendingCreditSettlementSuggestionData
    {
        public int id { get; set; }
        public string creditNr { get; set; }
        public decimal settlementAmount { get; set; }
        public DateTime settlementDate { get; set; }
        public DateTime? autoExpireDate { get; set; }
    }

    public class CreditSettlementSuggestionSwedishRseData
    {
        public decimal? estimatedAmount { get; set; }
        public decimal? interestRatePercent { get; set; }
    }

    public class CancelCreditSettlementSuggestionRequest
    {
        [Required]
        public int Id { get; set; }
    }

    public class CancelCreditSettlementSuggestionResponse
    {

    }

    public class CreditSettlementFetchInitialDataRequest
    {
        [Required]
        public string CreditNr { get; set; }
        public bool? IncludeNotificationEmail { get; set; }
    }

    public class CreditSettlementFetchInitialDataResponse
    {
        public string CreditNr { get; set; }
        public string CreditStatus { get; set; }
        public PendingCreditSettlementSuggestionData PendingOffer { get; set; }
        public string NotificationEmail { get; set; }
        public bool HasEmailProvider { get; set; }
    }
    public class CreateAndSendCreditSettlementRequest
    {
        [Required]
        public string CreditNr { get; set; }
        [Required]
        public DateTime? SettlementDate { get; set; }
        public string Email { get; set; }
        public decimal? SwedishRseEstimatedAmount { get; set; }
        public decimal? SwedishRseInterestRatePercent { get; set; }
    }

    public class CreateAndSendCreditSettlementResponse
    {
        public PendingCreditSettlementSuggestionData PendingOffer { get; set; }
        public string UserWarningMessage { get; set; }
    }
}