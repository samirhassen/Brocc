using nDocument.Code.Excel;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using NTech.Legacy.Module.Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace nDocument.Controllers
{
    public class ExcelController : Controller
    {
        protected const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public class ExcelRequest
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
                public int? SumNrOfDecimals { get; set; } //Can use a diffrent number than the rest of the columns. If missing will use the standard NrOfDecimals
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
                public string BackgroundColors { get; set; }
                public IDictionary<string, string> ColorValues { get; set; }
            }
        }

        private static ICellStyle CachedCellStyle(IWorkbook workbook, IDictionary<string, ICellStyle> cache, string dataformat = null, HorizontalAlignment? alignment = null, bool? bold = null, string backgroundColor = null, string compareCellWith = null)
        {
            string key = $"df={dataformat},ha={alignment},bold={bold},backgroundColor={backgroundColor}";
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
                if (!string.IsNullOrWhiteSpace(backgroundColor))
                {
                    style.FillForegroundColor = IndexedColors.ValueOf(backgroundColor).Index;
                    style.FillPattern = FillPattern.SolidForeground;
                }

                cache[key] = style;
            }
            return cache[key];
        }

        //Sample request
        //var r = new Random();
        //request = new ExcelRequest
        //    {
        //        Sheets = new ExcelRequest.Sheet[]
        //        {
        //            new ExcelRequest.Sheet
        //            {
        //                AutoSizeColumns = true,
        //                Title = "Test 1 ÅÄÖ",
        //                Columns = new ExcelRequest.Column[]
        //                {
        //                    new ExcelRequest.Column
        //                    {
        //                        HeaderText = "Text",
        //                        IsText = true
        //                    },
        //                    new ExcelRequest.Column
        //                    {
        //                        HeaderText = "Date",
        //                        IsDate = true //NOTE: Date only. May also allow date and time in the future
        //                    },
        //                    new ExcelRequest.Column
        //                    {
        //                        HeaderText = "Integer",
        //                        IsNumber = true,
        //                        NrOfDecimals = 0,
        //                        IncludeSum = true
        //                    },
        //                    new ExcelRequest.Column
        //                    {
        //                        HeaderText = "Decimal",
        //                        IsNumber = true,
        //                        NrOfDecimals = 2,
        //                        IncludeSum = true
        //                    },
        //                    new ExcelRequest.Column
        //                    {
        //                        HeaderText = "Percent",
        //                        IsPercent = true,
        //                        NrOfDecimals = 3
        //                    },
        //                },
        //                Cells = Enumerable.Range(1, 10000).Select(x => new string[]
        //                {
        //                    "Test åäö 123!!!!",
        //                    DateTimeOffset.Now.AddMinutes(-r.Next(1, 10000)).ToString("o"),
        //                    (r.NextDouble() * r.Next(100, 1000000)).ToString(CultureInfo.InvariantCulture),
        //                    (r.NextDouble() * r.Next(100, 1000000)).ToString(CultureInfo.InvariantCulture),
        //                   r.NextDouble().ToString(CultureInfo.InvariantCulture)
        //                }).ToArray()
        //            }
        //        }
        //    };

        [AllowAnonymous]
        [HttpPost]
        public ActionResult CreateXlsx()
        {
            ExcelRequest request;
            Request.InputStream.Position = 0;
            using (var r = new StreamReader(Request.InputStream))
            {
                request = JsonConvert.DeserializeObject<ExcelRequest>(r.ReadToEnd());
            }
            var tmp = CreateXlsxToTempFile(request);
            return new DeleteTempFileFileStreamResult(new FileStream(tmp, FileMode.Open, FileAccess.Read), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", tmp);
        }

        private ActionResult ParseExcel(Stream input, string filename, bool leavePercentAsFraction)
        {
            var r = new Code.Excel.ExcelParser(leavePercentAsFraction).ParseExcelFile(filename, input);
            object d;
            if (!r.Item1)
                d = new
                {
                    errorMessage = r.Item3.Item1,
                    errorCode = r.Item3.Item2
                };
            else
                d = new
                {
                    sheets = r.Item2
                };
            return new NTech.Services.Infrastructure.NTechWs.RawJsonActionResult
            {
                JsonData = JsonConvert.SerializeObject(d),
                CustomHttpStatusCode = r.Item1 ? new int?() : 400,
                CustomStatusDescription = r.Item1 ? (string)null : r.Item3.Item1
            };
        }

        [HttpPost]
        public ActionResult ParseDataUrlExcelFile(string fileAsDataUrl, string fileName, bool leavePercentAsFraction)
        {
            Files.TryParseDataUrl(fileAsDataUrl, out var mimeType, out var bytes);
            return ParseExcel(new MemoryStream(bytes), fileName, leavePercentAsFraction);
        }

        private static int? GetReportRowsInsertionRow(ISheet sheet)
        {
            var i = Enumerable
                    .Range(sheet.FirstRowNum, sheet.LastRowNum - sheet.FirstRowNum + 1)
                    .Select(rowIndex => new { RowIndex = rowIndex, Row = sheet.GetRow(rowIndex) })
                    .Where(x => x.Row != null)
                    .SelectMany(x => Enumerable.Range(x.Row.FirstCellNum, x.Row.LastCellNum - x.Row.FirstCellNum + 1).Select(cellIndex => new
                    {
                        x.RowIndex,
                        ColumnIndex = cellIndex,
                        StringValue = x.Row.GetCell(cellIndex, MissingCellPolicy.RETURN_BLANK_AS_NULL)?.StringCellValue
                    }))
                    .Where(x => x.StringValue == "[[[REPORT_ROWS]]]")
                    .Select(x => Tuple.Create(x.RowIndex, x.ColumnIndex))
                    .SingleOrDefault();
            if (i != null)
            {
                if (i.Item2 != 0)
                    throw new Exception("Column for REPORT_ROWS must be 0");
                return i.Item1;
            }
            else
                return null;
        }

        private static void FixRequest(ExcelRequest request)
        {
            //Never versions of NPOI throw errors when this is too long where older versions just clipped the title. This preserves the old behaviour
            if (request?.Sheets != null)
            {
                foreach (var sheet in request.Sheets)
                {
                    if (sheet.Title != null)
                        sheet.Title = sheet.Title.ClipRight(30);
                }
            }
        }

        private static string CreateXlsxToTempFile(ExcelRequest request)
        {
            FixRequest(request);
            XSSFWorkbook templateWorkbook = null;
            bool usesTemplate = false;
            if (!string.IsNullOrWhiteSpace(request?.TemplateXlsxDocumentBytesAsBase64))
            {
                usesTemplate = true;
                templateWorkbook = new XSSFWorkbook(new MemoryStream(Convert.FromBase64String(request.TemplateXlsxDocumentBytesAsBase64)));
            }
            var autoSizeService = new ExcelAutosizeService(NTechEnvironmentLegacy.SharedInstance);
            foreach (var sheet in request.Sheets)
            {
                //Fixes a serialization bug that causes this exact thing to serialize as null
                if (sheet.Cells == null)
                    sheet.Cells = new ExcelRequest.Value[][] { };
            }
            var prevCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture; //NPOI is kind of wierd about cultures so just to make sure that doesnt cause problems we always use invariant.
            try
            {
                XSSFWorkbook workbook;
                if (templateWorkbook == null)
                {
                    workbook = new XSSFWorkbook();
                }
                else
                {
                    if (templateWorkbook.NumberOfSheets < request.Sheets.Length)
                        throw new Exception("When using a template it must have at least as many sheets as the request");

                    workbook = templateWorkbook;
                }

                Dictionary<string, ICellStyle> styleCache = new Dictionary<string, ICellStyle>();
                Func<ExcelRequest.StyleData, HorizontalAlignment> alignment = c =>
                {
                    if (c.IsNumber)
                    {
                        return HorizontalAlignment.Right;
                    }
                    else if (c.IsDate)
                    {
                        return HorizontalAlignment.Right;
                    }
                    else if (c.IsPercent)
                    {
                        return HorizontalAlignment.Right;
                    }
                    else
                    {
                        return HorizontalAlignment.Left;
                    }
                };

                Func<string, ICell, string> initFormula = (formula, c2) =>
                {
                    //You can use the special macro [[CELL]] to refer to the cell that would normally contain the sum. Otherwise is a normal excel formula.
                    //Example. Sum the three sums to the left of this cell: SUM(OFFSET([[CELL]],0,-3,1,3))
                    //Beware that you need to use us english names and syntax which means commas instead of semicolons and english function names
                    //The user will still see localized names
                    return formula.Replace("[[CELL]]", new CellReference(c2.RowIndex, c2.ColumnIndex, true, true).FormatAsString());
                };

                foreach (var si in request.Sheets.Select((x, i) => new { SheetItem = x, SheetIndex = i }))
                {
                    var s = si.SheetItem;
                    var sheetIndex = si.SheetIndex;

                    ISheet sheet = workbook.NumberOfSheets > sheetIndex ? workbook.GetSheetAt(sheetIndex) : workbook.CreateSheet(s.Title);

                    var insertionRow = usesTemplate ? GetReportRowsInsertionRow(sheet) : null;

                    bool hasHeaderRow = s.Columns.Any(x => !string.IsNullOrWhiteSpace(x.HeaderText));
                    bool hasFooterRow = s.Cells.Length > 0 && s.Columns.Any(x => x.IncludeSum);

                    var extraRowCount = 0;
                    if (hasHeaderRow) extraRowCount++;
                    if (hasFooterRow) extraRowCount++;

                    int currentRowIndex;
                    if (insertionRow.HasValue)
                    {
                        if (insertionRow.Value + 1 <= sheet.LastRowNum)
                        {
                            sheet.ShiftRows(insertionRow.Value + 1, sheet.LastRowNum, s.Cells.Length + extraRowCount - 1, true, true);
                        }
                        sheet.RemoveRow(sheet.GetRow(insertionRow.Value));//Remove the template row that got copied down
                        currentRowIndex = insertionRow.Value;
                    }
                    else
                    {
                        currentRowIndex = 0;
                    }

                    if (hasHeaderRow)
                    {
                        hasHeaderRow = true;
                        var headerRow = sheet.CreateRow(currentRowIndex);
                        foreach (var cell in s.Columns.Select((x, i) => new { columnIndex = i, col = x }))
                        {
                            var colCell = headerRow.CreateCell(cell.columnIndex);
                            colCell.SetCellValue(cell.col.HeaderText ?? ("Column " + cell.columnIndex));
                            colCell.CellStyle = CachedCellStyle(workbook, styleCache,
                                alignment: alignment(cell.col),
                                bold: true);
                        }
                        sheet.CreateFreezePane(0, currentRowIndex + 1, 0, currentRowIndex + 1);
                        currentRowIndex++;
                    }

                    foreach (var row in s.Cells)
                    {
                        var currentRow = sheet.CreateRow(currentRowIndex);
                        foreach (var cell in s.Columns.Select((x, i) => new { index = i, style = x }))
                        {
                            var background = "";
                            string fieldNumber1 = "", fieldNumber2 = "";
                            if (request.ColorCell == true && request.Sheets[sheetIndex].Columns[cell.index].ColorValues != null)
                            {
                                request.Sheets[sheetIndex].Columns[cell.index].ColorValues.TryGetValue(cell.index.ToString(), out fieldNumber2);
                                if (request.Sheets[sheetIndex].Columns[cell.index].ColorValues.ContainsKey(cell.index.ToString()))
                                    fieldNumber1 = cell.index.ToString();
                            }
                            var colCell = currentRow.CreateCell(cell.index);
                            var cellStyle = row[cell.index].Style ?? cell.style;

                            if (cellStyle.IsDate)
                            {
                                if (row[cell.index].D.HasValue)
                                {
                                    if (request.ColorCell == true)
                                    {
                                        if (!string.IsNullOrWhiteSpace(cellStyle.BackgroundColors) && cellStyle.BackgroundColors.Contains(",") && !string.IsNullOrWhiteSpace(fieldNumber2) && !string.IsNullOrWhiteSpace(fieldNumber1) && row[int.Parse(fieldNumber2)].D != row[int.Parse(fieldNumber1)].D)
                                            background = cellStyle.BackgroundColors.Split(',')[1];
                                        if (!string.IsNullOrWhiteSpace(cellStyle.BackgroundColors) && cellStyle.BackgroundColors.Contains(",") && !string.IsNullOrWhiteSpace(fieldNumber2) && !string.IsNullOrWhiteSpace(fieldNumber1) && row[int.Parse(fieldNumber2)].D == row[int.Parse(fieldNumber1)].D)
                                            background = cellStyle.BackgroundColors.Split(',')[0];
                                    }
                                    colCell.SetCellValue(row[cell.index].D.Value);
                                }
                                else
                                    colCell.SetCellValue("");
                                colCell.CellStyle = CachedCellStyle(workbook, styleCache,
                                    dataformat: "yyyy-MM-dd" + (cellStyle.IncludeTime ? " HH:mm" : ""),
                                    alignment: alignment(cellStyle), backgroundColor: background);
                            }
                            else if (cellStyle.IsNumber || cellStyle.IsNumberFormula)
                            {
                                if (cellStyle.IsNumberFormula)
                                {
                                    colCell.SetCellFormula(initFormula(row[cell.index].S, colCell));
                                }
                                else
                                {
                                    if (row[cell.index].V.HasValue)
                                    {
                                        if (request.ColorCell == true)
                                        {
                                            if (!string.IsNullOrWhiteSpace(cellStyle.BackgroundColors) && cellStyle.BackgroundColors.Contains(",") && !string.IsNullOrWhiteSpace(fieldNumber2) && !string.IsNullOrWhiteSpace(fieldNumber1) && row[int.Parse(fieldNumber2)].V != row[int.Parse(fieldNumber1)].V)
                                                background = cellStyle.BackgroundColors.Split(',')[1];
                                            if (!string.IsNullOrWhiteSpace(cellStyle.BackgroundColors) && cellStyle.BackgroundColors.Contains(",") && !string.IsNullOrWhiteSpace(fieldNumber2) && !string.IsNullOrWhiteSpace(fieldNumber1) && row[int.Parse(fieldNumber2)].V == row[int.Parse(fieldNumber1)].V)
                                                background = cellStyle.BackgroundColors.Split(',')[0];
                                        }
                                        colCell.SetCellValue((double)row[cell.index].V);
                                    }
                                    else
                                        colCell.SetCellValue("");
                                }
                                var nrOfDecimals = cellStyle.NrOfDecimals ?? (cellStyle.IsNumericId ? 0 : 2);
                                colCell.CellStyle = CachedCellStyle(workbook, styleCache,
                                    dataformat: (cellStyle.IsNumericId ? "0" : "#,##0") + (nrOfDecimals > 0 ? "." + new string('0', nrOfDecimals) : ""),
                                    alignment: alignment(cellStyle), backgroundColor: background);
                            }
                            else if (cellStyle.IsPercent)
                            {
                                if (row[cell.index].V.HasValue)
                                {
                                    if (request.ColorCell == true)
                                    {
                                        if (!string.IsNullOrWhiteSpace(cellStyle.BackgroundColors) && cellStyle.BackgroundColors.Contains(",") && !string.IsNullOrWhiteSpace(fieldNumber2) && !string.IsNullOrWhiteSpace(fieldNumber1) && row[int.Parse(fieldNumber2)].V != row[int.Parse(fieldNumber1)].V)
                                            background = cellStyle.BackgroundColors.Split(',')[1];
                                        if (!string.IsNullOrWhiteSpace(cellStyle.BackgroundColors) && cellStyle.BackgroundColors.Contains(",") && !string.IsNullOrWhiteSpace(fieldNumber2) && !string.IsNullOrWhiteSpace(fieldNumber1) && row[int.Parse(fieldNumber2)].V == row[int.Parse(fieldNumber1)].V)
                                            background = cellStyle.BackgroundColors.Split(',')[0];
                                    }
                                    colCell.SetCellValue((double)row[cell.index].V);
                                }
                                else
                                    colCell.SetCellValue("");
                                colCell.CellStyle = CachedCellStyle(workbook, styleCache,
                                    dataformat: ("#,##0" + ((cellStyle.NrOfDecimals ?? 2) > 0 ? "." + new string('0', (cellStyle.NrOfDecimals ?? 2)) : "")) + "%",
                                    alignment: alignment(cellStyle), backgroundColor: background);
                            }
                            else
                            {
                                if (request.ColorCell == true)
                                {
                                    if (!string.IsNullOrWhiteSpace(cellStyle.BackgroundColors) && cellStyle.BackgroundColors.Contains(",") && !string.IsNullOrWhiteSpace(fieldNumber2) && !string.IsNullOrWhiteSpace(fieldNumber1) && row[int.Parse(fieldNumber2)].S != row[int.Parse(fieldNumber1)].S)
                                        background = cellStyle.BackgroundColors.Split(',')[1];
                                    if (!string.IsNullOrWhiteSpace(cellStyle.BackgroundColors) && cellStyle.BackgroundColors.Contains(",") && !string.IsNullOrWhiteSpace(fieldNumber2) && !string.IsNullOrWhiteSpace(fieldNumber1) && row[int.Parse(fieldNumber2)].S == row[int.Parse(fieldNumber1)].S)
                                        background = cellStyle.BackgroundColors.Split(',')[0];
                                }
                                colCell.SetCellValue(row[cell.index].S ?? "");
                                colCell.CellStyle = CachedCellStyle(workbook, styleCache,
                                    alignment: alignment(cellStyle), backgroundColor: background);
                            }
                        }
                        currentRowIndex++;
                    }

                    if (s.Cells.Length > 0 && s.Columns.Any(x => x.IncludeSum))
                    {
                        var footerRow = sheet.CreateRow(currentRowIndex);

                        foreach (var column in s.Columns.Select((x, i) => new { index = i, col = x }))
                        {
                            var colCell = footerRow.CreateCell(column.index);

                            if (!string.IsNullOrWhiteSpace(column.col.CustomSum) || (column.col.IncludeSum && (column.col.IsNumber || column.col.IsNumberFormula)))
                            {
                                if (!string.IsNullOrWhiteSpace(column.col.CustomSum))
                                {
                                    colCell.SetCellFormula(initFormula(column.col.CustomSum, colCell));
                                }
                                else
                                {
                                    var firstColumnReference = new CellReference(hasHeaderRow ? 1 : 0, column.index, true, true);
                                    var lastColumnReference = new CellReference(currentRowIndex - 1, column.index, true, true);
                                    colCell.SetCellFormula($"SUM({firstColumnReference.FormatAsString()}:{lastColumnReference.FormatAsString()})");
                                }
                                var nrOfDecimals = column.col.SumNrOfDecimals ?? column.col.NrOfDecimals ?? 2;
                                colCell.CellStyle = CachedCellStyle(workbook, styleCache,
                                    dataformat: "#,##0" + ((nrOfDecimals) > 0 ? "." + new string('0', (nrOfDecimals)) : ""),
                                    alignment: alignment(column.col),
                                    bold: true);
                            }
                            else
                            {
                                colCell.SetCellValue("");
                                colCell.CellStyle = CachedCellStyle(workbook, styleCache,
                                    alignment: alignment(column.col));
                            }
                        }

                        currentRowIndex++;
                    }

                    autoSizeService.AutoSizeColumns(s, sheet);
                }

                var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xlsx");
                using (var stream = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write))
                {
                    workbook.Write(stream);
                }

                return tmp;
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = prevCulture;
            }
        }

        [HttpPost]
        public ActionResult CreateXlsxToArchive()
        {
            //These two could just be regular parameters if the built in json deserializer wasnt so terrible
            ExcelRequest request;
            string archiveFileName;
            Request.InputStream.Position = 0;
            using (var r = new StreamReader(Request.InputStream))
            {
                var requestString = r.ReadToEnd();
                var rr = JsonConvert.DeserializeAnonymousType(requestString, new { request = (ExcelRequest)null, archiveFileName = (string)null });
                request = rr?.request;
                archiveFileName = rr?.archiveFileName;
            }

            var tmp = CreateXlsxToTempFile(request);
            try
            {
                string key;
                string errMsg;
                var p = Code.Archive.ArchiveProviderFactory.Create();
                if (!p.TryStore(System.IO.File.ReadAllBytes(tmp), XlsxContentType, archiveFileName, out key, out errMsg))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, errMsg);
                }
                else
                {
                    return Json(new { key = key });
                }
            }
            finally
            {
                try
                {
                    System.IO.File.Delete(tmp);
                }
                catch
                {
                    /* Ignored */
                }
            }
        }
    }

    public class DeleteTempFileFileStreamResult : FileStreamResult
    {
        protected string tempFileName;

        public DeleteTempFileFileStreamResult(Stream fileStream, string contentType, string tempFileName) : base(fileStream, contentType)
        {
            this.tempFileName = tempFileName;
        }

        protected override void WriteFile(HttpResponseBase response)
        {
            try
            {
                base.WriteFile(response);
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(tempFileName))
                        System.IO.File.Delete(tempFileName);
                }
                catch { /*Ignored*/ }
            }
        }
    }
}