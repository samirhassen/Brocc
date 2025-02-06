using NTech.Services.Infrastructure.NTechWs;
using System;
using System.IO;

namespace nSavings.WebserviceMethods.Reports
{
    public class CreateFatcaReportMethod : FileStreamWebserviceMethod<CreateFatcaReportMethod.Request>
    {
        public override string Path => "Reports/GetFatcaExport";

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, r =>
            {
                r.Require(x => x.Date);
            });

            var ms = new MemoryStream();
            requestContext.Service().FatcaExport.CreateFatcaFileToStream(request.Date.Value, ms);
            ms.Position = 0;
            return File(ms, downloadFileName: $"Fatca-{request.Date.Value.Year}.xml", contentType: "application/xml");
        }

        public class Request
        {
            public DateTime? Date { get; set; }
        }
    }
}