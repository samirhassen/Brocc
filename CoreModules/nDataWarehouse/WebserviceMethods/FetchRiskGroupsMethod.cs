using Dapper;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace nCredit.WebserviceMethods
{
    public class FetchRiskGroupsMethod : TypedWebserviceMethod<FetchRiskGroupsMethod.Request, FetchRiskGroupsMethod.Response>
    {
        public override string Path => "RiskGroups/FetchAll";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            return new Response
            {
                Groups = WithSqlConnection(conn =>
                conn.Query<RiskGroupModel>("select distinct ScoreGroup as RiskGroup from Fact_CreditApplicationSnapshot where not ScoreGroup is null order by ScoreGroup").ToList())
            };
        }

        private T WithSqlConnection<T>(Func<SqlConnection, T> f)
        {
            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DataWarehouse"].ConnectionString;
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                return f(conn);
            }
        }

        public class Request
        {

        }

        public class Response
        {
            public List<RiskGroupModel> Groups { get; set; }
        }

        public class RiskGroupModel
        {
            public string RiskGroup { get; set; }
        }
    }
}