using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlxMigrator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if(args.Length != 1)
            {
                Console.WriteLine(@"The only argument should be the migration settings file. Something like 'C:\Naktergal\SlxMigrationSettings.txt'");
                return;
            }
            var settingsFile = args[0];

            var elapsed = Stopwatch.StartNew();

            var settings = NTechSimpleSettings.ParseSimpleSettingsFile(settingsFile, forceFileExistance: true);
            var rootDir = settings.Req("RootFolder");
            Directory.CreateDirectory(rootDir);
            var currentDir = Path.Combine(settings.Req("RootFolder"), "Current");
            var batchSize = int.Parse(settings.Opt("BatchSize") ?? "20");
            var maxNrOfBatchesPerRunRaw = settings.Opt("MaxNrOfBatchesPerRun");
            var maxNrOfBatchesPerRun = maxNrOfBatchesPerRunRaw == null ? new int?() : int.Parse(maxNrOfBatchesPerRunRaw);
            var startAtCustomerIdRaw = settings.Opt("StartAtCustomerId");
            var startAtCustomerId = startAtCustomerIdRaw == null ? new int?() : int.Parse(startAtCustomerIdRaw);
            var includeLoans = (settings.Opt("OnlyProduct") ?? "All").IsOneOfIgnoreCase("All", "Credit", "Loans");
            var includeSavings = (settings.Opt("OnlyProduct") ?? "All").IsOneOfIgnoreCase("All", "Savings");

            var connectionsFactory = new ConnectionFactory(settings);

            using(var crossRunDb = new CrossRunCacheDb(rootDir, connectionsFactory))
            using(var migrationDb = new MigrationDb(currentDir))
            {
                var loansMigrator = !includeLoans ? null :
                        new LoansMigrator(connectionsFactory, 
                        settings.Req("EncryptionKeyName"), 
                        settings.Req("EncryptionKeyValue"), migrationDb, crossRunDb);

                var savingsMigrator = !includeSavings ? null :
                        new SavingsMigrator(connectionsFactory, settings.Req("EncryptionKeyName"), settings.Req("EncryptionKeyValue"), migrationDb, crossRunDb);

                var status = migrationDb.Get("Status");

                bool wasPending = false;
                if(status == "PendingSetup")
                {
                    wasPending = true;
                    Console.WriteLine("Starting new migration");
                    StartNewRun(currentDir, migrationDb, startAtCustomerId, loansMigrator, savingsMigrator);
                }
                status = migrationDb.Get("Status");

                if (status == "InProgress")
                {
                    if(!wasPending)
                        Console.WriteLine($"Continuing existing migration started at {migrationDb.Get("StartDate")}");

                    var hasMoreBatches = false;

                    if (includeLoans)
                    {
                        Console.WriteLine("Migrating loans");
                        crossRunDb.EnsureCreditInitialEffectiveInterestRate();
                        slx_loan_applications.EnsureCurrentCreditDecisionCache(crossRunDb, connectionsFactory);
                        var hasMoreLoanBatches = CreateBatchesReturningHasMore(batchSize, maxNrOfBatchesPerRun, migrationDb, loansMigrator, currentDir, true);
                        if (hasMoreLoanBatches)
                            hasMoreBatches = true;
                    }
                    
                    if(includeSavings)
                    {
                        Console.WriteLine("Migrating savings accounts");
                        var hasMoreSavingsBatches = CreateBatchesReturningHasMore(batchSize, maxNrOfBatchesPerRun, migrationDb, savingsMigrator, currentDir, false);
                        if (hasMoreSavingsBatches)
                            hasMoreBatches = true;
                    }

                    if(hasMoreBatches)
                    {
                        Console.WriteLine("Stopping with customers left to migrate");
                    }
                    else
                    {
                        migrationDb.Set("Status", "Done");
                        Console.WriteLine("Migration done");
                    }
                }
                else if (status == "Done")
                {
                    Console.WriteLine("Migration done");
                }
                else
                    throw new Exception($"Unexpected status: {status}");
                
                Console.WriteLine($"Time taken in seconds: {(int)Math.Round(elapsed.Elapsed.TotalSeconds)}");
            }
        }

        private static void StartNewRun(string currentDir, MigrationDb db, int? startAtCustomerId, LoansMigrator loansMigrator, SavingsMigrator savingsMigrator)
        {
            Directory.CreateDirectory(currentDir);

            loansMigrator?.AddCustomersToMigration(startAtCustomerId);
            savingsMigrator?.AddCustomersToMigration(startAtCustomerId);
            
            db.Set("Status", "InProgress");
        }

        private static bool CreateBatchesReturningHasMore(int batchSize, int? maxNrOfBatchesPerRun, MigrationDb db, IMigrator migrator, string currentDir, bool isLoan)
        {
            var outDir = Path.Combine(currentDir, "Out");
            Directory.CreateDirectory(outDir);
            var batchCount = 0;
            while(!maxNrOfBatchesPerRun.HasValue || batchCount < maxNrOfBatchesPerRun.Value)
            {
                var hasMore = db.WithCustomersIdsToMigrateBatch(batchSize, isLoan, x =>
                {
                    Console.WriteLine($"Migrating batch with {x.CustomerIds.Count} {(isLoan ? "Loan" : "Savings")} customers. Total progress {x.CountAfter} / {x.TotalCount}");
                    var file = migrator.CreateLoansFileCustomers(x.CustomerIds);
                    File.WriteAllText(Path.Combine(outDir, x.FileName), file.ToString(Newtonsoft.Json.Formatting.Indented));
                    Console.WriteLine($"Created file: {x.FileName}");
                });
                if(!hasMore)
                {
                    return false;
                }
                batchCount++;
            }
            return true;
        }
    }
}
