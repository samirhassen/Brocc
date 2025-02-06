using NTech.Core.Module.Shared.Database;
using NTech.Services.Infrastructure;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;

namespace nSavings
{
    public partial class SavingsContext : DbModel.ChangeTrackingDbContext
    {
        private const string ConnectionStringName = "SavingsContext";

        public static bool IsConcurrencyException(Exception ex) => ex.GetType().FullName == typeof(DbUpdateConcurrencyException).FullName;

        public SavingsContext() : base($"name={ConnectionStringName}")
        {
            this.Configuration.AutoDetectChangesEnabled = false;
        }

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

        private static EntityTypeConfiguration<T> ConfigureInfrastructureFields<T>(EntityTypeConfiguration<T> t) where T : InfrastructureBaseItem
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

            Func<bool, IndexAnnotation> index = isUnique => new IndexAnnotation(new IndexAttribute() { IsUnique = isUnique });

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
                ch.HasMany(x => x.CreatedLedgerTransactions).WithRequired(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId);
                ch.HasMany(x => x.CreatedSavingsAccounts).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.CreatedDatedSavingsAccountStrings).WithRequired(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId);
                ch.HasMany(x => x.CreatedDatedSavingsAccountValues).WithRequired(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId);
                ch.HasMany(x => x.CreatedSharedDatedValues).WithRequired(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId);
                ch.HasMany(x => x.CreatedSavingsAccountCreationRemarks).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.CreatedSharedSavingsInterestRates).WithRequired(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId);
                ch.HasMany(x => x.CreatedIncomingPaymentFiles).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.CreatedSavingsAccountInterestCapitalizations).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.CreatedOutgoingPayments).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.CreatedOutgoingPaymentFiles).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.InitiatedSavingsAccountWithdrawalAccountChanges).WithRequired(x => x.InitiatedByEvent).HasForeignKey(x => x.InitiatedByBusinessEventId);
                ch.HasMany(x => x.CreatedSavingsAccountKycQuestions).WithRequired(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId);
                ch.HasMany(x => x.CreatedSavingsAccountDocuments).WithRequired(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
                ch.HasMany(x => x.SharedSavingsInterestRateChangeRemovals).WithOptional(x => x.RemovedByBusinessEvent).HasForeignKey(x => x.RemovedByBusinessEventId);
                ch.HasMany(x => x.SharedSavingsInterestRateAppliesToAccountsSinces).WithOptional(x => x.AppliesToAccountsSinceBusinessEvent).HasForeignKey(x => x.AppliesToAccountsSinceBusinessEventId);
                ch.HasMany(x => x.CreatedSharedSavingsInterestRateChangeHeaders).WithRequired(x => x.BusinessEvent).HasForeignKey(x => x.BusinessEventId);
                ch.HasMany(x => x.CommittedOrCancelledSavingsAccountWithdrawalAccountChanges).WithOptional(x => x.CommitedOrCancelledByEvent).HasForeignKey(x => x.CommitedOrCancelledByEventId);
            });

            Cfg<DatedSavingsAccountValue>(modelBuilder, c =>
            {
                ConfigureInfrastructureFields(c);

                c.HasKey(x => x.Id);
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
                e.HasRequired(x => x.SavingsAccount).WithMany(x => x.Comments).HasForeignKey(x => x.SavingsAccountNr);
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
                ch.Property(x => x.AccountTypeCode).IsRequired().HasMaxLength(100);
                ch.Property(x => x.Status).IsRequired().HasMaxLength(100);
                ch.Property(x => x.MainCustomerId).IsRequired();
                ch.HasMany(x => x.Transactions).WithOptional(x => x.SavingsAccount).HasForeignKey(x => x.SavingsAccountNr);
                ch.HasMany(x => x.DatedValues).WithRequired(x => x.SavingsAccount).HasForeignKey(x => x.SavingsAccountNr);
                ch.HasMany(x => x.DatedStrings).WithRequired(x => x.SavingsAccount).HasForeignKey(x => x.SavingsAccountNr);
                ch.HasMany(x => x.Comments).WithRequired(x => x.SavingsAccount).HasForeignKey(x => x.SavingsAccountNr);
                ch.HasMany(x => x.CreationRemarks).WithRequired(x => x.SavingsAccount).HasForeignKey(x => x.SavingsAccountNr);
                ch.HasMany(x => x.SavingsAccountInterestCapitalizations).WithRequired(x => x.SavingsAccount).HasForeignKey(x => x.SavingsAccountNr);
                ch.HasMany(x => x.SavingsAccountWithdrawalAccountChanges).WithRequired(x => x.SavingsAccount).HasForeignKey(x => x.SavingsAccountNr);
                ch.HasMany(x => x.SavingsAccountKycQuestions).WithRequired(x => x.SavingsAccount).HasForeignKey(x => x.SavingsAccountNr);
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
                c.HasMany(x => x.AllAccountsHeaders).WithOptional(x => x.AllAccountsRate).HasForeignKey(x => x.AllAccountsRateId);
                c.HasMany(x => x.NewAccountsHeaders).WithOptional(x => x.NewAccountsOnlyRate).HasForeignKey(x => x.NewAccountsOnlyRateId);
            });

            Cfg<IncomingPaymentFileHeader>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
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

            Cfg<SavingsAccountInterestCapitalization>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.SavingsAccountNr).HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("InterestCapitalizationDuplicateGuardIndex", 1) { IsUnique = true }
                        }));
                ch.Property(x => x.FromDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.ToDate).IsRequired().HasColumnType("date").HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("InterestCapitalizationDuplicateGuardIndex", 2) { IsUnique = true }
                        }));
                ch.Property(x => x.CalculationDetailsDocumentArchiveKey).HasMaxLength(100);
            });

            Cfg<SavingsAccountWithdrawalAccountChange>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.PowerOfAttorneyDocumentArchiveKey).HasMaxLength(100);
                ch.Property(x => x.NewWithdrawalIban).IsRequired().HasMaxLength(100);
            });

            Cfg<TemporaryExternallyEncryptedItem>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.Property(x => x.CipherText).IsRequired();
                ch.Property(x => x.ProtocolVersionName).IsRequired().HasMaxLength(100);
                ch.Property(x => x.DeleteAfterDate).IsRequired();
                ch.Property(x => x.AddedDate).IsRequired();
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
                e.HasRequired(x => x.SavingsAccount).WithMany(x => x.Documents).HasForeignKey(x => x.SavingsAccountNr);
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
                e.Property(x => x.Key).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                e.Property(x => x.Value);
            });

            Cfg<CalendarDate>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.TheDate);
                ch.Property(x => x.TheDate).HasColumnType("date").IsRequired();
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

        private static void Cfg<T>(DbModelBuilder mb, Action<EntityTypeConfiguration<T>> a) where T : class
        {
            a(mb.Entity<T>());
        }

        public void RequireAmbientTransaction()
        {
            if (this.Database.CurrentTransaction == null)
            {
                throw new Exception("This methods writes directly to the database so it needs bo done in an ambient transaction.");
            }
        }

        public static void InitDatabase()
        {
            System.Data.Entity.Database.SetInitializer(new System.Data.Entity.MigrateDatabaseToLatestVersion<SavingsContext, Migrations.Configuration>());
            using (var context = new SavingsContext())
            {
                context.Database.Initialize(false);
            }

            using (var context = new SavingsContext())
            {
                context.EncryptedValues.Any();
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

        public static T RunWithExclusiveLock<T>(string lockName, Func<T> ifLockAquired, Func<T> ifAlreadyLocked, TimeSpan? waitForLock = null, bool useSharedLockSemantics = false) =>
            NTechPerServiceExclusiveLock.RunWithExclusiveLock(lockName, ifLockAquired, ifAlreadyLocked, waitForLock: waitForLock);
    }
}