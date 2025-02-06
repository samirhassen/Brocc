using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Services.Infrastructure.Aml
{
    public class AmlReportingService
    {
        private readonly Func<string, bool> isEuEesMemberState;
        private readonly string clientCountry;

        public AmlReportingService(Func<string, bool> isEuEesMemberState, string clientCountry)
        {
            //TODO: Is it worth taking a dependancy on NTech.Banking.Shared to avoid havin to inject this function?
            this.isEuEesMemberState = isEuEesMemberState;
            this.clientCountry = clientCountry;
        }

        public FiReportingCustomerModel CreateCustomerModelForFiReporting(int customerId, Dictionary<string, string> customerProperties)
        {
            var taxCountries = ParseCountriesArray("taxcountries", customerProperties);
            var citizenCountries = ParseCountriesArray("citizencountries", customerProperties);
            var addressCountry = (GetProperty("addressCountry", customerProperties) ?? clientCountry)?.ToUpperInvariant();

            var m = new FiReportingCustomerModel
            {
                CustomerId = customerId,
                CivicRegNr = GetProperty("civicRegNr", customerProperties),
                AddressCountry = addressCountry,
                TaxCountries = taxCountries.ToHashSet(),
                CitizenCountries = citizenCountries.ToHashSet()
            };

            m.HasTaxCountryFI = taxCountries.Contains(clientCountry);
            m.HasNonFICitizenship = citizenCountries.Any(x => x != clientCountry);
            m.HasNonFIInsideEuEesTaxCountry = taxCountries.Any(x => x != clientCountry && isEuEesMemberState(x));
            m.HasNonFIOutsideEuEesTaxCountry = taxCountries.Any(x => x != clientCountry && !isEuEesMemberState(x));
            m.HasNonFIAddressCountry = addressCountry != clientCountry;

            return m;
        }

        /// <summary>
        /// Parse taxcountries or citizencountries
        /// </summary>
        /// <param name="propertyName"></param>
        public string[] ParseCountriesArray(string propertyName, Dictionary<string, string> customerProperties)
        {
            var propertyValue = GetProperty(propertyName, customerProperties);
            if (propertyValue == null)
                return new string[] { clientCountry };
            string[] parsedValue;
            if (propertyValue.Contains("countryIsoCode"))
                parsedValue = JsonConvert.DeserializeObject<CountryNestedModel[]>(propertyValue)?.Select(x => x?.countryIsoCode)?.ToArray();
            else
                parsedValue = JsonConvert.DeserializeObject<string[]>(propertyValue);

            parsedValue = parsedValue
                .Where(x => x.NormalizeNullOrWhitespace() != null)
                .Select(x => x.NormalizeNullOrWhitespace().ToUpperInvariant())
                .Distinct()
                .ToArray();
            return parsedValue.Length == 0 ? new string[] { clientCountry } : parsedValue;
        }

        public List<(string Question, int Count)> ComputeFiQuestionAnswers(List<FiReportingCustomerModel> customers)
        {
            var summaries = new List<(string Question, int Count)>();

            summaries.Add(("How many customers who have tax country out of Finland?", customers.Count(x => x.HasNonFIInsideEuEesTaxCountry || x.HasNonFIOutsideEuEesTaxCountry)));
            summaries.Add(("How many of these are within EU/EES?", customers.Count(x => x.HasNonFIInsideEuEesTaxCountry)));
            summaries.Add(("How many of these are outside EU/EES?", customers.Count(x => x.HasNonFIOutsideEuEesTaxCountry)));
            summaries.Add(("How many customers have non Finnish citizenship?", customers.Count(x => x.HasNonFICitizenship)));
            summaries.Add(("How many customers have non Finnish address?", customers.Count(x => x.HasNonFIAddressCountry)));

            return summaries;
        }

        private class CountryNestedModel
        {
            public string countryIsoCode { get; set; }
        }

        public class FiReportingCustomerModel
        {
            public int CustomerId { get; set; }
            public string CivicRegNr { get; set; }
            public bool HasTaxCountryFI { get; set; }
            public bool HasNonFIInsideEuEesTaxCountry { get; set; }
            public bool HasNonFIOutsideEuEesTaxCountry { get; set; }
            public bool HasNonFICitizenship { get; set; }
            public bool HasNonFIAddressCountry { get; set; }
            public string AddressCountry { get; set; }
            public ISet<string> TaxCountries { get; set; }
            public ISet<string> CitizenCountries { get; set; }
        }

        public string GetProperty(string propertyName, Dictionary<string, string> customerProperties)
        {
            if (customerProperties == null || !customerProperties.ContainsKey(propertyName))
                return null;
            return customerProperties[propertyName].NormalizeNullOrWhitespace();
        }
    }
}
