using NTech.Core.Savings.Shared.Database;
using System;
using System.Linq;

namespace nSavings.Code
{
    public class SavingsAccountNrGenerator
    {
        private readonly SavingsContextFactory contextFactory;
        private Lazy<string> savingsAccountNrPrefix;

        public SavingsAccountNrGenerator(SavingsContextFactory contextFactory)
        {
            this.contextFactory = contextFactory;
            this.savingsAccountNrPrefix = new Lazy<string>(() => "S");
        }

        private string FormatSavingsAccountNr(long id)
        {
            return string.Concat(savingsAccountNrPrefix.Value, id.ToString());
        }

        public string GenerateNewSavingsAccountNr()
        {
            using (var context = contextFactory.CreateContext())
            {
                var seq = new SavingsAccountKeySequence();
                context.AddSavingsAccountKeySequences(seq);
                context.SaveChanges();
                return FormatSavingsAccountNr(seq.Id);
            }
        }

        public string[] GenerateNewSavingsAccountNrs(int count)
        {
            var seqs = Enumerable.Range(1, count).Select(x => new SavingsAccountKeySequence()).ToArray();
            foreach (var g in seqs.SplitIntoGroupsOfN(1000))
            {
                using (var context = contextFactory.CreateContext())
                {
                    context.AddSavingsAccountKeySequences(g.ToArray());
                    context.SaveChanges();
                }
            }
            return seqs.Select(x => x.Id).Select(FormatSavingsAccountNr).ToArray();
        }
    }
}