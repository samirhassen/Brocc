using Nito.AsyncEx;
using NTech.Core.Module.Shared.Clients;

namespace NTech.Core.Module.Infrastrucutre.HttpClient
{
    public class ServiceClientSyncConverterCore : IServiceClientSyncConverter
    {
        public TResult ToSync<TResult>(Func<Task<TResult>> action) => AsyncContext.Run(action);
        public void ToSync(Func<Task> action) => AsyncContext.Run(action);
    }
}
