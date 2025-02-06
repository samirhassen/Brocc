using nCredit;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using Newtonsoft.Json;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Models;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class MortgageLoanCollateralService : BusinessEventManagerOrServiceBase
    {
        public MortgageLoanCollateralService(INTechCurrentUserMetadata currentUser, ICoreClock clock, IClientConfigurationCore clientConfiguration)
            : base(currentUser, clock, clientConfiguration)
        {
        }

        public const string SeMortgageLoanAmortzationBasisKeyValueItemKey = "seMlAmortBasisKeyValueItemKey";

        public CollateralHeader CreateCollateral(
            ICreditContextExtended context,
            Dictionary<string, string> stringItems, BusinessEvent existingEvent = null,
            SwedishMortgageLoanAmortizationBasisModel seAmortizationBasis = null)
        {
            var evt = existingEvent ?? AddBusinessEvent(BusinessEventType.CreateMortgageLoanCollateral, context);

            var header = context.FillInfrastructureFields(new CollateralHeader
            {
                CollateralType = "MortgageLoanProperty",
                CreatedByEvent = evt
            });
            context.AddCollateralHeader(header);

            var localItems = new Dictionary<string, string>(stringItems);
            if (seAmortizationBasis != null && !localItems.ContainsKey(SeMortgageLoanAmortzationBasisKeyValueItemKey))
            {
                var key = AddSeAmortizationBasisToKeyValueStore(context, seAmortizationBasis);
                localItems[SeMortgageLoanAmortzationBasisKeyValueItemKey] = key;
            }

            context.AddCollateralItems(localItems.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => context.FillInfrastructureFields(new CollateralItem
            {
                Collateral = header,
                ItemName = x.Key,
                StringValue = x.Value,
                CreatedByEvent = evt
            })).ToArray());

            return header;
        }

        public CollateralHeader UpdateExistingCollateralWithNewSeAmortizationBasis(ICreditContextExtended context,
            int collateralId,
            SwedishMortgageLoanAmortizationBasisModel seAmortizationBasis,
            BusinessEvent businessEvent)
        {
            var collateral = context.CollateralHeadersQueryable.Where(x => x.Id == collateralId).Select(x => new
            {
                x.Items,
                Header = x
            }).Single();

            var existingItems = collateral.Items.Where(x => x.ItemName == SeMortgageLoanAmortzationBasisKeyValueItemKey && x.RemovedByBusinessEventId == null).ToList();
            existingItems.ForEach(x => x.RemovedByEvent = businessEvent);

            var key = AddSeAmortizationBasisToKeyValueStore(context, seAmortizationBasis);

            context.AddCollateralItems(context.FillInfrastructureFields(new CollateralItem
            {
                CollateralHeaderId = collateralId,
                CreatedByEvent = businessEvent,
                ItemName = SeMortgageLoanAmortzationBasisKeyValueItemKey,
                StringValue = key
            }));

            return collateral.Header;
        }

        public void SetCollateralStringItems(ICreditContextExtended context, int collateralId, BusinessEvent businessEvent, Dictionary<string, string> values)
        {
            var existingItems = context
                .CollateralHeadersQueryable
                .Where(x => x.Id == collateralId)
                .SelectMany(x => x.Items)
                .Where(x => values.Keys.Contains(x.ItemName))
                .ToList();
            existingItems.ForEach(x => x.RemovedByEvent = businessEvent);

            foreach(var newValue in values)
                context.AddCollateralItems(context.FillInfrastructureFields(new CollateralItem
                {
                    CollateralHeaderId = collateralId,
                    CreatedByEvent = businessEvent,
                    ItemName = newValue.Key,
                    StringValue = newValue.Value
                }));
        }

        public static Dictionary<string, CollateralItem> GetCurrentCollateralItems(IEnumerable<CollateralItem> items) => items
                .GroupBy(x => x.ItemName)
                .Select(x => x.OrderByDescending(y => y.CreatedByBusinessEventId).FirstOrDefault())
                .Where(x => x.RemovedByBusinessEventId == null)
                .ToDictionary(x => x.ItemName, x => x);

        public GetSeAmortizationBasisResponse GetSeMortageLoanAmortizationBasis(ICreditContextExtended context, GetSeAmortizationBasisRequest request)
        {
            if (request.CollateralId.HasValue)
            {
                return GetSeMortageLoanAmortizationBasisByQuery(context,
                    context.CollateralHeadersQueryable.Where(x => x.Id == request.CollateralId.Value),
                    request.UseUpdatedBalance.Value);
            }
            else if (!string.IsNullOrWhiteSpace(request.CreditNr))
            {
                return GetSeMortageLoanAmortizationBasisByQuery(context,
                    context.CollateralHeadersQueryable.Where(x => x.Credits.Any(y => y.CreditNr == request.CreditNr)),
                    request.UseUpdatedBalance.Value);
            }
            else
            {
                throw new NTechCoreWebserviceException("CreditNr or CollateralId is required");
            }
        }

        public GetCurrentLoansOnCollateralResponse GetCurrentLoansOnCollateral(ICreditContextExtended context, GetCurrentLoansOnCollateralRequest request)
        {
            if (request != null && request.CollateralId.HasValue)
            {
                return GetCurrentLoansOnCollateralInternal(context
                    .CreditHeadersQueryable
                    .Where(x => x.CollateralHeaderId == request.CollateralId.Value));
            }
            else if (request != null && !string.IsNullOrWhiteSpace(request.CreditNr))
            {
                return GetCurrentLoansOnCollateralInternal(context
                    .CreditHeadersQueryable
                    .Where(x => x.CreditNr == request.CreditNr)
                    .SelectMany(x => x.Collateral.Credits));
            }
            else
            {
                throw new NTechCoreWebserviceException("CollateralId or CreditNr required")
                {
                    IsUserFacing = true,
                    ErrorHttpStatusCode = 400
                };
            }
        }


        private GetCurrentLoansOnCollateralResponse GetCurrentLoansOnCollateralInternal(IQueryable<CreditHeader> creditsOnCollateral)
        {
            var loans = creditsOnCollateral.Select(x => new
            {
                x.CreditNr,
                x.Status,
                CurrentCapitalBalanceAmount = x
                    .Transactions
                    .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                    .Sum(y => y.Amount),
                ActualFixedMonthlyPayment = x
                    .DatedCreditValues
                    .Where(y => y.Name == DatedCreditValueCode.MonthlyAmortizationAmount.ToString())
                    .OrderByDescending(y => y.Id)
                    .Select(y => (decimal?)y.Value)
                    .FirstOrDefault(),
                ExceptionAmortizationAmount = x
                    .DatedCreditValues
                    .Where(y => y.Name == DatedCreditValueCode.ExceptionAmortizationAmount.ToString())
                    .OrderByDescending(y => y.Id)
                    .Select(y => (decimal?)y.Value)
                    .FirstOrDefault(),
                AmortizationExceptionUntilDate = x
                    .DatedCreditDates
                    .Where(y => y.Name == DatedCreditDateCode.AmortizationExceptionUntilDate.ToString())
                    .OrderByDescending(y => y.Id)
                    .Select(y => y.RemovedByBusinessEventId.HasValue ? null : (DateTime?)y.Value)
                    .FirstOrDefault(),
                AmortizationExceptionReasonsRaw = x
                    .DatedCreditStrings
                    .Where(y => y.Name == DatedCreditStringCode.AmortizationExceptionReasons.ToString())
                    .OrderByDescending(y => y.Id)
                    .Select(y => y.Value)
                    .FirstOrDefault(),

            }).ToList()
            .Select(x => new CurrentLoanOnCollateralModel
            {
                CreditNr = x.CreditNr,
                CurrentCapitalBalanceAmount = x.CurrentCapitalBalanceAmount,
                ActualFixedMonthlyPayment = x.ActualFixedMonthlyPayment,
                IsActive = x.Status == CreditStatus.Normal.ToString(),
                AmortizationException = x.AmortizationExceptionUntilDate.HasValue
                    ? new MortgageLoanSeAmortizationExceptionModel
                    {
                        AmortizationAmount = x.ExceptionAmortizationAmount.Value,
                        UntilDate = x.AmortizationExceptionUntilDate,
                        Reasons = x.AmortizationExceptionReasonsRaw == null
                            ? new List<string>()
                            : JsonConvert.DeserializeObject<List<string>>(x.AmortizationExceptionReasonsRaw)
                    }
                    : null
            })
            .ToList();

            return new GetCurrentLoansOnCollateralResponse
            {
                Loans = loans
            };
        }

        private GetSeAmortizationBasisResponse GetSeMortageLoanAmortizationBasisByQuery(ICreditContextExtended context, IQueryable<CollateralHeader> query, bool useUpdatedBalance)
        {
            var collateral = query.Select(x => new
            {
                x.Id,
                Items = x.Items,
                LoanBalances = x.Credits.Select(y => new
                {
                    y.CreditNr,
                    CapitalBalance = y.Transactions.Where(z => z.AccountCode == TransactionAccountType.CapitalDebt.ToString()).Sum(z => z.Amount)
                })
            })
            .SingleOrDefault();

            if (collateral == null)
            {
                return null;
            }

            var loanBalances = collateral.LoanBalances.ToDictionary(x => x.CreditNr, x => x.CapitalBalance);
            var stringItems = GetCurrentCollateralItems(collateral.Items).ToDictionary(x => x.Key, x => x.Value.StringValue);

            var amortizationBasisKey = stringItems.Opt(SeMortgageLoanAmortzationBasisKeyValueItemKey);

            var amortizationBasisRaw = amortizationBasisKey == null ? null : context.KeyValueItemsQueryable
                .Where(x => x.KeySpace == KeyValueStoreKeySpaceCode.SeMortgageLoanAmortzationBasisV1.ToString() && x.Key == amortizationBasisKey)
                .Select(x => x.Value)
                .FirstOrDefault();

            var amortizationBasis = SwedishMortgageLoanAmortizationBasisStorageModel.Parse(amortizationBasisRaw);

            if (amortizationBasis != null && useUpdatedBalance)
            {
                var currentPropertyLoanBalanceAmount = 0m;
                foreach (var loan in amortizationBasis.Model.Loans)
                {                    
                    var maxBalanceAmount = new[]
                        {
                            loan.CurrentCapitalBalanceAmount,
                            loan.MaxCapitalBalanceAmount ?? loan.CurrentCapitalBalanceAmount,
                            loanBalances[loan.CreditNr]
                        }.Max();
                    loan.MaxCapitalBalanceAmount = maxBalanceAmount;
                    loan.CurrentCapitalBalanceAmount = loanBalances[loan.CreditNr];
                    currentPropertyLoanBalanceAmount += loan.CurrentCapitalBalanceAmount;
                }

                if (amortizationBasis.Model.LtiFraction.HasValue)
                    amortizationBasis.Model.LtiFraction = SwedishMortgageLoanAmortizationBasisService.ComputeLti(
                        amortizationBasis.Model.CurrentCombinedYearlyIncomeAmount,
                        currentPropertyLoanBalanceAmount,
                        amortizationBasis.Model.OtherMortageLoansAmount);

                if(amortizationBasis.Model.LtvFraction.HasValue)
                    amortizationBasis.Model.LtvFraction = SwedishMortgageLoanAmortizationBasisService.ComputeLtv(amortizationBasis.Model.ObjectValue, currentPropertyLoanBalanceAmount);
            }

            return new GetSeAmortizationBasisResponse
            {
                PropertyId = GetPropertyId(stringItems, false),
                PropertyIdWithLabel = GetPropertyId(stringItems, true),
                AmortizationBasis = amortizationBasis?.Model,
                Amorteringsunderlag = amortizationBasis == null ? null : SwedishMortgageLoanAmortizationBasisService.GetSwedishAmorteringsunderlag(
                    amortizationBasis.Model, currentLoanBalanceAmount: useUpdatedBalance ? loanBalances : null),
                BalanceDate = useUpdatedBalance ? (DateTime?)Clock.Today : amortizationBasis?.TransactionDate,
                CollateralId = collateral.Id
            };
        }

        public List<SeHistoricalAmortizationBasis> GetAllHistoricalAmortizationBasis(GetAmortizationBasisHistoryRequest request, ICreditContextExtended context)
        {
            var historicalBasisValues = context
                .CollateralHeadersQueryable
                .Where(x => x.Id == request.CollateralId)
                .SelectMany(x => x.Items)
                .Where(x => x.ItemName == SeMortgageLoanAmortzationBasisKeyValueItemKey)
                .Select(x => new { x.CreatedByEvent.TransactionDate, Key = x.StringValue, x.CreatedByBusinessEventId } )
                .ToList();

            var basisKeys = historicalBasisValues.Select(x => x.Key).ToList();

            var basisDataByKey = context
                .KeyValueItemsQueryable
                .Where(x => x.KeySpace == KeyValueStoreKeySpaceCode.SeMortgageLoanAmortzationBasisV1.ToString() && basisKeys.Contains(x.Key))
                .Select(x => new { x.Key, x.Value })
                .ToDictionary(x => x.Key, x => x.Value);

            return historicalBasisValues
                .OrderByDescending(x => x.CreatedByBusinessEventId)
                .Select(x =>
                {
                    var amortizationBasis = SwedishMortgageLoanAmortizationBasisStorageModel.Parse(basisDataByKey[x.Key]).Model;
                    return new SeHistoricalAmortizationBasis
                    {
                        TransactionDate = x.TransactionDate,
                        AmortizationBasis = amortizationBasis,
                        Amorteringsunderlag = SwedishMortgageLoanAmortizationBasisService.GetSwedishAmorteringsunderlag(amortizationBasis)
                    };
                })
                .ToList();
        }

        public static string GetPropertyId(Dictionary<string, string> collateralItems, bool includeObjectTypeLabel)
        {
            var objectTypeCode = collateralItems.Opt("objectTypeCode");
            var isBrf = objectTypeCode == "seBrf";
            var propertyId = isBrf
                    ? $"{collateralItems.Opt("objectAddressMunicipality")} {collateralItems.Opt("seBrfName")} {collateralItems.Opt("seBrfApartmentNr")}"
                    : $"{collateralItems.Opt("objectId")}";
            if (includeObjectTypeLabel)
            {
                return isBrf ? $"BRF: {propertyId}" : propertyId;
            }
            else
            {
                return propertyId;
            }
        }

        public static Dictionary<int, string> GetPropertyIdByCollateralId(ICreditContextExtended context, HashSet<int> collateralIds, bool includeObjectTypeLabel)
        {
            var propertyIdItemsNames = new HashSet<string>
                {
                    "objectTypeCode", "seBrf", "objectAddressMunicipality", "seBrfName", "seBrfApartmentNr", "objectId"
                };
            return context
                .CollateralHeadersQueryable
                .Where(x => collateralIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    PropertyIdItems = x.Items.Where(y => propertyIdItemsNames.Contains(y.ItemName)).Select(y => new { y.ItemName, y.StringValue })
                })
                .ToList()
                .ToDictionary(x => x.Id, x => GetPropertyId(x.PropertyIdItems.ToDictionary(y => y.ItemName, y => y.StringValue), includeObjectTypeLabel));
        }

        public static Dictionary<string, string> GetPropertyIdByCreditNr(ICreditContextExtended context, HashSet<string> creditNrs, bool includeObjectTypeLabel)
        {
            var creditNrsWithCollateralId = context.CreditHeadersQueryable.Where(x => creditNrs.Contains(x.CreditNr)).Select(x => new
            {
                x.CollateralHeaderId,
                x.CreditNr
            })
            .ToList();
            var collateralIds = creditNrsWithCollateralId.Where(x => x.CollateralHeaderId.HasValue).Select(x => x.CollateralHeaderId.Value).ToHashSetShared();
            var propertyIds = GetPropertyIdByCollateralId(context, collateralIds, includeObjectTypeLabel);
            return creditNrsWithCollateralId
                .Where(x => x.CollateralHeaderId.HasValue && propertyIds.ContainsKey(x.CollateralHeaderId.Value))
                .ToDictionary(x => x.CreditNr, x => propertyIds[x.CollateralHeaderId.Value]);
        }

        public static string AddSeAmortizationBasisToKeyValueStore(ICreditContextExtended context, SwedishMortgageLoanAmortizationBasisModel seAmortizationBasis)
        {
            if (seAmortizationBasis == null)
            {
                throw new ArgumentNullException("seAmortizationBasis");
            }

            var key = OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken();
            KeyValueStoreService.SetValueComposable(context, key, KeyValueStoreKeySpaceCode.SeMortgageLoanAmortzationBasisV1.ToString(), JsonConvert.SerializeObject(new SwedishMortgageLoanAmortizationBasisStorageModel
            {
                TransactionDate = context.CoreClock.Today,
                Model = seAmortizationBasis
            }));

            return key;
        }
    }

    public class GetAmortizationBasisHistoryRequest
    {
        [Required]
        public int CollateralId { get; set; }
    }

    public class GetSeAmortizationBasisRequest
    {
        public string CreditNr { get; set; }
        public int? CollateralId { get; set; }

        /// <summary>
        /// Recalculates for current balance
        /// </summary>
        [Required]
        public bool? UseUpdatedBalance { get; set; }

        public bool? IncludeHistory { get; set; }
    }

    public class GetSeAmortizationBasisResponse
    {
        public string PropertyId { get; set; }
        public string PropertyIdWithLabel { get; set; }
        public SwedishMortgageLoanAmortizationBasisModel AmortizationBasis { get; set; }
        public SwedishAmorteringsunderlag Amorteringsunderlag { get; set; }
        public DateTime? BalanceDate { get; set; }
        public int CollateralId { get; set; }
    }

    public class SeHistoricalAmortizationBasis
    {
        public DateTime TransactionDate { get; set; }
        public SwedishMortgageLoanAmortizationBasisModel AmortizationBasis { get; set; }
        public SwedishAmorteringsunderlag Amorteringsunderlag { get; set; }
    }

    public class GetCurrentLoansOnCollateralRequest
    {
        public string CreditNr { get; set; }
        public int? CollateralId { get; set; }
    }

    public class GetCurrentLoansOnCollateralResponse
    {
        public List<CurrentLoanOnCollateralModel> Loans { get; set; }
    }

    public class CurrentLoanOnCollateralModel
    {
        public string CreditNr { get; set; }
        public decimal CurrentCapitalBalanceAmount { get; set; }
        public decimal? ActualFixedMonthlyPayment { get; set; }
        public bool IsActive { get; set; }
        public MortgageLoanSeAmortizationExceptionModel AmortizationException { get; set; }
    }

    public class MortgageLoanSeAmortizationExceptionModel
    {
        public decimal AmortizationAmount { get; set; }
        public DateTime? UntilDate { get; set; }
        public List<string> Reasons { get; set; }
    }
}
