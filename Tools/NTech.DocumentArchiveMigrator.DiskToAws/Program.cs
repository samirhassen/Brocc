using Microsoft.Extensions.Configuration;
using NTech.DocumentArchiveMigrator.DiskToAws;
using System.Diagnostics;

var builder = new ConfigurationBuilder();
builder.SetBasePath(Directory.GetCurrentDirectory())
       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
IConfiguration config = builder.Build();

var executionLog = new RotatingLogFile(Directory.GetCurrentDirectory(), "migrationLog");
void Log(string text)
{
    Console.WriteLine(text);
    executionLog.Log(text);
}

string RequireSetting(string name)
{
    var value = config[$"NTech:{name}"];
    if (string.IsNullOrWhiteSpace(value))
        throw new Exception($"Missing required appsetting {name}");
    return value;
}

try
{
    //This is the actual disk archive
    string archiveDirectory = RequireSetting("ArchiveDirectory");
    //Files that are moved to aws are moved here so that we can restore them in case there are issues. When everything is verified as working this can just be dropped.
    string backupDirectory = RequireSetting("BackupDirectory");

    int maxRuntimeInMinutes = int.Parse(RequireSetting("MaxRuntimeInMinutes"));

    List<string> GetMetadataFileNameBatch(int count)
    {
        //NOTE: The migration source can have millions of files (first use has ~4M) so we cant just do DirectoryInfo.GetFiles since that will take like all day just to enumerate the files
        //      This means that files can come in any order so make really, really sure that nothing is still writing to the ArchiveDirectory when running this.
        return Directory.EnumerateFiles(archiveDirectory).Where(x => x.EndsWith(".metadata.xml")).Take(count).ToList();
    }

    using DocumentMigrator migrator = new DocumentMigrator(archiveDirectory, backupDirectory, RequireSetting);

    Stopwatch w = Stopwatch.StartNew();
    int migratedCount = 0;
    while (w.Elapsed.TotalSeconds < (60 * maxRuntimeInMinutes))
    {
        var metadataFileNames = GetMetadataFileNameBatch(300);
        if (metadataFileNames.Count == 0) break;

        await Parallel.ForEachAsync(metadataFileNames,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = 4
            },
            async (metadataFileName, _) =>
            {
                await migrator.MigrateDocumentAsync(metadataFileName);
                Interlocked.Increment(ref migratedCount);
            });
    }
    w.Stop();

    Log($"Migrated {migratedCount} documents in {w.Elapsed.TotalSeconds:N2} seconds" 
        + Environment.NewLine
        + $"AWS time {migrator.AwsTimer.Elapsed.TotalSeconds:N2} seconds");
}
catch(Exception ex)
{
    Log("Error" + Environment.NewLine + ex.ToString());
}