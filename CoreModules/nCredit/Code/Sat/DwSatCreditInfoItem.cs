using System;

namespace nCredit.Controllers
{
    internal class DwSatCreditInfoItem
    {
        public string CreditNr { get; set; }
        public int? CustomerId { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public decimal? IncomePerMonth { get; set; }
    }
}