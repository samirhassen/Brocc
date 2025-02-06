using nCredit.DbModel.BusinessEvents;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace nCredit.Code.Services
{
    public class ReminderService
    {
        private readonly IDocumentClient documentClient;
        private readonly NewCreditRemindersBusinessEventManager reminderManager;
        private readonly ILoggingService loggingService;
        private readonly INotificationProcessSettingsFactory notificationProcessSettingsFactory;
        private readonly ICustomerClient customerClient;
        private readonly ICreditEnvSettings envSettings;

        public ReminderService(IDocumentClient documentClient, NewCreditRemindersBusinessEventManager reminderManager, ILoggingService loggingService,
            INotificationProcessSettingsFactory notificationProcessSettingsFactory, ICustomerClient customerClient, ICreditEnvSettings envSettings)
        {
            this.documentClient = documentClient;
            this.reminderManager = reminderManager;
            this.loggingService = loggingService;
            this.notificationProcessSettingsFactory = notificationProcessSettingsFactory;
            this.customerClient = customerClient;
            this.envSettings = envSettings;
        }

        public CreateRemindersResult CreateReminders(bool createReminders, bool createDeliverFile, CreditType creditType, HashSet<string> onlyTheseCreditNrs = null)
        {
            List<string> errors = new List<string>();
            var deliveryFileCreated = false;
            int nrOfRemindersCreated = 0;
            var w = Stopwatch.StartNew();
            Dictionary<string, int> reminderCountByCreditNr = null;

            var p = notificationProcessSettingsFactory.GetByCreditType(creditType);

            var customerPostalInfoRepository = new CustomerPostalInfoRepository(p.AllowMissingCustomerAddress, customerClient, reminderManager.ClientCfg);

            if (createReminders)
            {
                reminderCountByCreditNr = reminderManager.CreateReminders(customerPostalInfoRepository, creditType, onlyTheseCreditNrs: onlyTheseCreditNrs);
                nrOfRemindersCreated = reminderCountByCreditNr.Values.Sum();
            }

            if (createDeliverFile)
            {
                var result = reminderManager.CreateDeliveryExport(errors, documentClient, customerPostalInfoRepository, creditType);
                if (result != null)
                    deliveryFileCreated = true;
            }

            foreach (var error in errors)
            {
                loggingService.Warning($"CreditReminders: {error}");
            }

            w.Stop();

            loggingService.Information($"CreditReminders finished, TotalMilliseconds={w.ElapsedMilliseconds}");

            //Used by nScheduler
            var warnings = new List<string>();
            errors?.ForEach(x => warnings.Add(x));
            if (nrOfRemindersCreated == 0 && !deliveryFileCreated && !envSettings.HasPerLoanDueDay)
                warnings.Add("No reminders created or delivered");

            return new CreateRemindersResult
            {
                Errors = errors,
                TotalMilliseconds = w.ElapsedMilliseconds,
                Warnings = warnings,
                NrOfRemindersCreated = nrOfRemindersCreated,
                DeliveryFileCreated = deliveryFileCreated,
                ReminderCountByCreditNr = reminderCountByCreditNr ?? new Dictionary<string, int>()
            };
        }
    }

    public class CreateRemindersResult
    {
        public long TotalMilliseconds { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public int NrOfRemindersCreated { get; set; }
        public bool DeliveryFileCreated { get; set; }
        public Dictionary<string, int> ReminderCountByCreditNr { get; set; }
        public int GetReminderCount(string creditNr) => ReminderCountByCreditNr.OptS(creditNr) ?? 0;
    }
}