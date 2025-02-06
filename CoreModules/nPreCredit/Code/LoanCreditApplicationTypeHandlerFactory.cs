using System;

namespace nPreCredit.Code
{
    public static class LoanCreditApplicationTypeHandlerFactory
    {
        public static bool IsUnsecuredLoanApplication(string applicationType)
        {
            return string.IsNullOrWhiteSpace(applicationType) || applicationType.Equals("unsecuredLoan", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsMortgageLoanApplication(string applicationType)
        {
            return (applicationType?.Equals("mortgageLoan", StringComparison.OrdinalIgnoreCase) ?? false);
        }
    }
}