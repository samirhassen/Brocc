using System;

namespace nDataWarehouse.Code.Clients
{
    public class PreCreditClient : AbstractServiceClient
    {
        public PreCreditClient(Func<string> getBearerToken) : base(getBearerToken)
        {

        }

        protected override string ServiceName => "nPreCredit";

        public AggregatesResponse FetchRealtimeAggregates(DateTime forDate)
        {
            return Begin()
                .PostJson("api/Dashboard/Daily-Aggregate-Data", new { forDate })
                .ParseJsonAs<AggregatesResponse>();
        }

        public class AggregatesResponse
        {
            public decimal ApprovedAmount { get; set; }
        }
    }
}