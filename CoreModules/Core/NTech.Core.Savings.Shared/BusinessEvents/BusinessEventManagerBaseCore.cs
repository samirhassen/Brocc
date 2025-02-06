using Newtonsoft.Json;
using NTech.Core;
using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Savings.Shared.Database;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace nSavings.DbModel.BusinessEvents
{
    public class BusinessEventManagerBaseCore
    {
        private readonly NTech.Core.Module.Shared.Infrastructure.INTechCurrentUserMetadata currentUser;
        private readonly ICoreClock clock;

        public BusinessEventManagerBaseCore(NTech.Core.Module.Shared.Infrastructure.INTechCurrentUserMetadata currentUser, ICoreClock clock, IClientConfigurationCore clientConfiguration)
        {
            this.currentUser = currentUser;
            this.clock = clock;
            commentFormattingCulture = new Lazy<CultureInfo>(() => NTechCoreFormatting.GetScreenFormattingCulture(clientConfiguration.Country.BaseFormattingCulture));
            printFormattingCulture = new Lazy<CultureInfo>(() => NTechCoreFormatting.GetPrintFormattingCulture(clientConfiguration.Country.BaseFormattingCulture));
        }

        protected ICoreClock Clock => clock;
        protected int UserId => currentUser.UserId;
        protected string InformationMetadata => currentUser.InformationMetadata;
        protected DateTimeOffset Now => clock.Now;
        private readonly Lazy<CultureInfo> commentFormattingCulture;
        private readonly Lazy<CultureInfo> printFormattingCulture;

        protected CultureInfo CommentFormattingCulture => commentFormattingCulture.Value;
        protected CultureInfo PrintFormattingCulture => printFormattingCulture.Value;

        protected BusinessEvent AddBusinessEvent(BusinessEventType t, ISavingsContext context)
        {
            var evt = new BusinessEvent
            {
                EventDate = clock.Now,
                EventType = t.ToString(),
                TransactionDate = clock.Now.ToLocalTime().Date,
            };
            FillInInfrastructureFields(evt);
            context.AddBusinessEvents(evt);
            return evt;
        }

        protected void SetStatus(SavingsAccountHeader savingsAccount, SavingsAccountStatusCode status, BusinessEvent e, ISavingsContext context)
        {
            savingsAccount.Status = status.ToString();
            AddDatedSavingsAccountString(DatedSavingsAccountStringCode.SavingsAccountStatus.ToString(), status.ToString(), context,
                savingsAccount: savingsAccount,
                businessEvent: e);
        }

        protected DatedSavingsAccountString AddDatedSavingsAccountString(string name, string value, ISavingsContext context,
            SavingsAccountHeader savingsAccount = null, string savingsAccountNr = null,
            BusinessEvent businessEvent = null, int? businessEventId = null)
        {
            var r = new DatedSavingsAccountString
            {
                SavingsAccount = savingsAccount,
                SavingsAccountNr = savingsAccountNr,
                BusinessEvent = businessEvent,
                TransactionDate = Clock.Now.ToLocalTime().Date,
                Name = name,
                Value = value
            };
            if (businessEventId.HasValue)
                r.BusinessEventId = businessEventId.Value;

            FillInInfrastructureFields(r);
            context.AddDatedSavingsAccountStrings(r);
            return r;
        }

        protected LedgerAccountTransaction AddTransaction(ISavingsContext context, LedgerAccountTypeCode accountType, decimal amount, BusinessEvent e, DateTime bookKeepingDate,
            SavingsAccountHeader savingsAccount = null, string savingsAccountNr = null,
            IncomingPaymentHeader incomingPayment = null, int? incomingPaymentId = null,
            int? outgoingPaymentId = null, OutgoingPaymentHeader outgoingPayment = null,
            DateTime? interestFromDate = null, //Defaults to transaction date            
            string businessEventRoleCode = null)
        {
            var transactionDate = Now.ToLocalTime().Date;
            var tr = new LedgerAccountTransaction
            {
                AccountCode = accountType.ToString(),
                Amount = amount,
                BookKeepingDate = bookKeepingDate,
                InterestFromDate = interestFromDate ?? transactionDate,
                TransactionDate = transactionDate,
                BusinessEvent = e,
                SavingsAccount = savingsAccount,
                SavingsAccountNr = savingsAccountNr,
                IncomingPayment = incomingPayment,
                IncomingPaymentId = incomingPaymentId,
                OutgoingPayment = outgoingPayment,
                OutgoingPaymentId = outgoingPaymentId,
                BusinessEventRoleCode = businessEventRoleCode
            };
            FillInInfrastructureFields(tr);
            context.AddLedgerAccountTransactions(tr);
            return tr;
        }

        protected T FillInInfrastructureFields<T>(T item) where T : InfrastructureBaseItem
        {
            item.ChangedById = currentUser.UserId;
            item.ChangedDate = clock.Now;
            item.InformationMetaData = currentUser.InformationMetadata;
            return item;
        }


        protected SavingsAccountComment AddComment(string commentText, BusinessEventType eventType, ISavingsContext context, SavingsAccountHeader savingsAccount = null, string savingsAccountNr = null, List<string> attachmentArchiveKeys = null)
        {
            if (savingsAccount == null && savingsAccountNr == null)
                throw new Exception("One of savingsAccount or savingsAccountNr must be set");
            var c = new SavingsAccountComment
            {
                CommentById = UserId,
                CommentDate = Now,
                CommentText = commentText,
                Attachment = attachmentArchiveKeys == null ? null : JsonConvert.SerializeObject(new { archiveKeys = attachmentArchiveKeys }),
                SavingsAccount = savingsAccount,
                SavingsAccountNr = savingsAccountNr,
                EventType = "BusinessEvent_" + eventType.ToString(),
            };
            FillInInfrastructureFields(c);
            context.AddSavingsAccountComments(c);
            return c;
        }

        protected SavingsAccountDocument AddSavingsAccountDocument(SavingsAccountDocumentTypeCode code, string archiveKey, ISavingsContext context,
            string savingsAccountNr = null, SavingsAccountHeader savingsAccount = null, string documentData = null, BusinessEvent businessEvent = null, int? businessEventId = null)
        {
            var d = new SavingsAccountDocument
            {
                CreatedByEvent = businessEvent,
                DocumentArchiveKey = archiveKey,
                DocumentType = code.ToString(),
                SavingsAccount = savingsAccount,
                SavingsAccountNr = savingsAccountNr,
                DocumentData = documentData,
                DocumentDate = Clock.Now
            };
            if (businessEventId.HasValue)
                d.CreatedByBusinessEventId = businessEventId.Value;

            FillInInfrastructureFields(d);
            context.AddSavingsAccountDocuments(d);
            return d;
        }
    }
}