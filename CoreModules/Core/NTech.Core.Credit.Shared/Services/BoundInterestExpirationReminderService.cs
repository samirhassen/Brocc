using nCredit;
using nCredit.DbModel.BusinessEvents;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class BoundInterestExpirationReminderService : BusinessEventManagerOrServiceBase
    {
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICustomerClient customerClient;
        private readonly ICreditEnvSettings envSettings;
        private readonly CachedSettingsService settingsService;

        public BoundInterestExpirationReminderService(CreditContextFactory creditContextFactory, INTechCurrentUserMetadata currentUser, ICoreClock clock,
            IClientConfigurationCore clientConfiguration, ICustomerClient customerClient, ICreditEnvSettings envSettings, CachedSettingsService settingsService) : base(currentUser, clock, clientConfiguration)
        {
            this.creditContextFactory = creditContextFactory;
            this.customerClient = customerClient;
            this.envSettings = envSettings;
            this.settingsService = settingsService;
        }

        private void SendReminderMessage(string creditNr, string messageTemplateText, int businessEventId)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                AddDatedCreditDate(DatedCreditDateCode.LastMlRebindingReminderMessageDate, context.CoreClock.Today, null, context, creditNr: creditNr, businessEventId: businessEventId);
                var customerId = context.CreditCustomersQueryable.Where(x => x.CreditNr == creditNr && x.ApplicantNr == 1).Select(x => x.CustomerId).Single();
                customerClient.SendSecureMessage(customerId, creditNr, envSettings.ClientCreditType.ToString(), messageTemplateText, true, "markdown");

                context.SaveChanges();
            }
        }

        private HashSet<string> GetUnremindedCreditNrsWhereBindingExpiresInAtMostNDays(int maxExpirationDays)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var fallbackMonthCount = (decimal)MortgageLoanFixedInterestChangeEventManager.FallbackMonthCount;
                var today = context.CoreClock.Today;
                var minRebindDateToRemind = today.AddDays(maxExpirationDays);

                return GetRebindModels(context)
                .Where(x =>
                    x.Status == CreditStatus.Normal.ToString()
                    && x.MortgageLoanInterestRebindMonthCount > fallbackMonthCount
                    && x.MortgageLoanNextInterestRebindDate <= minRebindDateToRemind
                    && !x.HasBeenRemindedThisRebind)
                .Select(x => x.CreditNr)
                .ToHashSetShared();
            }
        }

        public int SendReminderMessages()
        {
            var settings = settingsService.LoadSettings("mlBindingExpirationSecureMessage");
            if (settings["isEnabled"] != "true")
                return 0;

            var templateText = settings["templateText"];

            var creditNrs = GetUnremindedCreditNrsWhereBindingExpiresInAtMostNDays(30);
            if (creditNrs.Count == 0)
            {
                return 0;
            }
            int businessEventId;
            using (var context = creditContextFactory.CreateContext())
            {
                var evt = AddBusinessEvent(BusinessEventType.MlRebindingReminderMessage, context);
                context.SaveChanges();
                businessEventId = evt.Id;
            }
            foreach (var creditNr in creditNrs)
            {
                SendReminderMessage(creditNr, templateText, businessEventId);
            }
            return creditNrs.Count;
        }

        public bool HasBeenRemindedThisRebind(string creditNr)
        {
            using (var context = creditContextFactory.CreateContext())
            {
                return GetRebindModels(context).Any(x => x.CreditNr == creditNr && x.HasBeenRemindedThisRebind);
            }
        }

        private IQueryable<RebindModel> GetRebindModels(ICreditContextExtended context) => context.CreditHeadersQueryable.Select(x => new
        {
            x.CreditNr,
            x.Status,
            MortgageLoanNextInterestRebindItem = x
                        .DatedCreditDates
                        .Where(y => y.Name == DatedCreditDateCode.MortgageLoanNextInterestRebindDate.ToString())
                        .OrderByDescending(y => y.Id)
                        .FirstOrDefault(),
            MortgageLoanInterestRebindMonthCount = x
                        .DatedCreditValues
                        .Where(y => y.Name == DatedCreditValueCode.MortgageLoanInterestRebindMonthCount.ToString())
                        .OrderByDescending(y => (decimal?)y.Id)
                        .Select(y => y.Value)
                        .FirstOrDefault(),
            x.DatedCreditDates
        })
        .Select(x => new RebindModel
        {
            CreditNr = x.CreditNr,
            Status = x.Status,
            MortgageLoanNextInterestRebindDate = (DateTime?)x.MortgageLoanNextInterestRebindItem.Value,
            MortgageLoanInterestRebindMonthCount = x.MortgageLoanInterestRebindMonthCount,
            HasBeenRemindedThisRebind = x
                .DatedCreditDates
                .Any(y => y.Name == DatedCreditDateCode.LastMlRebindingReminderMessageDate.ToString() && y.BusinessEvent.Id > x.MortgageLoanNextInterestRebindItem.Id)
        });


        private class RebindModel
        {
            public string CreditNr { get; set; }
            public string Status { get; set; }
            public DateTime? MortgageLoanNextInterestRebindDate { get; set; }
            public decimal MortgageLoanInterestRebindMonthCount { get; set; }
            public bool HasBeenRemindedThisRebind { get; set; }
        }
    }
}
