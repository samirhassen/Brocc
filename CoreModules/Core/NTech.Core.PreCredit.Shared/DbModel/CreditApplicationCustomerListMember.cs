namespace nPreCredit
{
    public class CreditApplicationCustomerListMember
    {
        public string ApplicationNr { get; set; }
        public CreditApplicationHeader CreditApplication { get; set; }
        public int CustomerId { get; set; }
        public string ListName { get; set; }
    }
}