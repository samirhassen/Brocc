using System;

namespace nCredit.Code.Services
{
    public class FetchPaymentsOutgoingPaymentModel
    {
        public int Id { get; set; }
        public string EventTypePaymentSource { get; set; }
        public string CreditNr { get; set; }
        public string CreditProviderName { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime BookKeepingDate { get; set; }
        public string ProviderApplicationId { get; set; }
        public string ApplicationNr { get; set; }
        public decimal PaidToCustomerAmount { get; set; }
    }
}