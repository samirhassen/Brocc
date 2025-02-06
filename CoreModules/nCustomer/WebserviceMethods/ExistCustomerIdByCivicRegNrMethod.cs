using nCustomer.DbModel;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Linq;

namespace nCustomer.WebserviceMethods
{
    public class ExistCustomerByCustomerIdMethod : TypedWebserviceMethod<ExistCustomerByCustomerIdMethod.Request, ExistCustomerByCustomerIdMethod.Response>
    {
        public override string Path => "ExistCustomerByCustomerId";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {
                x.Require(r => r.CustomerId);
            });

            return new Response
            {

                Exist = ExistCustomerByCustomerId(request.CustomerId.Value)
            };
        }

        private static bool ExistCustomerByCustomerId(int customerId, CustomersContext context = null)
        {
            Func<CustomersContext, bool> exist = c => c.CustomerProperties.Count(x => x.CustomerId == customerId) > 0;
            if (context != null)
                return exist(context);
            else
            {
                using (var cx = new CustomersContext())
                {
                    return exist(cx);
                }
            }
        }

        public class Request
        {
            public int? CustomerId { get; set; }
        }

        public class Response
        {
            public bool Exist { get; set; }
        }
    }
}