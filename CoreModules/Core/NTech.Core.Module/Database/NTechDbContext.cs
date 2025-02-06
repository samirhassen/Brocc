using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NTech.Core.Module.Shared.Database;
using System.Data;

namespace NTech.Core.Module.Database
{
    public abstract class NTechDbContext : DbContext, INTechDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var legacyHelper = new LegacyEntityFrameworkHelper();

            HandleCreate(modelBuilder, legacyHelper);

            // This call should always be last since it introspects the metadata which is changed by the earlier calls.
            legacyHelper.RestoreLegacyNamingConventions(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(NEnv.SharedInstance.GetConnectionString(ConnectionStringName));
            }
        }

        public abstract string ConnectionStringName { get; }
        protected abstract void HandleCreate(ModelBuilder modelBuilder, LegacyEntityFrameworkHelper legacyHelper);

        protected void Cfg<T>(ModelBuilder mb, Action<Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T>> a) where T : class
        {
            a(mb.Entity<T>());
        }

        protected Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> ConfigureInfrastructureFields<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> t) where T : InfrastructureBaseItem
        {
            t.Property(e => e.Timestamp).IsRequired().IsRowVersion();
            t.Property(e => e.ChangedById).IsRequired();
            t.Property(e => e.ChangedDate).IsRequired();
            t.Property(e => e.InformationMetaData);
            return t;
        }

        public void BeginTransaction() => Database.BeginTransaction();
        public void CommitTransaction() => Database.CurrentTransaction.Commit();
        public void RollbackTransaction() => Database.CurrentTransaction.Rollback();
        public bool HasCurrentTransaction => Database.CurrentTransaction != null;
        public IDbTransaction CurrentTransaction => Database?.CurrentTransaction?.GetDbTransaction();
        public IDbConnection GetConnection() => Database.GetDbConnection();

        public bool IsChangeTrackingEnabled
        {
            get
            {
                return ChangeTracker.AutoDetectChangesEnabled;
            }
            set
            {
                ChangeTracker.AutoDetectChangesEnabled = value;
            }
        }

        public void DetectChanges() => ChangeTracker.DetectChanges();
    }
}
