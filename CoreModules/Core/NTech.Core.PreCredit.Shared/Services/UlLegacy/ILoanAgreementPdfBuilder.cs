using System;

namespace nPreCredit.Code
{
    public interface ILoanAgreementPdfBuilder
    {
        bool IsCreateAgreementPdfAllowed(string applicationNr, out string reasonMessage);
        bool TryCreateAgreementPdf(out byte[] pdfBytes, out string errorMessage, string applicationNr, bool skipAllowedCheck = false, Action<string> observeAgreementDataHash = null);
    }
}