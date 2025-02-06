using NTech.Core.Module.Shared.Database;

namespace nPreCredit.DbModel
{
    public class HandlerLimitLevel : InfrastructureBaseItem
    {
        public int HandlerUserId { get; set; }
        public int LimitLevel { get; set; }
        public bool IsOverrideAllowed { get; set; }
    }
}