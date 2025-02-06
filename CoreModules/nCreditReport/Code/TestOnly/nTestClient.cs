using NTech.Banking.CivicRegNumbers;
using NTech.Banking.OrganisationNumbers;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCreditReport.Code
{
    public class nTestClient
    {
        public Dictionary<string, string> GetTestCompany(IOrganisationNumber orgnr, bool generateIfNotExists = false)
        {
            var response = NHttp
                .Begin(new Uri(NEnv.ServiceRegistry.Internal["nTest"]), NHttp.GetCurrentAccessToken())
                .PostJson("Api/Company/TestCompany/Get", new
                {
                    orgnr = orgnr.NormalizedValue,
                    orgnrCountry = orgnr.Country,
                    generateIfNotExists = generateIfNotExists
                });
            if (response.StatusCode == 404)
                return null;
            else
                return response.ParseJsonAs<Dictionary<string, string>>();
        }

        public Dictionary<string, string> GetTestPerson(string allWithThisPrefix, ICivicRegNumber civicRegNr, params string[] requestedProperties) =>
            GetTestPerson(allWithThisPrefix, civicRegNr, false, requestedProperties);

        public Dictionary<string, string> GetTestPerson(string allWithThisPrefix, ICivicRegNumber civicRegNr, bool generateIfNotExists, params string[] requestedProperties)
        {
            var response = NHttp
                .Begin(new Uri(NEnv.ServiceRegistry.Internal["nTest"]), NHttp.GetCurrentAccessToken())
                .PostJson("Api/TestPerson/Get", new
                {
                    civicRegNr = civicRegNr.NormalizedValue,
                    civicRegNrCountry = civicRegNr.Country,
                    requestedProperties = requestedProperties,
                    allWithThisPrefix = allWithThisPrefix,
                    generateIfNotExists = generateIfNotExists
                });
            if (response.StatusCode == 404)
                return null;
            else
                return response.ParseJsonAs<Dictionary<string, string>>();
        }

        public Dictionary<string, GetOrGenerateTestPersonResponseModel> GetOrGenerateTestPersons(List<GetOrGenerateTestPersonModel> persons, bool useCommonAddress, int? seed = null)
        {
            return NHttp
                .Begin(new Uri(NEnv.ServiceRegistry.Internal["nTest"]), NHttp.GetCurrentAccessToken())
                .PostJson("Api/TestPerson/GetOrGenerate", new
                {
                    persons = persons,
                    seed = seed,
                    useCommonAddress = useCommonAddress
                })
                .ParseJsonAsAnonymousType(new { Persons = (List<GetOrGenerateTestPersonResponseModel>)null })
                .Persons
                .ToDictionary(x => x.CivicRegNr, x => x);
        }

        public class GetOrGenerateTestPersonModel
        {
            public string CivicRegNr { get; set; }
            public bool? IsAccepted { get; set; }
            public IDictionary<string, string> Customizations { get; set; }
        }

        public class GetOrGenerateTestPersonResponseModel
        {
            public string CivicRegNr { get; set; }
            public IDictionary<string, string> Properties { get; set; }
            public bool WasGenerated { get; set; }
        }
    }
}