using NTech.Core.Module;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;

namespace nCredit.Code.Services
{
    public class ScheduledTasksService
    {
        private readonly INTechServiceRegistry serviceRegistry;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly ICreditEnvSettings envSettings;

        public ScheduledTasksService(INTechServiceRegistry serviceRegistry, IClientConfigurationCore clientConfiguration,
            ICreditEnvSettings envSettings)
        {
            this.serviceRegistry = serviceRegistry;
            this.clientConfiguration = clientConfiguration;
            this.envSettings = envSettings;
        }

        public List<ScheduledTaskMenuItem> GetScheduledTaskMenuItems(NTechNavigationTarget backTarget)
        {
            var clientCfg = clientConfiguration;
            var items = new List<ScheduledTaskMenuItem>();
            void Add(string taskName, string taskDisplayName, string url, bool isEnabled)
            {
                items.Add(new ScheduledTaskMenuItem { TaskName = taskName, TaskDisplayName = taskDisplayName, AbsoluteUrl = url, IsEnabled = isEnabled });
            }

            string GetNewBackOfficeUrl(string relativeUrl) =>
                serviceRegistry.InternalServiceUrl(
                    "nBackOffice", relativeUrl,
                    Tuple.Create("backTarget", backTarget?.GetBackTargetOrNull())).ToString();

            bool HasFeature(string name) => clientCfg.IsFeatureEnabled(name);

            Add("Notifications", "Notifications", GetNewBackOfficeUrl("s/scheduled-tasks/notifications"), true);
            Add("Reminders", "Reminders", GetNewBackOfficeUrl("s/scheduled-tasks/reminders"), true);
            Add("TerminationLetters", "Termination letters", GetNewBackOfficeUrl("s/scheduled-tasks/termination-letters"), true);
            Add("DebtCollection", "Debt collection", GetNewBackOfficeUrl("s/default-management/debt-collection-task"), true);
            Add("SatExport", "SAT export", GetNewBackOfficeUrl("s/scheduled-tasks/sat-export"), envSettings.IsUnsecuredLoansEnabled && clientCfg.Country.BaseCountry == "FI");
            Add("TrapetsAmlExport", "Trapets AML export", GetNewBackOfficeUrl("s/scheduled-tasks/trapets-aml-export"), clientCfg.IsFeatureEnabled("ntech.feature.trapetsaml.v1") && envSettings.IsUnsecuredLoansEnabled);
            Add("Cm1AmlExport", "Cm1 AML export", GetNewBackOfficeUrl("s/scheduled-tasks/cm1-aml-export"), clientCfg.IsFeatureEnabled("ntech.feature.Cm1aml.v1") && (envSettings.IsUnsecuredLoansEnabled || envSettings.IsCompanyLoansEnabled));
            Add("TreasuryAmlExport", "Treasury AML export", GetNewBackOfficeUrl("s/scheduled-tasks/treasury-aml-export"), clientCfg.IsFeatureEnabled("ntech.feature.Treasuryaml.v1"));
            Add("BookkeepingFiles", "Bookkeeping files", GetNewBackOfficeUrl("s/scheduled-tasks/book-keeping"), true);
            Add("DailyKycScreen", "KYC screening", GetNewBackOfficeUrl("s/scheduled-tasks/daily-kyc-screen"), clientCfg.IsFeatureEnabled("ntech.feature.kycbatchscreening"));
            Add("CreditAnnualStatements", "Annual statements", GetNewBackOfficeUrl("s/scheduled-tasks/annual-statements"), LoanStandardAnnualSummaryService.IsAnnualStatementFeatureEnabled(clientConfiguration, envSettings));
            Add("PositiveCreditRegister", "PCR export", GetNewBackOfficeUrl("s/positive-credit-register/main"),
                HasFeature("ntech.feature.unsecuredloans") && HasFeature("ntech.feature.positivecreditregister") && !HasFeature("ntech.feature.unsecuredloans.standard") && clientCfg.Country.BaseCountry == "FI");

            return items;
        }

        public class ScheduledTaskMenuItem
        {
            public string TaskDisplayName { get; set; }
            public bool IsEnabled { get; set; }
            public string TaskName { get; set; }
            public string AbsoluteUrl { get; set; }
        }
    }
}