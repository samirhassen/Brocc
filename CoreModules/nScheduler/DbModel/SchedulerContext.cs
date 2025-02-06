using NTech.Services.Infrastructure;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;

namespace nScheduler
{
    public class SchedulerContext : DbContext
    {
        private const string ConnectionStringName = "SchedulerContext";
        public SchedulerContext() : base($"name={ConnectionStringName}")
        {
        }

        public virtual DbSet<ServiceRun> ServiceRuns { get; set; }

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

            Cfg<ServiceRun>(modelBuilder, c =>
            {
                ConfigureInfrastructureFields(c);
                c.HasKey(x => x.Id);
                c.Property(x => x.JobName).IsRequired().HasMaxLength(128);
                c.Property(x => x.TimeSlotName).HasMaxLength(128);
                c.Property(x => x.StartDate).IsRequired();
                c.Property(x => x.EndDate);
                c.Property(x => x.EndStatus).HasMaxLength(128);
                c.Property(x => x.TriggeredById).IsRequired();
                c.Property(x => x.EndStatusData);
                c.Property(x => x.RuntimeInMs);
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
            System.Data.Entity.Database.SetInitializer(
                new System.Data.Entity.MigrateDatabaseToLatestVersion<SchedulerContext, Migrations.Configuration>());
            using (var context = new SchedulerContext())
            {
                context.Database.Initialize(false);
            }

            using (var context = new SchedulerContext())
            {
                context.ServiceRuns.Any();
            }
        }

        public static T RunWithExclusiveLock<T>(string lockName, Func<T> ifLockAquired, Func<T> ifAlreadyLocked) =>
            NTechPerServiceExclusiveLock.RunWithExclusiveLock(lockName, ifLockAquired, ifAlreadyLocked);
    }
}