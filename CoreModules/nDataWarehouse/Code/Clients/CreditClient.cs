using nDataWarehouse.DbModel;
using System;

namespace nDataWarehouse.Code.Clients
{
    public class CreditClient : AbstractServiceClient
    {
        public CreditClient(Func<string> getBearerToken) : base(getBearerToken)
        {

        }

        protected override string ServiceName => "nCredit";

        public DashboardDataRepository.AggregateModel FetchRealtimeAggregates()
        {
            return Begin()
                .PostJson("api/Dashboard/Aggregate-Data", new { })
                .ParseJsonAs<DashboardDataRepository.AggregateModel>();
        }
    }
}