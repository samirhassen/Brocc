using nDataWarehouse.Code;
using nDataWarehouse.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;

namespace nCredit.WebserviceMethods
{
    public class VintageReportMethod : FileStreamWebserviceMethod<VintageReportRequest>
    {
        public override string Path => "Reports/Vintage/Get";

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, VintageReportRequest request)
        {
            var s = new VintageReportService(() => DateTimeOffset.Now);

            VintageReportResult data;
            try
            {
                data = s.FetchVintageReportData(request);
            }
            catch (ServiceException ex)
            {
                if (ex.IsUserSafeException)
                    return Error(ex.Message, httpStatusCode: 400, errorCode: ex.ErrorCode);
                else
                    throw;
            }

            var cols = DocumentClientExcelRequest.CreateDynamicColumnList(data.DataRows);

            cols.Add(data.DataRows.Col(x => x.RowId, ExcelType.Date, "RowId"));
            cols.Add(data.DataRows.Col(x => x.InitialValue, ExcelType.Number, "InitialValue", nrOfDecimals: request.CellValueIsCount == "true" ? 0 : 2));
            for (var i = 0; i < data.ColumnCount; i++)
            {
                var localI = i;
                cols.Add(data.DataRows.Col(x => x.ColumnValues[localI], request.ShowPercent == "true" ? ExcelType.Percent : ExcelType.Number, $"{localI + 1}", nrOfDecimals: request.CellValueIsCount == "true" ? 0 : 2));
            }

            var sheets = new List<DocumentClientExcelRequest.Sheet>();
            sheets.Add(new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = "Vintage report",
            }
                .SetColumnsAndData(data.DataRows, cols.ToArray()));

            Func<string, bool> hasValue = x => !string.IsNullOrWhiteSpace(x);
            List<Tuple<string, string>> filters = new List<Tuple<string, string>>();
            Action<string, string> add = (x, y) => filters.Add(Tuple.Create(x, y));
            add("X scale", request.AxisScaleX);
            add("Y scale", request.AxisScaleY);
            if (request.ExcludeCapitalBalance == "true")
            {
                add("Balance", "Exclude");
            }
            else if (hasValue(request.OverdueDaysFrom) || hasValue(request.OverdueDaysTo))
            {
                add("Balance", $"OverdueDays between {(hasValue(request.OverdueDaysFrom) ? request.OverdueDaysFrom : "any")} and {(hasValue(request.OverdueDaysTo) ? request.OverdueDaysTo : "any")}");
            }
            else if (hasValue(request.OverdueMonthsFrom) || hasValue(request.OverdueMonthsTo))
            {
                add("Balance", $"OverdueMonths between {(hasValue(request.OverdueMonthsFrom) ? request.OverdueMonthsFrom : "any")} and {(hasValue(request.OverdueMonthsTo) ? request.OverdueMonthsTo : "any")}");
            }
            else
            {
                add("Balance", "OverdueMonths between 0 and any");
            }
            add("Debtcol. balance", request.AccumulateDebtCollectionBalance == "true" ? "0+" : (request.IncludeDebtCollectionBalance == "true" ? "0" : "Excluded"));

            add("Value type", request.CellValueIsCount == "true" ? "Count" : "Value");

            add("Additional loans", "Added to first month");
            if (hasValue(request.AxisYFrom) || hasValue(request.AxisYTo))
            {
                add("Y period", $"between {(hasValue(request.AxisYFrom) ? request.AxisYFrom : "any")} and {(hasValue(request.AxisYTo) ? request.AxisYTo : "any")}");
            }
            if (hasValue(request.ProviderName))
            {
                add("ProviderName", request.ProviderName);
            }
            if (hasValue(request.RiskGroup))
            {
                add("RiskGroup", request.RiskGroup);
            }

            add("Format", request.ShowPercent == "true" ? "Percent" : "Values");
            if (!string.IsNullOrWhiteSpace(request.TreatNotificationsAsClosedMaxBalance))
                add("Low balance limit", request.TreatNotificationsAsClosedMaxBalance);

            if (!string.IsNullOrWhiteSpace(request.CreditNr))
                add("Single credit", request.CreditNr);

            sheets.Add(new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = "Filter"
            }.SetColumnsAndData(filters,
                filters.Col(x => x.Item1, ExcelType.Text, "Name"),
                filters.Col(x => x.Item2, ExcelType.Text, "Value")));

            if (request.IncludeDetails == "true")
            {
                sheets.Add(new DocumentClientExcelRequest.Sheet
                {
                    Title = "Details",
                }
                .SetColumnsAndData(data.DetailRows,
                    data.DetailRows.Col(x => x.RowId, ExcelType.Date, "RowId"),
                    data.DetailRows.Col(x => x.InitialValue, ExcelType.Number, "InitialValue", nrOfDecimals: request.CellValueIsCount == "true" ? 0 : 2),
                    data.DetailRows.Col(x => x.ColumnId, ExcelType.Date, "ColumnId"),
                    data.DetailRows.Col(x => x.ItemId, ExcelType.Text, "ItemId"),
                    data.DetailRows.Col(x => x.OverdueDays, ExcelType.Number, "OverdueDays", nrOfDecimals: 0),
                    data.DetailRows.Col(x => x.OverdueMonths, ExcelType.Number, "OverdueMonths", nrOfDecimals: 0),
                    data.DetailRows.Col(x => x.ProviderName, ExcelType.Text, "ProviderName"),
                    data.DetailRows.Col(x => x.RiskGroup, ExcelType.Text, "RiskGroup"),
                    data.DetailRows.Col(x => x.Value, request.ShowPercent == "true" ? ExcelType.Percent : ExcelType.Number, "Value", nrOfDecimals: request.CellValueIsCount == "true" ? 0 : 2)));
            }

            var excelRequest = new DocumentClientExcelRequest
            {
                Sheets = sheets.ToArray()
            };

            var dc = new DocumentClient();
            var stream = dc.CreateXlsx(excelRequest, TimeSpan.FromHours(1));
            return this.ExcelFile(stream);
        }
    }
}