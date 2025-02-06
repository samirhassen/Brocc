using nDataWarehouse.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nDataWarehouse.Controllers
{
    [RoutePrefix("Api")]
    [NTechApi]
    public class DwController : NController
    {
        public class MergeDimensionRequest
        {
            public string DimensionName { get; set; }
            public List<ExpandoObject> Values { get; set; }
        }

        public class MergeFactRequest
        {
            public string FactName { get; set; }
            public List<ExpandoObject> Values { get; set; }
        }

        [HttpPost]
        [Route("MergeDimension")]
        public ActionResult MergeDimension()
        {
            MergeDimensionRequest request;
            Request.InputStream.Position = 0;
            using (var reader = new StreamReader(Request.InputStream))
            {
                request = JsonConvert.DeserializeObject<MergeDimensionRequest>(reader.ReadToEnd());
            }

            if (request == null || string.IsNullOrWhiteSpace(request.DimensionName) || request.Values == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var tableName = $"Dimension_{request.DimensionName}";

            var support = new DwSupport();
            string errorMessage;
            if (!support.TryMergeTable(tableName, request.Values, out errorMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, errorMessage);
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        [HttpPost]
        [Route("MergeFact")]
        public ActionResult MergeFact()
        {
            MergeFactRequest request;
            Request.InputStream.Position = 0;
            using (var reader = new StreamReader(Request.InputStream))
            {
                request = JsonConvert.DeserializeObject<MergeFactRequest>(reader.ReadToEnd());
            }
            if (request == null || string.IsNullOrWhiteSpace(request.FactName) || request.Values == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var tableName = $"Fact_{request.FactName}";
            var support = new DwSupport();
            if (!support.TryMergeTable(tableName, request.Values, out string errorMessage))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, errorMessage);
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        [HttpPost]
        [Route("FetchReportData")]
        public ActionResult FetchReportData()
        {
            if (!Request.ContentType.Contains("application/json"))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid request. Can only accept json requests");

            Request.InputStream.Position = 0;
            JObject request;
            using (var r = new StreamReader(Request.InputStream))
            {
                request = JObject.Parse(r.ReadToEnd());
            }

            var rows = new List<dynamic>(1000);

            Action<SqlDataReader, ReportMetadata> beforeFirstRowOfFirstBatch = (reader, reportMetadata) =>
            {

            };
            Action<SqlDataReader, ReportMetadata> onRowRead = (reader, reportMetadata) =>
            {
                var expandoObject = new ExpandoObject() as IDictionary<string, object>;
                rows.Add(expandoObject);
                for (int i = 0; i < reader.FieldCount; ++i)
                {
                    var localIndex = i;
                    var fieldName = reader.GetName(i);
                    if (reportMetadata.UseRowNrDataBatching && fieldName == "BatchingRowNr")
                    {
                        continue;
                    }
                    expandoObject[reader.GetName(i)] = reader.IsDBNull(localIndex) ? null : reader.GetValue(localIndex);
                }
            };
            if (!TryFetchReportData(request, out string failedMessage, beforeFirstRowOfFirstBatch, onRowRead))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            return Json2(rows);
        }

        private class ReportMetadata
        {
            public string ReportName { get; set; }
            public bool UseRowNrDataBatching { get; set; }

            /*
             Example of what this does:

            <Report name="SomeReport" providerNameColumn="TheProvider">
                <Parameter name="forDate" type="date" />
                    <Script>
                        <![CDATA[select a.Date as TheDate, a.Proider as TheProvider from SomeSource where a.Date > @forDate]]>
                    </Script>
                </Parameter>
            </Report>

            Adding providerNameColumn here will cause the result report to have rows like this:
            2022-12-12 Some provider
            
            Rather than:
            2022-12-12 someProvider

            It does this by looking up the provider code in the provider json files and swapping it for endUserDisplayName.
             */
            public string ProviderNameColumn { get; set; }
            public string Script { get; set; }
            public List<Parameter> Parameters { get; set; }
            public class Parameter
            {
                public string ParamName { get; set; }
                public string ParamType { get; set; }
            }
        }

        private bool TryFetchReportData(JObject request, out string failedMessage, Action<SqlDataReader, ReportMetadata> beforeFirstRowOfFirstBatch, Action<SqlDataReader, ReportMetadata> onRowRead)
        {
            var reportMetadata = new ReportMetadata();

            reportMetadata.ReportName = request.SelectToken("$.reportName")?.Value<string>();
            if (reportMetadata.ReportName == null)
            {
                failedMessage = "Missing reportName";
                return false;
            }

            var report = NEnv.DatawarehouseModel.Descendants().Where(x => x.Name.LocalName == "Report" && x.Attribute("name")?.Value == reportMetadata.ReportName).FirstOrDefault();
            if (report == null)
            {
                failedMessage = "Not found";
                return false;
            }

            reportMetadata.ProviderNameColumn = report.Attribute("providerNameColumn")?.Value;

            reportMetadata.Parameters = report.Descendants().Where(x => x.Name == "Parameter").Select(x => new ReportMetadata.Parameter
            {
                ParamName = x.Attribute("name").Value,
                ParamType = x.Attribute("type").Value
            }).ToList();

            reportMetadata.Script = report.Descendants().Where(x => x.Name == "Script").FirstOrDefault()?.Value;
            if (reportMetadata.Script == null)
            {
                failedMessage = $"Missing script for report {reportMetadata.ReportName}";
                return false;
            }

            reportMetadata.UseRowNrDataBatching = false;
            int? rowNrDataBatchingBatchSize = null;
            var rowNrDataBatchingElement = report.Descendants().Where(x => x.Name == "RowNrDataBatching").FirstOrDefault();
            if (rowNrDataBatchingElement != null)
            {
                var batchSize = rowNrDataBatchingElement.Attribute("batchSize")?.Value;
                if (batchSize == null)
                    throw new Exception("Missing batchSize in RowNrDataBatching");
                rowNrDataBatchingBatchSize = int.Parse(batchSize);
                reportMetadata.UseRowNrDataBatching = true;
            }

            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DataWarehouse"].ConnectionString;
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (var c = conn.CreateCommand())
                {
                    c.CommandText = reportMetadata.Script;
                    c.CommandTimeout = 3600;
                    foreach (var reportParameter in reportMetadata.Parameters)
                    {
                        if (reportParameter.ParamType?.ToLowerInvariant() == "date")
                        {
                            var paramValue = request?.SelectToken($"$.parameters.{reportParameter.ParamName}")?.Value<DateTime>();
                            if (!paramValue.HasValue)
                            {
                                failedMessage = $"Invalid or missing parameter {reportParameter.ParamName}";
                                return false;
                            }
                            c.Parameters.AddWithValue($"@{reportParameter.ParamName}", paramValue.Value);
                        }
                        else if (reportParameter.ParamType?.ToLowerInvariant() == "string")
                        {
                            var paramValue = request?.SelectToken($"$.parameters.{reportParameter.ParamName}")?.Value<string>();
                            if (string.IsNullOrWhiteSpace(paramValue))
                            {
                                failedMessage = $"Invalid or missing parameter {reportParameter.ParamName}";
                                return false;
                            }
                            c.Parameters.AddWithValue($"@{reportParameter.ParamName}", paramValue?.Trim());
                        }
                        else
                            throw new NotImplementedException();
                    }
                    SqlParameter fromBatchingRowNrParam = null;
                    SqlParameter toBatchingRowNrParam = null;
                    if (reportMetadata.UseRowNrDataBatching)
                    {
                        fromBatchingRowNrParam = c.Parameters.AddWithValue("fromBatchingRowNr", 0);
                        toBatchingRowNrParam = c.Parameters.AddWithValue("toBatchingRowNr", 0);
                    }

                    var excelColumns = new List<Tuple<DocumentClientExcelRequest.Column, Func<SqlDataReader, DocumentClientExcelRequest.Value>>>();

                    var rows = new List<DocumentClientExcelRequest.Value[]>(rowNrDataBatchingBatchSize ?? 1000);
                    Func<Tuple<int, int>, bool> handleBatch = batchingRowNrInterval =>
                    {
                        if (batchingRowNrInterval != null)
                        {
                            fromBatchingRowNrParam.Value = batchingRowNrInterval.Item1;
                            toBatchingRowNrParam.Value = batchingRowNrInterval.Item2;
                        }
                        using (var reader = c.ExecuteReader())
                        {
                            //Create columns
                            if (batchingRowNrInterval == null || batchingRowNrInterval.Item1 == 1)
                            {
                                beforeFirstRowOfFirstBatch(reader, reportMetadata);
                            }

                            bool hasRows = false;
                            while (reader.Read())
                            {
                                onRowRead(reader, reportMetadata);
                                hasRows = true;
                            }

                            return hasRows;
                        }
                    };

                    if (!reportMetadata.UseRowNrDataBatching)
                    {
                        handleBatch(null);
                    }
                    else
                    {
                        var fromRowNr = 1;
                        var runAgain = true;
                        var guard = 0;
                        while (runAgain && guard++ < 1000)
                        {
                            var toRowNr = fromRowNr + rowNrDataBatchingBatchSize.Value - 1;
                            runAgain = handleBatch(Tuple.Create(fromRowNr, toRowNr));
                            fromRowNr = toRowNr + 1;
                        }
                        if (guard >= 999)
                            throw new Exception("Hit guard code");
                    }

                    failedMessage = null;
                    return true;
                }
            }
        }

        [HttpPost]
        [Route("CreateReport")]
        public ActionResult CreateReport()
        {
            if (!Request.ContentType.Contains("application/json"))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid request. Can only accept json requests");

            Request.InputStream.Position = 0;
            JObject request;
            using (var r = new StreamReader(Request.InputStream))
            {
                request = JObject.Parse(r.ReadToEnd());
            }

            var excelColumns = new List<Tuple<DocumentClientExcelRequest.Column, Func<SqlDataReader, DocumentClientExcelRequest.Value>>>();
            var rows = new List<DocumentClientExcelRequest.Value[]>(1000);

            ReportMetadata meta = null;
            Action<SqlDataReader, ReportMetadata> beforeFirstRowOfFirstBatch = (reader, reportMetadata) =>
            {
                meta = reportMetadata;
                for (int i = 0; i < reader.FieldCount; ++i)
                {
                    var localIndex = i;
                    var fieldType = reader.GetDataTypeName(i);
                    var fieldName = reader.GetName(i);
                    if (reportMetadata.UseRowNrDataBatching && fieldName == "BatchingRowNr")
                    {
                        continue;
                    }
                    var col = new DocumentClientExcelRequest.Column
                    {
                        HeaderText = fieldName
                    };
                    Func<SqlDataReader, DocumentClientExcelRequest.Value> parser;
                    if (fieldType == "nvarchar")
                    {
                        col.IsText = true;
                        if (reportMetadata.ProviderNameColumn == fieldName)
                        {
                            parser = dataReader => new DocumentClientExcelRequest.Value { S = ProviderDisplayNames.GetProviderDisplayName(dataReader.IsDBNull(localIndex) ? null : dataReader.GetString(localIndex)) };
                        }
                        else
                            parser = dataReader => new DocumentClientExcelRequest.Value { S = dataReader.IsDBNull(localIndex) ? null : dataReader.GetString(localIndex) };
                    }
                    else if (fieldType == "decimal" || fieldType == "money")
                    {
                        col.IsNumber = true;
                        col.NrOfDecimals = 2;
                        parser = dataReader => new DocumentClientExcelRequest.Value { V = dataReader.IsDBNull(localIndex) ? new decimal?() : dataReader.GetDecimal(localIndex) };
                    }
                    else if (fieldType == "int")
                    {
                        col.IsNumber = true;
                        col.NrOfDecimals = 0;
                        parser = dataReader => new DocumentClientExcelRequest.Value { V = dataReader.IsDBNull(localIndex) ? new decimal?() : dataReader.GetInt32(localIndex) };
                    }
                    else if (fieldType == "bigint")
                    {
                        col.IsNumber = true;
                        col.NrOfDecimals = 0;
                        parser = dataReader => new DocumentClientExcelRequest.Value { V = dataReader.IsDBNull(localIndex) ? new decimal?() : dataReader.GetInt64(localIndex) };
                    }
                    else if (fieldType == "bit")
                    {
                        col.IsNumber = true;
                        col.NrOfDecimals = 0;
                        parser = dataReader => new DocumentClientExcelRequest.Value { V = dataReader.IsDBNull(localIndex) ? new decimal?() : (dataReader.GetBoolean(localIndex) ? 1 : 0) };
                    }
                    else if (fieldType == "date" || fieldType == "datetime")
                    {
                        col.IsDate = true;
                        parser = dataReader => new DocumentClientExcelRequest.Value { D = dataReader.IsDBNull(localIndex) ? new DateTime?() : dataReader.GetDateTime(localIndex) };
                    }
                    else
                        throw new Exception($"Field '{fieldName}' Sql type '{fieldType}' is not supported");

                    excelColumns.Add(Tuple.Create(col, parser));
                }
            };
            Action<SqlDataReader, ReportMetadata> onRowRead = (reader, reportMetadata) =>
            {
                rows.Add(excelColumns.Select(col => col.Item2(reader)).ToArray());
            };
            if (!TryFetchReportData(request, out string failedMessage, beforeFirstRowOfFirstBatch, onRowRead))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, failedMessage);
            }

            var excelRequest = new DocumentClientExcelRequest
            {
                Sheets = new DocumentClientExcelRequest.Sheet[]
                {
                            new DocumentClientExcelRequest.Sheet
                            {
                                Columns = excelColumns.Select(col => col.Item1).ToArray(),
                                AutoSizeColumns = false,
                                Title = meta?.ReportName,
                                Cells = rows.ToArray()
                            }
                }
            };

            var documentClient = new DocumentClient();
            var stream = documentClient.CreateXlsx(excelRequest, TimeSpan.FromHours(1));
            return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
    }

    public static class ExpandoObjectExtensions
    {
        public static void Set(this ExpandoObject source, string name, object value)
        {
            (source as IDictionary<string, object>)[name] = value;
        }

        public static object Get(this ExpandoObject source, string name)
        {
            if (source == null)
                return null;
            return (source as IDictionary<string, object>)[name];
        }
    }
}