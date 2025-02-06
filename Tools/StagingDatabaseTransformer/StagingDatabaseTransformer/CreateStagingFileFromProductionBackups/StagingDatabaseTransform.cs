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
using System.Text.RegularExpressions;
using NTech.Banking.CivicRegNumbers;

namespace StagingDatabaseTransformer
{
    public abstract class StagingDatabaseTransform : IDisposable
    {
        public StagingDatabaseTransform(SharedTransformSettings settings, string backupFilePath)
        {
            this.settings = settings;
            this.civicRegNumberParser = new CivicRegNumberParser(settings.ClientCfg.Country.BaseCountry);
            this.backupFilePath = backupFilePath;
        }

        protected abstract void DoTransform();
        protected abstract string ModuleName { get; }

        private readonly SharedTransformSettings settings;
        private readonly CivicRegNumberParser civicRegNumberParser;
        private readonly string backupFilePath;
        
        protected SharedTransformSettings Settings
        {
            get
            {
                return this.settings;
            }
        }

        protected IRandomnessSource Random
        {
            get
            {
                return this.settings.Random;
            }
        }
        
        protected SqliteDocumentDatabase TestDb
        {
            get
            {
                return this.settings.Db;
            }
        }

        protected ClientConfiguration ClientCfg
        {
            get
            {
                return this.settings.ClientCfg;
            }
        }

        protected CivicRegNumberParser CivicRegNumberParser
        {
            get
            {
                return this.civicRegNumberParser;
            }
        }

        protected SqlConnection StagingDbConnection { get; set; }
        protected string TempDatabaseName { get; set; }

        private string GetCurrentStagingStateName()
        {
            return StagingDbConnection.QueryFirst<string>("select top 1 StageName from StagingDatabaseState order by Id desc");
        }

        private void SetCurrentStagingStateName(StagingStateCode name)
        {
            StagingDbConnection.Execute("insert into StagingDatabaseState (StageName) values (@name)", new { name = name.ToString() });
        }

        private void RunStage(StagingStateCode initial, StagingStateCode running, StagingStateCode done, Action runStage, string stageName)
        {
            var currentStateName = GetCurrentStagingStateName();
            StagingStateCode state;
            if(!Enum.TryParse(currentStateName, out state))
            {
                throw new Exception($"Unknown state of staging database: {currentStateName}");
            }

            if (state != initial)
            {
                throw new Exception($"Invalid state of staging database: {state} != {initial.ToString()}");
            }

            SetCurrentStagingStateName(running);

            Console.WriteLine($"***** Running stage {ModuleName}: {stageName} *****");
            runStage();

            SetCurrentStagingStateName(done);
        }

