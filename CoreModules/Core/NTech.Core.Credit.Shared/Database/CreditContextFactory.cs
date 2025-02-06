using System;

namespace NTech.Core.Credit.Shared.Database
{
    /// <summary>
    /// Used to allow legacy and core to have shared services that depend on CreditContext by injecting this.
    /// </summary>
    public class CreditContextFactory
    {
        private readonly Func<ICreditContextExtended> createContext;

        public CreditContextFactory(Func<ICreditContextExtended> createContext)
        {
            this.createContext = createContext;
        }

        public ICreditContextExtended CreateContext() => createContext();
    }
}
