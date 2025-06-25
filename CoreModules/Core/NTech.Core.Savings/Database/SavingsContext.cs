using Microsoft.EntityFrameworkCore;
using NTech.Core.Module.Database;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFixed;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace NTech.Core.Savings.Database
{
    public partial class SavingsContext : NTechDbContext
    {
        public override string ConnectionStringName => "SavingsContext";

        public static bool IsConcurrencyException(Exception ex) => ex.GetType().FullName == typeof(DbUpdateConcurrencyException).FullName;

        public virtual DbSet<EncryptedValue> EncryptedValues { get; set; }
        public virtual DbSet<BusinessEvent> BusinessEvents { get; set; }
        public virtual DbSet<DatedSavingsAccountValue> DatedSavingsAccountValues { get; set; }
        public virtual DbSet<SharedDatedValue> SharedDatedValues { get; set; }
        public virtual DbSet<DatedSavingsAccountString> DatedSavingsAccountStrings { get; set; }
        public virtual DbSet<LedgerAccountTransaction> LedgerAccountTransactions { get; set; }
        public virtual DbSet<SavingsAccountComment> SavingsAccountComments { get; set; }
        public virtual DbSet<SavingsAccountKeySequence> SavingsAccountKeySequences { get; set; }
        public virtual DbSet<SavingsAccountHeader> SavingsAccountHeaders { get; set; }
        public virtual DbSet<OcrPaymentReferenceNrSequence> OcrPaymentReferenceNrSequences { get; set; }
        public virtual DbSet<SavingsAccountCreationRemark> SavingsAccountCreationRemarks { get; set; }
        public virtual DbSet<SharedSavingsInterestRate> SharedSavingsInterestRates { get; set; }
        public virtual DbSet<IncomingPaymentFileHeader> IncomingPaymentFileHeaders { get; set; }
        public virtual DbSet<IncomingPaymentHeader> IncomingPaymentHeaders { get; set; }
        public virtual DbSet<IncomingPaymentHeaderItem> IncomingPaymentHeaderItems { get; set; }
        public virtual DbSet<SavingsAccountInterestCapitalization> SavingsAccountInterestCapitalizations { get; set; }
        public virtual DbSet<TemporaryExternallyEncryptedItem> TemporaryExternallyEncryptedItems { get; set; }
        public virtual DbSet<OutgoingPaymentHeader> OutgoingPaymentHeaders { get; set; }
        public virtual DbSet<OutgoingPaymentFileHeader> OutgoingPaymentFileHeaders { get; set; }
        public virtual DbSet<OutgoingPaymentHeaderItem> OutgoingPaymentHeaderItems { get; set; }
        public virtual DbSet<OutgoingBookkeepingFileHeader> OutgoingBookkeepingFileHeaders { get; set; }
        public virtual DbSet<DailyKycScreenHeader> DailyKycScreenHeaders { get; set; }
        public virtual DbSet<SavingsAccountWithdrawalAccountChange> SavingsAccountWithdrawalAccountChanges { get; set; }
        public virtual DbSet<SavingsAccountKycQuestion> SavingsAccountKycQuestions { get; set; }
        public virtual DbSet<SavingsAccountDocument> SavingsAccountDocuments { get; set; }
        public virtual DbSet<OutgoingAmlMonitoringExportFileHeader> OutgoingAmlMonitoringExportFileHeaders { get; set; }
        public virtual DbSet<SystemItem> SystemItems { get; set; }
        public virtual DbSet<CalendarDate> CalendarDates { get; set; }
        public virtual DbSet<SharedSavingsInterestRateChangeHeader> SharedSavingsInterestRateChangeHeaders { get; set; }
        public virtual DbSet<KeyValueItem> KeyValueItems { get; set; }
        public virtual DbSet<OutgoingExportFileHeader> OutgoingExportFileHeaders { get; set; }

        public static void TempMigrate(SavingsContext context)
        {
            context.Database.ExecuteSqlRaw(@"CREATE NONCLUSTERED INDEX [CalendarDateDescIdx] ON [dbo].[CalendarDate]([TheDate] DESC)");
            context.Database.ExecuteSqlRaw(@"CREATE NONCLUSTERED INDEX [DatedAccountStrPerfIdx1]
                ON [dbo].[DatedSavingsAccountString]([TransactionDate] DESC, [Timestamp] DESC)
                INCLUDE([SavingsAccountNr], [Name], [ChangedDate])");
            context.Database.ExecuteSqlRaw(@"CREATE NONCLUSTERED INDEX [DatedAccountStrPerfIdx2]
                ON [dbo].[DatedSavingsAccountString]([BusinessEventId] DESC, [Value] ASC)
                INCLUDE([SavingsAccountNr], [Name])");
            context.Database.ExecuteSqlRaw(@"CREATE NONCLUSTERED INDEX [DatedSavingsAccountStringPerfIdx1]
                ON [dbo].[DatedSavingsAccountString]([SavingsAccountNr] ASC, [BusinessEventId] ASC, [Id] ASC, [Name] ASC, [Value] ASC, [TransactionDate] ASC)");
            context.Database.ExecuteSqlRaw(@"CREATE NONCLUSTERED INDEX [LedgerAccountTransactionPerfIdx1]
                ON [dbo].[LedgerAccountTransaction]([Timestamp] DESC)
                INCLUDE([AccountCode], [SavingsAccountNr])");
            context.Database.ExecuteSqlRaw(@"CREATE NONCLUSTERED INDEX [LedgerAccountTransactionPerfIdx2]
                ON [dbo].[LedgerAccountTransaction]([AccountCode] ASC, [SavingsAccountNr] ASC)
                INCLUDE([Amount])");
            context.Database.ExecuteSqlRaw(@"CREATE NONCLUSTERED INDEX [LedgerAccountTransactionPerfIdx3]
                ON [dbo].[LedgerAccountTransaction]([AccountCode] ASC, [SavingsAccountNr] ASC, [TransactionDate] ASC)
                INCLUDE([Amount])");
            context.Database.ExecuteSqlRaw(@"CREATE UNIQUE NONCLUSTERED INDEX [OutgoingPaymentHeader_UniqueToken_UIdx]
                ON [dbo].[OutgoingPaymentHeader]([UniqueToken] ASC) WHERE ([UniqueToken] IS NOT NULL)");
            context.Database.ExecuteSqlRaw(@"CREATE NONCLUSTERED INDEX [SavingsAccountHeaderPerfIdx1]
                ON [dbo].[SavingsAccountHeader]([SavingsAccountNr] ASC)
                INCLUDE([MainCustomerId], [Status], [Timestamp])");
            context.Database.ExecuteSqlRaw(@"CREATE NONCLUSTERED INDEX [SavingsAccountHeaderPerfIdx2]
                ON [dbo].[SavingsAccountHeader]([CreatedByBusinessEventId] ASC)
                INCLUDE([SavingsAccountNr], [Status], [ChangedDate])");
        }

        protected override void HandleCreate(ModelBuilder modelBuilder, LegacyEntityFrameworkHelper legacyHelper)
        {
            Cfg<EncryptedValue>(modelBuilder, ev =>
            {
                ev.HasKey(x => x.Id);
                ev.Property(x => x.EncryptionKeyName).IsRequired().HasMaxLength(100);
                ev.Property(e => e.Timestamp).IsRequired().IsRowVersion();
                ev.Property(e => e.CreatedById).IsRequired();
                ev.Property(e => e.CreatedDate).IsRequired();
                ev.Property(x => x.Value).IsRequired();
            });

            Cfg<BusinessEvent>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);

                ch.HasKey(x => x.Id);
                ch.Property(x => x.EventType).IsRequired().HasMaxLength(100);
                ch.Property(x => x.EventDate).IsRequired();
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.HasMany(x => x.CreatedLedgerTransactions).WithOne(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId).IsRequired();
                ch.HasMany(x => x.CreatedSavingsAccounts).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.CreatedDatedSavingsAccountStrings).WithOne(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.CreatedDatedSavingsAccountValues).WithOne(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.CreatedSharedDatedValues).WithOne(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId).IsRequired();
                ch.HasMany(x => x.CreatedSavingsAccountCreationRemarks).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.CreatedSharedSavingsInterestRates).WithOne(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId).IsRequired();
                ch.HasMany(x => x.CreatedIncomingPaymentFiles).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired();
                ch.HasMany(x => x.CreatedSavingsAccountInterestCapitalizations).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.CreatedOutgoingPayments).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired();
                ch.HasMany(x => x.CreatedOutgoingPaymentFiles).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired();
                ch.HasMany(x => x.InitiatedSavingsAccountWithdrawalAccountChanges).WithOne(x => x.InitiatedByEvent).HasForeignKey(x => x.InitiatedByBusinessEventId).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.CreatedSavingsAccountKycQuestions).WithOne(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.CreatedSavingsAccountDocuments).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId).IsRequired().OnDelete(DeleteBehavior.NoAction);
                ch.HasMany(x => x.SharedSavingsInterestRateChangeRemovals).WithOne(x => x.RemovedByBusinessEvent).HasForeignKey(x => x.RemovedByBusinessEventId);
                ch.HasMany(x => x.SharedSavingsInterestRateAppliesToAccountsSinces).WithOne(x => x.AppliesToAccountsSinceBusinessEvent).HasForeignKey(x => x.AppliesToAccountsSinceBusinessEventId);
                ch.HasMany(x => x.CreatedSharedSavingsInterestRateChangeHeaders).WithOne(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId).IsRequired();
                ch.HasMany(x => x.CommittedOrCancelledSavingsAccountWithdrawalAccountChanges).WithOne(x => x.CommitedOrCancelledByEvent).HasForeignKey(x => x.CommitedOrCancelledByEventId);
                ch.HasIndex(x => x.Id).IncludeProperties("TransactionDate").HasDatabaseName("BusinessEventPerfIdx1");
            });

            Cfg<DatedSavingsAccountValue>(modelBuilder, c =>
            {
                ConfigureInfrastructureFields(c);

                c.HasKey(x => x.Id);
                c.Property(x => x.SavingsAccountNr).HasMaxLength(128);
                c.Property(x => x.Name).IsRequired().HasMaxLength(100);
                c.Property(x => x.Value).IsRequired();
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

            Cfg<DatedSavingsAccountString>(modelBuilder, c =>
            {
                ConfigureInfrastructureFields(c);
                c.HasKey(x => x.Id);
                c.Property(x => x.SavingsAccountNr).HasMaxLength(128);
                c.Property(x => x.Name).IsRequired().HasMaxLength(100);
                c.Property(x => x.Value).IsRequired().HasMaxLength(100); //NOTE: Dont make this longer if longer values appear. Put them somewhere else. This needs to be indexable
                c.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
            });

            Cfg<LedgerAccountTransaction>(modelBuilder, r =>
            {
                ConfigureInfrastructureFields(r);
                r.HasKey(x => x.Id);
                r.Property(x => x.AccountCode).IsRequired().HasMaxLength(100);
                r.Property(x => x.Amount).IsRequired();
                r.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                r.Property(x => x.BookKeepingDate).IsRequired().HasColumnType("date");
                r.Property(x => x.InterestFromDate).IsRequired().HasColumnType("date");
                r.Property(x => x.BusinessEventRoleCode).HasMaxLength(100);
            });

            Cfg<SavingsAccountComment>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.Id);
                e.HasOne(x => x.SavingsAccount).WithMany(x => x.Comments).HasForeignKey(x => x.SavingsAccountNr).IsRequired();
                e.Property(x => x.CommentById).IsRequired();
                e.Property(x => x.CommentDate).IsRequired();
                e.Property(x => x.CommentText);
                e.Property(x => x.Attachment);
                e.Property(x => x.EventType).HasMaxLength(100);
            });

            modelBuilder.Entity<SavingsAccountKeySequence>().HasKey(x => x.Id);
            modelBuilder.Entity<OcrPaymentReferenceNrSequence>().HasKey(x => x.Id);

            Cfg<SavingsAccountHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.SavingsAccountNr);
                ch.Property(x => x.SavingsAccountNr).HasMaxLength(128);
                ch.Property(x => x.AccountTypeCode).IsRequired().HasMaxLength(100).HasDefaultValueSql("''");
                ch.Property(x => x.Status).IsRequired().HasMaxLength(100);
                ch.Property(x => x.MainCustomerId).IsRequired();
                ch.HasMany(x => x.Transactions).WithOne(x => x.SavingsAccount).HasForeignKey(x => x.SavingsAccountNr);
                ch.HasMany(x => x.DatedValues).WithOne(x => x.SavingsAccount).HasForeignKey(x => x.SavingsAccountNr).IsRequired();
                ch.HasMany(x => x.DatedStrings).WithOne(x => x.SavingsAccount).HasForeignKey(x => x.SavingsAccountNr).IsRequired();
                ch.HasMany(x => x.Comments).WithOne(x => x.SavingsAccount).HasForeignKey(x => x.SavingsAccountNr).IsRequired();
                ch.HasMany(x => x.CreationRemarks).WithOne(x => x.SavingsAccount).HasForeignKey(x => x.SavingsAccountNr).IsRequired();
                ch.HasMany(x => x.SavingsAccountInterestCapitalizations).WithOne(x => x.SavingsAccount).HasForeignKey(x => x.SavingsAccountNr).IsRequired();
                ch.HasMany(x => x.SavingsAccountWithdrawalAccountChanges).WithOne(x => x.SavingsAccount).HasForeignKey(x => x.SavingsAccountNr).IsRequired();
                ch.HasMany(x => x.SavingsAccountKycQuestions).WithOne(x => x.SavingsAccount).HasForeignKey(x => x.SavingsAccountNr).IsRequired();
            });

            Cfg<SavingsAccountCreationRemark>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.RemarkCategoryCode).IsRequired().HasMaxLength(100);
                ch.Property(x => x.RemarkData);
            });

            Cfg<SharedSavingsInterestRate>(modelBuilder, c =>
            {
                ConfigureInfrastructureFields(c);
                c.HasKey(x => x.Id);
                c.Property(x => x.AccountTypeCode).IsRequired().HasMaxLength(100);
                c.Property(x => x.InterestRatePercent).IsRequired();
                c.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                c.Property(x => x.ValidFromDate).IsRequired().HasColumnType("date");
                c.HasMany(x => x.AllAccountsHeaders).WithOne(x => x.AllAccountsRate).HasForeignKey(x => x.AllAccountsRateId);
                c.HasMany(x => x.NewAccountsHeaders).WithOne(x => x.NewAccountsOnlyRate).HasForeignKey(x => x.NewAccountsOnlyRateId);
            });

            Cfg<IncomingPaymentFileHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
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

            Cfg<SavingsAccountInterestCapitalization>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.HasIndex(x => new { x.SavingsAccountNr, x.ToDate }).IsUnique().HasDatabaseName("InterestCapitalizationDuplicateGuardIndex");
                ch.Property(x => x.SavingsAccountNr);
                ch.Property(x => x.FromDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.ToDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.CalculationDetailsDocumentArchiveKey).HasMaxLength(100);
            });

            Cfg<SavingsAccountWithdrawalAccountChange>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.PowerOfAttorneyDocumentArchiveKey).HasMaxLength(100);
                ch.Property(x => x.NewWithdrawalIban).IsRequired().HasMaxLength(100).HasDefaultValueSql("''");
                ch.Property(x => x.InitiatedByBusinessEventId).HasDefaultValueSql("(0)");
            });

            Cfg<TemporaryExternallyEncryptedItem>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.Property(x => x.Id).HasMaxLength(128);
                ch.Property(x => x.CipherText).IsRequired();
                ch.Property(x => x.ProtocolVersionName).IsRequired().HasMaxLength(100);
                ch.Property(x => x.DeleteAfterDate).IsRequired().HasColumnType("datetime");
                ch.Property(x => x.AddedDate).IsRequired().HasColumnType("datetime");
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
                ch.Property(x => x.UniqueToken).HasMaxLength(100);
                ch.HasMany(x => x.Items).WithOne(x => x.OutgoingPayment).HasForeignKey(x => x.OutgoingPaymentId).IsRequired();
                ch.HasMany(x => x.Transactions).WithOne(x => x.OutgoingPayment).HasForeignKey(x => x.OutgoingPaymentId);
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

            Cfg<DailyKycScreenHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.NrOfCustomersScreened).IsRequired();
                ch.Property(x => x.NrOfCustomersConflicted).IsRequired();
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.ResultModel).IsRequired();
            });

            Cfg<SavingsAccountKycQuestion>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.Property(x => x.Name).HasMaxLength(100).IsRequired();
                ch.Property(x => x.Group).HasMaxLength(100).IsRequired();
                ch.Property(x => x.Value).IsRequired();
                ch.HasKey(x => x.Id);
            });

            Cfg<SavingsAccountDocument>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.Id);
                e.HasOne(x => x.SavingsAccount).WithMany(x => x.Documents).HasForeignKey(x => x.SavingsAccountNr).IsRequired();
                e.Property(x => x.DocumentDate).IsRequired();
                e.Property(x => x.DocumentArchiveKey).HasMaxLength(100);
                e.Property(x => x.DocumentType).IsRequired().HasMaxLength(100);
                e.Property(x => x.DocumentData);
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

            Cfg<SystemItem>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.Id);
                e.Property(x => x.Key).IsRequired().HasMaxLength(100);
                e.HasIndex(x => x.Key);
                e.Property(x => x.Value);
            });

            Cfg<CalendarDate>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.TheDate);
                ch.Property(x => x.TheDate).HasColumnType("date").IsRequired();
                ch.HasIndex(x => x.TheDate).HasDatabaseName("CalendarDateAscIdx");
            });

            Cfg<SharedSavingsInterestRateChangeHeader>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.Id);
                e.Property(x => x.InitiatedAndCreatedByUserId).IsRequired();
                e.Property(x => x.InitiatedDate).IsRequired();
                e.Property(x => x.CreatedDate).IsRequired();
                e.Property(x => x.VerifiedByUserId).IsRequired();
                e.Property(x => x.VerifiedDate).IsRequired();
            });

            Cfg<KeyValueItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => new { x.Key, x.KeySpace });
                ch.Property(x => x.Key).IsRequired().HasMaxLength(128);
                ch.Property(x => x.KeySpace).IsRequired().HasMaxLength(128);
                ch.Property(x => x.Value);
            });

            Cfg<OutgoingExportFileHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.FileArchiveKey).HasMaxLength(100);
                ch.Property(x => x.FileType).HasMaxLength(100);
                ch.Property(x => x.ExportResultStatus);
                ch.Property(x => x.CustomData);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
            });
        }

        public IQueryable<FixedAccountProduct> FixedAccountProductQueryable => null;
    }
}