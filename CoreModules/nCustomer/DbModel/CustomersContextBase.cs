using NTech.Core.Module.Shared.Database;
using NTech.Services.Infrastructure;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;

namespace nCustomer.DbModel
{
    public abstract class CustomersContextBase : DbModel.ChangeTrackingDbContext
    {
        public virtual DbSet<CustomerProperty> CustomerProperties { get; set; }
        public virtual DbSet<EncryptedValue> EncryptedValues { get; set; }
        public virtual DbSet<TrapetsQueryResult> TrapetsQueryResults { get; set; }
        public virtual DbSet<TrapetsQueryResultItem> TrapetsQueryResultItems { get; set; }
        public virtual DbSet<CustomerSearchTerm> CustomerSearchTerms { get; set; }
        public virtual DbSet<KeyValueItem> KeyValueItems { get; set; }
        public virtual DbSet<CustomerComment> CustomerComments { get; set; }
        public virtual DbSet<CustomerRelation> CustomerRelations { get; set; }
        public virtual DbSet<BusinessEvent> BusinessEvents { get; set; }
        public virtual DbSet<CustomerMessage> CustomerMessages { get; set; }
        public virtual DbSet<CustomerMessageAttachedDocument> CustomerMessageAttachedDocuments { get; set; }
        public virtual DbSet<StoredCustomerQuestionSet> StoredCustomerQuestionSets { get; set; }
        public virtual DbSet<CustomerCheckpoint> CustomerCheckpoints { get; set; }
        public virtual DbSet<KycQuestionTemplate> KycQuestionTemplates { get; set; }

        private const string ConnectionStringName = "CustomersContext";

        public CustomersContextBase() : base($"name={ConnectionStringName}")
        {
        }

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

