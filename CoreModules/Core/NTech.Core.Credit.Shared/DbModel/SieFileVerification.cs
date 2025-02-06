using nCredit;
using System;
using System.Collections.Generic;

namespace NTech.Core.Credit.Shared.DbModel
{
    public class SieFileVerification
    {
        public int Id { get; set; }
        public string Text { get; set; }
        /// <summary>
        /// BookkeepingDate
        /// </summary>
        public DateTime Date { get; set; }
        /// <summary>
        /// TransactionDate
        /// </summary>
        public DateTime RegistrationDate { get; set; }
        public int? OutgoingBookkeepingFileHeaderId { get; set; }
        public OutgoingBookkeepingFileHeader OutgoingFile { get; set; }
        public virtual List<SieFileTransaction> Transactions { get; set; }
        public virtual List<SieFileConnection> Connections { get; set; }
    }
}
