using NTech.Services.Infrastructure.NTechWs;
using System;

namespace nSavings.WebserviceMethods
{
    public class CreateFatcaExportFileMethod : TypedWebserviceMethod<CreateFatcaExportFileMethod.Request, CreateFatcaExportFileMethod.Response>
    {
        public override string Path => "Fatca/CreateExportFile";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, r =>
            {
                r.Require(x => x.Year);
            });

            var date = new DateTime(request.Year.Value, 12, 31);

            OutgoingExportFileHeader.StandardExportResultStatusModel exportResult = null;
            var result = requestContext.Service().FatcaExport.CreateAndStoreAndExportFatcaExportFile(date,
                request.ExportProfile, requestContext.CurrentUserId(), requestContext.InformationMetadata(), observeExportResult: x => exportResult = x);

            return new Response
            {
                FileArchiveKey = result.FileArchiveKey,
                ExportResult = exportResult
            };
        }


        public class Request
        {
            public int? Year { get; set; }
            public string ExportProfile { get; set; }
        }

        public class Response
        {
            public string FileArchiveKey { get; set; }
            public OutgoingExportFileHeader.StandardExportResultStatusModel ExportResult { get; set; }
        }
    }
}