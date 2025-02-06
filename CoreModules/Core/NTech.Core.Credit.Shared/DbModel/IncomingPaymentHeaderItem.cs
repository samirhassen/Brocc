using NTech.Core.Module.Shared.Database;

namespace nCredit
{
    public enum IncomingPaymentHeaderItemCode
    {
        NoteText,
        OcrReference,
        ExternalId,
        ClientAccountIban,
        CustomerName,
        CustomerAddressCountry,
        CustomerAddressStreetName,
        CustomerAddressBuildingNumber,
        CustomerAddressPostalCode,
        CustomerAddressTownName,
        CustomerAddressLines,
        NotAutoPlacedReasonMessage,
        IsManualPayment,
        AutogiroPayerNumber,
        InitiatedByUserId,
        CustomerOrgnr
    }

    public class IncomingPaymentHeaderItem : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public IncomingPaymentHeader Payment { get; set; }
        public int IncomingPaymentHeaderId { get; set; }
        public string Name { get; set; }
        public bool IsEncrypted { get; set; }
        public string Value { get; set; }
    }
}