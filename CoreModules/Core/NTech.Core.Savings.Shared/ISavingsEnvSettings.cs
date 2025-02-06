using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.BankAccounts.Se;
using NTech.Core.Module.Shared;
using System;
using System.IO;

namespace NTech.Core.Savings.Shared
{
    public interface ISavingsEnvSettings : ISharedEnvSettings
    {
        decimal MaxAllowedSavingsCustomerBalance { get; }
        string OutgoingPaymentFileCustomerMessagePattern { get; }
        IBANFi OutgoingPaymentIban { get; }
    }
}
