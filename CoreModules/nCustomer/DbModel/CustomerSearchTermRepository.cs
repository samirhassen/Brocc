using nCustomer.Code;
using nCustomer.DbModel;
using NTech;
using NTech.Legacy.Module.Shared.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace nCustomer
{
    public class CustomerSearchTermRepository
    {
        private readonly Func<CustomersContext> createContext;
        private NtechCurrentUserMetadata currentUser;
        private IClock clock;

        public CustomerSearchTermRepository(
            Func<CustomersContext> createContext,
            NtechCurrentUserMetadata currentUser,
            IClock clock) //http://security.stackexchange.com/questions/59580/how-to-safely-store-sensitive-data-like-a-social-security-number
        {
            this.createContext = createContext;
            this.currentUser = currentUser;
            this.clock = clock;
        }

        private void RepairSearchTerm(CustomerProperty.Codes searchTermCode)
        {
            const int BatchSize = 2000;
            int guard = 0;
            while (guard < 100)
            {
                var count = RepairSearchTermBatch(searchTermCode, BatchSize);

                if (count == 0)
                    return;

                if (count < BatchSize)
                    guard++;
            }
            if (guard > 90)
                throw new Exception("Hit guard code. Try running again, something seems to be updating as fast as we can repair.");
        }

        private int RepairSearchTermBatch(CustomerProperty.Codes searchTermCode, int batchSize)
        {
            var w = Stopwatch.StartNew();
            int? count = null;
            try
            {
                using (var db = createContext())
                using (var tr = db.Database.BeginTransaction())
                {
                    var allValues = db
                        .CustomerProperties
                        .Where(x =>
                            x.IsCurrentData
                            && x.Name == searchTermCode.ToString()
                            && !db.CustomerSearchTerms.Any(y => y.CustomerId == x.CustomerId && y.IsActive && y.TermCode == searchTermCode.ToString())
                            )
                        .Select(x => new { x.CustomerId, x.IsEncrypted, x.Value })
                        .Take(batchSize)
                        .ToList();

                    count = allValues.Count;

                    if (allValues.Count == 0)
                        return 0;

                    var encryptedValues = allValues.Where(x => x.IsEncrypted).ToList();
                    IDictionary<long, string> decryptedValues = null;
                    if (encryptedValues.Any())
                    {
                        decryptedValues = EncryptionContext.Load(db, encryptedValues.Select(x => long.Parse(x.Value)).ToArray(), NEnv.EncryptionKeys.AsDictionary());
                    }

                    CustomerSearchTerms.OnCustomerPropertiesAddedShared(db, currentUser.CoreUser, CoreClock.SharedInstance, NEnv.ClientCfgCore, allValues.Select(x => new SearchTermUpdateItem
                    {
                        CustomerId = x.CustomerId,
                        PropertyName = searchTermCode.ToString(),
                        ClearTextValue = x.IsEncrypted ? decryptedValues[long.Parse(x.Value)] : x.Value
                    }).ToArray());

                    db.SaveChanges();
                    tr.Commit();

                    return allValues.Count;
                }
            }
            finally
            {
                w.Stop();
                NLog.Information($"RepairSearchTermBatch - {searchTermCode}: {count} properties in {(int)Math.Round(w.Elapsed.TotalMilliseconds)}ms");
            }
        }

        public void RepairSearchTerms()
        {
            foreach (var code in new[] { CustomerProperty.Codes.email, CustomerProperty.Codes.firstName, CustomerProperty.Codes.lastName, CustomerProperty.Codes.phone })
            {
                var w = Stopwatch.StartNew();
                try
                {
                    RepairSearchTerm(code);
                }
                finally
                {
                    w.Stop();
                    NLog.Information($"CustomerRepairSearchTerms - {code}: {(int)Math.Round(w.Elapsed.TotalMilliseconds)}ms");
                }
            }
        }
    }
}