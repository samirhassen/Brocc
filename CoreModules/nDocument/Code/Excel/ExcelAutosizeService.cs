using NPOI.SS.UserModel;
using NTech.Banking.Conversion;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Linq;
using static nDocument.Controllers.ExcelController;

namespace nDocument.Code.Excel
{
    public class ExcelAutosizeService
    {
        private Lazy<AutoSizeMode> mode;

        public ExcelAutosizeService(INTechEnvironment environment)
        {
            mode = new Lazy<AutoSizeMode>(() =>
            {
                return Enums.Parse<AutoSizeMode>(environment.OptionalSetting("ntech.excel.autosizemode") ?? AutoSizeMode.Fast.ToString(), ignoreCase: true) ?? AutoSizeMode.Fast;
            });
        }

        public void AutoSizeColumns(ExcelRequest.Sheet sheetDefinition, ISheet actualSheet)
        {            
            if (sheetDefinition.AutoSizeColumns && mode.Value != AutoSizeMode.Disabled)
            {
                foreach (var columnData in sheetDefinition.Columns.Select((column, columnIndex) => new { column, columnIndex }))
                {
                    var column = columnData.column;
                    var columnIndex = columnData.columnIndex;
                    if (mode.Value == AutoSizeMode.Standard)
                    {
                        actualSheet.AutoSizeColumn(columnIndex);
                    }
                    else if (mode.Value == AutoSizeMode.Fast)
                    {
                        int rowBasedAutoSizeWidth = 0;
                        if(sheetDefinition.Cells.Length > 0) //If there are any rows
                        {
                            var columnCells = Enumerable.Range(0, sheetDefinition.Cells.Length).Select(rowIndex => sheetDefinition.Cells[rowIndex][columnIndex]).ToList();
                            rowBasedAutoSizeWidth = columnCells.Select(x => GetAutosizeWidth(x)).Max();
                        }
                        var headerAutoSizeWidth = GetAutoSizeWidthFromCharCount((column.HeaderText ?? "").Length);
                        var autoSizeWidth = Math.Max(rowBasedAutoSizeWidth, headerAutoSizeWidth);
                        if(autoSizeWidth > 0)
                            actualSheet.SetColumnWidth(columnIndex, autoSizeWidth);
                    }
                    else
                        throw new NotImplementedException();
                }
            }
        }

        private int GetAutosizeWidth(ExcelRequest.Value value)
        {
            int charCount;
            if (value.S != null)
                charCount = value.S.Length;
            else if (value.D.HasValue)
                charCount = 10; //NOTE: Respect style would be even better. Current based on yyyy-MM-dd
            else if (value.V.HasValue)
                charCount = value.V.Value.ToString("N2").Length + 2; //NOTE: Respect style would be even better
            else
                charCount = 0;            
            return GetAutoSizeWidthFromCharCount(charCount);
        }

        /*
         * 1.14388 is a max character width of the "Serif" font and 256 font units. From https://stackoverflow.com/questions/18983203/how-to-speed-up-autosizing-columns-in-apache-poi
         * This turned out a bit small for us so upped it slightly to 1.3
        */
        private int GetAutoSizeWidthFromCharCount(int charCount) => ((int)(charCount * 1.3)) * 256;

        private enum AutoSizeMode
        {
            /// <summary>
            /// Use a fast but possibly not that accurate heuristic
            /// </summary>
            Fast,
            /// <summary>
            /// Uses NPOIs builtin super slow version
            /// </summary>
            Standard,
            /// <summary>
            /// No autosize at all
            /// </summary>
            Disabled
        }
    }
}