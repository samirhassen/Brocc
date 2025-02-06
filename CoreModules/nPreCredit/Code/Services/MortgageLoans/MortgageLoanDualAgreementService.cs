using Newtonsoft.Json;
using NTech;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.LoanModel;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;

namespace nPreCredit.Code.Services.MortgageLoans
{
    public class MortgageLoanDualAgreementService : IMortgageLoanDualAgreementService
    {
        private readonly IMortgageLoanWorkflowService mortgageLoanWorkflowService;
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;
        private readonly ICustomerClient customerClient;
        private readonly ICreditClient creditClient;
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;
        private readonly IClock clock;
        private readonly IClientConfiguration clientConfiguration;

        private readonly Lazy<CultureInfo> formattingCulture;
        private readonly Lazy<bool> isTest = new Lazy<bool>(() => !NEnv.IsProduction);

        public MortgageLoanDualAgreementService(
            IMortgageLoanWorkflowService mortgageLoanWorkflowService, IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository,
            ICustomerClient customerClient, ICreditClient creditClient, INTechCurrentUserMetadata ntechCurrentUserMetadata,
            IClientConfiguration clientConfiguration, IClock clock)
        {
            this.mortgageLoanWorkflowService = mortgageLoanWorkflowService;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
            this.customerClient = customerClient;
            this.creditClient = creditClient;
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
            this.clock = clock;
            this.formattingCulture = new Lazy<CultureInfo>(() => CultureInfo.GetCultureInfo(clientConfiguration.Country.BaseFormattingCulture));
            this.clientConfiguration = clientConfiguration;
        }

