using NTech.Banking.CivicRegNumbers;
using NTech.Services.Infrastructure.NTechWs;
using System;

namespace nCustomer.WebserviceMethods
{
    public class ParseCivicRegNrMethod : TypedWebserviceMethod<ParseCivicRegNrMethod.Request, ParseCivicRegNrMethod.Response>
    {
        public override string Path => "ParseCivicRegNr";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ICivicRegNumber c;
            if (string.IsNullOrEmpty(request?.CivicRegNr) || !NEnv.BaseCivicRegNumberParser.TryParse(request?.CivicRegNr, out c))
                return Error("Missing or invalid CivicRegNr");
            else
                return new Response
                {
                    NormalizedValue = c.NormalizedValue,
                    CrossCountryStorageValue = c.CrossCountryStorageValue,
                    Country = c.Country,
                    BirthDate = c.BirthDate,
                    IsMale = c.IsMale
                };
        }

        public class Request
        {
            public string CivicRegNr { get; set; }
        }

        public class Response
        {
            public string NormalizedValue { get; set; }
            public string CrossCountryStorageValue { get; set; }
            public string Country { get; set; }
            public DateTime? BirthDate { get; set; }
            public bool? IsMale { get; set; }
        }
    }
}