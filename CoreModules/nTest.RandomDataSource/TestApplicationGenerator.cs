using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nTest.RandomDataSource
{
    public class TestApplicationGenerator
    {
        public string CreateApplicationJson(StoredPerson p, StoredPerson p2, bool isAccepted, IRandomnessSource random, string providerName, bool includeAdditionalQuestionFields, string externalApplicationId = null)
        {
            var amount = random.NextIntBetween(1, 12) * 1000m;
            var repaymentTimeInYears = random.NextIntBetween(1, 8);
            var pattern = LoadEmbeddedApplicationPatternFile(isAccepted, p2 != null);

            const string AdditionalItemPattern = @",
    {
      ""Group"": ""{{groupName}}"",
      ""Name"": ""{{itemName}}"",
      ""Value"": ""{{itemValue}}""
    }";
            var additionalItems = new List<string>();
            Action<string, string, string> addItem = (g, n, v) => additionalItems.Add(
                AdditionalItemPattern.Replace("{{groupName}}", g).Replace("{{itemName}}", n).Replace("{{itemValue}}", v));
            Func<string, string> escape = s => s?.Replace(@"""", @"\""");

            pattern = pattern
                    .Replace("{{civicRegNr}}", p.CivicRegNr)
                    .Replace("{{civicRegNrCountry}}", p.CivicRegNrTwoLetterCountryIsoCode)
                    .Replace("{{phone}}", p.GetProperty("phone"))
                    .Replace("{{email}}", p.GetProperty("email"))
                    .Replace("{{amount}}", amount.ToString(CultureInfo.InvariantCulture))//TODO: Random with setting
                    .Replace("{{runtimeInYears}}", repaymentTimeInYears.ToString()) //TODO: Random with setting
                    .Replace("{{providerName}}", providerName);

            if (includeAdditionalQuestionFields)
            {
                if (p.CivicRegNrTwoLetterCountryIsoCode == "FI")
                    addItem("application", "iban", p.Properties["iban"]);
                else if (p.CivicRegNrTwoLetterCountryIsoCode == "SE")
                    addItem("application", "bankAccountNr", p.Properties["bankAccountNr"]);
                else
                    throw new NotImplementedException();

                addItem("question1", "loan_purpose", "consumption");
                addItem("question1", "loan_whosmoney", "own");
                addItem("question1", "loan_paymentfrequency", "onschedule");
                addItem("question1", "customer_ispep", "false");
                addItem("question1", "customer_taxcountries", escape("[{\"countryIsoCode\":\"FI\"}]"));
            }
            if (p2 != null)
            {
                pattern = pattern
                    .Replace("{{civicRegNr2}}", p2.CivicRegNr)
                    .Replace("{{civicRegNrCountry2}}", p2.CivicRegNrTwoLetterCountryIsoCode)
                    .Replace("{{phone2}}", p2.GetProperty("phone"))
                    .Replace("{{email2}}", p2.GetProperty("email"));

                addItem("question2", "loan_purpose", "consumption");
                addItem("question2", "loan_whosmoney", "own");
                addItem("question2", "loan_paymentfrequency", "onschedule");
                addItem("question2", "customer_ispep", "false");
                addItem("question2", "customer_taxcountries", escape("[{\"countryIsoCode\":\"FI\"}]"));
            }

            if (externalApplicationId != null)
            {
                addItem("application", "providerApplicationId", externalApplicationId);
            }

            pattern = pattern.Replace("[[[EXTENSION_POINT_ITEMS]]]", additionalItems.Any() ? string.Join("", additionalItems) : "");

            return pattern;
        }

        private static string LoadEmbeddedApplicationPatternFile(bool isAccepted, bool hasCoApplicant)
        {
            var n = isAccepted ? "ApprovedApplicationPattern" : "RejectedApplicationPattern";
            var appSuffix = hasCoApplicant ? "2" : "";
            var resourceName = $"{n}{appSuffix}.json";
            return EmbeddedResources.LoadFileAsString(resourceName);
        }
    }
}
