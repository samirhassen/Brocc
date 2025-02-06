using nCredit.DomainModel;
using System;
using System.Collections.Generic;

namespace nCredit.Code.Fileformats
{
    public class DebtCollectionFileModel
    {
        public string ExternalId { get; set; }
        public List<Credit> Credits { get; set; }

        public class Credit
        {
            public string CreditNr { get; set; }
            public bool IsCompanyLoan { get; set; }
            public string Currency { get; set; }
            public string Ocr { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime NextInterestDate { get; set; }
            public DateTime TerminationLetterDueDate { get; set; }
            public Dictionary<int, int> ApplicantNrByCustomerId { get; set; }
            public List<Tuple<Customer, HashSet<string>>> OrderedCustomersWithRoles { get; set; }
            public List<Notification> Notifications { get; set; }
            public decimal InterestRatePercent { get; set; }
            public decimal NotNotifiedCapitalAmount { get; set; }
            public decimal CapitalizedInitialFeeAmount { get; set; }
            public decimal NewCreditCapitalAmount { get; set; }
            public decimal AdditionalLoanCapitalAmount { get; set; }
            public string InitialLoanCampaignCode { get; set; }
        }

        public class Customer
        {
            public int CustomerId { get; set; }
            public string CivicRegNrOrOrgnr { get; set; }
            public string CivicRegNrOrOrgnrCountry { get; set; }
            public bool IsCompany { get; set; }
            public string CompanyName { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string PreferredLanguage { get; set; }
            public Address Adr { get; set; }
        }

        public class Notification
        {
            public IDictionary<string, decimal> Amounts { get; set; }
            public DateTime NotificationDate { get; set; }
            public DateTime DueDate { get; set; }
        }

        public class Address
        {
            public string Street { get; set; }
            public string Zipcode { get; set; }
            public string City { get; set; }
            public string Country { get; set; }
        }
    }
}