using nPreCredit;
using System;
using System.Linq;

namespace NTech.Core.PreCredit.Shared.Services
{
    public class CreditApplicationNrGenerator
    {
        private Lazy<string> applicationNrPrefix;
        private readonly ICreditApplicationKeySequenceGenerator keySequenceGenerator;

        public CreditApplicationNrGenerator(Func<string> getApplicationNrPrefix, ICreditApplicationKeySequenceGenerator keySequenceGenerator)
        {
            this.applicationNrPrefix = new Lazy<string>(getApplicationNrPrefix);
            this.keySequenceGenerator = keySequenceGenerator;
        }

        private string FormatApplicationNr(int id)
        {
            return string.Concat(applicationNrPrefix.Value, id.ToString());
        }

        public string GenerateNewApplicationNr()
        {
            var seq = keySequenceGenerator.CreateNewKeySequences(1)[0];
            return FormatApplicationNr(seq.Id);
        }

        public string[] GenerateNewApplicationNrs(int count)
        {
            var seqs = keySequenceGenerator.CreateNewKeySequences(count);
            return seqs.Select(x => x.Id).Select(FormatApplicationNr).ToArray();
        }
    }

    public interface ICreditApplicationKeySequenceGenerator
    {
        CreditApplicationKeySequence[] CreateNewKeySequences(int count);
    }
}
