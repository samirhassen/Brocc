using Dapper;
using Microsoft.Data.SqlClient;
using NTech.Core.Module.Shared.Clients;

namespace NTech.Core.Host.Infrastructure;

public class SystemLogService
{
    private readonly string connectionString;
    private const int InformationRetentionDays = 14;
    private const int WarningRetentionDays = 60;
    private const int ErrorRetentionDays = 180;

    private readonly object setupLock = new object();
    private bool isSetupDone = false;

    public SystemLogService(string connectionString)
    {
        this.connectionString = connectionString;
    }

    //Used by the module setup code to prevent race conditions where something tries to log before the logging database even exists or similar strange things.
    public bool IsPendingStartup { get; set; } = false;

    public async Task LogBatchAsync(List<AuditClientSystemLogItem> logItems)
    {
        if (IsPendingStartup)
            return;

        EnsureSetup();

        var items = logItems.Select(ToSystemLogItem);
        await using var connection = new SqlConnection(connectionString);
        await connection.ExecuteAsync(@"
INSERT INTO [dbo].[SystemLogItem] 
  ([Level], [EventDate], [EventType], [ServiceName], [ServiceVersion], [RequestUri], [RemoteIp], [UserId], [Message], [ExceptionMessage], [ExceptionData])
VALUES 
  (@Level, @EventDate, @EventType, @ServiceName, @ServiceVersion, @RequestUri, @RemoteIp, @UserId, @Message, @ExceptionMessage, @ExceptionData)",
            commandTimeout: 30, param: items);
    }

    public async Task TrimLogsAsync(int? maxTrimCount)
    {
        EnsureSetup();

        await using var connection = new SqlConnection(connectionString);

        await Trim("Information", InformationRetentionDays, connection);
        await Trim("Warning", WarningRetentionDays, connection);
        await Trim("Error", ErrorRetentionDays, connection);

        return;

        async Task Trim(string level, int retentionDays, SqlConnection conn)
        {
            var top = maxTrimCount.HasValue ? $"top {maxTrimCount.Value}" : "";
            await conn.ExecuteAsync(
                $"delete from SystemLogItem where Id in(select {top} Id from SystemLogItem where [Level] = @level and EventDate < getdate() - {retentionDays})",
                commandTimeout: 180,
                param: new { level });
        }
    }

    public void EnsureSetup()
    {
        lock (setupLock)
        {
            if (isSetupDone)
                return;

            RunSetup();

            isSetupDone = true;
        }

        return;

        void RunSetup()
        {
            using var connection = new SqlConnection(connectionString);

            if (connection.Query<int>(
                        "SELECT case WHEN OBJECT_ID(N'dbo.SystemLogItem', N'U') IS NULL THEN 0 ELSE 1 END")
                    .Single() >
                0)
                return;

            connection.Execute(@"
CREATE TABLE [dbo].[SystemLogItem](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Level] [nvarchar](15) NOT NULL,
	[EventDate] [datetimeoffset](7) NOT NULL,
	[EventType] [nvarchar](128) NULL,
	[ServiceName] [nvarchar](30) NULL,
	[ServiceVersion] [nvarchar](30) NULL,
	[RequestUri] [nvarchar](128) NULL,
	[RemoteIp] [nvarchar](30) NULL,
	[UserId] [nvarchar](128) NULL,
	[Message] [nvarchar](max) NULL,
	[ExceptionMessage] [nvarchar](max) NULL,
	[ExceptionData] [nvarchar](max) NULL,
 CONSTRAINT [PK_dbo.SystemLogItem] PRIMARY KEY CLUSTERED ([Id] ASC))");

            connection.Execute("CREATE INDEX [IX_EventDate] ON [dbo].[SystemLogItem]([EventDate] ASC)");
            connection.Execute("CREATE INDEX [IX_EventType] ON [dbo].[SystemLogItem]([EventType] ASC)");
            connection.Execute("CREATE INDEX [IX_Level] ON [dbo].[SystemLogItem]([Level] ASC)");
            connection.Execute(
                "CREATE INDEX [IX_TrimSupport] ON [dbo].[SystemLogItem] ([Level], [EventDate]) INCLUDE (Id)");
        }
    }

