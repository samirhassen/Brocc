using nCredit.Code;
using nCredit.DbModel.Repository;
using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nCredit.Controllers.Api.DataWarehouse
{
    public class MergeQuarterlyRatiBasisTask : DatawarehouseMergeTask
    {
        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled && NEnv.ClientCfg.Country.BaseCountry == "FI";

        public override void Merge(INTechCurrentUserMetadata currentUser, IClock clock)
        {
            using (var context = new CreditContext())
            {
                context.Configuration.AutoDetectChangesEnabled = false;

                var latestDoneQuarterToDate = context
                    .SystemItems
                    .Where(x => x.Key == SystemItemCode.DwQuarterlyRATI_LatestCompleteQuarterToDate.ToString())
                    .ToList()
                    .Select(x => (DateTime?)DateTime.ParseExact(x.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture))
                    .OrderByDescending(x => x)
                    .FirstOrDefault();

                var firstCreationDate = context
                    .CreditHeaders
                    .Select(x => (DateTime?)x.CreatedByEvent.TransactionDate)
                    .OrderBy(x => x)
                    .FirstOrDefault();

                var yesterday = clock.Today.AddDays(-1);

                var startDate = latestDoneQuarterToDate.HasValue ? latestDoneQuarterToDate.Value.AddDays(1) : firstCreationDate;

                if (!startDate.HasValue)
                {
                    NLog.Information("No credits available so no rati data available"); ;
                    return;
                }

                var q = Quarter.ContainingDate(startDate.Value);

                DateTime? newDoneQuarterDate = null;
                int guard = 0;
                var client = new DataWarehouseClient();
                var r = new RATIReportDataWarehouseModel();
                var allCreditNrs = context.CreditHeaders.Select(x => x.CreditNr).ToArray();
                var missingPaymentPlanCreditNrs = new HashSet<string>();
                while (q.ToDate <= yesterday && guard++ < 1000)
                {
                    //Dimension
                    client.MergeDimension<Quarter>("Quarter", new[] { q }.ToList());

                    foreach (var g in SplitIntoGroupsOfN(allCreditNrs, 100))
                    {
                        //Quarterly data
                        var model = r
                            .GetRatiModel(context, q.FromDate, q.ToDate, onlyTheseCreditNrs: g.ToList(), obeserveCreditNrMissingRuntime: nr => missingPaymentPlanCreditNrs.Add(nr))
                            .Select(x => new
                            {
                                QuarterFromDate = q.FromDate,
                                QuarterToDate = q.ToDate,
                                CreditNr = x.CreditNr,
                                StartDate = x.StartDate,
                                InitialRuntimeInMonths = x.InitialRuntimeInMonths,
                                CurrentRuntimeInMonths = x.CurrentRuntimeInMonths,
                                InitialCapitalDebt = x.InitialCapitalDebt,
                                CurrentCapitalDebt = x.CurrentCapitalDebt,
                                InitialInterestRate = x.InitialInterestRate,
                                CurrentInterestRate = x.CurrentInterestRate,
                                InitialEffectiveInterest = x.InitialEffectiveInterest,
                                DebtCollectionDate = x.DebtCollectionDate,
                                DebtCollectionInterestDebt = x.DebtCollectionInterestDebt,
                                DebtCollectionCapitalDebt = x.DebtCollectionCapitalDebt,
                                CurrentInterestDebt = x.CurrentInterestDebt,
                                OverdueDays = x.OverdueDays
                            })
                            .ToList();
                        if (model.Any())
                            client.MergeFact("QuarterlyCreditRATIData", model);
                    }

                    newDoneQuarterDate = q.ToDate;

                    q = q.GetNext();
                }
                if (guard > 998)
                    throw new Exception("Hit infinite loop guard code!");

                if (newDoneQuarterDate.HasValue)
                {
                    context.SystemItems.Add(new SystemItem
                    {
                        ChangedById = currentUser.UserId,
                        ChangedDate = clock.Now,
                        InformationMetaData = currentUser.InformationMetadata,
                        Key = SystemItemCode.DwQuarterlyRATI_LatestCompleteQuarterToDate.ToString(),
                        Value = newDoneQuarterDate.Value.ToString("yyyy-MM-dd")
                    });
                    context.SaveChanges();
                }

                if (missingPaymentPlanCreditNrs.Any())
                {
                    Log.Warning($"MergeQuarterlyRatiBasis: {missingPaymentPlanCreditNrs.Count} credits had terms that meant they would never be paid and so have no runtime. Examples: {string.Join(", ", missingPaymentPlanCreditNrs.Take(5))}");
                }
            }
        }
    }
}