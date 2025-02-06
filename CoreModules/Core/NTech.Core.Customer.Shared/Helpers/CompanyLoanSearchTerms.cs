using Dapper;
using nCustomer.DbModel;
using NTech.Core;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace nCustomer.Code.Services
{
    public static class CompanyLoanSearchTerms
    {
        public static string NormalizeCompanyName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var s = RemoveDiacritics(Regex.Replace(new string(name.Where(x => Char.IsLetterOrDigit(x) || Char.IsWhiteSpace(x)).ToArray()).ToLowerInvariant().Trim(), @"\s+", " "));
            return s.Length < 128 ? s : s.Substring(0, 128); //Search term table only allows 128 chars
        }

        public static void PopulateSearchTermsGroupComposable(INTechCurrentUserMetadata currentUser, IEnumerable<Tuple<int, string>> idsAndNames, ICustomerContext context, ICoreClock clock, Phonix.DoubleMetaphone generator = null)
        {
            generator = generator ?? new Phonix.DoubleMetaphone();

            //Remove existing            
            var query = $"update dbo.CustomerSearchTerm set IsActive = 0, ChangedById = @userId, ChangedDate = @changedDate where TermCode in (@searchTerm1, @searchTerm2) and CustomerId in (@customerIds)";
            context.GetConnection().Execute(query,
                param: new
                {
                    userId = currentUser.UserId,
                    changedDate = clock.Now,
                    searchTerm1 = SearchTermCode.companyNameNormalized.ToString(),
                    searchTerm2 = SearchTermCode.companyNamePhonetic.ToString(),
                    customerIds = idsAndNames.Select(x => x.Item1).ToList()
                },
                transaction: context.CurrentTransaction);

            //Add new
            Action<int, SearchTermCode, string> addCode = (customerId, code, value) =>
            {
                context.AddCustomerSearchTerms(new CustomerSearchTerm
                {
                    IsActive = true,
                    CustomerId = customerId,
                    ChangedById = currentUser.UserId,
                    ChangedDate = clock.Now,
                    InformationMetaData = currentUser.InformationMetadata,
                    TermCode = code.ToString(),
                    Value = value
                });
            };
            foreach (var idAndName in idsAndNames)
            {
                var customerId = idAndName.Item1;
                var companyName = idAndName.Item2;
                var normalizedName = NormalizeCompanyName(companyName);
                addCode(customerId, SearchTermCode.companyNameNormalized, normalizedName);
                foreach (var token in Tokenize(normalizedName, generator))
                {
                    addCode(customerId, SearchTermCode.companyNamePhonetic, token);
                }
            }
        }

        public static List<string> Tokenize(string value, Phonix.DoubleMetaphone generator = null)
        {
            if (generator == null)
                generator = new Phonix.DoubleMetaphone();
            return value.Split(new char[0]).Where(y => !string.IsNullOrWhiteSpace(y)).Select(y => generator.BuildKey(y)).Where(y => !string.IsNullOrWhiteSpace(y)).ToList();
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}