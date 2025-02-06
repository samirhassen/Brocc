using NTech.Core.Module.Shared.Database;
using System.Collections.Generic;

namespace nPreCredit.DbModel
{
    public class FraudControlProperty : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string Value { get; set; }
        public string Name { get; set; }
        public FraudControlProperty ReplacesFraudControlProperty { get; set; }
        public int? ReplacesFraudControlProperty_Id { get; set; }
        public virtual List<FraudControlProperty> ReplacedByFraudControlProperties { get; set; }
        public bool IsCurrentData { get; set; }
    }
}