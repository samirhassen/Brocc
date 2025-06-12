using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using nCustomerPages.Code;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.Shared.BankAccounts.Fi;
using CivicRegNumberFi = NTech.Banking.CivicRegNumbers.Fi.CivicRegNumberFi;

namespace nCustomerPages.Controllers.UnsecuredCreditAffiliates.ProviderIntegrations
{
    public abstract class ProviderIntegrationBase
    {
        protected List<ExternalProviderApplicationController.ExternalApplicationRequest.Item> externalItems;
        protected List<CreditApplicationRequest.Item> internalItems;
        protected List<string> errors;
        protected List<string> parsedNames;
        private bool hasBeenCalled;

        private IDictionary<string, ISet<string>> internalNameDuplicatePreventionSets =
            new Dictionary<string, ISet<string>>
            {
                { "civicRegNr", new HashSet<string>() }
            };

        protected virtual bool IncludeInternalNamesInErrorMessage => true;

        protected void RequiredCivicRegNumberFi(string externalName, string internalGroup, string internalName,
            Action<CivicRegNumberFi> afterAdd = null)
        {
            RequiredX(externalName, internalGroup, internalName,
                "Finnish personbeteckning/SSN",
                s =>
                {
                    CivicRegNumberFi c;
                    if (!CivicRegNumberFi.TryParse(s, out c))
                    {
                        return Tuple.Create(false, (CivicRegNumberFi)null,
                            "Finnish personbeteckning/SSN should have the format DDMMYYSNNNK");
                    }

                    return Tuple.Create(true, c, (string)null);
                },
                x => x.NormalizedValue,
                afterAdd: afterAdd);
        }

        protected void RequiredIbanFi(string externalName, string internalGroup, string internalName,
            Action<IBANFi> afterAdd = null)
        {
            RequiredX(externalName, internalGroup, internalName,
                "Finnish IBAN",
                s => !IBANFi.TryParse(s, out var iban)
                    ? Tuple.Create(false, (IBANFi)null, "Invalid finnish IBAN. Format should be FI<16 nrs>")
                    : Tuple.Create(true, iban, (string)null),
                x => x.NormalizedValue,
                afterAdd: afterAdd);
        }

        protected void RequiredDecimal(string externalName, string internalGroup, string internalName,
            Func<decimal, decimal> transformBeforeAdd = null)
        {
            RequiredX(externalName, internalGroup, internalName,
                "Decimal",
                s => !decimal.TryParse(s, NumberStyles.Integer | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var d)
                    ? Tuple.Create(false, default(decimal), "Decimals should have the format 9999.99")
                    : Tuple.Create(true, d, (string)null),
                x =>
                {
                    if (transformBeforeAdd != null)
                        x = transformBeforeAdd(x);
                    return x.ToString(CultureInfo.InvariantCulture);
                });
        }

        protected void RequiredInt(string externalName, string internalGroup, string internalName,
            Func<int, int> transformBeforeAdd = null)
        {
            RequiredX(externalName, internalGroup, internalName,
                "Integer",
                s => !int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
                    ? Tuple.Create(false, default(int), "Integers should have the format 9999")
                    : Tuple.Create(true, i, (string)null),
                x =>
                {
                    if (transformBeforeAdd != null)
                        x = transformBeforeAdd(x);
                    return x.ToString(CultureInfo.InvariantCulture);
                });
        }

        private bool TryParseEmail(string input, out MailAddress m)
        {
            try
            {
                m = new MailAddress(input);
                return true;
            }
            catch
            {
                m = null;
                return false;
            }
        }

        protected void RequiredEmail(string externalName, string internalGroup, string internalName)
        {
            RequiredX(externalName, internalGroup, internalName,
                "Email",
                s =>
                {
                    if (!TryParseEmail(s, out var m))
                    {
                        return Tuple.Create(false, (MailAddress)null,
                            "Email addresses should adher to RFC 5322");
                    }

                    return Tuple.Create(true, m, (string)null);
                },
                x => x.Address);
        }

