using System.Collections.Generic;

namespace nCustomerPages.Code
{

    public class MortgageLoanAnswerAdditionalQuestionsRequest
    {
        public class Item
        {
            public string QuestionCode { get; set; }
            public string QuestionText { get; set; }
            public int? ApplicantNr { get; set; }
            public string AnswerCode { get; set; }
            public string AnswerText { get; set; }
            public string QuestionGroup { get; set; }
        }

        public class CurrentMortgageLoanModel
        {
            public string BankName { get; set; }
            public int? MonthlyAmortizationAmount { get; set; }
            public int? CurrentBalance { get; set; }
            public string LoanNr { get; set; }
        }

        public List<CurrentMortgageLoanModel> Loans { get; set; }
        public List<Item> QuestionsAndAnswers { get; set; }
        public string Token { get; set; }
        public string BankAccountNr { get; set; }
        public decimal? RequestedAmortizationAmount { get; set; }
    }
}