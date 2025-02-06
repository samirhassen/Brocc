using NTech;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services.SharedStandard
{
    public class LtlDataTables : ILtlDataTables
    {
        public decimal? IncomeTaxMultiplier => Numbers.ParseDecimalOrNull(NTechEnvironment.Instance.Setting("ntech.precredit.ltl.incomeTaxMultiplier", false));
        public int? DefaultChildAgeInYears => Numbers.ParseInt32OrNull(NTechEnvironment.Instance.Setting("ntech.precredit.ltl.defaultChildAgeInYears", false));

        private const string StressInterestRatePercentCacheKey = "b97a6cf7-4d5e-4827-a375-21542d0408c3";
        public decimal StressInterestRatePercent =>
            NTechCache.WithCacheS(StressInterestRatePercentCacheKey, TimeSpan.FromMinutes(15), GetStressInterestRatePercentNonCached);

        public decimal GetStressInterestRatePercentNonCached()
        {
            var appSettingValue = NTechEnvironment.Instance.Setting("ntech.precredit.ltl.stressInterestRatePercent", false);

            if (appSettingValue != null)
                return Numbers.ParseDecimalOrNull(appSettingValue).Value;

            var client = new PreCreditCustomerClient();
            var settings = client.LoadSettings("ltlStressInterest");
            var settingValue = settings["stressInterestRatePercent"];
            return Numbers.ParseDecimalOrNull(settingValue).Value;
        }

        public int? DefaultApplicantAgeInYears => Numbers.ParseInt32OrNull(NTechEnvironment.Instance.Setting("ntech.precredit.ltl.defaultApplicantAgeInYears", false));
        public bool CreditsUse360DayInterestYear => NEnv.CreditsUse360DayInterestYear;

        private static decimal ParseResourceFileDecimalAmount(string value)
        {
            return Numbers.ParseDecimalOrNull(value.Replace(" ", "").Replace(",", ".")).Value;
        }

        private const string IndividualCostsTableCacheKey = "9228d805-36f6-4374-8d2b-405a9b75fd40";
        public Dictionary<int, (decimal FoodCost, decimal OtherCost, decimal IndividualCost)> GetIndividualCostsTable() =>
            NTechCache.WithCache(IndividualCostsTableCacheKey, TimeSpan.FromMinutes(15), GetIndividualCostsTableNonCached);

        public Dictionary<int, (decimal FoodCost, decimal OtherCost, decimal IndividualCost)> GetIndividualCostsTableNonCached()
        {
            /*
                Structure: separtor must be tabs to avoid having to handle the spaces as 1k separators

            Ålder	Matkostnader	Övrigt	Individkostnad
            0	 1 100,00 	1430	 2 530,00   

                */
            var result = new Dictionary<int, (decimal FoodCost, decimal OtherCost, decimal IndividualCost)>();
            foreach (var lineIter in ReadTextFileFromClientResources("LeftToLiveOnResources/IndividualCosts.txt", "individualCosts"))
            {
                var line = lineIter.Trim();
                if (!line.Any(Char.IsLetter))
                {
                    var parts = line.Split('\t');
                    if (parts.Length != 4)
                        throw new Exception("Did you forget to use tabs to split the items?");
                    result[int.Parse(parts[0])] = (
                        FoodCost: ParseResourceFileDecimalAmount(parts[1]),
                        OtherCost: ParseResourceFileDecimalAmount(parts[2]),
                        IndividualCost: ParseResourceFileDecimalAmount(parts[3]));
                }
            }
            return result;
        }

        private const string HouseholdSizeCostsTableCacheKey = "bfc793b2-ce75-46e9-b119-3e460088f8a1";
        public Dictionary<int, decimal> GetHouseholdSizeCostsTable() =>
            NTechCache.WithCache(HouseholdSizeCostsTableCacheKey, TimeSpan.FromMinutes(15), GetHouseholdSizeCostsTableNonCached);

        public Dictionary<int, decimal> GetHouseholdSizeCostsTableNonCached()
        {
            /*
                Structure: separtor must be tabs to avoid having to handle the spaces as 1k separators

            Ålder	Kostnad
            1	 1 100,00

                */
            var result = new Dictionary<int, decimal>();

            foreach (var lineIter in ReadTextFileFromClientResources("LeftToLiveOnResources/HouseholdSizeCosts.txt", "householdSizeCosts"))
            {
                var line = lineIter.Trim();
                if (!line.Any(Char.IsLetter))
                {
                    var parts = line.Split('\t');
                    if (parts.Length != 2)
                        throw new Exception("Did you forget to use tabs to split the items?");
                    result[int.Parse(parts[0])] = ParseResourceFileDecimalAmount(parts[1]);
                }
            }
            return result;
        }

        private static string[] ReadTextFileFromClientResources(string relativePath, string overrideSettingSuffix)
        {
            var filePath = System.IO.Path.Combine(NTechEnvironment.Instance.SharedResourceDirectory.FullName, relativePath);
            var filen = NTechEnvironment.Instance.ClientResourceFile($"ntech.standard.ltl.{overrideSettingSuffix}", filePath, true);
            return System.IO.File.ReadAllLines(filen.FullName, System.Text.Encoding.UTF8);
        }

        private (decimal? IndividualCostForAge, decimal IndividualCostMax) GetIndividualCostForAgeAndMaxCostForAllAges(int ageInYears)
        {
            var table = GetIndividualCostsTable();
            var maxCost = table.Values.Max(x => x.IndividualCost);

            return (IndividualCostForAge: (table.TryGetValue(ageInYears, out var row) ? row.IndividualCost : new decimal?()), IndividualCostMax: maxCost);
        }

        public decimal GetIndividualAgeCost(int ageInYears)
        {
            var (ageBasedCost, maxCost) = GetIndividualCostForAgeAndMaxCostForAllAges(ageInYears);
            return ageBasedCost ?? maxCost;
        }

        public decimal GetHouseholdMemberCountCost(int memberCount)
        {
            if (memberCount <= 0)
                return 0m;

            var householdSizeCosts = GetHouseholdSizeCostsTable();

            //Table is assumed to be dense from 1 up to some nr x and for everything above x we just take the cost for x.
            if (householdSizeCosts.ContainsKey(memberCount))
                return householdSizeCosts[memberCount];
            else
                return householdSizeCosts[householdSizeCosts.Keys.Max()];
        }

        public static void OnSettingChanged(string settingCode)
        {
            if (settingCode == "ltlStressInterest")
                NTechCache.Remove(StressInterestRatePercentCacheKey);
        }
    }
}