using NTech.Core.Module.Shared.Database;

namespace NTech.Core.Savings.Shared.DbModel
{
    public class KeyValueItem : InfrastructureBaseItem
    {
        public string Key { get; set; }
        public string KeySpace { get; set; }
        public string Value { get; set; }
    }
}