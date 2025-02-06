using ICSharpCode.SharpZipLib.Zip;
using NPOI.SS.UserModel;
using NPOI.XSSF.Streaming;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Threading;

namespace nDataWarehouse.Code
{
    public class ExcelQueryExporter
    {
        public class CivicRegNrMapping
        {
            public IDictionary<int, string> CivicRegNrByCustomerId { get; set; }
            public ISet<string> CustomerIdColumns { get; set; }
        }

        public void CreateSplitXlsxZipFromQueryToStream(SqlConnection connection, string query, Action<SqlCommand> setQueryParameters, Stream targetStream, CivicRegNrMapping civicRegNrMapping, Func<int, string> xlsFileNameFromFileNr, int maxNrOfRowsPerReport)
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);
            try
            {
                var nrOfRowsRead = 0;
                var fileNr = 0;

                int nrOfDataRowsInFile = 1; //Just to start the loop
                while (nrOfDataRowsInFile > 0 && fileNr++ < 1000)
                {
                    var tmpfile = Path.Combine(tempFolder, xlsFileNameFromFileNr(fileNr));
                    using (var stream = new FileStream(tmpfile, FileMode.CreateNew, FileAccess.ReadWrite))
                    {
                        nrOfDataRowsInFile = CreateXlsxFromQueryToStream(connection, query, setQueryParameters, stream, civicRegNrMapping, new SplitReportSettings
                        {
                            MaxNrOfRowsPerReport = maxNrOfRowsPerReport,
                            NrOfRowsToSkip = nrOfRowsRead
                        });
                        nrOfRowsRead += nrOfDataRowsInFile;
                    }
                    if (nrOfDataRowsInFile == 0)
                        File.Delete(tmpfile);
                }
                if (fileNr > 999)
                    throw new Exception("Hit guard code");

                FastZip f = new FastZip();
                f.CreateZip(targetStream, tempFolder, true, null, null);
            }
            finally
            {
                try { Directory.Delete(tempFolder, true); } catch { /* Ignored */ }
            }
        }

        public class SplitReportSettings
        {
            public int MaxNrOfRowsPerReport { get; set; }
            public int NrOfRowsToSkip { get; set; }
        }

        public int CreateXlsxFromQueryToStream(SqlConnection connection, string query, Action<SqlCommand> setQueryParameters, Stream targetStream, CivicRegNrMapping civicRegNrMapping, SplitReportSettings splitSettings = null)
        {
            var prevCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture; //NPOI is kind of wierd about cultures so just to make sure that doesnt cause problems we always use invariant.
            List<Action> javaportDispose = new List<Action>(); //If only there was a pattern for this in c#....
            try
            {
                XSSFWorkbook workbookActual = new XSSFWorkbook(); //Dispose from this throw NIE ...

                SXSSFWorkbook workbook = new SXSSFWorkbook(workbookActual, 100, false, true);
                javaportDispose.Add(() => workbook?.Dispose());

                Dictionary<string, ICellStyle> styleCache = new Dictionary<string, ICellStyle>();

                int currentRowIndex = 0;
                bool isDate1904 = workbookActual.IsDate1904();
                var columns = new List<Col>();
                ISheet sheet = workbook.CreateSheet("Data");
                bool hasHeaderRow = true;
                bool hasFooterRow = false;

                var extraRowCount = 0;
                if (hasHeaderRow) extraRowCount++;
                if (hasFooterRow) extraRowCount++;

                int nrOfDataRowsRead = 0;
                Read(connection, query, setQueryParameters, r =>
                {
                    currentRowIndex = ReadBeforeFirstRow(r, workbook, isDate1904, styleCache, currentRowIndex, columns, sheet, hasHeaderRow, civicRegNrMapping);
                }, r =>
                {
                    currentRowIndex = ReadRow(r, currentRowIndex, columns, sheet);
                    nrOfDataRowsRead++;
                    return (splitSettings == null || (splitSettings != null && nrOfDataRowsRead < splitSettings.MaxNrOfRowsPerReport));
                }, nrOfRowsToSkip: splitSettings?.NrOfRowsToSkip);

                workbook.Write(targetStream);

                return nrOfDataRowsRead;
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = prevCulture;
                foreach (var p in javaportDispose)
                {
                    try { p?.Invoke(); } catch { /* Ignored */ }
                }
            }
        }

        private static ICellStyle CachedCellStyle(IWorkbook workbook, IDictionary<string, ICellStyle> cache, string dataformat = null, HorizontalAlignment? alignment = null, bool? bold = null)
        {
            string key = $"df={dataformat},ha={alignment},bold={bold}";
            if (!cache.ContainsKey(key))
            {
                ICellStyle style = workbook.CreateCellStyle();
                Lazy<IFont> newFont = new Lazy<IFont>(() => workbook.CreateFont());
                if (dataformat != null)
                {
                    style.DataFormat = workbook.CreateDataFormat().GetFormat(dataformat);
                }
                if (alignment.HasValue)
                {
                    style.Alignment = alignment.Value;
                }
                if (bold.HasValue)
                {
                    var f = newFont.Value;
                    f.IsBold = bold.Value;
                    style.SetFont(f);
                }
                cache[key] = style;
            }
            return cache[key];
        }

        private static void Read(SqlConnection conn, string query, Action<SqlCommand> setQueryParameters, Action<SqlDataReader> beforeFirstRowOfFirstBatchRead, Func<SqlDataReader, bool> onRowRead, int batchSize = 5000, int? nrOfRowsToSkip = null)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = query;
                cmd.CommandTimeout = 60 * 60; //1h 
                setQueryParameters(cmd);
                var fromNrParam = cmd.Parameters.AddWithValue("ntechBatchingFromNr", 0);
                var toNrParam = cmd.Parameters.AddWithValue("ntechBatchingToNr", 0);

                bool isBeforeFirstBatch = true;

                Func<int, bool> handleBatch = fromNr =>
                {
                    fromNrParam.Value = fromNr;
                    toNrParam.Value = fromNr + batchSize - 1;
                    using (var r = cmd.ExecuteReader())
                    {
                        if (isBeforeFirstBatch)
                        {
                            beforeFirstRowOfFirstBatchRead(r);
                            isBeforeFirstBatch = false;
                        }
                        var hasRows = false;
                        while (r.Read())
                        {
                            if (!onRowRead(r))
                                return false;
                            hasRows = true;
                        }
                        return hasRows;
                    }
                };

                int guard = 0;
                var runAgain = true;
                var fn = 1 + (nrOfRowsToSkip ?? 0);
                while (runAgain && guard++ < 1000)
                {
                    runAgain = handleBatch(fn);
                    fn = fn + batchSize;
                }
                if (guard > 9000) throw new Exception("Hit guard code!");
            }
        }

        private class Col
        {
            public Action<SqlDataReader, ICell> SetValue { get; set; }
        }

        private static int ReadRow(SqlDataReader r, int currentRowIndex, List<Col> columns, ISheet sheet)
        {
            var currentRow = sheet.CreateRow(currentRowIndex);
            for (var ii = 0; ii < columns.Count; ii++)
            {
                var i = ii;
                var colCell = currentRow.CreateCell(i);
                columns[i].SetValue(r, colCell);
            }
            return currentRowIndex + 1;
        }

        private static int ReadBeforeFirstRow(SqlDataReader r, IWorkbook workbook, bool isDate1904, Dictionary<string, ICellStyle> styleCache, int currentRowIndex, List<Col> columns, ISheet sheet, bool hasHeaderRow, CivicRegNrMapping civicRegNrMapping)
        {
            IRow headerRow = null;
            if (hasHeaderRow)
            {
                headerRow = sheet.CreateRow(currentRowIndex);
            }

            for (var ii = 0; ii < r.FieldCount; ii++)
            {
                Action<SqlDataReader, ICell> set;

                HorizontalAlignment a = HorizontalAlignment.Right;

                var i = ii;
                var columnName = r.GetName(i);
                var isCivicRegNrMappedCustomerIdColumn = (civicRegNrMapping?.CustomerIdColumns?.Contains(columnName) ?? false);
                if (columnName == "NTechBatchingRowNr")
                {
                    continue;
                }
                var dt = r.GetFieldType(i).Name;
                if (dt == "DateTime")
                {
                    set = (rr, colCell) =>
                    {
                        if (rr.IsDBNull(i))
                            colCell.SetCellValue("");
                        else
                        {
                            //Since the streaming API doesnt implement SetCellValue for dates for some reason we manually do what the original workbooks celltype does.
                            colCell.SetCellValue(DateUtil.GetExcelDate(rr.GetDateTime(i), isDate1904));
                        }
                        colCell.CellStyle = CachedCellStyle(workbook, styleCache,
                            dataformat: "yyyy-MM-dd",
                            alignment: a);
                    };
                }
                else if (dt == "Int32" && !isCivicRegNrMappedCustomerIdColumn)
                {
                    set = (rr, colCell) =>
                    {
                        if (rr.IsDBNull(i))
                            colCell.SetCellValue("");
                        else
                            colCell.SetCellValue(rr.GetInt32(i));
                    };
                }
                else if (dt == "Int64")
                {
                    set = (rr, colCell) =>
                    {
                        if (rr.IsDBNull(i))
                            colCell.SetCellValue("");
                        else
                            colCell.SetCellValue(rr.GetInt64(i));
                    };
                }
                else if (dt == "Boolean")
                {
                    set = (rr, colCell) =>
                    {
                        if (rr.IsDBNull(i))
                            colCell.SetCellValue("");
                        else
                            colCell.SetCellValue(rr.GetBoolean(i));
                    };
                }
                else if (dt == "Decimal")
                {
                    set = (rr, colCell) =>
                    {
                        if (rr.IsDBNull(i))
                            colCell.SetCellValue("");
                        else
                            colCell.SetCellValue((double)rr.GetDecimal(i));
                    };
                }
                else if (dt == "String" || isCivicRegNrMappedCustomerIdColumn)
                {
                    a = HorizontalAlignment.Left;
                    set = (rr, colCell) =>
                    {
                        if (rr.IsDBNull(i))
                            colCell.SetCellValue("");
                        else if (isCivicRegNrMappedCustomerIdColumn)
                            colCell.SetCellValue(civicRegNrMapping.CivicRegNrByCustomerId[rr.GetInt32(i)]);
                        else
                            colCell.SetCellValue(rr.GetString(i));
                    };
                }
                else
                {
                    //Skipped
                    continue;
                }

                columns.Add(new Col { SetValue = set });

                if (hasHeaderRow)
                {
                    var headerCell = headerRow.CreateCell(columns.Count - 1); //Or maybe i + 1
                    headerCell.SetCellValue(r.GetName(i));
                    headerCell.CellStyle = CachedCellStyle(workbook, styleCache,
                        alignment: a,
                        bold: true);
                }
            }
            if (hasHeaderRow)
            {
                sheet.CreateFreezePane(0, currentRowIndex + 1, 0, currentRowIndex + 1);
                currentRowIndex++;
            }

            return currentRowIndex;
        }
    }
}
