using NTech.Services.Infrastructure.NTechWs;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCredit.WebserviceMethods.MortgageLoanCustomerPages
{
    public class MortgageLoanCustomerPagesGetLoanSecurityMethod : MortgageLoanCustomerPagesMethod<MortgageLoanCustomerPagesGetLoanSecurityMethod.Request, MortgageLoanCustomerPagesGetLoanSecurityMethod.Response>
    {
        protected override string MethodName => "loan-security";

        protected override Response DoCustomerLockedExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request, int customerPagesUserCustomerId)
        {
            using (var context = new CreditContext())
            {
                var c = context.CreditHeaders.Count(x => x.CreditNr == request.LoanNr && x.CreditCustomers.Any(y => y.CustomerId == customerPagesUserCustomerId));
                if (c == 0)
                    return Error("No such loan", httpStatusCode: 400, errorCode: "noSuchLoan");
            }
            var items = requestContext
                .Service()
                .CreditSecurity
                .FetchSecurityItems(request.LoanNr)
                .ToDictionary(x => x.Name, x => x);


            return new Response
            {
                SecurityType = "apartment",
                AddressStreet = items.Opt("brfLghAdressGata")?.StringValue,
                AddressCity = items.Opt("brfLghAdressPostort")?.StringValue,
                AddressZipcode = items.Opt("brfLghAdressPostnr")?.StringValue,
                ApartmentDetails = new Response.ApartmentDetailsModel
                {
                    AssociationName = items.Opt("brfNamn")?.StringValue,
                    AssociationOrganisationNumber = items.Opt("brfOrgnr")?.StringValue,
                    ValuationAmount = items.Opt("brfLghVarde")?.NumericValue,
                    ValuationDate = items.Opt("brfLghVarde")?.TransactionDate,
                    OfficalApartmentNumber = items.Opt("brfLghSkvLghNr")?.StringValue,
                    FloorNumber = (int?)NTech.Numbers.ParseDecimalOrNull(items.Opt("brfLghVaning")?.StringValue),
                    LivingArea = items.Opt("brfLghYta")?.NumericValue,
                    NumberOfRooms = NTech.Numbers.ParseDecimalOrNull(items.Opt("brfLghAntalRum")?.StringValue)
                }
            };
        }

        public class Request : MortgageLoanCustomerPagesRequestBase
        {
            [Required]
            public string LoanNr { get; set; }
        }

        public class Response
        {
            public string SecurityType { get; set; }

            public string AddressStreet { get; set; }
            public string AddressZipcode { get; set; }
            public string AddressCity { get; set; }

            public ApartmentDetailsModel ApartmentDetails { get; set; }

            public class ApartmentDetailsModel
            {
                public decimal? NumberOfRooms { get; set; }
                public decimal? LivingArea { get; set; }
                public int? FloorNumber { get; set; }
                public decimal? ValuationAmount { get; set; }
                public DateTime? ValuationDate { get; set; }
                public string OfficalApartmentNumber { get; set; }
                public string AssociationName { get; set; }
                public string AssociationOrganisationNumber { get; set; }
            }
        }
    }
}