using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;

namespace nDocument.Code.Excel
{
    public class ExcelParser
    {
        private readonly Func<bool, string> booleanFormatter;
        private readonly Func<DateTime, string> dateFormatter;
        private readonly Func<double, string> numberFormatter;
        private readonly bool leavePercentAsFraction;

        public ExcelParser(bool leavePercentAsFraction) : this(
            x => x ? "x" : "",
            x => x.ToString("yyyy-MM-dd"),
            x => Math.Round((decimal)x, 4).ToString("0.####", CultureInfo.InvariantCulture),
            leavePercentAsFraction)
        {

        }

        public ExcelParser(
            Func<bool, string> booleanFormatter,
            Func<DateTime, string> dateFormatter,
            Func<double, string> numberFormatter,
            bool leavePercentAsFraction)
        {
            this.booleanFormatter = booleanFormatter;
            this.dateFormatter = dateFormatter;
            this.numberFormatter = numberFormatter;
            this.leavePercentAsFraction = leavePercentAsFraction;
        }

        private Tuple<bool, Dictionary<string, List<List<string>>>, Tuple<string, string>> Ok(Dictionary<string, List<List<string>>> d)
        {
            return Tuple.Create(true, d, (Tuple<string, string>)null);
        }

        private Tuple<bool, Dictionary<string, List<List<string>>>, Tuple<string, string>> Error(string errorMessage, string errorCode)
        {
            return Tuple.Create(false, (Dictionary<string, List<List<string>>>)null, Tuple.Create(errorMessage, errorCode));
        }

        public Tuple<bool, Dictionary<string, List<List<string>>>, Tuple<string, string>> ParseExcelFile(string fileName, Stream fileContent)
        {
            if ((fileName ?? "").EndsWith(".xlsx"))
            {
                return ParseExcelFile(new NPOI.XSSF.UserModel.XSSFWorkbook(fileContent));
            }
            else if ((fileName ?? "").EndsWith(".xls"))
            {
                return ParseExcelFile(new NPOI.HSSF.UserModel.HSSFWorkbook(fileContent));
            }
            else
            {
                return Error("Invalid file type. Must be .xls or .xlsx", "invalidFileType");
            }
        }

        public Tuple<bool, Dictionary<string, List<List<string>>>, Tuple<string, string>> ParseExcelFile(IWorkbook wb)
        {
            var sheets = new Dictionary<string, List<List<string>>>();
            for (var i = 0; i < wb.NumberOfSheets; i++)
            {
                var sheet = wb.GetSheet(wb.GetSheetName(i));
                var rows = new List<List<string>>(sheet.LastRowNum + 1);
                var nrOfColumns = sheet.GetRow(0)?.Cells?.Count ?? 0;

                for (var rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var r = sheet.GetRow(rowIndex);
                    if (r != null)
                    {
                        var colCount = Math.Max(nrOfColumns, r.LastCellNum);
                        var row = new List<string>(colCount);

                        for (var columnIndex = 0; columnIndex < colCount; columnIndex++)
                        {
                            var c = r.GetCell(columnIndex, MissingCellPolicy.CREATE_NULL_AS_BLANK);
                            row.Add(GetCellValue(c));
                        }
                        rows.Add(row);
                    }
                }
                sheets[sheet.SheetName] = rows;
            }
            return Ok(sheets);
        }

        private bool IsCellType(ICell c, CellType t)
        {
            return c.CellType == t || (c.CellType == CellType.Formula && c.CachedFormulaResultType == t);
        }

        private string GetCellValue(ICell c)
        {
            if (IsCellType(c, CellType.Boolean))
                return booleanFormatter(c.BooleanCellValue);
            else if (IsCellType(c, CellType.String))
                return c.StringCellValue;
            else if (IsCellType(c, CellType.Numeric))
            {
                if (NPOI.HSSF.UserModel.HSSFDateUtil.IsCellDateFormatted(c))
                {
                    //NOTE: So the threading handling in NPOI is broken so timezone nullpointers sometimes more or less randomly. 
                    //      The way it's set when calling without a timezone (which is what our code does) is to read TimeZone.CurrentTimeZone put it in a ThreadLocal
                    //      so we just use that directly always to dodge the threading problem.
                    return dateFormatter(DateUtil.GetJavaDate(c.NumericCellValue, TimeZoneInfo.FindSystemTimeZoneById(TimeZone.CurrentTimeZone.StandardName)));
                }
                else
                {
                    if (!leavePercentAsFraction && (c?.CellStyle?.GetDataFormatString()?.Contains("%") ?? false))
                    {
                        return numberFormatter(c.NumericCellValue * 100d);
                    }
                    else
                    {
                        return numberFormatter(c.NumericCellValue);
                    }
                }
            }
            else
                return c.StringCellValue;
        }
    }
}