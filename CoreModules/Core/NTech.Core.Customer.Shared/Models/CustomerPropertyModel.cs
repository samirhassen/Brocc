using nCustomer.DbModel;
using System;
using System.Collections.Generic;

namespace nCustomer
{
    public class CustomerPropertyModelExtended : CustomerPropertyModel
    {
        public int Id { get; set; }
        public DateTimeOffset ChangeDate { get; set; }
        public int ChangedById { get; set; }
        public string ChangedByDisplayName { get; set; }
        public int? CreatedByBusinessEventId { get; set; }
    }

    public class CustomerPropertyModel
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public int CustomerId { get; set; }
        public string Value { get; set; }
        public bool IsSensitive { get; set; }
        public bool? ForceUpdate { get; set; }

        public static List<string> AdressHashFieldNames = new List<string> { "addressStreet", "addressZipcode", "addressCity", "addressCountry" };
        public static List<string> CustomerNameFields = new List<string> { "firstName", "lastName" };
        public static List<string> ContactInfoFieldNames = new List<string> { "email", "phone" };

        private class Template
        {
            public string GroupName { get; set; }
            public bool IsSensitive { get; set; }
        }
        private static Lazy<Dictionary<string, Template>> templates = new Lazy<Dictionary<string, Template>>(() =>
        {
            var t = new Dictionary<string, Template>(StringComparer.OrdinalIgnoreCase);

            Func<string, string, bool, Template> a = (n, g, s) =>
            {
                var tmp = new Template { GroupName = g, IsSensitive = s };
                t[n] = tmp;
                return tmp;
            };

            a("firstName", "official", false);
            a("lastName", "sensitive", true);
            a("email", "insensitive", false);
            a("phone", "insensitive", false);
            a("addressZipcode", "sensitive", true);
            a("addressStreet", "sensitive", true);
            a("addressCity", "sensitive", true);
            a("addressCountry", "sensitive", true);
            a("civicRegNr", "civicRegNr", true);
            a(CustomerProperty.Codes.companyName.ToString(), "official", false);
            a(CustomerProperty.Codes.orgnr.ToString(), "orgnr", true);
            a(CustomerProperty.Codes.isCompany.ToString(), "orgnr", false);
            a(CustomerProperty.Codes.orgnr_country.ToString(), "orgnr", false);
            a("birthDate", "insensitive", false);
            a("includeInFatcaExport", "fatca", false);
            a("hasOtherTaxOrCitizenCountry", "amlCft", false);
            a(CustomerProperty.Codes.hasOtherTaxCountry.ToString(), "amlCft", false);
            a(CustomerProperty.Codes.hasOtherCitizenCountry.ToString(), "amlCft", false);

            return t;
        });

        public static bool IsPropertySensitive(string name)
        {
            return templates.Value.ContainsKey(name) && templates.Value[name].IsSensitive;
        }

        public static bool IsCustomerIdProperty(string name)
        {
            return CustomerIdNames.Contains(name);
        }

        private static ISet<string> CustomerIdNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                CustomerProperty.Codes.orgnr.ToString(),
                CustomerProperty.Codes.orgnr_country.ToString(),
                CustomerProperty.Codes.isCompany.ToString(),
                CustomerProperty.Codes.civicRegNr.ToString(),
                CustomerProperty.Codes.civicregnr_country.ToString(),
                "civicRegNrCountry"
            };

        public static CustomerPropertyModel Create(int customerId, string name, string value, bool isCustomerKycQuestion, bool? forceUpdate = null, bool? forceSensetiveIfNoTemplate = false)
        {
            if (!isCustomerKycQuestion)
            {
                Template t;
                if (templates.Value.TryGetValue(name, out t))
                {
                    return new CustomerPropertyModel
                    {
                        CustomerId = customerId,
                        Group = t.GroupName,
                        IsSensitive = t.IsSensitive,
                        Name = name,
                        Value = value,
                        ForceUpdate = forceUpdate
                    };
                }
                else
                {
                    return new CustomerPropertyModel
                    {
                        CustomerId = customerId,
                        Group = "insensitive",
                        IsSensitive = forceSensetiveIfNoTemplate ?? false,
                        Name = name,
                        Value = value,
                        ForceUpdate = forceUpdate
                    };
                }
            }
            else
            {
                var groupName = "amlCft";
                if (name == "taxcountries" || name == "citizencountries")
                    groupName = "taxResidency";
                else if (name.Contains("pep"))
                    groupName = "pep";
                return new CustomerPropertyModel
                {
                    CustomerId = customerId,
                    Group = groupName,
                    IsSensitive = false,
                    Name = name,
                    Value = value,
                    ForceUpdate = forceUpdate
                };
            }
        }
    }
}