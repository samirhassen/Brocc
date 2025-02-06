using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nSavings.DbModel
{
    public abstract class ChangeTrackingDbContext : DbContext
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
            try
            {
                return base.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                var sb = new StringBuilder();

                foreach (var failure in ex.EntityValidationErrors)
                {
                    sb.AppendFormat("{0} failed validation\n", failure.Entry.Entity.GetType());
                    foreach (var error in failure.ValidationErrors)
                    {
                        sb.AppendFormat("- {0} : {1}", error.PropertyName, error.ErrorMessage);
                        sb.AppendLine();
                    }
                }

                throw new DbEntityValidationException(
                    "Entity Validation Failed - errors follow:\n" +
                    sb.ToString(), ex
                );
            }
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