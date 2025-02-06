using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nCredit.DbModel.Model
{
    public class OutgoingExportFileHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string FileType { get; set; }
        public DateTime TransactionDate { get; set; }
        public string FileArchiveKey { get; set; }
        public string ExportResultStatus { get; set; }
        public string CustomData { get; set; }
        public virtual List<CreditAnnualStatementHeader> AnnualStatements { get; set; }

        public class ExportResultStatusStandardModel
        {
            public string status { get; set; }
            public List<string> errors { get; set; }
            public List<string> warnings { get; set; }
            public int? deliveryTimeInMs { get; set; }
            public string deliveredToProfileName { get; set; }
            public string providerName { get; set; }
            public List<string> deliveredToProfileNames { get; set; }
            public List<string> failedProfileNames { get; set; }
        }
    }
}