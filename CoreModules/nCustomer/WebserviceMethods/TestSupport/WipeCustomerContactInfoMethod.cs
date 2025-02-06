using Dapper;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCustomer.WebserviceMethods
{
    public class WipeCustomerContactInfoMethod : TypedWebserviceMethod<WipeCustomerContactInfoMethod.Request, WipeCustomerContactInfoMethod.Response>
    {
        public override string Path => "TestSupport/Wipe-Customer-ContactInfo";

        public override bool IsEnabled => !NEnv.IsProduction;

        private static HashSet<string> NamesToDelete = new HashSet<string>
        {
            "phone",
            "email",
            "companyName",
            "firstName",
            "lastName",
            "addressHash",
            "addressStreet",
            "addressZipcode",
            "addressCity",
            "addressCountry"
        };

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var r = new Response();
            using (var context = new DbModel.CustomersContext())
            {
                var c = context.Database.Connection;
                c.Open();
                var tr = c.BeginTransaction();
                try
                {
                    var names = string.Join(",", NamesToDelete.Select(x => $"'{x}'"));
                    c.Execute($"delete from CustomerCardConflict where name in ({names}) and ApprovedDate is null and DiscardedDate is null and CustomerId in @customerIds", new { customerIds = request.CustomerIds }, transaction: tr, commandTimeout: 1800);
                    c.Execute($"delete from CustomerProperty where name in ({names}) and CustomerId in @customerIds", new { customerIds = request.CustomerIds }, transaction: tr, commandTimeout: 1800);
                    tr.Commit();
                }
                catch
                {
                    tr.Rollback();
                    throw;
                }
            }

            return r;
        }

        public class Request
        {
            [Required]
            public List<int> CustomerIds { get; set; }
        }

        public class Response
        {
        }
    }
}