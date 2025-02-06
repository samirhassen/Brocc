namespace nCredit
{
    public enum EInvoiceFiItemCode
    {
        CustomerName,
        CustomerAddressStreet,
        CustomerAddressZipcode,
        CustomerAddressArea,
        CustomerLanguageCode,
        LastInvoicePaidOcr,
        CustomerIdentification1,
        CustomerIdentification2,
        EInvoiceAddress,
        EInvoiceBankCode,
        SourceFileArchiveKey
    }

    public class EInvoiceFiMessageItem
    {
        public int EInvoiceFiMessageHeaderId { get; set; }
        public EInvoiceFiMessageHeader Message { get; set; }

        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsEncrypted { get; set; }
    }
}