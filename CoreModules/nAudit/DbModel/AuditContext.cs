using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;

namespace nAudit
{
    public class AuditContext : DbContext
    {
        public virtual DbSet<SystemLogItem> SystemLogItems { get; set; }
        /*
        public virtual DbSet<PersonalDataViewLogItem> PersonalDataViewLogItems { get; set; }
        public virtual DbSet<ChangeLogItem> ChangeLogItems { get; set; }
        public virtual DbSet<EncryptedValue> EncryptedValues { get; set; }
        */
        public AuditContext() : base("name=AuditContext")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Entity<SystemLogItem>().HasKey(x => x.Id);
            modelBuilder.Entity<SystemLogItem>().Property(x => x.Level).IsRequired().HasMaxLength(15).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute() { IsUnique = false }));
            modelBuilder.Entity<SystemLogItem>().Property(x => x.EventDate).IsRequired().HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute() { IsUnique = false }));
            modelBuilder.Entity<SystemLogItem>().Property(x => x.ServiceName).HasMaxLength(30);
            modelBuilder.Entity<SystemLogItem>().Property(x => x.ServiceVersion).HasMaxLength(30);
            modelBuilder.Entity<SystemLogItem>().Property(x => x.RemoteIp).HasMaxLength(30);
            modelBuilder.Entity<SystemLogItem>().Property(x => x.RequestUri).HasMaxLength(128);
            modelBuilder.Entity<SystemLogItem>().Property(x => x.UserId).HasMaxLength(128);
            modelBuilder.Entity<SystemLogItem>().Property(x => x.ExceptionMessage);
            modelBuilder.Entity<SystemLogItem>().Property(x => x.ExceptionData);
            modelBuilder.Entity<SystemLogItem>().Property(x => x.Message);
            modelBuilder.Entity<SystemLogItem>().Property(x => x.EventType).HasMaxLength(128).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute() { IsUnique = false }));
        }

        public static void InitDatabase()
        {
            System.Data.Entity.Database.SetInitializer(
                new System.Data.Entity.MigrateDatabaseToLatestVersion<AuditContext, Migrations.Configuration>());

            using (var context = new AuditContext())
            {
                context.Database.Initialize(false);
            }

            using (var context = new AuditContext())
            {
                context.SystemLogItems.Any();
            }
        }
    }
}