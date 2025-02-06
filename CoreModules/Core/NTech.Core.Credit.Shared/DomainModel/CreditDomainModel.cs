using nCredit.DbModel.DomainModel;
using Newtonsoft.Json;
using NTech.Banking.Autogiro;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.Conversion;
using NTech.Banking.LoanModel;
using NTech.Core.Credit.Shared.Database;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nCredit.DomainModel
{
    public class CreditDomainModel
    {
        public enum AmountType
        {
            Capital,
            Interest,
            NotificationFee,
            ReminderFee
        }

        public bool GetIsDirectDebitActive(DateTime today, Action<DateTime> observeTransactionDate = null)
        {
            return (GetDatedCreditString(today, DatedCreditStringCode.IsDirectDebitActive, null, observeTransactionDate: observeTransactionDate, allowMissing: true) ?? "false") == "true";
        }

        public int? GetDirectDebitAccountOwnerApplicantNr(DateTime today)
        {
            var applicantNrRaw = GetDatedCreditString(today, DatedCreditStringCode.DirectDebitAccountOwnerApplicantNr, null, allowMissing: true);
            if (string.IsNullOrWhiteSpace(applicantNrRaw))
                return null;

            return int.Parse(applicantNrRaw);
        }

        public string GetActiveDirectDebitPaymentNumberOrNull(DateTime today, Action<DateTime> observeTransactionDate = null)
        {
            var isDirectDebitActive = GetIsDirectDebitActive(today, observeTransactionDate: observeTransactionDate);
            if (isDirectDebitActive)
            {
                var applicantNr = GetDirectDebitAccountOwnerApplicantNr(today);
                if (!applicantNr.HasValue)
                    return null;

                var g = new AutogiroPaymentNumberGenerator();
                return g.GenerateNr(this.CreditNr, applicantNr.Value);
            }
            return null;
        }

        public BankAccountNumberSe GetDirectDebitBankAccountNr(DateTime today)
        {
            var nr = GetDatedCreditString(today, DatedCreditStringCode.DirectDebitBankAccountNr, null, allowMissing: true);
            if (string.IsNullOrWhiteSpace(nr))
                return null;
            return BankAccountNumberSe.Parse(nr);
        }

        public static CreditAmortizationModel CreateAmortizationModel(string modelCode, Func<decimal> getAnnuityAmount, Func<decimal> getMonthlyFixedCapitalAmount, DateTime? amortizationExceptionUntilDate, decimal? exceptionAmortizationAmount)
        {
            if (modelCode == AmortizationModelCode.MonthlyAnnuity.ToString() || string.IsNullOrWhiteSpace(modelCode))
            {
                if (amortizationExceptionUntilDate.HasValue)
                    throw new Exception("Amortization exceptions are currently not supported for annuites");
                return CreditAmortizationModel.CreateAnnuity(getAnnuityAmount(), null);
            }
            else if (modelCode == AmortizationModelCode.MonthlyFixedAmount.ToString())
                return CreditAmortizationModel.CreateMonthlyFixedCapitalAmount(getMonthlyFixedCapitalAmount(), null, amortizationExceptionUntilDate, amortizationExceptionUntilDate.HasValue ? exceptionAmortizationAmount : null);
            else
                throw new ArgumentException("Unkown code", "modelCode");
        }

        private List<AmountType> AllAmountTypes
        {
            get
            {
                return Enum.GetValues(typeof(CreditDomainModel.AmountType)).Cast<CreditDomainModel.AmountType>().ToList();
            }
        }

        public string CreditNr
        {
            get
            {
                return creditNr;
            }
        }

        public int GetNrOfApplicants()
        {
            return cachedNrOfApplicants.Value;
        }

        public DateTimeOffset GetStartDate()
        {
            return cachedStartDate.Value;
        }

        public CreditType GetCreditType()
        {
            return cachedCreditType.Value;
        }

        public CreditStatus GetStatus()
        {
            return (CreditStatus)Enum.Parse(typeof(CreditStatus), cachedStatus, true);
        }

        public CreditStatus GetStatus(DateTime transactionDate, Action<DateTime> observeTransactionDate = null)
        {
            var s = GetDatedCreditString(transactionDate, DatedCreditStringCode.CreditStatus, null, observeTransactionDate: observeTransactionDate);
            return (CreditStatus)Enum.Parse(typeof(CreditStatus), s, true);
        }

        public string GetIntialLoanCampaignCode(DateTime transactionDate, Action<DateTime> observeTransactionDate = null)
        {
            return GetDatedCreditString(transactionDate, DatedCreditStringCode.IntialLoanCampaignCode, null, observeTransactionDate: observeTransactionDate, allowMissing: true);
        }

        public CreditAmortizationModel GetAmortizationModel(DateTime transactionDate)
        {
            Func<AmortizationModelCode> getAmortizationModelCode = () =>
            {
                var s = GetDatedCreditString(transactionDate, DatedCreditStringCode.AmortizationModel, null, allowMissing: true);
                if (s == null)
                    return AmortizationModelCode.MonthlyAnnuity;
                else
                    return (AmortizationModelCode)Enum.Parse(typeof(AmortizationModelCode), s, true);
            };
            var amortizationModelCode = getAmortizationModelCode();

            var amortizationExceptionUntilDate = GetDatedCreditDate(transactionDate, DatedCreditDateCode.AmortizationExceptionUntilDate, null);
            var exceptionAmortizationAmount = amortizationExceptionUntilDate.HasValue
                ? GetDatedCreditValueOpt(transactionDate, DatedCreditValueCode.ExceptionAmortizationAmount)
                : null;

            return CreditDomainModel.CreateAmortizationModel(amortizationModelCode.ToString(),
                () => GetDatedCreditValue(transactionDate, DatedCreditValueCode.AnnuityAmount),
                () => GetDatedCreditValue(transactionDate, DatedCreditValueCode.MonthlyAmortizationAmount), amortizationExceptionUntilDate, exceptionAmortizationAmount);
        }

        public string GetProviderName()
        {
            return cachedProviderName;
        }

        public DateTime GetMaxTransactionDate() =>
            cachedCreditLevelTransactions.Select(x => x.TransactionDate).Max();

        public DateTime GetMinTransactionDate() =>
            cachedCreditLevelTransactions.Select(x => x.TransactionDate).Min();

        public decimal GetNotNotifiedCapitalBalance(DateTime transactionDate)
        {
            return GetTransactions(transactionDate)
                .Where(x => x.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString())
                .Sum(x => (decimal?)x.Amount) ?? 0m;
        }

        public Dictionary<string, decimal> GetNotNotifiedNotificationCosts(DateTime transactionDate) =>
            GetTransactions(transactionDate)
                .Where(x => x.AccountCode == TransactionAccountType.NotNotifiedNotificationCost.ToString() && x.SubAccountCode != null)
                .GroupBy(x => x.SubAccountCode)
                .Select(x => new
                {
                    SubAccountCode = x.Key,
                    Amount = x.Sum(y => y.Amount)
                })
                .ToList()
                .Where(x => x.Amount > 0)
                .ToDictionary(x => x.SubAccountCode, x => x.Amount);

        public decimal GetNotificationCostsBalance(DateTime transactionDate, string onlyThisSubAccountCode = null)
        {
            var q = GetTransactions(transactionDate).Where(x => x.AccountCode == TransactionAccountType.NotificationCost.ToString());
            if (onlyThisSubAccountCode != null)
                q = q.Where(x => x.SubAccountCode == onlyThisSubAccountCode);
            return q.Sum(x => x.Amount);
        }

        public decimal GetBalance(DateTime transactionDate)
        {
            return AllAmountTypes.Select(x => GetBalance(x, transactionDate)).Sum() + GetNotificationCostsBalance(transactionDate);
        }

        private ISet<DateTime> GetCapitalBalanceTransactionDates(DateTime transactionDate)
        {
            return new HashSet<DateTime>(GetTransactions(transactionDate).Where(x => x.AccountCode == TransactionAccountType.CapitalDebt.ToString()).Select(x => x.TransactionDate));
        }

        public decimal GetBalance(AmountType amountType, DateTime transactionDate)
        {
            if (amountType == AmountType.Capital)
            {
                return GetTransactions(transactionDate)
                    .Where(x => x.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                    .Sum(x => (decimal?)x.Amount) ?? 0m;
            }
            else
            {
                var code = MapNonCapitalAmountTypeToAccountType(amountType).ToString();
                return GetTransactions(transactionDate)
                    .Where(x => x.AccountCode == code)
                    .Sum(x => (decimal?)x.Amount) ?? 0m;
            }
        }

        public decimal GetPaidAmount(AmountType type, DateTime transactionDate)
        {
            if (type == AmountType.Capital)
            {
                return -GetTransactions(transactionDate)
                    .Where(x => x.AccountCode == TransactionAccountType.CapitalDebt.ToString() && x.IncomingPaymentId.HasValue)
                    .Sum(x => (decimal?)x.Amount) ?? 0m;
            }
            else
            {
                var code = MapNonCapitalAmountTypeToAccountType(type).ToString();
                return -GetTransactions(transactionDate)
                    .Where(x => x.AccountCode == code && x.IncomingPaymentId.HasValue)
                    .Sum(x => (decimal?)x.Amount) ?? 0m; ;
            }
        }

        public decimal GetWrittenOffAmount(AmountType type, DateTime transactionDate)
        {
            if (type == AmountType.Capital)
            {
                return -GetTransactions(transactionDate)
                    .Where(x => x.AccountCode == TransactionAccountType.CapitalDebt.ToString() && x.WriteoffId != null)
                    .Sum(x => (decimal?)x.Amount) ?? 0m; ;
            }
            else
            {
                var code = MapNonCapitalAmountTypeToAccountType(type);
                return -GetTransactions(transactionDate)
                    .Where(x => x.AccountCode == code.ToString() && x.WriteoffId != null)
                    .Sum(x => (decimal?)x.Amount) ?? 0m; ;
            }
        }

        public decimal GetTotalBusinessEventAmount(BusinessEventType businessEventType, TransactionAccountType accountType, DateTime transactionDate)
        {
            return GetTransactions(transactionDate)
                .Where(x => x.BusinessEventType == businessEventType.ToString() && x.AccountCode == accountType.ToString())
                .Sum(x => x.Amount);
        }

        public decimal ComputeNotNotifiedInterestUntil(DateTime transactionDate, DateTime untilDate, out int nrOfInterestDays)
        {
            var interestFromDate = GetNextInterestFromDate(transactionDate);

            return ComputeInterestBetweenDays(interestFromDate, untilDate, d => GetBalance(CreditDomainModel.AmountType.Capital, d),
                d => GetInterestRatePercent(d),
                () => GetDatedCreditValuesChangeDays(untilDate, DatedCreditValueCode.MarginInterestRate, DatedCreditValueCode.ReferenceInterestRate),
                () => GetCapitalBalanceTransactionDates(untilDate),
                out nrOfInterestDays,
                GetInterestDividerOverrideByCode(this.interestModelCode));
        }

        public static decimal ComputeInterestBetweenDays(DateTime fromDate, DateTime toDate,
            Func<DateTime, decimal> getCapitalBalance,
            Func<DateTime, decimal> getInterestRatePercent,
            Func<ISet<DateTime>> getInterestRatePercentChangeDays,
            Func<ISet<DateTime>> getCapitalBalanceChangeDays,
            out int nrOfInterestDays,
            Func<DateTime, decimal> getDividerOverride,
            Action<DateTime, decimal> observeDailyInterestAmount = null)
        {
            nrOfInterestDays = 0;

            if (fromDate > toDate)
            {
                return 0m;
            }

            decimal? interestRate = null;
            var interestChangeDays = (getInterestRatePercentChangeDays?.Invoke());

            decimal? capitalBalance = null;
            var capitalBalanceChangeDays = (getCapitalBalanceChangeDays?.Invoke());

            var d = fromDate;
            var interestAmount = 0m;
            while (d <= toDate)
            {
                if (!interestRate.HasValue || (interestChangeDays != null && interestChangeDays.Contains(d)))
                {
                    interestRate = getInterestRatePercent(d);
                }
                if (!capitalBalance.HasValue || (capitalBalanceChangeDays != null && capitalBalanceChangeDays.Contains(d)))
                {
                    capitalBalance = getCapitalBalance(d);
                }
                var divider = getDividerOverride(d);

                var interestDayAmount = capitalBalance.Value * interestRate.Value / 100m / divider;
                if (observeDailyInterestAmount != null)
                {
                    observeDailyInterestAmount(d, interestDayAmount);
                }
                interestAmount += interestDayAmount;
                d = d.AddDays(1);
                nrOfInterestDays++;
            }
            return Math.Round(interestAmount, 2);
        }

        public static Func<DateTime, decimal> GetInterestDividerOverrideByCode(InterestModelCode interestModelCode)
        {
            switch (interestModelCode)
            {
                case InterestModelCode.Actual_360:
                    return _ => 360m;

                case InterestModelCode.Actual_365_25:
                    return _ => 365.25m;

                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Compute the interest amount for a date interval based only on capital balance and interest rate one those dates and ignoring of those days have already been passed by
        /// interestFromDate. This is used for example when doing early settlement and there is and outstanding notification with interest computed into the future. This is then used to compute
        /// how much interest to write off.
        /// </summary>
        /// <param name="transactionDate">When is now</param>
        /// <param name="dateInterval">Inclusive from and to date to calculate interest between</param>
        /// <param name="observeNrOfInterestDays">Get back interest days</param>
        /// <returns>Interest amount</returns>
        public decimal ComputeInterestAmountIgnoringInterestFromDate(DateTime transactionDate, Tuple<DateTime, DateTime> dateInterval, Action<int> observeNrOfInterestDays = null)
        {
            var fromDate = dateInterval.Item1;
            var toDate = dateInterval.Item2;
            int nrOfInterestDays;
            var futureInterestAmount = ComputeInterestBetweenDays(
                fromDate,
                toDate,
                x => GetBalance(AmountType.Capital, x),
                GetInterestRatePercent,
                () => GetDatedCreditValuesChangeDays(transactionDate, DatedCreditValueCode.MarginInterestRate, DatedCreditValueCode.ReferenceInterestRate),
                () => GetCapitalBalanceTransactionDates(transactionDate), out nrOfInterestDays,
                GetInterestDividerOverrideByCode(this.interestModelCode));

            observeNrOfInterestDays?.Invoke(nrOfInterestDays);

            return futureInterestAmount;
        }

        public string GetSignedInitialAgreementArchiveKey(DateTime transactionDate, int? applicantNr)
        {
            return GetDatedCreditString(transactionDate, DatedCreditStringCode.SignedInitialAgreementArchiveKey, applicantNr, allowMissing: true);
        }

        public DateTime? GetMortgageLoanEndDate(DateTime transactionDate)
        {
            if (GetCreditType() == CreditType.MortgageLoan)
                return GetDatedCreditDate(transactionDate, DatedCreditDateCode.MortgageLoanEndDate, null);
            else
                return null;
        }

        public string GetOcrPaymentReference(DateTime transactionDate)
        {
            return GetDatedCreditString(transactionDate, DatedCreditStringCode.OcrPaymentReference, null);
        }

        public DateTime GetNextInterestFromDate(DateTime transactionDate)
        {
            var v = GetDatedCreditString(transactionDate, DatedCreditStringCode.NextInterestFromDate, null);
            return DateTime.ParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public DateTime GetNextInterestFromDateWithValueLessThan(DateTime transactionDate, DateTime maxValue) =>

            GetDatedCreditStrings(transactionDate)
                .Where(x => x.Name == DatedCreditStringCode.NextInterestFromDate.ToString())
                .Select(x => new
                {
                    x.TransactionDate,
                    NextInterestFromDate = DateTime.ParseExact(x.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                })
                .Where(x => x.NextInterestFromDate < maxValue)
                .OrderByDescending(x => x.TransactionDate)
                .First()
                .NextInterestFromDate;

        public decimal GetInterestRatePercent(DateTime transactionDate)
        {
            return GetDatedCreditValue(transactionDate, DatedCreditValueCode.MarginInterestRate) + GetDatedCreditValue(transactionDate, DatedCreditValueCode.ReferenceInterestRate, defaultValue: 0m);
        }

        public ISet<DateTime> GetDatedCreditValuesChangeDays(DateTime transactionDate, params DatedCreditValueCode[] codes)
        {
            var cs = codes.Select(x => x.ToString()).ToList();
            return new HashSet<DateTime>(GetDatedCreditValues(transactionDate)
                .Where(x => cs.Contains(x.Name))
                .Select(x => x.TransactionDate));
        }

        public DateTime? GetPromisedToPayDate(DateTime transactionDate)
        {
            return GetDatedCreditDate(transactionDate, DatedCreditDateCode.PromisedToPayDate, null);
        }

        public DateTime? GetDebtCollectionPausedUntilDate(DateTime transactionDate)
        {
            return GetDatedCreditDate(transactionDate, DatedCreditDateCode.DebtCollectionPausedUntilDate, null);
        }

        public decimal GetNotificationFee(DateTime transactionDate)
        {
            return GetDatedCreditValue(transactionDate, DatedCreditValueCode.NotificationFee, defaultValue: 0m);
        }

        public string GetApplicationNr(DateTime transactionDate, bool allowMissing = false)
        {
            return GetDatedCreditString(transactionDate, DatedCreditStringCode.ApplicationNr, null, allowMissing: allowMissing);
        }

        public int? GetSinglePaymentLoanRepaymentDays()
        {
            var value = cachedDatedValues.FirstOrDefault(x => x.Name == DatedCreditValueCode.SinglePaymentLoanRepaymentDays.ToString())?.Value;
            return value.HasValue ? (int)value.Value : new int?();
        }

        public decimal GetDatedCreditValue(DateTime transactionDate, DatedCreditValueCode code, decimal? defaultValue = null)
        {
            var v = GetDatedCreditValues(transactionDate)
                .Where(x => x.Name == code.ToString())
                .OrderByDescending(x => x.TransactionDate)
                .Select(x => (decimal?)x.Value)
                .FirstOrDefault();
            if (!v.HasValue)
            {
                if (defaultValue.HasValue)
                    return defaultValue.Value;
                else
                    throw new Exception($"Value {code} on credit {CreditNr} has no value for date {transactionDate}");
            }

            return v.Value;
        }

        public decimal? GetDatedCreditValueOpt(DateTime transactionDate, DatedCreditValueCode code)
        {
            return GetDatedCreditValues(transactionDate)
                .Where(x => x.Name == code.ToString())
                .OrderByDescending(x => x.TransactionDate)
                .Select(x => (decimal?)x.Value)
                .FirstOrDefault();
        }

        public string GetDatedCreditString(DateTime transactionDate, DatedCreditStringCode code, int? applicantNr, Action<DateTime> observeTransactionDate = null, bool allowMissing = false)
        {
            var c = code.ToString();
            if (applicantNr != null) c += applicantNr.Value.ToString();
            var v = GetDatedCreditStrings(transactionDate)
                .Where(x => x.Name == c)
                .OrderByDescending(x => x.TransactionDate)
                .FirstOrDefault();

            if (v != null && observeTransactionDate != null)
                observeTransactionDate(v.TransactionDate);

            var vv = v?.Value;
            if (vv == null)
            {
                if (allowMissing)
                    return null;
                else
                    throw new Exception($"String Value {code} on credit {CreditNr} has no value for date {transactionDate}");
            }

            return vv;
        }

        public DateTime? GetDatedCreditDate(DateTime transactionDate, DatedCreditDateCode code, int? applicantNr, Action<DateTime> observeTransactionDate = null)
        {
            var c = code.ToString();
            if (applicantNr != null) c += applicantNr.Value.ToString();
            var v = GetDatedCreditDates(transactionDate)
                .Where(x => x.Name == c)
                .OrderByDescending(x => x.TransactionDate)
                .FirstOrDefault();

            if (v == null)
                return null;

            if (v.RemovedByBusinessEventId.HasValue)
                return null;

            if (observeTransactionDate != null)
                observeTransactionDate(v.TransactionDate);

            return v.Value;
        }

        public List<string> GetAmortizationExceptionReasons(DateTime transactionDate)
        {
            var value = GetDatedCreditString(transactionDate, DatedCreditStringCode.AmortizationExceptionReasons, null, allowMissing: true);
            if (value == null)
                return new List<string>();
            return JsonConvert.DeserializeObject<List<string>>(value);
        }

        private IQueryable<DatedCreditValueModel> GetDatedCreditValues(DateTime transactionDate) =>
            cachedDatedValues.Where(x => x.TransactionDate <= transactionDate).AsQueryable();

        private IQueryable<AccountTransactionModel> GetTransactions(DateTime transactionDate) =>
            cachedCreditLevelTransactions.Where(x => x.TransactionDate <= transactionDate).AsQueryable();

        private IQueryable<DatedCreditStringModel> GetDatedCreditStrings(DateTime transactionDate) => cachedDatedCreditStrings.Where(x => x.TransactionDate <= transactionDate).AsQueryable();

        private IQueryable<DatedCreditDateModel> GetDatedCreditDates(DateTime transactionDate) => cachedDatedCreditDates.Where(x => x.TransactionDate <= transactionDate).AsQueryable();

        public static TransactionAccountType MapNonCapitalAmountTypeToAccountType(AmountType t)
        {
            switch (t)
            {
                case AmountType.Interest: return TransactionAccountType.InterestDebt;
                case AmountType.NotificationFee: return TransactionAccountType.NotificationFeeDebt;
                case AmountType.ReminderFee: return TransactionAccountType.ReminderFeeDebt;
                default: throw new NotImplementedException();
            }
        }

        private IList<AccountTransactionModel> cachedCreditLevelTransactions = null;
        private IList<DatedCreditValueModel> cachedDatedValues = null;
        private IList<DatedCreditStringModel> cachedDatedCreditStrings = null;
        private IList<DatedCreditDateModel> cachedDatedCreditDates = null;
        private DateTimeOffset? cachedStartDate = null;
        private CreditType? cachedCreditType = null;
        private int? cachedNrOfApplicants = null;
        private string cachedStatus = null;
        private string cachedProviderName = null;
        private string creditNr;
        private readonly InterestModelCode interestModelCode;

        private CreditDomainModel(string creditNr, bool isMortgageLoansEnabled, InterestModelCode interestModelCode)
        {
            this.creditNr = creditNr;
            this.interestModelCode = interestModelCode;
        }

        public static CreditDomainModel PreFetchForSingleCredit(string creditNr, ICreditContextExtended context, ICreditEnvSettings envSettings)
        {
            var c = new CreditDomainModel(creditNr, envSettings.IsMortgageLoansEnabled, envSettings.ClientInterestModel);
            PrefetchData(context, (_, f) => f(c), new[] { creditNr });
            return c;
        }

        public static IDictionary<string, CreditDomainModel> PreFetchForCredits(ICreditContextExtended context, string[] creditNrs, ICreditEnvSettings envSettings)
        {
            var cs = creditNrs.Distinct().ToDictionary(x => x, x => new CreditDomainModel(x, envSettings.IsMortgageLoansEnabled, envSettings.ClientInterestModel));

            PrefetchData(context, (creditNr, f) => f(cs[creditNr]), creditNrs);

            return cs;
        }

        private static void PrefetchData(ICreditContextExtended context, Action<string, Action<CreditDomainModel>> update, string[] creditNrs)
        {
            var allTransactions = GetAccountTransactionsQuery(context);
            
            /*
             NOTE: The way this is split into multiple small queries is not ideal for one of queries but this
             starts timing out for larger databases on things like importing payment files.
             If you change this, make sure to test the changes under those conditions
             */

            var ds = context
                .CreditHeadersQueryable
                .Where(x => creditNrs.Contains(x.CreditNr))
                .Select(x =>
                    new
                    {
                        x.CreditNr,
                        x.Status,
                        x.NrOfApplicants,
                        x.StartDate,
                        x.ProviderName,
                        x.CreditType
                    })
                .ToList();

            var datedCreditValues = context
                .CreditHeadersQueryable
                .Where(x => creditNrs.Contains(x.CreditNr))
                .Select(x =>
                    new
                    {
                        x.CreditNr,
                        DatedCreditValues = x.DatedCreditValues.GroupBy(y => new { y.Name, y.TransactionDate }).Select(y => y.OrderByDescending(z => z.Id)
                            .Select(z => new DatedCreditValueModel
                            {
                                Name = z.Name,
                                TransactionDate = z.TransactionDate,
                                Value = z.Value
                            }).FirstOrDefault())
                    })
                .ToList()
                .ToDictionary(x => x.CreditNr, x => x.DatedCreditValues);

            var datedCreditStrings = context
                .CreditHeadersQueryable
                .Where(x => creditNrs.Contains(x.CreditNr))
                .Select(x =>
                    new
                    {
                        x.CreditNr,
                        DatedCreditStrings = x.DatedCreditStrings.GroupBy(y => new { y.Name, y.TransactionDate }).Select(y => y.OrderByDescending(z => z.Id)
                            .Select(z => new DatedCreditStringModel
                            {
                                Name = z.Name,
                                TransactionDate = z.TransactionDate,
                                Value = z.Value
                            }).FirstOrDefault())
                    })
                .ToList()
                .ToDictionary(x => x.CreditNr, x => x.DatedCreditStrings);

            var datedCreditDates = context
                .CreditHeadersQueryable
                .Where(x => creditNrs.Contains(x.CreditNr))
                .Select(x =>
                    new
                    {
                        x.CreditNr,
                        DatedCreditDates = x.DatedCreditDates.GroupBy(y => new { y.Name, y.TransactionDate }).Select(y => y.OrderByDescending(z => z.Id)
                            .Select(z => new DatedCreditDateModel
                            {
                                Name = z.Name,
                                TransactionDate = z.TransactionDate,
                                Value = z.Value,
                                RemovedByBusinessEventId = z.RemovedByBusinessEventId
                            }).FirstOrDefault())
                    })
                .ToList()
                .ToDictionary(x => x.CreditNr, x => x.DatedCreditDates);

            var transactionsByCreditNr = allTransactions
                .Where(x => creditNrs.Contains(x.CreditNr))
                .ToList()
                .GroupBy(x => x.CreditNr)
                .ToDictionary(x => x.Key, x => x.ToList());

            foreach (var gd in ds)
            {
                var d = gd;
                update(d.CreditNr, c =>
                {
                    c.cachedCreditLevelTransactions = transactionsByCreditNr.Opt(d.CreditNr).ToList();
                    c.cachedDatedValues = datedCreditValues.Opt(d.CreditNr)?.ToList() ?? new List<DatedCreditValueModel>();
                    c.cachedNrOfApplicants = d.NrOfApplicants;
                    c.cachedStatus = d.Status;
                    c.cachedProviderName = d.ProviderName;
                    c.cachedDatedCreditStrings = datedCreditStrings.Opt(d.CreditNr)?.ToList() ?? new List<DatedCreditStringModel>();
                    c.cachedDatedCreditDates = datedCreditDates.Opt(d.CreditNr)?.ToList() ?? new List<DatedCreditDateModel>();
                    c.cachedCreditType = ParseCreditTypeFromDb(d.CreditType);
                    c.cachedStartDate = d.StartDate;
                });
            }
        }

        private static CreditType ParseCreditTypeFromDb(string creditType)
        {
            return Enums.Parse<CreditType>(creditType ?? "UnsecuredLoan").Value; //String on the inside to prevent issues with a third loan type defaulting to Unsecured. We only want null to default to Unsecured
        }

        private static IQueryable<AccountTransactionModel> GetAccountTransactionsQuery(ICreditContextExtended context)
        {
            return context.TransactionsQueryable.Select(x => new AccountTransactionModel
            {
                Id = x.Id,
                AccountCode = x.AccountCode,
                Amount = x.Amount,
                BusinessEventType = x.BusinessEvent.EventType,
                CreditNr = x.CreditNr,
                TransactionDate = x.TransactionDate,
                SubAccountCode = x.SubAccountCode, //TODO: Do we fall out of indexes now?
                WriteoffId = x.WriteoffId,
                IncomingPaymentId = x.IncomingPaymentId
            });
        }

        public class AccountTransactionModel
        {
            public long Id { get; set; }
            public string AccountCode { get; set; }
            public string BusinessEventType { get; set; }
            public string CreditNr { get; set; }
            public int? IncomingPaymentId { get; set; }
            public int? WriteoffId { get; set; }
            public decimal Amount { get; set; }
            public DateTime TransactionDate { get; set; }
            public string SubAccountCode { get; set; }
        }


        private class DatedCreditStringModel
        {
            public string Name { get; set; }
            public DateTime TransactionDate { get; set; }
            public string Value { get; set; }
        }

        private class DatedCreditValueModel
        {
            public string Name { get; set; }
            public DateTime TransactionDate { get; set; }
            public decimal Value { get; set; }
        }

        private class DatedCreditDateModel
        {
            public string Name { get; set; }
            public DateTime TransactionDate { get; set; }
            public int? RemovedByBusinessEventId { get; set; }
            public DateTime Value { get; set; }
        }
    }
}