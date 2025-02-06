using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace nSavings.Code
{
    public class DocumentClient : AbstractServiceClient
    {
        protected override string ServiceName => "nDocument";

        public Stream CreateXlsx(Excel.DocumentClientExcelRequest request)
        {
            var ms = new MemoryStream();
            Begin()
                .PostJson("Excel/CreateXlsx", request)
                .CopyToStream(ms);
            ms.Position = 0;
            return ms;
        }

        public string CreateXlsxToArchive(Excel.DocumentClientExcelRequest request, string archiveFileName)
        {
            return Begin()
                .PostJson("Excel/CreateXlsxToArchive", new { request = request, archiveFileName = archiveFileName })
                .ParseJsonAs<CreateXlsxToArchiveResult>().Key;
        }

        public ArchiveMetadataFetchResult FetchMetadata(string key, bool returnNullOnNotExists)
        {
            var r = Begin().PostJson("Archive/FetchMetadata", new
            {
                key = key
            });

            if (returnNullOnNotExists && r.IsNotFoundStatusCode)
                return null;

            return r.ParseJsonAs<ArchiveMetadataFetchResult>();
        }

        private class CreateXlsxToArchiveResult
        {
            public string Key { get; set; }
        }

        public bool TryFetchRaw(string key, out string contentType, out string fileName, out byte[] fileBytes)
        {
            using (var ms = new MemoryStream())
            {
                var r = Begin().Get("Archive/Fetch?key=" + key);

                if (r.IsNotFoundStatusCode)
                {
                    fileName = null;
                    fileBytes = null;
                    contentType = null;
                    return false;
                }

                r.DownloadFile(ms, out contentType, out fileName);

                fileBytes = ms.ToArray();

                return true;
            }
        }

        public byte[] FetchRaw(string key, out string contentType, out string fileName)
        {
            byte[] b;
            if (TryFetchRaw(key, out contentType, out fileName, out b))
            {
                return b;
            }
            else
                throw new Exception($"Missing document {key} in the archive");
        }

        private class ArchiveStoreResult
        {
            public string Key { get; set; }
        }

        public string ArchiveStore(byte[] fileData, string mimeType, string filename)
        {
            return Begin().PostJson("Archive/Store", new
            {
                MimeType = mimeType,
                FileName = filename,
                Base64EncodedFileData = Convert.ToBase64String(fileData)
            }).ParseJsonAs<ArchiveStoreResult>().Key;
        }

        public string ArchiveStoreFile(FileInfo file, string mimeType, string fileName)
        {
            using (var fs = file.OpenRead())
            {
                return Begin().UploadFile("Archive/StoreFile", fs, fileName, mimeType).ParseJsonAs<ArchiveStoreResult>().Key;
            }
        }

        public string ArchiveStoreFromUrl(Uri urlToFile, string fileName)
        {
            var client = new HttpClient();
            var result = client.GetAsync(urlToFile.ToString()).Result;
            using (var ms = new MemoryStream())
            {
                result.Content.CopyToAsync(ms).Wait();

                return Begin()
                    .PostJson("Archive/Store", new
                    {
                        MimeType = result.Content.Headers.ContentType.ToString(),
                        FileName = fileName,
                        Base64EncodedFileData = Convert.ToBase64String(ms.ToArray())
                    })
                    .ParseJsonAs<ArchiveStoreResult>()
                    .Key;
            }
        }

        private class BatchRenderBeginResult
        {
            public string BatchId { get; set; }
        }

        private byte[] GetPdfTemplate(string templateName)
        {
            return PdfTemplateReaderLegacy.GetPdfTemplate(templateName, NEnv.ClientCfg.Country.BaseCountry, x =>
            {
                var fs = new ICSharpCode.SharpZipLib.Zip.FastZip();
                using (var ms = new MemoryStream())
                {
                    fs.CreateZip(ms, x, true, null, null);
                    return ms.ToArray();
                }
            }, NEnv.IsTemplateCacheDisabled);
        }

        public string BatchRenderBegin(string templateName)
        {
            var bytes = GetPdfTemplate(templateName);

            return Begin()
                .PostJson("Pdf/BatchRenderBegin", new { template = Convert.ToBase64String(bytes) })
                .ParseJsonAs<BatchRenderBeginResult>()
                .BatchId;
        }

        private class BatchRenderResult
        {
            public string Key { get; set; }
        }

        public string BatchRenderDocumentToArchive(string batchId, string renderedPdfFileName, IDictionary<string, object> context)
        {
            return Begin()
                .PostJson("Pdf/BatchRenderDocumentToArchive", new { batchId = batchId, context = JsonConvert.SerializeObject(context), filename = renderedPdfFileName })
                .ParseJsonAs<BatchRenderResult>()
                .Key;
        }

        public void BatchRenderEnd(string batchId)
        {
            Begin()
                .PostJson("Pdf/BatchRenderEnd", new { batchId = batchId })
                .EnsureSuccessStatusCode();
        }

        public string BatchRenderDelayedBegin(string templateName)
        {
            var bytes = GetPdfTemplate(templateName);
            return Begin()
                .PostJson("Pdf/BatchRenderDelayedBegin", new { template = Convert.ToBase64String(bytes) })
                .ParseJsonAs<BatchRenderBeginResult>()
                .BatchId;
        }

        public string BatchRenderDelayedDocumentToArchive(string batchId, string renderedPdfFileName, IDictionary<string, object> context)
        {
            return Begin()
                .PostJson("Pdf/BatchRenderDelayedDocumentToArchive", new { batchId = batchId, context = JsonConvert.SerializeObject(context), filename = renderedPdfFileName })
                .ParseJsonAs<BatchRenderResult>()
                .Key;
        }

        public void BatchRenderDelayedEnd(string batchId)
        {
            Begin()
                .PostJson("Pdf/BatchRenderDelayedEnd", new { batchId = batchId })
                .EnsureSuccessStatusCode();
        }

        public T WithDocumentBatchRenderer<T>(string templateName, Func<Func<IDictionary<string, object>, string, string>, T> doWithRenderer, bool useDelayedDocuments = false)
        {
            var batchId = useDelayedDocuments ? BatchRenderDelayedBegin(templateName) : BatchRenderBegin(templateName);
            try
            {
                Func<IDictionary<string, object>, string, string> renderToDocumentArchive = (ctx, pdfFileName) =>
                    useDelayedDocuments
                    ? BatchRenderDelayedDocumentToArchive(batchId, pdfFileName, ctx)
                    : BatchRenderDocumentToArchive(batchId, pdfFileName, ctx);

                return doWithRenderer(renderToDocumentArchive);
            }
            finally
            {
                if (useDelayedDocuments)
                    BatchRenderDelayedEnd(batchId);
                else
                    BatchRenderEnd(batchId);
            }
        }

        public Stream PdfRenderDirect(string templateName, IDictionary<string, object> context)
        {
            var actualtemplate = Convert.ToBase64String(GetPdfTemplate(templateName));
            var actualContext = JsonConvert.SerializeObject(context);

            var ms = new MemoryStream();
            Begin()
                .PostJson("Pdf/RenderDirect", new
                {
                    template = actualtemplate,
                    context = actualContext
                })
                .CopyToStream(ms);

            ms.Position = 0;
            return ms;
        }

        private class TryExportResult
        {
            public bool IsSuccess { get; set; }
            public string ProfileName { get; set; }
            public List<string> SuccessProfileNames { get; set; }
            public List<string> FailedProfileNames { get; set; }
            public int TimeInMs { get; set; }
        }

        public bool TryExportArchiveFile(string archiveKey, string exportProfileName, out List<string> successProfileNames, out List<string> failedProfileNames, out int timeInMs, string filename = null)
        {
            var result = Begin()
                .PostJson("FileExport/Export", new { FileArchiveKey = archiveKey, ProfileName = exportProfileName, Filename = filename })
                .ParseJsonAs<TryExportResult>();
            timeInMs = result.TimeInMs;
            successProfileNames = result.SuccessProfileNames;
            failedProfileNames = result.FailedProfileNames;
            return result.IsSuccess;
        }
    }

    public class ArchiveMetadataFetchResult
    {
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }
}
namespace nSavings.Excel
{
    public class DocumentClientExcelRequest
    {
        public Sheet[] Sheets { get; set; }
        public string TemplateXlsxDocumentBytesAsBase64 { get; set; }

