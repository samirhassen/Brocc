using nCredit.DbModel.BusinessEvents;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Services;
using System.Collections.Generic;
using System.Diagnostics;

namespace nCredit.Code.Services
{
    public class MortgageLoansUpdateChangeTermsService
    {
        private readonly MortgageLoansCreditTermsChangeBusinessEventManager mlChangeTermsManager;
        private readonly ILoggingService loggingService;
        private readonly INotificationProcessSettingsFactory notificationProcessSettingsFactory;
        private readonly ICustomerClient customerClient;
        private readonly ICreditEnvSettings envSettings;

        public MortgageLoansUpdateChangeTermsService(MortgageLoansCreditTermsChangeBusinessEventManager mlChangeTermsManager, ILoggingService loggingService,
            INotificationProcessSettingsFactory notificationProcessSettingsFactory, ICustomerClient customerClient, ICreditEnvSettings envSettings)
        {
            this.mlChangeTermsManager = mlChangeTermsManager;
            this.loggingService = loggingService;
            this.notificationProcessSettingsFactory = notificationProcessSettingsFactory;
            this.customerClient = customerClient;
            this.envSettings = envSettings;
        }

        public UpdateChangeTermsResult UpdateChangeTerms()
        {
            List<string> errors = new List<string>();
            int nrOfUpdatedChangeTerms = 0;
            var w = Stopwatch.StartNew();

            var p = notificationProcessSettingsFactory.GetByCreditType(CreditType.MortgageLoan);

            var customerPostalInfoRepository = new CustomerPostalInfoRepository(p.AllowMissingCustomerAddress, customerClient, mlChangeTermsManager.ClientCfg);

            var changes = mlChangeTermsManager.UpdateChangeTerms();
            nrOfUpdatedChangeTerms = changes.UpdatedPendingChange.Count + changes.UpdatedDefault.Count;

            foreach (var error in errors)
            {
                loggingService.Warning($"UpdateChangeTerms: {error}");
            }

            w.Stop();

            loggingService.Information($"UpdateChangeTerms finished, TotalMilliseconds={w.ElapsedMilliseconds}");

            //Used by nScheduler
            var warnings = new List<string>();
            errors?.ForEach(x => warnings.Add(x));

            return new UpdateChangeTermsResult
            {
                Errors = errors,
                TotalMilliseconds = w.ElapsedMilliseconds,
                Warnings = warnings,
                NrOfTermChangesDone = nrOfUpdatedChangeTerms
            };
        }
    }

    public class UpdateChangeTermsResult
    {
        public long TotalMilliseconds { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public int NrOfTermChangesDone { get; set; }
        public bool DeliveryFileCreated { get; set; }
    }
}