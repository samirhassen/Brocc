using nCredit.Code;
using nCredit.DbModel.Repository;
using NTech;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Linq;

namespace nCredit.Controllers.Api.DataWarehouse
{
    public class MergeCreditNotificationBalanceSnapshotTask : DatawarehouseMergeTask
    {
        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled || NEnv.IsCompanyLoansEnabled;

        public override void Merge(INTechCurrentUserMetadata currentUser, IClock clock)
        {
            int? maxBusinessEventId = null;
            using (var context = new CreditContext())
            {
                maxBusinessEventId = context.BusinessEvents.Max(x => (int?)x.Id); // Used to prevent running endlessly if events keep trickling in while running
            }

            if (!maxBusinessEventId.HasValue)
                return;

            var paymentOrder = CreatePaymentOrderService(currentUser, clock).GetPaymentOrderItems();

            Func<bool> runBlock = () =>
            {
                var client = new DataWarehouseClient();

                using (var context = new CreditContextExtended(currentUser, clock))
                {
                    var repo = new SystemItemRepository(currentUser.UserId, currentUser.InformationMetadata);
                    var startAfterBusinessEventId = repo.GetInt(SystemItemCode.Dw_LatestMergedBusinessEventId_CreditNotificationBalanceSnapshot, context) ?? 0;
                    var items = context
                        .Transactions
                        .Where(x => x.BusinessEventId > startAfterBusinessEventId && x.BusinessEventId <= maxBusinessEventId.Value && x.CreditNotificationId.HasValue)
                        .GroupBy(x => new
                        {
                            x.CreditNotificationId,
                            x.TransactionDate,
                            x.BusinessEventId
                        })
                        .OrderBy(x => x.Key.BusinessEventId)
                        .Take(100)
                        .Select(x => x.Key)
                        .ToList();

                    if (items.Count == 0)
                        return false;

                    var notifications = DomainModel.CreditNotificationDomainModel.CreateForNotifications(items.Select(x => x.CreditNotificationId.Value).Distinct().ToList(), 
                        context, paymentOrder);

                    var facts = items.GroupBy(x => new { x.CreditNotificationId, x.TransactionDate }).Select(x => x.Key).Select(n =>
                    {
                        var notification = notifications[n.CreditNotificationId.Value];
                        var totalBalance = notification.GetRemainingBalance(n.TransactionDate);
                        var interestBalance = notification.GetRemainingBalance(n.TransactionDate, DomainModel.CreditDomainModel.AmountType.Interest);
                        var capitalBalance = notification.GetRemainingBalance(n.TransactionDate, DomainModel.CreditDomainModel.AmountType.Capital);

                        return new
                        {
                            NotificationId = n.CreditNotificationId.Value,
                            CreditNr = notification.CreditNr,
                            TransactionDate = n.TransactionDate,
                            TotalBalance = totalBalance,
                            InterestBalance = interestBalance,
                            CapitalBalance = capitalBalance,
                            NonInterestAndCapitalBalance = totalBalance - interestBalance - capitalBalance,
                            DwUpdatedDate = DateTime.Now
                        };
                    }).ToList();

                    client.MergeFact("CreditNotificationBalanceSnapshot", facts);

                    repo.SetInt(SystemItemCode.Dw_LatestMergedBusinessEventId_CreditNotificationBalanceSnapshot, items.Last().BusinessEventId, context);

                    context.SaveChanges();
                }
                return true;
            };

            int guard = 0;
            while (runBlock())
            {
                if (guard++ > 10000) throw new Exception("Hit guard");
            }
        }
    }
}