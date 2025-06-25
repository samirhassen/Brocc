using System;
using System.Collections.Generic;
using nSavings.Code.Services;
using nSavings.Controllers.Api;
using NTech.Services.Infrastructure.NTechWs;

namespace nSavings.WebserviceMethods
{
    public class FetchFatcaExportFilesMethod : TypedWebserviceMethod<FetchFatcaExportFilesMethod.Request,
        FetchFatcaExportFilesMethod.Response>
    {
        public override string Path => "Fatca/FetchExportFiles";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var pageSizeAndNr = (request.PageNr.HasValue && request.PageNr.HasValue)
                ? Tuple.Create(request.PageSize.Value, request.PageNr.Value)
                : null;
            var result = requestContext.Service().FatcaExport.GetFatcaExportFiles(pageSizeAndNr: pageSizeAndNr);

            return new Response
            {
                Files = result
            };
        }

        public class Request
        {
            public int? PageSize { get; set; }
            public int? PageNr { get; set; }
        }

        public class Response
        {
            public List<FatcaExportFileModel> Files { get; set; }
        }
    }
}