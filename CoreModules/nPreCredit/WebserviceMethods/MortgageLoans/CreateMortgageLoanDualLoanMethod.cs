using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Datasources;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.MortgageLoans;
using NTech;
using NTech.Banking.LoanModel;
using NTech.Core.PreCredit.Shared;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoans
{
    public class CreateMortgageLoanDualLoanMethod : TypedWebserviceMethod<CreateMortgageLoanDualLoanMethod.Request, CreateMortgageLoanDualLoanMethod.Response>
    {
        public override string Path => "MortgageLoan/Create-Dual-Loan";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (NEnv.ClientCfg.Country.BaseCountry != "FI")
                throw new Exception("To support other countries than FI add handling of the different kinds of bank accounts");

            var r = requestContext.Resolver();

            var infoService = r.Resolve<ApplicationInfoService>();

            var ai = infoService.GetApplicationInfo(request.ApplicationNr);
            if (ai == null)
                return Error("No such application", errorCode: "noSuchApplication");

            var applicants = infoService.GetApplicationApplicants(ai.ApplicationNr);

            var wf = r.Resolve<IMortgageLoanWorkflowService>();

            var currentListName = wf.GetCurrentListName(ai.ListNames);
            if (!wf.TryDecomposeListName(currentListName, out var names))
            {
                throw new Exception("Invalid application. Current list name is broken.");
            }
            var currentStepName = names.Item1;

            var settlementStep = wf.Model.FindStepByCustomData(x => x?.IsSettlement == "yes", new { IsSettlement = "" });

            if (settlementStep == null)
                throw new Exception("There needs to be a step in the workflow with CustomData item IsSettlement = \"yes\"");

            var isSettlementStep = currentStepName == settlementStep.Name;
            if (!isSettlementStep || !ai.IsActive || ai.IsFinalDecisionMade || !ai.HasLockedAgreement)
                return Error("No on the settlement step", errorCode: "wrongStatus");

            var ds = r.Resolve<ApplicationDataSourceService>();

            var extraItems = ds.NewSimpleRequest(CreditApplicationItemDataSource.DataSourceNameShared,
                "application.outgoingPaymentFileStatus", "application.mainLoanCreditNr", "application.childLoanCreditNr", "application.providerApplicationId",
                "application.requestedDueDay", "application.outgoingPaymentFileCreationDate",
                "applicant1.monthlyIncome", "applicant2.monthlyIncome");

            extraItems = ds.AppendToSimpleRequest(extraItems, CurrentCreditDecisionItemsDataSource.DataSourceNameShared, "*");

            extraItems = ds.AppendToSimpleRequest(extraItems, ComplexApplicationListDataSource.DataSourceNameShared,
                "ApplicationObject#*#*#*");

            ApplicationDataSourceResult dataItems = null;
            if (!CreateMortgageLoanDualSettlementPaymentsFileMethod.TryGetPaymentsPlus(request.ApplicationNr, ds, out var payments, out var errorMessage, extras: extraItems, observeResult: x => dataItems = x))
            {
                return Error(errorMessage, errorCode: "incorrectPayments", httpStatusCode: 400);
            }

            var application = dataItems.DataSource(CreditApplicationItemDataSource.DataSourceNameShared);
            var creditDecision = dataItems.DataSource(CurrentCreditDecisionItemsDataSource.DataSourceNameShared);
            var complexList = dataItems.DataSource(ComplexApplicationListDataSource.DataSourceNameShared);

            var outgoingPaymentFileStatus = application.Item("application.outgoingPaymentFileStatus").StringValue.Optional;

            if (outgoingPaymentFileStatus != "pending")
            {
                return Error("Settlement status is not 'pending'", errorCode: "wrongStatus");
            }

            if (creditDecision.Item("decisionType").StringValue.Optional != "Final")
            {
                return Error("Current credit decision is missing or not Final", errorCode: "wrongCreditDecisionType");
            }

            var commentParts = new List<string>();

            ApplicationDocumentModel GetSignedAgreement(int customerId, string applicationNr)
            {
                var signedDocumentTypeCode = CreditApplicationDocumentTypeCode.SignedAgreement.ToString();
                var documentTypes = new List<string> { signedDocumentTypeCode };
                var documentService = r.Resolve<IApplicationDocumentService>();
                var signedAgreements = documentService.FetchForApplication(applicationNr, documentTypes);
                return signedAgreements.OrderByDescending(d => d.DocumentId).FirstOrDefault(y =>
                    y.CustomerId == customerId && y.DocumentType == signedDocumentTypeCode);
            }

            //Get customers
            var customers = applicants.AllConnectedCustomerIdsWithRoles.Select(c => new
            {
                CustomerId = c.Key,
                IsApplicant = c.Value.Contains("Applicant"),
                IsCollateral = c.Value.Contains("ApplicationObject"),
                Agreement = GetSignedAgreement(c.Key, request.ApplicationNr)
            }).ToList();

            var currentReferenceInterestRate = r.Resolve<IReferenceInterestRateService>().GetCurrent();

            var mainPaymentAmount = 0m;
            var childPaymentAmount = 0m;
            var mainFeeAmount = 0m;
            var childFeeAmount = 0m;

            var mortgageLoanRequests = new List<MortgageLoanRequest>();
            foreach (var isMain in Enumerables.Array(true, false))
            {
                if (payments.All(x => x.IsMain != isMain))
                {
                    continue;
                }

                var createLoanRequest = new MortgageLoanRequest
                {
                    CreditNr = application.Item($"application.{(isMain ? "main" : "child")}LoanCreditNr").StringValue.Required,
                    ApplicationNr = request.ApplicationNr,
                    Applicants = Enumerable.Range(1, ai.NrOfApplicants).Select(applicantNr => new MortgageLoanRequest.Applicant
                    {
                        ApplicantNr = applicantNr,
                        CustomerId = applicants.CustomerIdByApplicantNr[applicantNr],
                        AgreementPdfArchiveKey = customers.SingleOrDefault(x => x.CustomerId == applicants.CustomerIdByApplicantNr[applicantNr])?.Agreement?.DocumentArchiveKey
                    }).ToList(),
                    MonthlyFeeAmount = creditDecision.Item($"{(isMain ? "main" : "child")}NotificationFeeAmount").DecimalValue.Required,
                    NrOfApplicants = ai.NrOfApplicants,
                    ProviderApplicationId = application.Item("application.providerApplicationId").StringValue.Optional,
                    CurrentObjectValue = complexList.Item("ApplicationObject#1#u#valuationAmount").DecimalValue.Optional,
                    CurrentObjectValueDate = complexList.Item("ApplicationObject#1#u#valuationDate").DateValue.Optional?.Date,
                    SettlementDate = DateTime.ParseExact(application.Item("application.outgoingPaymentFileCreationDate").StringValue.Required, "o", CultureInfo.InvariantCulture),
                    NominalInterestRatePercent = creditDecision.Item($"{(isMain ? "main" : "child")}MarginInterestRatePercent").DecimalValue.Required,
                    ProviderName = ai.ProviderName,
                    LoanAmountParts = new List<MortgageLoanRequest.AmountModel>(),
                    IsForNonPropertyUse = !isMain,
                    NotificationDueDay = application.Item("application.requestedDueDay").IntValue.Optional ?? 28,
                    ReferenceInterestRate = currentReferenceInterestRate,
                    Documents = new List<MortgageLoanRequest.Document>()
                };

                var paymentsAmount = 0m;
                foreach (var payment in payments.Where(x => x.IsMain == isMain).ToList())
                {
                    if (isMain)
                        mainPaymentAmount += payment.PaymentAmount;
                    else
                        childPaymentAmount += payment.PaymentAmount;

                    paymentsAmount += payment.PaymentAmount;

                    createLoanRequest.LoanAmountParts.Add(new MortgageLoanRequest.AmountModel
                    {
                        Amount = payment.PaymentAmount,
                        SubAccountCode = "loan"
                    });
                }

                if (isMain)
                    mainPaymentAmount = paymentsAmount;
                else
                    childPaymentAmount = paymentsAmount;

                decimal AddFee(MortgageLoanRequest rq, string code, decimal? amount)
                {
                    if (!(amount.HasValue && amount.Value > 0m)) return 0m;

                    rq.LoanAmountParts.Add(new MortgageLoanRequest.AmountModel { SubAccountCode = code, Amount = amount.Value });

                    return amount.Value;
                }

                var feeAmount = 0m;
                if (isMain)
                {
                    feeAmount += AddFee(createLoanRequest, "initialFee", creditDecision.Item("mainInitialFeeAmount").DecimalValue.Optional);
                    feeAmount += AddFee(createLoanRequest, "valuationFee", creditDecision.Item("mainValuationFeeAmount").DecimalValue.Optional);
                    feeAmount += AddFee(createLoanRequest, "deedFee", creditDecision.Item("mainDeedFeeAmount").DecimalValue.Optional);
                    feeAmount += AddFee(createLoanRequest, "mortgageApplicationFee", creditDecision.Item("mainMortgageApplicationFeeAmount").DecimalValue.Optional);
                }
                else
                {
                    feeAmount += AddFee(createLoanRequest, "initialFee", creditDecision.Item("childInitialFeeAmount").DecimalValue.Optional);
                }

                var expectedFeeAmount = creditDecision.Item($"{(isMain ? "main" : "child")}TotalInitialFeeAmount").DecimalValue.Required;
                if (expectedFeeAmount != feeAmount)
                    throw new Exception($"There are initial fees that are not handled on application {request.ApplicationNr}");

                if (isMain)
                    mainFeeAmount = feeAmount;
                else
                    childFeeAmount = feeAmount;

                var loanAmount = paymentsAmount + feeAmount;

                var paymentPlan = PaymentPlanCalculation.BeginCreateWithRepaymentTime(
                        createLoanRequest.LoanAmountParts.Sum(x => x.Amount),
                        creditDecision.Item($"{(isMain ? "main" : "child")}RepaymentTimeInMonths").IntValue.Required,
                        createLoanRequest.NominalInterestRatePercent + createLoanRequest.ReferenceInterestRate.Value,
                        true, null, NEnv.CreditsUse360DayInterestYear)
                    .WithMonthlyFee(createLoanRequest.MonthlyFeeAmount)
                    .EndCreate();

                if (NEnv.ClientCfg.Country.BaseCountry == "SE")
                {
                    createLoanRequest.EndDate = Dates.GetNextDateWithDayNrAfterDate(createLoanRequest.NotificationDueDay.Value, requestContext.Clock().Today)
                        .AddMonths(paymentPlan.Payments.Count);
                }

                createLoanRequest.AnnuityAmount = paymentPlan.AnnuityAmount;

                commentParts.Add($"{(isMain ? "Mortgage" : "Other")} Loan {createLoanRequest.CreditNr} created with initial capital {loanAmount.ToString(CultureInfo.InvariantCulture)}");

                createLoanRequest.CurrentCombinedYearlyIncomeAmount = 12m * Enumerable.Range(1, ai.NrOfApplicants).Sum(applicantNr => application.Item($"applicant{applicantNr}.monthlyIncome").DecimalValue.Optional ?? 0m);

                foreach (var collateralOnlyCustomer in customers.Where(x => x.IsCollateral && !x.IsApplicant && x.Agreement != null))
                {
                    createLoanRequest.Documents.Add(new MortgageLoanRequest.Document
                    {
                        ArchiveKey = collateralOnlyCustomer.Agreement.DocumentArchiveKey,
                        DocumentType = "CollateralOnlySignedAgreement"
                    });
                }

                createLoanRequest.Collaterals = new MortgageLoanRequest.MortgageLoanCollateralsModel
                {
                    Collaterals = CreateCollateralModel(
                    r.Resolve<ICreditApplicationCustomEditableFieldsService>(),
                    complexList)
                };

                mortgageLoanRequests.Add(createLoanRequest);
            }

            if (mortgageLoanRequests.Count == 0)
                return Error("There are no loans to create", errorCode: "noLoans");

            var creditClient = r.Resolve<ICreditClient>();

            if (mortgageLoanRequests.Count > 1)
            {
                var ocr = creditClient.GenerateReferenceNumbers(0, 1).Item2.Single();
                var mainLoan = mortgageLoanRequests.Single(x => !x.IsForNonPropertyUse);
                var childLoan = mortgageLoanRequests.Single(x => x.IsForNonPropertyUse);
                mainLoan.SharedOcrPaymentReference = ocr;
                childLoan.SharedOcrPaymentReference = ocr;
                childLoan.MainCreditCreditNr = mainLoan.CreditNr;
            }

            var u = requestContext.CurrentUserMetadata();
            var repo = requestContext.Resolver().Resolve<UpdateCreditApplicationRepository>();
            repo.UpdateApplication(
                request.ApplicationNr,
                new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest
                {
                    InformationMetadata = u.InformationMetadata,
                    StepName = currentStepName,
                    UpdatedByUserId = u.UserId,
                    Items = new List<UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem>
                    {
                        new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                        {
                            GroupName = "application",
                            Name = "outgoingPaymentFileStatus",
                            Value = "done"
                        },
                        new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                        {
                            GroupName = "application",
                            Name = "loanCreationDate",
                            Value = requestContext.Clock().Now.ToString("o")
                        }
                    }
                },
                also: context =>
                {
                    var h = context.CreditApplicationHeadersQueryable.Single(x => x.ApplicationNr == request.ApplicationNr);
                    h.IsFinalDecisionMade = true;
                    h.FinalDecisionById = u.UserId;
                    h.IsActive = false;
                    h.FinalDecisionDate = requestContext.Clock().Now;
                    wf.ChangeStepStatusComposable(context, currentStepName, wf.AcceptedStatusName, application: h);
                    context.CreateAndAddComment(string.Join(", ", commentParts), currentStepName, creditApplicationHeader: h);
                    creditClient.CreateMortgageLoans(mortgageLoanRequests.ToArray());

                    var mainInitialCapitalDebt = mainFeeAmount + mainPaymentAmount;
                    var childInitialCapitalDebt = childFeeAmount + childPaymentAmount;

                    var totalInitialCapitalDebt = mainInitialCapitalDebt + childInitialCapitalDebt;
                    var totalPaymentAmount = mainPaymentAmount + childPaymentAmount;
                    var totalFeeAmount = mainFeeAmount + childFeeAmount;

                    var settlementItems = new Dictionary<string, string>
                    {
                        {"MainInitialCapitalDebt", mainInitialCapitalDebt.ToString(CultureInfo.InvariantCulture) },
                        {"MainPaymentAmount", mainPaymentAmount.ToString(CultureInfo.InvariantCulture) },
                        {"MainFeeAmount", mainFeeAmount.ToString(CultureInfo.InvariantCulture) },

                        {"ChildInitialCapitalDebt", childInitialCapitalDebt.ToString(CultureInfo.InvariantCulture) },
                        {"ChildPaymentAmount", childPaymentAmount.ToString(CultureInfo.InvariantCulture) },
                        {"ChildFeeAmount", childFeeAmount.ToString(CultureInfo.InvariantCulture) },

                        {"TotalInitialCapitalDebt", totalInitialCapitalDebt.ToString(CultureInfo.InvariantCulture) },
                        {"TotalPaymentAmount", totalPaymentAmount.ToString(CultureInfo.InvariantCulture) },
                        {"TotalFeeAmount", totalFeeAmount.ToString(CultureInfo.InvariantCulture) }
                    };
                    ComplexApplicationListService.SetUniqueItems(request.ApplicationNr, "Settlement", 1, settlementItems, context);
                });

            return new Response();
        }

        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        private static List<MortgageLoanRequest.MortgageLoanCollateralsModel.CollateralModel> CreateCollateralModel(
            ICreditApplicationCustomEditableFieldsService fieldService,
            ApplicationDataSourceResult.DataSourceResult complexList)
        {
            var formattingCulture = CultureInfo.GetCultureInfo(NEnv.ClientCfg.Country.BaseFormattingCulture);

            MortgageLoanRequest.MortgageLoanCollateralsModel.PropertyModel GetCollateralProperty(ComplexApplicationListDataSource.ComplexListRow r, string itemName)
            {
                var t = fieldService.GetFieldModel(ComplexApplicationListDataSource.DataSourceNameShared, r.GetCompoundName(itemName, true));
                var rawValue = r.UniqueItems?.Opt(itemName);
                if (string.IsNullOrWhiteSpace(rawValue))
                    return null;
                else
                    return new MortgageLoanRequest.MortgageLoanCollateralsModel.PropertyModel
                    {
                        TypeCode = t.DataType,
                        CodeName = itemName,
                        DisplayName = t.LabelText,
                        CodeValue = rawValue,
                        DisplayValue = t.FormatValueForDisplay(rawValue, formattingCulture),
                    };
            }

            MortgageLoanRequest.MortgageLoanCollateralsModel.ValuationModel GetCollateralValuation(ComplexApplicationListDataSource.ComplexListRow r, string typeCode, string amountName, string dateName, string sourceName)
            {
                var value = Numbers.ParseDecimalOrNull(r.UniqueItems.Opt(amountName));
                if (!value.HasValue) return null;

                var valuationDate = Dates.ParseDateTimeExactOrNull(r.UniqueItems.Opt(dateName), "yyyy-MM-dd");

                return new MortgageLoanRequest.MortgageLoanCollateralsModel.ValuationModel { TypeCode = typeCode, Amount = value.Value, ValuationDate = valuationDate, SourceDescription = sourceName == null ? null : r.UniqueItems.Opt(sourceName) };
            }

            //TODO: Move to plugin as there is a lot of arbitrary per client things here
            var allRows = ComplexApplicationListDataSource
                .ToRows(complexList);
            return allRows
                .Where(x => x.ListName == "ApplicationObject")
                .Select(x =>
                {
                    var propertyType = x.UniqueItems?.Opt("propertyType");
                    var properties = new List<MortgageLoanRequest.MortgageLoanCollateralsModel.PropertyModel>();
                    var estateUniqueNames = new[] { "estatePropertyId", "estateRegisterUnit" };
                    var housingCompanyUniqueNames = new[] { "housingCompanyName", "housingCompanyLoans", "housingCompanyShareCount" };
                    var skippedNames = Enumerables.Array("exists").Concat(estateUniqueNames).Concat(housingCompanyUniqueNames).ToArray();
                    properties.AddRange(Enumerables.SkipNulls(x.UniqueItems.Where(y => !y.Key.IsOneOf(skippedNames)).Select(y => GetCollateralProperty(x, y.Key)).ToArray()));

                    string collateralId = null;

                    if (propertyType == "estate")
                    {
                        properties.AddRange(Enumerables.SkipNulls(x.UniqueItems.Where(y => y.Key.IsOneOf(estateUniqueNames)).Select(y => GetCollateralProperty(x, y.Key)).ToArray()));
                        properties.AddRange((x.RepeatedItems.Opt("estateDeeds") ?? new List<string>())
                            .Select(JsonConvert.DeserializeObject<MortgageLoanEstateDeedItemModel>)
                            .SelectMany(y =>
                            {
                                return Enumerables.SkipNulls(!y.deedAmount.HasValue ? null : new MortgageLoanRequest.MortgageLoanCollateralsModel.PropertyModel
                                {
                                    TypeCode = "positiveDecimal",
                                    CodeName = "deedAmount",
                                    DisplayName = "Estate deed amount",
                                    CodeValue = y.deedAmount.Value.ToString(CultureInfo.InvariantCulture),
                                    DisplayValue = y.deedAmount.Value.ToString("N2", formattingCulture),
                                }, string.IsNullOrWhiteSpace(y.deedNr) ? null : new MortgageLoanRequest.MortgageLoanCollateralsModel.PropertyModel
                                {
                                    TypeCode = "string",
                                    CodeName = "deedNr",
                                    DisplayName = "Estate deed nr",
                                    CodeValue = y.deedNr,
                                    DisplayValue = y.deedNr,
                                });
                            }));

                        collateralId = x.UniqueItems?.Opt("estatePropertyId");
                    }
                    else if (propertyType == "housingCompany")
                    {
                        properties.AddRange(Enumerables.SkipNulls(x.UniqueItems.Where(y => y.Key.IsOneOf(housingCompanyUniqueNames)).Select(y => GetCollateralProperty(x, y.Key)).ToArray()));

                        var housingCompanyName = x.UniqueItems?.Opt("housingCompanyName");
                        var housingCompanyShareCount = x.UniqueItems?.Opt("housingCompanyShareCount");
                        if (!string.IsNullOrWhiteSpace(housingCompanyName) || !string.IsNullOrWhiteSpace(housingCompanyShareCount))
                        {
                            collateralId = $"{housingCompanyName?.Trim()} {housingCompanyShareCount?.Trim()}".Trim();
                        }
                    }

                    if (string.IsNullOrWhiteSpace(collateralId))
                    {
                        collateralId = $"u:{Guid.NewGuid().ToString()}"; //Just make sure it matches nothing and stands out super obviously if someone looks at it. u: prefix to make it possible to find all of these afterwards if someone wants to manually add the actual ids
                    }

                    var valuations = new List<MortgageLoanRequest.MortgageLoanCollateralsModel.ValuationModel>();
                    valuations.AddRange(Enumerables.SkipNulls( //TODO: These should be in the bluestep plugin
                        GetCollateralValuation(x, "External", "valuationAmount", "valuationDate", "valuationSource"),
                        GetCollateralValuation(x, "Statistical", "statValuationAmount", "statValuationDate", null),
                        GetCollateralValuation(x, "Price", "priceAmount", "priceAmountDate", null)
                        ));

                    return new MortgageLoanRequest.MortgageLoanCollateralsModel.CollateralModel
                    {
                        IsMain = x.Nr == 1,
                        CollateralId = collateralId,
                        CustomerIds = x.GetRepeatedItem("customerIds", int.Parse),
                        Properties = properties,
                        Valuations = valuations
                    };
                }).ToList();
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
        }
    }
}