using nPreCredit.Code.Services;
using NTech;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class FetchCommonReportParametersMethod : TypedWebserviceMethod<FetchCommonReportParametersMethod.Request, FetchCommonReportParametersMethod.Response>
    {
        public override string Path => "Reports/Fetch-Common-Parameters";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();
            var response = new Response();

            //Application months
            using (var context = resolver.Resolve<PreCreditContextFactoryService>().Create())
            {
                var minDate = context.CreditApplicationHeaders.Min(x => (DateTimeOffset?)x.ApplicationDate);
                var maxDate = context.CreditApplicationHeaders.Max(x => (DateTimeOffset?)x.ApplicationDate);
                if (minDate.HasValue && maxDate.HasValue)
                {
                    response.ApplicationMonths = new List<DateTime>();
                    var month = new DateTime(minDate.Value.Year, minDate.Value.Month, 1);
                    var toMonth = new DateTime(maxDate.Value.Year, maxDate.Value.Month, 1);
                    if (month.Year < 1950 || toMonth.Year > 2100)
                        throw new Exception("Invalid application dates encountered. Testdata that is bad?");
                    while (month <= toMonth)
                    {
                        response.ApplicationMonths.Add(month);
                        month = month.AddMonths(1);
                    }
                }
                else
                {
                    //Fallback if db is empty is to just include this month. This since the typical usecase is to allow reports to be generated for
                    //these months and we want an empty report to be creatable when the system is just setup and has no applications.
                    var today = resolver.Resolve<IClock>().Today;
                    response.ApplicationMonths = new List<DateTime> { new DateTime(today.Year, today.Month, 1) };
                }
            }

            //Providers
            response.ProviderNames = NEnv.GetAffiliateModels().Select(x => x.DisplayToEnduserName).OrderBy(x => x).ToList();

            return response;
        }

        public class Request
        {

        }

        public class Response
        {
            public List<string> ProviderNames { get; set; }
            public List<DateTime> ApplicationMonths { get; set; }
        }
    }
}