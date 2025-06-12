using Newtonsoft.Json;
using nPreCredit;
using nPreCredit.Code;
using nPreCredit.Code.Scoring.BalanziaScoringRules;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.LegacyUnsecuredLoans;
using NTech.Banking.CivicRegNumbers;
using NTech.Banking.CivicRegNumbers.Fi;
using NTech.Banking.LoanModel;
using NTech.Banking.ScoringEngine;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared.Code.PetrusOnlyScoringService;
using NTech.Core.PreCredit.Shared.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;

namespace NTech.Core.PreCredit.Shared.Services.UlLegacy
{
    public class PetrusOnlyCreditCheckService
    {
        private readonly IPartialCreditApplicationModelRepositoryExtended partialCreditApplicationModelRepository;
        private readonly ICustomerServiceRepository customerServiceRepository;
        private readonly IPreCreditEnvSettings envSettings;
        private readonly ICustomerClient customerClient;
        private readonly IAbTestingService abTestingService;
        private readonly ICreditClient creditClient;
        private readonly IPreCreditContextFactoryService preCreditContextFactoryService;
        private readonly ApplicationCheckpointService applicationCheckpointService;
        private readonly ICoreClock clock;
        private readonly PetrusOnlyScoringServiceFactory petrusFactory;
        private readonly IPublishEventService publishEventService;
        private readonly IAdditionalQuestionsSender additionalQuestionsSender;
        private readonly LegacyUnsecuredLoansRejectionService rejectionService;
        private readonly IRandomNrScoringVariableProvider randomNrScoringVariableGenerator;
        private readonly IReferenceInterestRateService referenceInterestRateService;
        private readonly CivicRegNumberParser civicRegNumberParser;

        public PetrusOnlyCreditCheckService(
            IPartialCreditApplicationModelRepositoryExtended partialCreditApplicationModelRepository,
            ICustomerServiceRepository customerServiceRepository, IPreCreditEnvSettings envSettings,
            ICustomerClient customerClient, IAbTestingService abTestingService, ICreditClient creditClient,
            IPreCreditContextFactoryService preCreditContextFactoryService, ApplicationCheckpointService applicationCheckpointService,
            ICoreClock clock, PetrusOnlyScoringServiceFactory onlyScoringServiceFactory,
            IPublishEventService publishEventService,
            IAdditionalQuestionsSender additionalQuestionsSender,
            LegacyUnsecuredLoansRejectionService rejectionService,
            IRandomNrScoringVariableProvider randomNrScoringVariableGenerator,
            IClientConfigurationCore clientConfiguration,
            IReferenceInterestRateService referenceInterestRateService)
        {
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
            this.customerServiceRepository = customerServiceRepository;
            this.envSettings = envSettings;
            this.customerClient = customerClient;
            this.abTestingService = abTestingService;
            this.creditClient = creditClient;
            this.preCreditContextFactoryService = preCreditContextFactoryService;
            this.applicationCheckpointService = applicationCheckpointService;
            this.clock = clock;
            this.petrusFactory = onlyScoringServiceFactory;
            this.publishEventService = publishEventService;
            this.additionalQuestionsSender = additionalQuestionsSender;
            this.rejectionService = rejectionService;
            this.randomNrScoringVariableGenerator = randomNrScoringVariableGenerator;
            this.referenceInterestRateService = referenceInterestRateService;
            civicRegNumberParser = new CivicRegNumberParser(clientConfiguration.Country.BaseCountry);
        }

        public const string FallbackRejectionReason = "otherProvenir";

        public void AutomaticCreditCheck(string applicationNr, bool supressCheckpointCheck)
        {
            var now = clock.Now;

            try
            {
                if (!supressCheckpointCheck && applicationCheckpointService.DoesAnyApplicationHaveAnActiveCheckpoint(applicationNr))
                {
                    AddComment("Automatic credit check skipped due to customer checkpoint", "CreditCheckAcceptedAutomationSkipped", true, now, applicationNr);
                    return;
                }

                var simpleResult = CreditCheckSimple(applicationNr, out var applicationModel);
                var result = ConvertStrictCreditCheckResult(simpleResult);

                if (!result.Success)
                {
                    throw new NTechCoreWebserviceException("Internal server error") { ErrorHttpStatusCode = 500, IsUserFacing = true };
                }

                AffiliateModel affiliate;
                using (var context = preCreditContextFactoryService.CreateExtended())
                {
                    var providerName = context.CreditApplicationHeadersQueryable.Where(x => x.ApplicationNr == applicationNr).Select(x => x.ProviderName).Single();
                    affiliate = envSettings.GetAffiliateModel(providerName);
                }

                if (result.creditCheckResult.HasOffer)
                {
                    if (result.creditCheckResult.OfferedAdditionalLoanCreditNr == null)
                    {
                        DoCompleteCreditCheck(
                            applicationNr,
                            applicationModel,
                            affiliate,
                            result.creditCheckResult,
                            new DoCompleteCreditCheckRequest
                            {
                                isAccepted = true, 
                                amount = result.creditCheckResult.OfferedAmount.Value,
                                repaymentTimeInMonths = result.creditCheckResult.OfferedRepaymentTimeInMonths,
                                marginInterestRatePercent = result.creditCheckResult.OfferedInterestRatePercent.Value,
                                initialFeeAmount = result.creditCheckResult.OfferedInitialFeeAmount.Value ,
                                notificationFeeAmount = result.creditCheckResult.OfferedNotificationFeeAmount.Value,
                                referenceInterestRatePercent = result.referenceInterestRatePercent,
                            },
                            out var userWarningMessage);
                    }
                    else
                    {
                        DoCompleteCreditCheck(
                            applicationNr,
                            applicationModel,
                            affiliate,
                            result.creditCheckResult,
                            new DoCompleteCreditCheckRequest
                            {
                                isAccepted = true, 
                                additionalLoanOffer =  new AdditionalLoanOfferAcceptModel
                                {
                                    AdditionalLoanCreditNr = result.creditCheckResult.OfferedAdditionalLoanCreditNr,
                                    AdditionalLoanAmount = result.creditCheckResult.OfferedAmount.Value,
                                    NewAnnuityAmount = result.creditCheckResult.OfferedAdditionalLoanNewAnnuityAmount,
                                    NewMarginInterestRatePercent = result.creditCheckResult.OfferedAdditionalLoanNewMarginInterestPercent
                                }
                            },
                            out var userWarningMessage);
                    }
                }
                else
                {
                    DoCompleteCreditCheck(
                        applicationNr,
                        applicationModel,
                        affiliate,
                        result.creditCheckResult,
                        new DoCompleteCreditCheckRequest
                        {
                            isAccepted = false,
                            rejectionReasons = result.creditCheckResult.RejectionReasons
                        },
                        out var userWarningMessage);

                }
            }
            catch (SendAdditionalQuestionsEmailFailedException)
            {
                AddComment("Automatic credit check aborted since additional questions could not be sent to the customer", "CreditCheckAutomationCancelled", true, now, applicationNr);
            }
        }

        public class AppDataTemp : PartialCreditApplicationModelExtendedCustomDataBase //Just for the one call in DoNewCreditCheckI to please the compiler
        {
            public string ProviderName { get; set; }
        }
        
        public static PartialCreditApplicationModelExtended<AppDataTemp> GetPartialCreditApplicationModelExtendedForCreditCheck(string applicationNr,
            IPartialCreditApplicationModelRepositoryExtended partialCreditApplicationModelRepository)
        {
            return partialCreditApplicationModelRepository.GetExtended(applicationNr,
                GetApplicationRequestedFields(), (_, ctx) => ctx
                    .CreditApplicationHeadersQueryable
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new AppDataTemp
                    {
                        NrOfApplicants = x.NrOfApplicants,
                        ProviderName = x.ProviderName
                    })
                    .Single());
        }
                
        public class ScoringDataContext
        {
            public ITestingVariationSet TestingVariationSet { get; }

