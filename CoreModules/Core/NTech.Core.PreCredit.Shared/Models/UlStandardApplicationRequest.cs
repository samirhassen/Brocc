using NTech.Services.Infrastructure.CreditStandard;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NTech.Core.PreCredit.Shared.Models
{
    public class UlStandardApplicationRequest : IValidatableObject
    {
        [Required]
        public int? RequestedAmount { get; set; }

        public int? LoansToSettleAmount { get; set; }
        public string LoanObjective { get; set; }

        public int? RequestedRepaymentTimeInMonths { get; set; }
        public int? RequestedRepaymentTimeInDays { get; set; }
        public int? ChildBenefitAmount { get; set; }

        public string ProviderApplicationId { get; set; }
        public string PreScoreResultId { get; set; }

        [Required]
        public List<ApplicantModel> Applicants { get; set; }

        public int? NrOfHouseholdChildren { get; set; }
        public List<ChildModel> HouseholdChildren { get; set; }

        [EnumCode(EnumType = typeof(CreditStandardHousingType.Code))]
        public string HousingType { get; set; }
        public int? HousingCostPerMonthAmount { get; set; }
        public int? OtherHouseholdFixedCostsAmount { get; set; }

        /// <summary>
        /// This can only be used from secure sources and cannot be combined with Applicants DataShare properties
        /// </summary>
        public List<UlStandardApplicationBankDataShareApplicantModel> BankDataShareApplicants { get; set; }

        public class ApplicantModel
        {
            [Required]
            [CivicRegNr]
            public string CivicRegNr { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }

            public string Email { get; set; }

            public string Phone { get; set; }

            public DateTime? BirthDate { get; set; }

            public bool? IsOnPepList { get; set; }

            public bool? ClaimsToBePep { get; set; }

            public bool? ClaimsToHaveKfmDebt { get; set; }

            [EnumCode(EnumType = typeof(CreditStandardCivilStatus.Code))]
            public string CivilStatus { get; set; }

            public int? MonthlyIncomeAmount { get; set; }

            [EnumCode(EnumType = typeof(CreditStandardEmployment.Code))]
            public string EmploymentStatus { get; set; }
            public string EmployerName { get; set; }
            public string EmployerPhone { get; set; }
            public DateTime? EmployedSince { get; set; }
            public DateTime? EmployedTo { get; set; }
            public string AddressStreet { get; set; }
            public string AddressZipcode { get; set; }
            public string AddressCity { get; set; }

            public bool? HasConsentedToCreditReport { get; set; }
            public bool? HasConsentedToShareBankAccountData { get; set; }
            public bool? HasLegalOrFinancialGuardian { get; set; }
            public bool? ClaimsToBeGuarantor { get; set; }
            public string DataShareProviderName { get; set; }
            public string DataShareSessionId { get; set; }
            public bool HasDataShare() => DataShareProviderName != null || DataShareSessionId != null;
        }

        [Required]
        public MetadataModel Meta { get; set; }

        public class MetadataModel
        {
            [Required]
            public string ProviderName { get; set; }
            public string CustomerExternalIpAddress { get; set; }
            public bool? SkipHideFromManualUserLists { get; set; }
            public bool? SkipInitialScoring { get; set; }
            public bool? SupressUserNotification { get; set; }
        }

        public class OtherLoanModel
        {
            [EnumCode(EnumType = typeof(CreditStandardOtherLoanType.Code))]
            public string LoanType { get; set; }
            public int? CurrentDebtAmount { get; set; }
            public int? MonthlyCostAmount { get; set; }
            public decimal? CurrentInterestRatePercent { get; set; }
            public bool? ShouldBeSettled { get; set; }
        }

        public List<OtherLoanModel> HouseholdOtherLoans { get; set; }
        public class ChildModel
        {
            /// <summary>
            /// Not actually imported. This can be used by the sender if their serialization framework prunes empty objects
            /// if they want to send in a single child with no more known data
            /// </summary>
            public bool? Exists { get; set; }
            public int? AgeInYears { get; set; }
            public bool? SharedCustody { get; set; }
        }

        private ValidationResult Err(string errorMessage, params string[] memberNames) => new ValidationResult(errorMessage, memberNames);
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (RequestedRepaymentTimeInDays.HasValue == RequestedRepaymentTimeInMonths.HasValue)
                yield return Err("Exactly one of RequestedRepaymentTimeInDays or RequestedRepaymentTimeInMonths must be included", nameof(RequestedRepaymentTimeInDays), nameof(RequestedRepaymentTimeInMonths));

            if (RequestedRepaymentTimeInDays.HasValue && (RequestedRepaymentTimeInDays.Value < 7 || RequestedRepaymentTimeInDays.Value > 30))
                yield return Err("RequestedRepaymentTimeInDays must be >= 7 and <= 30");

            if (RequestedRepaymentTimeInMonths.HasValue && RequestedRepaymentTimeInMonths.Value < 1)
                yield return Err("RequestedRepaymentTimeInMonths must be >= 1");

            if (HouseholdChildren != null && NrOfHouseholdChildren.HasValue && HouseholdChildren.Count != NrOfHouseholdChildren.Value)
                yield return Err("When both HouseholdChildren and NrOfHouseholdChildren are used the counts must match");
        }
    }

    public class UlStandardApplicationResponse
    {
        public string ApplicationNr { get; set; }
        public string DecisionStatus { get; set; }
        public List<string> RejectionCodes { get; set; }
    }

    public class UlStandardApplicationBankDataShareApplicantModel
    {
        [Required]
        public int ApplicantNr { get; set; }
        public string ProviderName { get; set; }
        public string ProviderSessionId { get; set; }
        public int? IncomeAmount { get; set; }
        public int? LtlAmount { get; set; }
        public string ProviderDataArchiveKey { get; set; }
    }
}
