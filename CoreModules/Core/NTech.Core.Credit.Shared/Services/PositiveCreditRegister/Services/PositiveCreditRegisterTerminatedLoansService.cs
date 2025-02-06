using nCredit;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Linq;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.BaseLoanExportRequestModel;
using NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models;

namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Services
{
    internal class PositiveCreditRegisterTerminatedLoansService
    {
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly IClientConfigurationCore clientConfig;
        private readonly PcrTransformService transformService;

        private PositiveCreditRegisterSettingsModel Settings => envSettings.PositiveCreditRegisterSettings;

        public PositiveCreditRegisterTerminatedLoansService(CreditContextFactory creditContextFactory, ICreditEnvSettings envSettings, IClientConfigurationCore clientConfig, PcrTransformService transformService)
        {
            this.creditContextFactory = creditContextFactory;
            this.envSettings = envSettings;
            this.clientConfig = clientConfig;
            this.transformService = transformService;
        }

        public TerminatedLoansRequestModel GetBatchTerminatedLoans(DateTime daySnapshot)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var fields = new TerminatedLoansRequestModel();
                fields.TargetEnvironment = fields.TargetEnvironment = Settings.IsTargetProduction ? TargetEnvironment.Production : TargetEnvironment.Test;
                fields.Owner = new Owner
                {
                    IdCodeType = IdCodeType.BusinessId,
                    IdCode = Settings.OwnerIdCode
                };

                var currency = Enum.TryParse(clientConfig.Country.BaseCurrency, out CurrencyCode parsedCurrencyCode);
                var lenderMarketingName = Settings.LenderMarketingName;

                var fromDate = daySnapshot.Date;  // Start of the day (00:00)
                var toDate = fromDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);  // End of the day (23:59:59)

                var terminatedLoans = context.CreditHeadersQueryable
                    .Where(x =>
                       x.DatedCreditStrings.Any(z => z.Name == DatedCreditStringCode.CreditStatus.ToString() && z.Value == CreditStatus.Settled.ToString() && z.TransactionDate >= fromDate && z.TransactionDate <= toDate)
                    || x.DatedCreditStrings.Any(z => z.Name == DatedCreditStringCode.CreditStatus.ToString() && z.Value == CreditStatus.SentToDebtCollection.ToString() && z.TransactionDate >= fromDate && z.TransactionDate <= toDate)
                    || x.DatedCreditStrings.Any(z => z.Name == DatedCreditStringCode.CreditStatus.ToString() && z.Value == CreditStatus.WrittenOff.ToString() && z.TransactionDate >= fromDate && z.TransactionDate <= toDate))
                    .Select(x => new TerminateLoanDto
                    {
                        ReportReference = x.CreditNr,
                        ReportType = ReportType.NewReport,
                        LoanNumber = new LoanNumber
                        {
                            Type = LoanNumberType.Other,
                            Number = x.CreditNr
                        },
                        Termination = new TerminationDto
                        {
                            IsTerminated = true,
                            EndDate = x.DatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.CreditStatus.ToString()).OrderByDescending(y => y.TransactionDate).Select(y => y.TransactionDate).FirstOrDefault().ToString("yyyy-MM-dd"),
                            IsTransferredToAnotherLender = false
                        }
                    })
                    .ToList();

                fields.LoanTerminations = terminatedLoans;

                foreach(var loan in fields.LoanTerminations)
                {
                    loan.LoanNumber.Number = transformService.TransformLoanNr(loan.LoanNumber.Number);
                }

                return fields;

            }
        }
    }
}

