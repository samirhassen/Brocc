using nCredit.Code;
using NTech.Services.Infrastructure.NTechWs;
using System;

namespace nCredit.WebserviceMethods.MortgageLoanCustomerPages
{
    public class MortgageLoanCustomerPagesFetchConfigMethod : MortgageLoanCustomerPagesMethod<MortgageLoanCustomerPagesFetchConfigMethod.Request, MortgageLoanCustomerPagesFetchConfigMethod.Response>
    {
        protected override string MethodName => "fetch-config";

        protected override Response DoCustomerLockedExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request, int customerPagesUserCustomerId)
        {
            var c = new CreditCustomerClient();
            var customerItems = c.GetCustomerCardItems(customerPagesUserCustomerId, "firstName", "lastName");
            return new Response
            {
                IsTest = !NEnv.IsProduction,
                Customer = new Response.CustomerModel
                {
                    CustomerId = customerPagesUserCustomerId,
                    FirstName = customerItems.Opt("firstName"),
                    LastName = customerItems.Opt("lastName")
                }
            };
        }

        public class Request : MortgageLoanCustomerPagesRequestBase
        {

        }

        public class Response
        {
            public bool IsTest { get; set; }
            public CustomerModel Customer { get; set; }
            public class CustomerModel
            {
                public int CustomerId { get; set; }
                public string FirstName { get; set; }
                public string LastName { get; set; }
            }
        }
    }
}