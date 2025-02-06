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
    public class CreditStagingDatabaseTransform : StagingDatabaseTransform
    {
        public CreditStagingDatabaseTransform(SharedTransformSettings settings, string backupFilePath) : base(settings, backupFilePath)
        {
        }

        protected override string ModuleName => "nCredit";

        public DateTime? LatestTransactionDate { get; set; }

        protected override void DoAdditionalUserIdRemapping(SqlTransaction tr)
        {
            if (this.Settings.ReplacementUserId.HasValue)
            {
                this.StagingDbConnection.Execute("update CreditComment set CommentById = @userId",
                    param: new { userId = this.Settings.ReplacementUserId.Value },
                    transaction: tr);
            }
        }

        protected override void DoTransform()
        {
            RunMigrationHistoryCheck("201808221227088_AddedCreditCommentCreationEvent");

            //Wipe all user comments
            StagingDbConnection.Execute("delete from CreditComment where EventType = 'UserComment'");
            
            //Wipe all encrypted values
            StagingDbConnection.Execute("truncate table EncryptedValue");

            //Wipe incoming payment fields (customer name and address)
            StagingDbConnection.Execute("delete from IncomingPaymentHeaderItem");

            //Wipe outgoing items
            StagingDbConnection.Execute("delete from OutgoingPaymentHeaderItem");

            //Generate mock required outgoing payment items.
            StagingDbConnection.Execute(
@"with MissingToIban
as
(
	select	h.Id as OutgoingPaymentId
	from	OutgoingPaymentHeader h
	where	not exists(select 1 from OutgoingPaymentHeaderItem i where i.OutgoingPaymentId = h.Id and i.Name = 'ToIban')
	and		h.OutgoingPaymentFileheaderId is null
)
insert into OutgoingPaymentHeaderItem
(OutgoingPaymentId, Name, IsEncrypted, Value, ChangedById, ChangedDate)
select	t.OutgoingPaymentId, 'ToIban', 0, 'FI7840599436867151', 0, getdate()
from	MissingToIban t");

            StagingDbConnection.Execute(
@"with MissingCustomerName
as
(
	select	h.Id as OutgoingPaymentId
	from	OutgoingPaymentHeader h
	where	not exists(select 1 from OutgoingPaymentHeaderItem i where i.OutgoingPaymentId = h.Id and i.Name = 'CustomerName')
	and		h.OutgoingPaymentFileheaderId is null
)
insert into OutgoingPaymentHeaderItem
(OutgoingPaymentId, Name, IsEncrypted, Value, ChangedById, ChangedDate)
select	t.OutgoingPaymentId, 'CustomerName', 0, 'Maarit Hurme', 0, getdate()
from	MissingCustomerName t");

            //Used to feed the timemachine
            LatestTransactionDate = StagingDbConnection.QueryFirst<DateTime?>("select MAX(TransactionDate) from AccountTransaction");
        }
    }
}
