using Newtonsoft.Json;
using NTech;
using NTech.Banking.MortgageLoans;
using System;
using System.Collections.Generic;

namespace nPreCredit.Code.Services
{
    public class MortgageLoanAmortizationService : IMortgageLoanAmortizationService
    {
        private readonly IClock clock;
        private readonly IMortgageLoanApplicationValuationService mortgageLoanApplicationValuationService;
        private readonly IPartialCreditApplicationModelService partialCreditApplicationModelService;
        private readonly int? clientAmortizationFreePeriodLength;
        private readonly decimal? clientMinimumAmortizationPercent;
        private readonly IMortgageLoanWorkflowService mortgageLoanWorkflowService;
        private readonly KeyValueStore amortizationModelStore;

        public MortgageLoanAmortizationService(IKeyValueStoreService keyValueStoreService, IClock clock,
            IMortgageLoanApplicationValuationService mortgageLoanApplicationValuationService, IPartialCreditApplicationModelService partialCreditApplicationModelService,
            int? clientAmortizationFreePeriodLength,
            decimal? clientMinimumAmortizationPercent,
            IMortgageLoanWorkflowService mortgageLoanWorkflowService)
        {
            this.clock = clock;
            this.mortgageLoanApplicationValuationService = mortgageLoanApplicationValuationService;
            this.partialCreditApplicationModelService = partialCreditApplicationModelService;
            this.clientAmortizationFreePeriodLength = clientAmortizationFreePeriodLength;
            this.clientMinimumAmortizationPercent = clientMinimumAmortizationPercent;
            this.mortgageLoanWorkflowService = mortgageLoanWorkflowService;
            this.amortizationModelStore = new KeyValueStore(KeyValueStoreKeySpaceCode.MortgageLoanAmortizationModelV1, keyValueStoreService);
        }

        public const string WorkflowStepName = "MortgageLoanAmortizationBasis";

        public class StandardBankFormModel
        {
            public DateOnly AmortizationBasisDate { get; set; }
            public decimal AmortizationBasisObjectValue { get; set; }
            public decimal AmortizationBasisLoanAmount { get; set; }
            public decimal RuleNoneCurrentAmount { get; set; }
            public decimal RuleR201616CurrentAmount { get; set; }
            public decimal RuleR201723CurrentAmount { get; set; }
            public decimal RuleAlternateCurrentAmount { get; set; }
            public decimal AlternateRuleAmortizationAmount { get; set; }
        }

        public class OtherCalculationData
        {
            public decimal CombinedGrossMonthlyIncome { get; set; }
            public decimal CombinedCurrentOtherMortgageLoansAmount { get; set; }
            public decimal? RequestedAmortizationAmount { get; set; }
        }

        public class Settings
        {
            /// <summary>
            /// Not a government requirement but something the client sets. For instance to compute a reasonable suggestion when the customer has zero requirement
            /// and to have a fallback after the amortization free period is over.
            /// </summary>
            public decimal? ClientMinimumAmortizationPercent { get; set; }

            /// <summary>
            /// When required amortization is 0 and requested is 0 this is how long (in months) the amortization free period will be before falling back to actual
            /// </summary>
            public int? ClientAmortizationFreePeriodLength { get; set; }
        }

        public static MortageLoanAmortizationModel CalculateBasedOnBestGuessShared(decimal loanAmount, decimal objectValue, OtherCalculationData data, Settings settings, IClock clock)
        {
            var bf = new StandardBankFormModel
            {
                AmortizationBasisDate = DateOnly.Create(clock.Today),
                AmortizationBasisLoanAmount = loanAmount,
                AmortizationBasisObjectValue = objectValue,
                RuleR201723CurrentAmount = loanAmount
            };
            return CalculateSuggestionBasedOnStandardBankFormShared(bf, data, settings, clock);
        }

