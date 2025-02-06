namespace nCustomerPages.Code
{
    public class CustomerLockedHostClient : AbstractSystemUserServiceClient
    {
        private int customerId;
        protected override string ServiceName => "NTechHost";

        public CustomerLockedHostClient(int customerId)
        {
            this.customerId = customerId;
        }

        public bool GetIsKycUpdateRequired()
        {
            return Begin()
                .PostJson("Api/Customer/KycQuestionUpdate/GetCustomerStatus", new
                {
                    CustomerId = customerId
                })
                .ParseJsonAsAnonymousType(new { IsUpdateRequired = (bool?)null })
                ?.IsUpdateRequired ?? false;
        }

        public bool GetIsKycReminderRequired()
        {
            return Begin()
                .PostJson("Api/Customer/KycQuestionUpdate/GetCustomerStatus", new
                {
                    CustomerId = customerId
                })
                .ParseJsonAsAnonymousType(new { IsReminderRequired = (bool?)null })
                ?.IsReminderRequired ?? false;
        }
    }
}