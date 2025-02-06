namespace nPreCredit.DbModel
{
    public class WorkListFilterItem //Intentionally not infrastructure item since this is readonly historical data
    {
        public int WorkListHeaderId { get; set; }
        public WorkListHeader WorkList { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}