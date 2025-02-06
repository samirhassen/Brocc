using System;

namespace NTech.Core.Customer.Shared.Database
{
    /// <summary>
    /// Used to allow legacy and core to have shared services that depend on CustomerContext by injecting this.
    /// </summary>
    public class CustomerContextFactory
    {
        private readonly Func<ICustomerContextExtended> createContext;

        public CustomerContextFactory(Func<ICustomerContextExtended> createContext)
        {
            this.createContext = createContext;
        }

        public ICustomerContextExtended CreateContext() => createContext();
    }
}
