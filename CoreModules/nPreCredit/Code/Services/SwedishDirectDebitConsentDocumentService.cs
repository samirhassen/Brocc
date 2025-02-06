using nPreCredit.WebserviceMethods.UnsecuredLoansStandard;
using NTech;
using NTech.Banking.BankAccounts.Se;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.ElectronicSignatures;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class SwedishDirectDebitConsentDocumentService
    {
        private readonly IClock clock;
        private readonly ApplicationInfoService applicationInfoService;
        private readonly IComplexApplicationListReadOnlyService complexApplicationListService;
        private readonly INTechCurrentUserMetadata currentUserMetadata;

        public SwedishDirectDebitConsentDocumentService(IClock clock, ApplicationInfoService applicationInfoService, IComplexApplicationListReadOnlyService complexApplicationListService, INTechCurrentUserMetadata currentUserMetadata)
        {
            this.clock = clock;
            this.applicationInfoService = applicationInfoService;
            this.complexApplicationListService = complexApplicationListService;
            this.currentUserMetadata = currentUserMetadata;
        }

        public bool TryCreateUnsignedDirectDebitConsentPdfForApplication(ApplicationInfoModel ai, out string documentArchiveKey, out string failedCode, BankAccountNumberSe directDebitBankAccountNrOverride = null, int? directDebitApplicantNrOverride = null)
        {
            documentArchiveKey = null;

            using (var context = new PreCreditContextExtended(currentUserMetadata, clock))
            {
                var lists = complexApplicationListService.GetListsForApplication(ai.ApplicationNr, true, context, "Application", "Applicant");
                var applicationRow = lists["Application"].GetRow(1, true);

                var directDebitApplicantNr = directDebitApplicantNrOverride ?? applicationRow.GetUniqueItemInteger("directDebitAccountOwnerApplicantNr", false);
                if (!directDebitApplicantNr.HasValue)
                {
                    failedCode = "missingApplicant";
                    return false;
                }

                BankAccountNumberSe directDebitBankAccountNr;
                if (directDebitBankAccountNrOverride != null)
                {
                    directDebitBankAccountNr = directDebitBankAccountNrOverride;
                }
                else
                {
                    var directDebitBankAccountNrRaw = applicationRow.GetUniqueItem("directDebitBankAccountNr");
                    if (!BankAccountNumberSe.TryParse(directDebitBankAccountNrRaw, out directDebitBankAccountNr, out _))
                    {
                        failedCode = "invalidBankAccountNr";
                        return false;
                    }
                }

                var applicantCustomerId = lists["Applicant"].GetRow(directDebitApplicantNr.Value, true).GetUniqueItemInteger("customerId");
                if (!applicantCustomerId.HasValue)
                {
                    failedCode = "missingCustomerId";
                    return false;
                }

                var creditClient = new CreditClient();
                var creditNr = UnsecuredLoanStandardCreateLoanMethod.EnsureCreditNr(ai, applicationRow, context).CreditNr;

                context.SaveChanges();

                var directDebitData = creditClient.GenerateDirectDebitPayerNumber(creditNr, directDebitApplicantNr.Value);

                return TryCreateUnsignedDirectDebitConsentPdf(
                    applicantCustomerId.Value,
                    BankGiroNumberSe.Parse(directDebitData.ClientBankGiroNr),
                    directDebitBankAccountNr,
                    directDebitData.PayerNr,
                    out documentArchiveKey,
                    out failedCode);
            }
        }

        public bool TryCreateUnsignedDirectDebitConsentPdfForApplication(string applicationNr, out string documentArchiveKey, out string failedCode, BankAccountNumberSe directDebitBankAccountNrOverride = null, int? directDebitApplicantNrOverride = null)
        {
            var ai = applicationInfoService.GetApplicationInfo(applicationNr, true);
            if (ai == null)
            {
                documentArchiveKey = null;
                failedCode = "noSuchApplicationExists";
                return false;
            }
            return TryCreateUnsignedDirectDebitConsentPdfForApplication(ai, out documentArchiveKey, out failedCode,
                directDebitBankAccountNrOverride: directDebitBankAccountNrOverride, directDebitApplicantNrOverride: directDebitApplicantNrOverride);
        }

        public void OnSignatureEvent(ApplicationInfoModel applicationInfo, CommonElectronicIdSignatureSession session)
        {
            var applicationNr = applicationInfo.ApplicationNr;
            using (var context = new PreCreditContextExtended(currentUserMetadata, clock))
            {
                var listChanges = new List<ComplexApplicationListOperation>();
                void SetUniqueItem(string name, string value) => listChanges.Add(new ComplexApplicationListOperation
                {
                    ApplicationNr = applicationNr,
                    ListName = "DirectDebitSigningSession",
                    ItemName = name,
                    Nr = 1,
                    UniqueValue = value
                });

                if (session.HaveAllSigned())
                {
                    SetUniqueItem("SignedDirectDebitConsentFilePdfArchiveKey", session.SignedPdf.ArchiveKey);
                    SetUniqueItem("IsSessionActive", "false");
                }
                else
                {
                    // We currently do nothing if it is not successful. 
                }

                ComplexApplicationListService.ChangeListComposable(listChanges, context);
                context.SaveChanges();
            }
        }

        public void CancelDirectDebitSignatureSession(string applicationNr)
        {
            using (var context = new PreCreditContextExtended(currentUserMetadata, clock))
            {
                var directDebitSession = ComplexApplicationListService.GetListRow(applicationNr, "DirectDebitSigningSession", 1, context);

                if (!directDebitSession.UniqueItems.Any())
                {
                    throw new NTechWebserviceMethodException("No direct debit consent file signature session exists")
                    {
                        IsUserFacing = true,
                        ErrorHttpStatusCode = 400,
                        ErrorCode = "noActiveSignatureSession"
                    };
                }

                var deleteSignatureRowOperations = ComplexApplicationListService.CreateDeleteRowOperations(applicationNr, "DirectDebitSigningSession", 1, context);
                ComplexApplicationListService.ChangeListComposable(deleteSignatureRowOperations, context);

                context.SaveChanges();
            }
        }

        private bool TryCreateUnsignedDirectDebitConsentPdf(int customerId, BankGiroNumberSe clientBankGiroNr, BankAccountNumberSe customerBankAccountNr, string customerPayerNr, out string documentArchiveKey, out string failedCode)
        {
            if (NEnv.ClientCfg.Country.BaseCountry != "SE")
                throw new Exception("This document is specific to sweden");

            documentArchiveKey = null;

            var customerClient = new PreCreditCustomerClient();
            var customer = customerClient.BulkFetchPropertiesByCustomerIdsD(Enumerables.Singleton(customerId).ToHashSet(),
                "civicRegNr", "firstName", "lastName", "addressStreet", "addressZipcode", "addressCity").Opt(customerId);

            if (clientBankGiroNr == null)
            {
                failedCode = "missingClientBankGiroNr";
                return false;
            }
            if (customerBankAccountNr == null)
            {
                failedCode = "missingCustomerBankAccountNr";
                return false;
            }
            if (string.IsNullOrWhiteSpace(customerPayerNr))
            {
                failedCode = "missingCustomerPayerNr";
                return false;
            }

            var customerFirstName = customer?.Opt("firstName");
            var customerLastName = customer?.Opt("lastName");
            if (string.IsNullOrWhiteSpace(customerFirstName) && string.IsNullOrWhiteSpace(customerLastName))
            {
                failedCode = "missingName";
                return false;
            }

            if (!NEnv.BaseCivicRegNumberParser.TryParse(customer?.Opt("civicRegNr"), out var civicRegNr))
            {
                failedCode = "missingCivicRegNr";
                return false;
            }

            var context = new ExpandoObject();
            context.SetValues(x =>
            {
                x["customerFullName"] = $"{customerFirstName} {customerLastName}".Trim();
                x["customerBankAccountNr"] = customerBankAccountNr.FormatFor("display");
                x["customerPayerNr"] = customerPayerNr;
                x["clientBankGiroNr"] = clientBankGiroNr.DisplayFormattedValue;
                x["customerStreetAddress"] = customer.Opt("addressStreet") ?? "";
                x["customerZipCodeAndCity"] = $"{customer.Opt("addressZipcode")} {customer.Opt("addressCity")}".Trim();
            });

            var documentClient = new nDocumentClient();
            var pdfData = documentClient.PdfRenderDirect("credit-direct-debit-consent", context);

            documentArchiveKey = documentClient.ArchiveStore(pdfData, "application/pdf", $"direct-debit-consent-{customerId}-{clock.Today:yyyy-MM-dd}.pdf");
            failedCode = null;

            return true;
        }
    }
}