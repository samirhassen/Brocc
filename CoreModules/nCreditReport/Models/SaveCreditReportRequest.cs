using System;
using System.Collections.Generic;

namespace nCreditReport.Models
{
    public class SaveCreditReportRequest
    {
        public SaveCreditReportRequest()
        {

        }
        public DateTimeOffset RequestDate { get; set; }
        public string CreditReportProviderName { get; set; }
        public string InformationMetaData { get; set; }
        public int ChangedById { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public class Item
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
        public List<Item> Items { get; set; }
        public List<Item> SearchTerms { get; set; }
    }
}