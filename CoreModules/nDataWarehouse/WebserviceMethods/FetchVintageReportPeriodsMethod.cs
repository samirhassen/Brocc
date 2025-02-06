using nDataWarehouse.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;

namespace nCredit.WebserviceMethods
{
    public class FetchVintageReportPeriodsMethod : TypedWebserviceMethod<FetchVintageReportPeriodsMethod.Request, FetchVintageReportPeriodsMethod.Response>
    {
        public override string Path => "Reports/Vintage/FetchPeriods";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var s = new VintageReportService(() => DateTimeOffset.Now);
            List<DateTime> vintageMonths = null;

            if (request.IncludeMonths.GetValueOrDefault())
                vintageMonths = s.FetchVintageMonths();

            return new Response
            {
                VintageMonths = vintageMonths
            };
        }


        public class Request
        {
            public bool? IncludeMonths { get; set; }
        }

        public class Response
        {
            public List<DateTime> VintageMonths { get; set; }
        }
    }
}