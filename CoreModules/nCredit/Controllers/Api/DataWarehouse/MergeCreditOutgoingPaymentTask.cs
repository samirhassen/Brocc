using nCredit.Code;
using nCredit.DbModel.Repository;
using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Linq;

namespace nCredit.Controllers.Api.DataWarehouse
{
    public class MergeCreditOutgoingPaymentTask : DatawarehouseMergeTask
    {
        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled;

        public override void Merge(INTechCurrentUserMetadata currentUser, IClock clock)
        {
            RepeatWithGuard(() => MergeI(currentUser));
        }

        private bool MergeI(INTechCurrentUserMetadata currentUser)
        {
            var repo = new SystemItemRepository(currentUser.UserId, currentUser.InformationMetadata);

            using (var context = new CreditContext())
            {
                var v = repo.Get(SystemItemCode.DwLatestPaymentFileBusinessEventId_Fact_CreditOutgoingPayment, context);
                int? lastHandledPaymentFileBusinessEventId = v == null ? new int?() : int.Parse(v);

                var basis = context
                    .OutgoingPaymentFileHeaders
                    .Select(x => new
                    {
                        PaymentFileBusinessEventId = x.CreatedByBusinessEventId,
                        Payments = x.Payments.Select(y => new CreditOutgoingPaymentPaymentModel
                        {
                            PaymentId = y.Id,
                            SourceBusinessEventId = y.CreatedByBusinessEventId,
                            SourceBusinessEventType = y.CreatedByEvent.EventType,
                            SourceTransactionDate = y.CreatedByEvent.TransactionDate,
                            PaymentAmount = -(y
                                .Transactions
                                .Where(z => z.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString() && z.BusinessEventId == x.CreatedByBusinessEventId)
                                .Sum(z => (decimal?)z.Amount) ?? 0m),
                            CreditNr = y
                                .Transactions
                                .Where(z => z.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString())
                                .Select(z => z.CreditNr)
                                .FirstOrDefault(),
                            PaymentFileBusinessEventId = x.CreatedByBusinessEventId,
                            PaymentFileTransactionDate = x.CreatedByEvent.TransactionDate
                        })
                    });

                if (lastHandledPaymentFileBusinessEventId.HasValue)
                    basis = basis.Where(x => x.PaymentFileBusinessEventId > lastHandledPaymentFileBusinessEventId.Value);

                var files = basis.OrderBy(x => x.PaymentFileBusinessEventId).Take(20).ToList();
                if (files.Count == 0)
                    return false;

                var newLastHandledPaymentFileBusinessEventId = files.Max(x => x.PaymentFileBusinessEventId);

                var payments = files.SelectMany(x => x.Payments).Where(x => x.CreditNr != null).ToList();

                if (payments.Count > 0)
                {
                    var client = new DataWarehouseClient();
                    client.MergeFact("CreditOutgoingPayment", payments);
                }

                repo.Set(SystemItemCode.DwLatestPaymentFileBusinessEventId_Fact_CreditOutgoingPayment, newLastHandledPaymentFileBusinessEventId.ToString(), context);

                context.SaveChanges();

                return true;
            }
        }

        private class CreditOutgoingPaymentPaymentModel
        {
            public int PaymentId { get; set; }
            public string CreditNr { get; set; }
            public string SourceBusinessEventType { get; set; }
            public int SourceBusinessEventId { get; set; }
            public DateTime SourceTransactionDate { get; set; }
            public int PaymentFileBusinessEventId { get; set; }
            public DateTime PaymentFileTransactionDate { get; set; }
            public decimal PaymentAmount { get; set; }
        }
    }
}