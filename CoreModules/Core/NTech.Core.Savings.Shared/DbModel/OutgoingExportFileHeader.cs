using NTech.Core.Module.Shared.Database;
using System;
using System.Collections.Generic;

namespace nSavings
{
    public class OutgoingExportFileHeader : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public string FileType { get; set; }
        public DateTime TransactionDate { get; set; }
        public string FileArchiveKey { get; set; }
        public string ExportResultStatus { get; set; }
        public string CustomData { get; set; }

        public class StandardExportResultStatusModel
        {
            public string Status { get; set; }
            public List<string> SuccessProfileNames { get; set; }
            public List<string> FailedProfileNames { get; set; }
            public int? TimeInMs { get; set; }
            public List<string> Errors { get; set; }
            public List<string> Warnings { get; set; }
            public enum StatusCode
            {
                NoExportProfile,
                Warning,
                Error,
                Ok
            }
        }
    }
}