using System.Collections.Generic;

namespace nPreCredit.Controllers.Api
{
    public class SameAddressHit
    {
        public int CustomerId { get; set; }
        public int ApplicantNr { get; set; }
        public List<string> ApplicationNrs { get; set; }
    }
}