using NTech.Core.Module.Shared.Database;
using System;

namespace nPreCredit.DbModel
{
    public class ApplicationReportCasheRow : InfrastructureBaseItem
    {
        public string ApplicationNr { get; set; }
        public DateTime ApplicationDate { get; set; }
        public string Overrided { get; set; }
        public string Handler { get; set; }
        public string SysRecomendation { get; set; }
        public decimal? SysRecomendationMaxAmount { get; set; }
        public decimal? SysRecomendationInterestRate { get; set; }
        public decimal? SysRecomendationomendationAmount { get; set; }
        public int? SysRecomendationomendationRepaymentTime { get; set; }
        public decimal? SysRecomendationNotificationFee { get; set; }
        public string SysRecomendationRejectionReasons { get; set; }
        public string Decision { get; set; }
        public decimal? DecisionInterestRate { get; set; }
        public decimal? DecisionAmount { get; set; }
        public int? DecisionRepaymentTime { get; set; }
        public decimal? DecisionNotificationFee { get; set; }
        public string DecisionRejectionReasons { get; set; }
    }
}