        public static MortageLoanAmortizationModel CalculateSuggestionBasedOnStandardBankFormShared(StandardBankFormModel bankForm, OtherCalculationData data, Settings settings, IClock clock)
        {
            var yearlyIncome = data.CombinedGrossMonthlyIncome * 12;
            //NOTE: Not a mistake that alternate is not included since that is how much of the rest is alternate
            var currentLoanAmount = bankForm.RuleNoneCurrentAmount + bankForm.RuleR201616CurrentAmount + bankForm.RuleR201723CurrentAmount;
            var currentCombinedTotalLoanAmount = currentLoanAmount + data.CombinedCurrentOtherMortgageLoansAmount;

            MortageLoanAmortizationRuleCode rule;
            if (bankForm.RuleAlternateCurrentAmount > 0m && bankForm.RuleAlternateCurrentAmount == currentLoanAmount)
                rule = MortageLoanAmortizationRuleCode.alternate;
            else if (bankForm.RuleR201723CurrentAmount == currentLoanAmount)
                rule = MortageLoanAmortizationRuleCode.r201723;
            else if (bankForm.RuleR201616CurrentAmount == currentLoanAmount)
                rule = MortageLoanAmortizationRuleCode.r201616;
            else if (bankForm.RuleNoneCurrentAmount == currentLoanAmount)
                rule = MortageLoanAmortizationRuleCode.none;
            else
                rule = MortageLoanAmortizationRuleCode.r201723;

            decimal requiredAmortizationAmount;
            decimal? requiredAlternateAmortizationAmount = null;
            if (rule == MortageLoanAmortizationRuleCode.r201616)
                requiredAmortizationAmount = MortgageLoanAmortizationRules.CalcluateMimimumAmortizationUsingR201616(
                    bankForm.AmortizationBasisLoanAmount, bankForm.AmortizationBasisObjectValue);
            else if (rule == MortageLoanAmortizationRuleCode.r201723)
                requiredAmortizationAmount = MortgageLoanAmortizationRules.CalcluateMimimumAmortizationUsingR201723(
                    bankForm.AmortizationBasisLoanAmount, data.CombinedCurrentOtherMortgageLoansAmount, bankForm.AmortizationBasisObjectValue,
                    data.CombinedGrossMonthlyIncome, debtMultiplierLoanAmount: currentLoanAmount);
            else if (rule == MortageLoanAmortizationRuleCode.none)
                requiredAmortizationAmount = 0m;
            else if (rule == MortageLoanAmortizationRuleCode.alternate)
            {
                requiredAlternateAmortizationAmount = bankForm.AlternateRuleAmortizationAmount;
                requiredAmortizationAmount = bankForm.AlternateRuleAmortizationAmount;
            }
            else
                throw new NotImplementedException();

            var actualAmortizationAmount = requiredAmortizationAmount;

            var clientMinimumAmortizationPercent = settings.ClientMinimumAmortizationPercent;
            if (clientMinimumAmortizationPercent.HasValue && clientMinimumAmortizationPercent.Value > 0m)
            {
                var clientMinimumAmortizationAmount = Math.Round(currentLoanAmount * clientMinimumAmortizationPercent.Value / 100m / 12m);
                if (actualAmortizationAmount < clientMinimumAmortizationAmount)
                    actualAmortizationAmount = clientMinimumAmortizationAmount;
            }

            DateTime? amortizationFreeUntilDate = null;
            if (requiredAmortizationAmount == 0m && data.RequestedAmortizationAmount.HasValue && data.RequestedAmortizationAmount.Value == 0m && settings.ClientAmortizationFreePeriodLength.HasValue)
            {
                amortizationFreeUntilDate = clock.Today.AddMonths(settings.ClientAmortizationFreePeriodLength.Value);
            }

            if (data.RequestedAmortizationAmount.HasValue && actualAmortizationAmount < data.RequestedAmortizationAmount.Value)
                actualAmortizationAmount = data.RequestedAmortizationAmount.Value;

            return new MortageLoanAmortizationModel
            {
                CurrentLoanAmount = currentLoanAmount,
                AmortizationRule = rule.ToString(),
                CurrentCombinedTotalLoanAmount = currentCombinedTotalLoanAmount,
                CurrentCombinedYearlyIncomeAmount = yearlyIncome,
                ActualAmortizationAmount = actualAmortizationAmount,
                RequiredAmortizationAmount = requiredAmortizationAmount,
                RequiredAlternateAmortizationAmount = requiredAlternateAmortizationAmount,
                AmortizationBasisDate = bankForm.AmortizationBasisDate,
                AmortizationBasisLoanAmount = bankForm.AmortizationBasisLoanAmount,
                AmortizationBasisObjectValue = bankForm.AmortizationBasisObjectValue,
                TransactionDate = DateOnly.Create(clock.Today.Date),
                AmortizationFreeUntilDate = DateOnly.Create(amortizationFreeUntilDate),
                AmortizationExceptionReasons = null,
                AmortizationExceptionUntilDate = null,
                ExceptionAmortizationAmount = null
            };
        }

