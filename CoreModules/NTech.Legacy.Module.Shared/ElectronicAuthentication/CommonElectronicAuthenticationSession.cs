using System.Collections.Generic;

namespace NTech.Services.Infrastructure.ElectronicAuthentication
{
    public class CommonElectronicAuthenticationSession
    {
        public string LocalSessionId { get; set; }
        public string ProviderName { get; set; }
        public string ProviderSessionId { get; set; }
        public string BeginLoginRedirectUrl { get; set; }
        public string ExpectedCivicRegNumber { get; set; }
        public Dictionary<string, string> CustomData { get; set; }
        public bool IsClosed { get; set; }
        public bool IsAuthenticated { get; set; }
        public AuthenticatedUserModel AuthenticatedUser { get; set; }
        public string FailedMessage { get; set; }

        /// <summary>
        /// Beware. Different providers may support a different subset of these.
        /// </summary>
        public class AuthenticatedUserModel
        {
            public string CivicRegNumber { get; set; }
            public string FullName { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string IpAddress { get; set; }
        }

        public void SetCustomData(string key, string value)
        {
            CustomData = CustomData ?? new Dictionary<string, string>();
            CustomData[key] = value;
        }
    }
}
