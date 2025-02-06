using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace nPreCredit.Code
{
    public class nDocumentClient : AbstractServiceClient, IDocumentClient
    {
        protected override string ServiceName => "nDocument";

        public static byte[] GetPdfTemplate(string templateName, bool disableTemplateCache = false)
        {
            return PdfTemplateReaderLegacy.GetPdfTemplate(templateName, NEnv.ClientCfg.Country.BaseCountry, x =>
            {
                var fs = new ICSharpCode.SharpZipLib.Zip.FastZip();
                using (var ms = new MemoryStream())
                {
                    fs.CreateZip(ms, x, true, null, null);
                    return ms.ToArray();
                }
            }, disableTemplateCache || NEnv.IsTemplateCacheDisabled);
        }

        public byte[] PdfRenderDirect(string templateName, IDictionary<string, object> context, bool disableTemplateCache = false)
        {
            var template = GetPdfTemplate(templateName, disableTemplateCache: disableTemplateCache);
            var actualtemplate = Convert.ToBase64String(template);
            var actualContext = JsonConvert.SerializeObject(context);
            using (var ms = new MemoryStream())
            {
                Begin()
                    .PostJson("Pdf/RenderDirect", new
                    {
                        template = actualtemplate,
                        context = actualContext
                    })
                    .CopyToStream(ms);
                return ms.ToArray();
            }
        }

        private class ArchiveStoreResult
        {
            public string Key { get; set; }
        }

        public string ArchiveStore(Uri urlToFile, string filename)
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
                        FileName = filename,
                        Base64EncodedFileData = Convert.ToBase64String(ms.ToArray())
                    })
                    .HandlingApiError(x => x.ParseJsonAs<ArchiveStoreResult>(), x =>
                    {
                        throw new NTech.Services.Infrastructure.NTechWs.NTechWebserviceMethodException(x.ErrorMessage) { ErrorCode = x.ErrorCode };
                    })
                    .Key;
            }
        }
        public string ArchiveStore(byte[] fileData, string mimeType, string filename, string sourceType, string sourceId)
        {
            return Begin()
                .PostJson("Archive/Store", new
                {
                    MimeType = mimeType,
                    FileName = filename,
                    Base64EncodedFileData = Convert.ToBase64String(fileData),
                    SourceType = sourceType,
                    SourceId = sourceId,
                })
                .HandlingApiError(x => x.ParseJsonAs<ArchiveStoreResult>(), x =>
                {
                    throw new NTech.Services.Infrastructure.NTechWs.NTechWebserviceMethodException(x.ErrorMessage) { ErrorCode = x.ErrorCode };
                })
                .Key;
        }

        public string ArchiveStore(byte[] fileData, string mimeType, string filename) =>
            ArchiveStore(fileData, mimeType, filename, null, null);
        public byte[] FetchRaw(string key, out string contentType)
        {
            using (var ms = new MemoryStream())
            {
                string _;
                Begin()
                    .Get("Archive/Fetch?key=" + key)
                    .DownloadFile(ms, out contentType, out _, allowHtml: true);
                return ms.ToArray();
            }
        }

        public byte[] FetchRawWithFilename(string key, out string contentType, out string filename)
        {
            using (var ms = new MemoryStream())
            {
                Begin()
                    .Get("Archive/Fetch?key=" + key)
                    .DownloadFile(ms, out contentType, out filename, allowHtml: true);
                return ms.ToArray();
            }
        }

        public class ArchiveMetadataFetchResult
        {
            public string ContentType { get; set; }
            public string FileName { get; set; }
        }

        public ArchiveMetadataFetchResult FetchMetadata(string key)
        {
            return Begin()
                .PostJson("Archive/FetchMetadata", new
                {
                    key = key
                })
                .ParseJsonAs<ArchiveMetadataFetchResult>();
        }

        private class TempUrlResult
        {
            public string Url { get; set; }
        }

        public Uri CreateTemporarySecurePublicUrl(string key, int? expirationTimeInMinutes = null)
        {
            var rr = Begin()
                .PostJson("Archive/CreateTemporarySecurePublicUrl", new
                {
                    key = key,
                })
                .ParseJsonAs<TempUrlResult>();
            return new Uri(rr.Url);
        }

        public Stream CreateXlsx(DocumentClientExcelRequest request, TimeSpan? timeout = null)
        {
            var ms = new MemoryStream();
            Begin(timeout: timeout)
                .PostJson("Excel/CreateXlsx", request)
                .CopyToStream(ms);
            ms.Position = 0;
            return ms;
        }
    }

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
            public StyleData Style { get; set; } //Override the column style
        }

        public class Column : StyleData
        {
            public string HeaderText { get; set; }
            public bool IncludeSum { get; set; }
            public string CustomSum { get; set; }
            public int? SumNrOfDecimals { get; set; }
        }

        public class StyleData
        {
            public bool IsDate { get; set; }
            public bool IncludeTime { get; set; } //When IsDate is true is used to indicate that not just the date, but the date and time should be shown
            public bool IsNumber { get; set; }
            public bool IsText { get; set; }
            public bool IsPercent { get; set; }
            public int? NrOfDecimals { get; set; }
            public bool IsNumberFormula { get; set; }
            public bool IsNumericId { get; set; }
        }

        public static List<Tuple<Column, Func<T, object>, Func<T, DocumentClientExcelRequest.StyleData>>> CreateDynamicColumnList<T>(IList<T> rows)
        {
            //This is useful when using anonymous local types to avoid having to supply an explicit T
            return new List<Tuple<Column, Func<T, object>, Func<T, DocumentClientExcelRequest.StyleData>>>();
        }
    }

    public enum ExcelType
    {
        Date,
        Number,
        Percent,
        Text,
        NumberFormula
    }

    public static class ExcelExtensions
    {
        public static Tuple<DocumentClientExcelRequest.Column, Func<T, object>, Func<T, DocumentClientExcelRequest.StyleData>> Col<T>(
            this IList<T> source,
            Func<T, object> getValue,
            ExcelType type,
            string headerText,
            int? nrOfDecimals = null,
            bool includeSum = false,
            string customSum = null,
            int? sumNrOfDecimals = null,
            bool includeTime = false,
            bool isNumericId = false,
            Func<T, DocumentClientExcelRequest.StyleData> overrideRowStyle = null)
        {
            return new Tuple<DocumentClientExcelRequest.Column, Func<T, object>, Func<T, DocumentClientExcelRequest.StyleData>>(new DocumentClientExcelRequest.Column
            {
                HeaderText = headerText,
                IncludeSum = includeSum,
                NrOfDecimals = nrOfDecimals,
                CustomSum = customSum,
                SumNrOfDecimals = sumNrOfDecimals,
                IsDate = type == ExcelType.Date,
                IncludeTime = type == ExcelType.Date && includeTime,
                IsNumber = type == ExcelType.Number,
                IsPercent = type == ExcelType.Percent,
                IsText = type == ExcelType.Text,
                IsNumberFormula = type == ExcelType.NumberFormula,
                IsNumericId = isNumericId
            }, x => getValue(x), overrideRowStyle);
        }

        public static void SetColumnsAndData<T>(this DocumentClientExcelRequest.Sheet source, IList<T> rows, params Tuple<DocumentClientExcelRequest.Column, Func<T, object>, Func<T, DocumentClientExcelRequest.StyleData>>[] columns)
        {
            var actualColumns = columns.Where(x => x != null).ToArray(); //To allow conditionally skipping columns without using CreateDynamicColumnList
            source.Columns = actualColumns.Select(x => x.Item1).ToArray();
            source.Cells = rows.Select(row => actualColumns.Select(c =>
            {
                var so = c.Item3?.Invoke(row);
                var style = so ?? c.Item1;
                var value = c.Item2(row);
                if (style.IsNumber)
                {
                    if (value == null)
                        return new DocumentClientExcelRequest.Value { Style = so };
                    else if (value is decimal)
                        return new DocumentClientExcelRequest.Value { V = (decimal)value, Style = so };
                    else if (value is decimal?)
                    {
                        return new DocumentClientExcelRequest.Value { V = (decimal?)value, Style = so };
                    }
                    else if (value is int)
                        return new DocumentClientExcelRequest.Value { V = (int)value, Style = so };
                    else if (value is int?)
                    {
                        var v = (int?)value;
                        return v.HasValue ? new DocumentClientExcelRequest.Value { V = (decimal)v.Value, Style = so } : new DocumentClientExcelRequest.Value { Style = so };
                    }
                    else if (value is long)
                        return new DocumentClientExcelRequest.Value { V = (long)value, Style = so };
                    else if (value is long?)
                    {
                        var v = (long?)value;
                        return v.HasValue ? new DocumentClientExcelRequest.Value { V = (decimal)v.Value, Style = so } : new DocumentClientExcelRequest.Value { Style = so };
                    }
                    else
                        throw new Exception("Number column must be a decimal, decimal?, int or int? ");
                }
                else if (style.IsDate)
                {
                    if (value == null)
                        return new DocumentClientExcelRequest.Value { Style = so };
                    if (value is DateTime)
                        return new DocumentClientExcelRequest.Value { D = (DateTime)value, Style = so };
                    else if (value is DateTime?)
                        return new DocumentClientExcelRequest.Value { D = (DateTime?)value, Style = so };
                    else
                        throw new Exception("Date column must be a DateTime or DateTime?");
                }
                else if (style.IsPercent)
                {
                    if (value == null)
                        return new DocumentClientExcelRequest.Value { Style = so };
                    if (value is decimal)
                        return new DocumentClientExcelRequest.Value { V = ((decimal)value), Style = so };
                    else if (value is decimal?)
                    {
                        var v = (decimal?)value;
                        return new DocumentClientExcelRequest.Value { V = (decimal?)value, Style = so };
                    }
                    else
                        throw new Exception("Percent column must be a decimal or decimal? ");
                }
                else if (style.IsText)
                {
                    if (value == null)
                        return new DocumentClientExcelRequest.Value { Style = so };
                    if (value is string)
                        return new DocumentClientExcelRequest.Value { S = (string)value, Style = so };
                    else throw new Exception("Text column must be string");
                }
                else if (style.IsNumberFormula)
                {
                    if (value is string)
                        return new DocumentClientExcelRequest.Value { S = (string)value, Style = so };
                    else throw new Exception("Number formula column must be string");
                }
                else
                {
                    throw new NotImplementedException();
                }
            }).ToArray()).ToArray();
        }
    }
}