        public StandardBankFormModel CreateBankFormGuessBasedOnCurrentData(ApplicationInfoModel applicationInfo, bool useNoneAsDefaultRule = false)
        {
            var app = partialCreditApplicationModelService.Get(applicationInfo.ApplicationNr, new PartialCreditApplicationModelRequest
            {
                ApplicationFields = new List<string> { "mortgageLoanCurrentLoanAmount", "mortgageLoanHouseValueAmount" },
                ErrorIfGetNonLoadedField = true
            });

            var valuationData = this.mortgageLoanApplicationValuationService.FetchStatus(applicationInfo, null);
            decimal? objectValue = null;
            if (valuationData != null)
                objectValue = StringItem.ParseDecimal(valuationData?.ValuationItems?.Opt("brfLghVarde"));
            else
                objectValue = app.Application.Get("mortgageLoanHouseValueAmount").DecimalValue.Required;

            var loanAmount = app.Application.Get("mortgageLoanCurrentLoanAmount").DecimalValue.Required;

            return new StandardBankFormModel
            {
                AmortizationBasisDate = DateOnly.Create(clock.Today),
                RuleR201723CurrentAmount = useNoneAsDefaultRule ? 0m : loanAmount,
                RuleNoneCurrentAmount = useNoneAsDefaultRule ? loanAmount : 0m,
                AmortizationBasisLoanAmount = loanAmount,
                AmortizationBasisObjectValue = objectValue.Value
            };
        }

        //TODO: Appsetting
        private Settings GetSettings()
        {
            return new Settings
            {
                ClientAmortizationFreePeriodLength = this.clientAmortizationFreePeriodLength,
                ClientMinimumAmortizationPercent = this.clientMinimumAmortizationPercent
            };
        }

        public MortageLoanAmortizationModel CalculateModelSuggestionBasedOnStandardBankForm(string applicationNr, StandardBankFormModel bankForm, decimal? requestedAmortizationAmount)
        {
            var applicationModel = this.partialCreditApplicationModelService.Get(applicationNr, new PartialCreditApplicationModelRequest
            {
                ErrorIfGetNonLoadedField = true,
                ApplicantFields = new List<string>() { "incomePerMonthAmount", "mortgageLoanAmount" }
            });

            var totalCombinedOtherMortgageLoansAmount = 0m;
            var totalCombinedGrossMonthlyIncomeAmount = 0m;
            applicationModel.DoForEachApplicant(applicantNr =>
            {
                totalCombinedOtherMortgageLoansAmount += applicationModel.Applicant(applicantNr).Get("mortgageLoanAmount").DecimalValue.Required;
                totalCombinedGrossMonthlyIncomeAmount += applicationModel.Applicant(applicantNr).Get("incomePerMonthAmount").DecimalValue.Required;
            });

            return CalculateSuggestionBasedOnStandardBankFormShared(bankForm, new OtherCalculationData
            {
                RequestedAmortizationAmount = requestedAmortizationAmount ?? 0m,
                CombinedCurrentOtherMortgageLoansAmount = totalCombinedOtherMortgageLoansAmount,
                CombinedGrossMonthlyIncome = totalCombinedGrossMonthlyIncomeAmount
            }, GetSettings(), this.clock);
        }

        public MortageLoanAmortizationModel GetModel(string applicationNr)
        {
            var v = amortizationModelStore.GetValue(applicationNr);
            if (v == null)
                return null;
            return JsonConvert.DeserializeObject<MortageLoanAmortizationModel>(v);
        }

        public void SetModel(string applicationNr, MortageLoanAmortizationModel model)
        {
            if (applicationNr == null)
                throw new ArgumentNullException("applicationNr", "applicationNr cannot be null");
            if (model == null)
                throw new ArgumentNullException("model", "model cannot be null");

            amortizationModelStore.SetValue(
                applicationNr,
                JsonConvert.SerializeObject(model));

            mortgageLoanWorkflowService.ChangeStepStatus(WorkflowStepName, mortgageLoanWorkflowService.AcceptedStatusName, applicationNr: applicationNr);
        }
    }

