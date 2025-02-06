using System.Collections.Generic;

namespace nCustomer.Code.Services.Kyc.Mock
{
    public class MultiMockKycScreeningProviderService : IKycScreeningProviderService
    {
        public IDictionary<string, List<KycScreeningListHit>> Query(List<KycScreeningQueryItem> items, KycScreeningListCode list = KycScreeningListCode.All)
        {
            var hits = new Dictionary<string, List<KycScreeningListHit>>();
            foreach (var item in items)
            {
                var seed = 10000 * item.BirthDate.Year + 100 * item.BirthDate.Day + item.BirthDate.Day;
                var isPep = IsFixedPepHit(item);
                var isSanction = IsFixedSanction(item);
                var m = new MockKycScreeningProviderService(isPep, isSanction, false, seed);
                var result = m.Query(new List<KycScreeningQueryItem>() { item }, list: list);
                foreach (var itemHit in result)
                {
                    hits.Add(itemHit.Key, itemHit.Value);
                }
            }
            return hits;
        }

        public static bool IsFixedPepHit(KycScreeningQueryItem i)
        {
            return i.FirstName?.ToLowerInvariant() == "peptest" || (i.Email?.Contains("peptest") ?? false);
        }

        public static bool IsFixedSanction(KycScreeningQueryItem i)
        {
            return i.FirstName?.ToLowerInvariant() == "sanctiontest" || i.FirstName?.ToLowerInvariant() == "sanktiontest"
                || (i.Email?.Contains("sanktiontest") ?? false) || (i.Email?.Contains("sanctiontest") ?? false);
        }
    }
}