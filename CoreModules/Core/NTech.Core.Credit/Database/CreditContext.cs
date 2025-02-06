using Microsoft.EntityFrameworkCore;
using nCredit;
using nCredit.DbModel.Model;
using NTech.Core.Credit.Shared.DbModel;
using NTech.Core.Module.Database;

namespace NTech.Core.Credit.Database
{
    public class CreditContext : NTechDbContext
    {
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

        public override string ConnectionStringName => "CreditContext";

        protected override void HandleCreate(ModelBuilder modelBuilder, LegacyEntityFrameworkHelper legacyHelper)
        {
            modelBuilder.Entity<CreditKeySequence>().HasKey(x => x.Id);
            modelBuilder.Entity<OcrPaymentReferenceNrSequence>().HasKey(x => x.Id);

            Cfg<CreditHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.CreditNr);
                ch.Property(x => x.CreditNr).HasMaxLength(128);
                ch.Property(x => x.CreditType).HasMaxLength(100).HasDefaultValueSql("'UnsecuredLoan'");
                ch.Property(x => x.NrOfApplicants).IsRequired();
                ch.Property(x => x.Status).IsRequired().HasMaxLength(100);
                ch.Property(x => x.ProviderName).IsRequired().HasMaxLength(100);
                ch.Property(x => x.StartDate).IsRequired();
                ch.HasIndex(x => x.StartDate);
                ch.Property(x => x.Status).HasDefaultValueSql("''");
                ch.Property(x => x.CreatedByBusinessEventId).HasDefaultValueSql("0");
                ch.HasMany(x => x.Transactions).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.Reminders).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr).IsRequired();
                ch.HasMany(x => x.TerminationLetters).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr).IsRequired();
                ch.HasMany(x => x.Notifications).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr).IsRequired();
                ch.HasMany(x => x.DatedCreditValues).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.DatedCreditStrings).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr).OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.DatedCreditCustomerValues).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.DatedCreditDates).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.CreditCustomers).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr).IsRequired();
                ch.HasMany(x => x.CreditFuturePaymentFreeMonths).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.CreditPaymentFreeMonths).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.Documents).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.TermsChanges).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr).IsRequired();
                ch.HasMany(x => x.CreditSettlementOffers).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr).IsRequired();
                ch.HasMany(x => x.EInvoiceFiActions).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr);
                ch.HasMany(x => x.SecurityItems).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.CreditOutgoingDirectDebitItems).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.CustomerListMembers).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr).IsRequired();
                ch.HasMany(x => x.CustomerListOperations).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr).IsRequired();
                ch.HasMany(x => x.AnnualStatements).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr).IsRequired();
                ch.HasOne(x => x.Collateral).WithMany(x => x.Credits);
                ch.HasMany(x => x.AlternatePaymentPlans).WithOne(x => x.Credit).HasForeignKey(x => x.CreditNr);
            });

            Cfg<CreditCustomer>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.CustomerId).IsRequired();
                ch.Property(x => x.ApplicantNr).IsRequired();
                ch.HasIndex(x => x.CustomerId).HasDatabaseName("CreditCustomerCustomerIdIdx");
            });

            Cfg<CreditComment>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                ConfigureInfrastructureFields(e);
                e.HasOne(x => x.Credit).WithMany(x => x.Comments).HasForeignKey(x => x.CreditNr).IsRequired();
                e.Property(x => x.CommentById).IsRequired();
                e.Property(x => x.CommentDate).IsRequired();
                e.Property(x => x.CommentText);
                e.Property(x => x.Attachment);
                e.Property(x => x.EventType).HasMaxLength(100);
                e
                    .HasIndex(x => new { x.EventType, x.CommentDate })
                    .IncludeProperties(x => new { x.CreditNr })
                    .HasDatabaseName("CommentTypePerfIdx1");
            });

            Cfg<BusinessEvent>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.EventType).IsRequired().HasMaxLength(100);
                ch.Property(x => x.EventDate).IsRequired();
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.BookKeepingDate).IsRequired().HasColumnType("date");
                ch.HasMany(x => x.Transactions).WithOne(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId).IsRequired();
                ch.HasMany(x => x.DatedCreditValues).WithOne(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId).IsRequired();
                ch.HasMany(x => x.DatedCreditCustomerValues).WithOne(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId).IsRequired();
                ch.HasMany(x => x.DatedCreditStrings).WithOne(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId).IsRequired();
                ch.HasMany(x => x.DatedCreditDates).WithOne(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId).IsRequired();
                ch.HasMany(x => x.RemovedDatedCreditDates).WithOne(x => x.RemovedByBusinessEvent).HasForeignKey(x => x.RemovedByBusinessEventId);
                ch.HasMany(x => x.SharedDatedValues).WithOne(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId).IsRequired();
                ch.HasMany(x => x.CreatedOutgoingPayments).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired();
                ch.HasMany(x => x.CreatedOutgoingPaymentFiles).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired();
                ch.HasMany(x => x.CreatedIncomingPaymentFiles).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired();
                ch.HasMany(x => x.CreatedCredits).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired();
                ch.HasMany(x => x.CreditPaymentFreeMonths).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired();
                ch.HasMany(x => x.CreatedCreditFuturePaymentFreeMonths).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired();
                ch.HasMany(x => x.CommitedCreditFuturePaymentFreeMonths).WithOne(x => x.CommitedByEvent).HasForeignKey(x => x.CommitedByEventBusinessEventId);
                ch.HasMany(x => x.CancelledCreditFuturePaymentFreeMonths).WithOne(x => x.CancelledByEvent).HasForeignKey(x => x.CancelledByBusinessEventId);
                ch.HasMany(x => x.StartedTermsChanges).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.CommitedTermsChanges).WithOne(x => x.CommitedByEvent).HasForeignKey(x => x.CommitedByEventId);
                ch.HasMany(x => x.CancelledTermsChanges).WithOne(x => x.CancelledByEvent).HasForeignKey(x => x.CancelledByEventId);
                ch.HasMany(x => x.AddedCreditTermsChangeItems).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId).IsRequired();
                ch.HasMany(x => x.StartedCreditSettlementOffers).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.CommitedCreditSettlementOffers).WithOne(x => x.CommitedByEvent).HasForeignKey(x => x.CommitedByEventId);
                ch.HasMany(x => x.CancelledCreditSettlementOffers).WithOne(x => x.CancelledByEvent).HasForeignKey(x => x.CancelledByEventId);
                ch.HasMany(x => x.CreatedEInvoiceFiMessageHeaders).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId).IsRequired();
                ch.HasMany(x => x.ConnectedEInvoiceFiActions).WithOne(x => x.ConnectBusinessEvent).HasForeignKey(x => x.ConnectedBusinessEventId);
                ch.HasMany(x => x.CreatedCreditSecurityItems).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired();
                ch.HasMany(x => x.CreatedCreditOutgoingDirectDebitItems).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId).IsRequired();
                ch.HasMany(x => x.CreatedOutgoingDirectDebitStatusChangeFileHeaders).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId).IsRequired();
                ch.HasMany(x => x.CreatedIncomingDirectDebitStatusChangeFileHeaders).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId).IsRequired();
                ch.HasMany(x => x.CreatedCreditComments).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId);
                ch.HasMany(x => x.CreatedReferenceInterestChangeHeaders).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired();
                ch.HasMany(x => x.CreatedCollaterals).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.CreatedCollateralItems).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.RemovedCollateralItems).WithOne(x => x.RemovedByEvent).HasForeignKey(x => x.RemovedByBusinessEventId);
                ch.HasMany(x => x.CreatedFixedMortgageLoanInterestRates).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired();
                ch.HasMany(x => x.CreatedHFixedMortgageLoanInterestRates).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired();
                ch.HasMany(x => x.InactivatedTerminationLetters).WithOne(x => x.InactivatedByBusinessEvent).HasForeignKey(x => x.InactivatedByBusinessEventId);
                ch.HasMany(x => x.CreatedAlternatePaymentPlanHeaders).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByEventId).IsRequired();
                ch.HasMany(x => x.CancelledAlternatePaymentPlanHeaders).WithOne(x => x.CancelledByEvent).HasForeignKey(x => x.CancelledByEventId);
                ch.HasMany(x => x.FullyPaidAlternatePaymentPlanHeaders).WithOne(x => x.FullyPaidByEvent).HasForeignKey(x => x.FullyPaidByEventId);
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
                ch.HasMany(x => x.Items).WithOne(x => x.OutgoingPayment).HasForeignKey(x => x.OutgoingPaymentId).IsRequired();
                ch.HasMany(x => x.Transactions).WithOne(x => x.OutgoingPayment).HasForeignKey(x => x.OutgoingPaymentId);
                ch.Property(x => x.CreatedByBusinessEventId).HasDefaultValueSql("0");
            });

            Cfg<OutgoingPaymentHeaderItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.Name).IsRequired().HasMaxLength(100);
                ch.HasIndex(x => x.Name);
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
                ch.HasMany(x => x.Payments).WithOne(x => x.IncomingPaymentFile).HasForeignKey(x => x.IncomingPaymentFileId);
            });

            Cfg<IncomingPaymentHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.BookKeepingDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.IsFullyPlaced).IsRequired();
                ch.HasMany(x => x.Transactions).WithOne(x => x.IncomingPayment).HasForeignKey(x => x.IncomingPaymentId);
                ch.HasMany(x => x.Items).WithOne(x => x.Payment).HasForeignKey(x => x.IncomingPaymentHeaderId).IsRequired();
                ch.Property(x => x.IsFullyPlaced).HasDefaultValueSql("0");
            });

            Cfg<IncomingPaymentHeaderItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.Name).IsRequired().HasMaxLength(100);
                ch.HasIndex(x => x.Name);
                ch.Property(x => x.Value).IsRequired();
                ch.Property(x => x.IsEncrypted).IsRequired();
            });

            Cfg<WriteoffHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.BookKeepingDate).IsRequired().HasColumnType("date");
                ch.HasMany(x => x.Transactions).WithOne(x => x.Writeoff).HasForeignKey(x => x.WriteoffId);
            });

            Cfg<CreditReminderHeader>(modelBuilder, r =>
            {
                ConfigureInfrastructureFields(r);
                r.HasKey(x => x.Id);
                r.Property(x => x.ReminderNumber).IsRequired().HasDefaultValueSql("0");
                r.Property(x => x.ReminderDate).IsRequired().HasColumnType("date");
                r.Property(x => x.InternalDueDate).IsRequired().HasColumnType("date").HasDefaultValueSql("'1900-01-01T00:00:00.000'");
                r.HasMany(x => x.Documents).WithOne(x => x.Reminder).HasForeignKey(x => x.CreditReminderHeaderId);
                r.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                r.Property(x => x.BookKeepingDate).IsRequired().HasColumnType("date");
                r.HasMany(x => x.Transactions).WithOne(x => x.Reminder).HasForeignKey(x => x.ReminderId);
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
                ci.Property(x => x.OcrPaymentReference).IsRequired().HasMaxLength(100).HasDefaultValueSql("''");
                ci.Property(x => x.CoNotificationId).HasMaxLength(100);
                ci.Property(x => x.IsCoNotificationMaster);
                ci.HasMany(x => x.Transactions).WithOne(x => x.CreditNotification).HasForeignKey(x => x.CreditNotificationId);
                ci.HasMany(x => x.Reminders).WithOne(x => x.Notification).HasForeignKey(x => x.NotificationId).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ci
                    .HasIndex(x => new { x.ClosedTransactionDate, x.Id, x.CreditNr, x.DueDate })
                    .HasDatabaseName("_dta_index_CreditNotificationHeader_7_341576255__K12_K1_K2_K4");
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
                c
                    .HasIndex(x => new { x.CreditNr, x.Name, x.TransactionDate, x.Timestamp })
                    .IncludeProperties(x => new { x.Value })
                    .HasDatabaseName("DatedCreditStringPerfIdx1");
                c.HasIndex(x => x.CreditNr);
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
                r
                    .HasIndex(x => new { x.AccountCode, x.CreditNotificationId, x.Id, x.BusinessEventId })
                    .IncludeProperties(x => x.Amount)
                    .HasDatabaseName("_dta_index_AccountTransaction_7_405576483__K2_K3_K1_K4_9");
                r
                    .HasIndex(x => new { x.AccountCode, x.CreditNr, x.TransactionDate })
                    .IncludeProperties(x => new { x.Amount })
                    .HasDatabaseName("IX_CreditBalance");
                r
                    .HasIndex(x => new { x.CreditNotificationId, x.BusinessEventId })
                    .IncludeProperties(x => new { x.TransactionDate })
                    .HasDatabaseName("IX_CreditNotificationId2");
                r
                    .HasIndex(x => new { x.AccountCode, x.CreditNotificationId, x.TransactionDate })
                    .IncludeProperties(x => new { x.Amount })
                    .HasDatabaseName("IX_NotificationBalance");
                r.HasIndex(x => x.CreditNotificationId).HasDatabaseName("IX_CreditNotificationId");

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
                ch.HasMany(x => x.Transactions).WithOne(x => x.OutgoingBookkeepingFile).HasForeignKey(x => x.OutgoingBookkeepingFileHeaderId);
            });

            Cfg<OutgoingCreditNotificationDeliveryFileHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ExternalId);
                ch.Property(x => x.FileArchiveKey).HasMaxLength(100);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.HasMany(x => x.Notifications).WithOne(x => x.DeliveryFile).HasForeignKey(x => x.OutgoingCreditNotificationDeliveryFileHeaderId);
                ch.HasOne(x => x.CreatedByEvent).WithMany(x => x.CreatedNotificationDeliveryFiles).HasForeignKey(x => x.BusinessEvent_Id);
            });

            Cfg<OutgoingCreditReminderDeliveryFileHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ExternalId);
                ch.Property(x => x.FileArchiveKey).HasMaxLength(100);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.HasMany(x => x.Reminders).WithOne(x => x.DeliveryFile).HasForeignKey(x => x.OutgoingCreditReminderDeliveryFileHeaderId);
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
                fh.HasMany(x => x.AnnualStatements).WithOne(x => x.OutgoingExportFile).HasForeignKey(x => x.OutgoingExportFileHeaderId);
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
                e.Property(x => x.Key).IsRequired().HasMaxLength(100);
                e.HasIndex(x => x.Key);
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
                r.HasMany(x => x.Documents).WithOne(x => x.TerminationLetter).HasForeignKey(x => x.CreditTerminationLetterHeaderId);
                r.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                r.Property(x => x.BookKeepingDate).IsRequired().HasColumnType("date");
                r
                    .HasIndex(x => new { x.CreditNr, x.DueDate })
                    .IncludeProperties(x => new { x.Id })
                    .HasDatabaseName("_dta_index_CreditTerminationLetterHeader_7_338100245__K6_K4_1");
                r.HasIndex(x => x.CreditNr);
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
                ch.HasMany(x => x.TerminationLetters).WithOne(x => x.DeliveryFile).HasForeignKey(x => x.OutgoingCreditTerminationLetterDeliveryFileHeaderId);
            });

            Cfg<OutgoingDebtCollectionFileHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ExternalId).IsRequired().HasMaxLength(128);
                ch.HasIndex(x => x.ExternalId);
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
                ch.HasMany(x => x.Transactions).WithOne(x => x.PaymentFreeMonth).HasForeignKey(x => x.CreditPaymentFreeMonthId);
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
                ch.Property(x => x.AutoExpireDate).HasColumnType("datetime");
                ch.HasMany(x => x.Items).WithOne(x => x.CreditTermsChange).HasForeignKey(x => x.CreditTermsChangeHeaderId).IsRequired().OnDelete(DeleteBehavior.NoAction);
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
                ch.HasMany(x => x.Items).WithOne(x => x.CreditSettlementOffer).HasForeignKey(x => x.CreditSettlementOfferHeaderId).IsRequired().OnDelete(DeleteBehavior.NoAction);
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
                ch.HasIndex(x => x.TheDate).HasDatabaseName("CalendarDateAscIdx");
            });

            Cfg<WorkListHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);

                ch.HasKey(x => x.Id);
                ch.Property(x => x.ListType).IsRequired().HasMaxLength(128);
                ch.Property(x => x.CreationDate).IsRequired().HasColumnType("datetime");
                ch.Property(x => x.CreatedByUserId).IsRequired();
                ch.Property(x => x.ClosedByUserId);
                ch.Property(x => x.ClosedDate).HasColumnType("datetime");
                ch.Property(x => x.CustomData);
                ch.Property(x => x.IsUnderConstruction).IsRequired();
                ch.HasMany(x => x.Items).WithOne(x => x.WorkList).HasForeignKey(x => x.WorkListHeaderId).IsRequired();
                ch.HasMany(x => x.FilterItems).WithOne(x => x.WorkList).HasForeignKey(x => x.WorkListHeaderId).IsRequired();
            });

            Cfg<WorkListItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);

                ch.HasKey(x => new { x.WorkListHeaderId, x.ItemId });
                ch.Property(x => x.ItemId).IsRequired().HasMaxLength(128);
                ch.Property(x => x.WorkListHeaderId).IsRequired();
                ch.Property(x => x.OrderNr).IsRequired();
                ch.HasIndex(x => new { x.WorkListHeaderId, x.OrderNr }).HasDatabaseName("OrderUIdx").IsUnique();
                ch.Property(x => x.TakenByUserId);
                ch.Property(x => x.TakenDate).HasColumnType("datetime");
                ch.Property(x => x.CompletedDate).HasColumnType("datetime");
                ch.HasMany(x => x.Properties).WithOne(x => x.Item).HasForeignKey(x => new { x.WorkListHeaderId, x.ItemId }).IsRequired();
            });

            Cfg<WorkListItemProperty>(modelBuilder, ch =>
            {
                ch.HasKey(x => new { x.WorkListHeaderId, x.ItemId, x.Name });
                ch.Property(x => x.ItemId).IsRequired().HasMaxLength(128);
                ch.Property(x => x.WorkListHeaderId).IsRequired();
                ch.Property(x => x.IsEncrypted).IsRequired();
                ch.Property(x => x.Name).IsRequired().HasMaxLength(128);
                ch.Property(x => x.Value).HasMaxLength(128); //Not required since null is a reasonable value to want to store
                ch.HasIndex(x => new { x.WorkListHeaderId, x.ItemId }).HasDatabaseName("IX_WorkListHeaderId_ItemId");
            });

            Cfg<WorkListFilterItem>(modelBuilder, ch =>
            {
                ch.HasKey(x => new { x.WorkListHeaderId, x.Name });
                ch.Property(x => x.WorkListHeaderId).IsRequired();
                ch.Property(x => x.Name).IsRequired().HasMaxLength(128);
                ch.Property(x => x.Value).IsRequired().HasMaxLength(128);
                ch.HasIndex(x => x.WorkListHeaderId);
            });

            Cfg<EInvoiceFiMessageHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ExternalMessageType).IsRequired().HasMaxLength(128);
                ch.Property(x => x.ExternalMessageId).IsRequired().HasMaxLength(128);
                ch.Property(x => x.ExternalTimestamp).IsRequired();
                ch.Property(x => x.ImportDate).IsRequired().HasColumnType("datetime");
                ch.Property(x => x.ImportedByUserId).IsRequired();
                ch.Property(x => x.ProcessedDate).HasColumnType("datetime");
                ch.Property(x => x.ProcessedByUserId);
                ch.HasIndex(x => x.ProcessedByUserId).HasDatabaseName("ProcessIdx");
                ch.HasMany(x => x.Items).WithOne(x => x.Message).HasForeignKey(x => x.EInvoiceFiMessageHeaderId).IsRequired();
                ch.HasMany(x => x.Actions).WithOne(x => x.EInvoiceFiMessage).HasForeignKey(x => x.EInvoiceFiMessageHeaderId);
            });

            Cfg<EInvoiceFiMessageItem>(modelBuilder, ch =>
            {
                ch.HasKey(x => new { x.EInvoiceFiMessageHeaderId, x.Name });
                ch.Property(x => x.Name).HasMaxLength(128).IsRequired();
                ch.Property(x => x.Value).IsRequired();
                ch.Property(x => x.IsEncrypted).IsRequired();
                ch.HasIndex(x => x.EInvoiceFiMessageHeaderId);
            });

            Cfg<EInvoiceFiAction>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);

                ch.HasKey(x => x.Id);
                ch.Property(x => x.ActionName).HasMaxLength(128).IsRequired();
                ch.Property(x => x.ActionDate).HasColumnType("datetime");
                ch.Property(x => x.ActionMessage);
                ch.Property(x => x.CreatedByUserId).IsRequired();
                ch.Property(x => x.HandledDate).HasColumnType("datetime");
                ch.Property(x => x.HandledByUserId);
                ch.HasIndex(x => new { x.ActionName, x.HandledByUserId }).HasDatabaseName("ErrorListIdx");
            });

            Cfg<CreditSecurityItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);

                ch.HasKey(x => x.Id);
                ch.Property(x => x.CreditNr).HasMaxLength(128).IsRequired();
                ch.Property(x => x.Name).HasMaxLength(128).IsRequired();
                ch.Property(x => x.CreatedByBusinessEventId).IsRequired();
                ch.Property(x => x.StringValue).IsRequired();
                ch.Property(x => x.NumericValue);
                ch.Property(x => x.DateValue).HasColumnType("date");
                ch.HasIndex(x => new { x.CreditNr, x.Name, x.CreatedByBusinessEventId }).HasDatabaseName("CurValUIdx").IsUnique();
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
                ch.HasMany(x => x.CreditOutgoingDirectDebitItems).WithOne(x => x.OutgoingDirectDebitStatusChangeFile).HasForeignKey(x => x.OutgoingDirectDebitStatusChangeFileHeaderId);
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
                ch.Property(x => x.InitiatedDate).IsRequired().HasColumnType("datetime");
                ch.Property(x => x.InitiatedByUserId).IsRequired();
            });

            Cfg<CreditCustomerListMember>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => new { x.CreditNr, x.CustomerId, x.ListName });
                ch.Property(x => x.CustomerId);
                ch.Property(x => x.CreditNr).HasMaxLength(128).IsRequired();
                ch.Property(x => x.ListName).HasMaxLength(128).IsRequired();
                ch.HasIndex(x => x.CreditNr);
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
                ch.HasIndex(x => x.CreditNr);
            });

            Cfg<CollateralHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.CollateralType).HasMaxLength(128).IsRequired();
                ch.HasMany(x => x.Items).WithOne(x => x.Collateral).HasForeignKey(x => x.CollateralHeaderId).IsRequired();
            });

            Cfg<CollateralItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.ItemName).HasMaxLength(128).IsRequired();
                ch.Property(x => x.StringValue).IsRequired();
                ch.Property(x => x.NumericValue);
                ch.Property(x => x.DateValue).HasColumnType("datetime");
            });

            Cfg<FixedMortgageLoanInterestRate>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.MonthCount);
                ch.Property(x => x.MonthCount).ValueGeneratedNever();
                ch.Property(x => x.RatePercent).IsRequired();
            });

            Cfg<HFixedMortgageLoanInterestRate>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.HasIndex(x => new { x.CreatedByBusinessEventId, x.MonthCount }).IsUnique().HasDatabaseName("IX_CreatedByBusinessEventId_MonthCount");
                ch.Property(x => x.MonthCount).IsRequired();
                ch.Property(x => x.RatePercent).IsRequired();
            });

            Cfg<AlternatePaymentPlanHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.HasMany(x => x.Months).WithOne(x => x.PaymentPlan).HasForeignKey(x => x.AlternatePaymentPlanId).IsRequired();
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
                ch.HasOne(x => x.Verification).WithMany(x => x.Transactions).HasForeignKey(x => x.VerificationId).IsRequired();
            });

            Cfg<SieFileVerification>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.Property(x => x.Date).HasColumnType("date").IsRequired();
                ch.Property(x => x.RegistrationDate).HasColumnType("date").IsRequired();
                ch.Property(x => x.Text).IsRequired().HasMaxLength(256);
                ch.HasOne(x => x.OutgoingFile).WithMany(x => x.SieFileVerifications).HasForeignKey(x => x.OutgoingBookkeepingFileHeaderId);
            });

            Cfg<SieFileConnection>(modelBuilder, ch =>
            {
                ch.HasKey(x => new { x.VerificationId, x.ConnectionType });
                ch.Property(x => x.ConnectionType).HasMaxLength(128).IsRequired();
                ch.Property(x => x.ConnectionId).HasMaxLength(128).IsRequired();
                ch.HasOne(x => x.Verification).WithMany(x => x.Connections).HasForeignKey(x => x.VerificationId).IsRequired();
            });
        }

        /*
         * Only used from the integration tests
         * 
         * Because ef core is so weak we cant express desending indexes (it will be added in ef core 7)
         * so we need this script to have the integration test database mirror the real one.
         * 
         * Either when ef core 7 is out or we move migrations to the core side this should be folded into that
         * 
         */
        public static void TempMigrate(CreditContext context)
        {
            context.Database.ExecuteSqlRaw(@"CREATE NONCLUSTERED INDEX [CalendarDateDescIdx] ON [dbo].[CalendarDate]([TheDate] DESC)");
            context.Database.ExecuteSqlRaw(@"CREATE NONCLUSTERED INDEX [_dta_index_DatedCreditDate_7_466100701__K7D_K6_1_2_3_11]
    ON [dbo].[DatedCreditDate]([Timestamp] DESC, [Value] ASC)
    INCLUDE([Id], [CreditNr], [Name], [RemovedByBusinessEventId])");
        }
    }
}