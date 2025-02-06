using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Services.Infrastructure.CreditStandard
{
    public class CreditStandardEnumService
    {
        private CreditStandardEnumService()
        {

        }

        public static CreditStandardEnumService Instance => new CreditStandardEnumService();

        public EnumsApiModel GetApiEnums(string language = null) => new EnumsApiModel
        {
            CivilStatuses = GetApiItems(CreditStandardEnumTypeCode.CivilStatus, language: language),
            EmploymentStatuses = GetApiItems(CreditStandardEnumTypeCode.Employment, language: language),
            HousingTypes = GetApiItems(CreditStandardEnumTypeCode.HousingType, language: language),
            OtherLoanTypes = GetApiItems(CreditStandardEnumTypeCode.OtherLoanType, language: language)
        };

        public List<EnumItemApiModel> GetApiItems(CreditStandardEnumTypeCode enumType, string language = null)
        {
            switch (enumType)
            {
                case CreditStandardEnumTypeCode.LoanPurpose: return GetApiItemsFromModel(CreditStandardLoanPurpose.EnglishDisplayNameByCode);
                case CreditStandardEnumTypeCode.CivilStatus: return GetApiItemsFromModel(CreditStandardCivilStatus.EnglishDisplayNameByCode);
                case CreditStandardEnumTypeCode.HousingType: return GetApiItemsFromModel(CreditStandardHousingType.GetDisplayNameCodes(language));
                case CreditStandardEnumTypeCode.OtherLoanType: return GetApiItemsFromModel(CreditStandardOtherLoanType.GetDisplayNameCodes(language));
                case CreditStandardEnumTypeCode.Employment: return GetApiItemsFromModel(CreditStandardEmployment.GetDisplayNameCodes(language));
                case CreditStandardEnumTypeCode.BankAccountNrType: return GetApiItemsFromModel(CreditStandardBankAccountNrType.EnglishDisplayNameByCode);
                default:
                    throw new NotImplementedException();
            }
        }

        private List<EnumItemApiModel> GetApiItemsFromModel<TEnum>(Dictionary<TEnum, string> nameByCode) =>
            nameByCode.Select(x => new EnumItemApiModel(x.Key.ToString(), x.Value)).ToList();


    }
}
