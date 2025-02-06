using NTech.Core.Module.Shared.Database;

namespace nPreCredit
{
    public class FraudControlItem : InfrastructureBaseItem
    {
        public const string Initial = "Initial";
        public const string Rejected = "Rejected";
        public const string Approved = "Approved";
        public const string Verified = "Verified";

        public const string CheckPhone = "PhoneCheck";
        public const string CheckEmployment = "EmploymentCheck";
        public const string CheckEmail = "SameEmailCheck";
        public const string CheckAccountNr = "SameAccountNrCheck";
        public const string CheckAddress = "SameAddressCheck";
        public const string CheckOtherApprovedLoanRecently = "OtherApprovedLoanRecentlyCheck";

        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Status { get; set; }

        public FraudControl FraudControl { get; set; }
        public int? FraudControl_Id { get; set; }
    }
}