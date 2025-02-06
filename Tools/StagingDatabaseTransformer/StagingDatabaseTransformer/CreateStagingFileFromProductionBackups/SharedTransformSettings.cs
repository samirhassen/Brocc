using NTech.Services.Infrastructure;
using nTest.RandomDataSource;
using System.IO;

namespace StagingDatabaseTransformer
{
    public class SharedTransformSettings
    {
        public string TempDatabaseName { get; set; }
        public ClientConfiguration ClientCfg { get; set; }
        public SqliteDocumentDatabase Db { get; set; }
        public IRandomnessSource Random { get; set; }
        public DirectoryInfo BackupTargetFolder { get; set; }
        public DevelopmentDatabaseBackupManager BackupManager { get; set; }
        public int? ReplacementUserId { get; set; }
        public bool DisableMigrationHistoryCheck { get; set; }
        public string RestoreToDataFilesPath { get; }

        public SharedTransformSettings(ClientConfiguration clientCfg, IRandomnessSource random, SqliteDocumentDatabase db, DirectoryInfo backupTargetFolder, DevelopmentDatabaseBackupManager backupManager, int? replacementUserId, bool disableMigrationHistoryCheck, string restoreToDataFilesPath)
        {
            this.ClientCfg = clientCfg;
            this.Db = db;
            this.Random = random;
            this.BackupTargetFolder = backupTargetFolder;
            this.BackupManager = backupManager;
            this.ReplacementUserId = replacementUserId;
            this.DisableMigrationHistoryCheck = disableMigrationHistoryCheck;
            RestoreToDataFilesPath = restoreToDataFilesPath;
        }
    }
}
