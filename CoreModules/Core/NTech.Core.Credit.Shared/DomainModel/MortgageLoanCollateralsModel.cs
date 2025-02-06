using System;
using System.Collections.Generic;

namespace nCredit.DomainModel
{
    public class MortgageLoanCollateralsModel
    {
        public List<CollateralModel> Collaterals { get; set; }

        public class CollateralModel
        {
            public bool IsMain { get; set; }
            public string CollateralId { get; set; }
            public List<PropertyModel> Properties { get; set; }
            public List<ValuationModel> Valuations { get; set; }
            public List<int> CustomerIds { get; set; }
        }

        public class PropertyModel
        {
            public string CodeName { get; set; }
            public string DisplayName { get; set; }
            public string TypeCode { get; set; }
            public string CodeValue { get; set; }
            public string DisplayValue { get; set; }
        }

        public class ValuationModel
        {
            public DateTime? ValuationDate { get; set; }
            public decimal Amount { get; set; }
            public string TypeCode { get; set; }
            public string SourceDescription { get; set; }
        }
    }
}