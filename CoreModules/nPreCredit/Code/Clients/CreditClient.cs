using nCredit.DbModel.BusinessEvents.NewCredit;
using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.Code
{
    public class CreditClient : AbstractServiceClient, ICreditClient
    {
        protected override string ServiceName => "nCredit";

        private class NewCreditNumberResult
        {
            public string Nr { get; set; }
        }

        public string NewCreditNumber()
        {
            return Begin()
                .PostJson("Api/NewCreditNumber", new { })
                .ParseJsonAs<NewCreditNumberResult>()
                .Nr;
        }

        public (string PayerNr, string ClientBankGiroNr) GenerateDirectDebitPayerNumber(string creditNr, int applicantNr)
        {
            var result = Begin()
                .PostJson("Api/DirectDebit/Generate-PayerNumber", new { creditNr, applicantNr })
                .ParseJsonAsAnonymousType(new { PayerNr = "", ClientBankGiroNr = "" });
            return (PayerNr: result?.PayerNr, ClientBankGiroNr: result?.ClientBankGiroNr);
        }

        public Tuple<List<string>, List<string>> GenerateReferenceNumbers(int creditNrCount, int ocrNrCount)
        {
            var r = Begin()
                .PostJson("Api/Credit/Generate-Reference-Numbers", new { CreditNrCount = creditNrCount, OcrNrCount = ocrNrCount })
                .ParseJsonAsAnonymousType(new { CreditNrs = (List<string>)null, OcrNrs = (List<string>)null });
            return Tuple.Create(r.CreditNrs, r.OcrNrs);
        }

        public (decimal? ReminderFeeAmount, int? NrOfFreeInitialReminders) FetchNotificationProcessSettings()
        {
            var r = Begin()
                .PostJson("api/Credit/Fetch-Notification-Process-Settings", new { })
                .ParseJsonAsAnonymousType(new { ReminderFeeAmount = (decimal?)null, NrOfFreeInitialReminders = (int?)null });
            return (ReminderFeeAmount: r.ReminderFeeAmount, NrOfFreeInitialReminders: r.NrOfFreeInitialReminders);
        }

        private class GetCurrentReferenceInterestResult
        {
            public decimal ReferenceInterestRatePercent { get; set; }
        }

        public decimal GetCurrentReferenceInterest()
        {
            return Begin()
                .PostJson("Api/ReferenceInterest/GetCurrent", new { })
                .ParseJsonAs<GetCurrentReferenceInterestResult>()
                .ReferenceInterestRatePercent;
        }

        public CreditCollateral FetchCreditCollateral(int collateralId)
        {
            var result = Begin()
                .PostJson("Api/MortgageLoans/Fetch-Collaterals", new { CollateralIds = new[] { collateralId } })
                .ParseJsonAsAnonymousType(new
                {
                    Collaterals = new CreditCollateral[] { }
                });
            return result?.Collaterals?.SingleOrDefault();
        }

        public class CreditCollateral
        {
            public int CollateralId { get; set; }
            public Dictionary<string, CreditCollateralItem> CollateralItems { get; set; }
            public class CreditCollateralItem
            {
                public string ItemName { get; set; }
                public string StringValue { get; set; }
            }
        }

        public void CreateCredits(NewCreditRequest[] newCreditRequests, NewAdditionalLoanRequest[] additionalLoanRequests)
        {
            Begin()
                .PostJson("Api/CreateCredits", new { newCreditRequests = newCreditRequests, additionalLoanRequests = additionalLoanRequests })
                .EnsureSuccessStatusCode();
        }

        public void CreateMortgageLoans(MortgageLoanRequest[] mortgageLoanRequests)
        {
            Begin()
                .PostJson("Api/MortgageLoans/Create-Bulk", new { Loans = mortgageLoanRequests })
                .EnsureSuccessStatusCode();
        }

        public void CreateMortgageLoan(MortgageLoanRequest mortgageLoanRequest)
        {
            Begin()
                .PostJson("Api/MortgageLoans/Create", mortgageLoanRequest)
                .EnsureSuccessStatusCode();
        }

        public List<HistoricalCredit> GetCustomerCreditHistory(List<int> customerIds)
        {
            if (customerIds != null && customerIds.Count == 0)
                return new List<HistoricalCredit>();
            return Begin()
                .PostJson("Api/CustomerCreditHistoryBatch", new { customerIds = customerIds })
                .ParseJsonAsAnonymousType(new { Credits = (List<HistoricalCredit>)null })
                .Credits;
        }

        public List<HistoricalCredit> GetCustomerCreditHistoryByCreditNrs(List<string> creditNrs)
        {
            if (creditNrs != null && creditNrs.Count == 0)
                return new List<HistoricalCredit>();
            return Begin()
                .PostJson("Api/CustomerCreditHistoryByCreditNrs", new { creditNrs = creditNrs })
                .ParseJsonAsAnonymousType(new { Credits = (List<HistoricalCredit>)null })
                .Credits;
        }

        public List<string> CreateCompanyCredits(CreateCompanyCreditsRequest createCompanyCreditsRequest)
        {
            return Begin()
                .PostJson("Api/CompanyCredit/Create-Batch", createCompanyCreditsRequest)
                .ParseJsonAsAnonymousType(new { CreditNrs = (List<string>)null })
                ?.CreditNrs;
        }
    }
}