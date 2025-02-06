using nPreCredit.WebserviceMethods.UnsecuredLoansStandard;
using NTech;
using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.Conversion;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.ElectronicSignatures;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class UnsecuredLoanStandardAgreementService
    {
        private readonly ApplicationInfoService applicationInfoService;
        private readonly UnsecuredLoanStandardWorkflowService workflowService;
        private readonly IClock clock;
        private readonly INTechCurrentUserMetadata currentUser;
        private readonly ILockedAgreementService lockedAgreementService;
        private readonly IApplicationDocumentService applicationDocumentService;
        private readonly NTech.Core.Module.Shared.Clients.IDocumentClient documentClient;
        private readonly IClientConfiguration clientConfiguration;
        private readonly NTech.Core.Module.Shared.Clients.ICreditClient creditClient;
        private readonly Lazy<CultureInfo> formattingCulture;

        public UnsecuredLoanStandardAgreementService(ApplicationInfoService applicationInfoService, UnsecuredLoanStandardWorkflowService workflowService, IClock clock, INTechCurrentUserMetadata currentUser, ILockedAgreementService lockedAgreementService, IApplicationDocumentService applicationDocumentService, 
            NTech.Core.Module.Shared.Clients.IDocumentClient documentClient, IClientConfiguration clientConfiguration, NTech.Core.Module.Shared.Clients.ICreditClient creditClient)
        {
            this.applicationInfoService = applicationInfoService;
            this.workflowService = workflowService;
            this.clock = clock;
            this.currentUser = currentUser;
            this.lockedAgreementService = lockedAgreementService;
            this.applicationDocumentService = applicationDocumentService;
            this.documentClient = documentClient;
            this.clientConfiguration = clientConfiguration;
            this.creditClient = creditClient;
            this.formattingCulture = new Lazy<CultureInfo>(() => CultureInfo.GetCultureInfo(clientConfiguration.Country.BaseFormattingCulture));
        }

        protected CultureInfo F => formattingCulture.Value;

        public void CancelSignatureSession(string applicationNr, bool enforceLockedAgreement)
        {
            var ai = applicationInfoService.GetApplicationInfo(applicationNr, true);

            if (ai == null)
                throw new NTechWebserviceMethodException("Application does not exist") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            if (!ai.IsActive)
                throw new NTechWebserviceMethodException("Application is not active") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            var isStepCurrent =
                workflowService.IsStepStatusInitial(UnsecuredLoanStandardWorkflowService.AgreementStep.Name, ai.ListNames)
                &&
                workflowService.AreAllStepsBeforeComplete(UnsecuredLoanStandardWorkflowService.AgreementStep.Name, ai.ListNames);

            if (enforceLockedAgreement && !ai.HasLockedAgreement)
                throw new NTechWebserviceMethodException("No agreement session exists") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            if (ai.IsFinalDecisionMade || !isStepCurrent)
                throw new NTechWebserviceMethodException("Application cannot be changed") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            string sessionAgreementArchiveKey;
            using (var context = new PreCreditContextExtended(currentUser, clock))
            {
                var currentRow = ComplexApplicationListService.GetListRow(applicationNr, "AgreementSignatureSession", 1, context);
                sessionAgreementArchiveKey = currentRow.UniqueItems.Opt("SignedAgreementPdfArchiveKey");
                var deleteSignatureRowOperations = ComplexApplicationListService.CreateDeleteRowOperations(applicationNr, "AgreementSignatureSession", 1, context);
                ComplexApplicationListService.ChangeListComposable(deleteSignatureRowOperations, context);
                if (ai.HasLockedAgreement)
                    context.CreateAndAddComment("Signing cancelled", "SigningCancelled", applicationNr: applicationNr);
                context.SaveChanges();
            }

            //TODO: Make composable
            lockedAgreementService.UnlockAgreement(applicationNr);

            //TODO: Make composable
            if (sessionAgreementArchiveKey != null)
            {
                //NOTE: We dont remove all signed agreements, only ones created by this signature session
                foreach (var document in applicationDocumentService.FetchForApplication(applicationNr, new List<string> { CreditApplicationDocumentTypeCode.SignedAgreement.ToString() }))
                {
                    if (document.DocumentArchiveKey == sessionAgreementArchiveKey)
                        applicationDocumentService.RemoveDocument(applicationNr, document.DocumentId);
                }
            }
        }

        public ApplicationDocumentModel AttachSignedAgreementDirectly(string applicationNr, string attachedFileAsDataUrl, string attachedFileName)
        {
            //NOTE: Attach is allowed at exactly the same times as signature sessions so the Cancel method will take care of preconditions.
            //      The reason to remove any signature session is to not give the false impresson that the attached document
            //      has passed through any kind of system signing workflow.
            CancelSignatureSession(applicationNr, false);

            if (!applicationDocumentService.TryAddDocument(applicationNr, CreditApplicationDocumentTypeCode.SignedAgreement.ToString(), null, null, null, attachedFileAsDataUrl, attachedFileName, out var document, out var failedMessage))
                throw new NTechWebserviceMethodException(failedMessage) { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            return document;
        }

        public void RemoveSignedAgreementDirectly(string applicationNr)
        {
            //NOTE: Attach is allowed at exactly the same times as signature sessions so the Cancel method will take care of preconditions.
            //      The reason to remove any signature session is to not give the false impresson that the attached document
            //      has passed through any kind of system signing workflow.
            CancelSignatureSession(applicationNr, false);

            foreach (var document in applicationDocumentService.FetchForApplication(applicationNr, new List<string> { CreditApplicationDocumentTypeCode.SignedAgreement.ToString() }))
            {
                var archiveKey = document.DocumentArchiveKey;
                applicationDocumentService.RemoveDocument(applicationNr, document.DocumentId);
                if (archiveKey != null)
                {
                    documentClient.DeleteArchiveFile(archiveKey);
                }
            }
        }

        private void OnSignatureEventRegardlessOfProvider(ApplicationInfoModel applicationInfo, CommonElectronicIdSignatureSession session, PreCreditContextExtended context)
        {
            var applicationNr = applicationInfo.ApplicationNr;
            var listChanges = new List<ComplexApplicationListOperation>();
            void SetUniqueItem(string name, string value) => listChanges.Add(new ComplexApplicationListOperation
            {
                ApplicationNr = applicationNr,
                ListName = "AgreementSignatureSession",
                ItemName = name,
                Nr = 1,
                UniqueValue = value
            });
            void SetRepeatedItem(string name, List<string> value) => listChanges.Add(new ComplexApplicationListOperation
            {
                ApplicationNr = applicationNr,
                ListName = "AgreementSignatureSession",
                ItemName = name,
                Nr = 1,
                RepeatedValue = value
            });

            if (session.IsFailed())
            {
                SetUniqueItem("IsSessionActive", "false");
                SetUniqueItem("IsSessionFailed", "false");

                context.CreateAndAddComment($"Signature failed. ({session.ClosedMessage})", "SignatureFailed", applicationNr: applicationNr);
            }
            else
            {
                var signedByApplicantNrs = session.GetSignedByApplicantNrs();
                if (signedByApplicantNrs.Count > 0)
                {
                    SetRepeatedItem("SignedByApplicantNr", signedByApplicantNrs.OrderBy(x => x).Select(x => x.ToString()).ToList());
                }
                if (signedByApplicantNrs.Count == applicationInfo.NrOfApplicants)
                {
                    if (session.SignedPdf == null)
                        throw new Exception($"Successful signature session produced no signed agreement. Application {applicationNr}");

                    SetUniqueItem("SignedAgreementPdfArchiveKey", session.SignedPdf.ArchiveKey);
                    SetUniqueItem("IsSessionActive", "false");

                    context.CreateAndAddApplicationDocument(session.SignedPdf.ArchiveKey,
                            session.SignedPdf.FileName,
                            CreditApplicationDocumentTypeCode.SignedAgreement,
                            applicationNr: applicationNr);

                    context.CreateAndAddComment("Agreement signed", "AgreementSigned", applicationNr);
                }
            }

            ComplexApplicationListService.ChangeListComposable(listChanges, context);
        }

        public void OnCommonSignatureEvent(ApplicationInfoModel applicationInfo, CommonElectronicIdSignatureSession signatureSession)
        {
            var applicationNr = applicationInfo.ApplicationNr;
            using (var context = new PreCreditContextExtended(currentUser, clock))
            {
                Action run = () =>
                {
                    var sessionList = ComplexApplicationListService.GetListRow(applicationNr, "AgreementSignatureSession", 1, context);
                    if (sessionList.UniqueItems.Opt("IsSessionActive") != "true")
                    {
                        context.CreateAndAddComment($"Received signature event with no active signature session. Signicat SessionId={signatureSession.Id}", "AgreementSignatureCallback", applicationNr: applicationNr);
                        return;
                    }

                    OnSignatureEventRegardlessOfProvider(applicationInfo, signatureSession, context);
                };

                run();

                context.SaveChanges();
            }
        }

        public MemoryStream CreateAgreementPdf(UnsecuredLoanStandardAgreementPrintContext context, string overrideTemplateName = null, bool? disableTemplateCache = false)
        {
            var dc = new nDocumentClient();

            var pdfBytes = dc.PdfRenderDirect(
                overrideTemplateName ?? "credit-agreement",
                PdfCreator.ToTemplateContext(context),
                disableTemplateCache: disableTemplateCache.GetValueOrDefault());

            return new MemoryStream(pdfBytes);
        }

        public UnsecuredLoanStandardAgreementPrintContext GetPrintContext(ApplicationInfoModel applicationInfo)
        {
            var civicRegNrByApplicantNr = new Dictionary<int, string>();
            var applicants = applicationInfoService.GetApplicationApplicants(applicationInfo.ApplicationNr, includeCivicRegNr: (x, y) => civicRegNrByApplicantNr[x] = y);

            using (var context = new PreCreditContextExtended(currentUser, clock))
            {
                var application = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == applicationInfo.ApplicationNr)
                    .Select(x => new
                    {
                        ApplicationListItems = x.ComplexApplicationListItems,
                        DecisionItems = x.CurrentCreditDecision.DecisionItems
                    })
                    .Single();

                var applicationRow = ComplexApplicationList
                    .CreateListFromFlattenedItems("Application", application.ApplicationListItems.ToList())
                    .GetRow(1, true);
                var decisionRow = ComplexApplicationList.CreateListFromFlattenedItems("Temp", application.DecisionItems.Select(x => new ComplexApplicationListItemBase
                {
                    ItemName = x.ItemName,
                    ItemValue = x.Value,
                    ListName = "Temp",
                    Nr = 1
                }).ToList()).GetRow(1, true);
                var creditClient = new CreditClient();
                var creditNrResult = UnsecuredLoanStandardCreateLoanMethod.EnsureCreditNr(applicationInfo, applicationRow, context);
                if (creditNrResult.WasCreated)
                {
                    context.SaveChanges();
                }
                var creditNr = creditNrResult.CreditNr;

                var notificationProcessSettings = NTechCache.WithCacheS(
                    "33aa7360-5661-4c9f-a27f-cef8053cbf3f", TimeSpan.FromMinutes(15), () => creditClient.FetchNotificationProcessSettings());

                const string DecimalFormat = "#,##0.##";

                var bankAccountNrParser = new BankAccountNumberParser(clientConfiguration.Country.BaseCountry);

                var paidToCustomerBankAccountNr = ParseBankAccountOrNull(
                    applicationRow.GetUniqueItem("paidToCustomerBankAccountNr"),
                    applicationRow.GetUniqueItem("paidToCustomerBankAccountNrType"),
                    bankAccountNrParser);
                UnsecuredLoanStandardAgreementPrintContext.PersonalSettlementAccountModel personalSettlementAccount = null;
                if (paidToCustomerBankAccountNr != null && paidToCustomerBankAccountNr.AccountType == BankAccountNumberTypeCode.BankAccountSe)
                {
                    personalSettlementAccount = new UnsecuredLoanStandardAgreementPrintContext.PersonalSettlementAccountModel
                    {
                        AccountNrFormatted = paidToCustomerBankAccountNr.FormatFor("display"),
                        AccountTypeName = TranslateBankAccountTypeName(paidToCustomerBankAccountNr.AccountType),
                        BankName = (paidToCustomerBankAccountNr as BankAccountNumberSe)?.BankName
                    };
                }

                var loansToSettle = ComplexApplicationList
                    .CreateListFromFlattenedItems("LoansToSettle", application.ApplicationListItems)
                    .GetRows()
                    .Where(row => row.GetUniqueItemBoolean("shouldBeSettled") == true)
                    .Select(row =>
                        {
                            var bankAccountNr = ParseBankAccountOrNull(row.GetUniqueItem("bankAccountNr"), row.GetUniqueItem("bankAccountNrType"), bankAccountNrParser);
                            var paymentReference = row.GetUniqueItem("settlementPaymentReference");
                            var paymentMessage = row.GetUniqueItem("settlementPaymentMessage");
                            return new UnsecuredLoanStandardAgreementPrintContext.LoanToSettleModel
                            {
                                AccountNrFormatted = bankAccountNr?.FormatFor("display"),
                                AccountTypeName = TranslateBankAccountTypeName(bankAccountNr?.AccountType),
                                PaymentReference = paymentReference,
                                PaymentMessage = paymentMessage,
                                PaymentReferenceOrMessage = !string.IsNullOrWhiteSpace(paymentReference) ? paymentReference : paymentMessage
                            };
                        })
                    .ToList();

                var selfAffiliate = NEnv.GetAffiliateModels().Single(x => x.IsSelf);
                UnsecuredLoanStandardAgreementPrintContext.ProvidedByModel providedBy = null;
                if (applicationInfo.ProviderName != selfAffiliate.ProviderName)
                {
                    var affiliate = NEnv.GetAffiliateModel(applicationInfo.ProviderName);
                    providedBy = new UnsecuredLoanStandardAgreementPrintContext.ProvidedByModel
                    {
                        ProviderName = affiliate.DisplayToEnduserName,
                        EnduserContactEmail = affiliate.EnduserContactEmail,
                        Address = affiliate.StreetAddress
                    };
                }

                var loanAmount = decisionRow.GetUniqueItemInteger("loanAmount");
                var totalPaidAmount = decisionRow.GetUniqueItemDecimal("totalPaidAmount");
                var totalCostAmount = loanAmount.HasValue && totalPaidAmount.HasValue ? totalPaidAmount.Value - loanAmount.Value : new decimal?();

                var templateService = new SharedStandard.StandardHtmlTemplateService(new PreCreditCustomerClient());
                var generalTermsHtmlRaw = templateService.BuildWeasyPrintHtmlFromSettingsTemplate("generalTermsHtmlTemplate");

                var singlePaymentLoanRepaymentTimeInDays = decisionRow.GetUniqueItemInteger("singlePaymentLoanRepaymentTimeInDays");

                var customCostNameByCode = new Lazy<Dictionary<string, string>>(() =>
                    this.creditClient.GetCustomCosts().ToDictionary(x => CreditRecommendationUlStandardService.FormatFirstNotificationCostAmountDecisionItem(x.Code), x => x.Text));

                var firstNotificationCosts = decisionRow.GetUniqueItemNames().Where(CreditRecommendationUlStandardService.IsFirstNotificationCostAmountDecisionItem).Select(x => new UnsecuredLoanStandardAgreementPrintContext.FirstNotificationCostModel
                {
                    Amount = decisionRow.GetUniqueItemInteger(x)?.ToString("n0", F),
                    CostName = customCostNameByCode.Value?.Opt(x) ?? x
                }).ToList();

                var effectiveInterestRatePercent = decisionRow.GetUniqueItemDecimal("effectiveInterestRatePercent");

                return new UnsecuredLoanStandardAgreementPrintContext
                {
                    CreditNr = creditNr,
                    LoanAmount = loanAmount?.ToString("n0", F),
                    TotalPaidAmount = totalPaidAmount?.ToString(DecimalFormat, F),
                    TotalCostAmount = totalCostAmount?.ToString(DecimalFormat, F),
                    EffectiveInterestRatePercent = effectiveInterestRatePercent?.ToString(DecimalFormat, F),
                    RepaymentTimeInMonths = singlePaymentLoanRepaymentTimeInDays.HasValue
                        ? null 
                        : decisionRow.GetUniqueItemInteger("repaymentTimeInMonths")?.ToString("n0", F),
                    SinglePaymentLoanRepaymentTimeInDays = singlePaymentLoanRepaymentTimeInDays?.ToString("n0", F),
                    AnnuityAmount = singlePaymentLoanRepaymentTimeInDays.HasValue 
                        ? null 
                        : decisionRow.GetUniqueItemDecimal("annuityAmount")?.ToString(DecimalFormat, F),
                    MarginInterestRatePercent = decisionRow.GetUniqueItemDecimal("marginInterestRatePercent")?.ToString(DecimalFormat, F),
                    ReferenceInterestRatePercent = decisionRow.GetUniqueItemDecimal("referenceInterestRatePercent")?.ToString(DecimalFormat, F),
                    TotalInterestRatePercent = (decisionRow.GetUniqueItemDecimal("marginInterestRatePercent") + decisionRow.GetUniqueItemDecimal("referenceInterestRatePercent"))?.ToString(DecimalFormat, F),
                    NotificationFeeAmount = (decisionRow.GetUniqueItemInteger("notificationFeeAmount") ?? 0).ToString("n0", F),
                    InitialFeeWithheldAmount = decisionRow.GetUniqueItemInteger("initialFeeWithheldAmount").HasValue && decisionRow.GetUniqueItemInteger("initialFeeWithheldAmount").Value > 0
                        ? decisionRow.GetUniqueItemInteger("initialFeeWithheldAmount")?.ToString("n0", F)
                        : null,
                    InitialFeeCapitalizedAmount = decisionRow.GetUniqueItemInteger("initialFeeCapitalizedAmount").HasValue && decisionRow.GetUniqueItemInteger("initialFeeCapitalizedAmount").Value > 0
                        ? decisionRow.GetUniqueItemInteger("initialFeeCapitalizedAmount")?.ToString("n0", F)
                        : null,
                    FirstNotificationCosts = firstNotificationCosts,
                    Applicants = Enumerable.Range(1, applicants.NrOfApplicants).Select(applicantNr =>
                        {
                            var applicant = applicants.ApplicantInfoByApplicantNr[applicantNr];
                            return new UnsecuredLoanStandardAgreementPrintContext.ApplicantModel
                            {
                                CivicRegNr = civicRegNrByApplicantNr[applicantNr],
                                AddressSingleLine = $"{applicant.AddressStreet}, {applicant.AddressZipcode} {applicant.AddressCity}",
                                FullName = $"{applicant.FirstName} {applicant.LastName}"
                            };
                        }).ToList(),
                    PersonalSettlementAccount = personalSettlementAccount,
                    LoanToSettle = loansToSettle,
                    HasLoansToSettle = loansToSettle.Count > 0 ? "true" : null,
                    IsSwedishHighCostCredit = clientConfiguration.Country.BaseCountry == "SE" && effectiveInterestRatePercent.GetValueOrDefault() > 30m ? "true" : null,
                    EnduserContactEmail = selfAffiliate.EnduserContactEmail,
                    ProvidedBy = providedBy,
                    ReminderFeeAmount = (notificationProcessSettings.ReminderFeeAmount ?? 0m).ToString(DecimalFormat, F),
                    GeneralTermsRawHtml = generalTermsHtmlRaw
                };
            }
        }

        private string TranslateBankAccountTypeName(BankAccountNumberTypeCode? typeCode)
        {
            if (typeCode == null)
                return null;

            switch (typeCode.Value)
            {
                case BankAccountNumberTypeCode.BankAccountSe: return "Personkonto";
                case BankAccountNumberTypeCode.BankGiroSe: return "Bankgiro";
                case BankAccountNumberTypeCode.PlusGiroSe: return "Plusgiro";
                default: return typeCode.ToString();
            }
        }

        private IBankAccountNumber ParseBankAccountOrNull(string nr, string typeCode, BankAccountNumberParser parser)
        {
            if (nr == null)
                return null;

            if (typeCode == null)
                return parser.TryParseFromStringWithDefaults(nr, typeCode, out var parsedNr) ? parsedNr : null;

            var typeCodeParsed = Enums.Parse<BankAccountNumberTypeCode>(typeCode, ignoreCase: true);
            if (!typeCodeParsed.HasValue)
                return null;

            return parser.TryParseBankAccount(nr, typeCodeParsed.Value, out var parsedNr2) ? parsedNr2 : null;
        }
    }
}