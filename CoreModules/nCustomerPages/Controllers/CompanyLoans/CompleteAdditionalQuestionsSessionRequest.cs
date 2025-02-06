using System.ComponentModel.DataAnnotations;

namespace nCustomerPages.Controllers.CompanyLoans
{
    public class CompleteAdditionalQuestionsSessionRequest
    {
        [Required]
        public string QuestionsSubmissionToken { get; set; }

        [Required]
        public string BankAccountNr { get; set; }

        [Required]
        public string BankAccountNrType { get; set; }
    }
}