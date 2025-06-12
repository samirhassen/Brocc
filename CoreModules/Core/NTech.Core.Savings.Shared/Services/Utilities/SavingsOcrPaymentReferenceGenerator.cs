using System;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.DbModel;

namespace NTech.Core.Savings.Shared.Services.Utilities
{
    public class SavingsOcrPaymentReferenceGenerator
    {
        private readonly SavingsContextFactory contextFactory;
        private readonly string forCountryIsoCode;
        private readonly long sequenceNrShift;

        public SavingsOcrPaymentReferenceGenerator(SavingsContextFactory contextFactory, string forCountryIsoCode, long sequenceNrShift = 0)
        {
            this.contextFactory = contextFactory;
            this.forCountryIsoCode = forCountryIsoCode;
            this.sequenceNrShift = sequenceNrShift;
        }

        public IOcrNumber GenerateNew()
        {
            using (var context = contextFactory.CreateContext())
            {
                var seq = new OcrPaymentReferenceNrSequence();
                context.AddOcrPaymentReferenceNrSequences(seq);
                context.SaveChanges();

                return GenerateFromSequenceNumber(seq.Id);
            }
        }

        public IOcrNumber GenerateFromSequenceNumber(long nr)
        {
            if (forCountryIsoCode == "FI")
                return OcrNumberFi.FromSequenceNumber(nr + sequenceNrShift, 8);
            else if (forCountryIsoCode == "SE")
                return OcrNumberSe.FromSequenceNumber(nr + sequenceNrShift);
            else
                throw new NotImplementedException();
        }
    }
}