        protected void RequiredEnum<T>(string externalName, string internalGroup, string internalName,
            Func<T, string> mapToInternal)
        {
            if (!typeof(T).IsEnum)
                throw new Exception("Program error. Enum type is not an enum");

            RequiredX(externalName, internalGroup, internalName,
                "Enum",
                s =>
                {
                    var allValues = Enum.GetValues(typeof(T)).Cast<T>().ToList();
                    foreach (var v in allValues)
                    {
                        if (v.ToString().ToLowerInvariant() == s?.ToLowerInvariant())
                            return Tuple.Create(true, v, (string)null);
                    }

                    return Tuple.Create(false, default(T),
                        "Enum value should be one of " + string.Join("|", allValues.Select(x => x.ToString())));
                },
                mapToInternal);
        }

        protected void RequiredString(string externalName, string internalGroup, string internalName, int maxLength,
            bool filterLinebreaks = true)
        {
            RequiredX(externalName, internalGroup, internalName,
                "String",
                s =>
                {
                    if (filterLinebreaks)
                        s = FilterLineBreaks(s);
                    if ((s?.Length ?? 0) > maxLength)
                    {
                        return Tuple.Create(false, (string)null,
                            $"String({maxLength}) cannot be longer than {maxLength}");
                    }
                    else
                    {
                        return Tuple.Create(true, s?.Trim(), (string)null);
                    }
                },
                x => x);
        }

        private static string FilterLineBreaks(string s)
        {
            return s == null
                ? null
                : Regex.Replace(s, @"[\u000A\u000B\u000C\u000D\u2028\u2029\u0085]+", " "); //Remove linebreaks;
        }

        protected void OptionalString(string externalName, string internalGroup, string internalName, int maxLength,
            bool filterLinebreaks = true)
        {
            OptionalX(externalName, internalGroup, internalName,
                "String",
                s =>
                {
                    if (filterLinebreaks)
                        s = FilterLineBreaks(s);
                    if ((s?.Length ?? 0) > maxLength)
                    {
                        return Tuple.Create(false, (string)null,
                            $"String({maxLength}) cannot be longer than {maxLength}");
                    }

                    return Tuple.Create(true, s?.Trim(), (string)null);
                },
                x => x);
        }

        protected void OptionalMonth(string externalName, string internalGroup, string internalName)
        {
            OptionalX<DateTime>(externalName, internalGroup, internalName,
                "Month",
                s => !DateTime.TryParseExact(s + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var d)
                    ? Tuple.Create(false, default(DateTime), "Months should have the format YYYY-MM")
                    : Tuple.Create(true, d, (string)null), x => x.ToString("yyyy-MM"));
        }

        protected void OptionalDate(string externalName, string internalGroup, string internalName)
        {
            OptionalX<DateTime>(externalName, internalGroup, internalName,
                "Date",
                s => !DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out var d)
                    ? Tuple.Create(false, default(DateTime), "Dates should have the format YYYY-MM-DD")
                    : Tuple.Create(true, d, (string)null), x => x.ToString("yyyy-MM-dd"));
        }

        private readonly HashSet<string> allowedButIgnoredItems = new HashSet<string>();

        protected void AllowedButIgnored(string externalName)
        {
            allowedButIgnoredItems.Add(externalName);
        }

        protected void RequiredX<T>(string externalName, string internalGroup, string internalName, string typeName,
            Func<string, Tuple<bool, T, string>> parse, Func<T, string> format, Action<T> afterAdd = null)
        {
            parsedNames.Add(externalName);

            var mappingToMessage = this.IncludeInternalNamesInErrorMessage
                ? $" mapping to {internalGroup}.{internalName}"
                : "";
            Action<string> addError = m => errors.Add($"Required {typeName} {externalName}{mappingToMessage}: {m}");
            var hits = externalItems.Where(x => x.Name == externalName).ToList();
            if (hits.Count == 0)
            {
                addError("Is missing");
                return;
            }

            if (hits.Count > 1)
            {
                addError("Occurs multiple times");
                return;
            }

            var item = hits.Single();

            if (string.IsNullOrWhiteSpace(item.Value))
            {
                addError("Is empty");
            }

            var parseResult = parse(item.Value);
            if (!parseResult.Item1)
            {
                addError($"Is invalid. {parseResult.Item3}");
                return;
            }

            var formattedValue = format(parseResult.Item2);
            if (internalNameDuplicatePreventionSets != null &&
                internalNameDuplicatePreventionSets.TryGetValue(internalName, out var ds))
            {
                if (ds.Contains(formattedValue))
                {
                    addError("Is a duplicate");
                    return;
                }

                ds.Add(formattedValue);
            }

            internalItems.Add(new CreditApplicationRequest.Item
            {
                Group = internalGroup,
                Name = internalName,
                Value = formattedValue
            });
            afterAdd?.Invoke(parseResult.Item2);
        }

