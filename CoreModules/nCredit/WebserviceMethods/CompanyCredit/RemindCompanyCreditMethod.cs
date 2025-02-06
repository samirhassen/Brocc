using nCredit.DomainModel;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;

namespace nCredit.WebserviceMethods
{
    public class RemindCompanyCreditMethod : TypedWebserviceMethod<RemindCompanyCreditMethod.Request, RemindCompanyCreditMethod.Response>
    {
        public override string Path => "CompanyCredit/Remind";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Func<string, string> getSchedulerData = s => (request?.SchedulerData != null && request.SchedulerData.ContainsKey(s)) ? request.SchedulerData[s] : null;
            bool createReminders;
            bool createDeliveryFile;

            if (request.OnlyCreateReminders.HasValue && request.OnlyCreateDeliveryFile.HasValue)
            {
                return Error("onlyCreateReminders and onlyCreateDeliveryFile cannot be combined", httpStatusCode: 400, errorCode: "invalidInputCombination");
            }
            else if (request.OnlyCreateDeliveryFile.HasValue && request.OnlyCreateDeliveryFile.Value == true)
            {
                createReminders = false;
                createDeliveryFile = true;
            }
            else if (request.OnlyCreateReminders.HasValue && request.OnlyCreateReminders.Value == true)
            {
                createReminders = true;
                createDeliveryFile = false;
            }
            else
            {
                var skipDeliveryExport = getSchedulerData("skipDeliveryExport") == "true";
                createReminders = true;
                createDeliveryFile = !skipDeliveryExport;
            }

            var creditType = CreditType.CompanyLoan;
            var resolver = requestContext.Service();

            var useDelayedDocuments = getSchedulerData("useDelayedDocuments") == "true";
            using (var context = resolver.ContextFactory.CreateContext())
            {
                var m = resolver.CreateNewCreditRemindersBusinessEventManager(useDelayedDocuments);
                var status = m.GetStatus(context, creditType);

                if (status.NotificationCountInMonth == 0 && !(request.SkipRunOrderCheck ?? false))
                    return Error("Cannot send reminders before notifications. Override with skipRunOrderCheck", httpStatusCode: 400, errorCode: "reminderBeforeNotifications");

                if (status.NrOfRecentlyCreatedReminders > 0 && !(request.SkipRecentRemindersCheck ?? false))
                    return Error("Reminders have recently been created. If you really want to force creation again already use skipRecentRemindersCheck to override this check.", httpStatusCode: 400, errorCode: "recentRemindersExist");
            }

            return CreditContext.RunWithExclusiveLock("ntech.scheduledjobs.createcompanyreminders",
                    () =>
                    {
                        var r = resolver.CreateReminderService(useDelayedDocuments).CreateReminders(createReminders, createDeliveryFile, creditType);
                        return new Response
                        {
                            Errors = r.Errors,
                            NrOfRemindersCreated = r.NrOfRemindersCreated,
                            TotalMilliseconds = r.TotalMilliseconds,
                            Warnings = r.Warnings
                        };
                    },
                    () => Error("Job is already running", httpStatusCode: 400, errorCode: "alreadyRunning"));
        }

        public class Request
        {
            public bool? OnlyCreateReminders { get; set; }
            public bool? OnlyCreateDeliveryFile { get; set; }
            public bool? UseDelayedDocuments { get; set; }
            public bool? SkipRunOrderCheck { get; set; }
            public bool? SkipRecentRemindersCheck { get; set; }
            public IDictionary<string, string> SchedulerData { get; set; }
        }

        public class Response
        {
            public int NrOfRemindersCreated { get; set; }
            public List<string> Errors { get; set; }
            public long TotalMilliseconds { get; set; }
            public List<string> Warnings { get; set; }
        }
    }
}