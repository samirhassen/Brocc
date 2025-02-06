using NTech.Core.Module.Shared.Database;

namespace nCredit
{
    public enum OutgoingPaymentHeaderItemCode
    {
        FromIban,
        ToIban,
        ToBankAccountNr,
        CustomerName,
        CustomerMessage,
        ApplicationNr,
        ProviderApplicationId,
        ApplicationProviderName,
        FromBankAccountNr,
        ToBankAccountNrType,
        CreditNr,
        PaymentReference
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