using nCredit.Code;
using nCredit.DbModel.Repository;
using nCredit.DomainModel;
using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using Serilog;
using System;
using System.Globalization;
using System.Linq;

namespace nCredit.Controllers.Api.DataWarehouse
{
    public class MergeMonthlyLiquidityExposureTask : DatawarehouseMergeTask
    {
        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled && NEnv.ClientCfg.Country.BaseCountry == "FI";

        public override void Merge(INTechCurrentUserMetadata currentUser, IClock clock)
        {
            var model = new LiquidityExposureReportDataWarehouseModel();
            using (var context = new CreditContextExtended(currentUser, clock))
            {
                context.Configuration.AutoDetectChangesEnabled = false;

                var latestDoneMonthFromDate = context
                    .SystemItems
                    .Where(x => x.Key == SystemItemCode.DwMonthlyLiquidityExposure_LatestCompleteMonth.ToString())
                    .ToList()
                    .Select(x => (DateTime?)DateTime.ParseExact(x.Value + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture))
                    .OrderByDescending(x => x)
                    .FirstOrDefault();

                var firstCreationDate = context
                    .CreditHeaders
                    .Select(x => (DateTime?)x.CreatedByEvent.TransactionDate)
                    .OrderBy(x => x)
                    .FirstOrDefault();

                var thisMonthFromDate = new DateTime(clock.Today.Year, clock.Today.Month, 1);

                var startDate = latestDoneMonthFromDate.HasValue ? latestDoneMonthFromDate.Value.AddMonths(1) : firstCreationDate;

                if (!startDate.HasValue)
                {
                    NLog.Information("No credits available so no liquidity data available"); ;
                    return;
                }

                var monthFromDate = new DateTime(startDate.Value.Year, startDate.Value.Month, 1);

                DateTime? newDoneMonthFromDate = null;
                int guard = 0;
                var client = new DataWarehouseClient();

                var cl = CreditType.CompanyLoan.ToString();

                var allCreditNrs = context.CreditHeaders.Where(x => x.CreditType != cl).Select(x => x.CreditNr).ToArray();
                while (monthFromDate < thisMonthFromDate && guard++ < 1000)
                {
                    //Dimension
                    var month = new
                    {
                        FromDate = monthFromDate,
                        ToDate = monthFromDate.AddMonths(1).AddDays(-1),
                        Name = monthFromDate.ToString("yyyy-MM")
                    };
                    client.MergeDimension("Month", new[] { month }.ToList());

                    foreach (var g in SplitIntoGroupsOfN(allCreditNrs, 200))
                    {
                        var monthlyItems = model.GetLiquidityExposureModel(context, month.FromDate, g.ToList(), NEnv.IsMortgageLoansEnabled);
                        if (monthlyItems.Any())
                        {
                            client.MergeFact("LiquidityExposureItem", monthlyItems);
                        }
                    }

                    newDoneMonthFromDate = monthFromDate;

                    monthFromDate = monthFromDate.AddMonths(1);
                }
                if (guard > 998)
                    throw new Exception("Hit infinite loop guard code!");

                if (newDoneMonthFromDate.HasValue)
                {
                    context.SystemItems.Add(new SystemItem
                    {
                        ChangedById = currentUser.UserId,
                        ChangedDate = clock.Now,
                        InformationMetaData = currentUser.InformationMetadata,
                        Key = SystemItemCode.DwMonthlyLiquidityExposure_LatestCompleteMonth.ToString(),
                        Value = newDoneMonthFromDate.Value.ToString("yyyy-MM")
                    });
                    context.SaveChanges();
                }
            }
        }
    }
}