using ICSharpCode.SharpZipLib.Zip;
using nDocument.Code;
using nDocument.Code.Pdf;
using nDocument.Pdf;
using Newtonsoft.Json.Linq;
using NTech.Core.Module.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Mvc;

namespace nDocument.Controllers
{
    public class PdfController : Controller
    {
        private static string GetTemplatePathFromBatchId(string batchId)
        {
            return Path.Combine(Path.GetTempPath(), batchId);
        }

        private static void Log(string context, Func<string> produceLogData)
        {
            var logFolder = NEnv.DocumentCreationRequestLogFolder;
            if (logFolder == null)
                return;

            try
            {
                logFolder.Create();
                var fn = Path.Combine(logFolder.FullName, $"request-{context}-{Guid.NewGuid().ToString()}.txt");
                System.IO.File.WriteAllText(fn, produceLogData());
            }
            catch
            {
                /* Ignored */
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult BatchRenderBegin(string template)
        {
            var batchId = Guid.NewGuid().ToString();

            var templatePath = GetTemplatePathFromBatchId(batchId);
            Directory.CreateDirectory(templatePath);

            var zipBytes = Convert.FromBase64String(template);
            var fs = new FastZip();
            using (var ms = new MemoryStream(zipBytes))
            {
                fs.ExtractZip(ms, templatePath, FastZip.Overwrite.Never, null, null, null, false, true);
            }

            return Json(new { batchId = batchId });
        }

        #region "Delayed"
        [AllowAnonymous]
        [HttpPost]
        public ActionResult BatchRenderDelayedBegin(string template)
        {
            string batchId;
            var zipBytes = Convert.FromBase64String(template);
            using (var ms = new MemoryStream(zipBytes))
            {
                var ap = Code.Archive.ArchiveProviderFactory.Create();
                string errorMessage;
                if (!ap.TryStore(ms.ToArray(), DelayedDocumentHandler.PdfTemplateMimeType, $"template-{DateTime.Now.ToString("yyyy-MM-dd_HHmm")}.zip", out batchId, out errorMessage))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, errorMessage);
                }
                return Json(new { batchId = batchId });
            }
        }

        [HttpPost]
        [NTechAuthorize]
        public ActionResult BatchRenderDelayedDocumentToArchive(string batchId, string filename, string context)
        {
            string key;
            string errorMessage;
            var fileBytes = Encoding.UTF8.GetBytes(context);
            var ad = new Code.Archive.ArchiveOptionalData
            {
                DelayedDocumentTemplateArchiveKey = batchId,
                DelayedDocumentType = DelayedDocumentHandler.DelayedPdfType
            };
            var p = Code.Archive.ArchiveProviderFactory.Create();
            if (!p.TryStore(fileBytes, DelayedDocumentHandler.PdfContextMimeType, filename, out key, out errorMessage, optionalData: ad))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, errorMessage);
            }
            else
            {
                return Json(new { key = key });
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult BatchRenderDelayedEnd(string batchId)
        {
            if (string.IsNullOrWhiteSpace(batchId))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing batchId");

            return new EmptyResult();
        }
        #endregion

        [AllowAnonymous]
        [HttpPost]
        public ActionResult BatchRenderDocument(string batchId, string context)
        {
            string errorMessage;
            byte[] fileBytes;
            if (!TryBatchRenderDocument(batchId, context, out fileBytes, out errorMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, errorMessage);
            }
            else
            {
                return File(fileBytes, "application/pdf");
            }
        }

        private static IStaticHtmlToPdfConverter CreateHtmlToPdfConverter()
        {
            var p = NEnv.StaticHtmlToPdfProviderName?.ToLowerInvariant();
            if (p == "princexml")
            {
                var lic = NEnv.PrinceLicense;
                return new PrinceXmlStaticHtmlToPdfConverter(NEnv.PrinceXmlExePath, licenseKey: lic.Key, licenseFilePath: lic.FileName);
            }
            else if (p == "chromeheadless")
            {
                return new ChromeHeadlessStaticHtmlToPdfConverter(NEnv.StaticHtmlToPdfServiceUrl);
            }
            else if (p == "weasyprint")
            {
                return new WeasyPrintStaticHtmlToPdfConverter(NEnv.WeasyPrintExePath);
            }
            else
                throw new NotImplementedException();
        }

        private static bool TryBatchRenderDocument(string batchId, string context, out byte[] result, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(batchId))
            {
                errorMessage = "Missing batchId";
                result = null;
                return false;
            }

            if (string.IsNullOrWhiteSpace(context))
            {
                errorMessage = "Missing context";
                result = null;
                return false;
            }

            var templatePath = GetTemplatePathFromBatchId(batchId);

            if (!System.IO.Directory.Exists(templatePath))
            {
                errorMessage = "No such batch exists";
                result = null;
                return false;
            }

            Log("TryBatchRenderDocument", () =>
            {
                return $"BatchId={batchId}{Environment.NewLine}{context}";
            });

            JObject jsonObj = JObject.Parse(context);
            var parsedContext = jsonObj.ToObject<Dictionary<string, object>>();

            var targetFileName = Path.Combine(Path.GetTempPath(), "nDocument-Pdf-" + Guid.NewGuid() + ".pdf");
            try
            {
                var compiler = new Pdf.NTechHtmlToPdfTemplateCompiler(CreateHtmlToPdfConverter(), CreateHtmlTemplateLogger(), GetCommonContext);
                using (var t = compiler.CompileFromExistingFolder(templatePath, batchId, SkinningPath))
                {
                    t.RenderToFile(parsedContext, targetFileName);
                };

                var fileBytes = System.IO.File.ReadAllBytes(targetFileName);

                result = fileBytes;
                errorMessage = null;
                return true;
            }
            finally
            {
                try
                {
                    if (System.IO.File.Exists(targetFileName))
                        System.IO.File.Delete(targetFileName);
                }
                catch
                {
                    /*Ignored*/
                }
            }
        }

