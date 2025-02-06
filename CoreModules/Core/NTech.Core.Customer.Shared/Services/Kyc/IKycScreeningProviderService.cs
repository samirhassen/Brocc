using System;
using System.Collections.Generic;

namespace nCustomer.Code.Services.Kyc
{
    public class KycScreeningListHit
    {
        public string Name { get; set; }
        public DateTime? BirthDate { get; set; }
        public string ExternalId { get; set; }
        public string Ssn { get; set; }
        public string Title { get; set; }
        public List<string> Addresses { get; set; }
        public string Comment { get; set; }
        public List<string> ExternalUrls { get; set; }
        public string SourceName { get; set; }
        public bool IsPepHit { get; set; }
        public bool IsSanctionHit { get; set; }
    }

    public enum KycScreeningListCode
    {
        Pep,
        Sanction,
        All
    }

    public class KycScreeningQueryItem
    {
        public string ItemId { get; set; }
        public DateTime BirthDate { get; set; }
        public string FullName { get; set; }
        public List<string> TwoLetterIsoCountryCodes { get; set; }
        public string CivicRegNr { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        public ContactInfoModel ContactInfo { get; set; }

        public class ContactInfoModel
        {
            public string StreetAddress { get; set; }
            public string CareOfAddress { get; set; }
            public string ZipCode { get; set; }
            public string City { get; set; }
            public string Country { get; set; }
        }
    }

    public interface IKycScreeningProviderService
    {
        IDictionary<string, List<KycScreeningListHit>> Query(List<KycScreeningQueryItem> items, KycScreeningListCode list = KycScreeningListCode.All);
    }
}