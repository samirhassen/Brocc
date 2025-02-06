using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Banking.BankAccounts
{
    public interface IBankAccountNumber
    {
        /// <summary>
        /// Shared format names:
        /// - null: Normalized, rountripable format
        /// - display: Formatted for user display
        /// 
        /// Specialized formats:
        /// - bgmax: Used for swedish bank account, bgmax files
        /// - pain.001.001.3: Used for swedish and finnish pain.001.001.3 files
        /// 
        /// </summary>
        /// <param name="formatName"></param>
        /// <returns></returns>
        string FormatFor(string formatName);
        string TwoLetterCountryIsoCode { get; }
        BankAccountNumberTypeCode AccountType { get; }
    }

    public enum BankAccountNumberTypeCode
    {
        IBANFi,
        /// <summary>
        /// International, not country defined, IBAN implementation. 
        /// </summary>
        IBAN,
        BankAccountSe,
        BankGiroSe,
        PlusGiroSe
    }
}