        protected void OptionalInt(string externalName, string internalGroup, string internalName)
        {
            OptionalX(externalName, internalGroup, internalName,
                "Integer",
                s => !int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
                    ? Tuple.Create(false, 0, "Integers should have the format 9999")
                    : Tuple.Create(true, i, (string)null),
                x => x.ToString(CultureInfo.InvariantCulture));
        }

        protected void OptionalBool(string externalName, string internalGroup, string internalName)
        {
            OptionalX(externalName, internalGroup, internalName,
                "Boolean",
                s =>
                {
                    var value = s?.Trim().ToLowerInvariant();
                    if (value != "true" && value != "false")
                    {
                        return Tuple.Create(false, false, "Booleans should be true or false");
                    }

                    return Tuple.Create(true, value == "true", "Booleans should be true or false");
                },
                b => b ? "true" : "false");
        }

        protected void OptionalX<T>(string externalName, string internalGroup, string internalName, string typeName,
            Func<string, Tuple<bool, T, string>> parse, Func<T, string> format)
        {
            parsedNames.Add(externalName);

            var mappingToMessage = this.IncludeInternalNamesInErrorMessage
                ? $" mapping to {internalGroup}.{internalName}"
                : "";
            var hits = externalItems.Where(x => x.Name == externalName).ToList();
            if (hits.Count == 0)
            {
                return;
            }

            if (hits.Count > 1)
            {
                AddError("Occurs multiple times");
                return;
            }

            var item = hits.Single();

            if (string.IsNullOrWhiteSpace(item.Value))
            {
                return;
            }

            var parseResult = parse(item.Value);
            if (!parseResult.Item1)
            {
                AddError($"Is invalid. {parseResult.Item3}");
                return;
            }

            internalItems.Add(new CreditApplicationRequest.Item
            {
                Group = internalGroup,
                Name = internalName,
                Value = format(parseResult.Item2)
            });
            return;
            void AddError(string m) => errors.Add($"Optional {typeName} {externalName}{mappingToMessage}: {m}");
        }

        protected abstract Tuple<bool, CreditApplicationRequest> DoTranslate();

        public Tuple<bool, CreditApplicationRequest, List<string>> Translate(
            ExternalProviderApplicationController.ExternalApplicationRequest externalRequest)
        {
            if (hasBeenCalled)
            {
                throw new Exception("Provider integrations are one time use. Create a new one to call again");
            }

            hasBeenCalled = true;
            internalItems = new List<CreditApplicationRequest.Item>();
            errors = new List<string>();
            parsedNames = new List<string>();
            externalItems = null;

            CreditApplicationRequest internalRequest = null;

            if (externalRequest == null)
            {
                errors.Add(
                    "Request body missing. (Possible cause is missing Content-Type header which should be application/json)");
                return Tuple.Create(false, internalRequest, errors);
            }

            if (errors.Count > 0)
            {
                return Tuple.Create(false, internalRequest, errors);
            }

            externalItems = externalRequest.Items ??
                            new List<ExternalProviderApplicationController.ExternalApplicationRequest.Item>();

            if (string.IsNullOrWhiteSpace(externalRequest.ExternalId))
            {
                errors.Add("ExternalId is missing");
            }

            internalItems.Add(new CreditApplicationRequest.Item
            {
                Group = "application",
                Name = "providerApplicationId",
                Value = externalRequest.ExternalId
            });

            var result = DoTranslate();

            var unknownItems = externalItems.Where(x =>
                !parsedNames.Contains(x.Name) && !allowedButIgnoredItems.Contains(x.Name));
            if (unknownItems.Any())
            {
                errors.Add("Unknown items encountered: " + string.Join(", ", unknownItems.Select(x => x.Name)));
            }

            if (!result.Item1 || errors.Count > 0)
            {
                return Tuple.Create(false, internalRequest, errors);
            }

            return Tuple.Create(true, result.Item2, (List<string>)null);
        }
    }
}