            public ScoringDataContext(ITestingVariationSet testingVariationSet)
            {
                TestingVariationSet = testingVariationSet;
            }

            public string ApplicationNr { get; set; }

            public ScoringDataModel ScoringData { get; set; }
            public PartialCreditApplicationModel Application { get; set; }
            public Dictionary<int, List<HistoricalCreditExtended>> CustomerCreditHistoryByApplicantNr { get; set; }
            public IDictionary<int, IList<HistoricalApplication>> OtherApplicationsByApplicantNr { get; set; }
            public decimal? CurrentBalance { get; set; }
            public Dictionary<int, PartialCreditReportModel> SatReportsByApplicantNr { get; set; }
            public Dictionary<int, PartialCreditReportModel> CreditReportsByApplicantNr { get; set; }
            public bool? IsProviderApplication { get; set; }
            public string PetrusApplicationId { get; set; }
        }


        // ----------------- PRIVATE-------------------------
        // ----------------- PRIVATE-------------------------
        // ----------------- PRIVATE-------------------------

        private void AddComment(string text, string type, bool removeFromUiHide, DateTimeOffset now, string applicationNr)
        {
            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                var comment = new CreditApplicationComment
                {
                    ApplicationNr = applicationNr,
                    CommentText = CreditApplicationComment.CleanCommentText(text),
                    CommentDate = now,
                    ChangedDate = now,
                    CommentById = context.CurrentUserId,
                    ChangedById = context.CurrentUserId,
                    EventType = type,
                    InformationMetaData = context.InformationMetadata
                };

                if (removeFromUiHide)
                {
                    var a = context.CreditApplicationHeadersQueryable.SingleOrDefault(x => x.ApplicationNr == applicationNr && x.HideFromManualListsUntilDate.HasValue);
                    if (a != null)
                        a.HideFromManualListsUntilDate = null;
                }

                context.AddCreditApplicationComments(comment);
                context.SaveChanges();
            }
        }

        private void AppendPricingData(ScoringDataModel m, PartialCreditApplicationModel application, PricingResult pricing)
        {
            if (!pricing.SuggestedLoanAmount.HasValue || !pricing.SuggestedRepaymentTimeInMonths.HasValue)
            {
                m.Set("offeredLoanMonthlyCost", 0, null);
                m.Set("maxAllowedLoanAmount", 0m, null);
                m.Set("suggestedLoanAmount", 0m, null);
            }
            else
            {
                var referenceInterestRatePercent = referenceInterestRateService.GetCurrent();

                var terms = PaymentPlanCalculation
                    .BeginCreateWithRepaymentTime(pricing.SuggestedLoanAmount.Value, pricing.SuggestedRepaymentTimeInMonths.Value, pricing.InterestRate.Value + referenceInterestRatePercent, true, null, false)
                    .WithInitialFeeCapitalized(pricing.InitialFee ?? 0m)
                    .WithMonthlyFee(pricing.NotificationFee ?? 0m)
                    .EndCreate();

                var offeredLoanMonthlyCost = terms.AnnuityAmount + pricing.NotificationFee.GetValueOrDefault();
                m.Set("offeredLoanMonthlyCost", offeredLoanMonthlyCost, null);
                m.Set("maxAllowedLoanAmount", pricing.MaxLoanAmount.Value, null);
                m.Set("suggestedLoanAmount", pricing.SuggestedLoanAmount.Value, null);
            }
        }

        private int GetFullYearsSince(DateTimeOffset d)
        {
            var t = Today();
            if (t < d)
                return 0;

            var age = t.Year - d.Year;

            return (d.AddYears(age + 1) <= t) ? (age + 1) : age;
        }

        private BalanziaScoringVersion Version => BalanziaScoringVersion.Score2018;
        private Func<DateTimeOffset> Today { get { return () => clock.Now.Date; } }

        private void AppendInternalOtherApplicationsData(
            ScoringDataModel m,
            PartialCreditApplicationModel application,
            IDictionary<int, IList<HistoricalApplication>> otherApplicationsByApplicantNr)
        {
            application.DoForEachApplicant(applicantNr =>
            {
                var otherApplications = otherApplicationsByApplicantNr[applicantNr];

                var pausedUntilDate = otherApplications
                    ?.Where(x => x.PauseItems != null)
                    ?.SelectMany(x => x?.PauseItems?.Select(y => new { x.ApplicationNr, y.PausedUntilDate }))
                    ?.Where(x => x.PausedUntilDate >= Today())
                    ?.Max(x => (DateTime?)x.PausedUntilDate);
                m.Set("pausedDays", pausedUntilDate.HasValue ? ((int)Math.Round(pausedUntilDate.Value.Subtract(Today().DateTime.Date).TotalDays)) : 0, applicantNr);

                var activeOtherApplications = otherApplications.Where(x => !x.IsFinalDecisionMade && x.IsActive);
                var maxActiveApplicationDate = otherApplications.Where(x => !x.IsFinalDecisionMade && x.IsActive).Max(x => (DateTime?)x.ApplicationDate.Date.Date);
                var activeApplicationCount = activeOtherApplications.Count();
                m.Set("activeApplicationCount", activeApplicationCount, applicantNr);
                m.Set("maxActiveApplicationAgeInDays", maxActiveApplicationDate.HasValue ? ((int)Math.Round(Today().DateTime.Date.Subtract(maxActiveApplicationDate.Value).TotalDays)) : 0, applicantNr);

                var latestOtherHistorialApplication = otherApplications
                    .Where(x => !x.ArchivedDate.HasValue)
                    .OrderByDescending(x => x.ApplicationDate)
                    .ThenByDescending(x => x.ApplicationNr)
                    .FirstOrDefault() as HistoricalApplicationExtended;

                var latestOtherHistorialApplicationRejectionReasons = latestOtherHistorialApplication
                    ?.RejectionReasonSearchTerms
                    ?.ToList();

                m.Set("latestApplicationRejectionReasons", latestOtherHistorialApplicationRejectionReasons == null
                    ? "null"
                    : JsonConvert.SerializeObject(latestOtherHistorialApplicationRejectionReasons), applicantNr);

                string latestApplicationRejectionDate = null;
                int? latestApplicationRejectionAgeInDays = null;
                if (latestOtherHistorialApplication?.CurrentCreditDecisionDate != null && latestOtherHistorialApplicationRejectionReasons != null)
                {
                    latestApplicationRejectionDate = latestOtherHistorialApplication.CurrentCreditDecisionDate.Value.ToString("yyyy-MM-dd");
                    latestApplicationRejectionAgeInDays = (int)Math.Round(Today().DateTime.Date.Subtract(latestOtherHistorialApplication.CurrentCreditDecisionDate.Value.Date).TotalDays);
                }
                m.Set("latestApplicationRejectionDate", latestApplicationRejectionDate, applicantNr);
                m.Set("latestApplicationRejectionAgeInDays", latestApplicationRejectionAgeInDays, applicantNr);
            });
        }

        private void AppendInternalCreditHistoryData(
            ScoringDataModel m,
            PartialCreditApplicationModel application,
            Dictionary<int, List<HistoricalCreditExtended>> customerCreditHistoryByApplicantNr)
        {
            var thisApplicationCustomerIds = new HashSet<int>();

            application.DoForEachApplicant(applicantNr =>
            {
                var historicalCredits = customerCreditHistoryByApplicantNr[applicantNr];
                thisApplicationCustomerIds.Add(application.Applicant(applicantNr).Get("customerId").IntValue.Required);

                var currentlyOverdueSinceDate = historicalCredits.Min(x => x.CurrentlyOverdueSinceDate);
                var maxNrOfDaysBetweenDueDateAndPaymentEver = historicalCredits.Max(x => x.MaxNrOfDaysBetweenDueDateAndPaymentEver) ?? 0;
                var currentlyOverdueNrOfDays = currentlyOverdueSinceDate.HasValue ? Today().Date.Date.Subtract(currentlyOverdueSinceDate.Value.Date).Days : 0;
                var maxNrOfDaysBetweenDueDateAndPaymentLastSixMonths = historicalCredits.Max(x => x.MaxNrOfDaysBetweenDueDateAndPaymentLastSixMonths) ?? 0;
                var activeLoans = historicalCredits.Where(x => x.Status == "Normal");
                var minNrOfClosedNotificationsOnActiveLoans = activeLoans.Min(x => (int?)x.NrOfClosedNotifications) ?? 0;
                var nrOfActiveLoans = activeLoans.Count();
                var activeLoansBalance = activeLoans.Sum(x => x.CapitalBalance);

                var newestActiveLoan = activeLoans.OrderByDescending(x => x.StartDate).FirstOrDefault();
                decimal? newestActiveLoanTotalInterestRatePercent = null;
                decimal? newestActiveLoanAnnuity = null;
                decimal? newestActiveLoanInitialEffectiveInterestRatePercent = null;
                if (newestActiveLoan != null)
                {
                    newestActiveLoanTotalInterestRatePercent = newestActiveLoan.MarginInterestRatePercent + newestActiveLoan.ReferenceInterestRatePercent;
                    newestActiveLoanAnnuity = newestActiveLoan.AnnuityAmount;

                    //NOTE: When petrus 1 is removed we can replace HistoricalCredit with HistoricalCreditExtended everywhere for all clients and remove this ugly cast
                    if (newestActiveLoan?.InitialAnnuityAmount != null)
                    {
                        var initialPaymentPlan = PaymentPlanCalculation
                            .BeginCreateWithAnnuity(
                                newestActiveLoan.InitialCapitalBalance ?? 0m, newestActiveLoan.InitialAnnuityAmount ?? 0m,
                                (newestActiveLoan.InitialMarginInterestRatePercent ?? 0m) + (newestActiveLoan.InitialReferenceInterestRatePercent ?? 0m),
                                null,
                                false)
                            .WithInitialFeeCapitalized(newestActiveLoan.InitialCapitalizedInitialFeeAmount ?? 0m)
                            .WithMonthlyFee(newestActiveLoan.InitialNotificationFeeAmount ?? 0m)
                            .EndCreate();
                        newestActiveLoanInitialEffectiveInterestRatePercent = initialPaymentPlan.EffectiveInterestRatePercent;
                    }
                }

                m.Set("currentlyOverdueNrOfDays", currentlyOverdueNrOfDays, applicantNr);
                m.Set("maxNrOfDaysBetweenDueDateAndPaymentEver", maxNrOfDaysBetweenDueDateAndPaymentEver, applicantNr);
                m.Set("historicalDebtCollectionCount", historicalCredits.Where(x => x.IsOrHasBeenOnDebtCollection).Select(x => x.CreditNr).Count(), applicantNr);
                m.Set("maxNrOfDaysBetweenDueDateAndPaymentLastSixMonths", maxNrOfDaysBetweenDueDateAndPaymentLastSixMonths, applicantNr);
                m.Set("nrOfActiveLoans", nrOfActiveLoans, applicantNr);
                m.Set("existingCustomerBalance", activeLoansBalance, applicantNr);
                m.Set("minNrOfClosedNotificationsOnActiveLoans", minNrOfClosedNotificationsOnActiveLoans, applicantNr);
                m.Set("existingCustomerTotalInterestRatePercent", newestActiveLoanTotalInterestRatePercent, applicantNr);
                m.Set("existingCustomerAnnuity", newestActiveLoanAnnuity, applicantNr);
                m.Set("existingCustomerInitialEffectiveInterestRatePercent", newestActiveLoanInitialEffectiveInterestRatePercent, applicantNr);
            });

            List<HistoricalCreditExtended> customerCreditHistories = customerCreditHistoryByApplicantNr
                .Values
                .SelectMany(x => x)
                .GroupBy(x => x.CreditNr)
                .Select(x => x.First())
                .ToList();

            var existsActiveLoansThatAreNotShared = customerCreditHistories
                .Any(x => x.Status == "Normal" && !new HashSet<int>(x.CustomerIds).SetEquals(thisApplicationCustomerIds));

            m.Set("applicantsHaveSeparateLoans", existsActiveLoansThatAreNotShared, null);
        }

        private void AppendInternalData(ScoringDataModel m, PartialCreditApplicationModel application, decimal currentBalance,
            Dictionary<int, List<HistoricalCreditExtended>> customerCreditHistoryByApplicantNr,
            IDictionary<int, IList<HistoricalApplication>> otherApplicationsByApplicantNr,
            bool isProviderApplication, string applicationNr)
        {
            AppendInternalApplicationData(m, Version, application, currentBalance, isProviderApplication);
            AppendInternalCreditHistoryData(m, application, customerCreditHistoryByApplicantNr);
            AppendInternalOtherApplicationsData(m, application, otherApplicationsByApplicantNr);
            m.Set("randomNr", randomNrScoringVariableGenerator.GenerateRandomNrBetweenOneAndOneHundred(applicationNr), null);
            m.Set("randomNrRejectBelowLimit", randomNrScoringVariableGenerator.GetRejectBelowLimit(), null);
        }

        private void AppendInternalApplicationData(
            ScoringDataModel m,
            BalanziaScoringVersion balanziaScoringVersion,
            PartialCreditApplicationModel application,
            decimal currentBalance,
            bool isProviderApplication)
        {
            m.Set("nrOfApplicants", application.NrOfApplicants, null);
            m.Set("isProviderApplication", isProviderApplication, null);
            m.Set("scoreVersion", balanziaScoringVersion == BalanziaScoringVersion.Score2018 ? "2018" : "Original", null);
            m.Set("requestedAmount", application.Application.Get("amount").DecimalValue.Optional, null);
            m.Set("currentInternalLoanBalance", currentBalance, null);
            m.Set("campaignCode", application.Application.Get("campaignCode").StringValue.Optional ?? "null", null);
            m.Set("requestedRepaymentTimeInYears", application.Application.Get("repaymentTimeInYears").IntValue.Required, null);

            var loansToSettleAmount = application.Application.Get("loansToSettleAmount").DecimalValue.Optional ?? 0m;
            m.Set("isPurposeSettlement", loansToSettleAmount > 0m, null);

            Action<DateTimeOffset, int> addApplicantAgeInYears = (x, n) =>
            {
                var applicantAge = GetFullYearsSince(x);
                m.Set("ageInYears", applicantAge, applicantNr: n);
            };

            application.DoForEachApplicant(applicantNr =>
            {
                var applicant = application.Applicant(applicantNr);
                m.Set("incomePerMonthAmount", applicant.Get("incomePerMonthAmount").DecimalValue.Optional ?? 0m, applicantNr: applicantNr);
                m.Set("nrOfChildren", applicant.Get("nrOfChildren").IntValue.Optional ?? 0, applicantNr);
                m.Set("approvedSat", applicant.Get("approvedSat").BoolValue.Optional ?? false, applicantNr);

                var civicRegNrRaw = applicant.Get("civicRegNr").StringValue.Optional;
                if (civicRegNrRaw != null)
                {
                    var civicRegNr = civicRegNumberParser.Parse(civicRegNrRaw);
                    if (civicRegNr != null)
                    {
                        if (civicRegNr.IsMale.HasValue)
                            m.Set("isMale", civicRegNr.IsMale.Value, applicantNr: applicantNr);
                        if (!m.Exists("ageInYears", applicantNr) && civicRegNr.BirthDate.HasValue)
                            addApplicantAgeInYears(civicRegNr.BirthDate.Value, applicantNr);
                    }
                }

                var birthDate = applicant.Get("birthDate").DateValue.Optional;
                if (birthDate.HasValue)
                {
                    addApplicantAgeInYears(birthDate.Value, applicantNr);
                }

                m.Set("employment", TranslateBalanziaEmployment(applicant.Get("employment").StringValue.Optional), applicantNr: applicantNr);
                var employedSinceMonth = applicant.Get("employedSinceMonth").MonthValue(true).Optional;
                m.Set("currentEmploymentMonthCount", (employedSinceMonth.HasValue ? GetFullMonthsSince(employedSinceMonth.Value) : new int?()) ?? 0, applicantNr);

                m.Set("marriage", TranslateBalanziaMarriage(applicant.Get("marriage").StringValue.OptionalOneOf("marriage_gift", "marriage_ogift", "marriage_sambo")), applicantNr: applicantNr);
                m.Set("housing", TranslateBalanziaHousing(GetApplicantHousing(application, applicantNr)), applicantNr: applicantNr);

                var mortgageLoanAmount = applicant.Get("mortgageLoanAmount").DecimalValue.Optional ?? 0m;
                m.Set("mortgageLoanAmount", mortgageLoanAmount, applicantNr: applicantNr);
            });
        }

        private string GetApplicantHousing(PartialCreditApplicationModel model, int applicantNr)
        {
            Func<int, string> getHousing =
                an => model.Applicant(an).Get("housing").StringValue.OptionalOneOf("housing_egenbostad", "housing_bostadsratt", "housing_hyresbostad", "housing_hosforaldrar", "housing_tjanstebostad");

            var housing = getHousing(applicantNr);
            if (housing != null)
                return housing;

            var applicantsHaveSameAddress = (model.Application.Get("applicantsHaveSameAddress").BoolValue.Optional ?? false);
            if (!applicantsHaveSameAddress)
                return null;

            return getHousing(applicantNr == 1 ? 2 : 1);
        }

        private int GetFullMonthsSince(DateTimeOffset d)
        {
            var t = Today();
            if (t < d)
                return 0;

            var monthDiff = Math.Abs((t.Year * 12 + (t.Month - 1)) - (d.Year * 12 + (d.Month - 1)));

            if (d.AddMonths(monthDiff) > t || t.Day < d.Day)
            {
                return monthDiff - 1;
            }
            else
            {
                return monthDiff;
            }
        }

        private string TranslateBalanziaMarriage(string marriage)
        {
            if (string.IsNullOrWhiteSpace(marriage))
                return "unknown";

            if (marriage.Equals("marriage_gift", StringComparison.OrdinalIgnoreCase))
                return "married";

            if (marriage.Equals("marriage_ogift", StringComparison.OrdinalIgnoreCase))
                return "single";

            if (marriage.Equals("marriage_sambo", StringComparison.OrdinalIgnoreCase))
                return "cohabitant";

            return "unknown";
        }

        private string TranslateBalanziaEmployment(string employment)
        {
            if (string.IsNullOrWhiteSpace(employment))
                return "unknown";

            if (employment.Equals("employment_fastanstalld", StringComparison.OrdinalIgnoreCase))
                return "fulltime";

            if (employment.Equals("employment_visstidsanstalld", StringComparison.OrdinalIgnoreCase))
                return "temp";

            if (employment.Equals("employment_foretagare", StringComparison.OrdinalIgnoreCase))
                return "own";

            if (employment.Equals("employment_pensionar", StringComparison.OrdinalIgnoreCase))
                return "retired";

            if (employment.Equals("employment_sjukpensionar", StringComparison.OrdinalIgnoreCase))
                return "preretired";

            if (employment.Equals("employment_studerande", StringComparison.OrdinalIgnoreCase))
                return "student";

            if (employment.Equals("employment_arbetslos", StringComparison.OrdinalIgnoreCase))
                return "unemployed";

            return "unknown";
        }

        private string TranslateBalanziaHousing(string housing)
        {
            if (string.IsNullOrWhiteSpace(housing))
                return "unknown";

            var h = housing.ToLowerInvariant();
            if (h == "housing_egenbostad") return "owned";
            if (h == "housing_bostadsratt") return "condominium";
            if (h == "housing_hyresbostad") return "rentedapartment";
            if (h == "housing_tjanstebostad") return "servicehousing";
            if (h == "housing_hosforaldrar") return "withparents";

            return "unknown";
        }

        private class CreditCheckResultStrict
        {
            public bool Success { get; set; }            
            public ScoringResult ScoringResult { get; set; }
            public IDictionary<int, IList<HistoricalApplication>> OtherApplicationsByApplicantNr { get; set; }
            public IDictionary<int, List<HistoricalCreditExtended>> CustomerCreditHistoryItemsByApplicantNr { get; set; }
            public string ApplicationNr { get; set; }
        }

        private CreditCheckResult ConvertStrictCreditCheckResult(CreditCheckResultStrict result)
        {
            if (!result.Success)
            {
                return new CreditCheckResult
                {
                    UsesUnsupportedScoringVersion = false,
                    Success = result.Success
                };
            }

            var otherApplications = new ExpandoObject();
            foreach (var applicantNr in result.OtherApplicationsByApplicantNr.Keys.OrderBy(x => x))
            {
                ((IDictionary<string, object>)otherApplications)["applicant" + applicantNr] = result.OtherApplicationsByApplicantNr[applicantNr];
            }

            var credits = new ExpandoObject();
            foreach (var applicantNr in result.CustomerCreditHistoryItemsByApplicantNr.Keys.OrderBy(x => x))
            {
                ((IDictionary<string, object>)credits)["applicant" + applicantNr] = result.CustomerCreditHistoryItemsByApplicantNr[applicantNr];
            }

            //Get the current reference interest rate
            var referenceInterestRatePercent = creditClient.GetCurrentReferenceInterest();

            return new CreditCheckResult
            {
                applicationNr = result.ApplicationNr,
                notificationFeeAmount = result?.ScoringResult?.OfferedNotificationFeeAmount, //TODO: Why is this here at all?
                otherApplications = otherApplications,
                creditCheckResult = result.ScoringResult,
                credits = credits,
                referenceInterestRatePercent = referenceInterestRatePercent,
                Success = result.Success,
                creditReportsUsed = null
            };
        }

        private class CreditCheckResult
        {
            public bool Success { get; set; }
            public string CreditCheckProviderThatIsDown { get; set; }
            public bool IsCheckProviderIsDown { get; set; }
            public bool IsInvalidCredentialsError { get; set; }
            public bool UsesUnsupportedScoringVersion { get; set; }

            public ScoringResult creditCheckResult { get; set; }
            public ExpandoObject otherApplications { get; set; }
            public ExpandoObject credits { get; set; }
            public string applicationNr { get; set; }
            public decimal referenceInterestRatePercent { get; set; }
            public decimal? notificationFeeAmount { get; set; }
            public IList<ScoringBasisCreditReport> creditReportsUsed { get; set; }
        }
        
        private class DoCompleteCreditCheckRequest
        {
            public bool isAccepted { get; set; }
            //Accept
            public decimal? amount { get; set; }
            public int? repaymentTimeInMonths { get; set; }
            public decimal? marginInterestRatePercent { get; set; }
            public decimal? initialFeeAmount { get; set; }
            public decimal? notificationFeeAmount { get; set; }
            public decimal? referenceInterestRatePercent { get; set; }
            public AdditionalLoanOfferAcceptModel additionalLoanOffer { get; set; }
            //Reject
            public List<string> rejectionReasons { get; set; }
        }

        private void DoCompleteCreditCheck(
            string applicationNr,
            PartialCreditApplicationModel application,
            AffiliateModel affiliate,
            ScoringResult scoringResult,
            DoCompleteCreditCheckRequest request,
            out string userWarningMessage)
        {
            var isAccepted = request.isAccepted;

            //Accept
            var amount = request.amount;
            var repaymentTimeInMonths = request.repaymentTimeInMonths;
            decimal? marginInterestRatePercent = request.marginInterestRatePercent;
            decimal? initialFeeAmount = request.initialFeeAmount;
            decimal? notificationFeeAmount = request.notificationFeeAmount;
            decimal? referenceInterestRatePercent = request.referenceInterestRatePercent;
            AdditionalLoanOfferAcceptModel additionalLoanOffer = request.additionalLoanOffer;
            //Reject
            List<string> rejectionReasons = request.rejectionReasons;

            DoCompleteCreditCheckLegacyCreditHistoryModel credits = null;
            DoCompleteCreditCheckLegacyOtherApplicationsModel otherApplications = null;
            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                var scoringDataService = new LegacyUlApplicationScoringDataService();
                var customerScoringData = scoringDataService.GetApplicationScoringDataForCustomers(applicationNr, context, customerServiceRepository, creditClient);
                credits = new DoCompleteCreditCheckLegacyCreditHistoryModel
                {
                    CustomerCreditHistoryItemsByApplicantNr = customerScoringData
                        .CustomerCreditHistoryByApplicantNr
                        .ToDictionary(x => x.Key, x => x.Value.ToList())
                };
                otherApplications = new DoCompleteCreditCheckLegacyOtherApplicationsModel
                {
                    OtherApplicationsByApplicantNr = customerScoringData.OtherApplications
                };
            }

            var isAdditionalLoanOffer = !(additionalLoanOffer == null || !additionalLoanOffer.AdditionalLoanAmount.HasValue);
            if (isAccepted)
            {
                var e = new DoCompleteCreditCheckStrictResult();
                if (!isAdditionalLoanOffer)
                {
                    var offer = new
                    {
                        amount = amount.Value,
                        repaymentTimeInMonths = repaymentTimeInMonths.Value,
                        marginInterestRatePercent = marginInterestRatePercent.Value,
                        referenceInterestRatePercent = referenceInterestRatePercent.Value,
                        initialFeeAmount = initialFeeAmount ?? 0m,
                        notificationFeeAmount = notificationFeeAmount ?? 0m,
                    };
                    var terms = PaymentPlanCalculation
                        .BeginCreateWithRepaymentTime(offer.amount, offer.repaymentTimeInMonths, offer.marginInterestRatePercent + offer.referenceInterestRatePercent, true, null, envSettings.CreditsUse360DayInterestYear)
                        .WithMonthlyFee(offer.notificationFeeAmount)
                        .WithInitialFeeCapitalized(offer.initialFeeAmount)
                        .EndCreate();

                    e.offer = new DoCompleteCreditCheckStrictResult.OfferModel
                    {
                        amount = offer.amount,
                        repaymentTimeInMonths = offer.repaymentTimeInMonths,
                        marginInterestRatePercent = offer.marginInterestRatePercent,
                        referenceInterestRatePercent = offer.referenceInterestRatePercent,
                        initialFeeAmount = offer.initialFeeAmount,
                        notificationFeeAmount = offer.notificationFeeAmount,
                        annuityAmount = terms.AnnuityAmount,
                        effectiveInterestRatePercent = terms.EffectiveInterestRatePercent,
                        totalPaidAmount = terms.TotalPaidAmount,
                        initialPaidToCustomerAmount = terms.InitialPaidToCustomerAmount
                    };
                }
                else
                {
                    if (amount.HasValue)
                        throw new Exception("amount cannot be used with additional loan");
                    if (initialFeeAmount.HasValue)
                        throw new Exception("initialFeeAmount cannot be used with additional loan");
                    if (repaymentTimeInMonths.HasValue)
                        throw new Exception("repaymentTimeInMonths cannot be used with additional loan");
                    if (referenceInterestRatePercent.HasValue)
                        throw new Exception("Reference interest cannot be changed on an existing loan using additional loans");
                    if (notificationFeeAmount.HasValue)
                        throw new Exception("Margin interest cannot be changed on an existing loan using additional loans");

                    if (!additionalLoanOffer.AdditionalLoanAmount.HasValue)
                        throw new Exception("Missing amount in additionalLoanOffer");
                    if (string.IsNullOrWhiteSpace(additionalLoanOffer.AdditionalLoanCreditNr))
                        throw new Exception("Missing creditNr in additionalLoanOffer");

                    e.additionalLoanOffer = new DoCompleteCreditCheckStrictResult.AdditionalLoanOfferModel
                    {
                        amount = additionalLoanOffer.AdditionalLoanAmount.Value,
                        creditNr = additionalLoanOffer.AdditionalLoanCreditNr,
                        newAnnuityAmount = additionalLoanOffer.NewAnnuityAmount,
                        newMarginInterestRatePercent = additionalLoanOffer.NewMarginInterestRatePercent,
                        newNotificationFeeAmount = additionalLoanOffer.NewNotificationFeeAmount
                    };

                    string invalidAdditionalLoanMessage;
                    if (!IsValidAdditionalLoan(application, e.additionalLoanOffer, out invalidAdditionalLoanMessage))
                    {
                        throw new Exception($"Invalid additional loan on application {applicationNr}: " + invalidAdditionalLoanMessage);
                    }
                }

                AppendDecisionBasisStrict(e, application, otherApplications, credits, scoringResult);

                //This is before the update so we can abort if it fails
                bool additionalQuestionsSent = false;
                if (affiliate.IsUsingDirectLinkFlow && affiliate.IsSendingAdditionalQuestionsEmail)
                {
                    SendAdditionalQuestions(applicationNr, application, isAdditionalLoanOffer, additionalQuestionsSender);
                    additionalQuestionsSent = true;
                }

                var now = this.clock.Now;
                UpdateApplicationWithAcceptedDecision(applicationNr, e);

                publishEventService.Publish(PreCreditEventCode.CreditApplicationCreditCheckAccepted, JsonConvert.SerializeObject(new
                {
                    applicationNr = applicationNr,
                    wasAutomated = true,
                    providerName = affiliate?.ProviderName
                }));
                if (additionalQuestionsSent)
                    additionalQuestionsSender.EmitAdditionalQuestionsSentEvent(applicationNr); //This is done here instead of when sending to get the events in the correct order

                userWarningMessage = null;
            }
            else
            {
                var e = new DoCompleteCreditCheckStrictResult();

                var ed = e as IDictionary<string, object>;
                e.rejectionReasons = rejectionReasons;

                AppendDecisionBasisStrict(e, application, otherApplications, credits, scoringResult);

                var now = this.clock.Now;
                UpdateApplicationWithRejectedDecision(
                    applicationNr,
                    e,
                    GetCustomerIdsForApplication(application));

                var autoRejectResult = rejectionService.TryRejectApplication(applicationNr, true);
                userWarningMessage = autoRejectResult.UserWarningMessage;

                publishEventService.Publish(PreCreditEventCode.CreditApplicationCreditCheckRejected, JsonConvert.SerializeObject(new { applicationNr = applicationNr, wasAutomated = true }));
            }
        }

        private void SendAdditionalQuestions(string applicationNr, PartialCreditApplicationModel application, bool isAdditionalLoanOffer, IAdditionalQuestionsSender sender)
        {
            var customerIdByApplicantNr = new Dictionary<int, int>();
            application.DoForEachApplicant(applicantNr =>
            {
                var customerId = application.Applicant(applicantNr).Get("customerId").IntValue.Optional;
                if (customerId.HasValue)
                    customerIdByApplicantNr[applicantNr] = customerId.Value;
            });
            var result = sender.SendSendAdditionalQuestionsEmail(
                applicationNr,
                isAdditionalLoanOffer,
                application.NrOfApplicants,
                customerIdByApplicantNr,
                false);
            if (!result.success)
                throw new SendAdditionalQuestionsEmailFailedException(result.failedMessage);
        }

        private IList<int> GetCustomerIdsForApplication(PartialCreditApplicationModel model)
        {
            var result = new List<int>();
            model.DoForEachApplicant(applicantNr =>
            {
                result.Add(model.Applicant(applicantNr).Get("customerId").IntValue.Required);
            });
            return result;
        }

        private void UpdateApplicationWithAcceptedDecision(string applicationNr, DoCompleteCreditCheckStrictResult e)
        {
            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                var app = context.CreditApplicationHeadersQueryable.Single(x => x.ApplicationNr == applicationNr);
                app.CreditCheckStatus = CreditApplicationMarkerStatusName.Accepted;
                var decision = new AcceptedCreditDecision
                {
                    AcceptedDecisionModel = JsonConvert.SerializeObject(e.ToLegacyResult()),
                    DecisionDate = context.CoreClock.Now,
                    DecisionById = context.CurrentUserId,
                    CreditApplication = app,
                    WasAutomated = true
                };
                context.FillInfrastructureFields(decision);
                app.CurrentCreditDecision = decision;

                var additionalLoanCreditNr = e.additionalLoanOffer?.creditNr;
                context.CreateAndAddComment(
                    "New credit check accepted" + (additionalLoanCreditNr == null ? "" : $" as additional loan for {additionalLoanCreditNr}") + " (automated)",
                    "ApplicationCreditCheckAccepted", applicationNr: applicationNr);
                
                context.AddCreditDecisions(decision);
                context.SaveChanges();
            }
        }

        private void UpdateApplicationWithRejectedDecision(string applicationNr, DoCompleteCreditCheckStrictResult e, IList<int> applicationCustomerIds)
        {
            var scoringModel = envSettings.ScoringSetup;
            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                var app = context.CreditApplicationHeadersQueryable.Single(x => x.ApplicationNr == applicationNr);
                app.CreditCheckStatus = CreditApplicationMarkerStatusName.Rejected;
                var decision = new RejectedCreditDecision
                {
                    RejectedDecisionModel = JsonConvert.SerializeObject(e.ToLegacyResult()),
                    DecisionDate = context.CoreClock.Now,
                    DecisionById = context.CurrentUserId,
                    CreditApplication = app,
                    WasAutomated = true
                };
                app.CurrentCreditDecision = decision;
                context.FillInfrastructureFields(decision);

                CreditCheckCompletionProviderApplicationUpdater.AddRejectionReasonSearchTerms(e.rejectionReasons, scoringModel.IsKnownRejectionReason, decision, context);
                context.CreateAndAddComment("New credit check rejected (automated)", "ApplicationCreditCheckRejected", applicationNr: applicationNr);

                var getPauseDaysByRejectionReason = envSettings.ScoringSetup.GetRejectionReasonToPauseDaysMapping();
                CreditCheckCompletionProviderApplicationUpdater.AddRejectionReasonPauseDayItems(e?.rejectionReasons, getPauseDaysByRejectionReason, applicationCustomerIds.ToHashSetShared(), context, decision);
                                
                context.AddCreditDecisions(decision);
                context.SaveChanges();
            }
        }

        private void AppendDecisionBasisStrict(DoCompleteCreditCheckStrictResult e,
            PartialCreditApplicationModel application,
            DoCompleteCreditCheckLegacyOtherApplicationsModel otherApplications,
            DoCompleteCreditCheckLegacyCreditHistoryModel credits,
            ScoringResult scoringResult)
        {
            e.recommendation = scoringResult;
            e.application = application;
            e.otherApplications = otherApplications;
            e.credits = credits;
        }

        private bool IsValidAdditionalLoan(PartialCreditApplicationModel application, DoCompleteCreditCheckStrictResult.AdditionalLoanOfferModel additionalLoanOffer, out string invalidReasonMessage)
        {
            var applicationCustomerIds = new HashSet<int>();
            application.DoForEachApplicant(applicantNr =>
            {
                applicationCustomerIds.Add(application.Applicant(applicantNr).Get("customerId").IntValue.Required);
            });

            var h = creditClient.GetCustomerCreditHistoryByCreditNrs(new List<string>() { additionalLoanOffer.creditNr }).SingleOrDefault();
            if (h == null)
            {
                invalidReasonMessage = $"No such credit: {additionalLoanOffer.creditNr}";
                return false;
            }

            var creditCustomerIds = new HashSet<int>(h.CustomerIds);
            if (!applicationCustomerIds.SetEquals(creditCustomerIds))
            {
                invalidReasonMessage = $"The credit {additionalLoanOffer.creditNr} does not have the same customers as the application.";
                return false;
            }

            try
            {
                var b = PaymentPlanCalculation.BeginCreateWithAnnuity(
                    (h.CapitalBalance + additionalLoanOffer.amount),
                    (additionalLoanOffer.newAnnuityAmount ?? h.AnnuityAmount).Value,
                    (additionalLoanOffer.newMarginInterestRatePercent.HasValue
                        ? additionalLoanOffer.newMarginInterestRatePercent.Value
                        : h.MarginInterestRatePercent.Value) + h.ReferenceInterestRatePercent.Value, null, envSettings.CreditsUse360DayInterestYear);
                if (h.NotificationFeeAmount.HasValue)
                    b = b.WithMonthlyFee(h.NotificationFeeAmount.Value);
                var terms = b.EndCreate();
                if (terms.Payments.Count > 12 * 30)
                {
                    invalidReasonMessage = $"Nr of payments {terms.Payments.Count} seems impossibly high";
                    return false;
                }
            }
            catch (PaymentPlanCalculationException ex)
            {
                invalidReasonMessage = ex.Message;
                return false;
            }

            invalidReasonMessage = null;
            return true;
        }

        private CreditCheckResultStrict CreditCheckSimple(string applicationNr, out PartialCreditApplicationModel applicationModel)
        {
            ScoringDataContext dataContext = null;
            var calculateResult = CaclulateCreditCheck(applicationNr, x => dataContext = x);

            applicationModel = dataContext?.Application;

            if (!calculateResult.Success)
            {
                return new CreditCheckResultStrict
                {
                    Success = false,
                };
            }
            else
            {
                return new CreditCheckResultStrict
                {
                    Success = true,
                    ScoringResult = calculateResult.SuccessResult,
                    OtherApplicationsByApplicantNr = dataContext?.OtherApplicationsByApplicantNr,
                    CustomerCreditHistoryItemsByApplicantNr = dataContext?.CustomerCreditHistoryByApplicantNr,
                    ApplicationNr = applicationNr
                };
            }
        }

        private static PartialCreditApplicationModelRequest GetApplicationRequestedFields()
        {
            var request = new PartialCreditApplicationModelRequest
            {
                ApplicationFields = new List<string>()
                {
                    "amount",
                    "repaymentTimeInYears",
                    "loansToSettleAmount",
                    "forceExternalScoring",
                    "campaignCode",
                    "scoringVersion",
                    "applicantsHaveSameAddress"
                },
                ApplicantFields = new List<string>()
                {
                    "customerId",
                    "civicRegNr",
                    "birthDate",
                    "education",
                    "housing",
                    "housingCostPerMonthAmount",
                    "employment",
                    "employedSinceMonth",
                    "incomePerMonthAmount",
                    "marriage",
                    "mortgageLoanAmount",
                    "mortgageLoanCostPerMonthAmount",
                    "carOrBoatLoanAmount",
                    "carOrBoatLoanCostPerMonthAmount",
                    "studentLoanAmount",
                    "studentLoanCostPerMonthAmount",
                    "otherLoanAmount",
                    "otherLoanCostPerMonthAmount",
                    "creditCardAmount",
                    "creditCardCostPerMonthAmount",
                    "nrOfChildren",
                    "employer",
                    "employerPhone",
                    "approvedSat"
                }
            };

            request.ApplicantFields = request
                .ApplicantFields
                .Concat(PetrusOnlyRequestBuilder.RequiredApplicantFields)
                .DistinctPreservingOrder()
                .ToList();

            return request;
        }

        private ScoringCalculateResult CaclulateCreditCheck(string applicationNr, Action<ScoringDataContext> observeDataContext)
        {
            var application = GetPartialCreditApplicationModelExtendedForCreditCheck(applicationNr, partialCreditApplicationModelRepository);

            var applicantNrsAndCustomerIds = new List<Tuple<int, int>>();
            foreach (var applicantNr in Enumerable.Range(1, application.NrOfApplicants))
            {
                var customerId = application.Applicant(applicantNr).Get("customerId").IntValue.Required;
                applicantNrsAndCustomerIds.Add(Tuple.Create(applicantNr, customerId));
            }

            var scoringDataService = new LegacyUlApplicationScoringDataService();

            var customerScoringData = scoringDataService.GetApplicationScoringDataForCustomers(applicationNr, applicantNrsAndCustomerIds, customerServiceRepository, creditClient);

            var customerCreditHistoryByApplicantNr = customerScoringData.CustomerCreditHistoryByApplicantNr;
            var otherApplicationsByApplicantNr = customerScoringData.OtherApplications;

            var isProviderApplication = !envSettings.GetAffiliateModel(application.CustomData.ProviderName, allowMissing: false).IsSelf;
            var cc = new Lazy<ICustomerClient>(() => customerClient);

            var testingVariationSet = abTestingService.GetVariationSetForApplication(applicationNr);
            var dataContext = new ScoringDataContext(testingVariationSet)
            {
                ApplicationNr = applicationNr,
                IsProviderApplication = isProviderApplication,
                Application = application,
                CustomerCreditHistoryByApplicantNr = customerCreditHistoryByApplicantNr,
                OtherApplicationsByApplicantNr = otherApplicationsByApplicantNr,
                ScoringData = new ScoringDataModel()
            };
            observeDataContext(dataContext);
            List<HistoricalCreditExtended> customerCreditHistories = dataContext.CustomerCreditHistoryByApplicantNr
                           .Values
                           .SelectMany(x => x)
                           .GroupBy(x => x.CreditNr)
                           .Select(x => x.First())
                           .ToList();

            var thisApplicationCustomerIds = new HashSet<int>();
            dataContext.Application.DoForEachApplicant(applicantNr =>
            {
                thisApplicationCustomerIds.Add(dataContext.Application.Applicant(applicantNr).Get("customerId").IntValue.Required);
            });

            var activeLoansWithAnyOfTheseApplicants = customerCreditHistories
                .Where(x => x.Status == "Normal" && new HashSet<int>(x.CustomerIds).Overlaps(thisApplicationCustomerIds));

            dataContext.CurrentBalance = activeLoansWithAnyOfTheseApplicants.Aggregate(0m, (a, b) => a + b.CapitalBalance);

            AppendInternalData(dataContext.ScoringData, dataContext.Application, dataContext.CurrentBalance.Value, dataContext.CustomerCreditHistoryByApplicantNr, dataContext.OtherApplicationsByApplicantNr, dataContext.IsProviderApplication.Value,
                dataContext.ApplicationNr);

            //Minimum Demands
            List<MinimumDemandRejection> minimumDemandRejections = new List<MinimumDemandRejection>();

            var minimumDemandRules = new List<MinimumDemandScoringRule>()
            {
                new HistoricalLateLoansRule(),
                new HistoricalDebtCollectionRule(),
                new HistoricalLateNotificationsRule(),
                new RandomlyRejectedRule()
            };

            var disabledScoringRuleNames = envSettings.DisabledScoringRuleNames ?? new List<string>();

            if (!envSettings.DisabledScoringRuleNames.Contains("ActivePendingApplication"))
                minimumDemandRules.Add(new ActivePendingApplicationRule());

            if (!envSettings.IsCoApplicantScoringRuleDisabled)
                minimumDemandRules.Add(new CoApplicantRule());

            minimumDemandRejections.AddRange(CheckMinimumDemandRulesR(dataContext.ScoringData, minimumDemandRules.ToArray()));

            var rejectionMapping = envSettings.ScoringSetup.GetScoringRuleToRejectionReasonMapping();
            var minimumDemandRejectionReasons = minimumDemandRejections
                .Select(x => rejectionMapping[x.ReasonCode])
                .Distinct()
                .ToList();
            
            if (minimumDemandRejectionReasons.Any())
            {
                return CreateScoringCalculateResult(dataContext, minimumDemandRejectionReasons, null);
            }

            var petrusService = petrusFactory.GetService();
            var petrusResult = petrusService.NewCreditCheck(new PetrusOnlyCreditCheckRequest
            {
                ProviderName = application.CustomData.ProviderName,
                DataContext = dataContext,
                ReferenceInterestRatePercent = creditClient.GetCurrentReferenceInterest()
            });
            if (petrusResult.LoanApplicationId != null)
            {
                if (dataContext.Application.Application.Get("petrusApplicationId").StringValue.Optional != petrusResult.LoanApplicationId)
                {
                    using (var context = preCreditContextFactoryService.CreateExtended())
                    {
                        CreditApplicationItemService.SetNonEncryptedItemComposable(context, applicationNr, "petrusApplicationId", "application", petrusResult.LoanApplicationId, "CreditCheck");
                        context.SaveChanges();
                    }
                }
                dataContext.PetrusApplicationId = petrusResult.LoanApplicationId;
            }

            /*
             * Instead of rejecting with AdditionalLoanRule in minimum demannds we send these application to brocc so they can start building their scoring data
             * (by request from them). However we have not worked out how to let them accept additional loans yet so for now we agreed that they will always reject those.
             * This is just a safetly net for if that guard fails.
             */
            if (petrusResult.Accepted)
            {
                dataContext.Application.DoForEachApplicant(applicantNr =>
                {
                    if ((dataContext.ScoringData.GetInt("nrOfActiveLoans", applicantNr) ?? 0) > 0)
                    {
                        throw new NTechCoreWebserviceException($"Petrus accepted on an application with active loans which would lead to an additional loan. This is currently not supported: {applicationNr}")
                        {
                            ErrorCode = "petrusAdditionalLoanGuard"
                        };
                    }
                });
            }

            PricingResult pricing;
            if (petrusResult.Accepted)
            {
                UpdateCustomerAddressUsingPetrusResult(petrusResult, dataContext);
                pricing = new PricingResult
                {
                    SuggestedLoanAmount = petrusResult.Offer.Amount,
                    MaxLoanAmount = petrusResult.Offer.Amount,
                    InitialFee = petrusResult.Offer.InitialFeeAmount,
                    InterestRate = petrusResult.Offer.MarginInterestRatePercent,
                    NotificationFee = petrusResult.Offer.NotificationFeeAmount,
                    SuggestedRepaymentTimeInMonths = petrusResult.Offer.RepaymentTimeInMonths
                };
            }
            else
            {
                pricing = new PricingResult
                {
                    SuggestedLoanAmount = null,
                    InitialFee = null,
                    InterestRate = null,
                    NotificationFee = null,
                    SuggestedRepaymentTimeInMonths = null
                };
            }

            AppendPricingData(dataContext.ScoringData, dataContext.Application, pricing);

            /*
             NOTE: If we change this to allow brocc to set any rejectionReason they wish make sure that we normalize so that for instance all of:
            - paymentremark, paymentRemark, PaymentRemark and so on are all mapped to paymentRemark on our end. (And the same for all other rules)
             */
            List<string> rejectionReasons = null;
            if (!pricing.SuggestedLoanAmount.HasValue)
            {
                var petrusRejectionReason = petrusResult.RejectionReason ?? FallbackRejectionReason;
                var actualRejectionReason = envSettings.ScoringSetup.RejectionReasons.SingleOrDefault(x => x.Name.EqualsIgnoreCase(petrusRejectionReason))?.Name ?? FallbackRejectionReason;
                rejectionReasons = new List<string> { actualRejectionReason };
            }
            return CreateScoringCalculateResult(dataContext, rejectionReasons, pricing);
        }

        private bool UpdateCustomerAddressUsingPetrusResult(PetrusOnlyCreditCheckResponse petrusResult, ScoringDataContext dataContext)
        {
            var civicRegNr = CivicRegNumberFi.Parse(dataContext.Application.Applicant(1).Get("civicRegNr").StringValue.Required);
            var customerId = dataContext.Application.Applicant(1).Get("customerId").IntValue.Required;
            var a = petrusResult.MainApplicant;
            Func<string, bool> isSensitive = s => s != "firstName" && s != "birthDate";
            var itemNames = new List<string>();
            var customerItems = new List<CustomerClientCustomerPropertyModel>();
            void AddItem(string name, string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    customerItems.Add(new CustomerClientCustomerPropertyModel
                    {
                        CustomerId = customerId,
                        IsSensitive = isSensitive(name),
                        Group = isSensitive(name) ? "sensitive" : "official",
                        Name = name,
                        Value = value,
                        ForceUpdate = true //Since this comes from a trusted source we overwrite any current values
                    });
                }
            }

            AddItem("lastName", a?.LastName);
            AddItem("firstName", a?.FirstName);
            AddItem("addressStreet", a?.StreetAddress);
            AddItem("addressZipcode", a?.ZipCode);
            AddItem("addressCity", a?.City);
            if (string.IsNullOrWhiteSpace(a?.ZipCode))
            {
                AddItem("addressCountry", "FI");
            }
            if (civicRegNr.BirthDate.HasValue)
            {
                AddItem("birthDate", civicRegNr.BirthDate.Value.ToString("yyyy-MM-dd"));
            }

            if (customerItems.Count > 0)
            {
                customerClient.UpdateCustomerCard(customerItems, false);
                return true;
            }

            return false;
        }

        private ScoringCalculateResult CreateScoringCalculateResult(
            ScoringDataContext dataContext,
            List<string> rejectionReasons,
            PricingResult pricing)
        {
            var result = new ScoringResult();

            Func<Dictionary<string, decimal>, List<string>> toDebugItems = r => r == null
                ? null
                : r.Select(x => string.Format(CultureInfo.InvariantCulture, "{0}: {1:f2}", x.Key, x.Value)).ToList();

            if (rejectionReasons == null || !rejectionReasons.Any())
            {
                if (pricing == null || !pricing.MaxLoanAmount.HasValue || !pricing.InterestRate.HasValue)
                    throw new Exception("Logic error. No pricing info even though the application got accepted by petrus: " + dataContext.ApplicationNr);

                var requestedAmount = dataContext.Application.Application.Get("amount").DecimalValue.Required;

                result.HasOffer = true;
                result.MaxOfferedAmount = pricing.MaxLoanAmount;
                result.OfferedInterestRatePercent = pricing.InterestRate;
                result.OfferedRepaymentTimeInMonths = pricing.SuggestedRepaymentTimeInMonths ?? 12 * dataContext.Application.Application.Get("repaymentTimeInYears").IntValue.Required;

                if (pricing.SuggestedLoanAmount.HasValue)
                    result.OfferedAmount = pricing.SuggestedLoanAmount;
                else if (requestedAmount > result.MaxOfferedAmount)
                    result.OfferedAmount = result.MaxOfferedAmount;
                else
                    result.OfferedAmount = requestedAmount;

                result.OfferedNotificationFeeAmount = pricing.NotificationFee;
                result.OfferedInitialFeeAmount = pricing.InitialFee;
            }
            else
                result.RejectionReasons = rejectionReasons;

            result.ScoringData = ScoringDataModelFlat.FromModel(dataContext.ScoringData);

            result.PetrusVersion = 2;
            result.PetrusApplicationId = dataContext.PetrusApplicationId;

            return new ScoringCalculateResult
            {
                Success = true,
                SuccessResult = result
            };
        }

        private Tuple<List<string>, HashSet<string>> CheckMinimumDemandRules(ScoringDataModel dataModel, params MinimumDemandScoringRule[] rules)
        {
            var context = new ScoringContext();
            foreach (var r in rules)
                r.Score(dataModel, context);

            return Tuple.Create(context.Rejections.ToList(), new HashSet<string>());
        }

        private List<MinimumDemandRejection> CheckMinimumDemandRulesR(ScoringDataModel dataModel, params MinimumDemandScoringRule[] rules)
        {
            var r = CheckMinimumDemandRules(dataModel, rules);

            return r.Item1.Select(x => new MinimumDemandRejection
            {
                ReasonCode = x,
                DebugDecisionBasis = null
            }).ToList();
        }

        private class DoCompleteCreditCheckStrictResult
        {
            public List<string> rejectionReasons { get; set; }
            public ScoringResult recommendation { get; set; }
            public PartialCreditApplicationModel application { get; set; }
            public DoCompleteCreditCheckLegacyOtherApplicationsModel otherApplications { get; set; }
            public DoCompleteCreditCheckLegacyCreditHistoryModel credits { get; set; }
            public bool kycScreenFailed { get; set; }

            public class OfferModel
            {
                public decimal amount { get; set; }
                public int repaymentTimeInMonths { get; set; }
                public decimal marginInterestRatePercent { get; set; }
                public decimal referenceInterestRatePercent { get; set; }
                public decimal initialFeeAmount { get; set; }
                public decimal notificationFeeAmount { get; set; }
                public decimal annuityAmount { get; set; }
                public decimal? effectiveInterestRatePercent { get; set; }
                public decimal totalPaidAmount { get; set; }
                public decimal initialPaidToCustomerAmount { get; set; }
            }
            public OfferModel offer { get; set; }

            public class AdditionalLoanOfferModel
            {
                public decimal amount { get; set; }
                public decimal? newMarginInterestRatePercent { get; set; }
                public decimal? newAnnuityAmount { get; set; }
                public decimal? newNotificationFeeAmount { get; set; }
                public string creditNr { get; set; }
            }

            public AdditionalLoanOfferModel additionalLoanOffer { get; set; }

            public ExpandoObject ToLegacyResult(string userWarningMessage = null)
            {
                var e = new ExpandoObject();
                var ed = e as IDictionary<string, object>;

                if (userWarningMessage != null)
                    ed["userWarningMessage"] = userWarningMessage;

                if (offer != null || additionalLoanOffer != null)
                {
                    if (additionalLoanOffer == null)
                    {
                        ed["offer"] = new
                        {
                            offer.amount,
                            offer.repaymentTimeInMonths,
                            offer.marginInterestRatePercent,
                            offer.referenceInterestRatePercent,
                            offer.initialFeeAmount,
                            offer.notificationFeeAmount,
                            annuityAmount = offer.annuityAmount,
                            effectiveInterestRatePercent = offer.effectiveInterestRatePercent,
                            totalPaidAmount = offer.totalPaidAmount,
                            initialPaidToCustomerAmount = offer.initialPaidToCustomerAmount
                        };
                    }
                    else
                    {
                        ed["additionalLoanOffer"] = new
                        {
                            additionalLoanOffer.creditNr,
                            additionalLoanOffer.amount,
                            additionalLoanOffer.newAnnuityAmount,
                            additionalLoanOffer.newMarginInterestRatePercent,
                            additionalLoanOffer.newNotificationFeeAmount
                        };
                    }

                    AppendDecisionBasis(e, this.recommendation, this.application, this.otherApplications.ToJson(), this.credits.ToJson());

                    if (kycScreenFailed)
                    {
                        ed["kycScreenFailed"] = true;
                    }
                }
                else
                {
                    ed["rejectionReasons"] = this.rejectionReasons;
                    AppendDecisionBasis(e, this.recommendation, this.application, this.otherApplications.ToJson(), this.credits.ToJson());
                }

                return e;
            }

            private T DeepClone<T>(T item) where T : class
            {
                return item == null ? null : JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(item));
            }

            private void AppendDecisionBasis(ExpandoObject e, ScoringResult recommendation, PartialCreditApplicationModel application, string otherApplicationsJson, string creditsJson)
            {
                var ed = e as IDictionary<string, object>;

                //Filter out sensitive data from the decision basis
                var applicationClone = PartialCreditApplicationModel.FromJson(application.ToJson());
                var recommendationClone = DeepClone(recommendation);

                applicationClone.DoForEachApplicant(applicantNr =>
                {
                    applicationClone.RemoveIfExists($"applicant{applicantNr}", "civicRegNr");
                });

                ed["recommendation"] = recommendationClone;
                ed["application"] = JsonConvert.DeserializeObject(applicationClone.ToJson());
                ed["otherApplications"] = JsonConvert.DeserializeObject(otherApplicationsJson);
                ed["credits"] = JsonConvert.DeserializeObject(creditsJson);
            }
        }
    }

    public class ScoringCalculateResult
    {
        public bool Success { get; set; }
        public ScoringResult SuccessResult { get; set; }
    }
}
