using Newtonsoft.Json;
using NTech;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace nCreditReport
{
    public class AddressLookupCacheRepository
    {
        protected readonly string currentEncryptionKeyName;
        protected readonly IDictionary<string, string> encryptionKeysByName;

        private class CacheDataV1
        {
            public Dictionary<string, string> D { get; set; }

            public static Dictionary<string, string> FromString(string s)
            {
                return JsonConvert.DeserializeObject<CacheDataV1>(s)?.D;
            }

            public static string ToString(Dictionary<string, string> d)
            {
                return JsonConvert.SerializeObject(new CacheDataV1 { D = d });
            }
        }

        public AddressLookupCacheRepository(
            string currentEncryptionKeyName,
            IDictionary<string, string> encryptionKeysByName)
        {
            this.currentEncryptionKeyName = currentEncryptionKeyName;
            this.encryptionKeysByName = encryptionKeysByName;
        }

        public void StoreCachedResult(int customerId, string providerName, Dictionary<string, string> items, TimeSpan maxAge)
        {
            using (var context = new CreditReportContext())
            {
                var tx = context.Database.BeginTransaction();
                try
                {
                    context.Database.ExecuteSqlCommand(
                        "insert into AddressLookupCachedResult (ProviderName, CustomerId, RequestDate, EncryptionKeyName, DeleteAfterDate, EncryptedData) values (@ProviderName, @CustomerId, @RequestDate, @EncryptionKeyName, @DeleteAfterDate, EncryptByPassphrase(@PassPhrase, CONVERT(varbinary(max), @Value)))",
                        new SqlParameter("@ProviderName", providerName),
                        new SqlParameter("@CustomerId", customerId),
                        new SqlParameter("@RequestDate", ClockFactory.SharedInstance.Now),
                        new SqlParameter("@EncryptionKeyName", currentEncryptionKeyName),
                        new SqlParameter("@DeleteAfterDate", ClockFactory.SharedInstance.Now.Add(maxAge)),
                        new SqlParameter("@PassPhrase", encryptionKeysByName[currentEncryptionKeyName]),
                        new SqlParameter("@Value", CacheDataV1.ToString(items)));

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public Dictionary<string, string> GetCachedResult(int customerId, string providerName, TimeSpan maxAge)
        {
            using (var context = new CreditReportContext())
            {
                var oldestAllowedDate = ClockFactory.SharedInstance.Now.Subtract(maxAge);

                var hit = context
                    .AddressLookupCachedResults.Where(x => x.CustomerId == customerId && x.ProviderName == providerName && x.RequestDate >= oldestAllowedDate)
                    .OrderByDescending(y => y.Id)
                    .Select(x => new { x.Id, x.EncryptionKeyName })
                    .FirstOrDefault();

                if (hit == null)
                    return null;

                List<SqlParameter> parameters = new List<SqlParameter>();
                parameters.Add(new SqlParameter("@passphrase", encryptionKeysByName[hit.EncryptionKeyName]));
                parameters.Add(new SqlParameter("@id", hit.Id));

                var d = context.Database.SqlQuery<string>(
                    @"select  convert(nvarchar(max), DecryptByPassphrase(@passphrase, a.EncryptedData))
                        from   AddressLookupCachedResult a
                        where  a.Id = @id", parameters.ToArray())
                        .Single();

                return CacheDataV1.FromString(d);
            }
        }
    }
}