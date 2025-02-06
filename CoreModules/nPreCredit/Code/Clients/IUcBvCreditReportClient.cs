using System;
using System.Collections.Generic;

namespace nPreCredit.Code
{
    public interface IUcBvCreditReportClient
    {
        Tuple<bool, List<UcbvSokAdressHit>, string> UcbvSokAddress(string adress, string postnr, string postort, string kommun);
        Tuple<bool, UcbvObjectInfo, string> UcbvHamtaObjekt(string id);
        Tuple<bool, VarderaBostadsrattResult, string> UcbvVarderaBostadsratt(UcbvVarderaBostadsrattRequest request);
    }

    public class UcbvVarderaBostadsrattRequest
    {
        public string objektID { get; set; }
        public string yta { get; set; }
        public string skvlghnr { get; set; }
        public string avgift { get; set; }
        public string vaning { get; set; }
        public string rum { get; set; }
        public string kontraktsdatum { get; set; }
        public string kopesumma { get; set; }
        public string brflghnr { get; set; }
    }

    public class VarderaBostadsrattResult
    {
        public int? Varde { get; set; }
        public int? Vardeupp { get; set; }
        public int? Vardener { get; set; }
        public int? Alternativavgift { get; set; }
        public int? Andelskulder { get; set; }
        public decimal? XKoord { get; set; }
        public decimal? YKoord { get; set; }
        public List<string> Meddelande { get; set; }
        public bool? Oakta { get; set; }

        public BrfinfoModel Brfinfo { get; set; }

        public BrfekonomiModel Brfekonomi { get; set; }

        public List<KopiomradetModel> Kopiomradet { get; set; }
        public BrfsignalModel Brfsignal { get; set; }
        public string RawJson { get; set; }
        public class BrfsignalModel
        {
            public int? Ar { get; set; }
            public int? Belaning { get; set; }
            public string GetBelaningCode()
            {
                return BrfSignalToCode(Belaning);
            }
            public int? Likviditet { get; set; }
            public string GetLikviditetCode()
            {
                return BrfSignalToCode(Likviditet);
            }
            public int? Rantekanslighet { get; set; }
            public string GetRantekanslighetCode()
            {
                return BrfSignalToCode(Rantekanslighet);
            }
            public int? Sjalvforsorjningsgrad { get; set; }
            public string GetSjalvforsorjningsgradCode()
            {
                return BrfSignalToCode(Sjalvforsorjningsgrad);
            }

            private string BrfSignalToCode(int? value)
            {
                if (value == null)
                    return null;
                switch (value.Value)
                {
                    case 0: return "Okand";
                    case 1: return "OK";
                    case 2: return "Varning";
                    default: return $"Kod{value.Value}";
                }
            }
        }

        public class KopiomradetModel
        {
            public int? Nummer { get; set; }
            public string Forening { get; set; }
            public string Adress { get; set; }
            public int? Avgift { get; set; }
            public int? Hiss { get; set; }
            public bool? GetHissCode()
            {
                if (Hiss == null)
                    return null;
                switch (Hiss.Value)
                {
                    case 1: return true;
                    case 2: return false;
                    default: return null;
                }
            }
            public int? Balkong { get; set; }
            public bool? GetBalkongCode()
            {
                if (Hiss == null)
                    return null;
                switch (Hiss.Value)
                {
                    case 1: return true;
                    case 2: return false;
                    default: return null;
                }
            }

            public decimal? Yta { get; set; }
            public int? Kopesumma { get; set; }
            public string Kontraktsdatum { get; set; }

            public decimal? Antalrum { get; set; }
            public decimal? XKoord { get; set; }
            public decimal? YKoord { get; set; }
        }

        public class BrfekonomiModel
        {
            public int? Skuld { get; set; }
            public int? SkuldPerKvm { get; set; }
            public int? SkuldAr { get; set; }
            public int? SumRantor { get; set; }
        }

        public class BrfinfoModel
        {
            public string Orgnr { get; set; }
            public string Namn { get; set; }

            public string Adress { get; set; }
            public string COAdress { get; set; }
            public string PostNr { get; set; }
            public string PostOrt { get; set; }

            public string Kommun { get; set; }

            public int? Organisationsform { get; set; }

            public string GetOrganisationsformCode()
            {
                if (Organisationsform == null)
                    return null;
                switch (Organisationsform.Value)
                {
                    case 0: return "Okand";
                    case 1: return "BF";
                    case 2: return "BRF";
                    case 3: return "KB";
                    case 4: return "AB";
                    case 5: return "EK";
                    default: return $"Kod{Organisationsform.Value}";
                }
            }

            public string BrfStatus { get; set; }
            public int? SummaBostadsYta { get; set; }
            public int? SummaLokalYta { get; set; }
            public int? AntalLagenheter { get; set; }
            public string ByggAr { get; set; }
            public string ForvarvsAr { get; set; }

            public List<BrfAgare> Agare { get; set; }
        }

        public class BrfAgare
        {
            public int? Agartyp { get; set; }
            public string GetAgartypCode()
            {
                if (Agartyp == null)
                    return null;
                switch (Agartyp.Value)
                {
                    case 0: return "Okand";
                    case 1: return "Lagfart";
                    case 2: return "Tomtratt";
                    case 3: return "Taxerad";
                    default: return $"Kod{Agartyp.Value}";
                }
            }
            public string Orgnr { get; set; }
            public string Namn { get; set; }
        }
    }


    public class UcbvSokAdressHit
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class UcbvObjectInfo
    {
        public string Kommun { get; set; }
        public List<string> Adress { get; set; }
        public string Fastighet { get; set; }
        public string Forening { get; set; }
        public string Objekttyp { get; set; }
        public string Kommentar { get; set; }

        public List<Lagenhet> Lagenheter { get; set; }

        public class Lagenhet
        {
            public string Lghnr { get; set; }
            public decimal? Boarea { get; set; }
            public int? Vaning { get; set; }
            public string Rum { get; set; }
        }
    }
}