        private void RemapInfrastructureBaseUserIds()
        {
            if(this.settings.ReplacementUserId.HasValue)
            {
                using (var tr = StagingDbConnection.BeginTransaction())
                {
                    var tableNames = this.StagingDbConnection.Query<string>(
                        @"select    c.TABLE_NAME
                    from    INFORMATION_SCHEMA.COLUMNS c
                    where   exists(select 1 from INFORMATION_SCHEMA.COLUMNS cc where cc.TABLE_NAME = c.TABLE_NAME and cc.COLUMN_NAME = 'InformationMetaData')
                    and     c.COLUMN_NAME = 'ChangedById'
                    and     c.DATA_TYPE = 'int'", transaction: tr);
                    foreach (var tableName in tableNames)
                    {
                        StagingDbConnection.Execute($"update [{tableName}] set ChangedById = @changedById", 
                            param: new { changedById = this.settings.ReplacementUserId.Value },
                            transaction: tr,
                            commandTimeout: 30 * 60);
                    }
                    DoAdditionalUserIdRemapping(tr);
                    tr.Commit();
                }
            }
        }

        protected virtual void DoAdditionalUserIdRemapping(SqlTransaction tr)
        {
            
        }
        
        private void DoAquire()
        {
            StagingDbConnection.Execute($"alter database [{TempDatabaseName}] set recovery simple", commandTimeout: 30 * 60);
            RemapInfrastructureBaseUserIds();
        }

        private void DoBackup()
        {
            //If access denied give access to the user sql server runs as. https://stackoverflow.com/questions/3960257/cannot-open-backup-device-operating-system-error-5
            var path = Path.Combine(this.settings.BackupTargetFolder.FullName, $"{ModuleName}.bak");
            StagingDbConnection.Execute($"BACKUP DATABASE [{TempDatabaseName}] TO DISK = '{path}' WITH COPY_ONLY, COMPRESSION, INIT", commandTimeout: 30* 60);
        }


        private void PrepareStagingDatabase()
        {
            StagingDbConnection.Execute("create table StagingDatabaseState (Id int identity not null primary key, StageName nvarchar(128) not null, StageDate datetime not null default(getdate()))");
            StagingDbConnection.Execute("insert into StagingDatabaseState (StageName) values ('RestoredFromBackup')");
        }

        public void Run()
        {
            this.settings.BackupManager.WithTemporaryDatabase(this.backupFilePath, (conn, databaseName) => 
            {
                StagingDbConnection = conn;
                TempDatabaseName = databaseName;
                try
                {
                    PrepareStagingDatabase();
                    RunStage(StagingStateCode.RestoredFromBackup, StagingStateCode.Aquiring, StagingStateCode.AquireDone, DoAquire, "Aquire");
                    RunStage(StagingStateCode.AquireDone, StagingStateCode.Transforming, StagingStateCode.TransformDone, DoTransform, "Transform");
                    RunStage(StagingStateCode.TransformDone, StagingStateCode.BackingUp, StagingStateCode.BackingUpDone, DoBackup, "Backup");
                }
                finally
                {
                    StagingDbConnection = null;
                    TempDatabaseName = null;
                }
            }, this.settings.RestoreToDataFilesPath, debugTempNamePrefix: ModuleName);
        }

        protected IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n);
            }
        }

        /// <summary>
        /// Check that the database being used as a source doesn't have a latest migration which
        /// is after the latest known verified one.
        /// 
        /// The reason to do this is to guard against the case where the things added in the later migrations contain
        /// sensitive data that would then just be blindly copied into the staging data.
        /// </summary>
        protected void RunMigrationHistoryCheck(string latestVerifiedMigrationId)
        {
            if(this.Settings.DisableMigrationHistoryCheck)
            {
                Console.WriteLine("Migration history check skipped because of settings");
                return;
            }

            Regex r = new Regex(@"(([0-9]+)_(.+))");

            var latestVerifiedMatch = r.Match((latestVerifiedMigrationId??"").Trim());
            if (!latestVerifiedMatch.Success)
                throw new Exception($"MigrationHistoryCheck in {ModuleName} failed: Invalid latestVerifiedMigrationId '{latestVerifiedMigrationId}'");
            var latestVerifiedVersionNr = long.Parse(latestVerifiedMatch.Groups[2].Value);
            var latestVerifiedVersionName = latestVerifiedMatch.Groups[3].Value;
            
            var currentDbMigrationId = StagingDbConnection.Query<string>(
                @"SELECT	top 1 MigrationId
                FROM [dbo].[__MigrationHistory]
                order by cast(LEFT(MigrationId, CHARINDEX ('_', MigrationId) - 1) as bigint) desc").FirstOrDefault();
            if (currentDbMigrationId == null)
                throw new Exception($"MigrationHistoryCheck in {ModuleName} failed: No migration history present in __MigrationHistory");

            var currentDbMatch = r.Match(currentDbMigrationId);
            var currentDbVersionNr = long.Parse(currentDbMatch.Groups[2].Value);
            var currentDbVersionName = currentDbMatch.Groups[3].Value;
            
            //Exact same version is ok
            if (currentDbVersionName.Equals(latestVerifiedVersionName, StringComparison.CurrentCulture))
                return;

            //The staging database is lagging which is not a problem either
            if (currentDbVersionNr < latestVerifiedVersionNr)
                return;

            throw new Exception($"MigrationHistoryCheck in {ModuleName} failed: Current migration '{currentDbVersionName}' > Latest verified migration '{latestVerifiedVersionName}'. If you are sure this can't lead to production data leaking to test this check can be disabled with the setting disableMigrationHistoryCheck [true|false]");
        }

        public void Dispose()
        {
            
        }
    }
}
