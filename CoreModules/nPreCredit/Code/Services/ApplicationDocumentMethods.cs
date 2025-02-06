using Newtonsoft.Json;
using NTech.Services.Infrastructure.NTechWs;

namespace nPreCredit.Code.Services
{
    public class ApplicationDocumentAddMethod : TypedWebserviceMethod<ApplicationDocumentAddMethod.Request, ApplicationDocumentModel>
    {
        public override string Path => "ApplicationDocuments/Add";

        protected override ApplicationDocumentModel DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            SetCustomRequestLogJson(
                requestContext,
                () => JsonConvert.SerializeObject(new
                {
                    request?.ApplicationNr,
                    request?.DocumentType,
                    request?.CustomerId,
                    request?.DocumentSubType,
                    request?.ApplicantNr,
                    DataUrl = "<Removed>",
                    request?.Filename
                }));

            ApplicationDocumentModel addedDocument;
            string failedMessage;
            if (requestContext.Resolver().Resolve<IApplicationDocumentService>().TryAddDocument(request?.ApplicationNr, request?.DocumentType, request?.ApplicantNr, request?.CustomerId, request?.DocumentSubType, request?.DataUrl, request?.Filename, out addedDocument, out failedMessage))
                return addedDocument;
            else
                return Error(failedMessage);
        }

        public class Request
        {
            public string ApplicationNr { get; set; }
            public string DocumentType { get; set; }
            public int? ApplicantNr { get; set; }
            public int? CustomerId { get; set; }
            public string DocumentSubType { get; set; }
            public string DataUrl { get; set; }
            public string Filename { get; set; }
        }
    }
}