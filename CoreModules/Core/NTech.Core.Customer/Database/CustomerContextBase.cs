using Microsoft.EntityFrameworkCore;
using nCustomer;
using nCustomer.DbModel;
using NTech.Core.Module.Database;

namespace NTech.Core.Customer.Database
{
    public class CustomerContextBase : NTechDbContext
    {
        public virtual DbSet<CustomerProperty> CustomerProperties { get; set; }
        public virtual DbSet<CustomerCardConflict> CustomerCardConflicts { get; set; }
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
        public virtual DbSet<CustomerIdSequence> CustomerIdSequences { get; set; }
        public virtual DbSet<CustomerCheckpoint> CustomerCheckpoints { get; set; }
        public virtual DbSet<KycQuestionTemplate> KycQuestionTemplates { get; set; }

        public override string ConnectionStringName => "CustomersContext";

        protected override void HandleCreate(ModelBuilder modelBuilder, Module.Database.LegacyEntityFrameworkHelper legacyHelper)
        {
            Cfg<CustomerProperty>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => x.Id);
                ch.Property(x => x.CustomerId).IsRequired().HasDefaultValueSql("0");
                ch.HasIndex(x => x.CustomerId).HasDatabaseName("CustomerPropertyCustomerIdIdx");
                ch.Property(x => x.Value).IsRequired();
                ch.Property(x => x.Name).IsRequired().HasMaxLength(128).HasDefaultValueSql("''");
                ch.Property(x => x.Group).IsRequired().HasMaxLength(128).HasDefaultValueSql("''");
                ch.HasOne(x => x.ReplacesCustomerProperty).WithMany(x => x.ReplacedByCustomerProperties).HasForeignKey(x => x.ReplacesCustomerProperty_Id);
                ch.Property(x => x.IsCurrentData).IsRequired();
                ch.Property(x => x.IsSensitive).HasDefaultValueSql("0");
                ch.Property(x => x.ChangedById).HasDefaultValueSql("0");
                ch.Property(x => x.ChangedDate).HasDefaultValueSql("'0001-01-01T00:00:00.000+00:00'");
                ch.Property(x => x.IsEncrypted).HasDefaultValueSql("0");
                ch.HasIndex(x => new { x.IsCurrentData, x.CustomerId, x.Name }).HasDatabaseName("CustomerFetchIdx1");
                ch.HasIndex(x => new { x.CustomerId, x.Name }).HasDatabaseName("CustomerPropertyNameUIdx").HasFilter("[IsCurrentData]=(1)").IsUnique();
                ch.HasIndex(x => new { x.IsCurrentData, x.Name, x.Timestamp }).HasDatabaseName("CustomerPropertyTsReplicationIdx1")
                    .IncludeProperties(x => new { x.CustomerId }).HasFilter("[IsCurrentData]=(1)");
                ch.HasIndex(x => new { x.IsCurrentData, x.Name }).HasDatabaseName("CustomersWithSameDataSearchIdx1").IncludeProperties(x => new { x.Value, x.CustomerId });
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
                ch.Property(x => x.IsEncrypted).HasDefaultValueSql("0");
                ch.Property(x => x.ChangedById).HasDefaultValueSql("0");
                ch.Property(x => x.ChangedDate).HasDefaultValueSql("'0001-01-01T00:00:00.000+00:00'");
            });

            Cfg<CustomerIdSequence>(modelBuilder, s =>
            {
                s.HasKey(x => x.CustomerId);
                s.Property(x => x.CivicRegNrHash).IsRequired().HasMaxLength(100);
                s.Property(x => x.Timestamp).IsRequired().IsRowVersion();
                s.HasIndex(x => x.CivicRegNrHash).IsUnique();
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
                c.Property(x => x.CustomerId).IsRequired();
                c.Property(x => x.QueryDate).IsRequired().HasColumnType("date");
                c.HasIndex(x => new { x.CustomerId, x.QueryDate }).HasDatabaseName("TrapetsQueryResultCoveringIdx1");
                c.Property(x => x.IsPepHit).IsRequired();
                c.Property(x => x.IsSanctionHit).IsRequired();
                c.HasMany(x => x.Items).WithOne(x => x.QueryResult).HasForeignKey(x => x.TrapetsQueryResultId).IsRequired();
            });

            Cfg<TrapetsQueryResultItem>(modelBuilder, c =>
            {
                ConfigureInfrastructureFields(c);
                c.HasKey(x => x.Id);
                c.Property(x => x.Name).IsRequired().HasMaxLength(100);
                c.HasIndex(x => x.Name);
                c.Property(x => x.Value).IsRequired();
                c.Property(x => x.IsEncrypted).IsRequired();
                c.HasIndex(x => new { x.Name }).IncludeProperties(x => new { x.TrapetsQueryResultId, x.Value }).HasDatabaseName("TrapetsQueryResultItemIdx2");
            });

            Cfg<CustomerSearchTerm>(modelBuilder, c =>
            {
                ConfigureInfrastructureFields(c);
                c.HasKey(x => x.Id);
                c.Property(x => x.CustomerId).IsRequired();
                c.HasIndex(x => x.CustomerId);
                c.HasIndex(x => new { x.TermCode, x.Value, x.IsActive }).HasDatabaseName("CustomerSearchIdx1");
                c.Property(x => x.TermCode).IsRequired().HasMaxLength(100);
                c.Property(x => x.Value).IsRequired().HasMaxLength(100);
                c.Property(x => x.IsActive).IsRequired();
                c.HasIndex(x => new { x.TermCode, x.IsActive }).IncludeProperties(x => new { x.CustomerId, x.Value }).HasDatabaseName("CustomerSearchTermIdx2");
                c.HasIndex(x => new { x.TermCode }).IncludeProperties(x => new { x.Id, x.CustomerId, x.IsActive }).HasDatabaseName("SearchTermUpdateIdx1");
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
                e.Property(x => x.CustomerId).IsRequired();
                e.HasIndex(x => x.CustomerId);
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
                ch.HasMany(x => x.CreatedProperties).WithOne(x => x.CreatedByEvent).HasForeignKey(x => x.CreatedByBusinessEventId);
            });

            Cfg<CustomerMessage>(modelBuilder, cm =>
            {
                cm.HasKey(x => x.Id);
                cm.Property(x => x.ChannelId).IsRequired().HasMaxLength(100);
                cm.Property(x => x.ChannelType).IsRequired().HasMaxLength(100);
                cm.Property(x => x.CreatedByUserId).IsRequired();
                cm.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime");
                cm.Property(x => x.CustomerId).IsRequired();
                cm.Property(x => x.HandledByUserId);
                cm.Property(x => x.HandledDate).HasColumnType("datetime");
                cm.Property(x => x.IsFromCustomer).IsRequired();
                cm.Property(x => x.Text).IsRequired();
                cm.HasMany(x => x.CustomerMessageAttachedDocuments).WithOne(x => x.Message).HasForeignKey(x => x.CustomerMessageId).IsRequired();
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
                e.Property(x => x.AnswerDate).IsRequired().HasColumnType("datetime");
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
                e.Property(x => x.CustomerId).IsRequired();
                e.HasIndex(x => x.CustomerId);
                e.Property(x => x.ReasonText);
                e.Property(x => x.StateBy).IsRequired();
                e.Property(x => x.StateDate).IsRequired();
                e.HasMany(x => x.Codes).WithOne(x => x.Checkpoint).HasForeignKey(x => x.CustomerCheckpointId).IsRequired().OnDelete(DeleteBehavior.Cascade);
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
    }
}
