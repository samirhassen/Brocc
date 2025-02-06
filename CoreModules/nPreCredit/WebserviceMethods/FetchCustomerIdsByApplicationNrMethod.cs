using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class FetchCustomerIdsByApplicationNrMethod : TypedWebserviceMethod<FetchCustomerIdsByApplicationNrMethod.Request, FetchCustomerIdsByApplicationNrMethod.Response>
    {
        public override string Path => "Reporting/Fetch-CustomerIds-By-ApplicationNrs";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var customers = new Dictionary<int, Response.CustomerModel>();

                if (request.ApplicationNrs.Count > 0)
                {
                    var queryable = GetQueryable(context, request.ApplicationNrs);

                    foreach (var i in queryable.ToList())
                    {
                        var customerId = int.Parse(i.CustomerId);
                        if (!customers.ContainsKey(customerId))
                            customers[customerId] = new Response.CustomerModel { CustomerId = customerId, ApplicationNrs = new List<string> { i.ApplicationNr } };
                        else
                            customers[customerId].ApplicationNrs.Add(i.ApplicationNr);
                    }
                }


                return new Response { Customers = customers.Values.ToList() };
            }
        }

        private IQueryable<Tmp> GetQueryable(PreCreditContextExtended context, List<string> applicationNrs)
        {
            if (NEnv.IsMortgageLoansEnabled || NEnv.IsUnsecuredLoansEnabled)
            {
                // Get query based on the product type. 
                return NEnv.IsStandardUnsecuredLoansEnabled
                    ? GetUnsecuredStandardCustomerQuery(context, applicationNrs)
                    : GetCustomerQuery(false, context, applicationNrs);
            }
            else // if (NEnv.IsCompanyLoansEnabled) basically
                return GetCustomerQuery(true, context, applicationNrs);
        }

        /// <summary>
        /// Returns a query to retrieve customerIds from CreditApplicationItem-table in the precredit-database. 
        /// </summary>
        /// <param name="isCompanyLoan"></param>
        /// <param name="context"></param>
        /// <param name="applicationNrs"></param>
        /// <returns></returns>
        private IQueryable<Tmp> GetCustomerQuery(bool isCompanyLoan, PreCreditContextExtended context, List<string> applicationNrs)
        {
            var baseQuery = context.CreditApplicationItems.Where(x => applicationNrs.Contains(x.ApplicationNr)).AsQueryable();

            if (isCompanyLoan)
                return baseQuery.Where(x => x.GroupName == "application" && x.Name == "companyCustomerId" && !x.IsEncrypted)
                        .Select(x => new Tmp { CustomerId = x.Value, ApplicationNr = x.ApplicationNr });
            else
                return baseQuery.Where(x => x.GroupName.StartsWith("applicant") && x.Name == "customerId")
                        .Select(x => new Tmp { CustomerId = x.Value, ApplicationNr = x.ApplicationNr });

        }

        /// <summary>
        /// Unsecured loans standard saves to table ComplexApplicationListItem as compared to CreditApplicationItem for other products. 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="applicationNrs"></param>
        /// <returns></returns>
        private IQueryable<Tmp> GetUnsecuredStandardCustomerQuery(PreCreditContextExtended context, List<string> applicationNrs)
        {
            return context.ComplexApplicationListItems.Where(x => applicationNrs.Contains(x.ApplicationNr))
                .Where(x => x.ListName == "Applicant" && x.ItemName == "customerId")
                .Select(x => new Tmp { CustomerId = x.ItemValue, ApplicationNr = x.ApplicationNr });
        }

        private class Tmp
        {
            public string CustomerId { get; set; }
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public List<CustomerModel> Customers { get; set; }

            public class CustomerModel
            {
                public List<string> ApplicationNrs { get; set; }
                public int CustomerId { get; set; }
            }
        }

        public class Request
        {
            [Required]
            public List<string> ApplicationNrs { get; set; }
        }
    }
}