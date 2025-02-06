using NTech.Core.Module.Shared.Infrastructure;

namespace nCustomer.Code
{
    public class NtechCurrentUserMetadata
    {
        public int UserId { get; set; }
        public string InformationMetadata { get; set; }
        public INTechCurrentUserMetadata CoreUser { get; set; }
    }
}