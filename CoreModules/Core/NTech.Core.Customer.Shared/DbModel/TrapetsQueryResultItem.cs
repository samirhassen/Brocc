using NTech.Core.Module.Shared.Database;

namespace nCustomer.DbModel
{
    public enum KycScreeningQueryResultItemCode
    {
        QueryBirthDate,
        QueryFullName,
        QueryCountryCodes,
        PepHitExternalIds,
        SanctionHitExternalIds,
        IsForDailyBatchScreen,
        CustomerUpdateDoneDate,
        ResultedInConflict
    }

    public class TrapetsQueryResultItem : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public int TrapetsQueryResultId { get; set; }
        public TrapetsQueryResult QueryResult { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsEncrypted { get; set; }
    }
}