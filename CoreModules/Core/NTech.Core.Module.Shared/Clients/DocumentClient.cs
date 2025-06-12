using Newtonsoft.Json;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NTech.Core.Module.Shared.Clients
{
    public class DocumentClient : IDocumentClient
    {
        private readonly ServiceClient _client;

        public DocumentClient(INHttpServiceUser httpServiceUser, ServiceClientFactory serviceClientFactory)
        {
            _client = serviceClientFactory.CreateClient(httpServiceUser, "nDocument");
        }

        public Task<string> ArchiveStoreAsync(byte[] fileData, string mimeType, string filename) =>
            ArchiveStoreWithSourceAsync(fileData, mimeType, filename, null, null);

        public string ArchiveStore(byte[] fileData, string mimeType, string filename) =>
            ArchiveStoreWithSource(fileData, mimeType, filename, null, null);

        public async Task<string> ArchiveStoreWithSourceAsync(byte[] fileData, string mimeType, string filename,
            string sourceType, string sourceId) => (await _client.Call(
            x => x.PostJson("Archive/Store", new
            {
                MimeType = mimeType,
                FileName = filename,
                Base64EncodedFileData = Convert.ToBase64String(fileData),
                SourceType = sourceType,
                SourceId = sourceId
            }),
            x => x.ParseJsonAsAnonymousType(new { Key = "" }, propagateApiError: true)))?.Key;

        public string ArchiveStoreWithSource(byte[] fileData, string mimeType, string filename, string sourceType,
            string sourceId) => _client.ToSync(() =>
            ArchiveStoreWithSourceAsync(fileData, mimeType, filename, sourceType, sourceId));

        private class TryExportResult
        {
            public bool IsSuccess { get; set; }
            public string ProfileName { get; set; }
            public List<string> SuccessProfileNames { get; set; }
            public List<string> FailedProfileNames { get; set; }
            public int TimeInMs { get; set; }
        }

        public async
            Task<(bool IsSuccess, List<string> SuccessProfileNames, List<string> FailedProfileNames, int? TimeInMs)>
            ExportArchiveFileAsync(string archiveKey, string exportProfileName, string filename)
        {
            var result = await _client.Call(
                x => x.PostJson("FileExport/Export",
                    new { FileArchiveKey = archiveKey, ProfileName = exportProfileName, Filename = filename }),
                x => x.ParseJsonAs<TryExportResult>());
            return (IsSuccess: result.IsSuccess, SuccessProfileNames: result.SuccessProfileNames,
                FailedProfileNames: result.FailedProfileNames, TimeInMs: result.TimeInMs);
        }

        public (bool IsSuccess, List<string> SuccessProfileNames, List<string> FailedProfileNames, int? TimeInMs)
            ExportArchiveFile(string archiveKey, string exportProfileName, string filename) =>
            _client.ToSync(() => ExportArchiveFileAsync(archiveKey, exportProfileName, filename));

        public Task<MemoryStream> CreateXlsxAsync(nCredit.Excel.DocumentClientExcelRequest request) => _client.Call(
            x => x.PostJson("Excel/CreateXlsx", request),
            async x =>
            {
                var ms = new MemoryStream();
                await x.CopyToStream(ms);
                ms.Position = 0;
                return ms;
            });

        public MemoryStream CreateXlsx(nCredit.Excel.DocumentClientExcelRequest request) =>
            _client.ToSync(() => CreateXlsxAsync(request));

        private class CreateXlsxToArchiveResult
        {
            public string Key { get; set; }
        }

        public async Task<string> CreateXlsxToArchiveAsync(nCredit.Excel.DocumentClientExcelRequest request,
            string archiveFileName) => (await _client.Call(
            x => x.PostJson("Excel/CreateXlsxToArchive", new { request = request, archiveFileName = archiveFileName }),
            x => x.ParseJsonAs<CreateXlsxToArchiveResult>()))?.Key;

        public string CreateXlsxToArchive(nCredit.Excel.DocumentClientExcelRequest request, string archiveFileName) =>
            _client.ToSync(() => CreateXlsxToArchiveAsync(request, archiveFileName));

        public Task<(bool IsSuccess, string ContentType, string FileName, byte[] FileData)>
            TryFetchRawAsync(string key) => _client
            .Call(x => x.Get("Archive/Fetch?key=" + key), async x =>
            {
                if (x.IsNotFoundStatusCode)
                    return (IsSuccess: false, ContentType: null, FileName: null, FileData: null);

                using (var ms = new MemoryStream())
                {
                    var fileMetadata = await x.DownloadFile(ms);
                    var fileBytes = ms.ToArray();
                    return (IsSuccess: true, ContentType: fileMetadata.Item1, FileName: fileMetadata.Item2,
                        FileData: fileBytes);
                }
            });

        public (bool IsSuccess, string ContentType, string FileName, byte[] FileData) TryFetchRaw(string key) =>
            _client.ToSync(() => TryFetchRawAsync(key));

        public async Task<string> ArchiveStoreFileAsync(FileInfo file, string mimeType, string fileName) =>
            (await _client.Call(
                async x =>
                {
                    using (var fs = file.OpenRead())
                    {
                        return await x.UploadFile("Archive/StoreFile", fs, fileName, mimeType);
                    }
                },
                x => x.ParseJsonAsAnonymousType(new { Key = "" })))?.Key;

        public string ArchiveStoreFile(FileInfo file, string mimeType, string fileName) =>
            _client.ToSync(() => ArchiveStoreFileAsync(file, mimeType, fileName));

        public Task<(bool IsSuccess, List<string> SuccessProfileNames, List<string> FailedProfileNames, int TimeInMs)>
            TryExportArchiveFileAsync(string archiveKey, string exportProfileName, string filename = null) =>
            _client.Call(
                x => x.PostJson("FileExport/Export",
                    new { FileArchiveKey = archiveKey, ProfileName = exportProfileName, Filename = filename }),
                async x =>
                {
                    var result = await x.ParseJsonAs<TryExportResult>();
                    return (IsSuccess: result.IsSuccess, SuccessProfileNames: result.SuccessProfileNames,
                        FailedProfileNames: result.FailedProfileNames, TimeInMs: result.TimeInMs);
                });

        public (bool IsSuccess, List<string> SuccessProfileNames, List<string> FailedProfileNames, int TimeInMs)
            TryExportArchiveFile(string archiveKey, string exportProfileName, string filename = null) =>
            _client.ToSync(() => TryExportArchiveFileAsync(archiveKey, exportProfileName, filename: filename));

        public async Task<bool> DeleteArchiveFileAsync(string key) => (await _client.Call(
            x => x.PostJson("Archive/Delete", new
            {
                Key = key
            }),
            x => x.ParseJsonAsAnonymousType(new { WasDeleted = (bool)false }, propagateApiError: true))).WasDeleted;

        public bool DeleteArchiveFile(string key) => _client.ToSync(() => DeleteArchiveFileAsync(key));

        public async Task<string> BatchRenderBeginAsync(byte[] template) => (await _client.Call(
            x => x.PostJson("Pdf/BatchRenderBegin", new { template = Convert.ToBase64String(template) }),
            x => x.ParseJsonAsAnonymousType(new { BatchId = (string)null })))?.BatchId;

        public string BatchRenderBegin(byte[] template) => _client.ToSync(() => BatchRenderBeginAsync(template));

        public async Task<string> BatchRenderDocumentToArchiveAsync(string batchId, string renderedPdfFileName,
            IDictionary<string, object> context) => (await _client.Call(
            x => x.PostJson("Pdf/BatchRenderDocumentToArchive",
                new { batchId, context = JsonConvert.SerializeObject(context), filename = renderedPdfFileName }),
            x => x.ParseJsonAsAnonymousType(new { Key = (string)null })))?.Key;

        public string BatchRenderDocumentToArchive(string batchId, string renderedPdfFileName,
            IDictionary<string, object> context) => _client.ToSync(() =>
            BatchRenderDocumentToArchiveAsync(batchId, renderedPdfFileName, context));

        public Task BatchRenderEndAsync(string batchId) => _client.CallVoid(
            x => x.PostJson("Pdf/BatchRenderEnd", new { batchId }),
            x => x.EnsureSuccessStatusCode());

        public void BatchRenderEnd(string batchId) => _client.ToSync(() => BatchRenderEndAsync(batchId));

        public async Task<string> BatchRenderDelayedBeginAsync(byte[] template) => (await _client.Call(
            x => x.PostJson("Pdf/BatchRenderDelayedBegin", new { template = template }),
            x => x.ParseJsonAsAnonymousType(new { BatchId = (string)null })))?.BatchId;

        public string BatchRenderDelayedBegin(byte[] template) =>
            _client.ToSync(() => BatchRenderDelayedBeginAsync(template));

        public async Task<string> BatchRenderDelayedDocumentToArchiveAsync(string batchId, string renderedPdfFileName,
            IDictionary<string, object> context) =>
            (await _client.Call(
                x => x.PostJson("Pdf/BatchRenderDelayedDocumentToArchive",
                    new
                    {
                        batchId = batchId, context = JsonConvert.SerializeObject(context),
                        filename = renderedPdfFileName
                    }),
                x => x.ParseJsonAsAnonymousType(new { Key = (string)null })))?.Key;

        public string BatchRenderDelayedDocumentToArchive(string batchId, string renderedPdfFileName,
            IDictionary<string, object> context) =>
            _client.ToSync(() => BatchRenderDelayedDocumentToArchiveAsync(batchId, renderedPdfFileName, context));

        public Task BatchRenderDelayedEndAsync(string batchId) => _client.CallVoid(
            x => x.PostJson("Pdf/BatchRenderDelayedEnd", new { batchId = batchId }),
            x => x.EnsureSuccessStatusCode());

        public void BatchRenderDelayedEnd(string batchId) => _client.ToSync(() => BatchRenderDelayedEndAsync(batchId));

        public Task<byte[]> PdfRenderDirectAsync(byte[] template, IDictionary<string, object> context) => _client.Call(
            x => x.PostJson("Pdf/RenderDirect", new
            {
                template = template,
                context = JsonConvert.SerializeObject(context)
            }),
            async x =>
            {
                using (var ms = new MemoryStream())
                {
                    await x.CopyToStream(ms);
                    return ms.ToArray();
                }
            });

        public byte[] PdfRenderDirect(byte[] template, IDictionary<string, object> context) =>
            _client.ToSync(() => PdfRenderDirectAsync(template, context));

        public Dictionary<string, List<List<string>>> ParseDataUrlExcelFile(string fileName, string fileAsDataUrl,
            bool leavePercentAsFraction) =>
            _client.ToSync(() => ParseDataUrlExcelFileAsync(fileName, fileAsDataUrl, leavePercentAsFraction));

        public async Task<Dictionary<string, List<List<string>>>> ParseDataUrlExcelFileAsync(string fileName,
            string fileAsDataUrl, bool leavePercentAsFraction)
        {
            var result =
                await _client
                    .Call<(Dictionary<string, List<List<string>>> Success, NTechCoreWebserviceException Error)>(
                        x => x.PostJson("Excel/ParseDataUrlExcelFile",
                            new { fileName, fileAsDataUrl, leavePercentAsFraction }),
                        async x =>
                        {
                            if (x.IsSuccessStatusCode)
                            {
                                return (
                                    Success: (await x.ParseJsonAsAnonymousType(new
                                        { sheets = (Dictionary<string, List<List<string>>>)null }))?.sheets,
                                    Error: null);
                            }
                            else if (x.IsApiError)
                            {
                                var e = await x.ParseApiError();
                                return (Success: null,
                                    Error: new NTechCoreWebserviceException(e.ErrorMessage)
                                        { ErrorCode = e.ErrorCode });
                            }
                            else
                            {
                                x.EnsureSuccessStatusCode();
                                throw
                                    new NotImplementedException(); //NOTE: Will never be reached, the compiler is just stupid
                            }
                        });
            if (result.Success != null)
                return result.Success;
            else
                throw result.Error;
        }

        public Dictionary<string, ArchiveMetadataFetchResult> FetchMetadataBulk(ISet<string> keys) =>
            _client.ToSync(() => FetchMetadataBulkAsync(keys));

        public async Task<Dictionary<string, ArchiveMetadataFetchResult>> FetchMetadataBulkAsync(ISet<string> keys)
        {
            var result = await _client.Call(x => x.PostJson("Archive/FetchMetadataBulk", new
                {
                    keys = keys
                }),
                x => x.ParseJsonAsAnonymousType(new[]
                {
                    new
                    {
                        ArchiveKey = "",
                        Exists = false,
                        ContentType = "",
                        FileName = "",
                    }
                }));

            return result
                .Where(x => x.Exists)
                .ToDictionary(x => x.ArchiveKey, x => new ArchiveMetadataFetchResult
                {
                    ContentType = x.ContentType,
                    FileName = x.FileName
                });
        }

        public Task<ArchiveMetadataFetchResult> FetchMetadataAsync(string key, bool returnNullOnNotExists) =>
            _client.Call(
                x => x.PostJson("Archive/FetchMetadata", new
                {
                    key = key
                }),
                x =>
                {
                    if (returnNullOnNotExists && x.IsNotFoundStatusCode)
                        return null;
                    return x.ParseJsonAs<ArchiveMetadataFetchResult>();
                });

        public ArchiveMetadataFetchResult FetchMetadata(string key, bool returnNullOnNotExists) =>
            _client.ToSync(() => FetchMetadataAsync(key, returnNullOnNotExists));

        public class ArchiveMetadataFetchResult
        {
            public string ContentType { get; set; }
            public string FileName { get; set; }
        }
    }
}