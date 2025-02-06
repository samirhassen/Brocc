using System.Xml.Linq;

namespace NTech.Core.PreCredit.Shared.Code.PetrusOnlyScoringService
{
    public interface IPetrusOnlyScoringService
    {
        PetrusOnlyCreditCheckResponse NewCreditCheck(PetrusOnlyCreditCheckRequest request);
        XDocument GetPetrusLog(string applicationId);
    }

    public class PetrusSettings
    {
        public bool IsEnabled { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Url { get; set; }
    }
}
