using nCredit.DbModel.BusinessEvents;
using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Se;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;

namespace nCredit.DbModel
{
    public class DirectDebitOnCreditCreationBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        public DirectDebitOnCreditCreationBusinessEventManager(INTechCurrentUserMetadata currentUser, ICoreClock coreClock, IClientConfigurationCore clientConfiguration) : base(currentUser, coreClock, clientConfiguration)
        {

        }

        public bool TryScheduleDirectDebitActivation(ICreditContextExtended context, string creditNr, BankAccountNumberSe bankAccountNr, string paymentNr, int? customerId, IBankAccountNumber clientBankGiroNr, out string failedMessage, BusinessEvent evt = null)
        {
            if (clientBankGiroNr.AccountType != BankAccountNumberTypeCode.BankGiroSe)
                throw new Exception("Incoming payment account must be a swedish bankgiro account when using direct debit");

            return TryScheduleDirectDebitOperation(context, creditNr, OutgoingDirectDebitFileOperation.Activation, bankAccountNr, paymentNr, customerId, (BankGiroNumberSe)clientBankGiroNr, out failedMessage, evt: evt);
        }

        protected bool TryScheduleDirectDebitOperation(ICreditContextExtended context, string creditNr, OutgoingDirectDebitFileOperation operation, BankAccountNumberSe bankAccountNr, string paymentNr, int? customerId, BankGiroNumberSe clientBankGiroNr, out string failedMessage, BusinessEvent evt = null)
        {
            if (string.IsNullOrWhiteSpace(creditNr))
            {
                failedMessage = "creditNr missing";
                return false;
            }

            if (clientBankGiroNr == null)
            {
                failedMessage = "clientBankGiroNr missing";
                return false;
            }

            if (paymentNr == null)
            {
                failedMessage = "paymentNr missing";
                return false;
            }

            if (operation == OutgoingDirectDebitFileOperation.Activation)
            {
                if (bankAccountNr == null)
                {
                    failedMessage = "bankAccountNr missing";
                    return false;
                }
                if (customerId < 1)
                {
                    failedMessage = "customerId missing";
                    return false;
                }
            }

            evt = evt ?? AddBusinessEvent(BusinessEventType.ScheduledOutgoingDirectDebitChange, context);

            context.AddCreditOutgoingDirectDebitItem(FillInInfrastructureFields(new CreditOutgoingDirectDebitItem
            {
                Operation = operation.ToString(),
                CreatedByEvent = evt,
                CreditNr = creditNr,
                BankAccountNr = bankAccountNr?.PaymentFileFormattedNr,
                BankAccountOwnerCustomerId = customerId,
                ClientBankGiroNr = clientBankGiroNr?.NormalizedValue,
                PaymentNr = paymentNr
            }));

            failedMessage = null;
            return true;
        }
    }

    public enum OutgoingDirectDebitFileOperation
    {
        Activation,
        Cancellation
    }
}