using Nito.AsyncEx;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Threading.Tasks;

namespace NTech.Legacy.Module.Shared.Infrastructure.HttpClient
{
    public class ServiceClientSyncConverterLegacy : IServiceClientSyncConverter
    {
        public TResult ToSync<TResult>(Func<Task<TResult>> action)
        {
            return AsyncContext.Run(() => action());
        }

        public void ToSync(Func<Task> action) => AsyncContext.Run(action);
    }
}
