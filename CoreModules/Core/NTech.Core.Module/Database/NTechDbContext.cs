using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage;
using NTech.Core.Module.Shared.Database;

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
            if (optionsBuilder.IsConfigured) return;
            optionsBuilder.UseSqlServer(NEnv.SharedInstance.GetConnectionString(ConnectionStringName));
        }

        public abstract string ConnectionStringName { get; }
        protected abstract void HandleCreate(ModelBuilder modelBuilder, LegacyEntityFrameworkHelper legacyHelper);

        protected static void Cfg<T>(ModelBuilder mb, Action<EntityTypeBuilder<T>> a) where T : class
        {
            a(mb.Entity<T>());
        }

        protected static EntityTypeBuilder<T> ConfigureInfrastructureFields<T>(
            EntityTypeBuilder<T> t) where T : InfrastructureBaseItem
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
            get => ChangeTracker.AutoDetectChangesEnabled;
            set => ChangeTracker.AutoDetectChangesEnabled = value;
        }

        public void DetectChanges() => ChangeTracker.DetectChanges();
    }
}