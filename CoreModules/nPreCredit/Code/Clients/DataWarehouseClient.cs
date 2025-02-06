using System.Collections.Generic;
using System.Dynamic;

namespace nPreCredit.Code
{
    public class DataWarehouseClient : AbstractServiceClient
    {
        protected override string ServiceName => "nDataWarehouse";

        public void MergeDimension<T>(string dimensionName, List<T> values)
        {
            Begin()
                .PostJson("Api/MergeDimension", new { dimensionName = dimensionName, values = values })
                .EnsureSuccessStatusCode();
        }

        public void MergeFact<T>(string factName, List<T> values)
        {
            Begin()
                .PostJson("Api/MergeFact", new { factName = factName, values = values })
                .EnsureSuccessStatusCode();
        }

        public List<T> FetchReportData<T>(string reportName, ExpandoObject parameters)
        {
            return Begin()
                .PostJson("Api/FetchReportData", new { reportName, parameters })
                .ParseJsonAs<List<T>>();
        }
    }
}