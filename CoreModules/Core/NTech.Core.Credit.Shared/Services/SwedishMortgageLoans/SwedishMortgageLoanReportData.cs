using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.Credit.Shared.Services.SwedishMortgageLoans
{
    public static class SwedishMortgageLoanReportData
    {
        /// <summary>
        /// Used for testing
        /// </summary>
        public static int? OverrideGraceDays { get; set; }

        public static int GetGraceDays(IClientConfigurationCore clientConfiguration) =>
            OverrideGraceDays ?? clientConfiguration.GetSingleCustomInt(false, "NotificationProcessSettings", "NotificationOverDueGraceDays") ?? 5;
    }   
}
