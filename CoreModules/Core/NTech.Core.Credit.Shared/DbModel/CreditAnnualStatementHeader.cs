using nCredit.DbModel.Model;
using NTech.Core.Module.Shared.Database;

namespace nCredit
{
    public class CreditAnnualStatementHeader : InfrastructureBaseItem
    {
        public string CreditNr { get; set; }
        public CreditHeader Credit { get; set; }
        public int CustomerId { get; set; }
        public int Year { get; set; }
        public string StatementDocumentArchiveKey { get; set; }
        public int? OutgoingExportFileHeaderId { get; set; }
        public OutgoingExportFileHeader OutgoingExportFile { get; set; }
        public string CustomData { get; set; }
        public static string ExportFileType = "CreditAnnualStatements";
    }
}