        public class Sheet
        {
            public string Title { get; set; }

            public Column[] Columns { get; set; }

            public Value[][] Cells { get; set; }

            public bool AutoSizeColumns { get; set; }
        }

        public class Value
        {
            public string S { get; set; }
            public DateTime? D { get; set; }
            public decimal? V { get; set; }
        }

        public class Column
        {
            public string HeaderText { get; set; }
            public bool IsDate { get; set; }
            public bool IsNumber { get; set; }
            public bool IsText { get; set; }
            public bool IsPercent { get; set; }
            public int? NrOfDecimals { get; set; }
            public bool IncludeSum { get; set; }
            public string CustomSum { get; set; }
            public int? SumNrOfDecimals { get; set; }
            public bool IsNumericId { get; set; }
        }

        public static List<Tuple<Column, Func<T, object>>> CreateDynamicColumnList<T>(IList<T> rows)
        {
            //This is useful when using anonymous local types to avoid having to supply an explicit T
            return new List<Tuple<Column, Func<T, object>>>();
        }
    }
    public enum ExcelType
    {
        Date,
        Number,
        Percent,
        Text
    }

    public static class ExcelExtensions
    {
        public static Tuple<DocumentClientExcelRequest.Column, Func<T, object>> Col<T>(
            this IList<T> source,
            Func<T, object> getValue,
            ExcelType type,
            string headerText,
            int? nrOfDecimals = null,
            bool includeSum = false,
            string customSum = null,
            int? sumNrOfDecimals = null,
            bool isNumericId = false)
        {
            return new Tuple<DocumentClientExcelRequest.Column, Func<T, object>>(new DocumentClientExcelRequest.Column
            {
                HeaderText = headerText,
                IncludeSum = includeSum,
                NrOfDecimals = nrOfDecimals,
                CustomSum = customSum,
                SumNrOfDecimals = sumNrOfDecimals,
                IsDate = type == ExcelType.Date,
                IsNumber = type == ExcelType.Number,
                IsPercent = type == ExcelType.Percent,
                IsText = type == ExcelType.Text,
                IsNumericId = isNumericId
            }, x => getValue(x));
        }

