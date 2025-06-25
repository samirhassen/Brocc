using System;
using static nDocument.NEnv;

namespace nDocument.Code.Archive
{
    public static class ArchiveProviderFactory
    {
        public static IArchiveProvider Create()
        {
            var realProvider = CreateI();
            return IsHardeningEnabled ? new HardenedArchiveProvider(realProvider) : realProvider;
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
            return errorMessage != null &&
                   errorMessage.IsOneOf(HardenedArchiveProvider.FileTypeNotAllowedCode);
        }

        private static IArchiveProvider CreateI()
        {
            var providerName = StorageProvider;
            switch (providerName)
            {
                case StorageProviderCode.Azure: return new AzureArchiveProvider();
                case StorageProviderCode.Aws: return new AwsArchiveProvider();
                case StorageProviderCode.Disk: return new DiskArchiveProvider();
                case StorageProviderCode.Sqlite:
                    return new SqliteArchiveProvider(() => SqliteStorageProviderDatabaseFile);
                default:
                    throw new NotImplementedException();
            }
        }

        private static IArchiveProvider CreateBackupI()
        {
            var providerName = BackupStorageProvider;
            switch (providerName)
            {
                case StorageProviderCode.Azure: return new AzureArchiveProvider();
                case StorageProviderCode.Aws: return new AwsArchiveProvider();
                case StorageProviderCode.Disk: return new DiskArchiveProvider();
                case StorageProviderCode.Sqlite:
                    return new SqliteArchiveProvider(() => SqliteStorageProviderDatabaseFile);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}