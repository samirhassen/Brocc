using nCredit.Code.Uc.CreditRegistry;
using nCredit.DbModel.Repository;
using NTech.Banking.CivicRegNumbers;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code.Services
{
    public class UcCreditRegistryService : IUcCreditRegistryService
    {
        private readonly Func<UcCreditRegistrySettingsModel> getSettings;
        private readonly ICoreClock clock;
        private readonly bool isMortgageLoansEnabled;
        private readonly Func<ICustomerClient> createCustomerClient;
        private readonly Func<CivicRegNumberParser> getCivicRegNumberParser;
        private readonly CreditContextFactory contextFactory;

        public UcCreditRegistryService(Func<UcCreditRegistrySettingsModel> getSettings, ICoreClock clock,
            bool isMortgageLoansEnabled, Func<ICustomerClient> createCustomerClient, Func<CivicRegNumberParser> getCivicRegNumberParser,
            CreditContextFactory contextFactory)
        {
            this.getSettings = getSettings;
            this.clock = clock;
            this.isMortgageLoansEnabled = isMortgageLoansEnabled;
            this.createCustomerClient = createCustomerClient;
            this.getCivicRegNumberParser = getCivicRegNumberParser;
            this.contextFactory = contextFactory;
        }

        private T MaxChecked<T>(T? a, T? b) where T : struct, IComparable<T>
        {
            if (a.HasValue && b.HasValue)
                return a.Value.CompareTo(b.Value) > 0 ? a.Value : b.Value;
            else if (a.HasValue)
                return a.Value;
            else if (b.HasValue)
                return b.Value;
            else
                throw new Exception("Logical error. No transaction and no status change. This credit should not be included at all");
        }

        public void ReportCreditsChangedSinceLastReport(INTechCurrentUserMetadata u)
        {
            var s = new CoreSystemItemRepository(u);
            using (var context = contextFactory.CreateContext())
            {
                var startAfterBusinessEventId = s.GetInt(SystemItemCode.UcCreditRegistry_Daily_LatestReportedBusinessEventId, context) ?? 0;

                var credits = GetCreditModels(startAfterBusinessEventId);

                ReportCredits(credits);

                var newMaxBusinessEventId = credits.Max(x => (int?)x.MaxIncludedBusinessEventId);
                if (newMaxBusinessEventId.HasValue)
                {
                    s.SetInt(SystemItemCode.UcCreditRegistry_Daily_LatestReportedBusinessEventId, newMaxBusinessEventId.Value, context);
                    context.SaveChanges();
                }
            }
        }

        public List<CreditModel> GetCreditModels(int startAfterBusinessEventId)
        {
            using (var context = contextFactory.CreateContext())
            {
                var changes = context
                    .CreditHeadersQueryable
                    .Select(x => new
                    {
                        x.StartDate,
                        x.CreatedByBusinessEventId,
                        CreatedTransactionDate = x.CreatedByEvent.TransactionDate,
                        LatestCapitalTransaction = x
                            .Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString() && y.BusinessEventId > startAfterBusinessEventId)
                            .OrderByDescending(y => y.BusinessEventId)
                            .Select(y => new
                            {
                                y.BusinessEventId,
                                y.TransactionDate
                            })
                            .FirstOrDefault(),
                        x.CreditNr,
                        Balance = x
                            .Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                            .Sum(y => (decimal?)y.Amount) ?? 0m,
                        Status = x.Status,
                        LatestStatusChange = x
                            .DatedCreditStrings
                            .Where(y => y.Name == DatedCreditStringCode.CreditStatus.ToString() && y.BusinessEventId > startAfterBusinessEventId)
                            .OrderByDescending(y => y.BusinessEventId)
                            .Select(y => new
                            {
                                y.BusinessEventId,
                                y.Value,
                                y.TransactionDate
                            })
                            .FirstOrDefault(),
                        CustomerIds = x.CreditCustomers.Select(y => new
                        {
                            y.ApplicantNr,
                            y.CustomerId
                        }),
                        CollateralObjectTypeCode = x
                            .Collateral
                            .Items
                            .Where(y => y.ItemName == "objectTypeCode" && y.RemovedByBusinessEventId == null)
                            .Select(y => y.StringValue)
                            .FirstOrDefault()
                    })
                    .Where(x =>
                        //Closed loans that have never been reported while open should not be reported as closed
                        //Except when opened and closed the same day because uc uses that to remove things ... this service is incredibly strange.
                        !(x.Status != CreditStatus.Normal.ToString() && x.CreatedByBusinessEventId >= startAfterBusinessEventId && x.LatestStatusChange.TransactionDate != x.CreatedTransactionDate)
                        && (
                            //Balance change
                            x.LatestCapitalTransaction != null
                            ||
                            //Loan has been closed
                            (x.LatestStatusChange != null && x.LatestStatusChange.Value != CreditStatus.Normal.ToString()))
                           )
                    .ToList();

                var p = getCivicRegNumberParser();
                var customerIds = new HashSet<int>(changes.SelectMany(x => x.CustomerIds.Select(y => y.CustomerId)));
                var cc = createCustomerClient();
                var civicRegNrByCustomerId = cc
                    .BulkFetchPropertiesByCustomerIdsD(customerIds, "civicRegNr")
                    .ToDictionary(x => x.Key, x => p.Parse(x.Value["civicRegNr"]));

                return changes.Select(x => new CreditModel
                {
                    Balance = x.Balance,
                    CreditNr = x.CreditNr,
                    MaxIncludedBusinessEventId = MaxChecked(x.LatestCapitalTransaction?.BusinessEventId, x.LatestStatusChange?.BusinessEventId),
                    StartDate = x.StartDate.DateTime,
                    EndDate = GetEndDate(x.Status, x.LatestStatusChange?.TransactionDate),
                    CreditType = isMortgageLoansEnabled
                        ? GetMortgageLonCreditType(x.CollateralObjectTypeCode)
                        : CreditTypeCode.Blanco,
                    TransactionDate = MaxChecked(x.LatestCapitalTransaction?.TransactionDate, x.LatestStatusChange?.TransactionDate),
                    MainApplicantCivicRegNr = civicRegNrByCustomerId[x.CustomerIds.Single(y => y.ApplicantNr == 1).CustomerId],
                    CoMainApplicantCivicRegNrs = x.CustomerIds.Where(y => y.ApplicantNr > 1).Select(y => civicRegNrByCustomerId[y.CustomerId]).ToList()
                }).ToList();
            }
        }

        private DateTime? GetEndDate(string status, DateTime? transactionDate)
        {
            if (status == CreditStatus.Settled.ToString())
                return transactionDate;
            else if (status == CreditStatus.WrittenOff.ToString())
                return transactionDate;
            else
                return null;
        }

        private CreditTypeCode GetMortgageLonCreditType(string collateralObjectTypeCode)
        {
            if (collateralObjectTypeCode == "seFastighet")
            {
                return CreditTypeCode.HouseAsSecurity;
            }
            else if (collateralObjectTypeCode == "seBrf")
            {
                return CreditTypeCode.OwnedApartmentAsSecurity;
            }
            else
            {
                return CreditTypeCode.OwnedApartmentAsSecurity;
            }
        }

        private void ReportCredits(List<CreditModel> credits)
        {
            CreditRegister c;
            var settings = getSettings();
            var client = new UcCreditRegistryWebserviceClient(
                new Lazy<Uri>(() => settings.SharkEndpoint),
                new Lazy<Tuple<string, string>>(() => Tuple.Create(settings.SharkUsername, settings.SharkPassword)),
                new Lazy<string>(() => settings.LogFolder));

            if (credits == null || credits.Count == 0)
                c = CreateNothingModel(settings);
            else
                c = CreateSomethingModel(credits, settings);

            client.Send(c);
        }

        private CreditRegister CreateNothingModel(UcCreditRegistrySettingsModel settings)
        {
            var c = new CreditRegister();

            c.Item = new CreditRegisterNoDeliveryToday
            {
                creditorID = settings.SharkCreditorId,
                sourceSystemID = settings.SharkSourceSystemId,
                deliveryUniqueID = settings.SharkDeliveryUniqueId,
                deliveryTimeStamp = NudgeDate(clock.Now.DateTime)
            };

            return c;
        }

        public class CreditModel
        {
            public decimal Balance { get; set; }
            public CreditTypeCode CreditType { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public int MaxIncludedBusinessEventId { get; set; }
            public DateTime TransactionDate { get; set; }
            public string CreditNr { get; set; }
            public ICivicRegNumber MainApplicantCivicRegNr { get; set; }
            public List<ICivicRegNumber> CoMainApplicantCivicRegNrs { get; set; }
        }

        public enum CreditTypeCode : int
        {
            /// <summary>
            /// Blanco/guarantor credits. Refers to unsecured loans or the part of a credit that lacks collateral. For mixed loans where a certain part of the credit is unsecured and another partially covered by collateral, only the unsecured  part are to be reported on this type of credit and the remainder on another type of credit equivalent to the collateral. Report limit as 0 unless the account has been over drafted, then report correct limit.
            /// </summary>
            Blanco = 5,

            /// <summary>
            /// Credit with real estate as collateral. These loans are collateralized by property that are classified as a one or two family real estate. Other classifications of real estates should be placed under credit type 6. Report limit as 0 unless the account has been over drafted, then report correct limit. 
            /// </summary>
            HouseAsSecurity = 7,

            /// <summary>
            /// Credit with an owned apartment as collateral. These loans are collateralized by assets classified as condominium, owned apartment or proprietary. Report limit as 0 unless the account has been over drafted, then report correct limit. 
            /// </summary>
            OwnedApartmentAsSecurity = 9
        }

        private IEnumerable<Tuple<ICivicRegNumber, bool>> GetApplicants(CreditModel c)
        {
            yield return Tuple.Create(c.MainApplicantCivicRegNr, false);
            foreach (var a in c.CoMainApplicantCivicRegNrs)
                yield return Tuple.Create(a, true);
        }

        /// <summary>
        /// So Uc is just super strange.
        /// They told us during testing that we cant send dates with zero time parts ... so why have a time if we cant send any value?
        /// To handle this we change any hour minute or second that is 00 to 01. The balance still ends up on the same date which is the only thing that matters
        /// so this is just stupid.
        /// </summary>
        private DateTime NudgeDate(DateTime date)
        {
            var d = date.Hour == 0 ? date.AddHours(1) : date;
            d = d.Minute == 0 ? d.AddMinutes(1) : d;
            d = d.Second == 0 ? d.AddSeconds(1) : d;
            return d;
        }

        private CreditRegister CreateSomethingModel(List<CreditModel> credits, UcCreditRegistrySettingsModel settings)
        {
            var c = new CreditRegister();

            c.Item = new CreditRegisterDeliveryInfo
            {
                creditorID = settings.SharkCreditorId,
                sourceSystemID = settings.SharkSourceSystemId,
                deliveryUniqueID = settings.SharkDeliveryUniqueId,
                deliveryTimeStamp = clock.Now.DateTime,
                CreditContracts = new CreditRegisterDeliveryInfoCreditContracts[]
                {
                    new CreditRegisterDeliveryInfoCreditContracts
                    {
                        creditorID = settings.SharkCreditorId,
                        groupID = settings.SharkGroupId,
                        Contract = credits.SelectMany(x =>
                        {
                            return GetApplicants(x)
                                .Select(y =>  new CreditRegisterDeliveryInfoCreditContractsContract
                                {
                                    startDate = NudgeDate(x.StartDate),
                                    endDate = x.EndDate.HasValue ? NudgeDate(x.EndDate.Value) : default(DateTime),
                                    endDateSpecified = x.EndDate.HasValue,
                                    accountNum = new CreditRegisterDeliveryInfoCreditContractsContractAccountNum
                                    {
                                        creditType = (int)x.CreditType,
                                        Value = x.CreditNr
                                    },
                                    objectID = new CreditRegisterDeliveryInfoCreditContractsContractObjectID
                                    {
                                        coApplicant = y.Item2,
                                        country = CountryCode_ST.SE,
                                        idType = CreditRegisterDeliveryInfoCreditContractsContractObjectIDIdType.PersonalIDNum,
                                        Value = y.Item1.NormalizedValue
                                    },
                                    Balance = new CreditRegisterDeliveryInfoCreditContractsContractBalance
                                    {
                                        amount = x.Balance,
                                        timeStamp = NudgeDate(x.TransactionDate),
                                        limit = 0m,
                                        currency = Currency_ST.SEK
                                    }
                                });
                        }).ToArray()
                    }
                }
            };

            return c;
        }
    }

    public interface IUcCreditRegistryService
    {
        List<UcCreditRegistryService.CreditModel> GetCreditModels(int startAfterBusinessEventId);
        void ReportCreditsChangedSinceLastReport(INTechCurrentUserMetadata u);
    }
}