        public static void SetColumnsAndData<T>(this DocumentClientExcelRequest.Sheet source, IList<T> rows, params Tuple<DocumentClientExcelRequest.Column, Func<T, object>>[] columns)
        {
            source.Columns = columns.Select(x => x.Item1).ToArray();
            source.Cells = rows.Select(row => columns.Select(c =>
            {
                var col = c.Item1;
                var value = c.Item2(row);
                if (col.IsNumber)
                {
                    if (value == null)
                        return new DocumentClientExcelRequest.Value { };
                    if (value is decimal)
                        return new DocumentClientExcelRequest.Value { V = (decimal)value };
                    else if (value is decimal?)
                    {
                        return new DocumentClientExcelRequest.Value { V = (decimal?)value };
                    }
                    if (value is int)
                        return new DocumentClientExcelRequest.Value { V = (int)value };
                    else if (value is int?)
                    {
                        var v = (int?)value;
                        return v.HasValue ? new DocumentClientExcelRequest.Value { V = (decimal)v.Value } : new DocumentClientExcelRequest.Value { };
                    }
                    else
                        throw new Exception("Number column must be a decimal, decimal?, int or int? ");
                }
                else if (col.IsDate)
                {
                    if (value == null)
                        return new DocumentClientExcelRequest.Value { };
                    if (value is DateTime)
                        return new DocumentClientExcelRequest.Value { D = (DateTime)value };
                    else if (value is DateTime?)
                        return new DocumentClientExcelRequest.Value { D = (DateTime?)value };
                    else
                        throw new Exception("Date column must be a DateTime or DateTime?");
                }
                else if (col.IsPercent)
                {
                    if (value == null)
                        return new DocumentClientExcelRequest.Value { };
                    if (value is decimal)
                        return new DocumentClientExcelRequest.Value { V = ((decimal)value) };
                    else if (value is decimal?)
                    {
                        var v = (decimal?)value;
                        return new DocumentClientExcelRequest.Value { V = (decimal?)value };
                    }
                    else
                        throw new Exception("Percent column must be a decimal or decimal? ");
                }
                else if (col.IsText)
                {
                    if (value == null)
                        return new DocumentClientExcelRequest.Value { };
                    if (value is string)
                        return new DocumentClientExcelRequest.Value { S = (string)value };
                    else throw new Exception("Text column must be string");
                }
                else
                {
                    throw new NotImplementedException();
                }
            }).ToArray()).ToArray();
        }
    }
}