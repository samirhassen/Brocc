using System;

namespace NTech.Core.User.Shared
{
    /// <summary>
    /// Used to allow legacy and core to have shared services that depend on PreCreditContext by injecting this.
    /// </summary>
    public class UserContextFactory
    {
        private readonly Func<IUserContextExtended> createContext;

        public UserContextFactory(Func<IUserContextExtended> createContext)
        {
            this.createContext = createContext;
        }

        public IUserContextExtended CreateContext() => createContext();
    }
}
