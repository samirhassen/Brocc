using nCredit.Code.EInvoiceFi;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestsnPreCredit.Credit
{
    public class MockMatchingRepository : EInvoiceFiMessageHandler.IEInvoiceFiMessageMatchingRepository
    {
        HashSet<string> inactiveCreditNrs = new HashSet<string>();
        HashSet<string> existingExternalMessageIds = new HashSet<string>();
        Dictionary<string, List<string>> matchingCreditNrs = new Dictionary<string, List<string>>();

        public void AddInactiveCreditNrs(params string[] creditNrs)
        {
            creditNrs.ToList().ForEach(x => inactiveCreditNrs.Add(x));
        }

        public void AddExistingExternalMessageId(string externalMessageId)
        {
            existingExternalMessageIds.Add(externalMessageId);
        }

        public void AddCreditNrsMatchingEInvoiceIdentifier(string eInvoiceAddress, string eInvoiceBankCode, params string[] creditNrs)
        {
            matchingCreditNrs[$"eid#{eInvoiceAddress}#{eInvoiceBankCode}"] = creditNrs.ToList();
        }

        public void AddCreditNrsMatchingEmail(string email, params string[] creditNrs)
        {
            matchingCreditNrs[$"email#{email}"] = creditNrs.ToList();
        }

        public void AddCreditNrsMatchingOcr(string ocr, params string[] creditNrs)
        {
            matchingCreditNrs[$"ocr#{ocr}"] = creditNrs.ToList();
        }

        public List<string> FilterOutNonActiveCreditNrs(IList<string> creditNrs)
        {
            return creditNrs.Except(this.inactiveCreditNrs).ToList();
        }

        public List<string> GetCreditNrsUsingEInvoiceIdentifiers(string eInvoiceAddress, string eInvoiceBankCode)
        {
            return matchingCreditNrs.Opt($"eid#{eInvoiceAddress}#{eInvoiceBankCode}");
        }

        public List<string> GetCreditNrsUsingEmail(string email)
        {
            return matchingCreditNrs.Opt($"email#{email}");
        }

        public List<string> GetCreditNrsUsingOcr(string ocr)
        {
            return matchingCreditNrs.Opt($"ocr#{ocr}");
        }

        public bool IsDuplicateExternalMessageId(string externalMessageId)
        {
            return existingExternalMessageIds.Contains(externalMessageId);
        }
    }
}
