using System.Collections.Generic;

namespace nCustomerPages.Code
{

    public class MortgageLoanApplicationRequest
    {
        public string UserLanguage { get; set; }
        public int NrOfApplicants { get; set; }
        public Item[] Items { get; set; }
        public class Item
        {
            public string Group { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
        }
        public string ProviderName { get; set; }
        public string ApplicationRequestJson { get; set; }
        public MortgageLoanObjectModel MortageLoanObject { get; set; }
        public MortgageLoanCurrentLoansModel MortgageLoanCurrentLoans { get; set; }

        public class MortgageLoanObjectModel
        {
            /// <summary>
            /// 1 &#x3D; Bostadsrätt, 2 &#x3D; Villa, 3 &#x3D; Fritidshus
            /// </summary>
            /// <value>1 &#x3D; Bostadsrätt, 2 &#x3D; Villa, 3 &#x3D; Fritidshus</value>
            public int? PropertyType { get; set; }

            /// <summary>
            /// Bostadens värde/pris
            /// </summary>
            /// <value>Bostadens värde/pris</value>
            public int? PropertyEstimatedValue { get; set; }

            /// <summary>
            /// Kommun
            /// </summary>
            /// <value>Kommun</value>
            public string PropertyMunicipality { get; set; }

            /// <summary>
            /// Uppgifter om bostaden lånet avser
            /// </summary>
            /// <value>Uppgifter om bostaden lånet avser</value>
            public CondominiumPropertyDetailsModel CondominiumPropertyDetails { get; set; }

            public class CondominiumPropertyDetailsModel
            {
                /// <summary>
                /// Gatuadress
                /// </summary>
                /// <value>Gatuadress</value>
                public string Address { get; set; }

                /// <summary>
                /// Postnummer
                /// </summary>
                /// <value>Postnummer</value>
                public string PostalCode { get; set; }

                /// <summary>
                /// Postort
                /// </summary>
                /// <value>Postort</value>
                public string City { get; set; }

                /// <summary>
                /// Antal rum
                /// </summary>
                /// <value>Antal rum</value>
                public int? NumberOfRooms { get; set; }

                /// <summary>
                /// Yta (m2)
                /// </summary>
                /// <value>Yta (m2)</value>
                public int? LivingArea { get; set; }

                /// <summary>
                /// Antal trappor
                /// </summary>
                /// <value>Antal trappor</value>
                public int? Floor { get; set; }

                /// <summary>
                /// Bostadsrättsföreningens namn
                /// </summary>
                /// <value>Bostadsrättsföreningens namn</value>
                public string AssociationName { get; set; }

                /// <summary>
                /// Bostadsrättsföreningens orginisationsnummer
                /// </summary>
                /// <value>Bostadsrättsföreningens orginisationsnummer</value>
                public string AssociationNumber { get; set; }

                /// <summary>
                /// Månadsavgift till förening
                /// </summary>
                /// <value>Månadsavgift till förening</value>
                public int? MonthlyCost { get; set; }

                /// <summary>
                /// Lägenhetsnummer
                /// </summary>
                /// <value>Lägenhetsnummer</value>
                public int? ApartmentNumber { get; set; }

                /// <summary>
                /// Finns hiss?
                /// </summary>
                /// <value>Finns hiss?</value>
                public bool? Elevator { get; set; }

                /// <summary>
                /// 0 &#x3D; Saknas, 1 &#x3D; Balkong, 2 &#x3D; Altan
                /// </summary>
                /// <value>0 &#x3D; Saknas, 1 &#x3D; Balkong, 2 &#x3D; Altan</value>
                public int? PatioType { get; set; }

                /// <summary>
                /// Avser köpet nyproduktion och är kunden förste ägare?
                /// </summary>
                /// <value>Avser köpet nyproduktion och är kunden förste ägare?</value>
                public bool? NewConstruction { get; set; }
            }
        }
        public class MortgageLoanCurrentLoansModel
        {
            public class CurrentMortgageLoanModel
            {
                public string BankName { get; set; }
                public int? MonthlyAmortizationAmount { get; set; }
                public int? CurrentBalance { get; set; }
                public string LoanNr { get; set; }
            }
            public decimal? RequestedAmortizationAmount { get; set; }

            public List<CurrentMortgageLoanModel> Loans { get; set; }
        }
    }
}