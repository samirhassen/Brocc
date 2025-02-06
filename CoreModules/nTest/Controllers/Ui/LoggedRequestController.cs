using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using nTest.Code;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using System.Xml.Linq;

namespace nTest.Controllers
{
    public class LoggedRequestController : NController
    {
        [Route("Ui/LoggedRequest")]
        public ActionResult Index()
        {
            StringBuilder b = new StringBuilder();

            b.Append(@"<!DOCTYPE html><html><head><title>Logged requests</title><meta charset=""utf-8"" /></head>");
            foreach (var r in LoggedRequests.ToList().OrderByDescending(x => x.RequestDate))
            {
                b.Append($"<div style=\"margin-top:20px;border: dashed 2px black;padding:5px\"><h2>{r.Url}</h2>");
                b.Append("<table>");
                b.Append($"<tr><td>Request time</td><td>{r.RequestDate.ToString("yyyy-MM-dd HH:mm:ss")}</td></tr>");
                foreach (var h in r.Headers)
                {
                    b.Append($"<tr><td>{WebUtility.HtmlEncode(h.Key)}</td><td>{WebUtility.HtmlEncode(h.Value)}</td></tr>");
                }
                b.Append("</table>");
                if (r.RequestJsonBody != null)
                {
                    b.Append($"<h3>Json body</h3><pre>{WebUtility.HtmlEncode(r.RequestJsonBody)}</pre>");
                }
                else if (r.RequestHtmlBody != null)
                {
                    b.Append($"<h3>Html body</h3><pre>{WebUtility.HtmlEncode(r.RequestHtmlBody)}</pre>");
                }
                else if (r.RequestXmlBody != null)
                {
                    b.Append($"<h3>Xml body</h3><pre>{WebUtility.HtmlEncode(r.RequestXmlBody)}</pre>");
                }
                else
                {
                    b.Append($"<h3>No body</h3>");
                }
                b.Append("</div>");
            }
            b.Append(@"</html>");
            return Content(b.ToString(), "text/html", Encoding.UTF8);
        }

        [AllowAnonymous]
        public ActionResult ReceiveRequest(string subRoute)
        {
            //NOTE: Handles Api/LoggedRequest/{*subRoute}

            bool? isErrorTest = null;
            LoggedRequest r = null;
            try
            {
                if (Request.HttpMethod == "POST" || Request.HttpMethod == "PUT" || Request.HttpMethod == "GET")
                {
                    r = new LoggedRequest();
                    r.RequestDate = DateTimeOffset.Now;

                    var headers = new Dictionary<string, string>();
                    foreach (var h in Request.Headers.AllKeys)
                    {
                        headers[h] = Request.Headers[h];
                    }

                    r.IsCm1TestRequest = headers.ContainsKey("X-NTech-IsCm1Screening");

                    r.Headers = headers;
                    if (!string.IsNullOrWhiteSpace(subRoute))
                    {
                        r.Url = subRoute + Request.Url.Query;
                    }
                    else
                    {
                        r.Url = Request.RawUrl;
                    }
                    string providerName = null;
                    if (headers.ContainsKey("X-Ntech-ProviderName"))
                        providerName = headers["X-Ntech-ProviderName"];

                    if (headers.ContainsKey("X-Ntech-ErrorTest"))
                    {
                        isErrorTest = true;
                    }

                    if (Request.ContentType?.ToLowerInvariant()?.Contains("json") ?? false)
                    {
                        Request.InputStream.Position = 0;
                        using (var rr = new StreamReader(Request.InputStream, Request.ContentEncoding))
                        {
                            r.RequestJsonBody = JsonPrettify(rr.ReadToEnd());
                        }
                    }
                    else if (Request.ContentType?.ToLowerInvariant().Contains("html") ?? false)
                    {
                        Request.InputStream.Position = 0;
                        using (var rr = new StreamReader(Request.InputStream, Request.ContentEncoding))
                        {
                            r.RequestHtmlBody = rr.ReadToEnd();
                        }
                    }
                    else if (Request.ContentType?.ToLowerInvariant()?.Contains("xml") ?? false)
                    {
                        Request.InputStream.Position = 0;
                        using (var rr = new StreamReader(Request.InputStream, Request.ContentEncoding))
                        {
                            r.RequestXmlBody = XmlPrettify(rr.ReadToEnd());
                        }
                    }
                    else if (Request.ContentType?.ToLowerInvariant()?.Contains("application/jwt") ?? false)
                    {
                        Request.InputStream.Position = 0;
                        using (var rr = new StreamReader(Request.InputStream, Request.ContentEncoding))
                        {
                            r.RequestHtmlBody = rr.ReadToEnd();
                        }
                    }
                    LoggedRequests.Add(r);
                }
            }
            catch
            {
                /* Intentionally ignored */
            }

            var s = (subRoute ?? "").ToLowerInvariant();
            if (r != null && r.IsCm1TestRequest)
                return Cm1KycScreeningTestEndpoint.Handle(r);
            else if (s.Contains("vertaaensin"))
            {
                return Json2(new
                {
                    status = "success",
                    message = "running against the test module"
                });
            }
            else if (s.Contains("eone"))
            {
                return Json2(new
                {
                    Status = "OK"
                });
            }
            else if (s.Contains("lendo"))
            {
                return Json2(new
                {
                    result = new
                    {
                        status = "OK"
                    }
                });
            }
            else if (s.Contains("finnishcustomsaccounts"))
            {
                Response.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());
                return new JsonNetActionResult
                {
                    Data = isErrorTest.GetValueOrDefault() ? new
                    {
                        errorMessage = "Test error from provider " + DateTime.Now
                    } : (object)new
                    {
                    },
                    CustomHeaders = new Dictionary<string, string>
                    {
                        { "X-Correlation-ID", Guid.NewGuid().ToString() }
                    },
                    CustomHttpStatusCode = isErrorTest.GetValueOrDefault() ? 500 : 200
                };
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }

        public class LoggedRequest
        {
            public bool IsCm1TestRequest { get; set; }
            public string RequestJsonBody { get; set; }
            public string RequestXmlBody { get; set; }
            public string RequestHtmlBody { get; set; }
            public Dictionary<string, string> Headers { get; set; }
            public DateTimeOffset RequestDate { get; set; }
            public string Url { get; set; }
        }

        private static RingBuffer<LoggedRequest> LoggedRequests = new RingBuffer<LoggedRequest>(100);

        private static string JsonPrettify(string json)
        {
            using (var stringReader = new StringReader(json))
            using (var stringWriter = new StringWriter())
            {
                var jsonReader = new JsonTextReader(stringReader);
                var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
                jsonWriter.WriteToken(jsonReader);
                return stringWriter.ToString();
            }
        }

        private static string XmlPrettify(string xml)
        {
            try
            {
                return XDocuments.Parse(xml).ToString(SaveOptions.None);
            }
            catch
            {
                //Ignored intentionally
                return xml;
            }
        }
    }
}