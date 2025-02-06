using NTech.Core.Module.Shared.Database;

namespace nPreCredit.DbModel
{
    public class StandardPolicyFilterRuleSet : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string SlotName { get; set; }
        public string RuleSetName { get; set; }
        public string RuleSetModelData { get; set; }
    }
}