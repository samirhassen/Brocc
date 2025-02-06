using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PsdTwoPrototype.Models
{

    public class Rootobject
    {
        public Ruleresponse ruleResponse { get; set; }
        public object errorMessage { get; set; }
        public string requestId { get; set; }
        public DateTime timestamp { get; set; }
        public string usedCurrency { get; set; }
    }

    public class Ruleresponse
    {
        public Abilitytopay abilityToPay { get; set; }
        public Balancetrend[] balanceTrend { get; set; }
        public Basicconsumption basicConsumption { get; set; }
        public Cashdeposit[] cashDeposits { get; set; }
        public Cashwithdrawal[] cashWithdrawals { get; set; }
        public Consentinfo consentInfo { get; set; }
        public string consumptionBarometer { get; set; }
        public string currentBalanceAverage { get; set; }
        public Debtcollection[] debtCollection { get; set; }
        public bool forestProperty { get; set; }
        public Gambling gambling { get; set; }
        public Housingcompanytransaction[] housingCompanyTransactions { get; set; }
        public Income income { get; set; }
        public bool incomeSourceFound { get; set; }
        public Incometrend[] incomeTrend { get; set; }
        public string[] largestNegativeTransactionPerMonth { get; set; }
        public Loan[] loans { get; set; }
        public Minimumbalanceofallaccounts minimumBalanceOfAllAccounts { get; set; }
        public string minimumTotalBalanceOfAllAccounts { get; set; }
        public int mobilePaymentActivity { get; set; }
        public Monthlyoverdrafts monthlyOverdrafts { get; set; }
        public string mostActiveIban { get; set; }
        public Negativetransactiondistribution negativeTransactionDistribution { get; set; }
        public int numberOfAuthorisedAccounts { get; set; }
        public int numberOfRecentlyUsedAccounts { get; set; }
        public int[] numberOfTransactions { get; set; }
        public bool over10Transactions { get; set; }
        public int payDayConsumption { get; set; }
        public int payDayConsumptionAverage6Months { get; set; }
        public Rawdata[] rawData { get; set; }
        public Balance1[] balances { get; set; }
        public string sumOfCurrentBalances { get; set; }
        public Transactiondistribution transactionDistribution { get; set; }
        public Typicaltransaction typicalTransaction { get; set; }
        public bool vehicleOwnershipIndicator { get; set; }
        public Unevaluatedrule[] unevaluatedRules { get; set; }
    }

    public class Abilitytopay
    {
        public string[] activeness { get; set; }
        public int[] incomeRate { get; set; }
        public int[] regularIncomeRate { get; set; }
        public Overindebtednessrate[] overindebtednessRate { get; set; }
    }

    public class Overindebtednessrate
    {
        public float regularIncome { get; set; }
        public float allIncomes { get; set; }
        public float allIncomesWithoutLoans { get; set; }
    }

    public class Basicconsumption
    {
        public object mostActiveAccount { get; set; }
        public string paymentsToInsuranceCompanies3Months { get; set; }
        public string paymentsToInsuranceCompanies6Months { get; set; }
        public string paymentsToInsuranceCompanies12Months { get; set; }
        public object[] insuranceCompanies { get; set; }
    }

    public class Consentinfo
    {
        public bool consentGiven { get; set; }
        public string consentDate { get; set; }
        public string bank { get; set; }
        public string[] ibans { get; set; }
        public object[] accountOwners { get; set; }
    }

    public class Gambling
    {
        public string sum6MonthsNegative { get; set; }
        public string sum12MonthsNegative { get; set; }
        public object[] top3LargestNegative { get; set; }
        public string sum6MonthsPositive { get; set; }
        public string sum12MonthsPositive { get; set; }
        public object[] top3LargestPositive { get; set; }
    }

    public class Income
    {
        public Largestpositivetransaction[] largestPositiveTransactions { get; set; }
        public Largestincomesource[] largestIncomeSources { get; set; }
    }

    public class Largestpositivetransaction
    {
        public string name { get; set; }
        public bool lender { get; set; }
        public int existingMonthCount { get; set; }
        public int[] frequencyTrend { get; set; }
        public string incomeFor6Months { get; set; }
        public string incomeFor12Months { get; set; }
    }

    public class Largestincomesource
    {
        public string name { get; set; }
        public bool lender { get; set; }
        public int existingMonthCount { get; set; }
        public int[] frequencyTrend { get; set; }
        public string incomeFor6Months { get; set; }
        public string incomeFor12Months { get; set; }
    }

    public class Minimumbalanceofallaccounts
    {
        public string iban { get; set; }
        public string minimumbalance { get; set; }
    }

    public class Monthlyoverdrafts
    {
        public int[] overdrafts { get; set; }
        public object[] possibleCreditCardAccounts { get; set; }
    }

    public class Negativetransactiondistribution
    {
        public int shareOfPublicTransactions { get; set; }
        public int privateTransactionsCount { get; set; }
        public int publicTransactionsCount { get; set; }
    }

    public class Transactiondistribution
    {
        public int shareOfPublicTransactions { get; set; }
        public int privateTransactionsCount { get; set; }
        public int publicTransactionsCount { get; set; }
    }

    public class Typicaltransaction
    {
        public string average { get; set; }
        public string median { get; set; }
    }

    public class Balancetrend
    {
        public string iban { get; set; }
        public string[] trend { get; set; }
    }

    public class Cashdeposit
    {
        public int count { get; set; }
        public string total { get; set; }
        public string average { get; set; }
        public int cumulativeCount { get; set; }
        public string cumulativeTotal { get; set; }
        public string cumulativeAverage { get; set; }
    }

    public class Cashwithdrawal
    {
        public int count { get; set; }
        public string total { get; set; }
        public string average { get; set; }
        public int cumulativeCount { get; set; }
        public string cumulativeTotal { get; set; }
        public string cumulativeAverage { get; set; }
    }

    public class Debtcollection
    {
        public int count { get; set; }
        public string total { get; set; }
        public string average { get; set; }
        public object[] companies { get; set; }
        public int cumulativeCount { get; set; }
        public string cumulativeTotal { get; set; }
        public string cumulativeAverage { get; set; }
    }

    public class Housingcompanytransaction
    {
        public int count { get; set; }
        public string total { get; set; }
        public object[] companies { get; set; }
    }

    public class Incometrend
    {
        public string iban { get; set; }
        public string[] trend { get; set; }
    }

    public class Loan
    {
        public string totalPayouts { get; set; }
        public int payoutsCount { get; set; }
        public string totalDrawdowns { get; set; }
        public int drawdownsCount { get; set; }
        public string averagePayout { get; set; }
        public object averageDrawdown { get; set; }
        public Payoutspercreditor[] payoutsPerCreditor { get; set; }
        public object[] drawdownsPerCreditor { get; set; }
    }

    public class Payoutspercreditor
    {
        public string creditorName { get; set; }
        public string creditorType { get; set; }
        public string amount { get; set; }
        public string currency { get; set; }
    }

    public class Rawdata
    {
        public string currency { get; set; }
        public string iban { get; set; }
        public string accountName { get; set; }
        public Balance[] balances { get; set; }
        public Transaction[] transactions { get; set; }
    }

    public class Balance
    {
        public Balanceamount balanceAmount { get; set; }
        public string balanceType { get; set; }
        public object creditLimitIncluded { get; set; }
        public object lastChangeDateTime { get; set; }
        public string referenceDate { get; set; }
        public object lastCommittedTransaction { get; set; }
    }

    public class Balanceamount
    {
        public string currency { get; set; }
        public string amount { get; set; }
    }

    public class Transaction
    {
        public string transactionId { get; set; }
        public object entryReference { get; set; }
        public object endToEndId { get; set; }
        public object mandateId { get; set; }
        public object checkId { get; set; }
        public string creditorId { get; set; }
        public string bookingDate { get; set; }
        public string valueDate { get; set; }
        public Transactionamount transactionAmount { get; set; }
        public object[] currencyExchange { get; set; }
        public string creditorName { get; set; }
        public Creditoraccount creditorAccount { get; set; }
        public object ultimateCreditor { get; set; }
        public string debtorName { get; set; }
        public Debtoraccount debtorAccount { get; set; }
        public object ultimateDebtor { get; set; }
        public object remittanceInformationUnstructured { get; set; }
        public object remittanceInformationStructured { get; set; }
        public object additionalInformation { get; set; }
        public object purposeCode { get; set; }
        public object bankTransactionCode { get; set; }
        public object proprietaryBankTransactionCode { get; set; }
        public object links { get; set; }
    }

    public class Transactionamount
    {
        public string currency { get; set; }
        public string amount { get; set; }
    }

    public class Creditoraccount
    {
        public string iban { get; set; }
        public object bban { get; set; }
        public object pan { get; set; }
        public object maskedPan { get; set; }
        public object msisdn { get; set; }
        public object currency { get; set; }
    }

    public class Debtoraccount
    {
        public object iban { get; set; }
        public object bban { get; set; }
        public object pan { get; set; }
        public object maskedPan { get; set; }
        public object msisdn { get; set; }
        public object currency { get; set; }
    }

    public class Balance1
    {
        public string iban { get; set; }
        public string balance { get; set; }
    }

    public class Unevaluatedrule
    {
        public string rule { get; set; }
        public string reason { get; set; }
    }



}