        public MortgageLoanDualAgreementPrintContextModel GetPrintContext(ApplicationInfoModel applicationInfo, int customerId, Action<MortgageLoanDualAgreementPrintContextModel.SideChannelData> observeData = null)
        {
            var creditNrs = EnsureCreditNrs(applicationInfo);
            var sideChannelData = new MortgageLoanDualAgreementPrintContextModel.SideChannelData();

            var applicationNr = applicationInfo.ApplicationNr;
            var m = new MortgageLoanDualAgreementPrintContextModel();

            var collateralInfoFields = new List<string>
            {
                "employment", "employedSince", "employer", "profession", "employedTo", "marriage", "monthlyIncomeSalaryAmount",
                "monthlyIncomePensionAmount", "monthlyIncomeCapitalAmount",
                "monthlyIncomeBenefitsAmount", "monthlyIncomeOtherAmount", "childrenMinorCount", "childrenAdultCount",
                "costOfLivingRent", "costOfLivingFees"
            };
            var request = new PartialCreditApplicationModelRequest
            {
                ApplicationFields = new List<string>
                {
                    "companyCustomerId", "mainLoanCreditNr", "childLoanCreditNr", "requestedDueDay"
                },
                ApplicantFields = new List<string> { "customerId" }.Concat(collateralInfoFields).Distinct().ToList(),
                ErrorIfGetNonLoadedField = true
            };
            if (clientConfiguration.Country.BaseCountry == "FI")
            {
                request.ApplicationFields.Add("consumerBankAccountIban");
            }
            var app = this.partialCreditApplicationModelRepository.Get(applicationNr, request);

            var customerIdByApplicantNr = new Dictionary<int, int>();
            app.DoForEachApplicant(applicantNr =>
            {
                customerIdByApplicantNr[applicantNr] = app.Applicant(applicantNr).Get("customerId").IntValue.Required;
            });

            var documentsAdded = new List<Tuple<MortgageLoanDualAgreementPrintContextModel.DocumentModel, string>>();
            void ObserveDocumentAdded(MortgageLoanDualAgreementPrintContextModel.DocumentModel x, string y) => documentsAdded.Add(Tuple.Create(x, y));

            using (var context = new PreCreditContext())
            {
                var customers = GetCustomersComposable(context, applicationInfo, customerIdByApplicantNr, customerClient);
                sideChannelData.Customers = customers;
                var currentCustomer = customers.Opt(customerId);
                if (currentCustomer == null)
                    throw new NTechWebserviceMethodException("Customer is not a part of this application") { ErrorCode = "customerNotOnApplication" };

                var decision = GetDecisionModel(applicationNr, context, "Final");
                sideChannelData.CreditDecsionId = decision.Id;

                MortgageLoanDualAgreementPrintContextModel.CollateralAgreementModel.CollateralLoanModel CreateCollateralLoan(bool isMain) =>
                    decision.HasLoan(isMain) ? new MortgageLoanDualAgreementPrintContextModel.CollateralAgreementModel.CollateralLoanModel
                    {
                        loanAmount = decision.RequiredDecimal("LoanAmount", isMain).ToString("C", F),
                        loanAmountIncludingCapitalizedInitialFees = (decision.RequiredDecimal("LoanAmount", isMain) + decision.RequiredDecimal("TotalInitialFeeAmount", isMain)).ToString("C", F),
                        loanNr = isMain ? creditNrs.Item1 : creditNrs.Item2
                    } : null;

                sideChannelData.Loans = new List<MortgageLoanDualAgreementPrintContextModel.SideChannelData.LoanModel>();
                foreach (var isMain in Enumerables.Array(true, false))
                {
                    if (decision.HasLoan(isMain))
                    {
                        sideChannelData.Loans.Add(new MortgageLoanDualAgreementPrintContextModel.SideChannelData.LoanModel
                        {
                            IsMainLoan = isMain,
                            LoanAmount = decision.RequiredDecimal("LoanAmount", isMain)
                        });
                    }
                }

                var applicantCustomers = customers.Values.Where(x => x.IsApplicant).ToList();

                foreach (var applicantCustomer in customers.Select(x => x.Value).Where(x => x.IsApplicant && x.CustomerId == customerId).OrderBy(x => x.ApplicantNr ?? 0).ToList())
                {
                    var applicantNr = applicantCustomer.ApplicantNr.Value;

                    if (m.applicantperson == null)
                    {
                        m.applicantperson = new List<MortgageLoanDualAgreementPrintContextModel.ApplicantPersonModel>();
                    }

                    var applicant1Customer = customers.Values.SingleOrDefault(x => x.IsApplicant && x.ApplicantNr == 1);
                    var applicant2Customer = customers.Values.SingleOrDefault(x => x.IsApplicant && x.ApplicantNr == 2);
                    var dueDay = app.Application.Get("requestedDueDay").IntValue.Optional ?? 28;
                    var firstDueDate = ComputeNextDueDate(clock, dueDay);
                    var a = new MortgageLoanDualAgreementPrintContextModel.ApplicantPersonModel();

                    void SetupAgreement(bool isMain)
                    {
                        if (!decision.HasLoan(isMain)) return;

                        var sideChannelLoan = sideChannelData.Loans.Single(x => x.IsMainLoan == isMain);

                        var loanAmount = sideChannelLoan.LoanAmount;
                        var marginInterestRate = decision.RequiredDecimal("MarginInterestRatePercent", isMain);
                        var referenceInterestRate = decision.RequiredDecimal("ReferenceInterestRatePercent", isMain);
                        var notificationFeeAmount = decision.RequiredDecimal("NotificationFeeAmount", isMain);
                        var totalInitialFeeAmount = decision.RequiredDecimal("TotalInitialFeeAmount", isMain);
                        var totalInterestRate = marginInterestRate + referenceInterestRate;
                        var repaymentTimeInMonths = int.Parse(decision.Required("RepaymentTimeInMonths", isMain));
                        var annuityAmount = decision.RequiredDecimal("AnnuityAmount", isMain);
                        var effectiveInterestRatePercent = decision.RequiredDecimal("EffectiveInterestRatePercent", isMain);
                        var totalPaidAmount = decision.RequiredDecimal("TotalPaidAmount", isMain);

                        var hasDirectPaymentToCustomer =
                            (decision.OptionalDecimal("DirectToCustomerAmount", true) ?? 0m) > 0
                            || (decision.OptionalDecimal("DirectToCustomerAmount", false) ?? 0m) > 0;

                        var agreement = new MortgageLoanDualAgreementPrintContextModel.MortgageLoanAgreementModel
                        {
                            printDate = clock.Today.ToString("d", F),
                            contact1 = CreateContactFromCustomer(applicant1Customer),
                            contact2 =
                                applicant2Customer == null ? null : CreateContactFromCustomer(applicant2Customer),
                            contact_current =
                                CreateContactFromCustomer(applicantNr == 1 ? applicant1Customer : applicant2Customer),
                            loanNumber = isMain ? creditNrs.Item1 : creditNrs.Item2,
                            loanAmount = loanAmount.ToString("C", F),
                            loanAmountIncludingCapitalizedInitialFees = (loanAmount + totalInitialFeeAmount).ToString("C", F),
                            is_ml = isMain ? "true" : null,
                            is_ul = isMain ? null : "true",
                            marginInterestRate = (marginInterestRate / 100m).ToString("P", F),
                            referenceInterestRate = (referenceInterestRate / 100m).ToString("P", F),
                            totalInterestRate = (totalInterestRate / 100m).ToString("P", F),
                            repaymentTimeInMonths = repaymentTimeInMonths.ToString(),
                            notificationFeeAmount = notificationFeeAmount.ToString("C", F),
                            totalInitialFeeAmount = totalInitialFeeAmount.ToString("C", F),
                            monthlyPaymentExcludingFees = Math.Round(annuityAmount, 2).ToString("C", F),
                            monthlyPaymentIncludingFees =
                                Math.Round(annuityAmount + notificationFeeAmount, 2).ToString("C", F),
                            effectiveInterestRate = (effectiveInterestRatePercent / 100m).ToString("P", F),
                            totalPaidAmount = totalPaidAmount.ToString("C", F),
                            notificationDueDay = dueDay.ToString(),
                            projectedFirstDueDate = firstDueDate.ToString("d", F),
                            projectedEndDate = firstDueDate.AddMonths(repaymentTimeInMonths - 1).ToString("d", F),
                            fee = new Dictionary<string, string>(),
                            hasDirectPaymentToCustomer = hasDirectPaymentToCustomer ? "true" : null
                        };

                        if (this.clientConfiguration.Country.BaseCountry == "FI")
                        {
                            var consumerBankAccountIban = app.Application.Get("consumerBankAccountIban").StringValue.Optional;
                            if (consumerBankAccountIban != null)
                            {
                                agreement.consumerDirectPaymentBankAccount =
                                    new MortgageLoanDualAgreementPrintContextModel.BankAccountNrModel
                                    {
                                        raw = consumerBankAccountIban
                                    };
                                if (IBANFi.TryParse(consumerBankAccountIban, out var ibanFi))
                                {
                                    agreement.consumerDirectPaymentBankAccount.displayFormatted = ibanFi.GroupsOfFourValue;
                                    agreement.consumerDirectPaymentBankAccount.normalized = ibanFi.NormalizedValue;
                                }
                            }
                        }

                        var prefix = isMain ? "main" : "child";
                        foreach (var fee in decision.UniqueItems.Where(x => x.Key.StartsWith(prefix) && x.Key.EndsWith("FeeAmount")))
                        {
                            var feeName = fee.Key.Substring(prefix.Length, 1).ToLowerInvariant() + fee.Key.Substring(prefix.Length + 1);
                            var feeAmount = decision.RequiredDecimal(fee.Key, null);
                            agreement.fee[feeName] = feeAmount.ToString("C", F);
                            agreement.fee[$"has_{feeName}"] = feeAmount != 0m ? "true" : null;
                        }

                        if (isMain)
                        {
                            var totalStressInterestRatePercent = Math.Max(totalInterestRate + 3m, 6m);
                            var stressedPaymentPlan = PaymentPlanCalculation.BeginCreateWithRepaymentTime(loanAmount, repaymentTimeInMonths, totalStressInterestRatePercent, true, null, NEnv.CreditsUse360DayInterestYear)
                                .WithInitialFeeCapitalized(totalInitialFeeAmount)
                                .WithMonthlyFee(notificationFeeAmount)
                                .EndCreate();

                            var normalPaymentPlan = PaymentPlanCalculation.BeginCreateWithRepaymentTime(loanAmount, repaymentTimeInMonths, totalInterestRate, true, null, NEnv.CreditsUse360DayInterestYear)
                                .WithInitialFeeCapitalized(totalInitialFeeAmount)
                                .WithMonthlyFee(notificationFeeAmount)
                                .EndCreate();

                            a.mlagreement = SetupDocument($"ML agreement - Applicant {applicantCustomer.ApplicantNr}", agreement, ObserveDocumentAdded);

                            var loanAmountIncludingCapitalizedInitialFees = (loanAmount + totalInitialFeeAmount);

                            a.esis = SetupDocument($"ML esis - Applicant {applicantCustomer.ApplicantNr}", new MortgageLoanDualAgreementPrintContextModel.EsisModel
                            {
                                printDate = clock.Today.ToString("d", F),
                                loanAmount = loanAmount.ToString("C", F),
                                loanAmountIncludingCapitalizedInitialFees = loanAmountIncludingCapitalizedInitialFees.ToString("C", F),
                                repaymentTimeInMonths = normalPaymentPlan.Payments.Count.ToString(),
                                totalPaidAmount = normalPaymentPlan.TotalPaidAmount.ToString("C", F),
                                contact_current = CreateContactFromCustomer(applicantNr == 1 ? applicant1Customer : applicant2Customer),
                                paidAmountPerLoanCurrencyUnit = (loanAmount == 0 ? 0m : normalPaymentPlan.TotalPaidAmount / loanAmount).ToString("N2", F),
                                paidAmountPerLoanCurrencyUnitIncludingCapitalizedInitialFees = (loanAmountIncludingCapitalizedInitialFees == 0 ? 0m : normalPaymentPlan.TotalPaidAmount / loanAmountIncludingCapitalizedInitialFees).ToString("N2", F),
                                effectiveInterestRate = (normalPaymentPlan.EffectiveInterestRatePercent / 100m)?.ToString("P", F),
                                marginInterestRate = (marginInterestRate / 100m).ToString("P", F),
                                referenceInterestRate = (referenceInterestRate / 100m).ToString("P", F),
                                totalInterestRate = (totalInterestRate / 100m).ToString("P", F),
                                totalInterestAmount = normalPaymentPlan.Payments.Sum(x => x.Interest).ToString("N2", F),
                                totalInitialFeeAmount = totalInitialFeeAmount.ToString("N2", F),
                                totalNotificationFeeAmount = normalPaymentPlan.Payments.Sum(x => x.MonthlyFee).ToString("N2", F),
                                monthlyPaymentExcludingFees = Math.Round(normalPaymentPlan.AnnuityAmount, 2).ToString("N2", F),
                                monthlyPaymentIncludingFees = Math.Round(normalPaymentPlan.AnnuityAmount + notificationFeeAmount, 2).ToString("N2", F),
                                dueDay = dueDay.ToString(F),
                                projectedFirstDueDate = firstDueDate.ToString("d", F),
                                projectedLastDate = firstDueDate.AddMonths(repaymentTimeInMonths - 1).ToString("d", F),
                                stressedEffectiveInterestRate = (stressedPaymentPlan.EffectiveInterestRatePercent / 100m)?.ToString("P", F),
                                stressedTotalInterestRate = (totalStressInterestRatePercent / 100m).ToString("P", F),
                                stressedReferenceInterestRate = ((totalStressInterestRatePercent - marginInterestRate) / 100m).ToString("P", F),
                                stressedMonthlyPaymentExcludingFees = Math.Round(stressedPaymentPlan.AnnuityAmount, 2).ToString("N2", F),
                                stressedMonthlyPaymentIncludingFees = Math.Round(stressedPaymentPlan.AnnuityAmount + notificationFeeAmount, 2).ToString("N2", F),
                            }, ObserveDocumentAdded);
                        }
                        else
                        {
                            var normalPaymentPlan = PaymentPlanCalculation.BeginCreateWithRepaymentTime(loanAmount, repaymentTimeInMonths, totalInterestRate, true, null, NEnv.CreditsUse360DayInterestYear)
                                .WithInitialFeeCapitalized(totalInitialFeeAmount)
                                .WithMonthlyFee(notificationFeeAmount)
                                .EndCreate();

                            a.ulagreement = SetupDocument($"UL agreement - Applicant {applicantCustomer.ApplicantNr}", agreement, ObserveDocumentAdded);

                            a.sekki = SetupDocument($"UL sekki - Applicant {applicantCustomer.ApplicantNr}", new MortgageLoanDualAgreementPrintContextModel.SekkiModel
                            {
                                loanAmount = loanAmount.ToString("N2", F),
                                loanAmountIncludingCapitalizedInitialFees = (loanAmount + totalInitialFeeAmount).ToString("N2", F),
                                totalCostAmount = (totalPaidAmount - loanAmount).ToString("N2", F),
                                totalPaidAmount = totalPaidAmount.ToString("N2", F),
                                repaymentTimeInMonths = repaymentTimeInMonths.ToString(),
                                monthlyPaymentExcludingFees = Math.Round(normalPaymentPlan.AnnuityAmount, 2).ToString("N2", F),
                                monthlyPaymentIncludingFees = Math.Round(normalPaymentPlan.AnnuityAmount + notificationFeeAmount, 2).ToString("N2", F),
                                totalNotificationFeeAmount = normalPaymentPlan.Payments.Sum(x => x.MonthlyFee).ToString("N2", F),
                                totalInterestAmount = normalPaymentPlan.Payments.Sum(x => x.Interest).ToString("N2", F),
                                marginInterestRate = (marginInterestRate / 100m).ToString("P", F),
                                referenceInterestRate = (referenceInterestRate / 100m).ToString("P", F),
                                totalInterestRate = (totalInterestRate / 100m).ToString("P", F),
                                effectiveInterestRate = (normalPaymentPlan.EffectiveInterestRatePercent / 100m)?.ToString("P", F),
                                notificationFeeAmount = notificationFeeAmount.ToString("N2", F),
                                totalInitialFeeAmount = totalInitialFeeAmount.ToString("N2", F),
                            }, ObserveDocumentAdded);
                        }
                    }

                    SetupAgreement(true);
                    SetupAgreement(false);

                    //Include if at least one of ul and ml agreement is included
                    if (a.mlagreement != null || a.ulagreement != null)
                    {
                        m.generalterms = SetupDocument("General terms", new MortgageLoanDualAgreementPrintContextModel.GeneralTermsModel(), ObserveDocumentAdded);
                    }

                    if (applicantCustomer.IsMainApplicationObjectOwner)
                    {
                        var mainCollateral = applicantCustomer.OwnedCollateralsByNr.Values.Single(x => x.IsMainCollateral);

                        a.maincollateral = SetupDocument(
                            $"{mainCollateral.TestDisplayName} agreement - Applicant {applicantCustomer.ApplicantNr}",
                            CreateCollateralAgreement(mainCollateral.CustomerIds.Select(x => customers[x]).ToList(),
                                applicantCustomers,
                                mainCollateral,
                                Enumerables.SkipNulls(CreateCollateralLoan(true), CreateCollateralLoan(false)).ToList())
                            , ObserveDocumentAdded);

                        /*
                         * othercollateral_info_copies:
                         * All non main collateral owners get a copy of the economic status of the main collateral owners
                         * The main collateral owners get a copy of all of these here.
                         * Unclear if each main owner should only see copies of their own economic status or everyone but opting for everyone
                         * here reasoning that you would want to know the financial status of the people you are entering into a loan with also.
                        */
                        a.othercollateral_info_copies = new List<MortgageLoanDualAgreementPrintContextModel.CollateralInfoModel>();
                        foreach (var mainCollateralCustomer in customers.Values.Where(x => x.IsMainApplicationObjectOwner && x.IsApplicant))
                        {
                            foreach (var otherCollateralCustomer in customers.Values.Where(x => x.IsOtherApplicationObjectOwner && x.CustomerId != mainCollateralCustomer.CustomerId))
                            {
                                a.othercollateral_info_copies.Add(SetupDocument(
                                    $"Collateral info about applicant {mainCollateralCustomer.ApplicantNr.Value} for other collateral owner {otherCollateralCustomer.CivicRegNr}",
                                    CreateCollateralInfo(mainCollateralCustomer, otherCollateralCustomer, app),
                                    ObserveDocumentAdded));
                            }
                        }
                    }

                    m.applicantperson.Add(a);
                }

                if (currentCustomer.IsOtherApplicationObjectOwner)
                {
                    //Show a collateral agreement + terms for each other collateral where this customer is an owner
                    //TODO: Would it be enough to show one copy of the terms?
                    foreach (var otherCollateral in currentCustomer.OwnedCollateralsByNr.Values.OrderBy(x => x.Nr))
                    {
                        if (m.othercollateral_agreement == null)
                            m.othercollateral_agreement = new List<MortgageLoanDualAgreementPrintContextModel.CollateralAgreementModel>();
                        m.othercollateral_agreement.Add(SetupDocument(
                            $"{otherCollateral.TestDisplayName} agreement - {currentCustomer.CivicRegNr}",
                             CreateCollateralAgreement(otherCollateral.CustomerIds.Select(x => customers[x]).ToList(),
                                applicantCustomers,
                                otherCollateral,
                                Enumerables.SkipNulls(CreateCollateralLoan(true), CreateCollateralLoan(false)).ToList()),
                             ObserveDocumentAdded));
                    }

                    //If the customer owns at least one other collateral show collateral info (economic data) about each applicant so the customer knows
                    //how strong the paying power is of the customers they are securing with their collateral.
                    foreach (var mainCollateralCustomer in customers.Values.Where(x => x.IsMainApplicationObjectOwner && x.IsApplicant && x.CustomerId != customerId))
                    {
                        if (m.othercollateral_info == null)
                            m.othercollateral_info = new List<MortgageLoanDualAgreementPrintContextModel.CollateralInfoModel>();
                        m.othercollateral_info.Add(SetupDocument(
                                    $"Collateral info about applicant {mainCollateralCustomer.ApplicantNr.Value} for other collateral owner {currentCustomer.CivicRegNr}",
                                     CreateCollateralInfo(mainCollateralCustomer, mainCollateralCustomer, app),
                                     ObserveDocumentAdded));
                    }
                }

                if (isTest.Value)
                {
                    m.debug_data = new MortgageLoanDualAgreementPrintContextModel.DebugModel
                    {
                        debug_items = new List<MortgageLoanDualAgreementPrintContextModel.DebugItem>()
                    };
                    void Header(string x) => m.debug_data.debug_items.Add(new MortgageLoanDualAgreementPrintContextModel.DebugItem { header = x, text = null });
                    void Text(string x, string y) => m.debug_data.debug_items.Add(new MortgageLoanDualAgreementPrintContextModel.DebugItem { header = null, label = x, text = y });

                    if (documentsAdded.Count > 0)
                    {
                        Header("Documents included");
                        foreach (var d in documentsAdded)
                            Text(null, d.Item2);
                    }
                    foreach (var c in customers.Values)
                    {
                        Header($"Customer: {c.CivicRegNr}{" " + (currentCustomer.CustomerId == c.CustomerId ? "(current customer)" : "")}");
                        Text("Roles", string.Join(", ",
                            Enumerables.SkipNulls(
                                c.IsApplicant ? $"Applicant {c.ApplicantNr.Value.ToString()}" : null,
                                c.IsMainApplicationObjectOwner ? "Main collateral owner" : null,
                                c.IsOtherApplicationObjectOwner ? "Other collateral owner" : null)));
                        if (c.IsOtherApplicationObjectOwner)
                        {
                            Text("Other collaterals", string.Join(", ", c.OwnedCollateralsByNr.Values.OrderBy(x => x.Nr).Select(x => x.TestDisplayName)));
                        }

                        if (!c.IsApplicant)
                            continue;

                        foreach (var ci in collateralInfoFields)
                        {
                            Text(ci, app.Applicant(c.ApplicantNr.Value).Get(ci).StringValue.Optional ?? "-");
                        }
                    }

                    var allCollaterals = customers.Values.SelectMany(x => x.OwnedCollateralsByNr.Values).GroupBy(x => x.Nr).Select(x => x.First()).OrderBy(x => x.Nr).ToList();
                    foreach (var c in allCollaterals)
                    {
                        Header($"{c.TestDisplayName}");
                        Text("IsEstate", c.IsEstate.ToString());
                        Text("IsHousingCompany", c.IsHousingCompany.ToString());
                        Text("Owners", string.Join(", ", c.CustomerIds.Select(x => customers[x].CivicRegNr + (x == currentCustomer.CustomerId ? " (*)" : ""))));
                    }
                }
            }

            observeData?.Invoke(sideChannelData);

            return m;
        }

