using Dapper;
using Microsoft.Data.SqlClient;
using NTech.Core.Module.Shared.Clients;
using System.Data;

namespace NTech.Core.Host.Infrastructure
{
    public class SystemLogService
    {
        private readonly string connectionString;
        private readonly int informationRetentionDays = 14;
        private readonly int warningRetentionDays = 60;
        private readonly int errorRetentionDays = 180;
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
            using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync(
@"INSERT INTO [dbo].[SystemLogItem] 
([Level],[EventDate],[EventType],[ServiceName],[ServiceVersion],[RequestUri],[RemoteIp],[UserId],[Message],[ExceptionMessage],[ExceptionData])
VALUES
(@Level, @EventDate, @EventType, @ServiceName, @ServiceVersion, @RequestUri, @RemoteIp, @UserId, @Message, @ExceptionMessage, @ExceptionData)", commandTimeout: 30, param: items);
        }

        public async Task TrimLogsAsync(int? maxTrimCount)
        {
            EnsureSetup();

            using var connection = new SqlConnection(connectionString);
            async Task Trim(string level, int retentionDays)
            {
                var top = maxTrimCount.HasValue ? $"top {maxTrimCount.Value}" : "";
                await connection.ExecuteAsync($"delete from SystemLogItem where Id in(select {top} Id from SystemLogItem where [Level] = @level and EventDate < getdate() - {retentionDays})", commandTimeout: 180,
                    param: new { level });
            }
            await Trim("Information", informationRetentionDays);
            await Trim("Warning", warningRetentionDays);
            await Trim("Error", errorRetentionDays);
        }

        public void EnsureSetup()
        {
            void RunSetup()
            {
                using var connection = new SqlConnection(connectionString);

                if (connection.Query<int>(@"select case when OBJECT_ID(N'dbo.SystemLogItem', N'U') is null then 0 else 1 end").Single() > 0)
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
                connection.Execute("CREATE INDEX [IX_TrimSupport] ON [dbo].[SystemLogItem] ([Level], [EventDate]) INCLUDE (Id)");
            }

            if (isSetupDone)
                return;

            lock(setupLock)
            {
                if (isSetupDone)
                    return;

                RunSetup();

                isSetupDone = true;
            }
        }

        public async Task<List<SystemLogItem>> FetchLatestErrorsAsync(int page = 0)
        {
            EnsureSetup();

            using var connection = new SqlConnection(connectionString);
            var result = await connection.QueryAsync<SystemLogItem>(@"with SystemLogItemExtended
as
(
	select	s.*,
			ROW_NUMBER() OVER (ORDER BY s.Id desc) AS RowNumber
	from	SystemLogItem s
)
select	top 20 s.*
from	SystemLogItemExtended s
where	s.RowNumber > @page * 20
and		s.[Level] = 'Error'
order by s.RowNumber", param: new { page });
            return result.ToList();
        }        

        private static SystemLogItem ToSystemLogItem(AuditClientSystemLogItem x)
        {
            Func<string, string> emptyToNull = s => string.IsNullOrWhiteSpace(s) ? null : s;
            if (x.Properties == null)
                x.Properties = new Dictionary<string, string>(1);

            List<string> usedProperties = new List<string>();
            Func<IDictionary<string, string>, string, string> prop = (d, n) =>
            {
                usedProperties.Add(n);
                if (!d.ContainsKey(n))
                    return null;
                var v = d[n];
                if (string.IsNullOrWhiteSpace(v))
                    return null;
                if (v.StartsWith("\"") && v.EndsWith("\""))
                    v = v.Substring(1, v.Length - 2);
                return v;
            };

            Func<string, int, string> clipLeft = (s, n) =>
            {
                if (s == null)
                    return null;
                if (s.Length > n)
                    return s.Substring(s.Length - n);
                else
                    return s;
            };
            var now = DateTimeOffset.Now;
            return new SystemLogItem
            {
                EventDate = x.EventDate,
                Level = x.Level,
                Message = emptyToNull(x.Message),
                RemoteIp = prop(x.Properties, "RemoteIp"),
                RequestUri = clipLeft(StripRequestUri(prop(x.Properties, "RequestUri")), 128),
                ServiceName = prop(x.Properties, "ServiceName"),
                ServiceVersion = prop(x.Properties, "ServiceVersion"),
                UserId = prop(x.Properties, "UserId"),
                EventType = clipLeft(prop(x.Properties, "EventType"), 128),
                ExceptionMessage = emptyToNull(x.Exception),
                ExceptionData = GetExceptionData(x, usedProperties)
            };
        }

        private static string StripRequestUri(string uri)
        {
            try
            {
                if (uri == null)
                {
                    return null;
                }
                else if (!uri.StartsWith("/"))
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

        private static IDictionary<string, string> FilterProperties(IDictionary<string, string> p, IList<string> names, params string[] additionalNames)
        {
            if (p == null)
                return p;
            else
                return p.Where(x => !names.Contains(x.Key) && !additionalNames.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
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
}
