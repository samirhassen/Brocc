using System;
using System.Data.SqlClient;
using Dapper;

namespace StagingDatabaseTransformer
{
    public class TemporaryStagingTable : IDisposable
    {
        private SqlConnection conn;
        private SqlTransaction tr;
        private string tempTableName;
        
        public TemporaryStagingTable(SqlConnection conn, SqlTransaction tr, string sourceTableName, string sourceTableColumns)
        {
            this.tempTableName = $"#Staging-{sourceTableName}-{Guid.NewGuid().ToString()}";
            this.conn = conn;
            this.tr = tr;
                        
            conn.Execute($"select top 1 {sourceTableColumns} into [{tempTableName}] from {sourceTableName}", transaction: tr);

            conn.Execute($"truncate table [{tempTableName}]", transaction: tr);            
        }

        public string TempTableName
        {
            get
            {
                return tempTableName;
            }
        }

        public void EnableIdentityInsert()
        {
            conn.Execute($"SET IDENTITY_INSERT [{TempTableName}] ON", transaction: tr);
        }

        public void Dispose()
        {
            conn.Execute($"drop table [{tempTableName}]", transaction: tr);
        }
    }
}
