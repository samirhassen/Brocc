using NTech.Banking.CivicRegNumbers;
using System;

namespace nCustomerPages.Code.ElectronicIdSignature
{
    public interface ISavingsAgreementElectronicIdSignatureProvider
    {
        string StartSignatureSessionReturningSignatureUrl(string tempDataKey, byte[] pdfBytes, ICivicRegNumber civicRegNr, string documentDisplayName, string firstName, string lastName, string userLanguage);
        SavingsAgreementElectronicIdSignatureResult HandleSignatureCallback(Func<string, string> getRequestParameter);
    }
}
