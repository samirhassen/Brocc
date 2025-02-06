using System;
using System.Collections.Generic;
using System.Text;

namespace NTech.Core.Credit.Shared.DomainModel
{
    public class PositiveCreditRegisterSettingsModel
    {
        public bool IsMock { get; set; }
        public bool IsLogRequestToFileEnabled { get; set; }
        public bool IsLogResponseToFileEnabled { get; set; }
        public bool IsLogBatchStatusToFileEnabled { get; set; }
        public string LogFilePath { get; set; }
        public string AddLoansEndpointUrl { get; set; }
        public string ChangeLoansEndpointUrl { get; set; }
        public string RepaymentsEndpointUrl { get; set; }
        public string DelayedRepaymentsEndpointUrl { get; set; }
        public string TerminatedLoansEndpointUrl { get; set; }
        public string CheckBatchStatusEndpointUrl { get; set; }
        public string GetLoanEndpointUrl { get; set; }
        public string CertificateThumbPrint { get; set; }
        public bool ForceFirstTimeExportToTriggerLoanChanges { get; set; }
        public string LenderMarketingName { get; set; }
        public string OwnerIdCode { get; set; }
        public bool IsTargetProduction { get; set; }
        public string MockPcrBatchStatusFailureCode { get; set; }
        public string BatchFailedReportEmail { get; set; }
        public bool UsePcrTestCivicRegNrs { get; set; }
        /// <summary>
        /// PCRs test enviroment cannot be reset so we need to be able to "renumber" our loans to test more than once.
        /// </summary>
        public string CreditNrTestSuffix { get; set; }
    }
}
