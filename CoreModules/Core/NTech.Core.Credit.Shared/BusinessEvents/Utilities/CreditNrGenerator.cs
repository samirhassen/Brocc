using NTech.Core.Credit.Shared.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit
{
    public class CreditNrGenerator
    {
        private Lazy<string> creditNrPrefix;
        public const string CreditNrPrefix = "L";
        private readonly CreditContextFactory contextFactory;

        public CreditNrGenerator(CreditContextFactory contextFactory)
        {
            this.creditNrPrefix = new Lazy<string>(() => CreditNrPrefix);
            this.contextFactory = contextFactory;
        }

        private string FormatCreditNr(int id)
        {
            return string.Concat(creditNrPrefix.Value, id.ToString());
        }

        public string GenerateNewCreditNr()
        {
            using (var context = contextFactory.CreateContext())
            {
                var seq = new CreditKeySequence();
                context.AddCreditKeySequences(seq);
                context.SaveChanges();
                return FormatCreditNr(seq.Id);
            }
        }

        public string[] GenerateNewCreditNrs(int count)
        {
            var seqs = Enumerable.Range(1, count).Select(x => new CreditKeySequence()).ToArray();
            foreach (var g in SplitIntoGroupsOfN(seqs, 1000))
            {
                using (var context = contextFactory.CreateContext())
                {
                    context.AddCreditKeySequences(g.ToArray());
                    context.SaveChanges();
                }
            }
            return seqs.Select(x => x.Id).Select(FormatCreditNr).ToArray();
        }

        private static IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n);
            }
        }
    }
}
