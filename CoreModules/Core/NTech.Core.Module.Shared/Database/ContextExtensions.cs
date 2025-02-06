using NTech.Core.Module.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;

namespace NTech.Core
{
    public static class ContextExtensions
    {
        public static void EnsureCurrentTransaction(this INTechDbContext source)
        {
            if (!source.HasCurrentTransaction)
                throw new NTechCoreWebserviceException("Requires transaction") { ErrorCode = "requiresTransaction" };
        }

        public static T UsingTransaction<T>(this INTechDbContext source, Func<T> f)
        {
            if (source.HasCurrentTransaction)
                throw new NTechCoreWebserviceException("Transaction already active") { ErrorCode = "alreadyHasTransaction" };
            source.BeginTransaction();
            try
            {
                var result = f();
                source.CommitTransaction();
                return result;
            }
            catch
            {
                source.RollbackTransaction();
                throw;
            }
        }

        public static void DoUsingTransaction(this INTechDbContext source, Action a)
        {
            UsingTransaction<object>(source, () =>
            {
                a();
                return null;
            });
        }
    }
}
