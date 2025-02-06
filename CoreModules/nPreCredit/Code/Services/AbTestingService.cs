using nPreCredit.DbModel;
using NTech;
using NTech.Core.PreCredit.Shared.Services;
using System;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class AbTestingService : IAbTestingService
    {
        private readonly PreCreditContextFactoryService preCreditContextFactoryService;
        private readonly IClock clock;
        private static readonly Random Random = new Random();
        private static readonly object RandomLockObject = new object();

        public AbTestingService(PreCreditContextFactoryService preCreditContextFactoryService, IClock clock)
        {
            this.preCreditContextFactoryService = preCreditContextFactoryService;
            this.clock = clock;
        }

        public ApplicationAbTestExperimentModel AssignExperimentOrNull()
        {
            using (var context = preCreditContextFactoryService.Create())
            {
                var e = GetCurrentActiveExperimentOrNullComposable(context, clock);
                if (e == null)
                    return null;
                var variationName = (GetRandomPercent() <= (e.VariationPercent ?? 50m)) ? e.VariationName : null;
                return new ApplicationAbTestExperimentModel
                {
                    ExperimentId = e.Id,
                    ExperimentName = e.ExperimentName,
                    HasVariation = !string.IsNullOrWhiteSpace(variationName),
                    VariationName = variationName
                };
            }
        }

        private ApplicationAbTestExperimentModel GetExperimentForApplicationOrNull(string applicationNr)
        {
            using (var context = preCreditContextFactoryService.Create())
            {
                return GetExperimentForApplicationOrNullComposable(context, applicationNr);
            }
        }

        public ITestingVariationSet GetVariationSetForApplication(string applicationNr)
        {
            var e = GetExperimentForApplicationOrNull(applicationNr);
            return e?.GetVariationSet() ?? new EmptyVariationSet();
        }

        public static AbTestingExperiment GetCurrentActiveExperimentOrNullComposable(IPreCreditContext context, IClock clock)
        {
            return FilterActiveExperiments(context.AbTestingExperiments, context.ComplexApplicationListItems, clock).FirstOrDefault();
        }

        public static ApplicationAbTestExperimentModel GetExperimentForApplicationOrNullComposable(IPreCreditContext context, string applicationNr)
        {
            var names = new[] { "ExperimentId", "HasVariation", "VariationName" };
            var values = context
                .ComplexApplicationListItems
                .Where(x =>
                    x.ApplicationNr == applicationNr &&
                    x.Nr == 1 &&
                    x.ListName == "AbTestingExperiment" &&
                    names.Contains(x.ItemName))
                .Select(x => new { x.ItemName, x.ItemValue })
                .ToList()
                .ToDictionary(x => x.ItemName, x => x.ItemValue);
            var id = values.Opt("ExperimentId");
            if (id == null)
                return null;
            var experimentId = int.Parse(id);
            var experimentName = context.AbTestingExperiments.Single(x => x.Id == experimentId).ExperimentName;
            return new ApplicationAbTestExperimentModel
            {
                ExperimentId = experimentId,
                ExperimentName = experimentName,
                HasVariation = values.Opt("HasVariation") == "true",
                VariationName = values.Opt("VariationName")
            };
        }

        private decimal GetRandomPercent()
        {
            lock (RandomLockObject)
            {
                return (decimal)(Random.NextDouble() * 100d);
            }
        }

        public static IQueryable<AbTestingExperiment> FilterActiveExperiments(
            IQueryable<AbTestingExperiment> abTestingExperiments,
            IQueryable<ComplexApplicationListItem> complexApplicationListItems,
            IClock clock)
        {
            var today = clock.Today;
            return abTestingExperiments
                .Where(x =>
                    x.IsActive
                    && (x.EndDate == null || x.EndDate >= today)
                    && (x.StartDate == null || x.StartDate <= today)
                    && (x.MaxCount == null || complexApplicationListItems.Count(y => y.ListName == "AbTestingExperiment" && y.Nr == 1 && y.ItemName == "ExperimentId" && y.ItemValue == x.Id.ToString()) < x.MaxCount)
                );
        }
    }
}