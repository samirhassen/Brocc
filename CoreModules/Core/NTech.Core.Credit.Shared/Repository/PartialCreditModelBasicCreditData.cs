using System;
using System.Collections.Generic;

namespace nCredit.DbModel.Repository
{
    public class PartialCreditModelBasicCreditData
    {
        public string CreditNr { get; set; }
        public IEnumerable<ValueItem<decimal?>> Values { get; set; }
        public IEnumerable<ValueItem<string>> Strings { get; set; }
        public IEnumerable<ValueItem<DateTime?>> Dates { get; set; }

        public class ValueItem<TValue>
        {
            public string Name { get; set; }
            public DateTime TransactionDate { get; set; }
            public TValue Value { get; set; }
        }
    }
}