namespace NTech.Core.Credit.Shared.DbModel
{
    public class SieFileTransaction
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string AccountNr { get; set; }
        public SieFileVerification Verification { get; set; }
        public int VerificationId { get; set; }
    }
}
