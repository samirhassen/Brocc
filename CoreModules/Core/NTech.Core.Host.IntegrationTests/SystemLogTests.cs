using NTech.Core.Host.Infrastructure;
using NTech.Core.Host.IntegrationTests.UlLegacy;
using NTech.Core.Module.Infrastrucutre.HttpClient;
using NTech.Core.Module.Shared.Clients;

namespace NTech.Core.Host.IntegrationTests
{
    internal class SystemLogTests
    {
        [Test]
        public void TestSystemLogPerformance()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var connectionString = support.CreateCustomerContextFactory().CreateContext().GetConnection().ConnectionString;
                var items = Enumerable.Range(1, 20000).Select(i => new AuditClientSystemLogItem 
                { 
                    EventDate = DateTimeOffset.Now.AddMinutes(-i),
                    Exception = i % 10 == 0 ? "Faili fail fail fsfsdfwerewqrf r32r234r234" : null,
                    Level = i % 10 == 0 ? "Error" : "Information",
                    Message = $"Message {i}",
                    Properties = new Dictionary<string, string> { { "i", i.ToString() } }
                }).ToList();
                var service = new SystemLogService(connectionString);
                var s = new ServiceClientSyncConverterCore();
                s.ToSync(() => service.LogBatchAsync(items));
                s.ToSync(() => service.TrimLogsAsync(null));
            });
        }
    }
}
