using System;

namespace nDataWarehouse
{
    public class ExportedReport
    {
        public int Id { get; set; }
        public string ReportName { get; set; }
        public DateTime ReportDate { get; set; }
        public long? GenerationTimeInMs { get; set; }
        public string ReportArchiveKey { get; set; }
    }
}