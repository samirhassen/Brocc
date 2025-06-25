using nCredit.DbModel.Model;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DbModel;
using NTech.Core.Module.Shared.Database;
using NTech.Services.Infrastructure;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;

namespace nCredit
{
    public class CreditContext : DbModel.ChangeTrackingDbContext, INTechDbContext, ISystemItemCreditContext
    {
        private const string ConnectionStringName = "CreditContext";

        public CreditContext() : this($"name={ConnectionStringName}")
        {

        }

        public CreditContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
            Configuration.AutoDetectChangesEnabled = false;
        }

        public virtual DbSet<CreditKeySequence> CreditKeySequences { get; set; }
        public virtual DbSet<CreditHeader> CreditHeaders { get; set; }
        public virtual DbSet<BusinessEvent> BusinessEvents { get; set; }
        public virtual DbSet<CreditNotificationHeader> CreditNotificationHeaders { get; set; }
        public virtual DbSet<CreditReminderHeader> CreditReminderHeaders { get; set; }
        public virtual DbSet<AccountTransaction> Transactions { get; set; }
        public virtual DbSet<DatedCreditValue> DatedCreditValues { get; set; }
        public virtual DbSet<DatedCreditCustomerValue> DatedCreditCustomerValues { get; set; }
        public virtual DbSet<DatedCreditDate> DatedCreditDates { get; set; }
        public virtual DbSet<DatedCreditString> DatedCreditStrings { get; set; }
        public virtual DbSet<EncryptedValue> EncryptedValues { get; set; }
        public virtual DbSet<CreditCustomer> CreditCustomers { get; set; }
        public virtual DbSet<CreditComment> CreditComments { get; set; }
        public virtual DbSet<SharedDatedValue> SharedDatedValues { get; set; }
        public virtual DbSet<OcrPaymentReferenceNrSequence> OcrPaymentReferenceNrSequences { get; set; }
        public virtual DbSet<WriteoffHeader> WriteoffHeaders { get; set; }
        public virtual DbSet<OutgoingPaymentHeader> OutgoingPaymentHeaders { get; set; }
        public virtual DbSet<OutgoingPaymentFileHeader> OutgoingPaymentFileHeaders { get; set; }
        public virtual DbSet<OutgoingPaymentHeaderItem> OutgoingPaymentHeaderItems { get; set; }
        public virtual DbSet<IncomingPaymentFileHeader> IncomingPaymentFileHeaders { get; set; }
        public virtual DbSet<IncomingPaymentHeader> IncomingPaymentHeaders { get; set; }
        public virtual DbSet<IncomingPaymentHeaderItem> IncomingPaymentHeaderItems { get; set; }
        public virtual DbSet<OutgoingBookkeepingFileHeader> OutgoingBookkeepingFileHeaders { get; set; }
        public virtual DbSet<OutgoingCreditNotificationDeliveryFileHeader> OutgoingCreditNotificationDeliveryFileHeaders { get; set; }
        public virtual DbSet<DailyKycScreenHeader> DailyKycScreenHeaders { get; set; }
        public virtual DbSet<OutgoingCreditReminderDeliveryFileHeader> OutgoingCreditReminderDeliveryFileHeaders { get; set; }
        public virtual DbSet<CreditDocument> Documents { get; set; }
        public virtual DbSet<SystemItem> SystemItems { get; set; }
        public virtual DbSet<OneTimeToken> OneTimeTokens { get; set; }
        public virtual DbSet<CreditTerminationLetterHeader> CreditTerminationLetterHeaders { get; set; }
        public virtual DbSet<OutgoingCreditTerminationLetterDeliveryFileHeader> OutgoingCreditTerminationLetterDeliveryFileHeaders { get; set; }
        public virtual DbSet<OutgoingDebtCollectionFileHeader> OutgoingDebtCollectionFileHeaders { get; set; }
        public virtual DbSet<OutgoingSatExportFileHeader> OutgoingSatExportFileHeaders { get; set; }
        public virtual DbSet<CreditFuturePaymentFreeMonth> CreditFuturePaymentFreeMonths { get; set; }
        public virtual DbSet<CreditPaymentFreeMonth> CreditPaymentFreeMonths { get; set; }
        public virtual DbSet<OutgoingAmlMonitoringExportFileHeader> OutgoingAmlMonitoringExportFileHeaders { get; set; }
        public virtual DbSet<CreditTermsChangeHeader> CreditTermsChangeHeaders { get; set; }
        public virtual DbSet<CreditTermsChangeItem> CreditTermsChangeItems { get; set; }
        public virtual DbSet<CreditSettlementOfferHeader> CreditSettlementOfferHeaders { get; set; }
        public virtual DbSet<CreditSettlementOfferItem> CreditSettlementOfferItems { get; set; }
        public virtual DbSet<CalendarDate> CalendarDates { get; set; }
        public virtual DbSet<WorkListHeader> WorkListHeaders { get; set; }
        public virtual DbSet<WorkListItem> WorkListItems { get; set; }
        public virtual DbSet<WorkListItemProperty> WorkListItemProperties { get; set; }
        public virtual DbSet<WorkListFilterItem> WorkListFilterItems { get; set; }
        public virtual DbSet<EInvoiceFiMessageHeader> EInvoiceFiMessageHeaders { get; set; }
        public virtual DbSet<EInvoiceFiMessageItem> EInvoiceFiMessageItems { get; set; }
        public virtual DbSet<EInvoiceFiAction> EInvoiceFiActions { get; set; }
        public virtual DbSet<CreditSecurityItem> CreditSecurityItems { get; set; }
        public virtual DbSet<CreditOutgoingDirectDebitItem> CreditOutgoingDirectDebitItems { get; set; }
        public virtual DbSet<OutgoingDirectDebitStatusChangeFileHeader> OutgoingDirectDebitStatusChangeFileHeaders { get; set; }
        public virtual DbSet<IncomingDirectDebitStatusChangeFileHeader> IncomingDirectDebitStatusChangeFileHeaders { get; set; }
        public virtual DbSet<KeyValueItem> KeyValueItems { get; set; }
        public virtual DbSet<ReferenceInterestChangeHeader> ReferenceInterestChangeHeaders { get; set; }
        public virtual DbSet<CreditCustomerListMember> CreditCustomerListMembers { get; set; }
        public virtual DbSet<CreditCustomerListOperation> CreditCustomerListOperations { get; set; }
        public virtual DbSet<OutgoingExportFileHeader> OutgoingExportFileHeader { get; set; }
        public virtual DbSet<CreditAnnualStatementHeader> CreditAnnualStatementHeaders { get; set; }
        public virtual DbSet<CollateralHeader> CollateralHeaders { get; set; }
        public virtual DbSet<CollateralItem> CollateralItems { get; set; }
        public virtual DbSet<FixedMortgageLoanInterestRate> FixedMortgageLoanInterestRates { get; set; }
        public virtual DbSet<HFixedMortgageLoanInterestRate> HFixedMortgageLoanInterestRates { get; set; }
        public virtual DbSet<AlternatePaymentPlanHeader> AlternatePaymentPlanHeaders { get; set; }
        public virtual DbSet<AlternatePaymentPlanMonth> AlternatePaymentPlanMonths { get; set; }
        public virtual DbSet<SieFileVerification> SieFileVerifications { get; set; }
        public virtual DbSet<SieFileTransaction> SieFileTransactions { get; set; }
        public virtual DbSet<SieFileConnection> SieFileConnections { get; set; }

