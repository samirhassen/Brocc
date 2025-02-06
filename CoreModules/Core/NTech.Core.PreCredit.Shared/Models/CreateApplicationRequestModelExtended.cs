using NTech.Banking.CivicRegNumbers;
using NTech.Banking.PluginApis.CreateApplication;
using NTech.Core.Module.Shared.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods.SharedStandard
{
    //TODO: Merge this back into NTech.Banking when there stops being changes due to standard
    public class CreateApplicationRequestModelExtended : CreateApplicationRequestModel
    {
        public Dictionary<string, HashSet<int>> CustomerListMembers { get; } = new Dictionary<string, HashSet<int>>();

        public void SetCustomerListMember(string listName, int customerId)
        {
            if (!CustomerListMembers.ContainsKey(listName))
                CustomerListMembers.Add(listName, new HashSet<int>());

            CustomerListMembers[listName].Add(customerId);
        }

        public void SetUniqueComplexApplicationItem(string listName, int nr, string itemName, string ItemValue)
        {
            var list = ComplexApplicationItems.FirstOrDefault(x => x.ListName == listName && x.Nr == nr);
            if (list != null)
            {
                if (list.UniqueValues == null)
                    list.UniqueValues = new Dictionary<string, string>();
                list.UniqueValues[itemName] = ItemValue;
            }
            else
                AddComplexApplicationItem(listName, nr, new Dictionary<string, string> { { itemName, ItemValue } }, null);
        }

        /// <summary>
        /// Does not check validity or existance, only checks for dupes
        /// </summary>
        public static void CheckForDuplicateCivicRegNrs(IEnumerable<string> civicRegNrs, IClientConfigurationCore clientConfiguration)
        {
            var validNrs = new HashSet<string>();
            foreach (var civicRegNr in civicRegNrs ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(civicRegNr) && new CivicRegNumberParser(clientConfiguration.Country.BaseCountry).TryParse(civicRegNr, out var parsedNr))
                {
                    if (validNrs.Contains(parsedNr.NormalizedValue))
                        throw new NTechCoreWebserviceException($"Duplicate CivicRegNr")
                        {
                            IsUserFacing = true,
                            ErrorCode = $"duplicateApplicantCivicRegNr",
                            ErrorHttpStatusCode = 400
                        };
                    else
                        validNrs.Add(parsedNr.NormalizedValue);
                }
            }
        }
    }
}