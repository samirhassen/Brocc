using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Services.Infrastructure.NTechWs
{
    public class NTechWebserviceRequestValidator
    {
        public List<ValidationErrorItem> Validate<T>(T instance)
        {
            var result = ComponentModelAnnotationsObjectValidator.Validate(instance);
            return result?.Select(x => new ValidationErrorItem
            {
                FirstMessage = x.FirstMessage,
                ListErrorCount = x.ListErrorCount,
                ListFirstErrorIndex = x.ListFirstErrorIndex,
                Name = x.Name,
                Path = x.Path
            })?.ToList();
        }

        public static void InitializeValidationFramework(Func<string, bool> isValidCivicRegNr, Func<string, bool> isValidBankAccountNr, Func<string, bool> isValidOrgNr)
        {
            CivicRegNrAttribute.Initialize(isValidCivicRegNr);
            BankAccountNrAttribute.Initialize(isValidBankAccountNr);
            OrgNrAttribute.Initialize(isValidOrgNr);
        }

        public class ValidationErrorItem
        {
            public string Path { get; set; }
            public string Name { get; set; }
            public string FirstMessage { get; set; }
            public int? ListFirstErrorIndex { get; set; }
            public int ListErrorCount { get; set; }
        }
    }
}