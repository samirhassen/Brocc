namespace nPreCredit
{
    //Intentionally not an infrastructure-item since this is just a part of a credit decision not it's own thing
    public class CreditDecisionItem
    {
        public int Id { get; set; }
        public string ItemName { get; set; }
        public bool IsRepeatable { get; set; } //More than one of the same name can exist ... like RejectionReason
        public string Value { get; set; }
        public CreditDecision Decision { get; set; }
        public int CreditDecisionId { get; set; }
    }
}