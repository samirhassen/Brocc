using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace nCustomer.DbModel
{
    public class ChangeTrackingDbContext : DbContext
    {
        private void DetectChangedIfAutoDetectIsOff()
        {
            if (!this.Configuration.AutoDetectChangesEnabled)
            {
                this.ChangeTracker?.DetectChanges();
            }
        }

        public override int SaveChanges()
        {
            DetectChangedIfAutoDetectIsOff();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            DetectChangedIfAutoDetectIsOff();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync()
        {
            DetectChangedIfAutoDetectIsOff();
            return base.SaveChangesAsync();
        }

        #region "Default Constructors"

        public ChangeTrackingDbContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
        }

        public ChangeTrackingDbContext(DbConnection existingConnection, bool contextOwnsConnection) : base(existingConnection, contextOwnsConnection)
        {
        }

        public ChangeTrackingDbContext(ObjectContext objectContext, bool dbContextOwnsObjectContext) : base(objectContext, dbContextOwnsObjectContext)
        {
        }

        public ChangeTrackingDbContext(string nameOrConnectionString, DbCompiledModel model) : base(nameOrConnectionString, model)
        {
        }

        public ChangeTrackingDbContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection) : base(existingConnection, model, contextOwnsConnection)
        {
        }

        protected ChangeTrackingDbContext()
        {
        }

        protected ChangeTrackingDbContext(DbCompiledModel model) : base(model)
        {
        }

        #endregion "Default Constructors"
    }
}