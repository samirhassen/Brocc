using Microsoft.Owin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace NTech.Services.Infrastructure
{
    public class NTechVerboseRequestLogMiddleware : OwinMiddleware
    {
        private DirectoryInfo logFolder;
        private string serviceName;

        public NTechVerboseRequestLogMiddleware(OwinMiddleware next, DirectoryInfo logFolder, string serviceName) : base(next)
        {
            this.logFolder = logFolder;
            this.serviceName = serviceName;
        }

        private void CopyStreamToStream(Stream s1, Stream s2)
        {
            var pos = s1.Position;
            try
            {
                s1.Position = 0;
                s1.CopyTo(s2);
            }
            finally
            {
                s1.Position = pos;
            }
        }

        public override async Task Invoke(IOwinContext context)
        {
            var request = context?.Request;
            if (request == null || request.ContentType == null)
            {
                await Next.Invoke(context);
                return;
            }
            if (!request.ContentType.Contains("application/json"))
            {
                await Next.Invoke(context);
                return;
            }
            if (request.Method != "POST")
            {
                await Next.Invoke(context);
                return;
            }

            var httpResponse = System.Web.HttpContext.Current.Response;
            var outputCapture = new OutputCaptureStream(httpResponse.Filter);
            httpResponse.Filter = outputCapture;

            IOwinResponse owinResponse = context.Response;
            //buffer the response stream in order to intercept downstream writes
            var owinResponseStream = owinResponse.Body;
            var owinBody = new MemoryStream();
            owinResponse.Body = owinBody;

            var timer = System.Diagnostics.Stopwatch.StartNew();
            await Next.Invoke(context);
            timer.Stop();

            if (outputCapture.CapturedData.Length == 0)
            {
                //response is formed by OWIN
                //make sure the response we buffered is flushed to the client
                owinResponse.Body.Position = 0;
                await owinResponse.Body.CopyToAsync(owinResponseStream);
            }
            else
            {
                //response by MVC
                //write captured data to response body as if it was written by OWIN         
                outputCapture.CapturedData.Position = 0;
                outputCapture.CapturedData.CopyTo(owinResponse.Body);
            }

            //-------------------------------------------------------
            //-----------------------Log-----------------------------
            //-------------------------------------------------------
            var requestUri = request?.Uri;
            var userId = ((context?.Authentication?.User?.Identity) as ClaimsIdentity)?.FindFirst("ntech.userid")?.Value;
            var remoteIp = context?.Request?.RemoteIpAddress;

            var tag = Guid.NewGuid().ToString();
            var props = new List<string>();
            var rootFolder = logFolder.FullName;
            if (serviceName != null)
            {
                rootFolder = Path.Combine(rootFolder, serviceName);
            }
            Directory.CreateDirectory(rootFolder);

            using (var fs = System.IO.File.CreateText(Path.Combine(rootFolder, $"{tag}-request.txt")))
            {
                using (var ms = new MemoryStream())
                {
                    CopyStreamToStream(request.Body, ms);

                    if (requestUri != null)
                        props.Add($"RequestUri={requestUri.ToString()}");
                    if (userId != null)
                        props.Add($"UserId={userId}");
                    if (remoteIp != null)
                        props.Add($"RemoteIp={remoteIp}");
                    props.Add($"RequestLength={ms.Length}");
                    props.Add($"TimeInMs={timer.ElapsedMilliseconds}");
                    props.Add($"ResponseHttpStatusCode={owinResponse.StatusCode}");

                    fs.WriteLine(string.Join(Environment.NewLine, props));
                    fs.WriteLine("--------------Request---------------");
                    fs.WriteLine(Encoding.UTF8.GetString(ms.ToArray()));

                    if (owinBody.Length > 0 && (owinResponse.ContentType?.Contains("application/json") ?? false))
                    {
                        fs.WriteLine("--------------Response--------------");
                        //Format the response for readability if possible since this is mostly used for debugging
                        try
                        {
                            fs.WriteLine(JsonConvert.SerializeObject(JsonConvert.DeserializeObject(Encoding.UTF8.GetString(owinBody.ToArray())), Formatting.Indented));
                        }
                        catch
                        {
                            fs.WriteLine(Encoding.UTF8.GetString(owinBody.ToArray()));
                        }
                    }
                }
            }
        }

        internal class OutputCaptureStream : Stream
        {
            private Stream InnerStream;
            public MemoryStream CapturedData { get; private set; }

            public OutputCaptureStream(Stream inner)
            {
                InnerStream = inner;
                CapturedData = new MemoryStream();
            }

            public override bool CanRead
            {
                get { return InnerStream.CanRead; }
            }

            public override bool CanSeek
            {
                get { return InnerStream.CanSeek; }
            }

            public override bool CanWrite
            {
                get { return InnerStream.CanWrite; }
            }

            public override void Flush()
            {
                InnerStream.Flush();
            }

            public override long Length
            {
                get { return InnerStream.Length; }
            }

            public override long Position
            {
                get { return InnerStream.Position; }
                set { CapturedData.Position = InnerStream.Position = value; }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return InnerStream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                CapturedData.Seek(offset, origin);
                return InnerStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                CapturedData.SetLength(value);
                InnerStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                CapturedData.Write(buffer, offset, count);
                InnerStream.Write(buffer, offset, count);
            }
        }
    }
}
