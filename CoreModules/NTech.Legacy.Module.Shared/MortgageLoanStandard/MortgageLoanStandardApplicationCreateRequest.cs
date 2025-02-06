using NTech.Services.Infrastructure.CreditStandard;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NTech.Services.Infrastructure.MortgageLoanStandard
{
    public class MortgageLoanStandardApplicationCreateRequest
    {
        public string CustomerExternalIpAddress { get; set; }
        public string ProviderApplicationId { get; set; }
        [EnumCode(EnumType = typeof(ObjectTypeCodeValue))]
        public string ObjectTypeCode { get; set; }
        public string SeBrfApartmentNr { get; set; }
        public string ObjectAddressStreet { get; set; }
        public string ObjectAddressZipcode { get; set; }
        public string ObjectAddressCity { get; set; }
        public string ObjectAddressMunicipality { get; set; }
        public string ObjectAddressCounty { get; set; }
        public int? ObjectMonthlyFeeAmount { get; set; }
        public int? ObjectLivingArea { get; set; }
        public int? ObjectOtherMonthlyCostsAmount { get; set; }
        public int? OutgoingChildSupportAmount { get; set; }
        public int? IncomingChildSupportAmount { get; set; }
        public int? ChildBenefitAmount { get; set; }
        /// <summary>
        /// Can be used instead of sending in invividual children using HouseholdChildren if nothing is known about them. If combined with HouseholdChildren  the counts must match.
        /// </summary>
        public int? NrOfHouseholdChildren { get; set; }
        public List<ChildModel> HouseholdChildren { get; set; }
        public List<LoanToSettleModel> LoansToSettle { get; set; }
        [Required]
        public List<ApplicantModel> Applicants { get; set; }
        public PurchaseModel Purchase { get; set; }
        public ChangeExistingLoanModel ChangeExistingLoan { get; set; }
        [Required]
        public MetadataModel Meta { get; set; }

        public class ApplicantModel
        {
            [Required]
            [CivicRegNr]
            public string CivicRegNr { get; set; }
            public bool? IsPartOfTheHousehold { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public bool? HasConsentedToCreditReport { get; set; }
            public bool? HasConsentedToShareBankAccountData { get; set; }

            [EnumCode(EnumType = typeof(CreditStandardEmployment.Code))]
            public string Employment { get; set; }
            public string Employer { get; set; }
            [DateWithoutTime(AllowMonthOnly = true)]
            public string EmployedSince { get; set; }
            [DateWithoutTime(AllowMonthOnly = true)]
            public string EmployedTo { get; set; }
            public string EmployerPhone { get; set; }
            public int? IncomePerMonthAmount { get; set; }
        }

        public class PurchaseModel
        {
            [Required]
            public int? ObjectPriceAmount { get; set; }
            public int? OwnSavingsAmount { get; set; }
        }

        public class ChangeExistingLoanModel
        {
            public int? ObjectValueAmount { get; set; }
            public int? PaidToCustomerAmount { get; set; }
            [Required]
            public List<MortgageLoanToSettleModel> MortgageLoansToSettle { get; set; }
        }

        public class MortgageLoanToSettleModel
        {
            [Required]
            public int? CurrentDebtAmount { get; set; }
            [Required]
            public bool? ShouldBeSettled { get; set; }
            public string BankName { get; set; }
            public int? CurrentMonthlyAmortizationAmount { get; set; }
            public decimal? InterestRatePercent { get; set; }
            public string LoanNumber { get; set; }
        }

        public class LoanToSettleModel
        {
            [Required]
            public int? CurrentDebtAmount { get; set; }
            [EnumCode(EnumType = typeof(CreditStandardOtherLoanType.Code))]
            public string LoanType { get; set; }
            public int? MonthlyCostAmount { get; set; }
        }

        public class MetadataModel
        {
            [Required]
            public string ProviderName { get; set; }
        }

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

        //NOTE: Cant be named ObjectTypeCode because c# is wierd
        public enum ObjectTypeCodeValue
        {
            seBrf, seFastighet
        }
    }
}
