using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using NTech.Services.Infrastructure;
using nTest.RandomDataSource;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;

namespace StagingDatabaseTransformer
{
    public class DataWarehouseDatabaseTransform : StagingDatabaseTransform
    {
        public DataWarehouseDatabaseTransform(SharedTransformSettings settings, string backupFilePath) : base(settings, backupFilePath)
        {
        }

        protected override string ModuleName => "nDataWarehouse";
        
        protected override void DoTransform()
        {
            StagingDbConnection.Execute("truncate table Dimension_CreditReportItem");

            //Scrub employer information
            StagingDbConnection.Execute(
@"IF (EXISTS (select * from INFORMATION_SCHEMA.COLUMNS c where c.TABLE_SCHEMA = 'dbo' AND c.TABLE_NAME = 'Fact_CreditApplicationSnapshot' AND c.COLUMN_NAME = 'Applicant1Employer'))
begin
update Fact_CreditApplicationSnapshot set Applicant1Employer = 'removed in test' where Applicant1Employer is not null
end  ");
            StagingDbConnection.Execute(
@"IF (EXISTS (select * from INFORMATION_SCHEMA.COLUMNS c where c.TABLE_SCHEMA = 'dbo' AND c.TABLE_NAME = 'Fact_CreditApplicationSnapshot' AND c.COLUMN_NAME = 'Applicant2Employer'))
begin
update Fact_CreditApplicationSnapshot set Applicant2Employer = 'removed in test' where Applicant2Employer is not null
end  ");
        }
    }
}
