using System;
using System.Collections.Generic;

namespace nCustomerPages.Code
{
    public class CreateCompanyLoanApplicationResponse
    {
        public string ApplicationNr { get; set; }
        public string DecisionStatus { get; set; }

        public OfferModel Offer { get; set; }
        public List<string> RejectionCodes { get; set; }

        public class OfferModel
        {
            public decimal LoanAmount { get; set; }
            public decimal AnnuityAmount { get; set; }
            public decimal NominalInterestRatePercent { get; set; }
            public decimal MonthlyFeeAmount { get; set; }
            public decimal InitialFeeAmount { get; set; }
        }
    }

    public class CompanyLoanStartAdditionalQuestionsStatusResponse
    {
        public string ApplicationNr { get; set; }

        public bool IsPendingAnswers { get; set; }

        public DateTime? AnswerableSinceDate { get; set; }

        public string AdditionalQuestionsLink { get; set; }

        public DateTime? AnsweredDate { get; set; }

        public string AnswersDocumentKey { get; set; }

        public OfferModel Offer { get; set; }

        public CompanyCompanyInformationModel CompanyInformation { get; set; }

        public class CompanyCompanyInformationModel
        {
            public string Orgnr { get; set; }
            public string Name { get; set; }
        }

        public class OfferModel
        {
            public decimal? LoanAmount { get; set; }
            public decimal? NominalInterestRatePercent { get; set; }
            public decimal? ReferenceInterestRatePercent { get; set; }
            public decimal? MonthlyFeeAmount { get; set; }
            public decimal? InitialFeeAmount { get; set; }
            public decimal? AnnuityAmount { get; set; }
            public int? RepaymentTimeInMonths { get; set; }
            public decimal? TotalInterestRatePercent { get; set; }
            public decimal? TotalPaidAmount { get; set; }
            public decimal? EffectiveInterestRatePercent { get; set; }
        }
    }
}