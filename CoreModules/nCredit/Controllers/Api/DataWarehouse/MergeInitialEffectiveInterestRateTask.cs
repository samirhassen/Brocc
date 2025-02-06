using nCredit.Code;
using nCredit.DbModel.Repository;
using NTech;
using NTech.Banking.LoanModel;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Linq;

namespace nCredit.Controllers.Api.DataWarehouse
{
    public class MergeInitialEffectiveInterestRateTask : DatawarehouseMergeTask
    {
        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled || NEnv.IsCompanyLoansEnabled;

        public override void Merge(INTechCurrentUserMetadata currentUser, IClock clock)
        {
            RepeatWithGuard(() => MergeI(currentUser));
        }

        private bool MergeI(INTechCurrentUserMetadata currentUser)
        {
            var repo = new SystemItemRepository(currentUser.UserId, currentUser.InformationMetadata);

            using (var context = new CreditContext())
            {
                var v = repo.Get(SystemItemCode.DwLatestNewCreditBusinessEventId_Fact_InitialEffectiveInterestRate, context);
                int? lastHandledBusinessEventId = v == null ? new int?() : int.Parse(v);

                var basis = context
                    .BusinessEvents
                    .Where(x => x.EventType == BusinessEventType.NewCredit.ToString())
                    .Select(x => new
                    {
                        BusinessEventId = x.Id,
                        Credit = x
                            .CreatedCredits
                            .Select(y => new
                            {
                                y.CreditNr,
                                AnnuityAmount = y.DatedCreditValues
                                    .Where(z => z.BusinessEventId == x.Id && z.Name == DatedCreditValueCode.AnnuityAmount.ToString())
                                    .Select(z => (decimal?)z.Value)
                                    .FirstOrDefault(),
                                NotificationFeeAmount = y
                                    .DatedCreditValues
                                    .Where(z => z.BusinessEventId == x.Id && z.Name == DatedCreditValueCode.NotificationFee.ToString())
                                    .Select(z => (decimal?)z.Value)
                                    .FirstOrDefault(),
                                MarginInterestRatePercent = y
                                    .DatedCreditValues
                                    .Where(z => z.BusinessEventId == x.Id && z.Name == DatedCreditValueCode.MarginInterestRate.ToString())
                                    .Select(z => (decimal?)z.Value)
                                    .FirstOrDefault(),
                                ReferenceInterestRatePercent = y
                                    .DatedCreditValues
                                    .Where(z => z.BusinessEventId == x.Id && z.Name == DatedCreditValueCode.ReferenceInterestRate.ToString())
                                    .Select(z => (decimal?)z.Value)
                                    .FirstOrDefault(),
                                InitialCapitalAmount = y
                                    .Transactions
                                    .Where(z => z.BusinessEventId == x.Id && z.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                                    .Sum(z => (decimal?)z.Amount),
                                CapitalizedInitialFeeAmount = y
                                    .Transactions
                                    .Where(z => z.BusinessEvent.EventType == BusinessEventType.CapitalizedInitialFee.ToString() && z.AccountCode == TransactionAccountType.CapitalDebt.ToString() && z.TransactionDate == x.TransactionDate) //The last date filter is just guard code incase someone uses this event wrongly in the future to add initial fees to additional loans in the middle of a running credit
                                    .Sum(z => (decimal?)z.Amount),
                                DrawnFromLoanAmountInitialFeeAmount = -y
                                    .Transactions
                                    .Where(z =>
                                        z.BusinessEvent.EventType == BusinessEventType.NewCredit.ToString()
                                        && z.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString()
                                        && z.BusinessEventRoleCode == "initialFee"
                                        && z.TransactionDate == x.TransactionDate) //The last date filter is just guard code incase someone uses this event wrongly in the future to add initial fees to additional loans in the middle of a running credit
                                    .Sum(z => (decimal?)z.Amount)
                            })
                            .FirstOrDefault()
                    });

                if (lastHandledBusinessEventId.HasValue)
                    basis = basis.Where(x => x.BusinessEventId > lastHandledBusinessEventId.Value);

                var evts = basis.OrderBy(x => x.BusinessEventId).Take(300).ToList();
                if (evts.Count == 0)
                    return false;

                var newLastHandledBusinessEventId = evts.Max(x => x.BusinessEventId);

                var credits = evts.Select(x => x.Credit).Where(x => x.CreditNr != null).ToList();

                if (credits.Count > 0)
                {
                    var client = new DataWarehouseClient();
                    var rates = credits
                        .Select(x =>
                        {
                            var t = PaymentPlanCalculation
                                .BeginCreateWithAnnuity(
                                x.InitialCapitalAmount ?? 0m, x.AnnuityAmount ?? 0m,
                                    (x.MarginInterestRatePercent ?? 0m) + (x.ReferenceInterestRatePercent ?? 0m),
                                    null,
                                    NEnv.CreditsUse360DayInterestYear)
                                .WithInitialFeeCapitalized(x.CapitalizedInitialFeeAmount ?? 0m)
                                .WithInitialFeeDrawnFromLoanAmount(x.DrawnFromLoanAmountInitialFeeAmount ?? 0m)
                                .WithMonthlyFee(x.NotificationFeeAmount ?? 0m)
                                .EndCreate();
                            return new
                            {
                                x.CreditNr,
                                InitialEffectiveInterestRatePercent = t.EffectiveInterestRatePercent,
                                DwUpdatedDate = DateTime.Now
                            };
                        })
                        .ToList();
                    client.MergeFact("InitialEffectiveInterestRate", rates);
                }

                repo.Set(SystemItemCode.DwLatestNewCreditBusinessEventId_Fact_InitialEffectiveInterestRate, newLastHandledBusinessEventId.ToString(), context);

                context.SaveChanges();

                return true;
            }
        }
    }
}