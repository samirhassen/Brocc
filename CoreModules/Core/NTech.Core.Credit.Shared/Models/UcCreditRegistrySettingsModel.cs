using System;

namespace nCredit
{
    public class UcCreditRegistrySettingsModel
    {
        public Uri SharkEndpoint { get; set; }
        public string SharkUsername { get; set; }
        public string SharkPassword { get; set; }
        public string SharkCreditorId { get; set; }
        public string SharkSourceSystemId { get; set; }
        public string SharkDeliveryUniqueId { get; set; }
        public string SharkGroupId { get; set; }
        public string LogFolder { get; set; }
    }
}