        private static DateTime ComputeNextDueDate(IClock clock, int dueDay)
        {
            var today = clock.Today;
            var d = Dates.GetNextDateWithDayNrAfterDate(dueDay, today);
            return (Dates.GetAbsoluteNrOfDaysBetweenDates(d, today) >= 14)
                ? d
                : Dates.GetNextDateWithDayNrAfterDate(dueDay, d.AddDays(1));
        }

        private MortgageLoanDualAgreementPrintContextModel.CollateralAgreementModel CreateCollateralAgreement(List<CustomerModel> allCollateralOwners,
            List<CustomerModel> loanCustomers, CustomerModel.OwnedCollateralModel collateral, List<MortgageLoanDualAgreementPrintContextModel.CollateralAgreementModel.CollateralLoanModel> newLoans)
        {
            return new MortgageLoanDualAgreementPrintContextModel.CollateralAgreementModel
            {
                collateralowner = allCollateralOwners.Select(CreateContactFromCustomer).ToList(),
                loandebtor = loanCustomers.Select(CreateContactFromCustomer).ToList(),
                property_housing_company = collateral.IsHousingCompany ? new MortgageLoanDualAgreementPrintContextModel.CollateralAgreementModel.HousingCompanyModel
                {
                    housingCompanyName = collateral.HousingCompanyName,
                    housingCompanyShareCount = collateral.HousingCompanyShareCount
                } : null,
                property_estate = collateral.IsEstate ? new MortgageLoanDualAgreementPrintContextModel.CollateralAgreementModel.EstateModel
                {
                    estatePropertyId = collateral.EstatePropertyId,
                    estate_deeds = collateral.EstateDeeds.Select(y => new MortgageLoanDualAgreementPrintContextModel.CollateralAgreementModel.EstateDeedModel
                    {
                        deedNr = y.deedNr,
                        deedAmount = y.deedAmount?.ToString("C", F)
                    }).ToList()
                } : null,
                collateral_loans = newLoans,
                printDate = clock.Today.ToString("d", F),
                addressStreet = collateral.AddressStreet,
                addressCity = collateral.AddressCity,
                addressZipCode = collateral.AddressZipCode,
            };
        }