    public async Task<List<SystemLogItem>> FetchLatestErrorsAsync(int page = 0)
    {
        EnsureSetup();

        await using var connection = new SqlConnection(connectionString);
        var result = await connection.QueryAsync<SystemLogItem>(@"
WITH SystemLogItemExtended
AS
(
	SELECT s.*, ROW_NUMBER() OVER (ORDER BY s.Id desc) AS RowNumber
	from SystemLogItem s
)
SELECT TOP 20 s.* FROM SystemLogItemExtended s
WHERE s.RowNumber > @page * 20 AND s.[Level] = 'Error'
ORDER BY s.RowNumber
", param: new { page });
        return result.ToList();
    }

    private static SystemLogItem ToSystemLogItem(AuditClientSystemLogItem x)
    {
        x.Properties ??= new Dictionary<string, string>(1);

        var usedProperties = new List<string>();

        var now = DateTimeOffset.Now;
        return new SystemLogItem
        {
            EventDate = x.EventDate,
            Level = x.Level,
            Message = EmptyToNull(x.Message),
            RemoteIp = Prop(x.Properties, "RemoteIp"),
            RequestUri = ClipLeft(StripRequestUri(Prop(x.Properties, "RequestUri")), 128),
            ServiceName = Prop(x.Properties, "ServiceName"),
            ServiceVersion = Prop(x.Properties, "ServiceVersion"),
            UserId = Prop(x.Properties, "UserId"),
            EventType = ClipLeft(Prop(x.Properties, "EventType"), 128),
            ExceptionMessage = EmptyToNull(x.Exception),
            ExceptionData = GetExceptionData(x, usedProperties)
        };

        string ClipLeft(string s, int n)
        {
            if (s == null) return null;
            return s.Length > n ? s.Substring(s.Length - n) : s;
        }

        string Prop(IDictionary<string, string> d, string n)
        {
            usedProperties.Add(n);
            if (!d.TryGetValue(n, out var v)) return null;
            if (string.IsNullOrWhiteSpace(v)) return null;
            if (v.StartsWith("\"") && v.EndsWith("\"")) v = v.Substring(1, v.Length - 2);
            return v;
        }

        string EmptyToNull(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static string StripRequestUri(string uri)
    {
        if (uri == null) return null;
        try
        {
            if (!uri.StartsWith("/"))
            {
                uri = "/" + uri;
            }

            var u = new Uri(new Uri("http://localhost"), uri);
            return u.GetComponents(UriComponents.Path, UriFormat.Unescaped);
        }
        catch
        {
            return null;
        }
    }

    private static string GetExceptionData(AuditClientSystemLogItem x, List<string> usedProperties)
    {
        if (x.Level != "Error" || x.Properties == null)
            return null;
        var p = FilterProperties(x.Properties, usedProperties, "action", "controller", "MachineName");
        if (p.Count == 0)
            return null;
        return string.Join("; ", p.Select(y => $"{y.Key}={y.Value}"));
    }

    private static IDictionary<string, string> FilterProperties(IDictionary<string, string> p, IList<string> names,
        params string[] additionalNames)
    {
        return p?.Where(x => !names.Contains(x.Key) && !additionalNames.Contains(x.Key))
            .ToDictionary(x => x.Key, x => x.Value);
    }
}

public class SystemLogItem
{
    public int Id { get; set; }
    public string Level { get; set; }
    public DateTimeOffset EventDate { get; set; }
    public string EventType { get; set; }
    public string ServiceName { get; set; }
    public string ServiceVersion { get; set; }
    public string RequestUri { get; set; }
    public string RemoteIp { get; set; }
    public string UserId { get; set; }
    public string Message { get; set; }
    public string ExceptionMessage { get; set; }
    public string ExceptionData { get; set; }
}