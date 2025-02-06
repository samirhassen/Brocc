using NTech.Core.Module.Shared.Database;

namespace nSavings
{
    public enum SystemItemCode
    {
        TrapetsAml_LatestTimestamp_KycQuestionsAndAnswers,
        TrapetsAml_LatestTimestamp_Customer,
        TrapetsAml_LatestTimestamp_Transaction,
        TrapetsAml_LatestTimestamp_Account,
        TrapetsAml_LatestTimestamp_Asset,
        Cm1Aml_LatestId_Transaction
    }

    public class SystemItem : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}