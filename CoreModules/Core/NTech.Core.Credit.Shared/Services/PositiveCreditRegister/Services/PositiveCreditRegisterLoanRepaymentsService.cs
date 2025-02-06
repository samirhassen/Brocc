using nCredit;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Linq;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.BaseLoanExportRequestModel;
using NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models;
using System.Collections.Generic;

namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Services
{
    internal class PositiveCreditRegisterLoanRepaymentsService
    {
        private readonly ICoreClock clock;
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly IClientConfigurationCore clientConfig;
        private readonly PcrTransformService transformService;

        private PositiveCreditRegisterSettingsModel Settings => envSettings.PositiveCreditRegisterSettings;

        public PositiveCreditRegisterLoanRepaymentsService(ICoreClock clock, CreditContextFactory creditContextFactory, ICreditEnvSettings envSettings, IClientConfigurationCore clientConfig, PcrTransformService transformService)
        {
            this.clock = clock;
            this.creditContextFactory = creditContextFactory;
            this.envSettings = envSettings;
            this.clientConfig = clientConfig;
            this.transformService = transformService;
        }

        public LoanRepaymentsRequestModel GetBatchLoanRepayments(DateTime daySnapshot)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var fields = new LoanRepaymentsRequestModel();
                fields.TargetEnvironment = fields.TargetEnvironment = Settings.IsTargetProduction ? TargetEnvironment.Production : TargetEnvironment.Test;
                fields.Owner = new Owner
                {
                    IdCodeType = IdCodeType.BusinessId,
                    IdCode = Settings.OwnerIdCode
                };

                var currency = Enum.TryParse(clientConfig.Country.BaseCurrency, out CurrencyCode parsedCurrencyCode);
                var lenderMarketingName = Settings.LenderMarketingName;

                var forDate = daySnapshot.Date;

                var transactionCandidatesQueryable = context.TransactionsIncludingBusinessEventQueryable
                        .Where(t => t.IncomingPaymentId != null
                            && t.AccountCode != TransactionAccountType.UnplacedPayment.ToString()
                            && t.TransactionDate == forDate
                            && t.Credit.Status == CreditStatus.Normal.ToString()
                            && !t.Credit.AlternatePaymentPlans.Any());

                var creditNrsWithPayments = transactionCandidatesQueryable.Select(x => x.CreditNr).Distinct().ToList();

                var repayments = new List<Repayment>();
                foreach (var creditNrGroup in creditNrsWithPayments.ToArray().SplitIntoGroupsOfN(100))
                {
                    var creditBalances = context.CreditHeadersQueryable.Where(x => creditNrGroup.Contains(x.CreditNr)).Select(x => new
                    {
                        x.CreditNr,
                        Balance = x.Transactions
                                .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                                .Sum(y => (decimal?)y.Amount) ?? 0,
                    }).
                    ToDictionary(x => x.CreditNr, x => x.Balance);
                    var creditPayments = transactionCandidatesQueryable
                        .Where(x => creditNrGroup.Contains(x.CreditNr))
                        .GroupBy(x => x.CreditNr)
                        .Select(x => new
                        {
                            CreditNr = x.Key,
                            AnyIncludedTransactionId = x.First().Id,
                            AccountCodesAndAmounts = x.Select(y => new { y.AccountCode, y.Amount }),
                        })
                        .ToList();
                    foreach(var creditPayment in creditPayments)
                    {
                        repayments.Add(new Repayment
                        {
                            ReportCreationTimeUtc = clock.Now.UtcDateTime,
                            ReportType = ReportType.NewReport,
                            ReportReference = creditPayment.AnyIncludedTransactionId.ToString(),
                            LoanNumber = new LoanNumber
                            {
                                Number = transformService.TransformLoanNr(creditPayment.CreditNr),
                                Type = LoanNumberType.Other
                            },
                            LoanType = LoanType.LumpSumLoan,
                            LumpSumLoanRepayment = new LumpSumLoanRepayment
                            {
                                AmortizationPaid = -creditPayment.AccountCodesAndAmounts
                                .Where(item => item.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                                .Sum(item => item.Amount),
                                InterestPaid = -creditPayment.AccountCodesAndAmounts
                                .Where(item => item.AccountCode == TransactionAccountType.InterestDebt.ToString() || item.AccountCode == TransactionAccountType.CapitalizedInterest.ToString())
                                .Sum(item => item.Amount),
                                OtherExpenses = -creditPayment.AccountCodesAndAmounts
                                .Where(item => item.AccountCode != TransactionAccountType.CapitalDebt.ToString() && item.AccountCode != TransactionAccountType.InterestDebt.ToString() && item.AccountCode != TransactionAccountType.CapitalizedInterest.ToString())
                                .Sum(item => item.Amount),
                                PaymentDate = forDate.ToString("yyyy-MM-dd"),
                                Balance = creditBalances[creditPayment.CreditNr],
                            }
                        });
                    }
                }

                fields.Repayments = repayments;

                return fields;
            }
        }
    }
}

