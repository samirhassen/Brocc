using System;
using System.Linq;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace NTech.Core.Savings.Shared.Services.Utilities
{
    public class SavingsAccountNrGenerator
    {
        private readonly SavingsContextFactory _contextFactory;
        private readonly Lazy<string> _savingsAccountNrPrefix;

        public SavingsAccountNrGenerator(SavingsContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _savingsAccountNrPrefix = new Lazy<string>(() => "S");
        }

        private string FormatSavingsAccountNr(long id)
        {
            return string.Concat(_savingsAccountNrPrefix.Value, id.ToString());
        }

        public string GenerateNewSavingsAccountNr()
        {
            using (var context = _contextFactory.CreateContext())
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
                using (var context = _contextFactory.CreateContext())
                {
                    context.AddSavingsAccountKeySequences(g.ToArray());
                    context.SaveChanges();
                }
            }

            return seqs.Select(x => x.Id).Select(FormatSavingsAccountNr).ToArray();
        }
    }
}