namespace NTechSignicat.Services
{
    public enum SignatureSessionStateCode
    {
        Broken,
        PendingAllSignatures,
        PendingSomeSignatures,
        Failed,
        SignaturesSuccessful,
        Cancelled
    }
}
