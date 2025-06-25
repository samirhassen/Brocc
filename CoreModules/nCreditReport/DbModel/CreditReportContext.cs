using NTech.Services.Infrastructure;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace nCreditReport
{
    public class CreditReportContext : DbContext
    {
        public virtual DbSet<CreditReportHeader> CreditApplicationHeaders { get; set; }
        public virtual DbSet<CreditReportSearchTerm> CreditApplicationSearchTerms { get; set; }
        public virtual DbSet<SystemItem> SystemItems { get; set; }
        public virtual DbSet<AddressLookupCachedResult> AddressLookupCachedResults { get; set; }
        public string ConnectionStringName { get { return connectionStringName; } }

        public const string connectionStringName = "CreditReportContext";

        public CreditReportContext() : base($"name={connectionStringName}")
        {

        }

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

            var ch = modelBuilder.Entity<CreditReportHeader>();
            ConfigureInfrastructureFields(ch);
            ch.HasKey(x => x.Id);
            ch.Property(x => x.EncryptionKeyName).IsRequired();
            ch.Property(x => x.CreditReportProviderName).IsRequired().HasMaxLength(100);
            ch.HasMany(x => x.SearchTerms);
            ch.Property(x => x.RequestDate).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
            ch.Property(x => x.CustomerId).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));

            var cs = modelBuilder.Entity<CreditReportSearchTerm>();
            ConfigureInfrastructureFields(cs);
            cs.HasRequired(x => x.CreditReport).WithMany(x => x.SearchTerms).HasForeignKey(x => x.CreditReportHeaderId);
            cs.Property(x => x.CreditReportHeaderId).HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("CreditReportSearchTermCoveringIndex", 3)
                        }));
            cs.HasKey(x => x.Id);
            cs.Property(x => x.Name).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("CreditReportSearchTermNameIndex", 1),
                            new IndexAttribute("CreditReportSearchTermCoveringIndex", 1)
                        }));
            cs.Property(x => x.Value).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new[]
                        {
                            new IndexAttribute("CreditReportSearchTermValueIndex", 1),
                            new IndexAttribute("CreditReportSearchTermCoveringIndex", 2)
                        }));

            var ei = modelBuilder.Entity<EncryptedCreditReportItem>();
            ConfigureInfrastructureFields(ei);
            ei.HasRequired(x => x.CreditReport).WithMany(x => x.EncryptedItems).HasForeignKey(x => x.CreditReportHeaderId);
            ei.HasKey(x => x.Id);
            ei.Property(x => x.Name).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
            ei.Property(x => x.Value).IsRequired();

            Cfg<SystemItem>(modelBuilder, e =>
            {
                ConfigureInfrastructureFields(e);

                e.HasKey(x => x.Id);
                e.Property(x => x.Key).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                e.Property(x => x.Value);
            });

            Cfg<AddressLookupCachedResult>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.ProviderName).IsRequired().HasMaxLength(100)
                    .HasColumnAnnotation(IndexAnnotation.AnnotationName,
                        new IndexAnnotation(new[]
                        {
                            new IndexAttribute("AddressLookupCachedResultLookupIdx", 2),
                        }));

                e.Property(x => x.RequestDate).IsRequired()
                    .HasColumnAnnotation(IndexAnnotation.AnnotationName,
                        new IndexAnnotation(new[]
                        {
                            new IndexAttribute("AddressLookupCachedResultLookupIdx", 3),
                        }));

                e.Property(x => x.CustomerId).IsRequired()
                    .HasColumnAnnotation(IndexAnnotation.AnnotationName,
                        new IndexAnnotation(new[]
                        {
                            new IndexAttribute("AddressLookupCachedResultLookupIdx", 1),
                        }));

                e.Property(x => x.EncryptionKeyName).IsRequired();
                e.Property(x => x.EncryptedData).IsRequired();
                e.Property(x => x.DeleteAfterDate).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
            });
        }

        private static void Cfg<T>(DbModelBuilder mb, Action<EntityTypeConfiguration<T>> a) where T : class
        {
            a(mb.Entity<T>());
        }

        public void Seed(CreditReportContext context)
        {
            context.SaveChanges();
        }

        public static void InitDatabase()
        {
            System.Data.Entity.Database.SetInitializer(new System.Data.Entity.MigrateDatabaseToLatestVersion<CreditReportContext, Migrations.Configuration>());
            using (var context = new CreditReportContext())
            {
                context.Database.Initialize(false);
            }
        }

        public static T RunWithExclusiveLock<T>(string lockName, Func<T> ifLockAquired, Func<T> ifAlreadyLocked, TimeSpan? waitForLock = null) =>
            NTechPerServiceExclusiveLock.RunWithExclusiveLock(lockName, ifLockAquired, ifAlreadyLocked, acquireTimeout: waitForLock);
    }
}