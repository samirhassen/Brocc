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
    public class MergeQuarterlyRatiBusinessEventsTask : DatawarehouseMergeTask
    {
        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled && NEnv.ClientCfg.Country.BaseCountry == "FI";

        public override void Merge(INTechCurrentUserMetadata currentUser, IClock clock)
        {
            using (var context = new CreditContext())
            {
                context.Configuration.AutoDetectChangesEnabled = false;

                var latestDoneQuarterToDate = context
                    .SystemItems
                    .Where(x => x.Key == SystemItemCode.DwQuarterlyRATIBusinessEvents_LatestCompleteQuarterToDate.ToString())
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

                var missingPaymentPlanCreditNrs = new HashSet<string>();

                var q = Quarter.ContainingDate(startDate.Value);

                DateTime? newDoneQuarterDate = null;
                int guard = 0;
                var client = new DataWarehouseClient();
                var r = new RATIReportDataWarehouseModel();
                var allCreditNrs = context.CreditHeaders.Select(x => x.CreditNr).ToArray();
                while (q.ToDate <= yesterday && guard++ < 1000)
                {
                    //Dimension
                    client.MergeDimension<Quarter>("Quarter", new[] { q }.ToList());

                    foreach (var g in SplitIntoGroupsOfN(allCreditNrs, 100))
                    {
                        //Quarterly data
                        var model = r
                            .GetRatiBusinessEventRatiDataDataForPeriod(context, q.FromDate, q.ToDate, BusinessEventType.NewAdditionalLoan, s => missingPaymentPlanCreditNrs.Add(s), onlyTheseCreditNrs: g.ToList())
                            .Union(r.GetRatiBusinessEventRatiDataDataForPeriod(context, q.FromDate, q.ToDate, BusinessEventType.AcceptedCreditTermsChange, s => missingPaymentPlanCreditNrs.Add(s), onlyTheseCreditNrs: g.ToList()))
                            .Select(x => new
                            {
                                QuarterFromDate = q.FromDate,
                                QuarterToDate = q.ToDate,
                                CreditNr = x.CreditNr,
                                EventType = x.EventType,
                                EventId = x.EventId,
                                TransactionDate = x.TransactionDate,
                                AfterEventRuntimeInMonths = x.AfterEventRuntimeInMonths,
                                AfterEventCapitalDebt = x.AfterEventCapitalDebt,
                                AfterEventInterestRate = x.AfterEventInterestRate,
                                AfterEventEffectiveInterest = x.AfterEventEffectiveInterest,
                                ByEventAddedCapitalDebt = x.ByEventAddedCapitalDebt,
                                CurrentInterstDebtFraction = x.CurrentInterstDebtFraction
                            })
                            .ToList();
                        if (model.Any())
                            client.MergeFact("QuarterlyCreditRATIBusinessEventData", model);
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
                        Key = SystemItemCode.DwQuarterlyRATIBusinessEvents_LatestCompleteQuarterToDate.ToString(),
                        Value = newDoneQuarterDate.Value.ToString("yyyy-MM-dd")
                    });
                    context.SaveChanges();
                }

                if (missingPaymentPlanCreditNrs.Any())
                {
                    Log.Warning($"MergeQuarterlyRatiBusinessEvents: {missingPaymentPlanCreditNrs.Count} credits have terms that mean they will never be paid. Examples: {string.Join(", ", missingPaymentPlanCreditNrs.Take(5))}");
                }
            }
        }
    }
}