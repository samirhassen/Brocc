using nCredit;
using NTech.Banking.BankAccounts;

namespace NTech.Core.Host.IntegrationTests
{
    public static class CreditAssertionExtensions
    {
        public static void VerifyThat(this OutgoingPaymentHeader source, SupportShared support, IBankAccountNumber? sourceAccountIs = null, IBankAccountNumber? targetAccountIs = null, decimal? shouldBePaidToCustomerIs = null)
        {
            Assert.That(source, Is.Not.Null);

            string? GetItem(OutgoingPaymentHeaderItemCode code) =>
                source.Items.SingleOrDefault(x => x.Name == code.ToString())?.Value;

            if (sourceAccountIs != null)
            {
                var itemCode = support.ClientConfiguration.Country.BaseCountry == "FI" ? OutgoingPaymentHeaderItemCode.FromIban : OutgoingPaymentHeaderItemCode.FromBankAccountNr;
                Assert.That(GetItem(itemCode), Is.EqualTo(sourceAccountIs.FormatFor(null)));
            }

            if (targetAccountIs != null)
            {
                var itemCode = support.ClientConfiguration.Country.BaseCountry == "FI" ? OutgoingPaymentHeaderItemCode.ToIban : OutgoingPaymentHeaderItemCode.ToBankAccountNr;
                Assert.That(GetItem(itemCode), Is.EqualTo(targetAccountIs.FormatFor(null)));
                Assert.That(GetItem(OutgoingPaymentHeaderItemCode.ToBankAccountNrType), Is.EqualTo(targetAccountIs.AccountType.ToString()));
            }

            if (shouldBePaidToCustomerIs.HasValue)
            {
                var sum = source.Transactions.Where(x => x.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString()).Sum(x => x.Amount);
                Assert.That(sum, Is.EqualTo(shouldBePaidToCustomerIs.Value));
            }
        }

        public static void VerifyThat(this CreditHeader source, SupportShared support, decimal? capitalDebtIs = null)
        {
            if (capitalDebtIs.HasValue)
            {
                var capitalDebt = source.Transactions.Where(x => x.AccountCode == TransactionAccountType.CapitalDebt.ToString()).Sum(x => x.Amount);
                Assert.That(capitalDebt, Is.EqualTo(capitalDebtIs.Value));
            }
        }
    }
}
