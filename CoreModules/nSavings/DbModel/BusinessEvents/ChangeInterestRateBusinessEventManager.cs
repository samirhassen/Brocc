using System;
using System.Collections.Generic;
using System.Linq;
using NTech;
using NTech.Banking.Conversion;
using NTech.Core.Savings.Shared.DbModel;
using NTech.Core.Savings.Shared.DbModel.SavingsAccountFlexible;

namespace nSavings.DbModel.BusinessEvents
{
    public class ChangeInterestRateBusinessEventManager : BusinessEventManagerBase
    {
        public ChangeInterestRateBusinessEventManager(int userId, string informationMetadata, IClock clock) : base(
            userId, informationMetadata, clock)
        {
        }

        public bool TryChangeInterestRate(InMemoryInterestChangeManager.ChangeState change, out string failedMessage,
            out SharedSavingsInterestRateChangeHeader newSharedSavingsInterestRateHeader)
        {
            using (var context = new SavingsContext())
            {
                var today = Clock.Today.Date;

                newSharedSavingsInterestRateHeader = null;

                if (string.IsNullOrWhiteSpace(change?.SavingsAccountTypeCode))
                {
                    failedMessage = "Missing savingsAccountTypeCode";
                    return false;
                }

                var savingsAccountTypeCode = Enums.Parse<SavingsAccountTypeCode>(change.SavingsAccountTypeCode);
                if (!savingsAccountTypeCode.HasValue)
                {
                    failedMessage = "Invalid savingsAccountTypeCode";
                    return false;
                }

                if (change.AllAccountsValidFromDate.Date <= today)
                {
                    failedMessage = $"allAccountsValidFromDate must be > {today:yyyy-MM-dd}";
                    return false;
                }

                if (change.NewAccountsValidFromDate.HasValue && change.NewAccountsValidFromDate.Value.Date <= today)
                {
                    failedMessage = $"newAccountsValidFromDate must be > {today:yyyy-MM-dd}";
                    return false;
                }

                if (change.NewInterestRatePercent < 0m)
                {
                    failedMessage = "newInterestRatePercent must be >= 0";
                    return false;
                }

                if (!change.VerifiedByUserId.HasValue)
                {
                    failedMessage = "verifiedByUserId is missing";
                    return false;
                }

                var evt = AddBusinessEvent(BusinessEventType.InterestRateChange, context);
                newSharedSavingsInterestRateHeader = new SharedSavingsInterestRateChangeHeader
                {
                    BusinessEvent = evt,
                    CreatedDate = Clock.Now,
                    InitiatedAndCreatedByUserId = change.InitiatedByUserId,
                    InitiatedDate = change.InitiatedDate,
                    VerifiedByUserId = change.VerifiedByUserId.Value,
                    VerifiedDate = change.VerifiedOrRejectedDate.Value
                };
                FillInInfrastructureFields(newSharedSavingsInterestRateHeader);
                context.SharedSavingsInterestRateChangeHeaders.Add(newSharedSavingsInterestRateHeader);

                var allRate = new SharedSavingsInterestRate
                {
                    AccountTypeCode = savingsAccountTypeCode.Value.ToString(),
                    BusinessEvent = evt,
                    InterestRatePercent = change.NewInterestRatePercent,
                    TransactionDate = Clock.Today,
                    ValidFromDate = change.AllAccountsValidFromDate.Date
                };
                FillInInfrastructureFields(allRate);
                newSharedSavingsInterestRateHeader.AllAccountsRate = allRate;
                context.SharedSavingsInterestRates.Add(allRate);

                if (change.NewAccountsValidFromDate.HasValue)
                {
                    var newRate = new SharedSavingsInterestRate
                    {
                        AccountTypeCode = savingsAccountTypeCode.Value.ToString(),
                        BusinessEvent = evt,
                        InterestRatePercent = change.NewInterestRatePercent,
                        TransactionDate = Clock.Today,
                        ValidFromDate = change.NewAccountsValidFromDate.Value.Date,
                        AppliesToAccountsSinceBusinessEvent = evt
                    };
                    FillInInfrastructureFields(newRate);
                    newSharedSavingsInterestRateHeader.NewAccountsOnlyRate = newRate;
                    context.SharedSavingsInterestRates.Add(newRate);
                }

                context.SaveChanges();

                failedMessage = null;
                return true;
            }
        }

