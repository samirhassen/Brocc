using System.Collections.Generic;

namespace NTech.Services.Infrastructure.CreditStandard
{
    /// <summary>
    /// Exposed in apis for ml/ul standard
    /// </summary>
    public class EnumsApiModel
    {
        public List<EnumItemApiModel> CivilStatuses { get; set; }
        public List<EnumItemApiModel> EmploymentStatuses { get; set; }
        public List<EnumItemApiModel> HousingTypes { get; set; }
        public List<EnumItemApiModel> OtherLoanTypes { get; set; }
    }
}
