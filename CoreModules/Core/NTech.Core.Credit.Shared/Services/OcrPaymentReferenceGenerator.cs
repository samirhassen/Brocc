using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;

namespace nCredit.Code
{
    public class OcrPaymentReferenceGenerator : IOcrPaymentReferenceGenerator
    {
        private readonly string forCountryIsoCode;
        private readonly Func<ICreditContextExtended> createCreditContext;

        public OcrPaymentReferenceGenerator(IClientConfigurationCore clientConfiguration, CreditContextFactory creditContextFactory) : this(clientConfiguration.Country.BaseCountry, creditContextFactory.CreateContext)
        {

        }

        public OcrPaymentReferenceGenerator(string forCountryIsoCode, Func<ICreditContextExtended> createCreditContext)
        {
            this.forCountryIsoCode = forCountryIsoCode;
            this.createCreditContext = createCreditContext;
        }

        public IOcrNumber GenerateNew()
        {
            using (var context = createCreditContext())
            {
                var seq = new OcrPaymentReferenceNrSequence();
                context.AddOcrPaymentReferenceNrSequence(seq);
                context.SaveChanges();

                return GenerateFromSequenceNumber(this.forCountryIsoCode, seq.Id);
            }
        }

        public static IOcrNumber GenerateFromSequenceNumber(string forCountryIsoCode, int nr)
        {
            if (forCountryIsoCode == "FI")
                return OcrNumberFi.FromSequenceNumber(nr, 8);
            else if (forCountryIsoCode == "SE")
                return OcrNumberSe.FromSequenceNumber(nr);
            else
                throw new NotImplementedException();
        }
    }
}