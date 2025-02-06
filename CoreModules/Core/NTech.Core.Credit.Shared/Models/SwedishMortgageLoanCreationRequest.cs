using nCredit.DbModel.BusinessEvents;
using NTech.Core.Module.Shared.Infrastructure.CoreValidation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NTech.Core.Credit.Shared.Models
{
    public partial class SwedishMortgageLoanCreationRequest
    {
        public CollateralModel NewCollateral { get; set; }

        /// <summary>
        /// Can only be used in one api request
        /// All the loans with the same agreement nr are co notified
        /// </summary>
        public string AgreementNr { get; set; }

        [PositiveNumber]
        public int? ExistingCollateralId { get; set; }

        [Required]
        public List<SeMortgageLoanModel> Loans { get; set; }

        [Required]
        public SwedishMortgageLoanAmortizationBasisModel AmortizationBasis { get; set; }

        public class CollateralModel
        {
            public bool IsBrfApartment { get; set; }
            public string BrfOrgNr { get; set; }
            public string BrfName { get; set; }
            public string BrfApartmentNr { get; set; }
            public string TaxOfficeApartmentNr { get; set; }
            /// <summary>
            /// Fastighetsbeteckning
            /// </summary>
            public string ObjectId { get; set; }
            public string AddressStreet { get; set; }
            public string AddressZipcode { get; set; }
            public string AddressCity { get; set; }
            public string AddressMunicipality { get; set; }
        }
        public class SeMortgageLoanModel
        {
            [NonNegativeNumber()]
            public decimal MonthlyFeeAmount { get; set; }

            [NonNegativeNumber()]
            public decimal? FixedMonthlyAmortizationAmount { get; set; }

            public List<string> AmortizationExceptionReasons { get; set; }
            public DateTime? AmortizationExceptionUntilDate { get; set; }

            [NonNegativeNumber()]
            public decimal? ExceptionAmortizationAmount { get; set; }

            public MortgageLoanRequest.ActiveDirectDebitAccountModel ActiveDirectDebitAccount { get; set; }

            [PositiveNumber]
            public decimal? LoanAmount { get; set; }

            [Required]
            [ListSizeRange(1, 2)]
            public List<MortgageLoanRequest.Applicant> Applicants { get; set; }

            public string KycQuestionsJsonDocumentArchiveKey { get; set; }
            public string ApplicationNr { get; set; }
            public string ProviderApplicationId { get; set; }
            
            [Required]
            public string CreditNr { get; set; }
            
            public List<MortgageLoanRequest.Document> Documents { get; set; }
            
            [Required]
            public string ProviderName { get; set; }
            
            [Required]
            public DateTime? EndDate { get; set; }

            [Required]
            [PositiveNumber]
            public int? InterestRebindMounthCount { get; set; }

            [Required]
            public DateTime? NextInterestRebindDate { get; set; }

            [Required]
            public decimal NominalInterestRatePercent { get; set; }

            [Required]
            public decimal? ReferenceInterestRate { get; set; }

            public List<int> ConsentingPartyCustomerIds { get; set; }

            [Required]
            public List<int> PropertyOwnerCustomerIds { get; set; }

            [NonNegativeNumber]
            public decimal? DrawnFromLoanAmountInitialFeeAmount { get; set; }

            public string LoanOwnerName { get; set; }

            public List<FirstNotificationCostItem> FirstNotificationCosts { get; set; }
            public class FirstNotificationCostItem
            {
                /// <summary>
                /// Cost code. Code must be registred first.
                /// </summary>
                [Required]
                public string CostCode { get; set; }

                /// <summary>
                /// Cost amount
                /// </summary>
                [Required]
                public decimal CostAmount { get; set; }
            }
        }
    }
}