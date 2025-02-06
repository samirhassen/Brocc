using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace nCustomer.Code.Services.Kyc.Mock
{
    public class MockKycScreeningProviderService : IKycScreeningProviderService
    {
        private Random rand;
        private bool shouldBePepHit;
        private bool shouldBeSanctionHit;
        private bool isMockDown;

        public MockKycScreeningProviderService(bool shouldBePepHit, bool shouldBeSanctionHit, bool isMockDown, int seed)
        {
            rand = new Random(seed);
            this.shouldBePepHit = shouldBePepHit;
            this.shouldBeSanctionHit = shouldBeSanctionHit;
            this.isMockDown = isMockDown;
        }

        public IDictionary<string, List<KycScreeningListHit>> Query(List<KycScreeningQueryItem> items, KycScreeningListCode list = KycScreeningListCode.All)
        {
            var d = new Dictionary<string, List<KycScreeningListHit>>();

            if (isMockDown)
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
                throw new TimeoutException();
            }

            if (!shouldBePepHit && !shouldBeSanctionHit)
                return d;

            if (shouldBePepHit)
            {
                foreach (var item in items)
                {
                    d[item.ItemId] = Enumerable.Range(1, rand.NextDouble() < 0.5d ? 1 : 2).Select(_ => RandomizeHit(true, item)).ToList();
                }
            }

            if (shouldBeSanctionHit)
            {
                foreach (var item in items)
                {
                    var newHits = Enumerable.Range(1, rand.NextDouble() < 0.5d ? 1 : 2).Select(_ => RandomizeHit(false, item)).ToList();

                    if (d.ContainsKey(item.ItemId))
                    {
                        d[item.ItemId].AddRange(newHits);
                    }
                    else
                    {
                        d[item.ItemId] = newHits;
                    }
                }
            }
            return d;
        }

        private string IsPepHitName(bool isPepHit)
        {
            return isPepHit ? "Pep" : "Sanction";
        }

        private KycScreeningListHit RandomizeHit(bool isPepHit, KycScreeningQueryItem item)
        {
            return new KycScreeningListHit
            {
                Name = GetRandomName(item, isPepHit),
                Addresses = GetRandomAddresses(item, isPepHit),
                BirthDate = rand.NextDouble() < 0.5d ? new DateTime?(item.BirthDate) : null,
                Comment = GetRandomComment(item, isPepHit),
                ExternalId = Guid.NewGuid().ToString(),
                ExternalUrls = GetRandomExternalUrls(item, isPepHit),
                SourceName = IsPepHitName(isPepHit) + "_NtechLocalTest",
                IsPepHit = isPepHit,
                IsSanctionHit = !isPepHit,
                Ssn = GetRandomSsn(item, isPepHit),
                Title = GetRandomTitle(item, isPepHit)
            };
        }

        private static string[] NameFragments = new[] { "Tuuli", "Eriksson", "Sanna-Leen", "Lenho", "Lempi", "Ollanketo", "Aliisa", "Aravirta", "Elsa", "Otila", "Kaarina", "Mantere" };

        private string GetRandomName(KycScreeningQueryItem item, bool isPepHit)
        {
            if (string.IsNullOrWhiteSpace(item.FullName))
                return null;

            var parts = item.FullName.Split(' ');
            return rand.NextDouble() < 0.3d
                ? item.FullName
                : (parts[0] + " " + NameFragments[rand.Next(0, NameFragments.Length)] + " " + string.Join(" ", parts.Skip(1))).Trim();
        }

        private static string[] PepTitles = new string[] { "Prime minister", "Member of parliament", "Supreme court Judge", "Central bank employee", "Diplomatic aide" };
        private static string[] SanctionTitles = new string[] { "Prime minister", "Suspected terrorist", "Suspected arms dealer" };

        private string GetRandomTitle(KycScreeningQueryItem item, bool isPepHit)
        {
            if (rand.NextDouble() < 0.5d)
                return null;
            else if (isPepHit)
                return PepTitles[rand.Next(0, PepTitles.Length)];
            else if (!isPepHit)
                return SanctionTitles[rand.Next(0, SanctionTitles.Length)];
            else
                throw new NotImplementedException();
        }

        private string GetRandomSsn(KycScreeningQueryItem item, bool isPepHit)
        {
            if (rand.NextDouble() < 0.8d)
                return null;
            else if (item.TwoLetterIsoCountryCodes != null && item.TwoLetterIsoCountryCodes.Contains("FI"))
            {
                var civicNrFiPrefix = item.BirthDate.ToString("ddMMyy") + (item.BirthDate.Year < 1900 ? "+" : item.BirthDate.Year < 2000 ? "-" : "A") + rand.Next(2, 900).ToString().PadLeft(3, ' ');
                Func<string, string> generateChecksum = userCivicRegNr =>
                {
                    int index = int.Parse(userCivicRegNr.Substring(0, 6) + userCivicRegNr.Substring(7, 3)) % 31;
                    return "0123456789ABCDEFHJKLMNPRSTUVWXY".Substring(index, 1);
                };
                return "FI" + civicNrFiPrefix + generateChecksum(civicNrFiPrefix);
            }
            else
                return null;
        }

        private List<string> GetRandomExternalUrls(KycScreeningQueryItem item, bool isPepHit)
        {
            if (rand.NextDouble() < 0.3d)
                return new List<string>();
            else
                return new List<string>()
                {
                    "http://example.org/" + IsPepHitName(isPepHit) + "/" + Guid.NewGuid().ToString()
                };
        }

        private string GetRandomComment(KycScreeningQueryItem item, bool isPepHit)
        {
            if (rand.NextDouble() < 0.3d || isPepHit) //Peps dont seem to have comments
                return null;
            else if (!isPepHit)
            {
                return "(Entity is sanctioned in regime legal document re veritatis et quasi architecto beatae vitae dicta sunt explicabo. Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit.)";
            }
            else
                throw new NotImplementedException();
        }

        private List<string> GetRandomAddresses(KycScreeningQueryItem item, bool isPepHit)
        {
            var r = rand.NextDouble();
            if (r < 0.3d)
                return new List<string>();
            else if (r < 0.8d && item.TwoLetterIsoCountryCodes != null && item.TwoLetterIsoCountryCodes.Any())
            {
                return new List<string>() { ISO3166.FromAlpha(item.TwoLetterIsoCountryCodes.First(), "en").Name };
            }
            else
                return new List<string>() { ISO3166.FromAlpha("FI", "en").Name };
        }
    }
}