using Dapper;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace nCredit.WebserviceMethods
{
    public class FetchProvidersMethod : TypedWebserviceMethod<FetchProvidersMethod.Request, FetchProvidersMethod.Response>
    {
        public override string Path => "Providers/FetchAll";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {

            return new Response
            {
                Providers = WithSqlConnection(conn =>
                 conn.Query<ProviderModel>("select distinct ProviderName from Dimension_Credit order by ProviderName").ToList())
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
            public List<ProviderModel> Providers { get; set; }
        }

        public class ProviderModel
        {
            public string ProviderName { get; set; }
        }
    }
}