using System.Collections.Generic;

namespace nCreditReport.Code.PropertyValuation.UcBvSe
{
    public class HamtaObjektInfoResult
    {
        public string Kommun { get; set; }
        public List<Address> Adress2 { get; set; }
        public string Fastighet { get; set; }
        public string Forening { get; set; }
        public string Objekttyp { get; set; }
        public string Kommentar { get; set; }

        public List<Lagenhet> Lagenheter { get; set; }

        public class Lagenhet
        {
            public string Lghnr { get; set; }
        }

        public class Address
        {
            public string Adress { get; set; }
            public string Postnummer { get; set; }
            public string Postort { get; set; }
        }
    }
}