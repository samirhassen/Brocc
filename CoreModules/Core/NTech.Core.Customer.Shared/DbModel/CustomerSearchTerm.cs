using NTech.Core.Module.Shared.Database;

namespace nCustomer.DbModel
{
    public enum SearchTermCode
    {
        email,
        firstName,
        lastName,
        companyNameNormalized,
        companyNamePhonetic,
        phone
    }

    public class CustomerSearchTerm : InfrastructureBaseItem
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string TermCode { get; set; }
        public string Value { get; set; }
        public bool IsActive { get; set; }
    }
}