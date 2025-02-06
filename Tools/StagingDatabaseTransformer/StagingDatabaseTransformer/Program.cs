using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace StagingDatabaseTransformer
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length != 1)
                    throw new Exception("Missing params file");
                var paramsFileName = args[0];

                var parameters = ParameterFile.ParseFromFile(new FileInfo(paramsFileName));

                var functionName = parameters.Parameters["functionName"];

                if (functionName.Equals("CreateStagingFileFromProductionBackups", StringComparison.InvariantCultureIgnoreCase))
                {
                    CreateStagingFileFromProductionBackups.CreateStagingFileFromProductionBackupsOperation.Run(parameters);
                }
                else if (functionName.Equals("RestoreDevelopmentDatabasesFromStagingFile", StringComparison.InvariantCultureIgnoreCase))
                {
                    RestoreDevelopmentDatabasesFromStagingFile.RestoreDevelopmentDatabasesFromStagingFileOperation.Run(parameters.Parameters);
                }
                else if (functionName.Equals("RestoreTestModuleFromStagingFile", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new Exception("This functionally has been removed in favor of just copying over the db file from a backup while the app pools are stopped");
                }
                else if(functionName.Equals("SetupServicesAfterRestore", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new Exception("This functionally has been removed in favor of just stopping and starting the app pool");
                }
                else if(functionName.Equals("UploadStagingFile", StringComparison.InvariantCultureIgnoreCase))
                {
                    UploadStagingFile.UploadStagingFileOperation.Run(parameters.Parameters);
                }
                else if (functionName.Equals("DownloadStagingFile", StringComparison.InvariantCultureIgnoreCase))
                {
                    DownloadStagingFile.DownloadStagingFileOperation.Run(parameters.Parameters);
                }
                else if(functionName.Equals("CreatePrefixBackupsOperation", StringComparison.InvariantCultureIgnoreCase))
                {
                    CreatePrefixBackupsOperation.Run(parameters.Parameters);
                }
                else if (functionName.Equals("RestorePrefixBackupsOperation", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new Exception("This has been migrated to RestorePrefixBackupsOperation2. Remove serviceNamesSetup. Instead add serviceNames which is the databases/services to both restore and setup and serviceNamesSetupOnly are the ones to setup and not restore (like user). Also add isForTest=true|false.");
                }
                else if (functionName.Equals("RestorePrefixBackupsOperation2", StringComparison.InvariantCultureIgnoreCase))
                {
                    RestorePrefixBackupsOperation.Run(parameters.Parameters);
                }
                else
                    throw new Exception($"Unknown functionName: {functionName}");

                return 0;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return -1;
            }
        }
    }
}
