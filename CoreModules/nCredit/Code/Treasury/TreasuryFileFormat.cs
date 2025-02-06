using nCredit.DomainModel;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace nCredit.Code.Treasury
{
    public class TreasuryFileFormat
    {
        private string CreateCashFlowFileConsumerLoan(TreasuryDomainModel model, DateTime deliveryDate)
        {
            var file = NTechEnvironment.Instance.StaticResourceFile("ntech.credit.Treasury.settingsfile", "Treasury-business-credit-settings.txt", true);

            var f = NTechSimpleSettings.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);

            if (string.IsNullOrWhiteSpace(f.Opt("PrefixLoan")))
                throw new System.InvalidOperationException("PrefixLoan is missing in " + file.FullName);

            var tempFileName = Path.Combine(Path.GetTempPath(), $"TreasuryAmlExport_{Guid.NewGuid().ToString()}.csv");
            var sep = ";";
            using (var fs = System.IO.File.CreateText(tempFileName))
            {
                fs.WriteLine("Reporting Date;Loan number;Cashflow id;Cashflow date;Currency;Capital amount;Interest amount;Fee amount");

                var notificationSettings = NEnv.NotificationProcessSettings.GetByCreditType(CreditType.UnsecuredLoan);

                foreach (var t in model.TransactionsConsumerLoanCashFlow)
                {
                    var dueDate = new DateTime(t.CashflowDate.Year, t.CashflowDate.Month, notificationSettings.NotificationDueDay);

                    fs.WriteLine(deliveryDate.ToString("yyyy-MM-dd") + sep +
                        f.Req("PrefixLoan") + t.CreditNr + sep +
                        t.CashflowId + sep +
                        dueDate.ToString("yyyy-MM-dd") + sep +
                        NEnv.ClientCfg.Country.BaseCurrency + sep +
                        Math.Round(t.CapitalAmount, 4).ToString(CultureInfo.InvariantCulture) + sep +
                        Math.Round(t.InterestAmount ?? 0m, 4).ToString(CultureInfo.InvariantCulture) + sep +
                        Math.Round(t.FeeAmount, 4).ToString(CultureInfo.InvariantCulture));
                }
            }
            return tempFileName;
        }

        private string CreateCorporateLoansGurantorsFile(TreasuryDomainModel model, DateTime deliveryDate)
        {
            var file = NTechEnvironment.Instance.StaticResourceFile("ntech.credit.Treasury.settingsfile", "Treasury-business-credit-settings.txt", true);

            var f = NTechSimpleSettings.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);

            if (string.IsNullOrWhiteSpace(f.Opt("PrefixCompanyloanCustomerId")))
                throw new System.InvalidOperationException("PrefixCompanyloanCustomerId is missing in " + file.FullName);
            if (string.IsNullOrWhiteSpace(f.Opt("PrefixCompanyloanId")))
                throw new System.InvalidOperationException("PrefixCompanyloanId is missing in " + file.FullName);

            var tempFileName = Path.Combine(Path.GetTempPath(), $"TreasuryAmlExport_{Guid.NewGuid().ToString()}.csv");
            var sep = ";";
            using (var fs = System.IO.File.CreateText(tempFileName))
            {
                fs.WriteLine("Reporting Date;Loan number;Loan company customer id;Guarantee id;Guarantor customer id;Guarantor customer country;Guarantee percent");

                var newGuid = Guid.NewGuid().ToString();
                foreach (var t in model.GurantorsCorporateLoans)
                {
                    fs.WriteLine(deliveryDate.ToString("yyyy-MM-dd") + sep +
                        f.Req("PrefixCompanyloanId") + t.CreditNr + sep +
                        f.Req("PrefixCompanyloanCustomerId") + t.CustomerId1 + sep +
                        newGuid + sep +
                        f.Req("PrefixCompanyloanCustomerId") + t.GuarantorCustomerId + sep +
                        NEnv.ClientCfg.Country.BaseCountry + sep +
                        t.GuarantorCustPercent);
                }
            }
            return tempFileName;
        }

        private string CreateCashFlowFileCompanyLoan(TreasuryDomainModel model, DateTime deliveryDate)
        {
            var file = NTechEnvironment.Instance.StaticResourceFile("ntech.credit.Treasury.settingsfile", "Treasury-business-credit-settings.txt", true);

            var f = NTechSimpleSettings.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);

            if (string.IsNullOrWhiteSpace(f.Opt("PrefixCompanyloanId")))
                throw new System.InvalidOperationException("PrefixCompanyloanId is missing in " + file.FullName);

            var tempFileName = Path.Combine(Path.GetTempPath(), $"TreasuryAmlExport_{Guid.NewGuid().ToString()}.csv");
            var sep = ";";
            using (var fs = System.IO.File.CreateText(tempFileName))
            {
                fs.WriteLine("Reporting Date;Loan number;Cashflow id;Cashflow date;Currency;Capital amount;Interest amount;Fee amount");

                var notificationSettings = NEnv.NotificationProcessSettings.GetByCreditType(CreditType.CompanyLoan);

                foreach (var t in model.TransactionsCompanyLoanCashFlow)
                {
                    var dueDate = new DateTime(t.CashflowDate.Year, t.CashflowDate.Month, notificationSettings.NotificationDueDay);

                    fs.WriteLine(deliveryDate.ToString("yyyy-MM-dd") + sep +
                         f.Req("PrefixCompanyloanId") + t.CreditNr + sep +
                        t.CashflowId + sep +
                        dueDate.ToString("yyyy-MM-dd") + sep +
                        NEnv.ClientCfg.Country.BaseCurrency + sep +
                        Math.Round(t.CapitalAmount, 4).ToString(CultureInfo.InvariantCulture) + sep +
                        Math.Round(t.InterestAmount ?? 0m, 4).ToString(CultureInfo.InvariantCulture) + sep +
                        Math.Round(t.FeeAmount, 4).ToString(CultureInfo.InvariantCulture));
                }
            }
            return tempFileName;
        }

        private string CreateConsumerFile(TreasuryDomainModel model, DateTime deliveryDate)
        {
            var tempFileName = Path.Combine(Path.GetTempPath(), $"TreasuryAmlExport_{Guid.NewGuid().ToString()}.csv");
            const string sep = ";"; // Acts as the column delimiter in the csv file format. 

            var file = NTechEnvironment.Instance.StaticResourceFile("ntech.credit.Treasury.settingsfile", "Treasury-business-credit-settings.txt", true);
            var settings = NTechSimpleSettings.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);

            if (string.IsNullOrWhiteSpace(settings.Opt("ProductTypeConsumerLoan")))
                throw new System.InvalidOperationException("ProductTypeConsumerLoan is missing in " + file.FullName);
            if (string.IsNullOrWhiteSpace(settings.Opt("GeneralLedgerAccountPrefixConsumerLoan")))
                throw new System.InvalidOperationException("GeneralLedgerAccountPrefixConsumerLoan is missing in " + file.FullName);
            if (string.IsNullOrWhiteSpace(settings.Opt("PrefixConsumerLoanCustomerId")))
                throw new System.InvalidOperationException("PrefixConsumerLoanCustomerId is missing in " + file.FullName);
            if (string.IsNullOrWhiteSpace(settings.Opt("PrefixConsumerLoanLoanNr")))
                throw new System.InvalidOperationException("PrefixConsumerLoanLoanNr is missing in " + file.FullName);

            using (var fs = System.IO.File.CreateText(tempFileName))
            {
                var titles = new[]
                {
                    "Reporting Date", "Product type", "General ledger account", "Customer Id", "Customer full name",
                    "Customer country", "Customer2 Id", "Customer2 full name", "Customer2 country", "Loan number",
                    "Current balance", "Accrued Interest", "Currency", "Loan start date", "Projected loan end date",
                    "Status", "Days past due", "Interest rate", "Amortizationmodel", "Risk class",
                    "Civil status type", "Occupation type", "Collection date", "Provider", "Initial payout amount",
                    "Additional loan amount"
                };
                fs.WriteLine(string.Join(sep, titles));

                var transactions = model.TransactionsConsumerLoans.Where(x => x.CurrentBalance > 0 || x.IsDebtCollection).ToList();
                var applicationNrs = transactions.Where(x => x.ApplicationNr != null).Select(x => x.ApplicationNr).ToList();
                var client = LegacyServiceClientFactory.CreatePreCreditClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
                var applicationItemGroupsByApplicationNr = client.BulkFetchCreditApplicationItems(new NTech.Core.Module.Shared.Clients.BulkFetchCreditApplicationItemsRequest
                {
                    ApplicationNrs = applicationNrs,
                    ItemNames = new List<string> { "employment", "marriage" }
                });

                foreach (var transaction in transactions)
                {
                    var customerId2 = "";
                    if (transaction.CustomerId2.HasValue)
                        customerId2 = settings.Req("PrefixConsumerLoanCustomerId") + transaction.CustomerId2.ToString();
                    var endDate = transaction.EndDate != DateTime.MinValue
                        ? transaction.EndDate.ToString("yyyy-MM-dd")
                        : string.Empty;

                    var values = new[]
                    {
                        deliveryDate.ToString("yyyy-MM-dd"),
                        settings.Req("ProductTypeConsumerLoan"),
                        settings.Req("GeneralLedgerAccountPrefixConsumerLoan"),
                        settings.Req("PrefixConsumerLoanCustomerId") + transaction.CustomerId1,
                        transaction.CustomerFullName1,
                        transaction.CustomerCountry1,
                        customerId2,
                        transaction.CustomerFullName2,
                        transaction.CustomerCountry2,
                        settings.Req("PrefixConsumerLoanLoanNr") + transaction.CreditNr,
                        Math.Round(transaction.CurrentBalance, 4).ToString(CultureInfo.InvariantCulture),
                        Math.Round(transaction.AccruedIntrerest, 4).ToString(CultureInfo.InvariantCulture),
                        NEnv.ClientCfg.Country.BaseCurrency,
                        transaction.StartDate.ToString("yyyy-MM-dd"),
                        endDate,
                        (transaction.IsDebtCollection ? "debt collection" : "active"),
                        (transaction.IsDebtCollection ? "" : transaction.DaysPastDue?.ToString()),
                        Math.Round(transaction.TotalInterestRate, 4).ToString(CultureInfo.InvariantCulture),
                        "Annuity", // Let this be hardcoded. 
                        (string)null, //Risk group - Calculation moved to petrus
                        applicationItemGroupsByApplicationNr.Opt(transaction.ApplicationNr)?.Opt("applicant1")?.Opt("marriage"),
                        applicationItemGroupsByApplicationNr.Opt(transaction.ApplicationNr)?.Opt("applicant1")?.Opt("employment"),
                        transaction.CollectionDate?.ToString("yyyy-MM-dd"),
                        transaction.ProviderName,
                        transaction.InitialPayoutAmount?.ToString(CultureInfo.InvariantCulture) ?? "",
                        transaction.AdditionalLoanAmount?.ToString(CultureInfo.InvariantCulture) ?? ""
                    };

                    fs.WriteLine(string.Join(sep, values));
                }
            }

            return tempFileName;
        }

        private string CreateCorporateLoansFile(TreasuryDomainModel model, DateTime deliveryDate)
        {
            var tempFileName = Path.Combine(Path.GetTempPath(), $"TreasuryAmlExport_{Guid.NewGuid().ToString()}.csv");
            const string sep = ";";

            var file = NTechEnvironment.Instance.StaticResourceFile("ntech.credit.Treasury.settingsfile", "Treasury-business-credit-settings.txt", true);
            var settings = NTechSimpleSettings.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);

            if (string.IsNullOrWhiteSpace(settings.Opt("ProductTypeCorporateLoan")))
                throw new System.InvalidOperationException("ProductTypeCorporateLoan is missing in " + file.FullName);
            if (string.IsNullOrWhiteSpace(settings.Opt("GeneralLedgerAccountPrefixCorporateLoan")))
                throw new System.InvalidOperationException("GeneralLedgerAccountPrefixCorporateLoan is missing in " + file.FullName);
            if (string.IsNullOrWhiteSpace(settings.Opt("PrefixCompanyloanId")))
                throw new System.InvalidOperationException("PrefixCompanyloanId is missing in " + file.FullName);

            using (var fs = System.IO.File.CreateText(tempFileName))
            {
                var titles = new[]
                {
                    "Reporting Date", "Product type", "General ledger account", "Customer Id", "Customer full name",
                    "Customer country", "Customer SNI Code", "Loan number", "Current balance", "Accrued Interest",
                    "Currency", "Loan start date", "Projected loan end date", "Status", "Days past due", "Orgnr",
                    "PaymentFrequency", "Interest rate", "Amortizationmodel"
                };
                fs.WriteLine(string.Join(sep, titles));

                foreach (var transaction in model.TransactionsCorporateLoans.Where(x => x.CurrentBalance > 0 || x.IsDebtCollection))
                {
                    var customerId2 = "";
                    if (transaction.CustomerId2.HasValue)
                        customerId2 = settings.Req("PrefixConsumerLoanCustomerId") + transaction.CustomerId2.ToString();
                    var enddate = "";
                    if (transaction.EndDate.ToString() != new DateTime().ToString())
                        enddate = transaction.EndDate.ToString("yyyy-MM-dd");

                    var values = new[]
                    {
                        deliveryDate.ToString("yyyy-MM-dd"),
                        settings.Req("ProductTypeCorporateLoan"),
                        settings.Req("GeneralLedgerAccountPrefixCorporateLoan"),
                        settings.Req("PrefixCompanyloanId") + transaction.CustomerId1,
                        transaction.CustomerFullName1,
                        transaction.CustomerCountry1,
                        transaction.Sni,
                        settings.Req("PrefixCompanyloanId") + transaction.CreditNr,
                        Math.Round(transaction.CurrentBalance, 4).ToString(CultureInfo.InvariantCulture),
                        Math.Round(transaction.AccruedIntrerest, 4).ToString(CultureInfo.InvariantCulture),
                        NEnv.ClientCfg.Country.BaseCurrency,
                        transaction.StartDate.ToString("yyyy-MM-dd"),
                        enddate,
                        (transaction.IsDebtCollection ? "debt collection" : "active"),
                        (transaction.IsDebtCollection ? "" : transaction.DaysPastDue?.ToString()),
                        transaction.Orgnr,
                        "30",
                        Math.Round(transaction.TotalInterestRate, 4).ToString(CultureInfo.InvariantCulture),
                        "Annuity"
                    };

                    fs.WriteLine(string.Join(sep, values));
                }
            }
            return tempFileName;
        }

        public void WithTemporaryExportFileCashFlowConsumerLoans(TreasuryDomainModel model, DateTime deliveryDate, Action<string> withFile)
        {
            var tmp = CreateCashFlowFileConsumerLoan(model, deliveryDate);
            try
            {
                withFile(tmp);
            }
            finally
            {
                try { System.IO.File.Delete(tmp); } catch { /*ignored*/ }
            }
        }

        public void WithTemporaryExportFileCashFlowCompanyLoans(TreasuryDomainModel model, DateTime deliveryDate, Action<string> withFile)
        {
            var tmp = CreateCashFlowFileCompanyLoan(model, deliveryDate);
            try
            {
                withFile(tmp);
            }
            finally
            {
                try { System.IO.File.Delete(tmp); } catch { /*ignored*/ }
            }
        }

        public void WithTemporaryExportFileConsumer(TreasuryDomainModel model, DateTime deliveryDate, Action<string> withFile)
        {
            var tmpConsumer = CreateConsumerFile(model, deliveryDate);
            try
            {
                withFile(tmpConsumer);
            }
            finally
            {
                try { System.IO.File.Delete(tmpConsumer); } catch { /*ignored*/ }
            }
        }

        public void WithTemporaryExportFileCorporateLoan(TreasuryDomainModel model, DateTime deliveryDate, Action<string> withFile)
        {
            var tmpConsumer = CreateCorporateLoansFile(model, deliveryDate);
            try
            {
                withFile(tmpConsumer);
            }
            finally
            {
                try { System.IO.File.Delete(tmpConsumer); } catch { /*ignored*/ }
            }
        }

        public void WithTemporaryExportFileCorporateLoanGurantors(TreasuryDomainModel model, DateTime deliveryDate, Action<string> withFile)
        {
            var tmpConsumer = CreateCorporateLoansGurantorsFile(model, deliveryDate);
            try
            {
                withFile(tmpConsumer);
            }
            finally
            {
                try { System.IO.File.Delete(tmpConsumer); } catch { /*ignored*/ }
            }
        }

    }
}