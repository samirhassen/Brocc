using nCredit;
using nCredit.DomainModel;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.BaseLoanExportRequestModel;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.NewOrChangedLoansRequestModel;

namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Services
{
    internal class PositiveCreditRegisterLoanChangesService
    {
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly PcrTransformService transformService;

        private PositiveCreditRegisterSettingsModel Settings => envSettings.PositiveCreditRegisterSettings;

        public PositiveCreditRegisterLoanChangesService(CreditContextFactory creditContextFactory, ICreditEnvSettings envSettings, PcrTransformService transformService)
        {
            this.creditContextFactory = creditContextFactory;
            this.envSettings = envSettings;
            this.transformService = transformService;
        }

        public NewLoanReport GetBatchLoanChanges(DateTime daySnapshot)
        {
            var report = GetBatchLoanChangesInternal(daySnapshot);

            if (report.Loans != null)
            {
                foreach (var loan in report.Loans)
                {
                    transformService.FixLumpSumLoan(loan.LumpSumLoan);
                    loan.LoanNumber.Number = transformService.TransformLoanNr(loan.LoanNumber.Number);
                }
            }

            return report;
        }

        public NewLoanReport GetCorrectionBatchWithCurrentData(DateTime toDate)
        {
                var report = GetBatchLoanChangesForSpecificCredits(
                    toDate,
                    null,
                    reportType: ReportType.ErrorCorrection);

                if (report.Loans != null)
                {
                    foreach (var loan in report.Loans)
                    {
                        transformService.FixLumpSumLoan(loan.LumpSumLoan);
                        loan.LoanNumber.Number = transformService.TransformLoanNr(loan.LoanNumber.Number);
                    }
                }

                return report;
            
        }

        private NewLoanReport GetBatchLoanChangesForSpecificCredits(DateTime toDate, IQueryable<CreditHeader> creditsToSend, ReportType? reportType)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                if (creditsToSend == null)
                {
                    creditsToSend = context.CreditHeadersQueryable.Where(c => c.CreatedByEvent.TransactionDate <= toDate);
                }

                var lenderMarketingName = Settings.LenderMarketingName;

                var customerIds = creditsToSend
                     .SelectMany(x => x.CreditCustomers.Select(i => i.CustomerId))
                     .ToHashSetShared();

                var civicRegNrByCustomerId = transformService.GetCivicRegNrsByCustomerId(customerIds);

                var credits = creditsToSend
                            .Select(x => new
                            {
                                x.CreditNr,
                                x.NrOfApplicants,
                                Customers = x.CreditCustomers
                                                        .Select(c => new Borrower
                                                        {
                                                            IdCodeType = IdCodeType.PersonalIdentityCode,
                                                            IdCode = civicRegNrByCustomerId.ContainsKey(c.CustomerId)
                                                            ? civicRegNrByCustomerId[c.CustomerId]
                                                            : string.Empty,
                                                        })
                                                        .ToList(),
                                InitialPaidToCustomerAmount = x
                                                .CreatedByEvent
                                                .Transactions
                                                .Where(y => y.CreditNr == x.CreditNr && y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                                                .Sum(y => (decimal?)y.Amount) ?? 0,
                                InitialFeeDrawnFromLoanAmount = x
                                                .Transactions
                                                .Where(y => y.CreditNr == x.CreditNr
                                                    && y.AccountCode == TransactionAccountType.InitialFeeDrawnFromLoanAmount.ToString())
                                                .Sum(y => (decimal)y.Amount),
                                CapitalizedInitialFee = x.Transactions
                                                .Where(t => t.BusinessEvent.EventType == BusinessEventType.CapitalizedInitialFee.ToString() && t.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                                                .Sum(t => (decimal?)t.Amount) ?? 0,
                                PaidAmount = -(x.Transactions
                                                .Where(t => t.AccountCode == TransactionAccountType.CapitalDebt.ToString() && t.IncomingPaymentId.HasValue)
                                                .Sum(t => (decimal?)t.Amount) ?? 0m),
                                Balance = x.Transactions
                                                .Where(y => y.TransactionDate <= toDate && y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                                                .Sum(y => (decimal?)y.Amount) ?? 0,
                                ReferenceInterestRate = x.DatedCreditValues
                                                    .Where(y => y.Name == DatedCreditValueCode.ReferenceInterestRate.ToString())
                                                    .OrderByDescending(y => y.TransactionDate)
                                                    .ThenByDescending(y => y.Timestamp)
                                                    .Select(y => (decimal?)y.Value)
                                                    .FirstOrDefault() ?? 0m,
                                MarginPct = x
                                            .DatedCreditValues
                                            .Where(y => y.TransactionDate <= toDate && y.Name == DatedCreditValueCode.MarginInterestRate.ToString())
                                            .OrderByDescending(y => y.Id)
                                            .Select(y => (decimal?)y.Value)
                                            .FirstOrDefault() ?? 0,
                                AmortizationModel = x
                                                .DatedCreditStrings
                                                .Where(z => z.Name == DatedCreditStringCode.AmortizationModel.ToString())
                                                .OrderByDescending(z => z.TransactionDate)
                                                .ThenByDescending(z => z.Id)
                                                .Select(z => z.Value)
                                                .FirstOrDefault(),
                                ContractDate = x.CreatedByEvent.TransactionDate
                            })
                            .Select(x => new Loan
                            {
                                ReportReference = x.CreditNr,
                                ReportType = reportType ?? ReportType.NewReport,
                                LoanNumber = new LoanNumber
                                {
                                    Number = x.CreditNr,
                                    Type = LoanNumberType.Other
                                },
                                LenderMarketingName = lenderMarketingName,
                                ContractDate = x.ContractDate.ToString("yyyy-MM-dd"),
                                OneTimeServiceFees = x.InitialFeeDrawnFromLoanAmount + x.CapitalizedInitialFee,
                                LoanType = LoanType.LumpSumLoan,
                                BorrowersCount = x.NrOfApplicants,
                                Borrowers = x.Customers,
                                LumpSumLoan = new LumpSumLoan
                                {
                                    AmountIssued = x.InitialPaidToCustomerAmount,
                                    AmountPaid = x.PaidAmount,
                                    Balance = x.Balance,
                                    PurposeOfUse = LoanPurposeOfUse.OtherConsumerCredit,
                                    AmortizationFrequency = 1, //per month,
                                    RepaymentMethod = x.AmortizationModel == AmortizationModelCode.MonthlyFixedAmount.ToString() ? RepaymentMethod.FixedSizeAmortizations : RepaymentMethod.Annuities,
                                },
                                Interest = new Interest
                                {
                                    TotalInterestRatePct = x.ReferenceInterestRate + x.MarginPct,
                                    MarginPct = x.MarginPct,
                                    InterestType = InterestType.Euribor,
                                    InterestDeterminationPeriod = 3 //3-month Euribor
                                },
                                IsLoanWithCollateral = false,
                                IsPeerToPeerLoanBroker = false,
                                ConsumerCredit = new ConsumerCredit
                                {
                                    LoanConsumerProtectionAct = LoanConsumerProtectionAct.ConsumerCredit,
                                    IsGoodsOrServicesRelatedCredit = false,
                                }
                            })
                             .ToList();

                var fields = new NewLoanReport();
                fields.TargetEnvironment = Settings.IsTargetProduction ? TargetEnvironment.Production : TargetEnvironment.Test;
                fields.Owner = new Owner
                {
                    IdCodeType = IdCodeType.BusinessId,
                    IdCode = Settings.OwnerIdCode
                };

                fields.Loans = credits.ToList();

                return fields;
            }
        }

        private NewLoanReport GetBatchLoanChangesInternal(DateTime daySnapshot)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var fields = new NewLoanReport();
                fields.TargetEnvironment = Settings.IsTargetProduction ? TargetEnvironment.Production : TargetEnvironment.Test;
                fields.Owner = new Owner
                {
                    IdCodeType = IdCodeType.BusinessId,
                    IdCode = Settings.OwnerIdCode
                };

                var lenderMarketingName = Settings.LenderMarketingName;

                var fromDate = daySnapshot.Date;  // Start of the day (00:00)
                var toDate = fromDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);  // End of the day (23:59:59)

                var creditsPre = context.CreditHeadersQueryable;

                //credits with changes:
                //a change to the payment plan: for example, the final due date of the payment plan changes
                // a change to the number of debtors: for example, only one debtor remains, in which case the personal identity code of only this individual is reported in the borrower information
                //changes to interest: for example, the interest information that is valid when the fixed interest rate period ends.

                //If recent reference interest change
                //Send all loans
                var referenceInterestRateChanged = context.BusinessEventsQueryable.Where(x => x.EventType == BusinessEventType.ReferenceInterestRateChange.ToString() && x.TransactionDate >= fromDate && x.TransactionDate <= toDate).FirstOrDefault();

                if (referenceInterestRateChanged != null)
                {
                    return GetBatchLoanChangesForSpecificCredits(toDate, context.CreditHeadersQueryable, null);
                }
                else
                {

                    var creditsWithRecentChanges = creditsPre
                        .Select(x => new
                        {
                            RecentChangeTerms = context.CreditTermsChangeHeadersQueryable.Where(h => h.CreditNr == x.CreditNr && h.CommitedByEvent.EventType == BusinessEventType.AcceptedCreditTermsChange.ToString()
                                && h.CommitedByEvent.EventDate >= fromDate && h.CommitedByEvent.EventDate <= toDate).FirstOrDefault(),
                            RecentPaymentPlan = x.AlternatePaymentPlans.Where(a => a.ChangedDate >= fromDate && a.ChangedDate <= toDate).FirstOrDefault(),
                            Credit = x

                        })
                        .Where(x => x.RecentChangeTerms != null || x.RecentPaymentPlan != null)
                        .Select(x => x.Credit);

                    return GetBatchLoanChangesForSpecificCredits(toDate, creditsWithRecentChanges, null);
                }
            }
        }
    }
}

