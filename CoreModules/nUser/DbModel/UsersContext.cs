using NTech.Core.Module.Shared.Database;
using NTech.Legacy.Module.Shared;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace nUser.DbModel
{
    public class UsersContext : ChangeTrackingDbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<GroupMembership> GroupMemberships { get; set; }
        public DbSet<GroupMembershipCancellation> GroupMembershipCancellations { get; set; }
        public DbSet<AuthenticationMechanism> AuthenticationMechanisms { get; set; }
        public DbSet<UserSetting> UserSettings { get; set; }
        public virtual DbSet<KeyValueItem> KeyValueItems { get; set; }

        public IDbConnection GetConnection() => Database.Connection;
        public void BeginTransaction() => Database.BeginTransaction();
        public void CommitTransaction() => Database.CurrentTransaction.Commit();
        public void RollbackTransaction() => Database.CurrentTransaction.Rollback();
        public bool HasCurrentTransaction => Database.CurrentTransaction != null;
        public IDbTransaction CurrentTransaction => Database?.CurrentTransaction?.UnderlyingTransaction;
        public bool IsChangeTrackingEnabled
        {
            get
            {
                return Configuration.AutoDetectChangesEnabled;
            }
            set
            {
                Configuration.AutoDetectChangesEnabled = value;
            }
        }

        public void DetectChanges() => ChangeTracker.DetectChanges();
        public IQueryable<KeyValueItem> KeyValueItemsQueryable => KeyValueItems;
        public void RemoveKeyValueItem(KeyValueItem item) => KeyValueItems.Remove(item);
        public void AddKeyValueItem(KeyValueItem item) => KeyValueItems.Add(item);

        public UsersContext() : base("name=UsersContext")
        {

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Entity<User>().HasKey(x => x.Id);
            modelBuilder.Entity<User>().Property(x => x.CreationDate).IsRequired();
            modelBuilder.Entity<User>().Property(x => x.CreatedById).IsRequired();
            modelBuilder.Entity<User>().Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            modelBuilder.Entity<User>().Property(x => x.ConsentedDate);
            modelBuilder.Entity<User>().Property(x => x.ConsentText);
            modelBuilder.Entity<User>().Property(x => x.ProviderName).HasMaxLength(100);
            modelBuilder.Entity<User>().Property(x => x.IsSystemUser).IsRequired();
            modelBuilder.Entity<User>().HasMany(x => x.UserSettings).WithRequired(x => x.User).HasForeignKey(x => x.UserId);

            modelBuilder.Entity<GroupMembership>().HasKey(x => x.Id);
            modelBuilder.Entity<GroupMembership>().Property(x => x.CreationDate).IsRequired();
            modelBuilder.Entity<GroupMembership>().Property(x => x.CreatedById).IsRequired();
            modelBuilder.Entity<GroupMembership>().Property(x => x.ForProduct).HasMaxLength(100).IsRequired();
            modelBuilder.Entity<GroupMembership>().Property(x => x.GroupName).HasMaxLength(100).IsRequired();
            modelBuilder.Entity<GroupMembership>().Property(x => x.StartDate).IsRequired();
            modelBuilder.Entity<GroupMembership>().Property(x => x.EndDate).IsRequired();
            modelBuilder.Entity<GroupMembership>().Property(x => x.ApprovedDate);
            modelBuilder.Entity<GroupMembership>().Property(x => x.DisapprovedDate);
            modelBuilder.Entity<GroupMembership>().Property(x => x.ApprovedById);
            modelBuilder.Entity<GroupMembership>()
                .HasRequired(s => s.User)
                .WithMany(s => s.GroupMemberships)
                .HasForeignKey(x => x.User_Id);

            modelBuilder.Entity<GroupMembershipCancellation>().HasKey(x => x.Id);
            modelBuilder.Entity<GroupMembershipCancellation>().Property(x => x.CancellationBeginDate).IsRequired();
            modelBuilder.Entity<GroupMembershipCancellation>().Property(x => x.CancellationEndDate);
            modelBuilder.Entity<GroupMembershipCancellation>().Property(x => x.BegunById).IsRequired();
            modelBuilder.Entity<GroupMembershipCancellation>().Property(x => x.CommittedById);
            modelBuilder.Entity<GroupMembershipCancellation>().Property(x => x.UndoneById);
            modelBuilder.Entity<GroupMembershipCancellation>()
                .HasRequired(s => s.GroupMembership)
                .WithMany(s => s.GroupMembershipCancellation)
                .HasForeignKey(x => x.GroupMembership_Id);

            modelBuilder.Entity<AuthenticationMechanism>().HasKey(x => x.Id);
            modelBuilder.Entity<AuthenticationMechanism>().Property(x => x.CreationDate).IsRequired();
            modelBuilder.Entity<AuthenticationMechanism>().Property(x => x.CreatedById).IsRequired();
            modelBuilder.Entity<AuthenticationMechanism>().Property(x => x.RemovedById);
            modelBuilder.Entity<AuthenticationMechanism>().Property(x => x.RemovedDate);
            modelBuilder.Entity<AuthenticationMechanism>().Property(x => x.Credentials);
            modelBuilder.Entity<AuthenticationMechanism>()
                .Property(x => x.AuthenticationType)
                .HasMaxLength(128)
                .IsRequired()
                .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute() { IsUnique = false }));

            modelBuilder.Entity<AuthenticationMechanism>()
                .Property(x => x.AuthenticationProvider)
                .HasMaxLength(128)
                .IsRequired()
                .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(
                    new[]
                    {
                        new IndexAttribute() { IsUnique = false },
                        new IndexAttribute("IX_UniqueUserIdentity", 1) { IsUnique = true }
                    }));
            modelBuilder.Entity<AuthenticationMechanism>()
                .Property(x => x.UserIdentity)
                .HasMaxLength(128)
                .IsRequired()
                .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(
                    new[]
                    {
                        new IndexAttribute() { IsUnique = false },
                        new IndexAttribute("IX_UniqueUserIdentity", 2) { IsUnique = true }
                    }));

            modelBuilder.Entity<AuthenticationMechanism>()
                .HasRequired(s => s.User)
                .WithMany(s => s.AuthenticationMechanisms);

            Cfg<UserSetting>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Timestamp).IsRequired().IsRowVersion();
                e.Property(x => x.CreationDate).IsRequired();
                e.Property(x => x.CreatedById).IsRequired();
                e.Property(x => x.Name).IsRequired().HasMaxLength(128).HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute() { IsUnique = false }));
                e.Property(x => x.Value);
            });

            Cfg<KeyValueItem>(modelBuilder, ch =>
            {
                ConfigureInfrastructureFields(ch);
                ch.HasKey(x => new { x.Key, x.KeySpace });
                ch.Property(x => x.Key).IsRequired().HasMaxLength(128);
                ch.Property(x => x.KeySpace).IsRequired().HasMaxLength(128);
                ch.Property(x => x.Value);
            });
        }

        private static void Cfg<T>(DbModelBuilder mb, Action<EntityTypeConfiguration<T>> a) where T : class
        {
            a(mb.Entity<T>());
        }

        private static EntityTypeConfiguration<T> ConfigureInfrastructureFields<T>(EntityTypeConfiguration<T> t) where T : InfrastructureBaseItem
        {
            t.Property(e => e.Timestamp).IsRequired().IsRowVersion();
            t.Property(e => e.ChangedById).IsRequired();
            t.Property(e => e.ChangedDate).IsRequired();
            t.Property(e => e.InformationMetaData);
            return t;
        }

        public void Seed(UsersContext context)
        {
            context.SaveChanges();
        }

        public override int SaveChanges()
        {
            var result = base.SaveChanges();
            CacheHandler.ClearAllCaches();
            return result;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            var result = await base.SaveChangesAsync(cancellationToken);
            CacheHandler.ClearAllCaches();
            return result;
        }

        public static void InitDatabase()
        {
            System.Data.Entity.Database.SetInitializer(new System.Data.Entity.MigrateDatabaseToLatestVersion<UsersContext, Migrations.Configuration>());
            using (var context = new UsersContext())
            {
                context.Database.Initialize(false);
            }

            using (var context = new UsersContext())
            {
                context.Users.Any();
            }
        }
    }
}