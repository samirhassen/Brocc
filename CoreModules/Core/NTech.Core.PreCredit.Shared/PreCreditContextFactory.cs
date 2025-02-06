using nPreCredit.Code.Services;
using System;

namespace NTech.Core.PreCredit.Shared
{
    /// <summary>
    /// Used to allow legacy and core to have shared services that depend on PreCreditContext by injecting this.
    /// </summary>
    public class PreCreditContextFactory : IPreCreditContextFactoryService
    {
        private readonly Func<IPreCreditContextExtended> createContext;

        public PreCreditContextFactory(Func<IPreCreditContextExtended> createContext)
        {
            this.createContext = createContext;
        }

        public IPreCreditContextExtended CreateContext() => createContext();

        public IPreCreditContextExtended CreateExtended() => createContext();
    }
}