        public IQueryable<SystemItem> SystemItemsQueryable => SystemItems;
        public void AddSystemItems(params SystemItem[] items) => SystemItems.AddRange(items);

        private static System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<T> ConfigureInfrastructureFields<T>(System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<T> t) where T : InfrastructureBaseItem
        {
            t.Property(e => e.Timestamp).IsRequired().IsRowVersion();
            t.Property(e => e.ChangedById).IsRequired();
            t.Property(e => e.ChangedDate).IsRequired();
            t.Property(e => e.InformationMetaData);
            return t;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            IndexAnnotation index(bool isUnique) => new IndexAnnotation(new IndexAttribute() { IsUnique = isUnique });

            modelBuilder.Entity<CreditKeySequence>().HasKey(x => x.Id);
            modelBuilder.Entity<OcrPaymentReferenceNrSequence>().HasKey(x => x.Id);

            Cfg<CreditHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.CreditNr);
                ch.Property(x => x.CreditType).HasMaxLength(100);
                ch.Property(x => x.NrOfApplicants).IsRequired();
                ch.Property(x => x.Status).IsRequired().HasMaxLength(100);
                ch.Property(x => x.ProviderName).IsRequired().HasMaxLength(100);
                ch.Property(x => x.StartDate).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.HasMany(x => x.Transactions).WithOptional(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.Reminders).WithRequired(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.TerminationLetters).WithRequired(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.Notifications).WithRequired(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.DatedCreditValues).WithRequired(x => x.Credit).HasForeignKey(x => x.CreditNr);
                //NOTE: That DatedCreditStrings has an optional Credit is an old bug that was discovered during migration. It's preserved since there 
                // are production dbs out there that look there that look like this but there are is no good reason for it.
                ch.HasMany(x => x.DatedCreditStrings).WithOptional(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.DatedCreditCustomerValues).WithRequired(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.DatedCreditDates).WithRequired(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.CreditCustomers).WithRequired(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.CreditFuturePaymentFreeMonths).WithRequired(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.CreditPaymentFreeMonths).WithRequired(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.Documents).WithOptional(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.TermsChanges).WithRequired(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.CreditSettlementOffers).WithRequired(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.EInvoiceFiActions).WithOptional(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.SecurityItems).WithRequired(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.CreditOutgoingDirectDebitItems).WithRequired(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.CustomerListMembers).WithRequired(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.CustomerListOperations).WithRequired(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.AnnualStatements).WithRequired(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasOptional(x => x.Collateral).WithMany(x => x.Credits);
                ch.HasMany(x => x.AlternatePaymentPlans).WithRequired(x => x.Credit).HasForeignKey(x => x.CreditNr);
            });

            Cfg<CreditCustomer>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.CustomerId).IsRequired();
                ch.Property(x => x.ApplicantNr).IsRequired();
            });

            Cfg<CreditComment>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                ConfigureInfrastructureFields(e);
                e.HasRequired(x => x.Credit).WithMany(x => x.Comments).HasForeignKey(x => x.CreditNr);
                e.Property(x => x.CommentById).IsRequired();
                e.Property(x => x.CommentDate).IsRequired();
                e.Property(x => x.CommentText);
                e.Property(x => x.Attachment);
                e.Property(x => x.EventType).HasMaxLength(100);
            });

            Cfg<BusinessEvent>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.EventType).IsRequired().HasMaxLength(100);
                ch.Property(x => x.EventDate).IsRequired();
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.BookKeepingDate).IsRequired().HasColumnType("date");
                ch.HasMany(x => x.Transactions).WithRequired(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId);
                ch.HasMany(x => x.DatedCreditValues).WithRequired(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId);
                ch.HasMany(x => x.DatedCreditCustomerValues).WithRequired(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId);
                ch.HasMany(x => x.DatedCreditStrings).WithRequired(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId);
                ch.HasMany(x => x.DatedCreditDates).WithRequired(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId);
                ch.HasMany(x => x.RemovedDatedCreditDates).WithOptional(x => x.RemovedByBusinessEvent).HasForeignKey(x => x.RemovedByBusinessEventId);
                ch.HasMany(x => x.SharedDatedValues).WithRequired(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId);
                ch.HasMany(x => x.CreatedOutgoingPayments).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.CreatedOutgoingPaymentFiles).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.CreatedIncomingPaymentFiles).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.CreatedCredits).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.CreditPaymentFreeMonths).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.CreatedCreditFuturePaymentFreeMonths).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.CommitedCreditFuturePaymentFreeMonths).WithOptional(x => x.CommitedByEvent).HasForeignKey(x => x.CommitedByEventBusinessEventId);
                ch.HasMany(x => x.CancelledCreditFuturePaymentFreeMonths).WithOptional(x => x.CancelledByEvent).HasForeignKey(x => x.CancelledByBusinessEventId);
                ch.HasMany(x => x.StartedTermsChanges).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId).WillCascadeOnDelete(false);
                ch.HasMany(x => x.CommitedTermsChanges).WithOptional(x => x.CommitedByEvent).HasForeignKey(x => x.CommitedByEventId);
                ch.HasMany(x => x.CancelledTermsChanges).WithOptional(x => x.CancelledByEvent).HasForeignKey(x => x.CancelledByEventId);
                ch.HasMany(x => x.AddedCreditTermsChangeItems).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId);
                ch.HasMany(x => x.StartedCreditSettlementOffers).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId).WillCascadeOnDelete(false);
                ch.HasMany(x => x.CommitedCreditSettlementOffers).WithOptional(x => x.CommitedByEvent).HasForeignKey(x => x.CommitedByEventId);
                ch.HasMany(x => x.CancelledCreditSettlementOffers).WithOptional(x => x.CancelledByEvent).HasForeignKey(x => x.CancelledByEventId);
                ch.HasMany(x => x.CreatedEInvoiceFiMessageHeaders).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId);
                ch.HasMany(x => x.ConnectedEInvoiceFiActions).WithOptional(x => x.ConnectBusinessEvent).HasForeignKey(x => x.ConnectedBusinessEventId);
                ch.HasMany(x => x.CreatedCreditSecurityItems).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.CreatedCreditOutgoingDirectDebitItems).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId);
                ch.HasMany(x => x.CreatedOutgoingDirectDebitStatusChangeFileHeaders).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId);
                ch.HasMany(x => x.CreatedIncomingDirectDebitStatusChangeFileHeaders).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId);
                ch.HasMany(x => x.CreatedCreditComments).WithOptional(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId);
                ch.HasMany(x => x.CreatedReferenceInterestChangeHeaders).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.CreatedCollaterals).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).WillCascadeOnDelete(false);
                ch.HasMany(x => x.CreatedCollateralItems).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).WillCascadeOnDelete(false);
                ch.HasMany(x => x.RemovedCollateralItems).WithOptional(x => x.RemovedByEvent).HasForeignKey(x => x.RemovedByBusinessEventId);
                ch.HasMany(x => x.CreatedFixedMortgageLoanInterestRates).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.CreatedHFixedMortgageLoanInterestRates).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.InactivatedTerminationLetters).WithOptional(x => x.InactivatedByBusinessEvent).HasForeignKey(x => x.InactivatedByBusinessEventId);
                ch.HasMany(x => x.CreatedAlternatePaymentPlanHeaders).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId);
                ch.HasMany(x => x.CancelledAlternatePaymentPlanHeaders).WithOptional(x => x.CancelledByEvent).HasForeignKey(x => x.CancelledByEventId);
                ch.HasMany(x => x.FullyPaidAlternatePaymentPlanHeaders).WithOptional(x => x.FullyPaidByEvent).HasForeignKey(x => x.FullyPaidByEventId);
            });

            Cfg<OutgoingPaymentFileHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.FileArchiveKey).HasMaxLength(100);
                ch.Property(x => x.ExternalId);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.BookKeepingDate).IsRequired().HasColumnType("date");
            });

            Cfg<OutgoingPaymentHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.BookKeepingDate).IsRequired().HasColumnType("date");
                ch.HasMany(x => x.Items).WithRequired(x => x.OutgoingPayment).HasForeignKey(x => x.OutgoingPaymentId);
                ch.HasMany(x => x.Transactions).WithOptional(x => x.OutgoingPayment).HasForeignKey(x => x.OutgoingPaymentId);
            });

            Cfg<OutgoingPaymentHeaderItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.Name).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.Value).IsRequired();
                ch.Property(x => x.IsEncrypted).IsRequired();
            });

            Cfg<IncomingPaymentFileHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.BookKeepingDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.FileArchiveKey).HasMaxLength(100);
                ch.Property(x => x.ExternalId);
                ch.HasMany(x => x.Payments).WithOptional(x => x.IncomingPaymentFile).HasForeignKey(x => x.IncomingPaymentFileId);
            });

            Cfg<IncomingPaymentHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.BookKeepingDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.IsFullyPlaced).IsRequired();
                ch.HasMany(x => x.Transactions).WithOptional(x => x.IncomingPayment).HasForeignKey(x => x.IncomingPaymentId);
                ch.HasMany(x => x.Items).WithRequired(x => x.Payment).HasForeignKey(x => x.IncomingPaymentHeaderId);
            });

            Cfg<IncomingPaymentHeaderItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.Name).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.Value).IsRequired();
                ch.Property(x => x.IsEncrypted).IsRequired();
            });

            Cfg<WriteoffHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.BookKeepingDate).IsRequired().HasColumnType("date");
                ch.HasMany(x => x.Transactions).WithOptional(x => x.Writeoff).HasForeignKey(x => x.WriteoffId);
            });

            Cfg<CreditReminderHeader>(modelBuilder, r =>
            {
                ConfigureInfrastructureFields(r);
                r.HasKey(x => x.Id);
                r.Property(x => x.ReminderNumber).IsRequired();
                r.Property(x => x.ReminderDate).IsRequired().HasColumnType("date");
                r.Property(x => x.InternalDueDate).IsRequired().HasColumnType("date");
                r.HasMany(x => x.Documents).WithOptional(x => x.Reminder).HasForeignKey(x => x.CreditReminderHeaderId);
                r.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                r.Property(x => x.BookKeepingDate).IsRequired().HasColumnType("date");
                r.HasMany(x => x.Transactions).WithOptional(x => x.Reminder).HasForeignKey(x => x.ReminderId);
                r.Property(x => x.CoReminderId).HasMaxLength(100);
                r.Property(x => x.IsCoReminderMaster);
            });

            Cfg<CreditDocument>(modelBuilder, r =>
            {
                ConfigureInfrastructureFields(r);
                r.Property(x => x.ArchiveKey).HasMaxLength(100).IsRequired();
                r.Property(x => x.DocumentType).HasMaxLength(100).IsRequired();
                r.Property(x => x.ApplicantNr);
                r.Property(x => x.CustomerId);
            });

            Cfg<CreditNotificationHeader>(modelBuilder, ci =>
            {
                ConfigureInfrastructureFields(ci);
                ci.HasKey(x => x.Id);
                ci.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ci.Property(x => x.BookKeepingDate).IsRequired().HasColumnType("date");
                ci.Property(x => x.NotificationDate).IsRequired().HasColumnType("date");
                ci.Property(x => x.DueDate).IsRequired().HasColumnType("date");
                ci.Property(x => x.ClosedTransactionDate).HasColumnType("date");
                ci.Property(x => x.PdfArchiveKey).HasMaxLength(100);
                ci.Property(x => x.OcrPaymentReference).IsRequired().HasMaxLength(100);
                ci.Property(x => x.CoNotificationId).HasMaxLength(100);
                ci.Property(x => x.IsCoNotificationMaster);
                ci.HasMany(x => x.Transactions).WithOptional(x => x.CreditNotification).HasForeignKey(x => x.CreditNotificationId);
                ci.HasMany(x => x.Reminders).WithRequired(x => x.Notification).HasForeignKey(x => x.NotificationId).WillCascadeOnDelete(false);
            });

            Cfg<DatedCreditValue>(modelBuilder, c =>
            {
                ConfigureInfrastructureFields(c);
                c.HasKey(x => x.Id);
                c.Property(x => x.Name).IsRequired().HasMaxLength(100);
                c.Property(x => x.Value).IsRequired();
                c.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
            });

            Cfg<DatedCreditCustomerValue>(modelBuilder, c =>
            {
                ConfigureInfrastructureFields(c);
                c.HasKey(x => x.Id);
                c.Property(x => x.Name).IsRequired().HasMaxLength(100);
                c.Property(x => x.Value).IsRequired();
                c.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                c.Property(x => x.CustomerId).IsRequired();
            });

            Cfg<DatedCreditDate>(modelBuilder, c =>
            {
                ConfigureInfrastructureFields(c);
                c.HasKey(x => x.Id);
                c.Property(x => x.Name).IsRequired().HasMaxLength(100);
                c.Property(x => x.Value).HasColumnType("date").IsRequired();
                c.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
            });

            Cfg<SharedDatedValue>(modelBuilder, c =>
            {
                ConfigureInfrastructureFields(c);
                c.HasKey(x => x.Id);
                c.Property(x => x.Name).IsRequired().HasMaxLength(100);
                c.Property(x => x.Value).IsRequired();
                c.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
            });

            Cfg<DatedCreditString>(modelBuilder, c =>
            {
                ConfigureInfrastructureFields(c);
                c.HasKey(x => x.Id);
                c.Property(x => x.Name).IsRequired().HasMaxLength(100);
                c.Property(x => x.Value).IsRequired().HasMaxLength(100); //NOTE: Dont make this longer if longer values appear. Put them somewhere else. This needs to be indexable
                c.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
            });

            Cfg<AccountTransaction>(modelBuilder, r =>
            {
                ConfigureInfrastructureFields(r);
                r.HasKey(x => x.Id);
                r.Property(x => x.AccountCode).IsRequired().HasMaxLength(100);
                r.Property(x => x.Amount).IsRequired();
                r.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                r.Property(x => x.BookKeepingDate).IsRequired().HasColumnType("date");
                r.Property(x => x.BusinessEventRoleCode).HasMaxLength(100);
                r.Property(x => x.SubAccountCode).HasMaxLength(100);
            });

            Cfg<EncryptedValue>(modelBuilder, ev =>
            {
                ev.HasKey(x => x.Id);
                ev.Property(x => x.EncryptionKeyName).IsRequired().HasMaxLength(100);
                ev.Property(e => e.Timestamp).IsRequired().IsRowVersion();
                ev.Property(e => e.CreatedById).IsRequired();
                ev.Property(e => e.CreatedDate).IsRequired();
                ev.Property(x => x.Value).IsRequired();
            });

            Cfg<OutgoingBookkeepingFileHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.FileArchiveKey).HasMaxLength(100);
                ch.Property(x => x.XlsFileArchiveKey).HasMaxLength(100);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.FromTransactionDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.ToTransactionDate).IsRequired().HasColumnType("date");
                ch.HasMany(x => x.Transactions).WithOptional(x => x.OutgoingBookkeepingFile).HasForeignKey(x => x.OutgoingBookkeepingFileHeaderId);
            });

            Cfg<OutgoingCreditNotificationDeliveryFileHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ExternalId);
                ch.Property(x => x.FileArchiveKey).HasMaxLength(100);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.HasMany(x => x.Notifications).WithOptional(x => x.DeliveryFile).HasForeignKey(x => x.OutgoingCreditNotificationDeliveryFileHeaderId);
                ch.HasOptional(x => x.CreatedByEvent).WithMany(x => x.CreatedNotificationDeliveryFiles).HasForeignKey(x => x.BusinessEvent_Id);
            });

            Cfg<OutgoingCreditReminderDeliveryFileHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ExternalId);
                ch.Property(x => x.FileArchiveKey).HasMaxLength(100);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.HasMany(x => x.Reminders).WithOptional(x => x.DeliveryFile).HasForeignKey(x => x.OutgoingCreditReminderDeliveryFileHeaderId);
            });

            Cfg<OutgoingExportFileHeader>(modelBuilder, fh =>
            {
                ConfigureInfrastructureFields(fh);
                fh.HasKey(x => x.Id);
                fh.Property(x => x.FileArchiveKey).HasMaxLength(100);
                fh.Property(x => x.FileType).HasMaxLength(100);
                fh.Property(x => x.ExportResultStatus);
                fh.Property(x => x.CustomData);
                fh.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                fh.HasMany(x => x.AnnualStatements).WithOptional(x => x.OutgoingExportFile).HasForeignKey(x => x.OutgoingExportFileHeaderId);
            });

            Cfg<DailyKycScreenHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.NrOfCustomersScreened).IsRequired();
                ch.Property(x => x.NrOfCustomersConflicted).IsRequired();
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.ResultModel).IsRequired();
            });

            Cfg<SystemItem>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.Id);
                e.Property(x => x.Key).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                e.Property(x => x.Value);
            });

            Cfg<OneTimeToken>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.Token);
                e.Property(x => x.Token).HasMaxLength(60);
                e.Property(x => x.TokenType).IsRequired().HasMaxLength(100);
                e.Property(x => x.CreationDate).IsRequired();
                e.Property(x => x.CreatedBy).IsRequired();
                e.Property(x => x.TokenExtraData);
                e.Property(x => x.ValidUntilDate);
                e.Property(x => x.RemovedDate);
                e.Property(x => x.RemovedBy);
                e.Property(x => x.CreationDate).IsRequired();
            });

            Cfg<CreditTerminationLetterHeader>(modelBuilder, r =>
            {
                ConfigureInfrastructureFields(r);
                r.HasKey(x => x.Id);
                r.Property(x => x.PrintDate).IsRequired().HasColumnType("date");
                r.Property(x => x.DueDate).IsRequired().HasColumnType("date");
                r.HasMany(x => x.Documents).WithOptional(x => x.TerminationLetter).HasForeignKey(x => x.CreditTerminationLetterHeaderId);
                r.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                r.Property(x => x.BookKeepingDate).IsRequired().HasColumnType("date");
                r.Property(x => x.CoTerminationId).HasMaxLength(100);
                r.Property(x => x.IsCoTerminationMaster);
            });

            Cfg<OutgoingCreditTerminationLetterDeliveryFileHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ExternalId);
                ch.Property(x => x.FileArchiveKey).HasMaxLength(100);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.HasMany(x => x.TerminationLetters).WithOptional(x => x.DeliveryFile).HasForeignKey(x => x.OutgoingCreditTerminationLetterDeliveryFileHeaderId);
            });

            Cfg<OutgoingDebtCollectionFileHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ExternalId).IsRequired().HasMaxLength(128).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                ch.Property(x => x.FileArchiveKey).HasMaxLength(100);
                ch.Property(x => x.XlsFileArchiveKey).HasMaxLength(100);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
            });

            Cfg<OutgoingSatExportFileHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.FileArchiveKey).HasMaxLength(100);
                ch.Property(x => x.ExportResultStatus);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
            });

            Cfg<CreditPaymentFreeMonth>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.DueDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.NotificationDate).IsRequired().HasColumnType("date");
                ch.HasMany(x => x.Transactions).WithOptional(x => x.PaymentFreeMonth).HasForeignKey(x => x.CreditPaymentFreeMonthId);
            });

            Cfg<CreditFuturePaymentFreeMonth>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ForMonth).IsRequired().HasColumnType("date");
            });

            Cfg<OutgoingAmlMonitoringExportFileHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.FileArchiveKey).HasMaxLength(100);
                ch.Property(x => x.ExportResultStatus);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.ProviderName);
            });

            Cfg<CreditTermsChangeHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.AutoExpireDate);
                ch.HasMany(x => x.Items).WithRequired(x => x.CreditTermsChange).HasForeignKey(x => x.CreditTermsChangeHeaderId);
            });

            Cfg<CreditTermsChangeItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ApplicantNr);
                ch.Property(x => x.Name).HasMaxLength(100).IsRequired();
                ch.Property(x => x.Value).IsRequired();
            });

            Cfg<CreditSettlementOfferHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.AutoExpireDate).HasColumnType("date");
                ch.Property(x => x.ExpectedSettlementDate).HasColumnType("date");
                ch.HasMany(x => x.Items).WithRequired(x => x.CreditSettlementOffer).HasForeignKey(x => x.CreditSettlementOfferHeaderId);
            });

            Cfg<CreditSettlementOfferItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.Name).HasMaxLength(100).IsRequired();
                ch.Property(x => x.Value).IsRequired();
            });

            Cfg<CalendarDate>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.TheDate);
                ch.Property(x => x.TheDate).HasColumnType("date").IsRequired();
            });

            Cfg<WorkListHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);

                ch.HasKey(x => x.Id);
                ch.Property(x => x.ListType).IsRequired().HasMaxLength(128);
                ch.Property(x => x.CreationDate).IsRequired();
                ch.Property(x => x.CreatedByUserId).IsRequired();
                ch.Property(x => x.ClosedByUserId);
                ch.Property(x => x.ClosedDate);
                ch.Property(x => x.CustomData);
                ch.Property(x => x.IsUnderConstruction).IsRequired();
                ch.HasMany(x => x.Items).WithRequired(x => x.WorkList).HasForeignKey(x => x.WorkListHeaderId);
                ch.HasMany(x => x.FilterItems).WithRequired(x => x.WorkList).HasForeignKey(x => x.WorkListHeaderId);
            });

            Cfg<WorkListItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);

                ch.HasKey(x => new { x.WorkListHeaderId, x.ItemId });
                ch.Property(x => x.ItemId).IsRequired().HasMaxLength(128);
                ch.Property(x => x.WorkListHeaderId).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("OrderUIdx") { Order = 1, IsUnique = true }));
                ch.Property(x => x.OrderNr).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("OrderUIdx") { Order = 2, IsUnique = true }));
                ch.Property(x => x.TakenByUserId);
                ch.Property(x => x.TakenDate);
                ch.Property(x => x.CompletedDate);
                ch.HasMany(x => x.Properties).WithRequired(x => x.Item).HasForeignKey(x => new { x.WorkListHeaderId, x.ItemId });
            });

            Cfg<WorkListItemProperty>(modelBuilder, ch =>
            {
                ch.HasKey(x => new { x.WorkListHeaderId, x.ItemId, x.Name });
                ch.Property(x => x.ItemId).IsRequired().HasMaxLength(128);
                ch.Property(x => x.WorkListHeaderId).IsRequired();
                ch.Property(x => x.IsEncrypted).IsRequired();
                ch.Property(x => x.Name).IsRequired().HasMaxLength(128);
                ch.Property(x => x.Value).HasMaxLength(128); //Not required since null is a reasonable value to want to store
            });

            Cfg<WorkListFilterItem>(modelBuilder, ch =>
            {
                ch.HasKey(x => new { x.WorkListHeaderId, x.Name });
                ch.Property(x => x.WorkListHeaderId).IsRequired();
                ch.Property(x => x.Name).IsRequired().HasMaxLength(128);
                ch.Property(x => x.Value).IsRequired().HasMaxLength(128);
            });

            Cfg<EInvoiceFiMessageHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ExternalMessageType).IsRequired().HasMaxLength(128);
                ch.Property(x => x.ExternalMessageId).IsRequired().HasMaxLength(128);
                ch.Property(x => x.ExternalTimestamp).IsRequired();
                ch.Property(x => x.ImportDate).IsRequired();
                ch.Property(x => x.ImportedByUserId).IsRequired();
                ch.Property(x => x.ProcessedDate);
                ch.Property(x => x.ProcessedByUserId).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("ProcessIdx") { Order = 1 }));
                ch.HasMany(x => x.Items).WithRequired(x => x.Message).HasForeignKey(x => x.EInvoiceFiMessageHeaderId);
                ch.HasMany(x => x.Actions).WithOptional(x => x.EInvoiceFiMessage).HasForeignKey(x => x.EInvoiceFiMessageHeaderId);
            });

            Cfg<EInvoiceFiMessageItem>(modelBuilder, ch =>
            {
                ch.HasKey(x => new { x.EInvoiceFiMessageHeaderId, x.Name });
                ch.Property(x => x.Name).HasMaxLength(128).IsRequired();
                ch.Property(x => x.Value).IsRequired();
                ch.Property(x => x.IsEncrypted).IsRequired();
            });

            Cfg<EInvoiceFiAction>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);

                ch.HasKey(x => x.Id);
                ch.Property(x => x.ActionName).HasMaxLength(128).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("ErrorListIdx") { Order = 1 }));
                ch.Property(x => x.ActionDate);
                ch.Property(x => x.ActionMessage);
                ch.Property(x => x.CreatedByUserId).IsRequired();
                ch.Property(x => x.HandledDate);
                ch.Property(x => x.HandledByUserId).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("ErrorListIdx") { Order = 2 }));
            });

            Cfg<CreditSecurityItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);

                ch.HasKey(x => x.Id);
                ch.Property(x => x.CreditNr).HasMaxLength(128).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("CurValUIdx") { Order = 1, IsUnique = true }));
                ch.Property(x => x.Name).HasMaxLength(128).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("CurValUIdx") { Order = 2, IsUnique = true }));
                ch.Property(x => x.CreatedByBusinessEventId).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("CurValUIdx") { Order = 3, IsUnique = true }));
                ch.Property(x => x.StringValue).IsRequired();
                ch.Property(x => x.NumericValue);
                ch.Property(x => x.DateValue).HasColumnType("date");
            });

            Cfg<CreditOutgoingDirectDebitItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);

                ch.HasKey(x => x.Id);
                ch.Property(x => x.Operation).HasMaxLength(128).IsRequired();
                ch.Property(x => x.BankAccountOwnerCustomerId);
                ch.Property(x => x.BankAccountNr).HasMaxLength(128);
                ch.Property(x => x.ClientBankGiroNr).HasMaxLength(128);
                ch.Property(x => x.PaymentNr).HasMaxLength(128);
            });

            Cfg<OutgoingDirectDebitStatusChangeFileHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);

                ch.HasKey(x => x.Id);
                ch.Property(x => x.ExternalId);
                ch.Property(x => x.FileArchiveKey).HasMaxLength(100);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.HasMany(x => x.CreditOutgoingDirectDebitItems).WithOptional(x => x.OutgoingDirectDebitStatusChangeFile).HasForeignKey(x => x.OutgoingDirectDebitStatusChangeFileHeaderId);
            });

            Cfg<IncomingDirectDebitStatusChangeFileHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);

                ch.HasKey(x => x.Id);
                ch.Property(x => x.FileArchiveKey).HasMaxLength(128);
                ch.Property(x => x.Filename).HasMaxLength(128);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
            });

            Cfg<KeyValueItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => new { x.Key, x.KeySpace });
                ch.Property(x => x.Key).IsRequired().HasMaxLength(128);
                ch.Property(x => x.KeySpace).IsRequired().HasMaxLength(128);
                ch.Property(x => x.Value);
            });

            Cfg<ReferenceInterestChangeHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.NewInterestRatePercent).IsRequired();
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.InitiatedDate).IsRequired();
                ch.Property(x => x.InitiatedByUserId).IsRequired();
            });

            Cfg<CreditCustomerListMember>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => new { x.CreditNr, x.CustomerId, x.ListName });
                ch.Property(x => x.CustomerId);
                ch.Property(x => x.CreditNr).HasMaxLength(128).IsRequired();
                ch.Property(x => x.ListName).HasMaxLength(128).IsRequired();
            });

            Cfg<CreditCustomerListOperation>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.CreditNr).HasMaxLength(128).IsRequired();
                ch.Property(x => x.CustomerId).IsRequired();
                ch.Property(x => x.ListName).HasMaxLength(128).IsRequired();
                ch.Property(x => x.IsAdd).IsRequired();
                ch.Property(x => x.OperationDate).IsRequired();
                ch.Property(x => x.ByUserId).IsRequired();
            });

            Cfg<CreditAnnualStatementHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => new { x.CreditNr, x.CustomerId, x.Year });
                ch.Property(x => x.StatementDocumentArchiveKey).HasMaxLength(100).IsRequired();
                ch.Property(x => x.CustomData);
            });

            Cfg<CollateralHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.CollateralType).HasMaxLength(128).IsRequired();
                ch.HasMany(x => x.Items).WithRequired(x => x.Collateral).HasForeignKey(x => x.CollateralHeaderId);
            });

            Cfg<CollateralItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ItemName).HasMaxLength(128).IsRequired();
                ch.Property(x => x.StringValue).IsRequired();
                ch.Property(x => x.NumericValue);
                ch.Property(x => x.DateValue);
            });

            Cfg<FixedMortgageLoanInterestRate>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.MonthCount);
                ch.Property(x => x.MonthCount).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
                ch.Property(x => x.RatePercent).IsRequired();
            });

            Cfg<HFixedMortgageLoanInterestRate>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.HasIndex(x => new { x.CreatedByBusinessEventId, x.MonthCount }).IsUnique();
                ch.Property(x => x.MonthCount).IsRequired();
                ch.Property(x => x.RatePercent).IsRequired();
            });

            Cfg<AlternatePaymentPlanHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.Property(x => x.MinCapitalizedDueDate).HasColumnType("date");
                ch.HasKey(x => x.Id);
                ch.HasMany(x => x.Months).WithRequired(x => x.PaymentPlan).HasForeignKey(x => x.AlternatePaymentPlanId);
            });

            Cfg<AlternatePaymentPlanMonth>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.DueDate).HasColumnType("date").IsRequired();
                ch.HasIndex(x => new { x.AlternatePaymentPlanId, x.DueDate }).IsUnique();
                ch.Property(x => x.MonthAmount).IsRequired();
                ch.Property(x => x.TotalAmount).IsRequired();
            });

            Cfg<SieFileTransaction>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.Property(x => x.AccountNr).HasMaxLength(128).IsRequired();
                ch.Property(x => x.Amount).IsRequired();
                ch.HasRequired(x => x.Verification).WithMany(x => x.Transactions).HasForeignKey(x => x.VerificationId);
            });

            Cfg<SieFileVerification>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.Property(x => x.Date).HasColumnType("date").IsRequired();
                ch.Property(x => x.RegistrationDate).HasColumnType("date").IsRequired();
                ch.Property(x => x.Text).IsRequired().HasMaxLength(256);
                ch.HasOptional(x => x.OutgoingFile).WithMany(x => x.SieFileVerifications).HasForeignKey(x => x.OutgoingBookkeepingFileHeaderId);
            });

            Cfg<SieFileConnection>(modelBuilder, ch =>
            {
                ch.HasKey(x => new { x.VerificationId, x.ConnectionType });
                ch.Property(x => x.ConnectionType).HasMaxLength(128).IsRequired();
                ch.Property(x => x.ConnectionId).HasMaxLength(128).IsRequired();
                ch.HasRequired(x => x.Verification).WithMany(x => x.Connections).HasForeignKey(x => x.VerificationId);
            });
        }

        private static void Cfg<T>(DbModelBuilder mb, Action<EntityTypeConfiguration<T>> a) where T : class
        {
            a(mb.Entity<T>());
        }

        public void RequireAmbientTransaction()
        {
            if (Database.CurrentTransaction == null)
            {
                throw new Exception("This methods writes directly to the database so it needs bo done in an ambient transaction.");
            }
        }
        public void BeginTransaction()
        {
            Database.BeginTransaction();
        }

        public void CommitTransaction()
        {
            Database.CurrentTransaction.Commit();
        }

        public void RollbackTransaction()
        {
            Database.CurrentTransaction.Rollback();
        }

        public bool HasCurrentTransaction => Database.CurrentTransaction != null;
        public IDbTransaction CurrentTransaction => Database?.CurrentTransaction?.UnderlyingTransaction;
        public IDbConnection GetConnection()
        {
            return Database?.Connection;
        }

        public bool IsChangeTrackingEnabled
        {
            get => Configuration.AutoDetectChangesEnabled;
            set => Configuration.AutoDetectChangesEnabled = value;
        }

        public void DetectChanges()
        {
            ChangeTracker.DetectChanges();
        }

        public static T RunWithExclusiveLock<T>(string lockName, Func<T> ifLockAquired, Func<T> ifAlreadyLocked, TimeSpan? waitForLock = null)
        {
            return NTechPerServiceExclusiveLock.RunWithExclusiveLock(lockName, ifLockAquired, ifAlreadyLocked, acquireTimeout: waitForLock);
        }

        public static void InitDatabase()
        {
            System.Data.Entity.Database.SetInitializer(new System.Data.Entity.MigrateDatabaseToLatestVersion<CreditContext, Migrations.Configuration>());
            using (var context = new CreditContext())
            {
                context.Database.Initialize(false);
                //TODO: When migrations are moved to the core side, also move the call to CreditContextSetup.AfterInitialize
                CreditContextSetup.AfterInitialize(context.Database.Connection);
            }
        }

        public static void OnSetup()
        {
            using (var context = new CreditContext())
            {
                context.Database.Initialize(true);
                //TODO: When migrations are moved to the core side, also move the call to CreditContextSetup.AfterInitialize
                CreditContextSetup.AfterInitialize(context.Database.Connection);
            }
        }
    }
}