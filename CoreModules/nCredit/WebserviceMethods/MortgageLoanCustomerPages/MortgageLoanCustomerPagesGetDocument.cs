using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace nCredit.WebserviceMethods.MortgageLoanCustomerPages
{
    public class MortgageLoanCustomerPagesGetDocument : FileStreamWebserviceMethod<MortgageLoanCustomerPagesGetDocument.Request>
    {
        public override bool IsEnabled => NEnv.IsMortgageLoansEnabled;
        public override string Path => "MortageLoan/CustomerPages/document";

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var d = Controllers.ApiCustomerPagesController
                .GetCreditDocumentContentTypeNameAndData(request.CustomerId.Value, request.DocumentType, request.DocumentId, requestContext.Service().DocumentClientHttpContext);
            if (d == null)
                return Error("Not such document", httpStatusCode: 404, errorCode: "noSuchDocument");
            else
                return this.File(new MemoryStream(d.Item3), downloadFileName: d.Item2, contentType: d.Item1);
        }

        public class Request
        {
            [Required]
            public int? CustomerId { get; set; }

            [Required]
            public string DocumentType { get; set; }

            [Required]
            public string DocumentId { get; set; }
        }
    }
}