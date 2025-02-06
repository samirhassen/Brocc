using System;
using static nDocument.NEnv;

namespace nDocument.Code.Archive
{
    public static class ArchiveProviderFactory
    {
        public static IArchiveProvider Create()
        {
            var realProvider = CreateI();
            if (NEnv.IsHardeningEnabled)
            {
                return new HardenedArchiveProvider(realProvider);
            }
            else
                return realProvider;
        }

        //Used when migrating providers. Backup provider = provider we are migrating from.
        public static bool IsBackupProviderSet = NEnv.IsBackupProviderSet; 

        public static IArchiveProvider CreateBackup()
        {
            var backupProvider = CreateBackupI();

            return backupProvider;
        }

        public static bool IsKnownCodedErrorMessage(string errorMessage)
        {
            return errorMessage != null && errorMessage.IsOneOf(Code.Archive.HardenedArchiveProvider.FileTypeNotAllowedCode);
        }

        private static IArchiveProvider CreateI()
        {
            var providerName = NEnv.StorageProvider;
            switch (providerName)
            {
                case NEnv.StorageProviderCode.Azure: return new AzureArchiveProvider();
                case NEnv.StorageProviderCode.Aws: return new AwsArchiveProvider(); 
                case NEnv.StorageProviderCode.Disk: return new DiskArchiveProvider();
                case NEnv.StorageProviderCode.Sqlite: return new SqliteArchiveProvider(() => NEnv.SqliteStorageProviderDatabaseFile);
                default:
                    throw new NotImplementedException();
            }
        }

        private static IArchiveProvider CreateBackupI()
        {
            var providerName = NEnv.BackupStorageProvider;
            switch (providerName)
            {
                case NEnv.StorageProviderCode.Azure: return new AzureArchiveProvider();
                case NEnv.StorageProviderCode.Aws: return new AwsArchiveProvider();
                case NEnv.StorageProviderCode.Disk: return new DiskArchiveProvider();
                case NEnv.StorageProviderCode.Sqlite: return new SqliteArchiveProvider(() => NEnv.SqliteStorageProviderDatabaseFile);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}