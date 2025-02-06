using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nTest.RandomDataSource
{
    public class TestSavingsAccountApplicationGenerator
    {
        public string CreateApplicationJson(StoredPerson p, bool hasRemarks, IRandomnessSource random, List<Tuple<string, string>> additionalApplicationItems = null, bool generateIban = true)
        {
            var applicationItems = new List<Tuple<string, string>>();
            Action<string, string> a = (n, v) => applicationItems.Add(Tuple.Create(n, v));

            a("customerCivicRegNr", p.CivicRegNr);
            a("customerAddressCity", p.GetProperty("addressCity"));
            a("customerAddressStreet", p.GetProperty("addressStreet"));
            a("customerAddressZipcode", p.GetProperty("addressZipcode"));
            a("customerAddressCountry", p.CivicRegNrTwoLetterCountryIsoCode);
            a("customerFirstName", p.GetProperty("firstName"));
            a("customerLastName", p.GetProperty("lastName"));
            a("customerEmail", p.GetProperty("email"));
            a("customerPhone", p.GetProperty("phone"));
            a("savingsAccountTypeCode", "StandardAccount");
            if (generateIban)
            {
                var bg = new BankAccountGenerator();
                a("withdrawalIban", bg.GenerateIbanFi(random).NormalizedValue);
            }

            if (additionalApplicationItems != null && additionalApplicationItems.Any())
                additionalApplicationItems.ForEach(applicationItems.Add);

            var customerQuestionItems = new List<Tuple<string, string>>();
            Action<string, string> c = (n, v) => customerQuestionItems.Add(Tuple.Create(n, v));

            c("mainoccupation", "agriculture");
            c("taxcountries", "[{\"countryIsoCode\":\"FI\"}]");
            if (hasRemarks)
            {
                c("ispep", "true");
                c("pep_roles", "[\"supremecourtjudge\",\"diplomantordefense\"]");
            }
            else
            {
                c("ispep", "false");
            }

            var productQuestionItems = new List<Tuple<string, string>>();
            Action<string, string> q = (n, v) => productQuestionItems.Add(Tuple.Create(n, v));
            q("purpose", "trading");
            q("savingshorizonestimate", "onetofiveyears");
            q("sourceoffunds", "owncompany");
            q("nrdepositsperyearrangeestimate", "10_50");
            q("initialdepositrangeestimate", "1000_10000");

            return JsonConvert.SerializeObject(new
            {
                allowSavingsAccountNrGeneration = true,
                allowCreateWithoutSignedAgreement = true,
                applicationItems = applicationItems.Select(x => new
                {
                    name = x.Item1,
                    value = x.Item2
                }).ToList(),
                customerQuestionItems = customerQuestionItems.Select(x => new
                {
                    name = x.Item1,
                    value = x.Item2
                }).ToList(),
                productQuestionItems = productQuestionItems.Select(x => new
                {
                    name = x.Item1,
                    value = x.Item2
                }).ToList()
            });
        }
    }
}
