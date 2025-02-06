namespace NTech.Core.Credit.Shared.DbModel
{
    public class SieFileConnection
    {
        public SieFileVerification Verification { get; set; }
        public int VerificationId { get; set; }
        public string ConnectionType { get; set; }
        public string ConnectionId { get; set; }
    }
}
