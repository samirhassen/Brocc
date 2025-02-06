using NTech;
using NTech.Banking.BankAccounts;
using NTech.Banking.Conversion;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace nPreCredit.Code.Services.CompanyLoans
{
    public class CompanyLoanAgreementService : ICompanyLoanAgreementService
    {
        private readonly ICompanyLoanWorkflowService companyLoanWorkflowService;
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;
        private readonly ICustomerClient customerClient;
        private readonly ICreditClient creditClient;
        private readonly UpdateCreditApplicationRepository updateCreditApplicationRepository;
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;
        private readonly IClientConfiguration clientConfiguration;
        private readonly IClock clock;
        private readonly ICreditApplicationCustomerListService creditApplicationCustomerListService;
        private readonly IKeyValueStoreService keyValueStoreService;
        private readonly Lazy<CultureInfo> formattingCulture;

        public CompanyLoanAgreementService(ICompanyLoanWorkflowService companyLoanWorkflowService, IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository, ICustomerClient customerClient, ICreditClient creditClient, UpdateCreditApplicationRepository updateCreditApplicationRepository, INTechCurrentUserMetadata ntechCurrentUserMetadata, IClientConfiguration clientConfiguration, IClock clock, ICreditApplicationCustomerListService creditApplicationCustomerListService, IKeyValueStoreService keyValueStoreService)
        {
            this.companyLoanWorkflowService = companyLoanWorkflowService;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
            this.customerClient = customerClient;
            this.creditClient = creditClient;
            this.updateCreditApplicationRepository = updateCreditApplicationRepository;
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
            this.clientConfiguration = clientConfiguration;
            this.clock = clock;
            this.creditApplicationCustomerListService = creditApplicationCustomerListService;
            this.keyValueStoreService = keyValueStoreService;
            this.formattingCulture = new Lazy<CultureInfo>(() => CultureInfo.GetCultureInfo(clientConfiguration.Country.BaseFormattingCulture));
        }

        protected CultureInfo F => this.formattingCulture.Value;

        public string EnsureCreditNr(string applicationNr)
        {
            var creditNr = this.partialCreditApplicationModelRepository.Get(applicationNr, new PartialCreditApplicationModelRequest
            {
                ApplicationFields = new List<string> { "creditnr" },
                ErrorIfGetNonLoadedField = true
            }).Application.Get("creditnr").StringValue.Optional;
            if (!string.IsNullOrWhiteSpace(creditNr))
                return creditNr;

            creditNr = this.creditClient.NewCreditNumber();

            updateCreditApplicationRepository.UpdateApplication(applicationNr, new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest
            {
                InformationMetadata = this.ntechCurrentUserMetadata.InformationMetadata,
                UpdatedByUserId = this.ntechCurrentUserMetadata.UserId,
                StepName = "CompanyLoanAgreement",
                Items = new List<UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem>
                {
                    new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                    {
                        GroupName = "application",
                        Name = "creditnr",
                        Value = creditNr,
                        IsSensitive = false
                    }
                }
            });

            return creditNr;
        }

        public MemoryStream CreateAgreementPdf(CompanyLoanAgreementPrintContextModel context, string overrideTemplateName = null, bool? disableTemplateCache = false)
        {
            var dc = new nDocumentClient();

            var pdfBytes = dc.PdfRenderDirect(
                overrideTemplateName ?? "companyloan-agreement",
                PdfCreator.ToTemplateContext(context),
                disableTemplateCache: disableTemplateCache.GetValueOrDefault());

            return new MemoryStream(pdfBytes);
        }

        public CompanyLoanAgreementPrintContextModel GetPrintContext(ApplicationInfoModel applicationInfo)
        {
            var applicationNr = applicationInfo.ApplicationNr;

            //---------------------------------------------------------------
            //----------------- Credit application repository----------------
            //---------------------------------------------------------------
            var app = this.partialCreditApplicationModelRepository.Get(applicationNr, new PartialCreditApplicationModelRequest
            {
                ApplicationFields = new List<string>
                {
                    "companyCustomerId", "creditnr", "bankAccountNr", "bankAccountNrType","companyYearlyRevenue","loanPurposeCode"
                },
                ErrorIfGetNonLoadedField = true
            });

            var companyCustomerId = app.Application.Get("companyCustomerId").IntValue.Required;

            var p = new BankAccountNumberParser(clientConfiguration.Country.BaseCountry);

            if (!p.TryParseFromStringWithDefaults(
                app.Application.Get("bankAccountNr").StringValue.Required,
                app.Application.Get("bankAccountNrType").StringValue.Optional,
                out var bankAccount))
                throw new NTechWebserviceMethodException("Invalid bankAccountNr")
                {
                    ErrorCode = "invalidBankAccountNr",
                    ErrorHttpStatusCode = 400,
                    IsUserFacing = true
                };

            //---------------------------------------------------------------
            //----------------- Raw application -----------------------------
            //---------------------------------------------------------------
            Services.CompanyLoans.CompanyLoanCreditDecisionModel decisionModel;

            using (var context = new PreCreditContext())
            {
                var h = context
                    .CreditApplicationHeaders.Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new
                    {
                        x.CurrentCreditDecision
                    })
                    .Single();

                var d = h.CurrentCreditDecision;
                var decisionModelRaw = ((d as AcceptedCreditDecision)?.AcceptedDecisionModel) ?? ((d as RejectedCreditDecision)?.RejectedDecisionModel);
                if (string.IsNullOrWhiteSpace(decisionModelRaw))
                    throw new NTechWebserviceMethodException("No current credit decision exists")
                    {
                        ErrorCode = "missingCreditDecision",
                        ErrorHttpStatusCode = 400,
                        IsUserFacing = true
                    };

                decisionModel = Code.CreditDecisionModelParser.ParseCompanyLoanCreditDecision(decisionModelRaw);
            }

            var companyLoanCollateralCustomerIds = this.creditApplicationCustomerListService.GetMemberCustomerIds(applicationNr, "companyLoanCollateral");
            var companyLoanBeneficialOwnerCustomerIds = this.creditApplicationCustomerListService.GetMemberCustomerIds(applicationNr, "companyLoanBeneficialOwner");

            //---------------------------------------------------------------
            //----------------- Customer module -----------------------------
            //---------------------------------------------------------------
            var customerIds = new HashSet<int> { companyCustomerId };
            customerIds.UnionWith(companyLoanCollateralCustomerIds);
            customerIds.UnionWith(companyLoanBeneficialOwnerCustomerIds);

            var customerData = this.customerClient.BulkFetchPropertiesByCustomerIdsD(customerIds,
                "orgnr", "companyName", "addressStreet", "addressCity", "addressZipcode", "civicRegNr", "firstName", "lastName", "isCompany");

            Func<int, string, string> custProp = (x, y) => customerData.Opt(x).Opt(y);

            var creditNr = app.Application.Get("creditnr").StringValue.Optional ?? EnsureCreditNr(applicationInfo.ApplicationNr);
            var extOffer = decisionModel.GetExtendedOfferModel(NEnv.EnvSettings);

            var marginInterestRatePercentPerYear = extOffer.NominalInterestRatePercent / 100m;
            var referenceInterestRatePercentPerYear = (extOffer.ReferenceInterestRatePercent ?? 0m) / 100m;
            var totalInterestRatePercentPerYear = marginInterestRatePercentPerYear + referenceInterestRatePercentPerYear;
            var additionalQuestionsAnswers = WebserviceMethods.CompanyLoans.FetchCompanyLoanApplicationAdditionalQuestionsAnswers.GetAnswers(applicationNr, keyValueStoreService);

            var m = new CompanyLoanAgreementPrintContextModel
            {
                PrintDate = clock.Now.ToString("yyyy-MM-dd")
            };

            //Extra questions
            var companySector = additionalQuestionsAnswers.Items.Where(x => x.QuestionCode == "companySector").Select(x => x.AnswerText).FirstOrDefault();
            var companyEmployeeCount = additionalQuestionsAnswers.Items.Where(x => x.QuestionCode == "companyEmployeeCount").Select(x => x.AnswerText).FirstOrDefault();
            var isPaymentServiceProvider = additionalQuestionsAnswers.Items.Where(x => x.QuestionCode == "isPaymentServiceProvider").Select(x => x.AnswerText).FirstOrDefault();
            var paymentSource = additionalQuestionsAnswers.Items.Where(x => x.QuestionCode == "paymentSource").Select(x => x.AnswerText).FirstOrDefault();
            var extraPayments = additionalQuestionsAnswers.Items.Where(x => x.QuestionCode == "extraPayments").Select(x => x.AnswerText).FirstOrDefault();
            var isAnyPep = additionalQuestionsAnswers.Items.Where(x => x.QuestionCode == "isPep").Any(x => x.AnswerCode == "true") ? "true" : null;

            var auditor = decisionModel?.Recommendation?.ScoringData?.GetString("creditReportStyrelseRevisorKod", null);

            if (auditor != null && (auditor.Equals("Auktoriserad revisor") || auditor.Equals("Godkänd revisor")))
                auditor = "Ja";
            else
                auditor = "Nej";
            if (string.IsNullOrWhiteSpace(auditor))
                auditor = "Okänd";


            m.CompanyCustomer = new CompanyLoanAgreementPrintContextModel.CompanyCustomerModel
            {
                Name = custProp(companyCustomerId, "companyName"),
                Orgnr = custProp(companyCustomerId, "orgnr"),
                StreetAddress = custProp(companyCustomerId, "addressStreet"),
                ZipcodeAndCityAddress = $"{custProp(companyCustomerId, "addressZipcode")} {custProp(companyCustomerId, "addressCity")}".Trim(),
                CompanyEmployeeCount = companyEmployeeCount,
                CompanySector = companySector,
                PaymentSource = paymentSource,
                CompanyYearlyRevenue = app.Application.Get("companyYearlyRevenue").DecimalValue.Optional?.ToString("N0", F),
                LoanPurposeCode = app.Application.Get("loanPurposeCode").StringValue.Optional,
                IsPaymentServiceProvider = isPaymentServiceProvider,
                ExtraPayments = extraPayments,
                Auditor = auditor,
                IsAnyPep = isAnyPep
            };

            m.LoanDetails = new CompanyLoanAgreementPrintContextModel.LoanDetailsModel
            {
                LoanNr = creditNr,
                LoanAmount = extOffer.LoanAmount?.ToString("C", F),
                RepaymentTimeInMonths = extOffer.RepaymentTimeInMonths?.ToString(F),
                AnnuityAmount = extOffer.AnnuityAmount?.ToString("C", F),
                MonthlyAmountIncludingFees = (extOffer.AnnuityAmount + extOffer.MonthlyFeeAmount)?.ToString("C", F),
                MarginInterestRatePercentPerYear = marginInterestRatePercentPerYear?.ToString("P", F),
                ReferenceInterestRatePercentPerYear = referenceInterestRatePercentPerYear.ToString("P", F),
                TotalInterestRatePercentPerYear = totalInterestRatePercentPerYear?.ToString("P", F),
                MarginInterestRatePercentPerMonth = (marginInterestRatePercentPerYear / 12m)?.ToString("P", F),
                ReferenceInterestRatePercentPerMonth = (referenceInterestRatePercentPerYear / 12m).ToString("P", F),
                TotalInterestRatePercentPerMonth = (totalInterestRatePercentPerYear / 12m)?.ToString("P", F),
                EffectiveInterestRatePercentPerYear = (extOffer.EffectiveInterestRatePercent / 100m)?.ToString("P", F),
                EffectiveInterestRatePercentPerMonth = (extOffer.EffectiveInterestRatePercent / 100m / 12m)?.ToString("P", F),
                MonthlyFeeAmount = extOffer.MonthlyFeeAmount?.ToString("C", F),
                InitialFeeAmount = extOffer.InitialFeeAmount?.ToString("C", F),
                TotalPaidAmount = extOffer.TotalPaidAmount?.ToString("C", F)
            };

            HandleCollaterals(companyLoanCollateralCustomerIds, custProp, m);
            HandleBeneficialOwners(companyLoanBeneficialOwnerCustomerIds, custProp, m, additionalQuestionsAnswers);
            HandleBankAccount(bankAccount, m);
            HandlePepPersons(custProp, m, additionalQuestionsAnswers);
            HandleCashHandling(additionalQuestionsAnswers, m);
            HahdleCurrencyExchange(additionalQuestionsAnswers, m);

            return m;
        }

        private static void HahdleCurrencyExchange(AdditionalQuestionsDocumentModel additionalQuestionsAnswers, CompanyLoanAgreementPrintContextModel m)
        {
            var hasCurrencyExchange = additionalQuestionsAnswers.Items.Where(x => x.QuestionCode == "hasCurrencyExchange").Select(x => x.AnswerCode).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(hasCurrencyExchange))
            {
                var currencyExchangeDescription = additionalQuestionsAnswers.Items.Where(x => x.QuestionCode == "currencyExchangeDescription").Select(x => x.AnswerText).FirstOrDefault();
                m.CurrencyExchange = new CompanyLoanAgreementPrintContextModel.CurrencyExchangeModel
                {
                    HasCurrencyExchange = hasCurrencyExchange == "true",
                    Description = currencyExchangeDescription
                };
            }
        }

        private void HandleCashHandling(AdditionalQuestionsDocumentModel additionalQuestionsAnswers, CompanyLoanAgreementPrintContextModel m)
        {
            var hasCashHandling = additionalQuestionsAnswers.Items.Where(x => x.QuestionCode == "hasCashHandling").Select(x => x.AnswerCode).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(hasCashHandling))
            {
                var cashHandlingDescription = additionalQuestionsAnswers.Items.Where(x => x.QuestionCode == "cashHandlingDescription").Select(x => x.AnswerText).FirstOrDefault();
                var cashHandlingYearlyVolumeRaw = additionalQuestionsAnswers.Items.Where(x => x.QuestionCode == "cashHandlingYearlyVolume").Select(x => x.AnswerCode).FirstOrDefault();
                decimal? cashHandlingYearlyVolume = null;
                if (!string.IsNullOrWhiteSpace(cashHandlingYearlyVolumeRaw))
                    cashHandlingYearlyVolume = Numbers.ParseDecimalOrNull(cashHandlingYearlyVolumeRaw);

                m.CashHandling = new CompanyLoanAgreementPrintContextModel.CashHandlingModel
                {
                    HasCashHandling = hasCashHandling == "true",
                    Description = cashHandlingDescription,
                    YearlyVolume = cashHandlingYearlyVolume?.ToString("N0", F)
                };
            }
        }

        private static void HandleCollaterals(List<int> companyLoanCollateralCustomerIds, Func<int, string, string> custProp, CompanyLoanAgreementPrintContextModel m)
        {
            m.Collaterals = companyLoanCollateralCustomerIds.Select(x => new CompanyLoanAgreementPrintContextModel.CollateralModel
            {
                CivicRegNr = custProp(x, "civicRegNr"),
                FirstName = custProp(x, "firstName"),
                LastName = custProp(x, "lastName"),
                FullName = $"{custProp(x, "firstName")} {custProp(x, "lastName")}".Trim()
            }).ToList();
            m.HasCollaterals = m.Collaterals.Count > 0;
        }

        private void HandleBeneficialOwners(List<int> companyLoanBeneficialOwnerCustomerIds, Func<int, string, string> custProp, CompanyLoanAgreementPrintContextModel m, AdditionalQuestionsDocumentModel additionalQuestionsAnswers)
        {
            var beneficialOwnerConnectionByCustomerId = additionalQuestionsAnswers
                .Items
                .Where(x => x.CustomerId.HasValue && x.QuestionCode == "beneficialOwnerConnection" && !string.IsNullOrWhiteSpace(x.AnswerText))
                .GroupBy(x => x.CustomerId.Value)
                .ToDictionary(x => x.Key, x => x.First().AnswerText);

            var beneficialOwnerPercentByCustomerId = additionalQuestionsAnswers
                .Items
                .Where(x => x.CustomerId.HasValue && x.QuestionCode == "beneficialOwnerOwnershipPercent" && !string.IsNullOrWhiteSpace(x.AnswerText))
                .GroupBy(x => x.CustomerId.Value)
                .ToDictionary(x => x.Key, x => decimal.Parse(x.First().AnswerText, CultureInfo.InvariantCulture));

            var isUsPersonCustomerId = additionalQuestionsAnswers
               .Items
               .Where(x => x.CustomerId.HasValue && x.QuestionCode == "answeredYesOnIsUSPersonQuestion" && x.AnswerCode.Equals("true"))
               .GroupBy(x => x.CustomerId.Value)
               .ToDictionary(x => x.Key, x => x.First().AnswerCode);

            m.PercentBeneficialOwners = new List<CompanyLoanAgreementPrintContextModel.PercentBeneficialOwnerModel>();
            m.ConnectionBeneficialOwners = new List<CompanyLoanAgreementPrintContextModel.ConnectionBeneficialOwnerModel>();
            m.UsPersonOwners = new List<CompanyLoanAgreementPrintContextModel.IsUsPersonBeneficialOwnerModel>();

            m.HasBeneficialOwners = m.Collaterals.Count > 0;
            foreach (var customerId in companyLoanBeneficialOwnerCustomerIds)
            {
                if (beneficialOwnerPercentByCustomerId.ContainsKey(customerId))
                {
                    var o = new CompanyLoanAgreementPrintContextModel.PercentBeneficialOwnerModel
                    {

                        OwnershipPercent = (beneficialOwnerPercentByCustomerId[customerId]).ToString("G29", F),
                    };
                    m.PercentBeneficialOwners.Add(o);
                    UpdatePersonBaseModel(o, custProp, customerId);

                }
                else if (beneficialOwnerConnectionByCustomerId.ContainsKey(customerId))
                {
                    var o = new CompanyLoanAgreementPrintContextModel.ConnectionBeneficialOwnerModel
                    {
                        ConnectionText = beneficialOwnerConnectionByCustomerId[customerId]
                    };
                    m.ConnectionBeneficialOwners.Add(o);
                    UpdatePersonBaseModel(o, custProp, customerId);
                }
                //Remaining companyLoanBeneficialOwnerCustomerIds shown in list with empty OwnershipPercent
                else
                {
                    var o = new CompanyLoanAgreementPrintContextModel.PercentBeneficialOwnerModel
                    {
                        OwnershipPercent = string.Empty
                    };
                    m.PercentBeneficialOwners.Add(o);
                    UpdatePersonBaseModel(o, custProp, customerId);
                }
            }
            foreach (var customerId in companyLoanBeneficialOwnerCustomerIds)
            {

                if (isUsPersonCustomerId.ContainsKey(customerId))
                {
                    var o = new CompanyLoanAgreementPrintContextModel.IsUsPersonBeneficialOwnerModel();

                    m.UsPersonOwners.Add(o);
                    UpdatePersonBaseModel(o, custProp, customerId);

                }
            }
            m.HasBeneficialOwners = m.PercentBeneficialOwners.Count > 0;
            m.HasUsPersonOwners = m.UsPersonOwners.Count > 0;
        }

        public void UpdatePersonBaseModel(CompanyLoanAgreementPrintContextModel.PersonBaseModel pb, Func<int, string, string> custProp, int customerId)
        {
            pb.CivicRegNr = custProp(customerId, "civicRegNr");
            pb.FirstName = custProp(customerId, "firstName");
            pb.LastName = custProp(customerId, "lastName");
            pb.FullName = $"{custProp(customerId, "firstName")} {custProp(customerId, "lastName")}".Trim();

        }

        private void HandleBankAccount(IBankAccountNumber bankAccount, CompanyLoanAgreementPrintContextModel m)
        {
            m.BankAccount = new CompanyLoanAgreementPrintContextModel.BankAccountModel
            {
                DisplayNr = bankAccount.FormatFor("display"),
                AccountType = bankAccount.AccountType.ToString(),
                AccountTypeFlags = new ExpandoObject(),
                NormalizedNr = bankAccount.FormatFor(null)
            };
            m.BankAccount.AccountTypeFlags.SetValues(x =>
            {
                foreach (var a in Enums.GetAllValues<BankAccountNumberTypeCode>())
                    x[a.ToString()] = a == bankAccount.AccountType;
            });
        }

        private void HandlePepPersons(Func<int, string, string> custProp, CompanyLoanAgreementPrintContextModel m, AdditionalQuestionsDocumentModel additionalQuestionsAnswers)
        {
            var pepCustomerIds = additionalQuestionsAnswers
                .Items
                .Where(x => x.CustomerId.HasValue && x.QuestionCode == "isPep" && x.AnswerCode == "true")
                .Select(x => x.CustomerId.Value)
                .Distinct()
                .ToList();

            var pepRoleByCustomerId = additionalQuestionsAnswers
                .Items
                .Where(x => x.CustomerId.HasValue && x.QuestionCode == "pepWho" && !string.IsNullOrWhiteSpace(x.AnswerText))
                .GroupBy(x => x.CustomerId.Value)
                .ToDictionary(x => x.Key, x => x.First().AnswerText);
            m.PepPersons = pepCustomerIds.Select(customerId => new CompanyLoanAgreementPrintContextModel.PepPersonModel
            {
                CivicRegNr = custProp(customerId, "civicRegNr"),
                FirstName = custProp(customerId, "firstName"),
                LastName = custProp(customerId, "lastName"),
                FullName = $"{custProp(customerId, "firstName")} {custProp(customerId, "lastName")}".Trim(),
                PepRole = pepRoleByCustomerId?.Opt(customerId)
            }).ToList();
            m.HasPepPersons = m.PepPersons.Count > 0;
        }
    }

    public interface ICompanyLoanAgreementService
    {
        CompanyLoanAgreementPrintContextModel GetPrintContext(ApplicationInfoModel applicationInfo);
        string EnsureCreditNr(string applicationNr);
        MemoryStream CreateAgreementPdf(CompanyLoanAgreementPrintContextModel context, string overrideTemplateName = null, bool? disableTemplateCache = false);
    }
}