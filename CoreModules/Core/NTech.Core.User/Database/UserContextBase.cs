using Microsoft.EntityFrameworkCore;
using NTech.Core.Module.Database;
using nUser.DbModel;
using UserClass = nUser.DbModel.User;

namespace NTech.Core.User.Database
{
    public abstract class UserContextBase : NTechDbContext
    {
        public virtual DbSet<UserClass> Users { get; set; }
        public virtual DbSet<GroupMembership> GroupMemberships { get; set; }
        public virtual DbSet<GroupMembershipCancellation> GroupMembershipCancellations { get; set; }
        public virtual DbSet<AuthenticationMechanism> AuthenticationMechanisms { get; set; }
        public virtual DbSet<UserSetting> UserSettings { get; set; }
        public virtual DbSet<KeyValueItem> KeyValueItems { get; set; }

        public override string ConnectionStringName => "UsersContext";

        protected override void HandleCreate(ModelBuilder modelBuilder, LegacyEntityFrameworkHelper legacyHelper)
        {
            modelBuilder.Entity<UserClass>().HasKey(x => x.Id);
            modelBuilder.Entity<UserClass>().Property(x => x.CreationDate).IsRequired().HasColumnType("datetime");
            modelBuilder.Entity<UserClass>().Property(x => x.DeletionDate).HasColumnType("datetime");
            modelBuilder.Entity<UserClass>().Property(x => x.CreatedById).IsRequired();
            modelBuilder.Entity<UserClass>().Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            modelBuilder.Entity<UserClass>().Property(x => x.ConsentedDate).HasColumnType("datetime");
            modelBuilder.Entity<UserClass>().Property(x => x.ConsentText);
            modelBuilder.Entity<UserClass>().Property(x => x.ProviderName).HasMaxLength(100);

            //NOTE: This default value is a bit insane but we need to exactly mirror core and legacy for now. Will not in practice default to true since boolean has a default value so this will never actually be used.
            modelBuilder.Entity<UserClass>().Property(x => x.IsSystemUser).IsRequired().HasDefaultValueSql("(1) ");
            modelBuilder.Entity<UserClass>().HasMany(x => x.UserSettings).WithOne(x => x.User).HasForeignKey(x => x.UserId).IsRequired();

            modelBuilder.Entity<GroupMembership>().HasKey(x => x.Id);
            modelBuilder.Entity<GroupMembership>().Property(x => x.CreationDate).IsRequired().HasColumnType("datetime");
            modelBuilder.Entity<GroupMembership>().Property(x => x.CreatedById).IsRequired();
            modelBuilder.Entity<GroupMembership>().Property(x => x.ForProduct).HasMaxLength(100).IsRequired();
            modelBuilder.Entity<GroupMembership>().Property(x => x.GroupName).HasMaxLength(100).IsRequired();
            modelBuilder.Entity<GroupMembership>().Property(x => x.StartDate).IsRequired().HasColumnType("datetime");
            modelBuilder.Entity<GroupMembership>().Property(x => x.EndDate).IsRequired().HasColumnType("datetime");
            modelBuilder.Entity<GroupMembership>().Property(x => x.ApprovedDate).HasColumnType("datetime");
            modelBuilder.Entity<GroupMembership>().Property(x => x.DisapprovedDate).HasColumnType("datetime");
            modelBuilder.Entity<GroupMembership>().Property(x => x.ApprovedById);
            modelBuilder.Entity<GroupMembership>()
                .HasOne(s => s.User)
                .WithMany(s => s.GroupMemberships)
                .HasForeignKey(x => x.User_Id)
                .IsRequired();

            modelBuilder.Entity<GroupMembershipCancellation>().HasKey(x => x.Id);
            modelBuilder.Entity<GroupMembershipCancellation>().Property(x => x.CancellationBeginDate).IsRequired().HasColumnType("datetime");
            modelBuilder.Entity<GroupMembershipCancellation>().Property(x => x.CancellationEndDate).HasColumnType("datetime");
            modelBuilder.Entity<GroupMembershipCancellation>().Property(x => x.BegunById).IsRequired();
            modelBuilder.Entity<GroupMembershipCancellation>().Property(x => x.CommittedById);
            modelBuilder.Entity<GroupMembershipCancellation>().Property(x => x.UndoneById);
            modelBuilder.Entity<GroupMembershipCancellation>().Property(x => x.GroupMembership_Id).HasDefaultValueSql("(0)");
            modelBuilder.Entity<GroupMembershipCancellation>()
                .HasOne(s => s.GroupMembership)
                .WithMany(s => s.GroupMembershipCancellation)
                .HasForeignKey(x => x.GroupMembership_Id)
                .IsRequired();

            modelBuilder.Entity<AuthenticationMechanism>().HasKey(x => x.Id);
            modelBuilder.Entity<AuthenticationMechanism>().Property(x => x.CreationDate).IsRequired().HasColumnType("datetime");
            modelBuilder.Entity<AuthenticationMechanism>().Property(x => x.CreatedById).IsRequired();
            modelBuilder.Entity<AuthenticationMechanism>().Property(x => x.RemovedById);
            modelBuilder.Entity<AuthenticationMechanism>().Property(x => x.RemovedDate).HasColumnType("datetime");
            modelBuilder.Entity<AuthenticationMechanism>().Property(x => x.Credentials);
            modelBuilder.Entity<AuthenticationMechanism>()
                .Property(x => x.AuthenticationType)
                .HasMaxLength(128)
                .IsRequired();

            modelBuilder.Entity<AuthenticationMechanism>().HasIndex(x => new { x.AuthenticationType });

            modelBuilder.Entity<AuthenticationMechanism>()
                .Property(x => x.AuthenticationProvider)
                .HasMaxLength(128)
                .IsRequired();

            modelBuilder
                .Entity<AuthenticationMechanism>()
                .HasIndex(x => new { x.AuthenticationProvider })
                .HasDatabaseName("IX_AuthenticationProvider");

            modelBuilder
                .Entity<AuthenticationMechanism>()
                .HasIndex(x => new { x.UserIdentity })
                .HasDatabaseName("IX_UserIdentity");

            modelBuilder.Entity<AuthenticationMechanism>()
                .Property(x => x.UserIdentity)
                .HasMaxLength(128)
                .IsRequired();

            modelBuilder.Entity<AuthenticationMechanism>()
                .HasOne(s => s.User)
                .WithMany(s => s.AuthenticationMechanisms)
                .IsRequired();

            Cfg<UserSetting>(modelBuilder, e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Timestamp).IsRequired().IsRowVersion();
                e.Property(x => x.CreationDate).IsRequired().HasColumnType("datetime");
                e.Property(x => x.CreatedById).IsRequired();
                e.Property(x => x.Name).IsRequired().HasMaxLength(128);
                e.Property(x => x.Value);
                e.HasIndex(x => new { x.Name });
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
    }
}
