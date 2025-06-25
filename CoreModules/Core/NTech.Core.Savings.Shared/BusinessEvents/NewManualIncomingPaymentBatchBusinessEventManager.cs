using System;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Savings.Shared.Database;
using NTech.Core.Savings.Shared.DbModel;

namespace NTech.Core.Savings.Shared.BusinessEvents
{
    public class NewManualIncomingPaymentBatchBusinessEventManager : BusinessEventManagerBaseCore
    {
        public NewManualIncomingPaymentBatchBusinessEventManager(NTech.Core.Module.Shared.Infrastructure.INTechCurrentUserMetadata currentUser, 
            ICoreClock clock, IClientConfigurationCore clientConfiguration) : base(currentUser, clock, clientConfiguration)
        {
        }

        public class ManualPayment
        {
            public decimal Amount { get; set; }
            public DateTime BookkeepingDate { get; set; }
            public string NoteText { get; set; }
            public int? InitiatedByUserId { get; set; }
        }

        public BusinessEvent CreateBatch(ISavingsContext context, params ManualPayment[] payments)
        {

            var evt = AddBusinessEvent(BusinessEventType.NewManualIncomingPaymentBatch, context);

            foreach (var p in payments)
            {
                var pmt = new IncomingPaymentHeader
                {
                    BookKeepingDate = p.BookkeepingDate,
                    TransactionDate = Now.ToLocalTime().Date,
                };
                FillInInfrastructureFields(pmt);
                context.AddIncomingPaymentHeaders(pmt);

                if (!string.IsNullOrWhiteSpace(p.NoteText))
                {
                    var noteItem = new IncomingPaymentHeaderItem
                    {
                        IsEncrypted = false,
                        Name = IncomingPaymentHeaderItemCode.NoteText.ToString(),
                        Payment = pmt,
                        Value = p.NoteText
                    };
                    FillInInfrastructureFields(noteItem);
                    context.AddIncomingPaymentHeaderItems(noteItem);
                }

                context.AddIncomingPaymentHeaderItems(FillInInfrastructureFields(new IncomingPaymentHeaderItem
                {
                    IsEncrypted = false,
                    Name = IncomingPaymentHeaderItemCode.IsManualPayment.ToString(),
                    Payment = pmt,
                    Value = "true"
                }));

                if (p.InitiatedByUserId.HasValue)
                {
                    context.AddIncomingPaymentHeaderItems(FillInInfrastructureFields(new IncomingPaymentHeaderItem
                    {
                        IsEncrypted = false,
                        Name = IncomingPaymentHeaderItemCode.InitiatedByUserId.ToString(),
                        Payment = pmt,
                        Value = p.InitiatedByUserId.Value.ToString()
                    }));
                }

                AddTransaction(context, LedgerAccountTypeCode.UnplacedPayment, p.Amount, evt, p.BookkeepingDate, incomingPayment: pmt);
            }

            return evt;
        }
    }
}