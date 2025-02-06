namespace NTech.Core.PreCredit.Shared.Code.PetrusOnlyScoringService
{
    public class PetrusOnlyCreditCheckResponse
    {
        public bool Accepted { get; set; }
        public string LoanApplicationId { get; set; }
        public string RejectionReason { get; set; }

        public class OfferModel
        {
            public decimal? Amount { get; set; }
            public decimal? MarginInterestRatePercent { get; set; }
            public int? RepaymentTimeInMonths { get; set; }
            public decimal? InitialFeeAmount { get; set; }
            public decimal? NotificationFeeAmount { get; set; }
        }

        public class ApplicantModel
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string StreetAddress { get; set; }
            public string ZipCode { get; set; }
            public string City { get; set; }
        }

        public OfferModel Offer { get; set; }
        public ApplicantModel MainApplicant { get; set; }

        //Will possibly be added in the future
        //public ApplicantModel CoApplicant { get; set; }
    }
}
