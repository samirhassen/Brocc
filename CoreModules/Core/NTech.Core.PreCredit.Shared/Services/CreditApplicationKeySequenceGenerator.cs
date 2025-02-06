using nPreCredit;
using nPreCredit.Code.Services;
using NTech.Core.PreCredit.Shared.Services;
using System.Linq;

namespace NTech.Core.PreCredit.Services
{
    public class CreditApplicationKeySequenceGenerator : ICreditApplicationKeySequenceGenerator
    {
        private readonly IPreCreditContextFactoryService preCreditContextFactoryService;

        public CreditApplicationKeySequenceGenerator(IPreCreditContextFactoryService preCreditContextFactoryService)
        {
            this.preCreditContextFactoryService = preCreditContextFactoryService;
        }

        public CreditApplicationKeySequence[] CreateNewKeySequences(int count)
        {
            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                var seqs = Enumerable.Range(1, count).Select(x => new CreditApplicationKeySequence()).ToArray();
                context.AddCreditApplicationKeySequences(seqs);
                context.SaveChanges();
                return seqs.ToArray();
            }
        }
    }
}
