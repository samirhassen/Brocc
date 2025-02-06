namespace nCreditReport
{
    public enum SystemItemCode
    {
        DwLatestMergedTimestamp_Dimension_CreditReportItem,
        DwLatestMergedTimestamp_Dimension_CreditReportItem2
    }

    public class SystemItem : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}