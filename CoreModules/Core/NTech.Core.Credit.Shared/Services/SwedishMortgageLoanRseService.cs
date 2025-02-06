using nCredit;
using nCredit.DbModel.BusinessEvents;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using nCredit.Excel;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class SwedishMortgageLoanRseService
    {
        private readonly CreditContextFactory contextFactory;
        private readonly INotificationProcessSettingsFactory notificationProcessSettingsFactory;
        private readonly ICoreClock clock;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly ICreditEnvSettings envSettings;

        public SwedishMortgageLoanRseService(CreditContextFactory contextFactory, INotificationProcessSettingsFactory notificationProcessSettingsFactory,
            ICoreClock clock, IClientConfigurationCore clientConfiguration, ICreditEnvSettings envSettings)
        {
            this.contextFactory = contextFactory;
            this.notificationProcessSettingsFactory = notificationProcessSettingsFactory;
            this.clock = clock;
            this.clientConfiguration = clientConfiguration;
            this.envSettings = envSettings;
        }

        public DocumentClientExcelRequest CreateRseReportForCredit(RseForCreditRequest request)
        {
            var rseResult = CalculateRseForCredit(request);

            var summaryItems = new List<Tuple<string, string, decimal?>>();
            void AddSummary(string header, string text = null, decimal? value = null) => summaryItems.Add(Tuple.Create(header, text, value));

            DocumentClientExcelRequest.Sheet CreateRseSummarySheet()
            {
                var sheet = new DocumentClientExcelRequest.Sheet
                {
                    AutoSizeColumns = true,
                    Title = $"RSE {request.CreditNr} {clock.Today:yyyy-MM-dd}"
                };
                sheet.SetColumnsAndData(summaryItems,
                    summaryItems.Col(x => x.Item1, ExcelType.Text, "Name"),
                    summaryItems.Col(x => x.Item2, ExcelType.Text, "Text"),
                    summaryItems.Col(x => x.Item3, ExcelType.Number, "Value"));
                return sheet;
            }

            if (!rseResult.HasRse)
            {
                AddSummary("RSE att betala", text: rseResult.NoRseReasonText, value: 0m);
                return new DocumentClientExcelRequest
                {
                    Sheets = new DocumentClientExcelRequest.Sheet[]
                    {
                            CreateRseSummarySheet()
                    }
                };
            }

            var rse = rseResult.Rse;

            rse.RseAmountParts.ForEach(x => AddSummary(x.Description, value: x.Amount));
            AddSummary("RSE", value: rse.RseAmount);

            var excelRequest = new DocumentClientExcelRequest
            {
                Sheets = new DocumentClientExcelRequest.Sheet[]
                {
                        CreateRseSummarySheet(),
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = "Detaljer"
                        }
                }
            };

            excelRequest.Sheets[1].SetColumnsAndData(rse.Payments,
                rse.Payments.Col(x => x.PaymentDate, ExcelType.Date, "Betaldatum"),
                rse.Payments.Col(x => x.CapitalAmount, ExcelType.Number, "Amortering"),
                rse.Payments.Col(x => x.InterestAmount, ExcelType.Number, "Ränta"),
                rse.Payments.Col(x => x.FutureInterestFraction, ExcelType.Number, "Omräkningsfaktor"),
                rse.Payments.Col(x => x.TotalAmount, ExcelType.Number, "Ränta + amortering"),
                rse.Payments.Col(x => x.TotalCurrentAmount, ExcelType.Number, "Nuvärde Ränta + amortering", includeSum: true));

            return excelRequest;
        }

        public (bool HasRse, RseResponse Rse, string NoRseReasonText) CalculateRseForCredit(RseForCreditRequest request)
        {
            using (var context = contextFactory.CreateContext())
            {
                var model = AmortizationPlan.GetHistoricalCreditModel(request.CreditNr, context, true);
                if (!AmortizationPlan.TryGetAmortizationPlan(
                    model, notificationProcessSettingsFactory.GetByCreditType(model.GetCreditType()),
                    out var amortizationPlan, out var failedMessage, clock, clientConfiguration,
                    CreditDomainModel.GetInterestDividerOverrideByCode(envSettings.ClientInterestModel)))
                {
                    throw new NTechCoreWebserviceException(failedMessage)
                    {
                        ErrorHttpStatusCode = 400,
                        IsUserFacing = true
                    };
                }

                var credit = CreditDomainModel.PreFetchForSingleCredit(request.CreditNr, context, envSettings);

                var rebindMonthCount = credit.GetDatedCreditValueOpt(clock.Today, DatedCreditValueCode.MortgageLoanInterestRebindMonthCount);
                if (!rebindMonthCount.HasValue || rebindMonthCount.Value <= MortgageLoansCreditTermsChangeBusinessEventManager.DefaultInterestBindMonthCount)
                {
                    return (HasRse: false, Rse: null, NoRseReasonText: "Pga rörlig ränta");
                }
                var mortgageLoanNextInterestRebindDate = credit.GetDatedCreditDate(clock.Today, DatedCreditDateCode.MortgageLoanNextInterestRebindDate, null);
                if (!mortgageLoanNextInterestRebindDate.HasValue || mortgageLoanNextInterestRebindDate.Value <= clock.Today)
                {
                    return (HasRse: false, Rse: null, NoRseReasonText: "Bindningstiden har gått ut");
                }

                var loanInterestRatePercent = credit.GetInterestRatePercent(clock.Today);
                if (loanInterestRatePercent <= request.ComparisonInterestRatePercent)
                {
                    return (HasRse: false, Rse: null, NoRseReasonText: "Lånränta <= jämförelseränta");
                }

                var futurePayments = new List<RseRequest.Payment>();

                var lastActualPayment = amortizationPlan.Items.Where(x => !x.IsFutureItem && x.EventTypeCode == "NewNotification").LastOrDefault();
                if (lastActualPayment != null)
                {
                    var lastActualDueDate = new DateTime(lastActualPayment.EventTransactionDate.Year, lastActualPayment.EventTransactionDate.Month, notificationProcessSettingsFactory.GetByCreditType(CreditType.MortgageLoan).NotificationDueDay);
                    if (IsNotificationThatContainsDate(lastActualDueDate, clock.Today))
                    {
                        futurePayments.Add(new RseRequest.Payment
                        {
                            PaymentDate = lastActualDueDate,
                            CapitalAmount = lastActualPayment.CapitalTransaction,
                            TotalPaymentAmount = lastActualPayment.CapitalTransaction + (lastActualPayment.InterestTransaction ?? 0m)
                        });
                    }
                }

                futurePayments.AddRange(amortizationPlan
                        .Items
                        .Where(x => x.IsFutureItem && x.FutureItemDueDate.HasValue && x.FutureItemDueDate.Value <= mortgageLoanNextInterestRebindDate.Value)
                        .Select(x => new RseRequest.Payment
                        {
                            PaymentDate = x.FutureItemDueDate.Value,
                            CapitalAmount = x.CapitalTransaction,
                            TotalPaymentAmount = x.CapitalTransaction + x.InterestTransaction ?? 0m
                        }).ToList());

                var rseRequest = new RseRequest
                {
                    NotNotifiedCapitalAmount = credit.GetNotNotifiedCapitalBalance(clock.Today),
                    ComparisonInterestRatePercent = request.ComparisonInterestRatePercent.Value,
                    FuturePayments = futurePayments,
                    LoanInterestRatePercent = loanInterestRatePercent,
                    NextInterestRebindDate = mortgageLoanNextInterestRebindDate.Value,
                    Today = clock.Today
                };

                var rse = CalculateRse(rseRequest);

                return (HasRse: true, Rse: rse, NoRseReasonText: null);
            }
        }

        private static bool IsNotificationThatContainsDate(DateTime dueDate, DateTime dateToCheck) =>
            IsNotificationThatContainsDateWithFutureFraction(dueDate, dateToCheck, out var _);

        private static bool IsNotificationThatContainsDateWithFutureFraction(DateTime dueDate, DateTime dateToCheck, out decimal futureInterestFraction)
        {
            var lastMonthDueDate = new DateTime(
                dueDate.AddMonths(-1).Year,
                dueDate.AddMonths(-1).Month,
                dueDate.Day);

            var isContained = dueDate > dateToCheck && lastMonthDueDate < dateToCheck;
            if (isContained)
            {
                var futureDayCount = (decimal)Dates.GetAbsoluteNrOfDaysBetweenDates(dateToCheck, dueDate);
                var pastDayCount = (decimal)Dates.GetAbsoluteNrOfDaysBetweenDates(lastMonthDueDate, dateToCheck);
                futureInterestFraction = futureDayCount / (futureDayCount + pastDayCount);
            }
            else
            {
                futureInterestFraction = 1m;
            }

            return isContained;
        }

        public static RseResponse CalculateRse(RseRequest request)
        {
            var currentValueConverter = new CurrentValueConverter(
                request.Today, request.ComparisonInterestRatePercent);

            var rsePayments = new List<RsePayment>();
            var capitalAmount = request.NotNotifiedCapitalAmount;

            var futurePayments = request.FuturePayments.Where(x => x.PaymentDate <= request.NextInterestRebindDate);

            decimal Round(decimal value) => Math.Round(value, 2);

            futurePayments.Select(x =>
            {
                decimal totalPaymentAmount = x.TotalPaymentAmount;
                decimal? futureInterestFraction = null;
                if (IsNotificationThatContainsDateWithFutureFraction(x.PaymentDate, request.Today, out var futureInterestFractionLocal))
                {
                    //Notification contains future and past interest. Remove the past interest
                    var interestAmount = x.TotalPaymentAmount - x.CapitalAmount;
                    var futureInterestAmount = interestAmount * futureInterestFractionLocal;
                    totalPaymentAmount = Math.Round(futureInterestAmount, 2) + x.CapitalAmount;
                    futureInterestFraction = futureInterestFractionLocal;
                }

                var capitalBefore = capitalAmount;
                capitalAmount -= x.CapitalAmount;
                return new RsePayment
                {
                    PaymentDate = x.PaymentDate,
                    CapitalBefore = capitalBefore,
                    CapitalAmount = x.CapitalAmount,
                    InterestAmount = totalPaymentAmount - x.CapitalAmount,
                    TotalAmount = totalPaymentAmount,
                    TotalCurrentAmount = Round(currentValueConverter.GetCurrentValue(totalPaymentAmount, x.PaymentDate)),
                    FutureInterestFraction = futureInterestFraction
                };
            }).ToList().ForEach(rsePayments.Add);

            DateTime notNotifiedInterestFromDate;
            decimal rebindDateCapitalAmount;
            if (rsePayments.Any())
            {
                var lastPayment = rsePayments.Last();
                notNotifiedInterestFromDate = lastPayment.PaymentDate;
                rebindDateCapitalAmount = lastPayment.CapitalBefore - lastPayment.CapitalAmount;
            }
            else
            {
                notNotifiedInterestFromDate = request.Today;
                rebindDateCapitalAmount = request.NotNotifiedCapitalAmount;
            }
            var notNotifiedInterestDayCount = (int)Math.Round(request.NextInterestRebindDate.Subtract(notNotifiedInterestFromDate).TotalDays);
            if (notNotifiedInterestDayCount > 0 && rebindDateCapitalAmount > 0m)
            {
                var interestAmount = Round(rebindDateCapitalAmount * request.LoanInterestRatePercent / 100m * ((decimal)notNotifiedInterestDayCount) / 365m);
                rsePayments.Add(new RsePayment
                {
                    PaymentDate = request.NextInterestRebindDate,
                    CapitalBefore = rebindDateCapitalAmount,
                    CapitalAmount = 0m,
                    InterestAmount = interestAmount,
                    TotalAmount = interestAmount,
                    TotalCurrentAmount = Round(currentValueConverter.GetCurrentValue(interestAmount, request.NextInterestRebindDate))
                });
            }

            var response = new RseResponse
            {
                Payments = rsePayments,
                CurrentCapitalAmount = request.NotNotifiedCapitalAmount,
                RebindDateCapitalAmount = rebindDateCapitalAmount,
                RebindDateCapitalCurrentValueAmount = Round(currentValueConverter.GetCurrentValue(rebindDateCapitalAmount, request.NextInterestRebindDate))
            };

            response.RseAmountParts = new List<RseAmountPart>
            {
                    new RseAmountPart { Description = "Summan av betalningar, i nuvärde", Amount = rsePayments.Sum(x => x.TotalCurrentAmount) },
                    new RseAmountPart { Description = "Skulden på ränteändringsdagen, i nuvärde", Amount = response.RebindDateCapitalCurrentValueAmount },
                    new RseAmountPart { Description = "Kvarvarande skuld vid lösentidpunkt", Amount = -response.CurrentCapitalAmount }
            };

            response.RseAmount = response.RseAmountParts.Sum(x => x.Amount);

            return response;
        }

        private class CurrentValueConverter
        {
            private readonly DateTime currentDate;
            private readonly decimal conversionInterestRatePercent;

            public CurrentValueConverter(DateTime currentDate, decimal conversionInterestRatePercent)
            {
                this.currentDate = currentDate.Date;
                this.conversionInterestRatePercent = conversionInterestRatePercent;
            }

            public decimal GetCurrentValue(decimal futureValue, DateTime futureDate)
            {
                var daysCount = GetDaysUntil(futureDate);
                if (daysCount == 0)
                {
                    return futureValue;
                }
                var currentValue =
                    futureValue / (decimal)Math.Pow(
                        (double)(1m + conversionInterestRatePercent / 100m),
                        (double)(daysCount / 365m));

                return currentValue;
            }

            private int GetDaysUntil(DateTime futureDate)
            {
                if (futureDate.Date <= currentDate)
                {
                    return 0;
                }
                return (int)Math.Round(futureDate.Date.Subtract(currentDate).TotalDays);
            }
        }
    }

    public class RseRequest
    {
        /// <summary>
        /// Today and also settlement date
        /// The interests needed to compute RSE change every day
        /// so it's not possible to compute RSE for future dates.
        /// </summary>
        public DateTime Today { get; set; }
        public decimal NotNotifiedCapitalAmount { get; set; }
        public decimal LoanInterestRatePercent { get; set; }
        public decimal ComparisonInterestRatePercent { get; set; }
        public DateTime NextInterestRebindDate { get; set; }
        public class Payment
        {
            public DateTime PaymentDate { get; set; }
            public decimal TotalPaymentAmount { get; set; }
            public decimal CapitalAmount { get; set; }
        }
        public List<Payment> FuturePayments { get; set; }
    }

    public class RseResponse
    {
        /// <summary>
        /// Betalningar i nuvärde
        /// </summary>
        public List<RsePayment> Payments { get; set; }

        /// <summary>
        /// Skuld på ränteändringsdagen
        /// </summary>
        public decimal RebindDateCapitalAmount { get; set; }

        /// <summary>
        /// Skuld på ränteändringsdagen i nuvärde
        /// </summary>
        public decimal RebindDateCapitalCurrentValueAmount { get; set; }

        /// <summary>
        /// Kvarvarande skuld vid lösentidpunkt
        /// </summary>
        public decimal CurrentCapitalAmount { get; set; }

        public decimal RseAmount { get; set; }
        public List<RseAmountPart> RseAmountParts { get; set; }
    }

    public class RseAmountPart
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }

    public class RsePayment
    {
        public DateTime PaymentDate { get; set; }
        public decimal CapitalBefore { get; set; }
        public decimal CapitalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalCurrentAmount { get; set; }
        public decimal? FutureInterestFraction { get; set; }
    }

    public class RseForCreditRequest
    {
        [Required]
        public decimal? ComparisonInterestRatePercent { get; set; }
        [Required]
        public string CreditNr { get; set; }
    }
}
