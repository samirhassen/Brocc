using NTech.Services.Infrastructure;
using System;
using System.Data.SqlClient;

namespace SlxMigrator
{
    internal class ConnectionFactory
    {
        private readonly NTechSimpleSettings simpleSettings;

        public ConnectionFactory(NTechSimpleSettings simpleSettings)
        {
            this.simpleSettings = simpleSettings;
        }
        public string GetConnectionString(DatabaseCode databaseName)
        {
            switch (databaseName)
            {
                case DatabaseCode.Credit:
                    return simpleSettings.Req("CreditConnectionString");
                case DatabaseCode.Customer:
                    return simpleSettings.Req("CustomerConnectionString");
                case DatabaseCode.PreCredit:
                    return simpleSettings.Req("PreCreditConnectionString");
                case DatabaseCode.Savings:
                    return simpleSettings.Req("SavingsConnectionString");
                default:
                    throw new NotImplementedException();
            }
        }
        public SqlConnection CreateOpenConnection(DatabaseCode databaseName)
        {
            var connection = new SqlConnection(GetConnectionString(databaseName));
            connection.Open();
            return connection;
        }
    }

    public enum DatabaseCode
    {
        Credit,
        Customer,
        PreCredit,
        Savings
    }
}
