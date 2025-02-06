using System;
using System.Collections.Generic;
using System.Text;

namespace NTech.Banking.Shared.Globalization
{
    public class NTechCountryHelper
    {
        private HashSet<string> EuOrEesCountryCodes { get; set; }
        private HashSet<string> EesOnlyCountryCodes { get; set; }

        /// <summary>
        /// Load data externally to do checks, intention to get data from settings. 
        /// </summary>
        /// <param name="euOrEesCountryCodes"></param>
        /// <param name="eesOnlyCountryCodes"></param>
        public NTechCountryHelper(HashSet<string> euOrEesCountryCodes, HashSet<string> eesOnlyCountryCodes)
        {
            EuOrEesCountryCodes = euOrEesCountryCodes;
            EesOnlyCountryCodes = eesOnlyCountryCodes;
        }

        /// <summary>
        /// Data straight from settings load. 
        /// </summary>
        /// <param name="settingValues"></param>
        public NTechCountryHelper(Dictionary<string, string> settingValues)
        {
            EuOrEesCountryCodes = settingValues["euAndEesCountryCodes"].Split(',').ToHashSetShared();
            EesOnlyCountryCodes = settingValues["eesOnlyCountryCodes"].Split(',').ToHashSetShared();
        }

        public bool IsEuMemberState(NTechCountry country)
        {
            return country != null && EuOrEesCountryCodes.Contains(country.TwoLetterIsoCountryCode) 
                && !EesOnlyCountryCodes.Contains(country.TwoLetterIsoCountryCode);
        }

        public bool IsEuEesMemberState(NTechCountry country)
        {
            return country != null && EuOrEesCountryCodes.Contains(country.TwoLetterIsoCountryCode);
        }

    }
}