        //NOTE: Don't put AllowAnonymous here. The others just produce files which is fine to do anonyomously but this actually writes to permanent storage
        /// <summary>
        /// 
        /// </summary>
        /// <param name="batchId"></param>
        /// <param name="filename"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        [HttpPost]
        [NTechAuthorize]
        public ActionResult BatchRenderDocumentToArchive(string batchId, string filename, string context)
        {
            string errorMessage;
            byte[] fileBytes;
            if (!TryBatchRenderDocument(batchId, context, out fileBytes, out errorMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, errorMessage);
            }
            else
            {
                string key;
                string errorMessage2;
                var p = Code.Archive.ArchiveProviderFactory.Create();
                if (!p.TryStore(fileBytes, "application/pdf", filename, out key, out errorMessage2))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, errorMessage2);
                }
                else
                {
                    return Json(new { key = key });
                }
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult BatchRenderEnd(string batchId)
        {
            if (string.IsNullOrWhiteSpace(batchId))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing batchId");

            var templatePath = GetTemplatePathFromBatchId(batchId);

            if (!System.IO.Directory.Exists(templatePath))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such batch exists");

            try
            {
                System.IO.Directory.Delete(templatePath, true);
            }
            catch (Exception ex)
            {
                NLog.Warning("({Operation}) Failed to delete: {templatePath}. Type: {exceptionType}", "BatchRenderEnd", templatePath, ex?.GetType()?.Name);
            }

            return new EmptyResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template">Template zip file as base64 encoded byte array</param>
        /// <param name="context">Context converted to json</string></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        public ActionResult RenderDirect(string template, string context)
        {
            var fileBytes = RenderDirectShared(() => Convert.FromBase64String(template), context);
            return File(fileBytes, "application/pdf");
        }

        public static byte[] RenderDirectShared(Func<byte[]> getTemplateBytes, string context)
        {
            JObject jsonObj = JObject.Parse(context);
            var parsedContext = jsonObj.ToObject<Dictionary<string, object>>();
            var templateBytes = getTemplateBytes();

            Log("RenderDirect", () =>
            {
                return context;
            });

            var targetFileName = Path.Combine(Path.GetTempPath(), "nDocument-Pdf-" + Guid.NewGuid() + ".pdf");
            try
            {
                var compiler = new Pdf.NTechHtmlToPdfTemplateCompiler(CreateHtmlToPdfConverter(), CreateHtmlTemplateLogger(), GetCommonContext);
                using (var t = compiler.CompileFromZipfile(templateBytes, SkinningPath))
                {
                    t.RenderToFile(parsedContext, targetFileName);
                };

                return System.IO.File.ReadAllBytes(targetFileName);
            }
            finally
            {
                try
                {
                    if (System.IO.File.Exists(targetFileName))
                        System.IO.File.Delete(targetFileName);
                }
                catch
                {
                    /*Ignored*/
                }
            }
        }

        private static IHtmlTemplateLogger CreateHtmlTemplateLogger()
        {
            if (NEnv.IsHtmlTemplateLoggingEnabled)
                return new FileSystemHtmlTemplateLogger();
            else
                return new DoNothingHtmlTemplateLogger();
        }

        private static string SkinningPath
        {
            get
            {
                return NTechEnvironment.Instance.ClientResourceDirectory("ntech.document.pdfrender.skinningpath", "skinning", true).FullName;
            }
        }

        private static Dictionary<string, object> GetCommonContext()
        {
            var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
            var service = new DocumentClientDataService(customerClient, NEnv.ClientCfgCore, NEnv.SharedEnv);
            return service.GetCommonContext();
        }
    }
}