using NTech;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace nCreditReport.Code.SatFi
{
    /// <summary>
    /// Integrated using integration document "CONSUMER INFORMATION 2018 V1.2"
    /// </summary>
    public class SatFiCreditReportParser
    {
        private readonly string QType41CreditInformationServices = "41";
        private readonly string QType37PopulationInformationExtensiveLevel2Plus = "37";

        private XDocument xmlDocument { get; set; }
        private string currentQType { get; set; }
        private IEnumerable<CreditReportField> CreditReportSettings { get; set; }

        public void Initiate(string xml, string qType, IEnumerable<CreditReportField> creditReportSettings = null)
        {
            xmlDocument = XDocuments.Parse(xml);
            currentQType = qType;
            CreditReportSettings = creditReportSettings;
        }

        /// <summary>
        /// Returns list of values we save encrypted to our database.
        /// These will be the same for all clients using SatFiCreditReport. 
        /// </summary>
        /// <returns></returns>
        public List<(string name, string value)> ParseInternalValues()
        {
            var values = new List<(string name, string value)>();

            void AddValueIfNotNull(string name, string value)
            {
                if (value != null)
                {
                    values.Add((name, value));
                }
            }

            // Parsing for qType 41
            if (currentQType == QType41CreditInformationServices)
            {
                AddValueIfNotNull("hasPaymentRemark", HasPaymentDefaultEntries().Equals("Yes") ? "true" : "false");
                AddValueIfNotNull("nrOfPaymentRemarks", NumberOfPaymentDefaultEntries());
                AddValueIfNotNull("hasBusinessConnection", ParticipatesInCompanies());
            }

            if (currentQType == QType37PopulationInformationExtensiveLevel2Plus)
            {
                AddValueIfNotNull("personStatus", InternalPersonStatus());
                AddValueIfNotNull("firstName", GetName("firstName"));
                AddValueIfNotNull("lastName", GetName("lastName"));
                AddValueIfNotNull("immigrationDate", ImmigrationDateOrNull());

                var permanentAddressFinland = GetLatestAddressRow(PermanentCode);
                // Only save these if the person has a permanent address. 
                if (permanentAddressFinland != null)
                {
                    AddValueIfNotNull("addressStreet", permanentAddressFinland.Street);
                    AddValueIfNotNull("addressZipcode", permanentAddressFinland.Zip);
                    AddValueIfNotNull("addressCity", permanentAddressFinland.Town);
                    AddValueIfNotNull("addressCountry", permanentAddressFinland.TwoLetterCountry);
                    AddValueIfNotNull("domesticAddressSinceDate", GetDomesticAddressSinceDate());
                }

                AddValueIfNotNull("hasDomesticAddress", permanentAddressFinland != null ? "true" : "false");
                AddValueIfNotNull("hasPostBoxAddress", GetLatestAddressRow(PostalCode) != null ? "true" : "false");
                AddValueIfNotNull("hasPosteRestanteAddress", ActivePosteRestanteAddress() != null ? "true" : "false");
            }

            return values;
        }

        /// <summary>
        /// Return list of values with the intention of showing in a GUI. 
        /// </summary>
        /// <returns></returns>
        public List<DictionaryEntry> ParseTabledValues()
        {
            var q37AvailableFields = new Dictionary<string, Func<string>>
            {
                { "populationRegisterStatus",     PopulationRegisterStatus},
                { "personIdentityNumber",         GetPersonId},
                { "firstName",                    () => GetName("firstName")},
                { "lastName",                     () => GetName("lastName")},
                { "dateOfDeath",                  DateOfDeath},
                { "placeOfResidence",             PlaceOfResidence},
                { "nativeLanguage",               NativeLanguage},
                { "citizenship",                  PersonsCitizenship},
                { "citizenshipStartdate",         PersonsCitizenshipStartDate},
                { "maritalStatus",                MaritalStatus},
                { "immigrationDate",              () => ImmigrationDateOrNull() ?? ""},
                { "numberOfDependants",           NumberOfDependants},
                { "numberOfInhabitants",          NumberOfHabitants},
                { "permanentAddressInFinland",    () => GetLatestAddressRow(PermanentCode)?.ToString() ?? ""},
                { "postalAddressInFinland",       () => GetLatestAddressRow(PostalCode)?.ToString() ?? ""},
                { "supervisionStartDate",         SupervisionStartDate},
                { "restrictionOfCompetenceToAct", RestrictionOfCompetenceToAct},
                { "domesticAddressSinceDate",     GetDomesticAddressSinceDate},
                { "hasPostBoxAddress",            () => GetPostBoxAddress() != null ? "Yes" : "No"},
                { "hasPosteRestanteAddress",      () => ActivePosteRestanteAddress() != null ? "Yes" : "No"},
                { "addressStreet",                () => GetLatestAddressRow(PermanentCode)?.Street},
                { "addressZipcode",               () => GetLatestAddressRow(PermanentCode)?.Zip},
                { "addressCity",                  () => GetLatestAddressRow(PermanentCode)?.Town},
                { "addressCountry",               () => GetLatestAddressRow(PermanentCode)?.TwoLetterCountry},
                { "hasDomesticAddress",           () => GetLatestAddressRow(PermanentCode) != null ? "true" : "false"},
                { "personStatus",                 InternalPersonStatus},
            };

            var q41AvailableFields = new Dictionary<string, Func<string>>
            {
                { "existsInRegister",                   ExistsInRegister},
                { "addressWhenDefaulted",               GetRegisteredAddressForPaymentDefault},
                { "soleTrader",                         SoleTrader},
                { "hasCreditInformationEntries",        HasCreditInformationEntries},
                { "hasPaymentDefaults",                 HasPaymentDefaultEntries},
                { "underSupervisionOfInterests",        UnderSupervision},
                { "ownStoppage",                        OwnCreditStoppage},
                { "banOfBusiness",                      BanOfBusiness},
                { "participatesInCompanies",            ParticipatesInCompanies},
                { "numberOfCreditInformationAEntries",  NumberOfCreditInformationEntries},
                { "numberOfPaymentDefaults",            NumberOfPaymentDefaultEntries},
            };

            var values = new List<DictionaryEntry>();

            void AddValue(string name, string value) => values.Add(new DictionaryEntry(name, value));

            if (currentQType == QType41CreditInformationServices)
            {
                foreach (var availableField in q41AvailableFields)
                {
                    var setting = CreditReportSettings.SingleOrDefault(f => f.Field == availableField.Key);
                    if (setting != null)
                    {
                        AddValue(setting.Title, availableField.Value());
                    }
                }
            }

            if (currentQType == QType37PopulationInformationExtensiveLevel2Plus)
            {
                foreach (var availableField in q37AvailableFields)
                {
                    var setting = CreditReportSettings.SingleOrDefault(f => f.Field == availableField.Key);
                    if (setting != null)
                    {
                        AddValue(setting.Title, availableField.Value());
                    }
                }
            }

            return values;
        }

        protected class AddressRow
        {
            public string Street { get; set; }
            public string AddressCode { get; set; }
            public string Zip { get; set; }
            public string Town { get; set; }
            public string TwoLetterCountry { get; set; }
            /// <summary>
            /// Might be populated for temporary addresses. 
            /// </summary>
            public DateTime? StartDate { get; set; }
            /// <summary>
            /// Might be populated for temporary addresses. 
            /// </summary>
            public DateTime? EndDate { get; set; }

            public override string ToString()
            {
                return $"{Street}, {Zip}, {Town}";
            }
        }

        protected string BirthDate()
        {
            // personId ddmmyyXnnnc, X -> century of birth ‘+‘= 1800, ‘-‘= 1900, ‘A’= 2000
            var personId = GetPersonId();
            var centuryChar = personId.Substring(6, 1);
            var century = centuryChar == "-" ? "19" : (centuryChar == "A" ? "20" : "18");

            var ddmmyy = personId.Substring(0, 6);
            var year = ddmmyy.Substring(4, 2);
            var month = ddmmyy.Substring(2, 2);
            var day = ddmmyy.Substring(0, 2);
            var asDate = new DateTime(Convert.ToInt16(century + year), Convert.ToInt16(month), Convert.ToInt16(day));
            return asDate.ToString("yyyy-MM-dd");
        }

        protected string GetDomesticAddressSinceDate()
        {
            var permanentAddress = GetLatestAddressRow(PermanentCode);
            return permanentAddress?.StartDate?.ToString("yyyy-MM-dd") ?? null;
        }

        protected AddressRow ActivePosteRestanteAddress()
        {
            var addresses = GetAddresses();
            return addresses.FirstOrDefault(x => x.Street.Equals("Poste Restante", StringComparison.OrdinalIgnoreCase) && x.EndDate == null);
        }

        protected IEnumerable<AddressRow> GetAddresses(string addressCode = null)
        {
            var addressData = xmlDocument.Find("addressData", false);
            var allRows = addressData?.Children("addressRow") ?? Enumerable.Empty<XElement>();

            foreach (var address in allRows)
            {
                var currentAddressCode = address.GetRequiredValue("code");
                if (addressCode != null && currentAddressCode != addressCode)
                {
                    continue;
                }
                yield return new AddressRow
                {
                    Street = address.GetOptionalValue("address/street") ?? "",
                    Zip = address.GetOptionalValue("address/zip"),
                    Town = address.GetOptionalValue("address/town"),
                    AddressCode = currentAddressCode,
                    TwoLetterCountry = _domesticAddressCodes.Contains(currentAddressCode) ? "FI" : "",
                    StartDate = Dates.ParseDateTimeExactOrNull(address.GetOptionalValue("startDate"), "yyyy-MM-dd"),
                    EndDate = Dates.ParseDateTimeExactOrNull(address.GetOptionalValue("endDate"), "yyyy-MM-dd")
                };
            }

        }

        protected AddressRow GetLatestAddressRow(string addressCode = null) => GetAddresses(addressCode).FirstOrDefault();

        /// <summary>
        /// PO Boxes always trails with a "1" in the zipcode. 
        /// </summary>
        /// <returns></returns>
        protected AddressRow GetPostBoxAddress() => GetAddresses().FirstOrDefault(x => x.Zip.Trim().EndsWith("1"));

        private static string[] _domesticAddressCodes = new[]
            {PermanentCode, FormerPermanentCode, TemporaryCode, FormerTemporaryCode, PostalCode};

        protected static string PermanentCode => "01";
        protected static string FormerPermanentCode => "02";
        protected static string TemporaryCode => "03";
        protected static string FormerTemporaryCode => "04";
        /// <summary>
        /// AddressCode will be 05 (postal address row in Finland) with street "Poste Restante". 
        /// </summary>
        protected static string PostalCode => "05";

        private XElement PersonData => xmlDocument.Find("personData");
        protected string GetPersonId() => PersonData.GetRequiredValue("personIdentificationData/personId");
        protected string GetName(string name) => PersonData.GetRequiredValue($"personIdentificationData/populationRegisterName/{name}");
        protected string DateOfDeath() => PersonData.GetOptionalValue("dateOfDeath") ?? "";
        protected string NativeLanguage() => PersonData.GetOptionalValue("language/text") ?? "";
        protected string MaritalStatus() => PersonData.Child("maritalStatus").GetOptionalValue("text") ?? "";
        protected string NumberOfDependants() => PersonData.GetRequiredValue("childrenAndDependants/numberOfDependants");
        protected string PlaceOfResidence() => PersonData.GetOptionalValue("domicileData/text") ?? "";
        protected string NumberOfHabitants() => PersonData.GetOptionalValue("apartmentInformation/numberOfHabitants") ?? "";
        protected string ImmigrationDateOrNull() => PersonData.GetOptionalValue("immigrationDate");

        protected List<(string countryName, string countryCode, string startDate)> AllNationalities()
        {
            var nationalities = PersonData.OptionalChild("nationalityData")?.Children("nationalityRow");
            return nationalities?.Select(x =>
                (x.GetOptionalValue("text"), x.GetOptionalValue("code"), x.GetOptionalValue("startDate"))).ToList();
        }

        protected bool IsFinnishCitizen()
        {
            return AllNationalities()?.Any(x => x.countryName.Equals("Finland")) ?? false;
        }

        protected string NationalitiesCommaSeparated()
        {
            var nationalities = AllNationalities();
            return string.Join(", ", nationalities != null ? nationalities.Select(x => x.countryName) : new List<string>());
        }

        /// <summary>
        /// Examples of nationalityRow.text are Finland, Sweden, Denmark. 
        /// </summary>
        /// <returns></returns>
        protected string PersonsCitizenship()
        {
            var latestNationalityRow = PersonData.OptionalChild("nationalityData")?.Children("nationalityRow");
            var latestCitizenshipAlwaysFirst = latestNationalityRow?.FirstOrDefault();
            return latestCitizenshipAlwaysFirst?.GetOptionalValue("text") ?? "";
        }
        protected string PersonsCitizenshipStartDate()
        {
            var latestNationalityRow = PersonData.Children("nationalityRow");
            var latestCitizenshipAlwaysFirst = latestNationalityRow.FirstOrDefault();
            return latestCitizenshipAlwaysFirst?.GetOptionalValue("startDate") ?? "";
        }

        protected string RestrictionOfCompetenceToAct()
        {
            var supervisionDataRow = PersonData.OptionalChild("supervisionDataRow");
            // 0 = Supervision of interests has not been appointed, 1 = has been appointed
            if (int.TryParse(supervisionDataRow?.GetOptionalValue("supervisionOfInterests"), out var val) && val == 0)
            {
                return "No";
            }

            if (int.TryParse(supervisionDataRow?.GetOptionalValue("restrictionOnTheCompetenceToAct"), out var restrictionCode))
            {
                switch (restrictionCode)
                {
                    case 1:
                        return "Supervisor appointed but competence to act not restricted";
                    case 2:
                        return "Competence to act restricted";
                    case 3:
                        return "Declared legally incompetent";
                    default:
                        return "Could not parse restriction code. ";
                }
            }

            return "No";
        }

        protected string SupervisionOfInterest()
        {
            var supervisionDataRow = PersonData.OptionalChild("supervisionDataRow");
            return supervisionDataRow?.GetOptionalValue("supervisionOfInterests") == "1" ? "Yes" : "No";
        }

        protected string SupervisionStartDate()
        {
            var supervisionDataRow = PersonData.OptionalChild("supervisionDataRow");
            return supervisionDataRow?.GetOptionalValue("startDate") ?? "";
        }

        protected string PopulationRegisterStatus()
        {
            return xmlDocument.Find("populationRegisterStatusText").Value;
        }

        protected string InternalPersonStatus()
        {
            var populationRegisterStatus = xmlDocument.Find("populationRegisterStatus").Value;

            if (DateOfDeath() != "")
                return "dead";
            else if (SupervisionOfInterest() == "Yes")
                return "hasguardian";
            else if (populationRegisterStatus == "001")
                return "nodata"; // Person not found
            else if (populationRegisterStatus == "002")
                return "deactivated"; // ID is passive
            else
                return "normal";
        }

        ///////////// From credit information services ///////////////////

        protected string GetFullName() => xmlDocument.Find("personIdentification").GetOptionalValue("name") ?? "";

        protected string GetRegisteredAddressForPaymentDefault()
        {
            var address = xmlDocument.Find("personIdentification")?.OptionalChild("addressWhenDefaulted");
            if (address == null) return "";

            return $"{address?.GetOptionalValue("street")}, " +
                   $"{address?.GetOptionalValue("zip")}, " +
                   $"{address?.GetOptionalValue("town")}";
        }

        /// <summary>
        /// E = trader, empty = private person
        /// </summary>
        /// <returns></returns>
        protected string SoleTrader() => xmlDocument.Find("consumerResponse").GetOptionalValue("soletrader") == "E" ? "Yes" : "No";
        private XElement CreditInformationData => xmlDocument.Find("creditInformationData");
        private XElement CreditInformationSummary => CreditInformationData.Child("creditInformationEntrySummary");
        private string GetSummaryValueFor(string key) => bool.TryParse(CreditInformationSummary.GetRequiredValue(key), out var isTrue) && isTrue ? "Yes" : "No";
        protected string HasCreditInformationEntries() => GetSummaryValueFor("creditInformationEntries");
        protected string NumberOfCreditInformationEntries() => CreditInformationData.GetOptionalValue("creditInformationEntryCount") ?? "";
        protected string HasPaymentDefaultEntries() => GetSummaryValueFor("paymentDefaults");
        protected string NumberOfPaymentDefaultEntries() => CreditInformationData.GetOptionalValue("paymentDefaultCount") ?? "";
        protected string ParticipatesInCompanies() => GetSummaryValueFor("personInCharge");
        protected string UnderSupervision() => GetSummaryValueFor("supervision");
        protected string OwnCreditStoppage() => GetSummaryValueFor("ownCreditStoppage");
        protected string BanOfBusiness() => GetSummaryValueFor("banOfBusiness");
        protected string ExistsInRegister() => xmlDocument.Find("consumerResponse").GetOptionalValue("noRegisteredMessage/EIRText") == null ? "Yes" : "No";
    }

}