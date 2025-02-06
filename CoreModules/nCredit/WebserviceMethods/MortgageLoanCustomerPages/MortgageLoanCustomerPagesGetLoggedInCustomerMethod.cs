using nCredit.Code;
using NTech.Services.Infrastructure.NTechWs;
using System;

namespace nCredit.WebserviceMethods.MortgageLoanCustomerPages
{
    public class MortgageLoanCustomerPagesGetLoggedInCustomerMethod : MortgageLoanCustomerPagesMethod<MortgageLoanCustomerPagesGetLoggedInCustomerMethod.Request, MortgageLoanCustomerPagesGetLoggedInCustomerMethod.Response>
    {
        protected override string MethodName => "loggedin-customer-info";

        protected override Response DoCustomerLockedExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request, int customerPagesUserCustomerId)
        {
            var cc = new CreditCustomerClient();
            var items = cc.GetCustomerCardItems(customerPagesUserCustomerId, "civicRegNr", "firstName", "lastName", "addressStreet", "addressZipcode", "addressCity", "addressCountry", "email", "phone");
            return new Response
            {
                CivicRegNr = items.Opt("civicRegNr"),
                FirstName = items.Opt("firstName"),
                LastName = items.Opt("lastName"),
                Contact = new Response.ContactInfoModel
                {
                    AddressCity = items.Opt("addressCity"),
                    AddressCountry = items.Opt("addressCountry"),
                    AddressStreet = items.Opt("addressStreet"),
                    AddressZipcode = items.Opt("addressZipcode"),
                    Email = items.Opt("email"),
                    Phone = items.Opt("phone")
                }
            };
        }

        public class Request : MortgageLoanCustomerPagesRequestBase
        {
        }

        public class Response
        {
            public string CivicRegNr { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public ContactInfoModel Contact { get; set; }
            public class ContactInfoModel
            {
                public string AddressStreet { get; set; }
                public string AddressZipcode { get; set; }
                public string AddressCity { get; set; }
                public string AddressCountry { get; set; }
                public string Email { get; set; }
                public string Phone { get; set; }
            }
        }
    }
}