        private T SetupDocument<T>(string context, T d, Action<MortgageLoanDualAgreementPrintContextModel.DocumentModel, string> observe) where T : MortgageLoanDualAgreementPrintContextModel.DocumentModel
        {
            if (isTest.Value)
                d.TestContext = context;

            observe(d, context);

            return d;
        }

        protected CultureInfo F => this.formattingCulture.Value;

        private const string RequiredDecisionType = "Final";

        public Tuple<string, string> EnsureCreditNrs(ApplicationInfoModel applicationInfo)
        {
            var app = this.partialCreditApplicationModelRepository.Get(applicationInfo.ApplicationNr, new PartialCreditApplicationModelRequest
            {
                ApplicationFields = new List<string>
                {
                    "mainLoanCreditNr", "childLoanCreditNr"
                },
                ErrorIfGetNonLoadedField = true
            });

            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, this.clock))
            {
                return context.UsingTransaction(() =>
                {
                    var mainLoanCreditNr = app.Application.Get("mainLoanCreditNr").StringValue.Optional;
                    var childLoanCreditNr = app.Application.Get("childLoanCreditNr").StringValue.Optional;
                    var d = GetDecisionModel(applicationInfo.ApplicationNr, context, RequiredDecisionType);
                    var newItems = new List<PreCreditContextExtended.CreditApplicationItemModel>();
                    var needsMainCreditNr = d.HasLoan(true) && mainLoanCreditNr == null;
                    var needsChildLoanNr = d.HasLoan(false) && childLoanCreditNr == null;

                    var newCreditNrCount = (needsMainCreditNr ? 1 : 0) + (needsChildLoanNr ? 1 : 0);

                    if (newCreditNrCount == 0)
                        return Tuple.Create(mainLoanCreditNr, childLoanCreditNr);

                    var newNrs = this.creditClient.GenerateReferenceNumbers(newCreditNrCount, 0);

                    if (needsMainCreditNr)
                    {
                        mainLoanCreditNr = newNrs.Item1[0];
                        newItems.Add(
                            new PreCreditContextExtended.CreditApplicationItemModel
                            {
                                GroupName = "application",
                                Name = "mainLoanCreditNr",
                                Value = mainLoanCreditNr
                            });
                    }

                    if (needsChildLoanNr)
                    {
                        childLoanCreditNr = newNrs.Item1[needsMainCreditNr ? 1 : 0];
                        newItems.Add(
                            new PreCreditContextExtended.CreditApplicationItemModel
                            {
                                GroupName = "application",
                                Name = "childLoanCreditNr",
                                Value = childLoanCreditNr
                            });
                    }

                    var currentStepName = mortgageLoanWorkflowService.GetCurrentListName(applicationInfo.ListNames);
                    var applicationNr = applicationInfo.ApplicationNr;
                    var h = context.CreditApplicationHeaders.Include("Items").Single(x => x.ApplicationNr == applicationNr);
                    context.AddOrUpdateCreditApplicationItems(h, newItems, currentStepName);

                    context.SaveChanges();

                    return Tuple.Create(mainLoanCreditNr, childLoanCreditNr);
                });
            }
        }

        private MortgageLoanDualAgreementPrintContextModel.CollateralInfoModel CreateCollateralInfo(CustomerModel mainCollateralCustomer, CustomerModel otherCollateralCustomer, PartialCreditApplicationModel app)
        {
            if (!mainCollateralCustomer.ApplicantNr.HasValue)
                throw new Exception("Missing applicantNr");

            var i = app.Applicant(mainCollateralCustomer.ApplicantNr.Value);
            return new MortgageLoanDualAgreementPrintContextModel.CollateralInfoModel
            {
                applicant = CreateContactFromCustomer(mainCollateralCustomer),
                collateralowner = CreateContactFromCustomer(otherCollateralCustomer),
                employment = i.Get("employment").StringValue.Optional,
                employedSince = i.Get("employedSince").MonthValue(true).Optional?.ToString("yyyy-MM"),
                employer = i.Get("employer").StringValue.Optional,
                profession = i.Get("profession").StringValue.Optional,
                employedTo = i.Get("employedTo").MonthValue(true).Optional?.ToString("yyyy-MM"),
                marriage = i.Get("marriage").StringValue.Optional,
                monthlyIncomeSalaryAmount = i.Get("monthlyIncomeSalaryAmount").DecimalValue.Optional?.ToString("C", F),
                monthlyIncomePensionAmount = i.Get("monthlyIncomePensionAmount").DecimalValue.Optional?.ToString("C", F),
                monthlyIncomeCapitalAmount = i.Get("monthlyIncomeCapitalAmount").DecimalValue.Optional?.ToString("C", F),
                monthlyIncomeBenefitsAmount = i.Get("monthlyIncomeBenefitsAmount").DecimalValue.Optional?.ToString("C", F),
                monthlyIncomeOtherAmount = i.Get("monthlyIncomeOtherAmount").DecimalValue.Optional?.ToString("C", F),
                childrenMinorCount = i.Get("childrenMinorCount").IntValue.Optional?.ToString(),
                childrenAdultCount = i.Get("childrenAdultCount").IntValue.Optional?.ToString(),
                costOfLivingRent = i.Get("costOfLivingRent").DecimalValue.Optional?.ToString("C", F),
                costOfLivingFees = i.Get("costOfLivingFees").DecimalValue.Optional?.ToString("C", F)
            };
        }

        public MemoryStream CreateAgreementPdf(MortgageLoanDualAgreementPrintContextModel context, string overrideTemplateName = null, bool? disableTemplateCache = false)
        {
            var dc = new nDocumentClient();

            var pdfBytes = dc.PdfRenderDirect(
                overrideTemplateName ?? "mortgageloan-agreement",
                PdfCreator.ToTemplateContext(context),
                disableTemplateCache: disableTemplateCache.GetValueOrDefault());

            return new MemoryStream(pdfBytes);
        }

        private static AcceptedDecisionModel GetDecisionModel(string applicationNr, PreCreditContext context, string requiredDecisionType)
        {
            var decision = context
                .CreditApplicationHeaders
                .Where(x => x.ApplicationNr == applicationNr)
                .Select(x => new
                {
                    x.CurrentCreditDecisionId,
                    HasDecision = x.CurrentCreditDecisionId.HasValue,
                    IsAccepted = x.CurrentCreditDecisionId.HasValue && (x.CurrentCreditDecision as AcceptedCreditDecision) != null,
                    DecisionItems = x.CurrentCreditDecision.DecisionItems.Select(y => new { y.ItemName, y.Value, y.IsRepeatable })
                })
                .Single();

            if (!decision.HasDecision)
                throw new NTechWebserviceMethodException("Missing credit decision") { ErrorCode = "missingDecision", IsUserFacing = true };
            if (!decision.IsAccepted)
                throw new NTechWebserviceMethodException("Missing accepted credit decision") { ErrorCode = "missingDecision", IsUserFacing = true };

            if (!decision.CurrentCreditDecisionId.HasValue)
                throw new Exception("Missing CurrentCreditDecisionId");

            var m = new AcceptedDecisionModel
            {
                Id = decision.CurrentCreditDecisionId.Value,
                UniqueItems = decision.DecisionItems.Where(x => !x.IsRepeatable).ToDictionary(x => x.ItemName, x => x.Value),
                RepeatingItems = decision
                    .DecisionItems
                    .Where(x => x.IsRepeatable)
                    .GroupBy(x => x.ItemName)
                    .ToDictionary(x => x.Key, x => x.Select(y => y.Value).ToList())
            };
            if (m.Required("decisionType", null) != requiredDecisionType)
                throw new NTechWebserviceMethodException("Missing accepted final credit decision") { ErrorCode = "missingFinalDecision", IsUserFacing = true };

            return m;
        }

        private class AcceptedDecisionModel
        {
            public int Id { get; set; }
            public Dictionary<string, string> UniqueItems { get; set; }
            public Dictionary<string, List<string>> RepeatingItems { get; set; }

            public bool HasLoan(bool isMain)
            {
                var v = OptionalDecimal("LoanAmount", isMain);
                return v.HasValue && v.Value > 0m;
            }

            private string Optional(string name, bool? isMain)
            {
                var fullName = FullName(name, isMain);
                var v = UniqueItems.Opt(fullName);
                return string.IsNullOrWhiteSpace(v) ? null : v;
            }

            private string FullName(string name, bool? isMain)
            {
                if (isMain.HasValue && (name.StartsWith("main") || name.StartsWith("child")))
                    throw new Exception("Programming error. Use either isMain which prefixes child or main or use the full name and null.");
                return $"{(isMain.HasValue ? (isMain.Value ? "main" : "child") : "")}{name}";
            }

            public decimal RequiredDecimal(string name, bool? isMain)
            {
                var v = Required(name, isMain);
                var d = Numbers.ParseDecimalOrNull(v);
                if (!d.HasValue)
                    throw new NTechWebserviceMethodException("Missing required credit decision item: " + name) { ErrorCode = "missingDecisionItem", IsUserFacing = true };
                return d.Value;
            }

            public decimal? OptionalDecimal(string name, bool? isMain)
            {
                var v = Optional(name, isMain);
                if (v == null)
                    return null;
                return Numbers.ParseDecimalOrNull(v);
            }

            public string Required(string name, bool? isMain)
            {
                var v = Optional(name, isMain);
                if (string.IsNullOrWhiteSpace(v))
                    throw new NTechWebserviceMethodException("Missing required credit decision item: " + name) { ErrorCode = "missingDecisionItem", IsUserFacing = true };
                return v;
            }
        }

        private MortgageLoanDualAgreementPrintContextModel.ContactModel CreateContactFromCustomer(CustomerModel c)
        {
            return new MortgageLoanDualAgreementPrintContextModel.ContactModel
            {
                civicRegNr = c.CivicRegNr,
                areaAndZipcode = $"{c.AddressZipcode} {c.AddressCity}".Trim(),
                streetAddress = c.AddressStreet,
                fullName = $"{c.FirstName} {c.LastName}".Trim(),
                phone = c.Phone,
                email = c.Email
            };
        }

        public class CustomerModel
        {
            public int CustomerId { get; set; }
            public int? ApplicantNr { get; set; }
            public bool IsApplicant { get; set; }
            public bool IsMainApplicationObjectOwner { get; set; }
            public bool IsOtherApplicationObjectOwner { get; set; }
            public string FirstName { get; set; }
            public DateTime? BirthDate { get; set; }
            public bool HasSignedApplicationAndPoaDocument { get; set; }
            public string LastName { get; set; }
            public string CivicRegNr { get; set; }
            public string AddressZipcode { get; set; }
            public string AddressCity { get; set; }
            public string AddressStreet { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }

            public Dictionary<int, OwnedCollateralModel> OwnedCollateralsByNr { get; set; }

            public class OwnedCollateralModel
            {
                public int Nr { get; internal set; }
                public bool IsHousingCompany { get; internal set; }
                public string HousingCompanyName { get; internal set; }
                public string HousingCompanyShareCount { get; internal set; }
                public bool IsEstate { get; internal set; }
                public string EstatePropertyId { get; internal set; }
                public List<MortgageLoanEstateDeedItemModel> EstateDeeds { get; internal set; }
                public HashSet<int> CustomerIds { get; internal set; }
                public bool IsMainCollateral { get; internal set; }
                public string TestDisplayName { get; internal set; }
                public string AddressStreet { get; internal set; }
                public string AddressZipCode { get; internal set; }
                public string AddressCity { get; internal set; }
            }
        }

        public static Dictionary<int, CustomerModel> GetCustomersComposable(PreCreditContext context, ApplicationInfoModel applicationInfo, Dictionary<int, int> customerIdByApplicantNr, ICustomerClient customerClient)
        {
            var customers = new Dictionary<int, CustomerModel>();

            CustomerModel D(int x)
            {
                if (!customers.ContainsKey(x))
                {
                    customers[x] = new CustomerModel { CustomerId = x, OwnedCollateralsByNr = new Dictionary<int, CustomerModel.OwnedCollateralModel>() };
                }

                return customers[x];
            }

            for (var applicantNr = 1; applicantNr <= applicationInfo.NrOfApplicants; applicantNr++)
            {
                var customerId = customerIdByApplicantNr[applicantNr];
                var c = D(customerId);
                c.ApplicantNr = applicantNr;
                c.IsApplicant = true;
            }

            var applicationNr = applicationInfo.ApplicationNr;
            var requestsItemNames = new List<string> { "propertyType", "estatePropertyId", "estateDeeds", "housingCompanyName",
                "housingCompanyShareCount", "customerIds", "addressStreet", "addressZipCode", "addressCity"  };

            var appData = context
                .CreditApplicationHeaders
                .Where(x => x.ApplicationNr == applicationNr)
                .Select(x => new
                {
                    ApplicationObjectItems = x
                        .ComplexApplicationListItems
                        .Where(y => y.ListName == "ApplicationObject" && requestsItemNames.Contains(y.ItemName))
                        .Select(y => new
                        {
                            y.Nr,
                            y.ItemName,
                            y.ItemValue,
                            y.IsRepeatable
                        }),
                    ApplicantNrsWhoSignedPoaDocuments = x.Documents.Where(y => y.DocumentType == CreditApplicationDocumentTypeCode.SignedApplication.ToString() && !y.RemovedByUserId.HasValue).Select(y => y.ApplicantNr)
                })
                .Single();

            var applicantNrsWhoSignedPoaDocuments = appData.ApplicantNrsWhoSignedPoaDocuments.Where(x => x.HasValue).Select(x => x.Value).ToHashSet();
            var items = appData.ApplicationObjectItems.ToList();

            var collaterals = items
                .GroupBy(x => x.Nr)
                .Select(x =>
                {
                    string U(string n) => x.SingleOrDefault(y => y.ItemName == n && !y.IsRepeatable)?.ItemValue;
                    IEnumerable<string> R(string n) => x.Where(y => y.ItemName == n && y.IsRepeatable).Select(y => y.ItemValue);

                    var propertyType = U("propertyType");
                    var isHousingCompany = propertyType == "housingCompany";
                    var isEstate = propertyType == "estate";

                    string housingCompanyName = null;
                    string housingCompanyShareCount = null;
                    if (isHousingCompany)
                    {
                        housingCompanyName = U("housingCompanyName");
                        housingCompanyShareCount = U("housingCompanyShareCount");
                    }

                    List<MortgageLoanEstateDeedItemModel> estateDeeds = null;
                    string estatePropertyId = null;
                    if (isEstate)
                    {
                        estatePropertyId = U("estatePropertyId");
                        estateDeeds = R("estateDeeds").Select(JsonConvert.DeserializeObject<MortgageLoanEstateDeedItemModel>).ToList();
                    }

                    var customerIds = R("customerIds").Select(int.Parse).ToHashSet();

                    return new CustomerModel.OwnedCollateralModel
                    {
                        Nr = x.Key,
                        IsMainCollateral = x.Key == 1,
                        IsHousingCompany = isHousingCompany,
                        HousingCompanyName = housingCompanyName,
                        HousingCompanyShareCount = housingCompanyShareCount,
                        IsEstate = isEstate,
                        EstatePropertyId = estatePropertyId,
                        EstateDeeds = estateDeeds,
                        CustomerIds = customerIds,
                        TestDisplayName = x.Key == 1 ? "Main collateral" : $"Other collateral {x.Key - 1}",
                        AddressStreet = U("addressStreet"),
                        AddressZipCode = U("addressZipCode"),
                        AddressCity = U("addressCity")
                    };
                })
                .ToDictionary(x => x.Nr, x => x);

            var allCustomerIds = collaterals.SelectMany(x => x.Value.CustomerIds).ToHashSet();

            foreach (var customerId in allCustomerIds)
            {
                var customer = D(customerId);
                foreach (var ownedCollateral in collaterals.Values.Where(x => x.CustomerIds.Contains(customerId)))
                {
                    if (ownedCollateral.IsMainCollateral)
                    {
                        customer.IsMainApplicationObjectOwner = true;
                    }
                    else
                    {
                        customer.IsOtherApplicationObjectOwner = true;
                    }
                    customer.OwnedCollateralsByNr[ownedCollateral.Nr] = ownedCollateral;
                }
            }

            for (var applicantNr = 1; applicantNr <= applicationInfo.NrOfApplicants; applicantNr++)
            {
                var customerId = customerIdByApplicantNr[applicantNr];
                var customer = D(customerId);
                if (applicantNrsWhoSignedPoaDocuments.Contains(applicantNr))
                {
                    customer.HasSignedApplicationAndPoaDocument = true;
                }
            }

            var customerData = customerClient.BulkFetchPropertiesByCustomerIdsD(customers.Keys.ToHashSet(),
                "addressStreet", "addressCity", "addressZipcode", "civicRegNr", "firstName", "lastName", "email", "phone", "birthDate");

            string CustProp(int x, string y) => customerData.Opt(x).Opt(y);

            foreach (var c in customers.Values)
            {
                var bdRaw = CustProp(c.CustomerId, "birthDate");

                c.FirstName = CustProp(c.CustomerId, "firstName");
                c.BirthDate = string.IsNullOrWhiteSpace(bdRaw) ? new DateTime?() : Dates.ParseDateTimeExactOrNull(bdRaw, "yyyy-MM-dd");
                c.LastName = CustProp(c.CustomerId, "lastName");
                c.CivicRegNr = CustProp(c.CustomerId, "civicRegNr");
                c.AddressZipcode = CustProp(c.CustomerId, "addressZipcode");
                c.AddressCity = CustProp(c.CustomerId, "addressCity");
                c.AddressStreet = CustProp(c.CustomerId, "addressStreet");
                c.Email = CustProp(c.CustomerId, "email");
                c.Phone = CustProp(c.CustomerId, "phone");
            }

            return customers;
        }
    }

    public interface IMortgageLoanDualAgreementService
    {
        MortgageLoanDualAgreementPrintContextModel GetPrintContext(ApplicationInfoModel applicationInfo, int customerId, Action<MortgageLoanDualAgreementPrintContextModel.SideChannelData> observeData = null);

        Tuple<string, string> EnsureCreditNrs(ApplicationInfoModel applicationInfo);

        MemoryStream CreateAgreementPdf(MortgageLoanDualAgreementPrintContextModel context, string overrideTemplateName = null, bool? disableTemplateCache = false);
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class MortgageLoanEstateDeedItemModel
    {
        public string uid { get; set; }
        public string deedNr { get; set; }
        public decimal? deedAmount { get; set; }
    }
}