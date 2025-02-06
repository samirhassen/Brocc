using nCredit;
using nCredit.DomainModel;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.BaseLoanExportRequestModel;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.NewOrChangedLoansRequestModel;

namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Services
{
    internal class PositiveCreditRegisterNewLoansService
    {
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly IClientConfigurationCore clientConfig;
        private readonly PcrTransformService transformService;

        private PositiveCreditRegisterSettingsModel Settings => envSettings.PositiveCreditRegisterSettings;

        public PositiveCreditRegisterNewLoansService(CreditContextFactory creditContextFactory, ICreditEnvSettings envSettings, IClientConfigurationCore clientConfig, PcrTransformService transformService)
        {
            this.creditContextFactory = creditContextFactory;
            this.envSettings = envSettings;
            this.clientConfig = clientConfig;
            this.transformService = transformService;
        }

        public NewLoanReport GetBatchNewLoans(DateTime? daySnapshot, bool isFirstTimeExport = false, DateTime? firstExportFromDate = null, DateTime? firstExportToDate = null)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var fields = new NewLoanReport();
                fields.TargetEnvironment = fields.TargetEnvironment = Settings.IsTargetProduction ? TargetEnvironment.Production : TargetEnvironment.Test;
                fields.Owner = new Owner
                {
                    IdCodeType = IdCodeType.BusinessId,
                    IdCode = Settings.OwnerIdCode
                };

                var currency = Enum.TryParse(clientConfig.Country.BaseCurrency, out CurrencyCode parsedCurrencyCode);
                var lenderMarketingName = Settings.LenderMarketingName;

                DateTime fromDate;
                DateTime toDate;

                if (isFirstTimeExport)
                {
                    if (!firstExportFromDate.HasValue || !firstExportToDate.HasValue)
                    {
                        throw new Exception("firstExportFromDate and firstExportToDate required for first export");
                    }

                    fromDate = firstExportFromDate.Value.Date;
                    toDate = firstExportToDate.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                }
                else
                {
                    if (!daySnapshot.HasValue)
                    {
                        throw new Exception("daySnapshot date required");
                    }

                    fromDate = daySnapshot.Value.Date;
                    toDate = fromDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                }

                var creditsPre = context.CreditHeadersQueryable
                    .Where(c => c.CreatedByEvent.TransactionDate >= fromDate
                    && c.CreatedByEvent.TransactionDate <= toDate);

                if (isFirstTimeExport)
                {
                    //We dont send already closed loans on first export as those will never be reported closed otherwise.
                    creditsPre = creditsPre.Where(x => x.Status == CreditStatus.Normal.ToString());
                }

                if (!creditsPre.Any())
                {
                    fields.Loans = new List<Loan>();
                    return fields;
                }

                var customerIds = creditsPre
                     .SelectMany(x => x.CreditCustomers.Select(i => i.CustomerId))
                     .ToHashSetShared();

                var civicRegNrByCustomerId = transformService.GetCivicRegNrsByCustomerId(customerIds);

                var credits = creditsPre
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
                                   .Sum(y => (decimal?)y.Amount) ?? 0m,
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
                       ContractDate = x.CreatedByEvent.TransactionDate.ToString("yyyy-MM-dd")
                   })
                   .Select(x => new Loan
                   {
                       ReportReference = x.CreditNr,
                       LoanNumber = new LoanNumber
                       {
                           Number = x.CreditNr,
                           Type = LoanNumberType.Other
                       },
                       LenderMarketingName = lenderMarketingName,
                       ContractDate = x.ContractDate,
                       CurrencyCode = parsedCurrencyCode,
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

                fields.Loans = credits.ToList();

                foreach (var loan in fields.Loans)
                {
                    transformService.FixLumpSumLoan(loan.LumpSumLoan);
                    loan.LoanNumber.Number = transformService.TransformLoanNr(loan.LoanNumber.Number);
                }

                return fields;
            }
        }
    }
}