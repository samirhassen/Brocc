using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;

namespace nDataWarehouse
{
    public class AnalyticsContext : DbContext
    {
        public virtual DbSet<AnalyticsSetting> AnalyticsSettings { get; set; }
        public virtual DbSet<ExportedReport> ExportedReports { get; set; }

        public AnalyticsContext() : base("name=AnalyticsContext")
        {

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            Func<bool, IndexAnnotation> index = isUnique => new IndexAnnotation(new IndexAttribute() { IsUnique = isUnique });

            Cfg<AnalyticsSetting>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Key).IsRequired().HasMaxLength(100).HasColumnAnnotation(IndexAnnotation.AnnotationName, index(false));
                e.Property(x => x.Value);
            });

            Cfg<ExportedReport>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.ReportArchiveKey).HasMaxLength(100);
                e.Property(x => x.ReportDate).IsRequired();
                e.Property(x => x.ReportName).HasMaxLength(100).IsRequired();
            });
        }

        private static void Cfg<T>(DbModelBuilder mb, Action<EntityTypeConfiguration<T>> a) where T : class
        {
            a(mb.Entity<T>());
        }

        public void Seed(AnalyticsContext context)
        {
            context.SaveChanges();
        }

        public static void InitDatabase()
        {
            System.Data.Entity.Database.SetInitializer(new System.Data.Entity.MigrateDatabaseToLatestVersion<AnalyticsContext, Migrations.Configuration>());

            using (var context = new AnalyticsContext())
            {
                context.Database.Initialize(false);
            }

            using (var context = new AnalyticsContext())
            {
                context.AnalyticsSettings.Any();
            }
        }
    }
}