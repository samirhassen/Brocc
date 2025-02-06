using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Legacy.Module.Shared
{
    public static class CacheHandler
    {
        public static void ClearAllCaches()
        {
            NTech.Services.Infrastructure.NTechCache.ClearCache();
            FewItemsCache.SharedInstance.ClearCache();
        }
    }
}
