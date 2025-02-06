using nCredit;
using nCredit.DbModel.BusinessEvents;
using System;
using nCredit.DomainModel;
using nCredit.DbModel.DomainModel;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class CreditAttentionStatusService
    {
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICurrentNotificationStateService notificationStateService;
        private readonly ICreditEnvSettings envSettings;
        private readonly IClientConfigurationCore clientConfiguration;

        public CreditAttentionStatusService(CreditContextFactory creditContextFactory, ICurrentNotificationStateService notificationStateService, ICreditEnvSettings envSettings,
            IClientConfigurationCore clientConfiguration)
        {
            this.creditContextFactory = creditContextFactory;
            this.notificationStateService = notificationStateService;
            this.envSettings = envSettings;
            this.clientConfiguration = clientConfiguration;
        }

        public CreditAttentionStatusModel GetAttentionCreditAttentionStatus(string creditNr)
        {
            CreditAttentionStatusModel CreateStatus(bool isActive, bool? isOverdue, string text, string code, DateTime? statusDate)
                => new CreditAttentionStatusModel { IsActive = isOverdue, IsOverdue = isOverdue, Text = text, Code = code, StatusDate = statusDate };

            CreditAttentionStatusModel status = null;
            using (var context = creditContextFactory.CreateContext())
            {
                var model = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, envSettings);

                DateTime? GetActiveAlternatePaymentPlanStartDate()
                {
                    if (!clientConfiguration.IsFeatureEnabled("ntech.feature.paymentplan"))
                        return null;

                    return context.AlternatePaymentPlanHeadersQueryable.Where(x => x.CreditNr == creditNr && x.CancelledByEventId == null && x.FullyPaidByEventId == null).Max(x => (DateTime?)x.CreatedByEvent.TransactionDate);
                }

                DateTime? statusDate = null;
                var currentStatus = model.GetStatus(context.CoreClock.Today, d => statusDate = d);
                var statusString = currentStatus.ToString();

                switch (currentStatus)
                {
                    case CreditStatus.SentToDebtCollection:
                        {
                            status = CreateStatus(false, true, "Sent to debt collection", statusString, statusDate.Value);
                            break;
                        };
                    case CreditStatus.Settled:
                        {
                            status = CreateStatus(false, null, "Settled", statusString, statusDate.Value);
                            break;
                        };
                    case CreditStatus.WrittenOff:
                        {
                            status = CreateStatus(false, null, "Written off", statusString, statusDate.Value);
                            break;
                        };
                    case CreditStatus.Normal:
                        {
                            var activeTerminationLetterDateQueryBase = context
                                .CreditTerminationLetterHeadersQueryable
                                .Where(x => x.CreditNr == creditNr);
                            if (NewCreditTerminationLettersBusinessEventManager.HasTerminationLettersThatSuspendTheCreditProcess(clientConfiguration))
                            {
                                activeTerminationLetterDateQueryBase = activeTerminationLetterDateQueryBase
                                    .Where(x => x.InactivatedByBusinessEventId == null);
                            }
                            else
                            {
                                activeTerminationLetterDateQueryBase = activeTerminationLetterDateQueryBase
                                    .Where(x => !x.Credit.Notifications.Any(y => y.NotificationDate > x.DueDate));
                            }
                            var activeTerminationLetterDate = activeTerminationLetterDateQueryBase
                                .OrderByDescending(x => x.DueDate)
                                .Select(x => (DateTime?)x.TransactionDate)
                                .FirstOrDefault();


                            if (activeTerminationLetterDate.HasValue)
                            {
                                status = CreateStatus(true, true, "Termination letter sent", "TerminationLetterSent", activeTerminationLetterDate.Value);
                            }
                            else
                            {
                                var activeAlternatePaymentPlanStartDate = GetActiveAlternatePaymentPlanStartDate();
                                if (activeAlternatePaymentPlanStartDate.HasValue)
                                {
                                    status = CreateStatus(true, null, "Alternate payment plan active", "AlternatePaymentPlanActive", activeAlternatePaymentPlanStartDate);
                                }
                                else
                                {
                                    var oldestOpenN = notificationStateService
                                        .GetCurrentOpenNotificationsStateQuery(context)
                                        .Where(x => x.CreditNr == creditNr)
                                        .OrderBy(x => x.DueDate)
                                        .Select(x => new
                                        {
                                            x.DueDate,
                                            x.NrOfPassedDueDatesWithoutFullPaymentSinceNotification
                                        })
                                        .FirstOrDefault();

                                    if (oldestOpenN != null && oldestOpenN.NrOfPassedDueDatesWithoutFullPaymentSinceNotification >= 2)
                                    {
                                        status = CreateStatus(true, true, "Overdue", "Overdue", oldestOpenN.DueDate);
                                    }
                                    else
                                    {
                                        status = null;
                                    }
                                }

                            }
                        };
                        break;

                    default:
                        throw new NotImplementedException();
                }

            }

            return status;
        }
    }

    public class CreditAttentionStatusModel
    {
        public bool? IsActive { get; set; }
        public bool? IsOverdue { get; set; }
        public string Text { get; set; }
        public string Code { get; set; }
        public DateTime? StatusDate { get; set; }
    }


}
