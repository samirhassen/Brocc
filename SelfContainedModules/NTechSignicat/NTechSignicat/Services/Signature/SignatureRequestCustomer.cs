using NTech.Banking.CivicRegNumbers;

namespace NTechSignicat.Services
{
    public class SignatureRequestCustomer
    {
        public ICivicRegNumber CivicRegNr { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int ApplicantNr { get; set; }
        public string UserLanguage { get; set; }
        public SignicatLoginMethodCode SignicatLoginMethod { get; set; }
    }
}