        public static IQueryable<SharedInterestRateModel> GetActiveInterestRates(SavingsContext context)
        {
            return context
                .SharedSavingsInterestRates
                .Where(x => !x.RemovedByBusinessEventId.HasValue)
                .GroupBy(x => new { x.ValidFromDate, x.AppliesToAccountsSinceBusinessEventId })
                .Select(x => x.OrderByDescending(y => y.BusinessEventId).FirstOrDefault())
                .Select(x => new SharedInterestRateModel
                {
                    Id = x.Id,
                    AppliesToAccountsSinceBusinessEventId = x.AppliesToAccountsSinceBusinessEventId,
                    BusinessEventId = x.BusinessEventId,
                    AccountTypeCode = x.AccountTypeCode.ToString(),
                    InterestRatePercent = x.InterestRatePercent,
                    TransactionDate = x.TransactionDate,
                    ValidFromDate = x.ValidFromDate,
                    ChangedById = x.ChangedById
                });
        }

        public static IQueryable<PerAccountInterestRateModel> GetPerAccountActiveInterestRates(SavingsContext context)
        {
            var rates = GetActiveInterestRates(context);
            var tmp = context
                .SavingsAccountHeaders
                .SelectMany(x => rates
                    .Where(y => x.AccountTypeCode.ToString() == y.AccountTypeCode &&
                                (!y.AppliesToAccountsSinceBusinessEventId.HasValue ||
                                 y.AppliesToAccountsSinceBusinessEventId.Value <= x.CreatedByBusinessEventId))
                    .Select(y => new
                    {
                        Id = y.Id,
                        SavingsAccountNr = x.SavingsAccountNr,
                        BusinessEventId = y.BusinessEventId,
                        InterestRatePercent = y.InterestRatePercent,
                        TransactionDate = y.TransactionDate,
                        ValidFromDate = y.ValidFromDate,
                        ChangedById = y.ChangedById
                    }));

            return tmp.Select(x => new PerAccountInterestRateModel
            {
                BusinessEventId = x.BusinessEventId,
                Id = x.Id,
                SavingsAccountNr = x.SavingsAccountNr,
                ChangedById = x.ChangedById,
                InterestRatePercent = x.InterestRatePercent,
                TransactionDate = x.TransactionDate,
                ValidFromDate = x.ValidFromDate
            });
        }

        /// <summary>
        /// Filters out rates from before the account was created and after it was closed
        /// </summary>
        public static List<PerAccountInterestRateModel> GetSavingsAccountFilteredActiveInterestRates(
            SavingsContext context, string savingsAccountNr, DateTime createdTransactionDate,
            int? closedBusinessEventId)
        {
            var b = GetPerAccountActiveInterestRates(context)
                .Where(x => x.SavingsAccountNr == savingsAccountNr && (!closedBusinessEventId.HasValue ||
                                                                       x.BusinessEventId <=
                                                                       closedBusinessEventId.Value))
                .Select(x => new
                {
                    V = x,
                    IsFromOrBeforeCreationDate = x.ValidFromDate <= createdTransactionDate,
                });
            return b
                .Select(x => new
                {
                    x.V,
                    x.IsFromOrBeforeCreationDate,
                    IsBeforeFirst = x.IsFromOrBeforeCreationDate
                                    && b.Any(y => y.IsFromOrBeforeCreationDate && y.V.ValidFromDate > x.V.ValidFromDate)
                })
                .Where(x => !x.IsBeforeFirst)
                .Select(x => x.V)
                .ToList();
        }

        public static SharedInterestRateModel GetCurrentInterestRateForNewAccounts(SavingsContext context,
            SavingsAccountTypeCode savingsAccountTypeCode, DateTime forDate)
        {
            return GetActiveInterestRates(context)
                .Where(x => x.AccountTypeCode == savingsAccountTypeCode.ToString() && x.ValidFromDate <= forDate)
                .OrderByDescending(x => x.ValidFromDate)
                .FirstOrDefault();
        }

