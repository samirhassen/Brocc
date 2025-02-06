using System;
using System.Collections.Generic;
using System.Linq;

namespace nTest.Controllers
{
    public class InvoiceModel
    {
        public string CreditNr { get; set; }
        public decimal UnpaidAmount { get; set; }
        public DateTime NotificationDate { get; set; }
        public DateTime DueDate { get; set; }
        public string OcrPaymentReference { get; set; }
        public string SharedOcrPaymentReference { get; set; }
        public string ExpectedPaymentOcrPaymentReference { get; set; }
        public string CoNotificationId { get; set; }
    }
}