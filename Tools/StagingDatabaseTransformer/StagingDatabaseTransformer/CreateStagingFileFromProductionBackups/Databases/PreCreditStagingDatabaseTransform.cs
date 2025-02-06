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
    public class PreCreditStagingDatabaseTransform : StagingDatabaseTransform
    {
        public PreCreditStagingDatabaseTransform(SharedTransformSettings settings, string backupFilePath) : base(settings, backupFilePath)
        {
        }

        protected override string ModuleName => "nPreCredit";

        protected override void DoAdditionalUserIdRemapping(SqlTransaction tr)
        {
            if(this.Settings.ReplacementUserId.HasValue)
            {
                this.StagingDbConnection.Execute("update CreditApplicationComment set CommentById = @userId", 
                    param: new { userId = this.Settings.ReplacementUserId.Value },
                    transaction: tr);
            }            
        }

        /// <summary>
        /// Replace civicRegNr and birthDate on all applicants
        /// </summary>
        private void HandleCreditApplicationItems()
        {
            using (var dbTr = TestDb.BeginTransaction())
            using (var tr = StagingDbConnection.BeginTransaction())
            {
                var creditApplicationCustomers = StagingDbConnection.Query<CreditApplicationCustomer>("select h.ApplicationNr, i.GroupName, i.Value as CustomerId from CreditApplicationHeader h join CreditApplicationItem i on  i.Name = 'customerId' and i.ApplicationNr = h.ApplicationNr order by h.ApplicationNr asc", transaction: tr).ToList();
                var creditApplicationItemStagingItems = new List<CreditApplicationItemStagingItem>(creditApplicationCustomers.Count * 2);
                foreach (var customer in creditApplicationCustomers)
                {
                    var civicRegNr = CivicRegNumberParser.Parse(dbTr.Get<string>(customer.CustomerId, "StagingCivicRegNrByCustomerId"));
                    creditApplicationItemStagingItems.Add(new CreditApplicationItemStagingItem
                    {
                        ApplicationNr = customer.ApplicationNr,
                        GroupName = customer.GroupName,
                        Name = "civicRegNr",
                        Value = civicRegNr.NormalizedValue
                    });
                    creditApplicationItemStagingItems.Add(new CreditApplicationItemStagingItem
                    {
                        ApplicationNr = customer.ApplicationNr,
                        GroupName = customer.GroupName,
                        Name = "birthDate",
                        Value = civicRegNr.BirthDate.Value.ToString("yyyy-MM-dd")
                    });
                }

                using (var creditApplicationItemStagingTable = new TemporaryStagingTable(StagingDbConnection, tr, "CreditApplicationItem", "Name, GroupName, ApplicationNr, Value"))
                {
                    StagingDbConnection.Execute($"insert into [{creditApplicationItemStagingTable.TempTableName}] (Name, GroupName, ApplicationNr, Value) values(@Name, @GroupName, @ApplicationNr, @Value)",
                        param: creditApplicationItemStagingItems,
                        transaction: tr,
                        commandTimeout: 30 * 60);
                    
                    StagingDbConnection.Execute($"create index [{creditApplicationItemStagingTable.TempTableName}-Idx] on [{creditApplicationItemStagingTable.TempTableName}](Name, GroupName, ApplicationNr) Include(Value)", 
                        transaction: tr,
                        commandTimeout: 30 * 60);

                    StagingDbConnection.Execute(
                            $@"update	a set 
		                        Value = b.Value,
		                        IsEncrypted = 0
                        from	CreditApplicationItem a 
                        join	[{creditApplicationItemStagingTable.TempTableName}] b on a.ApplicationNr = b.ApplicationNr and a.Name = b.Name and a.GroupName = b.GroupName", 
                            transaction: tr,
                            commandTimeout: 30*60);
                }

                //Update the replication timestamp for credit applications so we dont trigger a dw-update on the entire application snapshot table after the above rebuild of the application item table
                //The rebuild works just fine but it take way to long to be worth it
                //NOTE: There is potential for strange edgecases here where the dw was not up to date on export so try to put the prod db export right after a dw export
                StagingDbConnection.Execute(
@"
insert into SystemItem
([Key], Value, ChangedById, ChangedDate)
values
('DwLatestMergedTimestamp_Fact_CreditApplicationSnapshot_ItemTs', (select (SELECT CAST(MAX(i.[Timestamp]) as varbinary(max)) FOR XML PATH(''), BINARY BASE64) from	CreditApplicationItem i), 0, GETDATE())", 
                            transaction: tr,
                            commandTimeout: 30 * 60);

                StagingDbConnection.Execute(
@"insert into SystemItem
([Key], Value, ChangedById, ChangedDate)
values
('DwLatestMergedTimestamp_Fact_CreditApplicationSnapshot', (select(SELECT CAST(MAX(i.[Timestamp]) as varbinary(max)) FOR XML PATH(''), BINARY BASE64) from CreditApplicationHeader i), 0, GETDATE())",
                            transaction: tr,
                            commandTimeout: 30 * 60);

                dbTr.Commit();
                tr.Commit();
            }
        }

        private static string[] creditDecisionFilterPaths = new[]
        {
            "recommendation.CreditReportDebugItems",

            "application.applicant1.civicRegNr",
            "application.applicant1.birthDate",
            "application.applicant1.employer",
            "application.applicant1.employerPhone",
            "application.applicant1.email",
            "application.applicant1.phone",

            "application.applicant2.civicRegNr",
            "application.applicant2.birthDate",
            "application.applicant2.employer",
            "application.applicant2.employerPhone",
            "application.applicant2.email",
            "application.applicant2.phone",

            "creditreport1.firstName",
            "creditreport1.lastName",
            "creditreport1.addressStreet",
            "creditreport1.addressZipcode",
            "creditreport1.addressCity",
            "creditreport1.addressCountry",

            "creditreport2.firstName",
            "creditreport2.lastName",
            "creditreport2.addressStreet",
            "creditreport2.addressZipcode",
            "creditreport2.addressCity",
            "creditreport2.addressCountry"
        };

        private string FilterCreditDecision(string input)
        {
            return TransformJsonExpression(input,
                                        new[] { "offer", "additionalLoanOffer", "creditReportsUsed", "recommendation", "application", "rejectionReasons", "creditreport1", "creditreport2" },
                                        creditDecisionFilterPaths);
        }

        /// <summary>
        /// Remove everything in the decisions except:
        /// offer, additionalLoanOffer, rejectionReasons, creditReportsUsed, recommendation (though remove credit report debug items from recommendations)
        /// </summary>
        private void HandleCreditDecisions()
        {
            using (var tr = StagingDbConnection.BeginTransaction())
            using (var creditDecisionStagingTable = new TemporaryStagingTable(StagingDbConnection, tr, "CreditDecision", "Id, AcceptedDecisionModel, RejectedDecisionModel"))
            {
                creditDecisionStagingTable.EnableIdentityInsert();

                var creditDecisionIds = StagingDbConnection.Query<int>("select Id from CreditDecision order by id desc", transaction: tr);
                foreach (var creditDecisionIdGroup in SplitIntoGroupsOfN(creditDecisionIds.ToArray(), 100))
                {
                    var stagingItems = StagingDbConnection.Query<CreditDecisionStagingItem>("select Id, AcceptedDecisionModel, RejectedDecisionModel from CreditDecision where Id in @ids", 
                        param: new { ids = creditDecisionIdGroup }, 
                        transaction: tr).ToList();

                    foreach(var item in stagingItems)
                    {
                        item.AcceptedDecisionModel = FilterCreditDecision(item.AcceptedDecisionModel);
                        item.RejectedDecisionModel = FilterCreditDecision(item.RejectedDecisionModel);
                    }
                    
                    StagingDbConnection.Execute($"insert into [{creditDecisionStagingTable.TempTableName}] (Id, AcceptedDecisionModel, RejectedDecisionModel) values(@Id, @AcceptedDecisionModel, @RejectedDecisionModel)",
                        param: stagingItems,
                        transaction: tr);
                }

                StagingDbConnection.Execute($"create index [{creditDecisionStagingTable.TempTableName}-Idx] on [{creditDecisionStagingTable.TempTableName}](Id) Include(AcceptedDecisionModel, RejectedDecisionModel)", transaction: tr);

                StagingDbConnection.Execute(
                        $@"update	a set 
		                        AcceptedDecisionModel = b.AcceptedDecisionModel,
		                        RejectedDecisionModel = b.RejectedDecisionModel
                        from	CreditDecision a 
                        join	[{creditDecisionStagingTable.TempTableName}] b on a.Id = b.Id", transaction: tr, commandTimeout: 30 * 60);

                tr.Commit();
            }
        }
                
        protected override void DoTransform()
        {
            //AddedCustomerCheckpoint
            RunMigrationHistoryCheck("201808151102402_AddedCreditDecisionType");

            //Verify that there are not applications without customers as that case is not handled
            var countApplicationsWithoutCustomerId = StagingDbConnection.QueryFirst<int>(@"select COUNT(*) from CreditApplicationHeader h where not exists(select 1 from CreditApplicationItem i where i.Name = 'customerId' and i.ApplicationNr = h.ApplicationNr)");
            if (countApplicationsWithoutCustomerId > 0)
                throw new Exception($"There are applications with no customerId present. This case is not handled");
            
            //Wipe all encrypted values
            StagingDbConnection.Execute("truncate table EncryptedValue", commandTimeout: 30 * 60);

            //Since the reasons are encrypted anyway. Alternatively these could be scrambled.
            StagingDbConnection.Execute("truncate table CustomerCheckpoint", commandTimeout: 30 * 60);

            //Wipe all search terms (TODO: Remove this table from production and all code)
            StagingDbConnection.Execute("truncate table CreditApplicationSearchTerm", commandTimeout: 30 * 60);

            StagingDbConnection.Execute("truncate table KeyValueItem", commandTimeout: 30 * 60);

            StagingDbConnection.Execute("truncate table TemporaryExternallyEncryptedItem", commandTimeout: 30 * 60);

            //Wipe all user comments
            StagingDbConnection.Execute("delete from CreditApplicationComment where EventType = 'UserComment'", commandTimeout: 30 * 60);

            StagingDbConnection.Execute("delete from CreditApplicationOneTimeToken where TokenType = 'SignInitialCreditAgreement'", commandTimeout: 30 * 60);

            HandleCreditDecisions();

            //NOTE: Dont change anything about applications below this or the entire application snapshot dw will resynch
            HandleCreditApplicationItems();
        }
        
        private string TransformJsonExpression(string value, IList<string> namesToKeep, IList<string> filterPaths)
        {
            if (value == null)
                return null;
            var p = JToken.Parse(value);
            if (p.Type != JTokenType.Object)
                throw new Exception($"value excepted to be a json object but instead was: {p.Type.ToString()}");

            foreach (var a in p.ToList())
            {
                if (a.Type != JTokenType.Property)
                    throw new Exception($"Json object expected to only have properties but instead had: {p.Type.ToString()}");

                var ap = a as JProperty;
                
                if (!namesToKeep.Contains(ap.Name))
                    ap.Remove();
            }
            
            foreach (var path in filterPaths)
            {
                foreach (var t in p.SelectTokens(path).ToList())
                {
                    t.Parent.Remove();
                }
            }

            return p.ToString(Formatting.None);
        }

        private class CreditDecisionStagingItem
        {
            public int Id { get; set; }
            public string AcceptedDecisionModel { get; set; }
            public string RejectedDecisionModel { get; set; }
        }

        private class CreditApplicationItemStagingItem
        {
            public string Name { get; set; }
            public string ApplicationNr { get; set; }
            public string GroupName { get; set; }
            public string Value { get; set; }
        }

        private class CreditApplicationCustomer
        {
            public string ApplicationNr { get; set; }
            public string GroupName { get; set; }
            public string CustomerId { get; set; }
        }
    }
}
