using Dapper;
using System.Data;

namespace NTech.Core.Credit.Shared.Database
{
    public static class CreditContextSetup
    {
        public static void AfterInitialize(IDbConnection connection)
        {

            void ReseedIfNeeded(string tableName, int minAllowedValue)
            {
                var count = connection.ExecuteScalar<int>($"select count(*) from {tableName}");
                if (count > 0)
                    return;
                var currentValue = connection.ExecuteScalar<decimal>($"select IDENT_CURRENT('{tableName}')");
                if (currentValue < minAllowedValue)
                {
                    connection.Execute($"DBCC CHECKIDENT ('{tableName}', RESEED, {minAllowedValue})");
                }
            }
            ReseedIfNeeded("CreditKeySequence", 10000);
            ReseedIfNeeded("OcrPaymentReferenceNrSequence", 11111111);
        }
    }
}
