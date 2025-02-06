using System;
using System.Data;

namespace NTech.Core.Module.Shared.Database
{
    public interface INTechDbContext : IDisposable
    {
        IDbConnection GetConnection();
        void BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();
        bool HasCurrentTransaction { get; }
        IDbTransaction CurrentTransaction { get; }
        bool IsChangeTrackingEnabled { get; set; }
        void DetectChanges();
    }
}
