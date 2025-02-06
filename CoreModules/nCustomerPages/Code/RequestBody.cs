using Newtonsoft.Json;
using NTech.Legacy.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace nCustomerPages.Code
{
    public class RequestBody
    {
        private Stream inputStream;
        private Encoding encoding;
        private Dictionary<string, string> headers;

        public static RequestBody CreateFromFromRequest(System.Web.HttpRequestBase request, string correlationId = null)
        {
            var headers = request.Headers.AllKeys.ToDictionary(x => x, x => request.Headers[x]);
            var ms = new MemoryStream();
            if (request.InputStream.Position != 0)
                request.InputStream.Position = 0;
            request.InputStream.CopyTo(ms);
            ms.Position = 0;
            return new RequestBody(ms, headers, request.RawUrl, correlationId: correlationId);
        }

        public RequestBody(Stream inputStream, Dictionary<string, string> headers, string rawUrl, string correlationId = null)
        {
            this.inputStream = inputStream;
            this.headers = headers;
            this.RawUrl = rawUrl;
            this.CorrelationId = correlationId;

            //TODO: Infer from headers
            this.encoding = Encoding.UTF8;
        }

        public string CorrelationId { get; }
        public string RawUrl { get; }

        public IDictionary<string, string> Headers
        {
            get
            {
                return this.headers;
            }
        }

        public AuthorizationHeader GetAuthorizationHeader()
        {
            if (AuthorizationHeader.TryParseHeader(Headers?.Opt("Authorization"), out var header))
                return header;
            return null;
        }

        public void SaveAs(string filename)
        {
            using (var fs = new FileStream(filename, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = AsByteArray();
                fs.Write(bytes, 0, bytes.Length);
            }
        }

        public byte[] AsByteArray()
        {
            var preambleStr = $"Request{Environment.NewLine}RawUrl: {this.RawUrl}{Environment.NewLine}";
            if (this.CorrelationId != null)
                preambleStr += $"CorrelationId: {this.CorrelationId}{Environment.NewLine}";
            var preamble = encoding.GetBytes(preambleStr);

            var headers = encoding.GetBytes(
                string.Join(Environment.NewLine, this.Headers.Select(x => $"{x.Key}={x.Value}"))
                +
                Environment.NewLine
                );
            using (var ms = new MemoryStream())
            {
                ms.Write(preamble, 0, preamble.Length);
                ms.Write(headers, 0, headers.Length);
                var posBefore = inputStream.Position;
                inputStream.Position = 0;
                inputStream.CopyTo(ms);

                inputStream.Position = posBefore;
                return ms.ToArray();
            }
        }

        public string AsString()
        {
            return encoding.GetString(AsByteArray());
        }

        public U ParseAs<U>()
        {

            inputStream.Position = 0;
            try
            {
                using (var r = new StreamReader(inputStream, encoding))
                {
                    var bodyText = r.ReadToEnd();
                    return JsonConvert.DeserializeObject<U>(bodyText);
                }
            }
            catch (Exception ex)
            {
                throw new RequestBodyParseException("Failed to parse request body as json", ex);
            }
        }

        public U ParseAsAnonymousType<U>(U templateObject)
        {
            inputStream.Position = 0;
            try
            {
                using (var r = new StreamReader(inputStream, encoding))
                {
                    return JsonConvert.DeserializeAnonymousType<U>(r.ReadToEnd(), templateObject);
                }
            }
            catch (Exception ex)
            {
                throw new RequestBodyParseException("Failed to parse request body as json", ex);
            }
        }
    }

    public class RequestBodyParseException : Exception
    {
        public RequestBodyParseException() : base()
        {
        }

        public RequestBodyParseException(string message) : base(message)
        {
        }

        public RequestBodyParseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RequestBodyParseException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}
