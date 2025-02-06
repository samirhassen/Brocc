using nCredit.Code;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using Newtonsoft.Json.Serialization;
using NTech.Banking.LoanModel;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiCreditDetailsController : NController
    {
        public class SearchCreditRequest
        {
            public string CivicRegNr { get; set; }
            public string CreditNr { get; set; }
        }

        [HttpGet]
        [Route("Api/Credit/ApplicationLink")]
        public ActionResult ApplicationLink(string creditNr, string applicationNr)
        {
            if (!NEnv.ClientCfgCore.IsFeatureEnabled("ntech.feature.precredit"))
            {
                return HttpNotFound();
            }

            if (applicationNr == null)
            {
                var c = new PreCreditClient();
                var nrs = c.GetApplicationNrsByCreditNrs(new HashSet<string> { creditNr });
                if (nrs.ContainsKey(creditNr))
                    applicationNr = nrs[creditNr];
            }

            if (applicationNr == null)
                return HttpNotFound();

            using (var context = CreateCreditContext())
            {
                var creditModel = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, NEnv.EnvSettings);

                var creditType = creditModel.GetCreditType();

                if (creditType == CreditType.MortgageLoan)
                {
                    return Redirect(new Uri(new Uri(NEnv.ServiceRegistry.External["nPreCredit"]), $"Ui/MortgageLoan/Application?applicationNr={applicationNr}").ToString());
                }
                else if (creditType == CreditType.UnsecuredLoan)
                {
                    return Redirect(new Uri(new Uri(NEnv.ServiceRegistry.External["nPreCredit"]), $"CreditManagement/CreditApplication?applicationNr={applicationNr}").ToString());
                }
                else if (creditType == CreditType.CompanyLoan)
                {
                    return Redirect(new Uri(new Uri(NEnv.ServiceRegistry.External["nPreCredit"]), $"Ui/CompanyLoan/Application?applicationNr={applicationNr}").ToString());
                }
                else
                    throw new NotImplementedException();
            }

        }

        [HttpPost]
        [Route("Api/Credit/FetchAttentionStatus")]
        public ActionResult FetchAttentionStatus(string creditNr)
        {
            if (string.IsNullOrWhiteSpace(creditNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing creditNr");

            var status = Service.CreditAttentionStatus.GetAttentionCreditAttentionStatus(creditNr);

            var result = new JsonNetActionResult
            {
                Data = status
            };
            result.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            return result;
        }

        [HttpPost]
        [Route("Api/Credit/Details")]
        public ActionResult CreditDetails(string creditNr)
        {
            if (string.IsNullOrWhiteSpace(creditNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing creditNr");
            using (var context = CreateCreditContext())
            {
                var model = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, NEnv.EnvSettings);
                var date = Clock.Today;

                string childCreditCreditNr = context
                    .DatedCreditStrings
                    .Where(x => x.Name == DatedCreditStringCode.MainCreditCreditNr.ToString() && x.Value == creditNr)
                    .Select(x => x.CreditNr)
                    .Distinct()
                    .SingleOrDefault();

                dynamic result = new ExpandoObject();

                DateTime? statusDate = null;
                var currentStatus = model.GetStatus(date, d => statusDate = d);
                int? currentNrOfOverdueDays = null;

                if (currentStatus == CreditStatus.Normal)
                {
                    var oldestOpenN = CurrentNotificationStateServiceLegacy
                        .GetCurrentOpenNotificationsStateQuery(context, date)
                        .Where(x => x.CreditNr == creditNr)
                        .OrderBy(x => x.DueDate)
                        .Select(x => new
                        {
                            x.DueDate,
                            x.NrOfPassedDueDatesWithoutFullPaymentSinceNotification
                        })
                        .FirstOrDefault();

                    currentNrOfOverdueDays = oldestOpenN != null && oldestOpenN.DueDate < Clock.Today ? (int?)Clock.Today.Subtract(oldestOpenN.DueDate).TotalDays : null;
                }

                decimal? totalSentToDebtCollectionAmount = null;
                int? debtCollectionExportNrOfOverdueDays = null;

                if (currentStatus == CreditStatus.SentToDebtCollection)
                {
                    var debtCollectionExportDate = statusDate.Value;

                    var debtCollectionInfo = context
                        .CreditHeaders
                        .Where(x => x.CreditNr == creditNr)
                        .Select(x => new
                        {
                            totalSentToDebtCollectionAmount = -x
                                .Transactions
                                .Where(y =>
                                    y.BusinessEvent.EventType == BusinessEventType.CreditDebtCollectionExport.ToString()
                                    && (
                                        //Written off notification
                                        (y.WriteoffId.HasValue && y.CreditNotificationId.HasValue && y.AccountCode != TransactionAccountType.NotNotifiedCapital.ToString())
                                        ||
                                        //Written off capital
                                        y.WriteoffId.HasValue && !y.CreditNotificationId.HasValue && y.AccountCode == TransactionAccountType.CapitalDebt.ToString()
                                        )
                                    )
                                .Sum(y => (decimal?)y.Amount) ?? 0m,
                            oldestOpenDueDateAtExport = x
                                .Notifications
                                .Where(y => y.ClosedTransactionDate == debtCollectionExportDate && y.Transactions.Any(z => z.BusinessEvent.EventType == BusinessEventType.CreditDebtCollectionExport.ToString() && z.TransactionDate == debtCollectionExportDate))
                                .OrderBy(y => y.DueDate)
                                .Select(y => (DateTime?)y.DueDate)
                                .FirstOrDefault()
                        })
                        .Single();

                    totalSentToDebtCollectionAmount = debtCollectionInfo.totalSentToDebtCollectionAmount;
                    if (debtCollectionInfo.oldestOpenDueDateAtExport.HasValue)
                        debtCollectionExportNrOfOverdueDays = (int)debtCollectionExportDate.Subtract(debtCollectionInfo.oldestOpenDueDateAtExport.Value).TotalDays;
                }

                Func<int?, string> getSignedAgreementLink = GetSignedAgreementLinkBuilder(context, model, date);

                var applicationNr = model.GetApplicationNr(date, allowMissing: true);
                var isMortgageLoan = model.GetCreditType() == CreditType.MortgageLoan;
                var isCompanyLoan = model.GetCreditType() == CreditType.CompanyLoan;

                int nrOfRemainingPayments;
                string repaymentTimeInMonthsFailedMessage = null;
                int? repaymentTimeInMonths = null;
                if (!isMortgageLoan)
                {
                    if (TryGetNrOfRemainingPayments(Clock.Today, NEnv.NotificationProcessSettings.GetByCreditType(model.GetCreditType()), model.GetAmortizationModel(date),
                        model.GetNotNotifiedCapitalBalance(date),
                        model.GetInterestRatePercent(date),
                        model.GetNotificationFee(date),
                        null,
                        out nrOfRemainingPayments,
                        out repaymentTimeInMonthsFailedMessage))
                    {
                        repaymentTimeInMonths = nrOfRemainingPayments;
                    }
                }
                else
                {
                    //TODO: Find a better way to do this
                    var hm = AmortizationPlan.GetHistoricalCreditModel(creditNr, context, NEnv.IsMortgageLoansEnabled);

                    if (AmortizationPlan.TryGetAmortizationPlan(hm, NEnv.NotificationProcessSettings.GetByCreditType(hm.GetCreditType()), out var amPlan, out var _,
                        CoreClock.SharedInstance, NEnv.ClientCfgCore,
                        CreditDomainModel.GetInterestDividerOverrideByCode(NEnv.ClientInterestModel)))
                    {
                        repaymentTimeInMonths = amPlan.NrOfRemainingPayments;
                    }
                }

                var providerName = model.GetProviderName();
                var sentToDebtCollectionDateOrNull = currentStatus == CreditStatus.SentToDebtCollection ? statusDate : null;

                var mortgageLoanInterestRebindMonthCount = model.GetDatedCreditValueOpt(date, DatedCreditValueCode.MortgageLoanInterestRebindMonthCount);
                var amortizationModel = model.GetAmortizationModel(date);
                var requestedMarginInterestRate = model.GetDatedCreditValueOpt(date, DatedCreditValueCode.RequestedMarginInterestRate);
                var marginInterestRate = model.GetDatedCreditValueOpt(date, DatedCreditValueCode.MarginInterestRate);
                var referenceInterestRate = model.GetDatedCreditValueOpt(date, DatedCreditValueCode.ReferenceInterestRate);
                var mortgageLoanAgreementNr = model.GetDatedCreditString(date, DatedCreditStringCode.MortgageLoanAgreementNr, null, allowMissing: true);

                result.details = new
                {
                    isMortgageLoan = isMortgageLoan,
                    isCompanyLoan = isCompanyLoan,
                    currentStatusCode = currentStatus.ToString(),
                    creditNr = model.CreditNr,
                    applicationNr = applicationNr,
                    notNotifiedCapitalAmount = model.GetNotNotifiedCapitalBalance(date),
                    capitalDebtAmount = model.GetBalance(CreditDomainModel.AmountType.Capital, date),
                    mortgageLoanEndDate = model.GetMortgageLoanEndDate(date),
                    mortgageLoanNextInterestRebindDate = model.GetDatedCreditDate(date, DatedCreditDateCode.MortgageLoanNextInterestRebindDate, null),
                    singlePaymentLoanRepaymentDays = model.GetSinglePaymentLoanRepaymentDays(),
                    mortgageLoanInterestRebindMonthCount = mortgageLoanInterestRebindMonthCount == null ? new int?() : (int)mortgageLoanInterestRebindMonthCount,
                    startDate = model.GetStartDate(),
                    totalInterestRatePercent = model.GetInterestRatePercent(date),
                    providerDisplayName = providerName,
                    providerDisplayNameLong = providerName == null ? null : (NEnv.GetAffiliateModel(providerName, allowMissing: true)?.DisplayToEnduserName ?? providerName),
                    repaymentTimeInMonths = repaymentTimeInMonths,
                    repaymentTimeInMonthsFailedMessage = repaymentTimeInMonthsFailedMessage,
                    signedAgreementLink1 = !isCompanyLoan ? getSignedAgreementLink(1) : getSignedAgreementLink(null),
                    signedAgreementLink2 = !isCompanyLoan ? getSignedAgreementLink(2) : null,
                    coSignedAgreementLink = !isCompanyLoan ? getSignedAgreementLink(null) : null,
                    applicationLink = NEnv.ClientCfgCore.IsFeatureEnabled("ntech.feature.precredit")
                        ? Url.Action("ApplicationLink", new { creditNr = creditNr, applicationNr = applicationNr })
                        : null,
                    totalSentToDebtCollectionAmount = totalSentToDebtCollectionAmount,
                    debtCollectionExportNrOfOverdueDays = debtCollectionExportNrOfOverdueDays,
                    currentNrOfOverdueDays = currentNrOfOverdueDays,
                    campaignCode = model.GetIntialLoanCampaignCode(date),
                    mainCreditCreditNr = model.GetDatedCreditString(date, DatedCreditStringCode.MainCreditCreditNr, null, allowMissing: true),
                    isForNonPropertyUse = model.GetDatedCreditString(date, DatedCreditStringCode.IsForNonPropertyUse, null, allowMissing: true) == "true",
                    childCreditCreditNr,
                    companyLoanRiskValues = NEnv.IsCompanyLoansEnabled ? new
                    {
                        lgd = model.GetDatedCreditValueOpt(date, DatedCreditValueCode.ApplicationLossGivenDefault),
                        pd = model.GetDatedCreditValueOpt(date, DatedCreditValueCode.ApplicationProbabilityOfDefault),
                    } : null,
                    sentToDebtCollectionDate = sentToDebtCollectionDateOrNull,
                    annuityAmount = amortizationModel.UsesAnnuities ? amortizationModel.GetActualAnnuityOrException() : new decimal?(),
                    currentFixedMonthlyCapitalPayment = !amortizationModel.UsesAnnuities ? amortizationModel.GetCurrentFixedMonthlyPaymentOrException(date) : new decimal?(),
                    marginInterestRatePercent = marginInterestRate,
                    requestedMarginInterestRatePercent = requestedMarginInterestRate,
                    referenceInterestRatePercent = referenceInterestRate,
                    mortgageLoanAgreementNr
                };

                var transactions = context
                    .Transactions
                    .Where(x => x.CreditNr == creditNr && x.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                    .OrderByDescending(x => x.TransactionDate)
                    .ThenByDescending(x => x.Timestamp)
                    .Select(x => new CapitalTransactionModel
                    {
                        id = x.Id,
                        transactionDate = x.TransactionDate,
                        eventDisplayName = x.BusinessEvent.EventType,
                        amount = x.Amount,
                        isWriteOff = x.WriteoffId.HasValue,
                        businessEventRoleCode = x.BusinessEventRoleCode,
                        subAccountCode = x.SubAccountCode
                    })
                    .ToList();

                var sum = 0m;
                foreach (var t in transactions.AsEnumerable().Reverse())
                {
                    sum += t.amount;
                    t.totalAmountAfter = sum;
                }

                result.capitalTransactions = transactions;

                return Json2(result);
            }
        }

        private class CapitalTransactionModel
        {
            public DateTime transactionDate { get; set; }
            public long id { get; set; }
            public string eventDisplayName { get; set; }
            public decimal amount { get; set; }
            public bool isWriteOff { get; set; }
            public string businessEventRoleCode { get; set; }
            public string subAccountCode { get; set; }
            public decimal totalAmountAfter { get; set; }
        }

        public static bool TryGetNrOfRemainingPayments(DateTime transactionDate, DbModel.DomainModel.NotificationProcessSettings ns, CreditAmortizationModel model, decimal balance, decimal interestRatePercent, decimal notificationFee, int? monthCap, out int nrOfPayments, out string failedMessage)
        {
            var getOverrideAmortizationAmountForMonth = PaymentPlanCalculation.CreateGetOverrideAmortizationAmountForMonth(model, new DateTime(transactionDate.Year, transactionDate.Month, ns.NotificationNotificationDay), ns.NotificationDueDay);

            var c = model.UsingActualAnnuityOrFixedMonthlyCapital(
                annuityAmount => PaymentPlanCalculation
                    .BeginCreateWithAnnuity(balance, annuityAmount, interestRatePercent, getOverrideAmortizationAmountForMonth, NEnv.CreditsUse360DayInterestYear)
                    .WithMonthlyFee(notificationFee)
                    .EndCreate(),
                fixedAmount => PaymentPlanCalculation
                    .BeginCreateWithFixedMonthlyCapitalAmount(balance, fixedAmount, interestRatePercent, monthCap, getOverrideAmortizationAmountForMonth, NEnv.CreditsUse360DayInterestYear)
                    .WithMonthlyFee(notificationFee)
                    .EndCreate());

            if (!c.TryPrefetchPayments(out failedMessage))
            {
                nrOfPayments = 0;
                return false;
            }

            failedMessage = null;
            nrOfPayments = c.Payments.Count;
            return true;
        }

        private Func<int?, string> GetSignedAgreementLinkBuilder(CreditContext context, CreditDomainModel model, DateTime date)
        {
            var latestAgreementByApplicant = context
                .Documents
                .Where(x => x.CreditNr == model.CreditNr && (x.DocumentType == "AdditionalLoanAgreement" || x.DocumentType == "InitialAgreement"))
                .Select(x => new
                {
                    x.ApplicantNr,
                    x.DocumentType,
                    x.ArchiveKey,
                    x.Timestamp
                })
                .GroupBy(x => x.ApplicantNr)
                .Select(x => x.OrderByDescending(y => y.Timestamp).FirstOrDefault())
                .Select(x => new
                {
                    x.ArchiveKey,
                    x.ApplicantNr,
                    x.DocumentType
                })
                .ToList()
                .ToDictionary(x => x.ApplicantNr ?? -1, x => new { x.ArchiveKey, x.DocumentType });

            Func<int?, string> getSignedAgreementLink = applicantNr =>
            {
                string archiveKey;

                if (latestAgreementByApplicant.ContainsKey(applicantNr ?? -1))
                    archiveKey = latestAgreementByApplicant[applicantNr ?? -1].ArchiveKey;
                else if (applicantNr.HasValue && model.GetNrOfApplicants() >= applicantNr)
                    archiveKey = model.GetSignedInitialAgreementArchiveKey(date, applicantNr);
                else
                    archiveKey = model.GetSignedInitialAgreementArchiveKey(date, null);

                if (archiveKey == null)
                    return null;

                return Url.Action("ArchiveDocument", "ApiArchiveDocument", new { key = archiveKey });
            };
            return getSignedAgreementLink;
        }
    }
}