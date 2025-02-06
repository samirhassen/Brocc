using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static NTech.Core.Module.Shared.Clients.DocumentClient;

namespace NTech.Core.Module.Shared.Clients
{
    public interface IDocumentClient
    {
        Task<string> ArchiveStoreAsync(byte[] fileData, string mimeType, string filename);
        string ArchiveStore(byte[] fileData, string mimeType, string filename);
        Task<string> ArchiveStoreWithSourceAsync(byte[] fileData, string mimeType, string filename, string sourceType, string sourceId);
        string ArchiveStoreWithSource(byte[] fileData, string mimeType, string filename, string sourceType, string sourceId);
        Task<(bool IsSuccess, List<string> SuccessProfileNames, List<string> FailedProfileNames, int? TimeInMs)> ExportArchiveFileAsync(string archiveKey, string exportProfileName, string filename);
        (bool IsSuccess, List<string> SuccessProfileNames, List<string> FailedProfileNames, int? TimeInMs) ExportArchiveFile(string archiveKey, string exportProfileName, string filename);
        Task<MemoryStream> CreateXlsxAsync(nCredit.Excel.DocumentClientExcelRequest request);
        MemoryStream CreateXlsx(nCredit.Excel.DocumentClientExcelRequest request);
        Task<string> CreateXlsxToArchiveAsync(nCredit.Excel.DocumentClientExcelRequest request, string archiveFileName);
        string CreateXlsxToArchive(nCredit.Excel.DocumentClientExcelRequest request, string archiveFileName);
        Task<(bool IsSuccess, string ContentType, string FileName, byte[] FileData)> TryFetchRawAsync(string key);
        (bool IsSuccess, string ContentType, string FileName, byte[] FileData) TryFetchRaw(string key);
        Task<string> ArchiveStoreFileAsync(FileInfo file, string mimeType, string fileName);
        string ArchiveStoreFile(FileInfo file, string mimeType, string fileName);
        bool DeleteArchiveFile(string key);
        Task<(bool IsSuccess, List<string> SuccessProfileNames, List<string> FailedProfileNames, int TimeInMs)> TryExportArchiveFileAsync(string archiveKey, string exportProfileName, string filename = null);
        (bool IsSuccess, List<string> SuccessProfileNames, List<string> FailedProfileNames, int TimeInMs) TryExportArchiveFile(string archiveKey, string exportProfileName, string filename = null);
        string BatchRenderBegin(byte[] template);
        string BatchRenderDocumentToArchive(string batchId, string renderedPdfFileName, IDictionary<string, object> context);
        void BatchRenderEnd(string batchId);
        byte[] PdfRenderDirect(byte[] template, IDictionary<string, object> context);
        Dictionary<string, List<List<string>>> ParseDataUrlExcelFile(string fileName, string fileAsDataUrl, bool leavePercentAsFraction);
        Dictionary<string, ArchiveMetadataFetchResult> FetchMetadataBulk(ISet<string> keys);
        ArchiveMetadataFetchResult FetchMetadata(string key, bool returnNullOnNotExists);
        string BatchRenderDelayedBegin(byte[] template);
        string BatchRenderDelayedDocumentToArchive(string batchId, string renderedPdfFileName, IDictionary<string, object> context);
        void BatchRenderDelayedEnd(string batchId);
    }
}
namespace nCredit.Excel
{
    public class DocumentClientExcelRequest
    {
        public Sheet[] Sheets { get; set; }
        public string TemplateXlsxDocumentBytesAsBase64 { get; set; }

        public bool ColorCell { get; set; }

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
            public string BackgroundColors { get; set; }

            public IDictionary<string, string> ColorValues { get; set; }
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

        /// <summary>
        /// The items list must only have properties with simple type value like int, string and such.
        /// This will create and excel file with one sheet with one column per property
        /// </summary>
        public static DocumentClientExcelRequest CreateSimpleRequest<TItem>(List<TItem> items, string title) where TItem : class, new()
        {
            var columns = CreateDynamicColumnList(items);
            foreach (var property in typeof(TItem).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
            {
                if (property.PropertyType == typeof(string))
                    columns.Add(items.Col(property.GetValue, ExcelType.Text, property.Name));
                else if (property.PropertyType == typeof(DateTime?) || property.PropertyType == typeof(DateTime))
                    columns.Add(items.Col(property.GetValue, ExcelType.Date, property.Name));
                else if (property.PropertyType == typeof(System.DateTimeOffset?) || property.PropertyType == typeof(System.DateTimeOffset))
                    columns.Add(items.Col(x => ((DateTimeOffset?)property.GetValue(x))?.DateTime, ExcelType.Date, property.Name));
                else if (property.PropertyType == typeof(decimal?) || property.PropertyType == typeof(decimal))
                    columns.Add(items.Col(property.GetValue, ExcelType.Number, property.Name));
                else if (property.PropertyType == typeof(int?) || property.PropertyType == typeof(int))
                    columns.Add(items.Col(property.GetValue, ExcelType.Number, property.Name, nrOfDecimals: 0));
                else if (property.PropertyType == typeof(bool?) || property.PropertyType == typeof(bool))
                    columns.Add(items.Col(x => 
                    {
                        var v = (bool?)property.GetValue(x);
                        return v == null ? new int?(): (v.Value ? 1 : 0);
                    }, ExcelType.Number, property.Name, nrOfDecimals: 0));
                else
                    columns.Add(items.Col(_ => "Unsupported type", ExcelType.Text, property.Name));
            }

            var request = new DocumentClientExcelRequest
            {
                Sheets = new Sheet[]
                {
                    new Sheet
                    {
                        AutoSizeColumns = true,
                        Title = title,

                    }
                }
            };
            var sheet = request.Sheets[0];
            sheet.SetColumnsAndData(items, columns.ToArray());
            return request;
        }

        public const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
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
            string backgroundColors = null,
            Dictionary<string, string> ColorValues = null,
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
                IsNumericId = isNumericId,
                BackgroundColors = backgroundColors,
                ColorValues = ColorValues
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
