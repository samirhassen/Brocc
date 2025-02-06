using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using NTech.Services.Infrastructure;
using nTest.RandomDataSource;
using System.IO;
using System.Security.Cryptography;
using NTech.Banking.CivicRegNumbers;

namespace StagingDatabaseTransformer
{
    public class CustomerStagingDatabaseTransform : StagingDatabaseTransform
    {
        public CustomerStagingDatabaseTransform(SharedTransformSettings settings, string backupFilePath) : base(settings, backupFilePath)
        {
        }

        protected override string ModuleName => "nCustomer";

        protected override void DoTransform()
        {
            var now = DateTimeOffset.Now;

            RunMigrationHistoryCheck("201809271109483_AddedCustomerComment");

            //Wipe all conflicts but keep a summary of the types around so we can measure how the things we do to reduce the burden of conflict handling work
            StagingDbConnection.Execute(@"with Basis
as
(
	select	Name, 
			isnull(ApprovedDate, DiscardedDate) as HandledDate,
			case when ApprovedDate is not null then 1 else 0 end as IsApproved
	from	CustomerCardConflict 
	where	(ApprovedDate is not null or DiscardedDate is not null)
)
select	b.Name,
		YEAR(b.HandledDate) as HandledYear,  
		DATEPART(wk, b.HandledDate) as HandledWeek,
		SUM(b.IsApproved) as ApprovedCount,
		SUM(1 - b.IsApproved) as DiscarededCount
		into StagingCustomerCardConflictSummary
from	Basis b
group by b.Name, YEAR(b.HandledDate), DATEPART(wk, b.HandledDate)");
            StagingDbConnection.Execute("truncate table CustomerCardConflict", commandTimeout: 60 * 30); //30 minutes

            //Wipe all search terms
            StagingDbConnection.Execute("truncate table CustomerSearchTerm", commandTimeout: 60 * 30); //30 minutes

            //Wipe customer comments
            StagingDbConnection.Execute("truncate table CustomerComment", commandTimeout: 60 * 30); //30 minutes

            //Wipe all encrypted values
            StagingDbConnection.Execute("truncate table EncryptedValue", commandTimeout: 60 * 30); //30 minutes

            //Wipe keyvalu
            StagingDbConnection.Execute("truncate table KeyValueItem", commandTimeout: 60 * 30); //30 minutes

            //Wipe some trapets data
            //TODO: Could potentially update birthdate if it is actually needed for anything
            StagingDbConnection.Execute("delete from TrapetsQueryResultItem where IsEncrypted = 1 Or Name = 'QueryBirthDate'", commandTimeout: 60 * 30); //30 minutes

            //Remove historical values to make finding the current value easier from direct sql
            StagingDbConnection.Execute("update CustomerProperty set ReplacesCustomerProperty_Id = null", commandTimeout: 60 * 30);
            StagingDbConnection.Execute("delete from CustomerProperty where IsCurrentData = 0", commandTimeout: 60 * 30);

            var customerIds = StagingDbConnection.Query<int>("select distinct CustomerId from CustomerProperty order by CustomerId asc", commandTimeout: 60 * 30).ToList();

            var updateFieldNames = new List<string>()
                {
                    "birthDate",
                    "phone",                    
                    "email",
                    "civicRegNr",
                    "lastName",
                    "firstName",
                    "sanction",
                    "externalKycScreeningDate",
                    "addressStreet",
                    "addressZipcode",
                    "addressCity",
                    "addressHash"                    
                };

            var testPersons = new ComposableTestPersonRepository(this.ClientCfg.Country.BaseCountry);
            
            using (var dbTr = TestDb.BeginTransaction())
            using (var tr = StagingDbConnection.BeginTransaction())
            {
                var customerPropertyStagingItems = new List<CustomerPropertyStagingItem>(customerIds.Count * updateFieldNames.Count);
                var customerIdSequenceStagingItems = new List<CustomerIdSequenceStagingItem>(customerIds.Count);
                foreach (var customerId in customerIds)
                {
                    //Make a new customer for this id
                    var p = testPersons.GenerateNewTestPerson(true, this.Random, dbTr, now.Date); //TODO: Credit scoring is a problem here.

                    var civicRegNr = CivicRegNumberParser.Parse(p.CivicRegNr);

                    dbTr.AddOrUpdate(p.CivicRegNr, "StagingCustomerIdByCivicRegNr", customerId.ToString());
                    dbTr.AddOrUpdate(customerId.ToString(), "StagingCivicRegNrByCustomerId", p.CivicRegNr);

                    customerIdSequenceStagingItems.Add(new CustomerIdSequenceStagingItem { CustomerId = customerId, CivicRegNrHash = ComputeCivicRegnrHash(civicRegNr) });

                    foreach (var fieldName in updateFieldNames)
                    {
                        string value;
                        List<string> searchValues = null;

                        if (fieldName == "sanction")
                            value = "false";
                        else if(fieldName == "externalKycScreeningDate")
                            value = "2016-01-01"; //Anything valid. It's mostly used as a boolean flag
                        else if(fieldName == "addressHash")
                        {
                            value = ComputeAddressHash(p.Properties["addressStreet"], p.Properties["addressZipcode"], p.Properties["addressCity"], "");
                        }
                        else if(fieldName == "firstName" || fieldName == "lastName" || fieldName =="email")
                        {
                            value = p.Properties[fieldName];
                            searchValues = TranslateSearchTermValue(fieldName, value);
                        }
                        else
                            value = p.Properties[fieldName];

                        customerPropertyStagingItems.Add(new CustomerPropertyStagingItem { CustomerId = customerId, Name = fieldName, Value = value, SearchValues = searchValues });
                    }
                }

                //Update civic regnr sequences
                using (var custSeqTempTable = new TemporaryStagingTable(StagingDbConnection, tr, "CustomerIdSequence", "CivicRegNrHash, CustomerId"))
                {
                    custSeqTempTable.EnableIdentityInsert();

                    StagingDbConnection.Execute($"insert into [{custSeqTempTable.TempTableName}] (CivicRegNrHash, CustomerId) values(@CivicRegNrHash, @CustomerId)",
                        param: customerIdSequenceStagingItems,
                        transaction: tr,
                        commandTimeout: 60 * 30);

                    StagingDbConnection.Execute("ALTER INDEX IX_CivicRegNrHash ON CustomerIdSequence DISABLE", transaction: tr, commandTimeout: 60 * 30);
                    try
                    {
                        StagingDbConnection.Execute(
                                $@"update a set 
		                        CivicRegNrHash = b.CivicRegNrHash
                        from	CustomerIdSequence a 
                        join	[{custSeqTempTable.TempTableName}] b on a.CustomerId = b.CustomerId", transaction: tr, commandTimeout: 60 * 30);
                    }
                    finally
                    {
                        StagingDbConnection.Execute("ALTER INDEX IX_CivicRegNrHash ON CustomerIdSequence REBUILD", transaction: tr, commandTimeout: 60 * 30);
                    }
                }

                //Update all customer customer data that is included on test persons
                using (var custTempTable = new TemporaryStagingTable(StagingDbConnection, tr, "CustomerProperty", "Name, CustomerId, Value"))
                {
                    StagingDbConnection.Execute($"insert into [{custTempTable.TempTableName}] (Name, CustomerId, Value) values(@Name, @CustomerId, @Value)",
                        param: customerPropertyStagingItems,
                        transaction: tr, 
                        commandTimeout: 60 * 30);

                    StagingDbConnection.Execute($"create index [{custTempTable.TempTableName}-Idx] on [{custTempTable.TempTableName}](CustomerId, Name) Include(Value)", transaction: tr, commandTimeout: 60 * 30);

                    StagingDbConnection.Execute(
                            $@"update	a set 
		                        Value = b.Value,
		                        IsEncrypted = 0
                        from	CustomerProperty a 
                        join	[{custTempTable.TempTableName}] b on a.CustomerId = b.CustomerId and a.Name = b.Name", transaction: tr, commandTimeout: 60 * 30);
                }
                
                //Remove all others
                StagingDbConnection.Execute("delete from CustomerProperty where Name not in @names", param: new { names = updateFieldNames }, transaction: tr, commandTimeout: 60 * 30);

                //Update search terms                
                var searchTerms = customerPropertyStagingItems
                    .Where(x => x.SearchValues != null && x.SearchValues.Count > 0)
                    .SelectMany(x => x.SearchValues.Select(y => new
                    {
                        CustomerId = x.CustomerId,
                        TermCode = x.Name,
                        Value = y,
                        IsActive = true,
                        ChangedById = 0,
                        ChangedDate = now,
                        InformationMetaData = (string)null
                    })).ToList();
                StagingDbConnection.Execute("INSERT INTO [dbo].[CustomerSearchTerm] ([CustomerId], [TermCode] ,[Value] ,[IsActive] ,[ChangedById] ,[ChangedDate], [InformationMetaData]) values (@CustomerId, @TermCode ,@Value ,@IsActive , @ChangedById , @ChangedDate, @InformationMetaData)",
                    param: searchTerms,
                    transaction: tr,
                    commandTimeout: 60 * 30);

                dbTr.Commit();
                tr.Commit();
            }
        }

        private string ComputeCivicRegnrHash(ICivicRegNumber civicRegNr)
        {
            using (var h = SecurityDriven.Inferno.SuiteB.HashFactory())
            {
                return Convert.ToBase64String(h.ComputeHash(SecurityDriven.Inferno.Utils.SafeUTF8.GetBytes(civicRegNr.NormalizedValue)));
            }
        }

        public string ComputeAddressHash(string addressStreet, string addressZipcode, string addressCity, string addressCountry)
        {
            Func<string[], string> getHash = input =>
            {
                var s = string.Join("", input.Select(x => x?.Trim()?.ToLowerInvariant()));
                MD5 md5Hash = MD5.Create();
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(s));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString();
            };

            return getHash(new[] { addressStreet, addressZipcode, addressCity, addressCountry });
        }


        private static string GetMd5HashForTranslateSearchTermValue(params string[] input)
        {
            var s = string.Concat(input.Select(x => x?.Trim()?.ToLowerInvariant()));
            MD5 md5Hash = MD5.Create();
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(s));
            return Convert.ToBase64String(data);
        }

        private List<string> TranslateSearchTermValue(string term, string value)
        {
            if (term == "email")
            {
                return new List<string> { GetMd5HashForTranslateSearchTermValue(value) };
            }
            else if (term == "firstName" || term == "lastName")
            {
                var generator = new Phonix.DoubleMetaphone();
                return value.Split(new char[0]).Where(y => !string.IsNullOrWhiteSpace(y)).Select(y => generator.BuildKey(y)).ToList();
            }
            else
                throw new NotImplementedException();
        }

        private class CustomerPropertyStagingItem
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public int CustomerId { get; set; }
            public List<string> SearchValues { get; set; }
        }

        private class CustomerIdSequenceStagingItem
        {
            public int CustomerId { get; set; }
            public string CivicRegNrHash { get; set; }
        }
    }
}
