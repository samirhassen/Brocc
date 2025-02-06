using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace nCreditReport.Code
{
    public static class TestPersonsFactory
    {
        private static IDictionary<string, List<Tuple<string, string>>> ParseTestPersonsTxtV1(string filename)
        {
            var d = new Dictionary<string, List<Tuple<string, string>>>();
            var lines = File.ReadAllLines(filename).Where(x => !string.IsNullOrWhiteSpace(x) && !x.Trim().StartsWith("#")).Select(x => x.Trim());
            List<Tuple<string, string>> currentList = null;
            string currentCivicRegNr = null;
            foreach (var line in lines)
            {
                if (line.EndsWith(":"))
                {
                    if (currentList != null)
                        d.Add(currentCivicRegNr, currentList);
                    currentCivicRegNr = line.Substring(0, line.Length - 1);
                    currentList = new List<Tuple<string, string>>();
                }
                else
                {
                    var p = line.Split('=');
                    if (p.Length != 2)
                        throw new Exception("Invalid test person item. Format should be '<name>=<value>'");
                    currentList.Add(Tuple.Create(p[0], p[1]));
                }
            }
            if (currentList != null)
                d.Add(currentCivicRegNr, currentList);
            return d;
        }

        /*json v1 structure
        [            
            {
                civicRegNr,
                birthDate,
                civicRegNrCountry,
                fullName,
                firstName,
                lastName,
                middleName,
                firstNameAndMiddleName,
                middleNameAndLastName,
                addressStreet,
                addressCity,
                addressZipCode,
                addressCountry,
                email,
                phone,
                creditreport : 
                {
                    hasDomesticAddress,
                    hasPaymentRemark,
                    domesticAddressSinceDate,
                    bricRiskOfPaymentRemark,
                    nrOfPaymentRemarks,
                    personStatus
                }
            }
        ]
        */
        private static IDictionary<string, List<Tuple<string, string>>> ParseTestPersonsJsonV1(string filename)
        {
            var d = new Dictionary<string, List<Tuple<string, string>>>();
            var personInfoItems = new List<string> { "firstName", "lastName", "addressStreet", "addressZipCode", "addressCity", "addressCountry" };
            foreach (var p in JsonConvert.DeserializeObject<List<JObject>>(File.ReadAllText(filename)))
            {
                var civicRegNr = p.SelectToken("$.civicRegNr")?.Value<string>();
                if (civicRegNr == null)
                    continue;

                var dd = new List<Tuple<string, string>>();
                foreach (var personInfoItemName in personInfoItems)
                {
                    var itemValue = p.SelectToken($"$.{personInfoItemName}")?.Value<string>();
                    if (!string.IsNullOrWhiteSpace(itemValue))
                        dd.Add(Tuple.Create(personInfoItemName == "addressZipCode" ? "addressZipcode" : personInfoItemName, itemValue));
                }

                var creditreportParent = p.SelectToken("$.creditreport")?.Value<JObject>();
                if (creditreportParent != null)
                {
                    foreach (var pc in creditreportParent.Properties())
                    {
                        var itemName = pc.Name;
                        var itemValue = creditreportParent.SelectToken($"$.{itemName}")?.Value<string>();
                        if (!string.IsNullOrWhiteSpace(itemValue))
                            dd.Add(Tuple.Create(itemName, itemValue));
                    }
                }

                d.Add(civicRegNr, dd);
            }
            return d;
        }

        public static IDictionary<string, List<Tuple<string, string>>> Create()
        {
            var d = new Dictionary<string, List<Tuple<string, string>>>();
            var f = NEnv.CreditReportTestPersonsFile;
            if (f == null)
                return d;
            if (!File.Exists(f))
                return d;

            var format = (NEnv.CreditReportTestPersonsFileFormat).ToLowerInvariant();
            if (format == "txt.v1")
            {
                return ParseTestPersonsTxtV1(f);
            }
            else if (format == "json.v1")
            {
                return ParseTestPersonsJsonV1(f);
            }
            else
                throw new NotImplementedException();
        }
    }
}