            Cfg<CustomerProperty>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.CustomerId).IsRequired();
                ch.Property(x => x.Value).IsRequired();
                ch.Property(x => x.Name).IsRequired().HasMaxLength(128);
                ch.Property(x => x.Group).IsRequired().HasMaxLength(128);
                ch.HasOptional(x => x.ReplacesCustomerProperty).WithMany(x => x.ReplacedByCustomerProperties).HasForeignKey(x => x.ReplacesCustomerProperty_Id);
                ch.Property(x => x.IsCurrentData).IsRequired();
            });

            Cfg<CustomerCardConflict>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.CustomerId).IsRequired();
                ch.Property(x => x.Name).IsRequired();
                ch.Property(x => x.Group).IsRequired();
                ch.Property(x => x.Value).IsRequired();
                ch.Property(x => x.IsSensitive).IsRequired();
                ch.Property(x => x.ApprovedDate);
                ch.Property(x => x.DiscardedDate);
            });

            Cfg<CustomerIdSequence>(modelBuilder, s =>
            {
                s.HasKey(x => x.CustomerId);
                s.Property(x => x.CivicRegNrHash).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute() { IsUnique = true }));
                s.Property(x => x.Timestamp).IsRequired().IsRowVersion();
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

            Cfg<TrapetsQueryResult>(modelBuilder, c =>
            {
                ConfigureInfrastructureFields(c);
                c.HasKey(x => x.Id);
                c.Property(x => x.CustomerId).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("TrapetsQueryResultCoveringIdx1", 1)
                        }));
                c.Property(x => x.QueryDate).IsRequired().HasColumnType("date").HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("TrapetsQueryResultCoveringIdx1", 2)
                        }));
                c.Property(x => x.IsPepHit).IsRequired();
                c.Property(x => x.IsSanctionHit).IsRequired();
                c.HasMany(x => x.Items).WithRequired(x => x.QueryResult).HasForeignKey(x => x.TrapetsQueryResultId);
            });

            Cfg<TrapetsQueryResultItem>(modelBuilder, c =>
            {
                ConfigureInfrastructureFields(c);
                c.HasKey(x => x.Id);
                c.Property(x => x.Name).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute() { IsUnique = false }));
                c.Property(x => x.Value).IsRequired();
                c.Property(x => x.IsEncrypted).IsRequired();
            });

            Cfg<CustomerSearchTerm>(modelBuilder, c =>
            {
                ConfigureInfrastructureFields(c);
                c.HasKey(x => x.Id);
                c.Property(x => x.CustomerId).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute() { IsUnique = false }));
                c.Property(x => x.TermCode).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("CustomerSearchIdx1", 1)
                        }));
                c.Property(x => x.Value).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("CustomerSearchIdx1", 2)
                        }));
                c.Property(x => x.IsActive).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("CustomerSearchIdx1", 3)
                        }));
            });

            Cfg<KeyValueItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => new { x.Key, x.KeySpace });
                ch.Property(x => x.Key).IsRequired().HasMaxLength(128);
                ch.Property(x => x.KeySpace).IsRequired().HasMaxLength(128);
                ch.Property(x => x.Value);
            });

            Cfg<CustomerComment>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                ConfigureInfrastructureFields(e);
                e.Property(x => x.CustomerId).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute() { IsUnique = false }));
                e.Property(x => x.CommentById).IsRequired();
                e.Property(x => x.CommentDate).IsRequired();
                e.Property(x => x.CommentText);
                e.Property(x => x.Attachment);
                e.Property(x => x.EventType).HasMaxLength(100);
            });

            Cfg<CustomerRelation>(modelBuilder, e =>
            {
                e.HasKey(x => new { x.CustomerId, x.RelationType, x.RelationId });
                e.Property(x => x.CustomerId).IsRequired();
                e.Property(x => x.RelationType).IsRequired().HasMaxLength(100);
                e.Property(x => x.RelationId).IsRequired().HasMaxLength(100);
                e.Property(x => x.StartDate).HasColumnType("date");
                e.Property(x => x.EndDate).HasColumnType("date");
            });

            Cfg<BusinessEvent>(modelBuilder, ch =>
            {
                ch.HasKey(x => x.Id);
                ch.Property(x => x.EventType).IsRequired().HasMaxLength(100);
                ch.Property(x => x.EventDate).IsRequired();
                ch.Property(x => x.UserId).IsRequired();
                ch.Property(x => x.TransactionDate).IsRequired().HasColumnType("date");
                ch.Property(x => x.EventDate).IsRequired();
                ch.HasMany(x => x.CreatedProperties).WithOptional(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
            });

            Cfg<CustomerMessage>(modelBuilder, cm =>
            {
                cm.HasKey(x => x.Id);
                cm.Property(x => x.ChannelId).IsRequired().HasMaxLength(100);
                cm.Property(x => x.ChannelType).IsRequired().HasMaxLength(100);
                cm.Property(x => x.CreatedByUserId).IsRequired();
                cm.Property(x => x.CreatedDate).IsRequired();
                cm.Property(x => x.CustomerId).IsRequired();
                cm.Property(x => x.HandledByUserId);
                cm.Property(x => x.HandledDate);
                cm.Property(x => x.IsFromCustomer).IsRequired();
                cm.Property(x => x.Text).IsRequired();
                cm.HasMany(x => x.CustomerMessageAttachedDocuments).WithRequired(x => x.Message).HasForeignKey(x => x.CustomerMessageId);
            });

            Cfg<CustomerMessageAttachedDocument>(modelBuilder, cm =>
            {
                cm.HasKey(x => x.Id);
                cm.Property(x => x.ArchiveKey).IsRequired();
                cm.Property(x => x.ContentTypeMimetype).IsRequired().HasMaxLength(100);
                cm.Property(x => x.CustomerMessageId).IsRequired();
                cm.Property(x => x.FileName).IsRequired();
            });

            Cfg<StoredCustomerQuestionSet>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);
                e.HasKey(x => x.Id);
                e.Property(x => x.AnswerDate).IsRequired();
                e.Property(x => x.CustomerId).IsRequired();
                e.Property(x => x.KeyValueStorageKey).IsRequired().HasMaxLength(128);
                e.Property(x => x.KeyValueStorageKeySpace).IsRequired().HasMaxLength(128);
                e.Property(x => x.SourceType).IsRequired().HasMaxLength(128);
                e.Property(x => x.SourceId).IsRequired().HasMaxLength(128);
            });

            Cfg<CustomerCheckpoint>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.Id);
                e.Property(x => x.CustomerId).IsRequired().HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute() { IsUnique = false }));
                e.Property(x => x.ReasonText);
                e.Property(x => x.StateBy).IsRequired();
                e.Property(x => x.StateDate).IsRequired();
                e.HasMany(x => x.Codes).WithRequired(x => x.Checkpoint).HasForeignKey(x => x.CustomerCheckpointId).WillCascadeOnDelete(true);
            });

            Cfg<CustomerCheckpointCode>(modelBuilder, e =>
            {
                e.HasKey(x => new { x.CustomerCheckpointId, x.Code });
            });

            Cfg<KycQuestionTemplate>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.CreatedByUserId).IsRequired();
                e.Property(x => x.CreatedDate).IsRequired();
                e.Property(x => x.RelationType).IsRequired().HasMaxLength(100);
            });
        }

        public BusinessEvent CreateBusinessEvent(string eventType, int userId, DateTimeOffset eventDate)
        {
            return new BusinessEvent
            {
                EventDate = eventDate,
                TransactionDate = eventDate.Date,
                EventType = eventType,
                UserId = userId
            };
        }

        public static T RunWithExclusiveLock<T>(string lockName, Func<T> ifLockAquired, Func<T> ifAlreadyLocked, TimeSpan? waitForLock = null) =>
            NTechPerServiceExclusiveLock.RunWithExclusiveLock(lockName, ifLockAquired, ifAlreadyLocked, waitForLock: waitForLock);

        public void Seed(CustomersContext context)
        {
        }
        public class CreateInitializer : CreateDatabaseIfNotExists<CustomersContext>
        {
            protected override void Seed(CustomersContext context)
            {
                context.Seed(context);
                base.Seed(context);
            }
        }
        private static void Cfg<T>(DbModelBuilder mb, Action<EntityTypeConfiguration<T>> a) where T : class
        {
            a(mb.Entity<T>());
        }

        public static void InitDatabase()
        {
            System.Data.Entity.Database.SetInitializer(
                new System.Data.Entity.MigrateDatabaseToLatestVersion<CustomersContext, nCustomer.Migrations.Configuration>());
            using (var context = new CustomersContext())
            {
                context.Database.Initialize(false);
            }

            ReseedIfNeeded();

            using (var c = new CustomersContext())
            {
                c.CustomerProperties.Any();
            }
        }

        public static void OnSetup()
        {
            using (var context = new CustomersContext())
            {
                context.Database.Initialize(true);
            }
            ReseedIfNeeded();
        }

        public static void ReseedIfNeeded()
        {
            void ReseedIfNeeded(CustomersContext context, string tableName, int minAllowedValue)
            {
                var count = context.Database.SqlQuery<int>($"select count(*) from {tableName}").Single();
                if (count > 0)
                    return;
                var currentValue = context.Database.SqlQuery<decimal>($"select IDENT_CURRENT('{tableName}')").Single();
                if (currentValue < minAllowedValue)
                {
                    context.Database.ExecuteSqlCommand($"DBCC CHECKIDENT ('{tableName}', RESEED, {minAllowedValue})");
                }
            }
            using (var context = new CustomersContext())
            {
                ReseedIfNeeded(context, "CustomerIdSequence", 1000);
            }
        }

        public void RequireAmbientTransaction()
        {
            if (this.Database.CurrentTransaction == null)
                throw new Exception("Must be run in an ambient transaction");
        }

        public T FillInfrastructureFields<T>(T b, Code.NtechCurrentUserMetadata userMetadata, NTech.IClock clock) where T : InfrastructureBaseItem
        {
            b.ChangedById = userMetadata.UserId;
            b.ChangedDate = clock.Now;
            b.InformationMetaData = userMetadata.InformationMetadata;
            return b;
        }
    }
}