        public class SharedInterestRateModel
        {
            public int Id { get; set; }
            public int BusinessEventId { get; set; }
            public int? AppliesToAccountsSinceBusinessEventId { get; set; }
            public string AccountTypeCode { get; set; }
            public decimal InterestRatePercent { get; set; }
            public DateTime TransactionDate { get; set; }
            public DateTime ValidFromDate { get; set; }
            public int ChangedById { get; set; }
        }

        public class PerAccountInterestRateModel
        {
            public int Id { get; set; }
            public int BusinessEventId { get; set; }
            public string SavingsAccountNr { get; set; }
            public decimal InterestRatePercent { get; set; }
            public DateTime TransactionDate { get; set; }
            public DateTime ValidFromDate { get; set; }
            public int ChangedById { get; set; }
        }

        public class UpcomingChangeModel
        {
            public int Id { get; set; }

            public class Rate
            {
                public string SavingsAccountTypeCode { get; set; }
                public decimal? NewInterestRatePercent { get; set; }
                public DateTime ValidFromDate { get; set; }
                public bool IsPending { get; set; }
            }

            public Rate AllAccountsRate { get; set; }
            public Rate NewAccountsOnlyRate { get; set; }
            public int InitiatedAndCreatedByUserId { get; set; }
            public int VerifiedByUserId { get; set; }
            public DateTimeOffset InitiatedDate { get; set; }
            public DateTimeOffset VerifiedDate { get; set; }
            public DateTimeOffset CreatedDate { get; set; }
            public bool HadNewAccountsOnlyRate { get; set; }
        }

        public static IQueryable<UpcomingChangeModel> GetUpcomingChangesModels(SavingsContext context, IClock clock)
        {
            var today = clock.Today;
            return context
                .SharedSavingsInterestRateChangeHeaders
                .Where(x =>
                    (x.NewAccountsOnlyRateId.HasValue && x.NewAccountsOnlyRate.ValidFromDate > today &&
                     !x.NewAccountsOnlyRate.RemovedByBusinessEventId.HasValue)
                    ||
                    (x.AllAccountsRateId.HasValue && x.AllAccountsRate.ValidFromDate > today &&
                     !x.AllAccountsRate.RemovedByBusinessEventId.HasValue)
                )
                .Select(x => new UpcomingChangeModel
                {
                    Id = x.Id,
                    AllAccountsRate =
                        x.AllAccountsRateId.HasValue && !x.AllAccountsRate.RemovedByBusinessEventId.HasValue
                            ? new UpcomingChangeModel.Rate
                            {
                                NewInterestRatePercent = x.AllAccountsRate.InterestRatePercent,
                                SavingsAccountTypeCode = x.AllAccountsRate.AccountTypeCode,
                                ValidFromDate = x.AllAccountsRate.ValidFromDate,
                                IsPending = x.AllAccountsRate.ValidFromDate > today
                            }
                            : null,
                    NewAccountsOnlyRate =
                        x.NewAccountsOnlyRateId.HasValue && !x.NewAccountsOnlyRate.RemovedByBusinessEventId.HasValue
                            ? new UpcomingChangeModel.Rate
                            {
                                NewInterestRatePercent = x.NewAccountsOnlyRate.InterestRatePercent,
                                SavingsAccountTypeCode = x.NewAccountsOnlyRate.AccountTypeCode,
                                ValidFromDate = x.NewAccountsOnlyRate.ValidFromDate,
                                IsPending = x.NewAccountsOnlyRate.ValidFromDate > today
                            }
                            : null,
                    HadNewAccountsOnlyRate = x.NewAccountsOnlyRateId.HasValue,
                    InitiatedAndCreatedByUserId = x.InitiatedAndCreatedByUserId,
                    VerifiedByUserId = x.VerifiedByUserId,
                    InitiatedDate = x.InitiatedDate,
                    VerifiedDate = x.VerifiedDate,
                    CreatedDate = x.CreatedDate,
                });
        }

