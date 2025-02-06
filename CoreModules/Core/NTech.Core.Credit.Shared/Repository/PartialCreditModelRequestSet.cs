using System;
using System.Collections.Generic;

namespace nCredit.DbModel.Repository
{
    public class PartialCreditModelRequestSet
    {
        private PartialCreditModelRequestSet()
        {

        }
        public DateTime TransactionDate { get; set; }

        public ISet<DatedCreditDateCode> Dates { get; } = new HashSet<DatedCreditDateCode>();
        public ISet<DatedCreditValueCode> Values { get; } = new HashSet<DatedCreditValueCode>();
        public ISet<DatedCreditStringCode> Strings { get; } = new HashSet<DatedCreditStringCode>();

        public static PartialCreditModelRequestSet Create(DateTime transactionDate)
        {
            return new PartialCreditModelRequestSet
            {
                TransactionDate = transactionDate
            };
        }
    }
}