using nCredit.DbModel.BusinessEvents;
using NTech.Core.Credit.Shared.Database;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class TerminationLetterInactivationService
    {
        private readonly CreditTerminationLettersInactivationBusinessEventManager terminationLettersInactivationManager;
        private readonly CreditContextFactory creditContextFactory;

        public TerminationLetterInactivationService(CreditTerminationLettersInactivationBusinessEventManager terminationLettersInactivationManager, CreditContextFactory creditContextFactory)
        {
            this.terminationLettersInactivationManager = terminationLettersInactivationManager;
            this.creditContextFactory = creditContextFactory;
        }

        public InactivateTerminationLettersResult InactivateTerminationLetters(InactivateTerminationLettersRequest request)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var result = terminationLettersInactivationManager.InactivateAllTerminationLetters(context, (request?.CreditNrs ?? new List<string>()).ToHashSetShared());
                context.SaveChanges();
                return new InactivateTerminationLettersResult
                {
                    InactivatedOnCreditNrs = result.Keys.ToList()
                };
            }
        }

        public PostponeTerminationLettersResponse PostponeTerminationLetters(PostponeTerminationLettersRequest request) => 
            terminationLettersInactivationManager.PostponeTerminationLetters(request);

        public ResumeTerminationLettersResponse ResumeTerminationLetters(ResumeTerminationLettersRequest request) =>
            terminationLettersInactivationManager.ResumeTerminationLetters(request);
    }

    public class InactivateTerminationLettersRequest
    {
        [Required]
        public List<string> CreditNrs { get; set; }
    }

    public class InactivateTerminationLettersResult
    {
        public List<string> InactivatedOnCreditNrs { get; set; }
    }
}
