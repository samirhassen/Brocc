using NTech.Banking.BankAccounts;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code.Services
{
    public class LoanPayoutReceiptService
    {
        private readonly IDocumentClient documentClient;
        private readonly PdfTemplateReader pdfTemplateReader;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly ICreditEnvSettings envSettings;
        private readonly EncryptionService encryptionService;
        private readonly BankAccountNumberParser bankAccountNumberParser;

        public LoanPayoutReceiptService(IDocumentClient documentClient, PdfTemplateReader pdfTemplateReader, IClientConfigurationCore clientConfiguration, 
            ICreditEnvSettings envSettings, EncryptionService encryptionService)
        {
            this.documentClient = documentClient;
            this.pdfTemplateReader = pdfTemplateReader;
            this.clientConfiguration = clientConfiguration;
            this.envSettings = envSettings;
            this.encryptionService = encryptionService;
            this.bankAccountNumberParser = new BankAccountNumberParser(clientConfiguration.Country.BaseCountry);
        }

        public void WithDocumentArchiveRenderer(Action<Func<(Dictionary<string, object> Context, string RenderedPdfFilename), string>> withRenderer)
        {
            if (IsFeatureActive())
            {
                var templateBytes = pdfTemplateReader.GetPdfTemplate("credit-payout-receipt", clientConfiguration.Country.BaseCountry, envSettings.IsTemplateCacheDisabled);
                var batchId = documentClient.BatchRenderBegin(templateBytes);
                try
                {
                    withRenderer(x => documentClient.BatchRenderDocumentToArchive(batchId, x.RenderedPdfFilename, x.Context));
                }
                finally
                {
                    try { documentClient.BatchRenderEnd(batchId); } catch { /* ignored to not mask potential actual exception */ }
                }
            }
            else
            {
                //This branch is just to make the feature easier to use. This can now be called regardless of if the feature is active or not and
                //the logic can just not call render instead
                withRenderer(x => { throw new Exception("Feature not active"); });
            }

        }

        public Dictionary<string, Dictionary<string, object>> CreatePrintContexts(HashSet<string> creditNrs, ICreditContextExtended context)
        {
            var clientCountry = clientConfiguration.Country;

            if (clientCountry.BaseCountry != "SE")
                throw new NotImplementedException();

            var printFormattingCulture = NTechCoreFormatting.GetPrintFormattingCulture(clientCountry.BaseFormattingCulture);

            var credits = context
                .CreditHeadersQueryable
                .Where(x => creditNrs.Contains(x.CreditNr))
                .Select(x => new
                {
                    x.CreditNr,
                    Payments = x
                        .CreatedByEvent
                        .CreatedOutgoingPayments
                        .Select(y => new
                        {
                            PaidOutAmount = y
                                .Transactions
                                .Where(z => z.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString() && z.BusinessEventId == x.CreatedByBusinessEventId)
                                .Sum(z => z.Amount),
                            SubAccountCode = y
                                .Transactions
                                .Where(z => z.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString() && z.BusinessEventId == x.CreatedByBusinessEventId)
                                .Select(z => z.SubAccountCode)
                                .FirstOrDefault(),
                            Items = y.Items.Select(z => new { z.Name, z.Value, z.IsEncrypted })
                        })
                })
                .ToList();
            var result = new Dictionary<string, Dictionary<string, object>>();
            var itemIdsToDecrypt = credits.SelectMany(x => x.Payments.SelectMany(y => y.Items.Where(z => z.IsEncrypted).Select(z => long.Parse(z.Value)))).ToArray();
            var decryptedItemsById = encryptionService.DecryptEncryptedValues(context, itemIdsToDecrypt);
            var printDate = context.CoreClock.Today;

            foreach (var credit in credits)
            {
                var creditPrintContext = new Dictionary<string, object>();
                var payments = new List<Dictionary<string, object>>();
                foreach (var payment in credit.Payments)
                {
                    var paymentContext = new Dictionary<string, object>();
                    paymentContext["amount"] = payment.PaidOutAmount.ToString("C", printFormattingCulture);
                    paymentContext["type"] = FormatSubAccountCode(payment.SubAccountCode, clientCountry.BaseCountry);
                    var itemsDict = payment.Items.ToDictionary(item => item.Name, item => item.IsEncrypted ? decryptedItemsById[long.Parse(item.Value)] : item.Value);
                    paymentContext["toAccountFormatted"] = FormatToAccount(itemsDict);
                    paymentContext["customerMessage"] = itemsDict.Opt("CustomerMessage");
                    paymentContext["paymentReference"] = itemsDict.Opt("PaymentReference");
                    payments.Add(paymentContext);
                }
                creditPrintContext["payments"] = payments;
                creditPrintContext["creditNr"] = credit.CreditNr;
                creditPrintContext["printDate"] = context.CoreClock.Today.ToString("yyyy-MM-dd");

                result[credit.CreditNr] = creditPrintContext;
            }
            return result;
        }

        public bool IsFeatureActive()
        {
            return envSettings.IsStandardUnsecuredLoansEnabled && clientConfiguration.Country.BaseCountry == "SE";
        }

        private string FormatSubAccountCode(string subAccountCode, string clientCountryIsoCode)
        {
            if (subAccountCode == "settledLoan" && clientCountryIsoCode == "SE") return "Löst lån";
            if (subAccountCode == "paidToCustomer" && clientCountryIsoCode == "SE") return "Utbetalas";

            return subAccountCode;
        }

        private string FormatToAccount(Dictionary<string, string> items)
        {
            var toBankAccountNr = items.Opt("ToBankAccountNr");
            var toBankAccountNrType = items.Opt("ToBankAccountNrType");
            if (toBankAccountNr == null || toBankAccountNrType == null) return "";

            
            var account = bankAccountNumberParser.ParseFromStringWithDefaults(toBankAccountNr, toBankAccountNrType);
            if (account.AccountType == BankAccountNumberTypeCode.BankAccountSe)
            {
                var a = (NTech.Banking.BankAccounts.Se.BankAccountNumberSe)account;
                return $"Bankkonto {a.FormatFor("display")} ({a.BankName})";
            }
            else if (account.AccountType == BankAccountNumberTypeCode.BankGiroSe)
            {
                return $"Bankgiro {account.FormatFor("display")}";
            }
            else if (account.AccountType == BankAccountNumberTypeCode.PlusGiroSe)
            {
                return $"Plusgiro {account.FormatFor("display")}";
            }
            else
            {
                return account.FormatFor("display");
            }
        }
    }
}