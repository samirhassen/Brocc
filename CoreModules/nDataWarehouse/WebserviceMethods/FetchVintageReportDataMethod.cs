using nDataWarehouse.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;

namespace nCredit.WebserviceMethods
{
    public class FetchVintageReportDataMethod : TypedWebserviceMethod<VintageReportRequest, VintageReportResult>
    {
        public override string Path => "Reports/Vintage/FetchData";

        protected override VintageReportResult DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, VintageReportRequest request)
        {
            Validate(request, x =>
            {

            });

            var s = new VintageReportService(() => DateTimeOffset.Now);

            return s.FetchVintageReportData(request);
        }
    }
}