    public interface IMortgageLoanAmortizationService
    {
        MortageLoanAmortizationModel GetModel(string applicationNr);
        void SetModel(string applicationNr, MortageLoanAmortizationModel model);
        MortgageLoanAmortizationService.StandardBankFormModel CreateBankFormGuessBasedOnCurrentData(ApplicationInfoModel applicationInfo, bool useNoneAsDefaultRule = false);
        MortageLoanAmortizationModel CalculateModelSuggestionBasedOnStandardBankForm(string applicationNr, MortgageLoanAmortizationService.StandardBankFormModel bankForm, decimal? requestedAmortizationAmount);
    }

    public class MortageLoanAmortizationModel
    {
        /// <summary>
        /// CurrentLoanAmount is current per this date and so on.
        /// </summary>
        public DateOnly TransactionDate { get; set; }

        /// <summary>
        /// Amortization chosen. Never lower than required but can be higher if the customer wants to pay faster.
        /// </summary>
        public decimal? ActualAmortizationAmount { get; set; }

        /// <summary>
        /// Amortization used instead of ActualAmortizationAmount during the time until exception until date
        /// </summary>
        public decimal? ExceptionAmortizationAmount { get; set; }

        /// <summary>
        /// Minimum required amortization amount using any rule. Note that this is only for history. This has to be recomputed live every time
        /// when actually changing amortization since the strict rule is dependet on current values.
        /// </summary>
        public decimal? RequiredAmortizationAmount { get; set; }

        /// <summary>
        /// Minimum required amortization amount when using the alternate rule. Cannot be computed since we don't know the initial loan amount
        /// and actual amortization amount can be higher. This is needed when creating an amortization basis for another bank.
        /// </summary>
        public decimal? RequiredAlternateAmortizationAmount { get; set; }

        /// <summary>
        /// Actual amortization will be 0 until this date is passed then it will fall back to ActualAmortizationAmount
        /// </summary>
        public DateOnly AmortizationExceptionUntilDate { get; set; }

        /// <summary>
        /// Reasons for exception. Can be one of Nyproduktion, Lantbruksenhet, Sjukdom, Arbetslöshet, Dödsfall
        /// </summary>
        public List<string> AmortizationExceptionReasons { get; set; }

        /// <summary>
        /// When RequiredAmortizationAmount is 0 and the customer want zero we set ActualAmortizationAmount to our default minimum (which is not a regualtory requirement)
        /// and we set this date which will cause the actual amortization to be 0 until this date is passed and then it falls back to our minimum.
        /// </summary>
        public DateOnly AmortizationFreeUntilDate { get; set; }

        /// <summary>
        /// One of the amortization codes in MortageLoanAmortizationRuleCode
        /// </summary>
        public string AmortizationRule { get; set; }

        /// <summary>
        /// The object value used for amortization calculation. When moving loans will typically be from the other bank.
        /// </summary>
        public decimal? AmortizationBasisObjectValue { get; set; }

        /// <summary>
        /// The date of the object value used for amortization calculation. When moving loans will typically be from the other bank.
        /// </summary>
        public DateOnly AmortizationBasisDate { get; set; }

        /// <summary>
        /// The loan amount ysed for amortization calculation. When moving loans will typically be from the other bank.
        /// </summary>
        public decimal? AmortizationBasisLoanAmount { get; set; }

        /// <summary>
        /// We store the current income since it's used to compute the debt income ratio for r201723
        /// </summary>
        public decimal? CurrentCombinedYearlyIncomeAmount { get; set; }

        /// <summary>
        /// Total loans used for calculating loan income ratio for r201723. At the time of writing it is defined as CurrentLoanAmount + total other mortgage loans among the applicants.
        /// </summary>
        public decimal? CurrentCombinedTotalLoanAmount { get; set; }

        /// <summary>
        /// Loan amount now. This will be the amount that will be eventually sent to the credit module.
        /// </summary>
        public decimal? CurrentLoanAmount { get; set; }
    }
}