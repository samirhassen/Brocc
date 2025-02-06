using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;

namespace nCredit.DbModel.BusinessEvents
{
    public class NewManualIncomingPaymentBatchBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        public NewManualIncomingPaymentBatchBusinessEventManager(INTechCurrentUserMetadata currentUser, ICoreClock clock, IClientConfigurationCore clientConfiguration) : base(currentUser, clock, clientConfiguration)
        {
        }

        public class ManualPayment
        {
            public decimal Amount { get; set; }
            public DateTime BookkeepingDate { get; set; }
            public string NoteText { get; set; }
            public int? InitiatedByUserId { get; set; }
        }

        public BusinessEvent CreateBatch(ICreditContextExtended context, ManualPayment[] payments)
        {
            var evt = new BusinessEvent
            {
                EventDate = Now,
                EventType = BusinessEventType.NewManualIncomingPaymentBatch.ToString(),
                BookKeepingDate = Now.ToLocalTime().Date,
                TransactionDate = Now.ToLocalTime().Date,
                ChangedById = UserId,
                ChangedDate = Now,
                InformationMetaData = InformationMetadata,
            };
            context.AddBusinessEvent(evt);

            foreach (var p in payments)
            {
                var pmt = new IncomingPaymentHeader
                {
                    BookKeepingDate = p.BookkeepingDate,
                    TransactionDate = Now.ToLocalTime().Date,
                    ChangedById = UserId,
                    ChangedDate = Now,
                    InformationMetaData = InformationMetadata
                };
                context.AddIncomingPaymentHeader(pmt);

                if (!string.IsNullOrWhiteSpace(p.NoteText))
                {
                    var noteItem = new IncomingPaymentHeaderItem
                    {
                        ChangedById = UserId,
                        ChangedDate = Now,
                        InformationMetaData = InformationMetadata,
                        IsEncrypted = false,
                        Name = IncomingPaymentHeaderItemCode.NoteText.ToString(),
                        Payment = pmt,
                        Value = p.NoteText
                    };
                    context.AddIncomingPaymentHeaderItem(noteItem);
                }

                context.AddIncomingPaymentHeaderItem(new IncomingPaymentHeaderItem
                {
                    ChangedById = UserId,
                    ChangedDate = Now,
                    InformationMetaData = InformationMetadata,
                    IsEncrypted = false,
                    Name = IncomingPaymentHeaderItemCode.IsManualPayment.ToString(),
                    Payment = pmt,
                    Value = "true"
                });

                if (p.InitiatedByUserId.HasValue)
                {
                    context.AddIncomingPaymentHeaderItem(FillInInfrastructureFields(new IncomingPaymentHeaderItem
                    {
                        IsEncrypted = false,
                        Name = IncomingPaymentHeaderItemCode.InitiatedByUserId.ToString(),
                        Payment = pmt,
                        Value = p.InitiatedByUserId.Value.ToString()
                    }));
                }

                context.AddAccountTransactions(CreateTransaction(TransactionAccountType.UnplacedPayment, p.Amount, p.BookkeepingDate, evt, incomingPayment: pmt));
            }

            return evt;
        }
    }
}