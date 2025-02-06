using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace nDataWarehouse.Code
{
    public class DocumentClient
    {
        public Stream CreateXlsx(DocumentClientExcelRequest request, TimeSpan timeout)
        {
            var ms = new MemoryStream();
            NHttp
                .Begin(new Uri(NEnv.ServiceRegistry.Internal["nDocument"]), NHttp.GetCurrentAccessToken(), timeout: timeout)
                .PostJson("Excel/CreateXlsx", request, allowSkipNulls: true)
                .CopyToStream(ms);
            ms.Position = 0;
            return ms;
        }

        public string ArchiveStoreFile(FileInfo file, string mimeType, string fileName, TimeSpan timeout)
        {
            using (var fs = file.OpenRead())
            {
                return NHttp
                        .Begin(new Uri(NEnv.ServiceRegistry.Internal["nDocument"]), NHttp.GetCurrentAccessToken(), timeout: timeout)
                        .UploadFile("Archive/StoreFile", fs, fileName, mimeType).ParseJsonAsAnonymousType(new { Key = "" })
                        .Key;
            }
        }

        public static Uri GetArchiveFetchLink(string key)
        {
            return new Uri(new Uri(NEnv.ServiceRegistry.External["nDocument"]), "Archive/Fetch?key=" + key);
        }
    }

    public class DocumentClientExcelRequest
    {
        public Sheet[] Sheets { get; set; }

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
            public bool IncludeTime { get; set; }
            public bool IsNumber { get; set; }
            public bool IsNumericId { get; set; }
            public bool IsText { get; set; }
            public bool IsPercent { get; set; }
            public int? NrOfDecimals { get; set; }
            public bool IncludeSum { get; set; }
            public string CustomSum { get; set; }
            public int? SumNrOfDecimals { get; set; }
            public bool IsNumberFormula { get; set; }
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
        Text,
        NumberFormula
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
            bool includeTime = false,
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
                IncludeTime = type == ExcelType.Date && includeTime,
                IsNumber = type == ExcelType.Number,
                IsPercent = type == ExcelType.Percent,
                IsText = type == ExcelType.Text,
                IsNumberFormula = type == ExcelType.NumberFormula,
                IsNumericId = isNumericId
            }, x => getValue(x));
        }

        public static DocumentClientExcelRequest.Sheet SetColumnsAndData<T>(this DocumentClientExcelRequest.Sheet source, IList<T> rows, params Tuple<DocumentClientExcelRequest.Column, Func<T, object>>[] columns)
        {
            var actualColumns = columns.Where(x => x != null).ToArray(); //To allow conditionally skipping columns without using CreateDynamicColumnList 
            source.Columns = actualColumns.Select(x => x.Item1).ToArray();
            source.Cells = rows.Select(row => actualColumns.Select(c =>
            {
                var col = c.Item1;
                var value = c.Item2(row);
                if (col.IsNumber)
                {
                    if (value == null)
                        return new DocumentClientExcelRequest.Value { };
                    else if (value is decimal)
                        return new DocumentClientExcelRequest.Value { V = (decimal)value };
                    else if (value is decimal?)
                    {
                        return new DocumentClientExcelRequest.Value { V = (decimal?)value };
                    }
                    else if (value is int)
                        return new DocumentClientExcelRequest.Value { V = (int)value };
                    else if (value is int?)
                    {
                        var v = (int?)value;
                        return v.HasValue ? new DocumentClientExcelRequest.Value { V = (decimal)v.Value } : new DocumentClientExcelRequest.Value { };
                    }
                    else if (value is long)
                        return new DocumentClientExcelRequest.Value { V = (long)value };
                    else if (value is long?)
                    {
                        var v = (long?)value;
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
                else if (col.IsNumberFormula)
                {
                    if (value is string)
                        return new DocumentClientExcelRequest.Value { S = (string)value };
                    else throw new Exception("Number formula column must be string");
                }
                else
                {
                    throw new NotImplementedException();
                }
            }).ToArray()).ToArray();

            return source;
        }
    }
}