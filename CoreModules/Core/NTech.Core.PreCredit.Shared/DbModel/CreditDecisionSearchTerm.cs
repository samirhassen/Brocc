namespace nPreCredit
{
    //Intentionally not an infrastructure-item since this is just a part of a credit decision not it's own thing
    public class CreditDecisionSearchTerm
    {
        public int Id { get; set; }
        public string TermName { get; set; }
        public string TermValue { get; set; }
        public CreditDecision Decision { get; set; }
        public int CreditDecisionId { get; set; }

        public enum CreditDecisionSearchTermCode
        {
            RejectionReason
        }
    }
}