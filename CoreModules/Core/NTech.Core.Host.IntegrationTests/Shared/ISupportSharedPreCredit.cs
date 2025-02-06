
using nPreCredit;
using nPreCredit.Code.Services;
using NTech.Core.PreCredit.Database;

namespace NTech.Core.Host.IntegrationTests.Shared
{
    public interface ISupportSharedPreCredit
    {
        IPreCreditEnvSettings PreCreditEnvSettings { get; }
        IPreCreditContextFactoryService PreCreditContextService { get;}
    }

    public static class ISupportSharedPreCreditExtensions
    {
        public static TReturn WithPreCreditDb<TReturn>(this ISupportSharedPreCredit source, Func<PreCreditContextExtended, TReturn> f)
        {
            using (var context = (PreCreditContextExtended)source.PreCreditContextService.CreateExtended())
            {
                return f(context);
            }
        }
    }
}