        public class HistoryItemModel
        {
            public int Id { get; set; }
            public string AccountTypeCode { get; set; }
            public decimal InterestRatePercent { get; set; }
            public DateTime ValidFromDate { get; set; }
            public int? RemovedByBusinessEventId { get; set; }
            public int? AppliesToAccountsSinceBusinessEventId { get; set; }

            public bool IsPartOfSplitChange { get; set; }
            public DateTime FallbackInitiatedDate { get; set; }
            public DateTimeOffset? InitiatedDate { get; set; }
            public int? CreatedByUserId { get; set; }
            public int? VerifiedByUserId { get; set; }
            public int? RemovedByUserId { get; set; }
            public DateTime? RemovedDate { get; set; }
            public int FallbackCreatedByUserId { get; set; }
        }

        public static IQueryable<HistoryItemModel> GetHistoryModelItems(SavingsContext context, IClock clock)
        {
            var today = clock.Today;
            return context
                .SharedSavingsInterestRates
                .Where(x => x.ValidFromDate <= today || x.RemovedByBusinessEventId.HasValue)
                .Select(x => new
                {
                    Id = x.Id,
                    x.BusinessEvent,
                    AccountTypeCode = x.AccountTypeCode,
                    InterestRatePercent = x.InterestRatePercent,
                    ValidFromDate = x.ValidFromDate,
                    RemovedByBusinessEvent = x.RemovedByBusinessEvent,
                    AppliesToAccountsSinceBusinessEventId = x.AppliesToAccountsSinceBusinessEventId,
                    Header = x.AppliesToAccountsSinceBusinessEventId.HasValue
                        ? x.NewAccountsHeaders.FirstOrDefault()
                        : x.AllAccountsHeaders.FirstOrDefault()
                })
                .Select(x => new HistoryItemModel
                {
                    Id = x.Id,
                    AccountTypeCode = x.AccountTypeCode,
                    InterestRatePercent = x.InterestRatePercent,
                    ValidFromDate = x.ValidFromDate,
                    RemovedByBusinessEventId = x.RemovedByBusinessEvent.Id,
                    AppliesToAccountsSinceBusinessEventId = x.AppliesToAccountsSinceBusinessEventId,
                    IsPartOfSplitChange = x.Header != null && x.Header.AllAccountsRateId.HasValue &&
                                          x.Header.NewAccountsOnlyRateId.HasValue,
                    CreatedByUserId = x.Header.InitiatedAndCreatedByUserId,
                    InitiatedDate = x.Header.InitiatedDate,
                    FallbackInitiatedDate = x.BusinessEvent.TransactionDate,
                    RemovedDate = x.RemovedByBusinessEvent.TransactionDate,
                    RemovedByUserId = x.RemovedByBusinessEvent.ChangedById,
                    VerifiedByUserId = x.Header.VerifiedByUserId,
                    FallbackCreatedByUserId = x.BusinessEvent.ChangedById
                });
        }

        internal bool TryRemovePendingInterestRateChange(int sharedSavingsInterestRateChangeHeaderId,
            out string failedMessage)
        {
            using (var context = new SavingsContext())
            {
                var change = context
                    .SharedSavingsInterestRateChangeHeaders
                    .Include("NewAccountsOnlyRate")
                    .Include("AllAccountsRate")
                    .SingleOrDefault(x => x.Id == sharedSavingsInterestRateChangeHeaderId);

                if (change == null)
                {
                    failedMessage = "No such change";
                    return false;
                }

                var evt = AddBusinessEvent(BusinessEventType.InterestRateChangeRemoval, context);

                var cancelledCount = 0;
                var today = Clock.Today;

                RemoveIfPossible(change.AllAccountsRate);
                RemoveIfPossible(change.NewAccountsOnlyRate);

                if (cancelledCount == 0)
                {
                    failedMessage = "Change could not be removed";
                    return false;
                }

                context.SaveChanges();

                failedMessage = null;
                return true;

                void RemoveIfPossible(SharedSavingsInterestRate s)
                {
                    if (s == null || s.RemovedByBusinessEventId.HasValue || s.ValidFromDate <= today) return;
                    s.RemovedByBusinessEvent = evt;
                    cancelledCount += 1;
                }
            }
        }
    }
}