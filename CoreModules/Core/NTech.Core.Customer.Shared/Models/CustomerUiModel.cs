using nCustomer.DbModel;
using NTech.Banking.Conversion;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer
{
    public static class CustomerUiModel
    {
        private static Lazy<Dictionary<CustomerProperty.Codes, ItemTemplate>> Templates = new Lazy<Dictionary<CustomerProperty.Codes, ItemTemplate>>(() =>
        {
            var d = new Dictionary<CustomerProperty.Codes, ItemTemplate>();
            Action<CustomerProperty.Codes, CustomerProperty.Groups, bool, bool, UiTypeCode> add = (code, group, isSensitive, isReadonly, uiCode) =>
                d.Add(code, new ItemTemplate { Name = code, Group = group, IsReadonly = isReadonly, IsSensitive = isSensitive, UiType = uiCode });

            add(CustomerProperty.Codes.email, CustomerProperty.Groups.insensitive, false, false, UiTypeCode.Email);
            add(CustomerProperty.Codes.phone, CustomerProperty.Groups.insensitive, false, false, UiTypeCode.Phonenr);

            add(CustomerProperty.Codes.firstName, CustomerProperty.Groups.insensitive, false, false, UiTypeCode.String);
            add(CustomerProperty.Codes.lastName, CustomerProperty.Groups.sensitive, true, false, UiTypeCode.String);

            add(CustomerProperty.Codes.civicRegNr, CustomerProperty.Groups.civicRegNr, true, true, UiTypeCode.Civicregnr);
            add(CustomerProperty.Codes.birthDate, CustomerProperty.Groups.insensitive, false, false, UiTypeCode.Date);

            add(CustomerProperty.Codes.addressStreet, CustomerProperty.Groups.sensitive, true, false, UiTypeCode.String);
            add(CustomerProperty.Codes.addressZipcode, CustomerProperty.Groups.sensitive, true, false, UiTypeCode.String);
            add(CustomerProperty.Codes.addressCity, CustomerProperty.Groups.sensitive, true, false, UiTypeCode.String);
            add(CustomerProperty.Codes.addressCountry, CustomerProperty.Groups.sensitive, true, false, UiTypeCode.String);

            add(CustomerProperty.Codes.orgnr, CustomerProperty.Groups.orgnr, true, true, UiTypeCode.Orgnr);
            add(CustomerProperty.Codes.companyName, CustomerProperty.Groups.official, false, false, UiTypeCode.String);
            add(CustomerProperty.Codes.amlRiskClass, CustomerProperty.Groups.amlCft, true, false, UiTypeCode.String);

            return d;
        });

        public static ItemTemplate GetTemplate(string name)
        {
            var c = Enums.Parse<CustomerProperty.Codes>(name);
            return c.HasValue ? Templates.Value.Opt(c.Value) : null;
        }

        public static ItemTemplate GetTemplate(CustomerProperty.Codes name)
        {
            return Templates.Value.Opt(name);
        }

        public static (bool isValid, string normalizedValue) ValidateAndNormalize(string value, UiTypeCode uiTypeCode, IClientConfigurationCore clientConfiguration)
        {
            var v = string.IsNullOrWhiteSpace(value) ? null : value?.Trim();
            if (v == null)
                return (false, (string)null);
            switch (uiTypeCode)
            {
                case UiTypeCode.String:
                    return (true, v);
                case UiTypeCode.Date:
                    {
                        var d = NTech.Dates.ParseDateTimeExactOrNull(v, "yyyy-MM-dd");
                        if (d.HasValue)
                            return (true, d.Value.ToString("yyyy-MM-dd"));
                        else
                            return (false, (string)null);
                    }
                case UiTypeCode.Email:
                    {
                        if (v.Contains("@") && v.First() != '@' && v.Last() != '@' && v.Last() != '.') //Just to prevent common typing mistakes. Not intended to be a defense against malicious input.
                            return (true, v);
                        else
                            return (false, (string)null);
                    }
                case UiTypeCode.Phonenr:
                    {
                        var vr = PhoneNumberHandler.GetInstance(clientConfiguration.Country.BaseCountry).Parse(v);
                        if (vr.IsValid)
                            return (true, v);
                        else
                            return (false, (string)null);
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public class ItemTemplate
        {
            public CustomerProperty.Codes Name { get; set; }
            public CustomerProperty.Groups Group { get; set; }
            public bool IsSensitive { get; set; }
            public bool IsReadonly { get; set; }
            public UiTypeCode UiType { get; set; } = UiTypeCode.String; //For validation and such
        }

        public enum UiTypeCode
        {
            String,
            Email,
            Phonenr,
            Date,
            Boolean,
            Civicregnr,
            Orgnr,
            Custom
        }

        private static HashSet<string> EditableContactInfoItemNamessWhitelist = new HashSet<string> {
            "email", "phone", "firstName", "lastName", "birthDate",
            "addressStreet", "addressZipcode", "addressCity", "addressCountry", "companyName", "amlRiskClass" };


        public static bool IsEditableContactInfoItem(string name)
        {
            var template = CustomerUiModel.GetTemplate(name);
            return (template != null && !template.IsReadonly && EditableContactInfoItemNamessWhitelist.Contains(name));
        }
    }
}