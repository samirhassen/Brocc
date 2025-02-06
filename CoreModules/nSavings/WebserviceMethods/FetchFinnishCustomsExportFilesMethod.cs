using nSavings.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;

namespace nSavings.WebserviceMethods
{
    public class FetchFinnishCustomsExportFilesMethod : TypedWebserviceMethod<FetchFinnishCustomsExportFilesMethod.Request, FetchFinnishCustomsExportFilesMethod.Response>
    {
        public override string Path => "FinnishCustomsAccounts/FetchExportFiles";

        public override bool IsEnabled => NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.savingsCustomsAccountsExport.v1");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var result = requestContext.Service().FinnishCustomsAccounts(requestContext.CurrentUserMetadataCore()).GetExportsPageAndNrOfPages(request.PageSize ?? 50, request.PageNr ?? 0);

            return new Response
            {
                PageExports = result.Item1,
                TotalPageCount = result.Item2
            };
        }

        public class Request
        {
            public int? PageSize { get; set; }
            public int? PageNr { get; set; }
        }

        public class Response
        {
            public List<FinnishCustomsAccountsExportModel> PageExports { get; set; }
            public int TotalPageCount { get; set; }
        }
    }
}