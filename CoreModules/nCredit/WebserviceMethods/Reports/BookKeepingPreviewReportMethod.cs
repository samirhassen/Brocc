using nCredit.Code;
using nCredit.DbModel.BusinessEvents;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Linq;

namespace nCredit.WebserviceMethods.Reports
{
    public class BookKeepingPreviewReportMethod : FileStreamWebserviceMethod<BookKeepingPreviewReportMethod.Request>
    {
        public override string Path => "Reports/BookKeepingPreview";

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (!request.FromDate.HasValue && !request.ToDate.HasValue)
            {
                request.ToDate = requestContext.Clock().Today;
                request.FromDate = request.ToDate.Value.AddMonths(-1);
            }
            else if (!request.FromDate.HasValue)
                request.FromDate = request.ToDate.Value;
            else
                request.ToDate = request.FromDate.Value;

            var selfProviderNames = NEnv.GetAffiliateModels().Where(x => x.IsSelf).Select(x => x.ProviderName).ToHashSet();

            var resolver = requestContext.Service();
            var mgr = resolver.BookKeeping;
            using (var context = new CreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var formatCode = request.UseSieFormat == true
                    ? BookKeepingFileManager.BookKeepingPreviewFileFormatCode.Sie
                    : BookKeepingFileManager.BookKeepingPreviewFileFormatCode.Excel;
                var filePrefix = $"BookKeepingPreview-{request.FromDate.Value.ToString("yyyy-MM-dd")}-{request.ToDate.Value.ToString("yyyy-MM-dd")}";
                var fileData = mgr.CreatePreview(context, request.FromDate.Value.Date, request.ToDate.Value.Date, selfProviderNames, resolver.KeyValueStore,
                    resolver.DocumentClientHttpContext, formatCode, NEnv.BookKeepingAccountPlan);

                if (formatCode == BookKeepingFileManager.BookKeepingPreviewFileFormatCode.Excel)
                    return ExcelFile(fileData, downloadFileName: $"{filePrefix}.xlsx");
                else if (formatCode == BookKeepingFileManager.BookKeepingPreviewFileFormatCode.Sie)
                    return File(fileData, downloadFileName: $"{filePrefix}.sie");
                else
                    throw new NotImplementedException();
            }
        }

        public class Request
        {
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
            public bool? UseSieFormat { get; set; }
        }
    }
}