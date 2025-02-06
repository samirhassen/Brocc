using System;
using System.Linq;

namespace NTech.Services.Infrastructure.NTechWs
{
    public abstract class FileStreamWebserviceMethod<TRequest> : NTechWebserviceMethod
        where TRequest : class, new()
    {
        public FileStreamWebserviceMethod(bool usePost = false, bool allowDirectFormPost = false)
        {
            this.usePost = usePost;
            this.allowDirectFormPost = allowDirectFormPost;
        }

        public override string HttpVerb => !usePost ? "GET" : "POST";

        private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string PdfContentType = "application/pdf";
        private readonly bool usePost;
        private readonly bool allowDirectFormPost;

        public override Type RequestType => typeof(TRequest);

        public override Type ResponseType => typeof(NTechWsDoc.ServiceMethodDocumentationGenerator.FileStreamResponseMarkerType);

        private bool IsFormRequest(System.Web.HttpRequestBase r)
        {
            return r.ContentType?.Contains("application/x-www-form-urlencoded") ?? false;
        }

        protected override ActionResult DoExecute(NTechWebserviceMethodRequestContext requestContext)
        {
            var request = !usePost
                ? requestContext.ParseQueryStringRequest<TRequest>(x => SetCustomRequestLogJson(requestContext, () => x))
                : (allowDirectFormPost && IsFormRequest(requestContext.HttpRequest)
                    ? requestContext.ParseFormContent<TRequest>(x => SetCustomRequestLogJson(requestContext, () => x))
                    : requestContext.ParseJsonRequest<TRequest>());

            var result = DoExecuteFileStream(requestContext, request);

            return new ActionResult
            {
                FileStreamResult = result
            };
        }

        protected T[] SkipNulls<T>(params T[] items) where T : class
        {
            return items.Where(x => x != null).ToArray();
        }

        protected abstract ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, TRequest request);

        protected ActionResult.FileStream File(System.IO.Stream stream, string downloadFileName = null, string contentType = "application/octet-stream")
        {
            return new ActionResult.FileStream
            {
                ContentType = contentType,
                DownloadFileName = downloadFileName,
                Stream = stream
            };
        }

        protected ActionResult.FileStream ExcelFile(System.IO.Stream stream, string downloadFileName = null)
        {
            return File(stream, downloadFileName: downloadFileName, contentType: XlsxContentType);
        }

        protected ActionResult.FileStream PdfFile(System.IO.Stream stream, string downloadFileName = null)
        {
            return File(stream, downloadFileName: downloadFileName, contentType: PdfContentType);
        }

        protected ActionResult.FileStream Error(string errorMessage, int? httpStatusCode = null, string errorCode = null)
        {
            throw new NTechWebserviceMethodException(errorMessage)
            {
                ErrorCode = errorCode,
                ErrorHttpStatusCode = httpStatusCode,
                IsUserFacing = true
            };
        }

        public class FileStreamResponseMarkerType
        {

        }
    }
}
