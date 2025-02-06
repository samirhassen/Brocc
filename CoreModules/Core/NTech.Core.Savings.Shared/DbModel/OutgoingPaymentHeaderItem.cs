using NTech.Core.Module.Shared.Database;

namespace nSavings
{
    public enum OutgoingPaymentHeaderItemCode
    {
        FromIban,
        ToIban,
        CustomerName,
        CustomerMessage, //Message that is sent in the outgoing payment file
        SavingsAccountNr,
        CustomTransactionMessage, //Message stored locally to remember why this was done
        RequestIpAddress,
        RequestAuthenticationMethod,
        RequestDate,
        RequestedByCustomerId, //When initiated by the customer
        RequestedByHandlerUserId //When initiated by the handler
    }

    public class OutgoingPaymentHeaderItem : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public OutgoingPaymentHeader OutgoingPayment { get; set; }
        public int OutgoingPaymentId { get; set; }
        public string Name { get; set; }
        public bool IsEncrypted { get; set; }
        public string Value { get; set; }
    }
}