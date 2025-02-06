using Dapper;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace nCustomer.Code
{
    public class DatabaseMergeCommand<T>
    {
        private readonly string serviceName;
        private readonly string connectionString;

        public DatabaseMergeCommand(string serviceName, string connectionString)
        {
            this.serviceName = serviceName;
            this.connectionString = connectionString;
        }

        private void BulkInsertAndMerge(string tableName, IEnumerable<T> values, List<Column> columns, List<PrimaryKeyColumn> primaryKeyColumns, SqlConnection conn, SqlTransaction tr)
        {
            using (var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.KeepNulls | SqlBulkCopyOptions.CheckConstraints, tr))
            {
                var tempTableName = $"#Temp_{tableName}";

                var allColumnNames = string.Join(", ", columns.Select(x => $"[{x.ColumnName}]"));
                conn.Execute($"Select {allColumnNames} Into [{tempTableName}] From [{tableName}] Where 1 = 2", transaction: tr); //Copy the structure of the table

                bulkCopy.DestinationTableName = tempTableName;

                Func<Column, Type> getType = n =>
                {
                    if (n.DataTypeName == "nvarchar") return typeof(string);
                    if (n.DataTypeName == "date") return typeof(DateTime);
                    if (n.DataTypeName == "datetime") return typeof(DateTime);
                    if (n.DataTypeName == "int") return typeof(int);
                    if (n.DataTypeName == "decimal") return typeof(decimal);
                    if (n.DataTypeName == "money") return typeof(decimal);
                    if (n.DataTypeName == "bit") return typeof(bool);
                    if (n.DataTypeName == "bigint") return typeof(long);
                    throw new NotImplementedException();
                };

                //Create a data
                var dt = new DataTable();

                foreach (var column in columns)
                {
                    dt.Columns.Add(column.ColumnName, getType(column));
                }

                var et = typeof(T);
                Func<T, Column, object> getValue = (e, n) =>
                {
                    var t = getType(n);
                    if (t == typeof(decimal))
                    {
                        var v = et.GetProperty(n.ColumnName)?.GetValue(e);
                        if (v == null || v == DBNull.Value)
                            return v;
                        else
                        {
                            var vv = Convert.ToDecimal(v);
                            if (vv > 922337203685477m || vv < -922337203685477m)
                            {
                                if (n.IsNullable)
                                    return null;
                                else
                                {
                                    //Could put 0 instead here but that feels really dangerous
                                    return 0m;
                                }
                            }
                            else
                                return v;
                        }
                    }
                    else
                    {
                        return et.GetProperty(n.ColumnName)?.GetValue(e);
                    }
                };

                foreach (var v in values)
                {
                    dt.Rows.Add(columns.Select(x => getValue(v, x)).ToArray());
                }

                bulkCopy.WriteToServer(dt);

                var nonKeyColumns = columns.Where(x => !primaryKeyColumns.Any(y => y.ColumnName == x.ColumnName)).ToList();
                var updateFragment = string.Join(", ", nonKeyColumns.Select(x => $"Target.[{x.ColumnName}]=Source.[{x.ColumnName}]"));
                var updateLine = nonKeyColumns.Any() ? $"WHEN MATCHED THEN UPDATE SET {updateFragment}" : " ";
                var pkFragment = string.Join(" and ", primaryKeyColumns.Select(x => $"Target.[{x.ColumnName}]=Source.[{x.ColumnName}]"));
                var insertColumnNames = string.Join(", ", columns.Select(x => $"[{x.ColumnName}]"));
                var insertSourceNames = string.Join(", ", columns.Select(x => $"Source.[{x.ColumnName}]"));
                var mergeCmd =
$@"MERGE INTO {tableName} WITH (HOLDLOCK) AS Target
USING {tempTableName} AS Source
ON ({pkFragment})
{updateLine}
WHEN NOT MATCHED BY TARGET THEN INSERT ({insertColumnNames}) VALUES ({insertSourceNames});";

                const int TWO_HOURS = 7200;
                conn.Execute(mergeCmd, transaction: tr, commandTimeout: TWO_HOURS);
            }
        }

        public bool TryMergeTable(string tableName, List<T> values, out string errorMessage)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var tr = conn.BeginTransaction())
                {
                    var meta = GetMetadata(tableName, conn, tr);

                    if (meta.Item1.Count == 0)
                    {
                        errorMessage = $"No such table: {tableName}";
                        return false;
                    }

                    BulkInsertAndMerge(tableName, values, meta.Item2, meta.Item1, conn, tr);
                    tr.Commit();

                    errorMessage = null;
                    return true;
                }
            }
        }

        private static FewItemsCache metadataCache = new FewItemsCache();

        private Tuple<List<PrimaryKeyColumn>, List<Column>> GetMetadata(string tableName, SqlConnection conn, SqlTransaction tr)
        {
            return metadataCache.WithCache($"ntech.{serviceName}.tablemetadata.{tableName}", TimeSpan.FromMinutes(15), () =>
            {
                var columns = conn.Query<Column>(
               @"select	t.COLUMN_NAME as ColumnName,
		cast(case when t.IS_NULLABLE = 'YES' then 1 else 0 end as bit) as IsNullble,
		t.DATA_TYPE as DataTypeName
from	INFORMATION_SCHEMA.COLUMNS t
where	t.TABLE_SCHEMA = 'dbo'
and		t.TABLE_NAME = @name
and		t.DATA_TYPE <> 'timestamp'
order by t.ORDINAL_POSITION asc", new { name = tableName }, transaction: tr).ToList();

                var primaryKey = conn.Query<PrimaryKeyColumn>(@"select	cc.TABLE_NAME as TableName,
		kk.COLUMN_NAME as ColumnName
from	INFORMATION_SCHEMA.TABLE_CONSTRAINTS cc
join	INFORMATION_SCHEMA.KEY_COLUMN_USAGE kk on kk.CONSTRAINT_NAME = cc.CONSTRAINT_NAME and kk.TABLE_NAME = cc.TABLE_NAME and kk.TABLE_SCHEMA = cc.TABLE_SCHEMA
where	cc.CONSTRAINT_TYPE = 'PRIMARY KEY'
and		cc.TABLE_NAME = @name
order by cc.TABLE_NAME, kk.COLUMN_NAME", new { name = tableName }, transaction: tr).ToList();

                return Tuple.Create(primaryKey, columns);
            });
        }

        public class Column
        {
            public string ColumnName { get; set; }
            public bool IsNullable { get; set; }
            public string DataTypeName { get; set; }
        }

        public class PrimaryKeyColumn
        {
            public string TableName { get; set; }
            public string ColumnName { get; set; }
        }
    }
}