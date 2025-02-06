namespace nPreCredit.DbModel
{
    public class WorkListItemProperty //Intentionally not infrastructure item since this is readonly historical data
    {
        public int WorkListHeaderId { get; set; }
        public string ItemId { get; set; }
        public WorkListItem Item { get; set; }
        public bool IsEncrypted { get; set; }
        public string DataTypeName { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}