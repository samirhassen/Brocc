using NTech.Core.Customer.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;

namespace nCustomer.Code.Services.CustomerMessages
{
    public class CustomerMessageCommentDispatcher
    {
        private readonly CrossModuleClientFactory crossModuleClientFactory;

        public CustomerMessageCommentDispatcher(CrossModuleClientFactory crossModuleClientFactory)
        {
            this.crossModuleClientFactory = crossModuleClientFactory;
        }

        private static readonly ISet<string> CreditTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Credit_UnsecuredLoan", "Credit_MortgageLoan", "Credit_CompanyLoan"
        };

        private static readonly ISet<string> SavingsTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SavingsAccount_StandardAccount"
        };

        private static readonly ISet<string> PreCreditTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Application_UnsecuredLoan", "Application_MortgageLoan"
        };

        public void CreateComment(CustomerMessageModel m, bool wasEmailSent, bool isFailedToSend)
        {
            if (m == null)
                return;

            string message = GetCommentText(wasEmailSent, isFailedToSend, m.IsFromCustomer);
            string eventType = $"SecureMessage{(m.IsFromCustomer ? "From" : "To")}Customer";

            if (CreditTypes.Contains(m.ChannelType))
            {
                crossModuleClientFactory.CreditClient.CreateCreditCommentCore(m.ChannelId, message, eventType, null, null, m.Id);
            }
            else if (SavingsTypes.Contains(m.ChannelType))
            {
                crossModuleClientFactory.SavingsClient.CreateComment(m.ChannelId, message, eventType, true, null, null, m.Id);
            }
            else if (PreCreditTypes.Contains(m.ChannelType))
            {
                crossModuleClientFactory.PreCreditClient.AddCommentToApplication(m.ChannelId, message, m.Id);
            }
        }

        private string GetCommentText(bool wasEmailSent, bool isFailedToSend, bool isFromCustomer)
        {
            var isFailedToSendText = isFailedToSend ? ". Email notification could not be sent." : "";
            var isEmailSentText = wasEmailSent ? ". Email notification was sent." : isFailedToSendText;

            var message = $"Secure message {(isFromCustomer ? "from" : "to")} customer{isEmailSentText}";

            return message;
        }
    }
}