using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using nDocument.Code;
using nDocument.Code.Archive;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using Serilog;

namespace nDocument.Controllers
{
    [NTechAuthorize]
    public class ArchiveController : Controller
    {
        public class StoreRequest
        {
            public string MimeType { get; set; }
            public string FileName { get; set; }
            public string SourceType { get; set; }
            public string SourceId { get; set; }
            public string Base64EncodedFileData { get; set; }
        }

        private static ActionResult ServiceError(string errorMessage)
        {
            var errorCode = errorMessage.IsOneOf(HardenedArchiveProvider.FileTypeNotAllowedCode) ? errorMessage : null;
            return NTechWebserviceMethod.ToFrameworkErrorActionResult(
                NTechWebserviceMethod.CreateErrorResponse(errorMessage,
                    errorCode: errorCode));
        }

        [HttpPost]
        public ActionResult Store(StoreRequest request)
        {
            try
            {
                var fileBytes = Convert.FromBase64String(request.Base64EncodedFileData);

                var p = ArchiveProviderFactory.Create();

                ArchiveOptionalData optionalData = null;
                if (!string.IsNullOrWhiteSpace(request?.SourceType) || !string.IsNullOrWhiteSpace(request?.SourceId))
                {
                    optionalData = new ArchiveOptionalData
                    {
                        SourceId = request.SourceId?.NormalizeNullOrWhitespace(),
                        SourceType = request?.SourceType?.NormalizeNullOrWhitespace()
                    };
                }

                if (!p.TryStore(fileBytes, request.MimeType, request.FileName, out var key, out var errorMessage,
                        optionalData: optionalData))
                {
                    return ServiceError(errorMessage);
                }

                return Json(new { key });
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to store document");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        public ActionResult StoreFile(HttpPostedFileBase file)
        {
            try
            {
                var p = ArchiveProviderFactory.Create();

                if (!p.TryStore(file.InputStream, file.ContentType, file.FileName, out var key,
                        out var errorMessage))
                {
                    return ServiceError(errorMessage);
                }

                return Json(new { key });
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to store document");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        public ActionResult FetchMetadata(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return HttpNotFound();

            var p = ArchiveProviderFactory.Create();
            var result = p.FetchMetadata(key);

            if (result == null && ArchiveProviderFactory.IsBackupProviderSet)
            {
                var backupProvider = ArchiveProviderFactory.CreateBackup();
                result = backupProvider.FetchMetadata(key);
            }

            if (result == null)
                return HttpNotFound();

            return Json(new
            {
                result.ContentType,
                result.FileName,
                result.OptionalData
            });
        }

        [HttpPost]
        public ActionResult Delete(string key)
        {
            var wasDeleted = false;
            if (!string.IsNullOrWhiteSpace(key))
            {
                var p = ArchiveProviderFactory.Create();
                wasDeleted = p.Delete(key);
            }

            return Json(new
            {
                WasDeleted = wasDeleted
            });
        }

        [HttpPost]
        public ActionResult FetchMetadataBulk(List<string> keys)
        {
            if (keys == null || keys.Count == 0)
                return Json(new List<object>());

            var p = ArchiveProviderFactory.Create();
            var result = p.FetchMetadataBulk(keys?.ToHashSet()) ?? new Dictionary<string, ArchiveMetadataFetchResult>();

            var keysToFetchFromBackup = keys.Except(result.Keys).ToList();
            if (keysToFetchFromBackup.Count > 0 && ArchiveProviderFactory.IsBackupProviderSet)
            {
                var backupProvider = ArchiveProviderFactory.CreateBackup();
                var backupResult = backupProvider.FetchMetadataBulk(keysToFetchFromBackup.ToHashSet()) ??
                                   new Dictionary<string, ArchiveMetadataFetchResult>();

                foreach (var kvp in backupResult)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return Json(keys.Select(x =>
            {
                var resVal = result.TryGetValue(x, out var value) ? value : null;
                return new
                {
                    ArchiveKey = x,
                    Exists = result.ContainsKey(x),
                    ContentType = resVal?.ContentType,
                    FileName = resVal?.FileName,
                    OptionalData = resVal?.OptionalData
                };
            }).ToList());
        }

        [HttpGet]
        public ActionResult Fetch(string key, bool skipFilename = false)
        {
            if (string.IsNullOrWhiteSpace(key))
                return HttpNotFound();

            var p = ArchiveProviderFactory.Create();
            var result = p.Fetch(key);

            if (result == null && ArchiveProviderFactory.IsBackupProviderSet)
            {
                var backupProvider = ArchiveProviderFactory.CreateBackup();
                result = backupProvider.Fetch(key);
            }

            if (result?.OptionalData?.DelayedDocumentType == DelayedDocumentHandler.DelayedPdfType)
            {
                var d = new DelayedDocumentHandler();
                return d.HandleDelayedPdf(key, result);
            }

            if (result == null)
                return HttpNotFound();

            var r = new FileStreamResult(result.Content, result.ContentType);

            if (!skipFilename)
            {
                r.FileDownloadName = result.FileName;
            }

            return r;
        }

        /// <summary>
        /// Cross module communication works much better through the api gateway which does post only currently
        /// hence this method
        /// </summary>
        [HttpPost]
        public ActionResult Download(string key)
        {
            return Fetch(key, skipFilename: true);
        }
    }
}