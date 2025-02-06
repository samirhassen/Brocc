using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class BusinessEventManagerOrServiceBase
    {
        public int UserId => currentUser.UserId;
        public string InformationMetadata => currentUser.InformationMetadata;
        public DateTimeOffset Now => clock.Now;
        public ICoreClock Clock => clock;
        public IClientConfigurationCore ClientCfg => clientConfiguration;
        public INTechCurrentUserMetadata CurrentUser => currentUser;

        public BusinessEventManagerOrServiceBase(INTechCurrentUserMetadata currentUser, ICoreClock clock, IClientConfigurationCore clientConfiguration)
        {
            this.currentUser = currentUser;
            this.clock = clock;
            this.clientConfiguration = clientConfiguration;
        }

        protected void SetStatus(CreditHeader credit, CreditStatus status, BusinessEvent e, ICreditContextExtended context)
        {
            credit.Status = status.ToString();
            AddDatedCreditString(DatedCreditStringCode.CreditStatus.ToString(), status.ToString(), credit, e, context);
        }

        protected AccountTransaction CreateTransaction(TransactionAccountType accountType, decimal amount, DateTime bookKeepingDate, BusinessEvent e, CreditHeader credit = null, CreditReminderHeader reminder = null, CreditNotificationHeader notification = null, IncomingPaymentHeader incomingPayment = null, WriteoffHeader writeOff = null, int? incomingPaymentId = null, string creditNr = null, int? notificationId = null, int? outgoingPaymentId = null, OutgoingPaymentHeader outgoingPayment = null, int? writeOffId = null, CreditPaymentFreeMonth creditPaymentFreeMonth = null, int? creditPaymentFreeMonthId = null, string businessEventRuleCode = null, GuardedHistoricalTransactionDate historicalTransactionDate = null, string subAccountCode = null)
        {
            return new AccountTransaction
            {
                AccountCode = accountType.ToString(),
                Amount = amount,
                BookKeepingDate = bookKeepingDate,
                TransactionDate = historicalTransactionDate?.HistoricalTransactionDate ?? Now.ToLocalTime().Date,
                BusinessEvent = e,
                ChangedById = currentUser.UserId,
                ChangedDate = Now,
                CreditNotification = notification,
                CreditNotificationId = notificationId,
                Credit = credit,
                CreditNr = creditNr,
                InformationMetaData = currentUser.InformationMetadata,
                IncomingPayment = incomingPayment,
                IncomingPaymentId = incomingPaymentId,
                Reminder = reminder,
                Writeoff = writeOff,
                WriteoffId = writeOffId,
                OutgoingPayment = outgoingPayment,
                OutgoingPaymentId = outgoingPaymentId,
                CreditPaymentFreeMonthId = creditPaymentFreeMonthId,
                PaymentFreeMonth = creditPaymentFreeMonth,
                BusinessEventRoleCode = businessEventRuleCode,
                SubAccountCode = subAccountCode
            };
        }

        protected CreditSecurityItem AddSecurityItem(string name, ICreditContextExtended context,
            CreditHeader credit = null,
            string creditNr = null,
            BusinessEvent businessEvent = null,
            int? businessEventId = null,
            string stringValue = null,
            DateTime? dateValue = null,
            decimal? numericValue = null)
        {
            if (credit == null && creditNr == null)
                throw new Exception("credit or creditId is required");
            if (businessEvent == null && !businessEventId.HasValue)
                throw new Exception("businessEvent or businessEventId is required");

            var i = new CreditSecurityItem
            {
                CreatedByBusinessEventId = businessEventId ?? 0,
                CreatedByEvent = businessEvent,
                Credit = credit,
                CreditNr = creditNr,
                Name = name
            };

            var guardCount = 0;
            if (stringValue != null)
            {
                guardCount++;
                i.StringValue = stringValue;
            }

            if (dateValue.HasValue)
            {
                guardCount++;
                i.StringValue = dateValue.Value.ToString("yyyy-MM-dd");
                i.DateValue = dateValue.Value.Date;
            }

            if (numericValue.HasValue)
            {
                guardCount++;
                i.NumericValue = numericValue.Value;
                i.StringValue = numericValue.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (guardCount != 1)
                throw new Exception("Exactly one of stringValue, dateValue and numericValue must have a value");

            this.FillInInfrastructureFields(i);
            context.AddCreditSecurityItem(i);
            return i;
        }

        protected CreditComment AddComment(string commentText, string eventType, CreditHeader credit, ICreditContextExtended context, CreditCommentAttachmentModel attachment = null, BusinessEvent evt = null, string creditNr = null)
        {
            var c = new CreditComment
            {
                CommentById = UserId,
                ChangedById = UserId,
                ChangedDate = Now,
                CommentDate = Now,
                CommentText = commentText,
                Credit = credit,
                CreditNr = creditNr,
                EventType = eventType,
                InformationMetaData = InformationMetadata,
                Attachment = attachment?.Serialize(),
                CreatedByEvent = evt
            };
            context.AddCreditComment(c);
            return c;
        }

        public BusinessEvent AddBusinessEvent(BusinessEventType t, ICreditContextExtended context) => AddBusinessEventShared(t, context);


        public static BusinessEvent AddBusinessEventShared(BusinessEventType t, ICreditContextExtended context)
        {
            var evt = new BusinessEvent
            {
                EventDate = context.CoreClock.Now,
                EventType = t.ToString(),
                BookKeepingDate = context.CoreClock.Now.ToLocalTime().Date,
                TransactionDate = context.CoreClock.Now.ToLocalTime().Date,
                ChangedById = context.CurrentUser.UserId,
                ChangedDate = context.CoreClock.Now,
                InformationMetaData = context.CurrentUser.InformationMetadata
            };
            context.AddBusinessEvent(evt);
            return evt;
        }

        public static CreditComment AddCommentShared(string commentText, string eventType, ICreditContextExtended context, CreditHeader credit = null, string creditNr = null, CreditCommentAttachmentModel attachment = null, BusinessEvent evt = null)
        {
            if (credit == null && creditNr == null)
                throw new Exception("One of credit or creditNr must be set");
            var c = new CreditComment
            {
                CommentById = context.CurrentUser.UserId,
                ChangedById = context.CurrentUser.UserId,
                ChangedDate = context.CoreClock.Now,
                CommentDate = context.CoreClock.Now,
                CommentText = commentText,
                Attachment = attachment?.Serialize(),
                Credit = credit,
                CreditNr = creditNr,
                EventType = "BusinessEvent_" + eventType,
                InformationMetaData = context.CurrentUser.InformationMetadata,
                CreatedByEvent = evt
            };
            context.AddCreditComment(c);
            return c;
        }

        protected CreditComment AddComment(string commentText, BusinessEventType eventType, ICreditContextExtended context, CreditHeader credit = null, string creditNr = null, CreditCommentAttachmentModel attachment = null, BusinessEvent evt = null) =>
            AddCommentShared(commentText, eventType.ToString(), context, credit: credit, creditNr: creditNr, attachment: attachment, evt: evt);

        protected DatedCreditValue AddDatedCreditValue(string name, decimal amount, CreditHeader credit, BusinessEvent e, ICreditContextExtended context, GuardedHistoricalTransactionDate historicalTransactionDate = null)
        {
            var r = new DatedCreditValue
            {
                Credit = credit,
                BusinessEvent = e,
                TransactionDate = historicalTransactionDate?.HistoricalTransactionDate ?? Now.ToLocalTime().Date,
                Name = name,
                Value = amount,
                ChangedById = UserId,
                ChangedDate = Now,
                InformationMetaData = InformationMetadata
            };
            context.AddDatedCreditValue(r);
            return r;
        }

        protected DatedCreditValue AddDatedCreditValue(DatedCreditValueCode code, decimal value, BusinessEvent e, ICreditContextExtended context, string creditNr = null, CreditHeader credit = null)
        {
            var r = new DatedCreditValue
            {
                Credit = credit,
                CreditNr = creditNr,
                BusinessEvent = e,
                TransactionDate = Now.ToLocalTime().Date,
                Name = code.ToString(),
                Value = value,
                ChangedById = UserId,
                ChangedDate = Now,
                InformationMetaData = InformationMetadata
            };
            context.AddDatedCreditValue(r);
            return r;
        }

        protected DatedCreditCustomerValue AddDatedCreditCustomerValue(string name, decimal amount, CreditHeader credit, BusinessEvent e, ICreditContextExtended context, int customerId, GuardedHistoricalTransactionDate historicalTransactionDate = null)
        {
            var r = new DatedCreditCustomerValue
            {
                Credit = credit,
                BusinessEvent = e,
                TransactionDate = historicalTransactionDate?.HistoricalTransactionDate ?? Now.ToLocalTime().Date,
                Name = name,
                Value = amount,
                ChangedById = UserId,
                ChangedDate = Now,
                InformationMetaData = InformationMetadata,
                CustomerId = customerId
            };
            context.AddDatedCreditCustomerValue(r);
            return r;
        }

        protected DatedCreditString AddDatedCreditString(string name, string value, CreditHeader credit, BusinessEvent e, ICreditContextExtended context, GuardedHistoricalTransactionDate historicalTransactionDate = null)
        {
            return AddDatedCreditStringI(name, value, credit, null, e, context, historicalTransactionDate: historicalTransactionDate);
        }

        protected DatedCreditString AddDatedCreditString(string name, string value, string creditNr, BusinessEvent e, ICreditContextExtended context)
        {
            return AddDatedCreditStringI(name, value, null, creditNr, e, context);
        }

        private DatedCreditString AddDatedCreditStringI(string name, string value, CreditHeader credit, string creditNr, BusinessEvent e, ICreditContextExtended context, GuardedHistoricalTransactionDate historicalTransactionDate = null)
        {
            var r = new DatedCreditString
            {
                Credit = credit,
                CreditNr = creditNr,
                BusinessEvent = e,
                TransactionDate = historicalTransactionDate?.HistoricalTransactionDate ?? Now.ToLocalTime().Date,
                Name = name,
                Value = value,
                ChangedById = UserId,
                ChangedDate = Now,
                InformationMetaData = InformationMetadata
            };
            context.AddDatedCreditString(r);
            return r;
        }

        protected DatedCreditDate AddDatedCreditDate(DatedCreditDateCode code, DateTime value, BusinessEvent e, ICreditContextExtended context, string creditNr = null, CreditHeader credit = null, int? businessEventId = null)
        {
            var r = new DatedCreditDate
            {
                Credit = credit,
                CreditNr = creditNr,
                BusinessEvent = e,
                TransactionDate = Now.ToLocalTime().Date,
                Name = code.ToString(),
                Value = value,
                ChangedById = UserId,
                ChangedDate = Now,
                InformationMetaData = InformationMetadata
            };
            if (e == null)
            {
                r.BusinessEventId = businessEventId.Value;
            }
            context.AddDatedCreditDate(r);
            return r;
        }

        protected void RemoveDatedCreditDate(ICreditContextExtended context, string creditNr, DatedCreditDateCode code, BusinessEvent evt)
        {
            var latestValue = context
                .DatedCreditDatesQueryable
                .Where(x => x.CreditNr == creditNr && x.Name == code.ToString())
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

            if (latestValue == null || latestValue.RemovedByBusinessEventId.HasValue)
                return;

            latestValue.RemovedByBusinessEvent = evt;
        }

        protected CreditDocument AddCreditDocument(string documentType, int? applicantNr, string archiveKey, ICreditContextExtended context, string creditNr = null, CreditHeader credit = null, CreditReminderHeader reminder = null, CreditTerminationLetterHeader terminationLetter = null, int? customerId = null)
        {
            var d = new CreditDocument
            {
                CreditNr = creditNr,
                Credit = credit,
                Reminder = reminder,
                TerminationLetter = terminationLetter,
                ApplicantNr = applicantNr,
                CustomerId = customerId,
                ArchiveKey = archiveKey,
                DocumentType = documentType
            };
            FillInInfrastructureFields(d);
            context.AddCreditDocument(d);
            return d;
        }

        protected IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n);
            }
        }

        private CultureInfo commentFormattingCulture;

        protected CultureInfo CommentFormattingCulture
        {
            get
            {
                if (commentFormattingCulture == null)
                {
                    commentFormattingCulture = NTechCoreFormatting.GetScreenFormattingCulture(ClientCfg.Country.BaseFormattingCulture);
                }
                return commentFormattingCulture;
            }
        }

        private CultureInfo printFormattingCulture;
        private readonly INTechCurrentUserMetadata currentUser;
        private readonly ICoreClock clock;
        private readonly IClientConfigurationCore clientConfiguration;

        protected CultureInfo PrintFormattingCulture
        {
            get
            {
                if (printFormattingCulture == null)
                {
                    printFormattingCulture = NTechCoreFormatting.GetPrintFormattingCulture(ClientCfg.Country.BaseFormattingCulture);
                }
                return printFormattingCulture;
            }
        }

        /// <summary>
        /// YYYY-MM but culture aware
        /// </summary>
        protected string FormatMonthCultureAware(DateTime d)
        {
            return NTechCoreFormatting.FormatMonth(d, CommentFormattingCulture);
        }

        protected T FillInInfrastructureFields<T>(T item) where T : InfrastructureBaseItem
        {
            item.ChangedById = UserId;
            item.ChangedDate = Clock.Now;
            item.InformationMetaData = InformationMetadata;
            return item;
        }
    }

    public class GuardedHistoricalTransactionDate
    {
        private readonly Lazy<DateTime?> guardedHistoricalTransactionDate;

        public GuardedHistoricalTransactionDate(ICreditContextExtended context, DateTime? historicalTransactionDate, ICoreClock clock, CreditHeader creditHeader)
        {
            this.guardedHistoricalTransactionDate = new Lazy<DateTime?>(() =>
            {
                if (historicalTransactionDate.HasValue)
                {
                    var d = historicalTransactionDate.Value.Date;
                    if (d == clock.Today)
                        return null;

                    if (d > clock.Today.Date)
                        throw new Exception("Historical transaction date cannot be a future date");

                    if (string.IsNullOrWhiteSpace(creditHeader?.CreditNr))
                        throw new Exception("Cannot tell if new credit or not when creditnr not present on credit entity");
                    if (!context.HasNewCreditAddedToContext(creditHeader.CreditNr))
                        throw new Exception("Historical transaction dates can only be used on new credits");
                }
                return historicalTransactionDate;
            });
        }

        public DateTime? HistoricalTransactionDate
        {
            get
            {
                return this.guardedHistoricalTransactionDate.Value;
            }
        }
    }
}