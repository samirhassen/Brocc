using System;

namespace NTech.Core.Savings.Shared.Database
{
    public class SavingsContextFactory
    {
        private readonly Func<ISavingsContext> createContext;
        private readonly Func<Exception, bool> isConcurrencyException;

        public SavingsContextFactory(Func<ISavingsContext> createContext, Func<Exception, bool> isConcurrencyException)
        {
            this.createContext = createContext;
            this.isConcurrencyException = isConcurrencyException;
        }

        public ISavingsContext CreateContext() => createContext();
        public bool IsConcurrencyException(Exception exception) => isConcurrencyException(exception);
    }
}