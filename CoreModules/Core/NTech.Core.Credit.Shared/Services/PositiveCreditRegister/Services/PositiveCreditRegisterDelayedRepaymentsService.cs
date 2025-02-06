using nCredit;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Linq;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.BaseLoanExportRequestModel;
using NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models;
using System.Collections.Generic;
using nCredit.DomainModel;

namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Services
{
    internal class PositiveCreditRegisterDelayedRepaymentsService
    {
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly IClientConfigurationCore clientConfig;
        private readonly PaymentOrderService paymentOrderService;
        private readonly PcrTransformService transformService;

        private PositiveCreditRegisterSettingsModel Settings => envSettings.PositiveCreditRegisterSettings;

        public PositiveCreditRegisterDelayedRepaymentsService(CreditContextFactory creditContextFactory, ICreditEnvSettings envSettings, IClientConfigurationCore clientConfig, PaymentOrderService paymentOrderService,
            PcrTransformService transformService)
        {
            this.creditContextFactory = creditContextFactory;
            this.envSettings = envSettings;
            this.clientConfig = clientConfig;
            this.paymentOrderService = paymentOrderService;
            this.transformService = transformService;
        }

        public DelayedRepaymentsRequestModel GetBatchDelayedRepayments(DateTime daySnapshot)
        {
            using (var context = this.creditContextFactory.CreateContext())
            {
                var fields = new DelayedRepaymentsRequestModel();
                fields.TargetEnvironment = Settings.IsTargetProduction ? TargetEnvironment.Production : TargetEnvironment.Test;
                fields.Owner = new Owner
                {
                    IdCodeType = IdCodeType.BusinessId,
                    IdCode = Settings.OwnerIdCode
                };

                var currency = Enum.TryParse(clientConfig.Country.BaseCurrency, out CurrencyCode parsedCurrencyCode);
                var lenderMarketingName = Settings.LenderMarketingName;

                var fromDate = daySnapshot.Date;  // Start of the day (00:00)
                var toDate = fromDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);  // End of the day (23:59:59)

                var creditNrs = context.CreditHeadersQueryable.Where(x => x.Status == CreditStatus.Normal.ToString() && !x.AlternatePaymentPlans.Any()).Select(x => x.CreditNr).ToHashSetShared();
                var paymentOrder = paymentOrderService.GetPaymentOrderItems();
                var modelsbyNotificationId = CreditNotificationDomainModel.CreateForSeveralCredits(creditNrs, context, paymentOrder, onlyFetchOpen: false);

                IEnumerable<CreditNotificationDomainModel> allNotifications = modelsbyNotificationId.Values.SelectMany(x => x.Values);

                var sixtyDaysAgo = context.CoreClock.Now.AddDays(-60);

                var creditsData = allNotifications
                    .Where(x => x.DueDate <= sixtyDaysAgo) 
                    .Select(x => new
                    {
                        x.CreditNr,
                        x.DueDate,
                        IsPaid = x.GetRemainingBalance(toDate) <= 0m,
                        LastPaidDate = x.GetLastPaymentTransactionDate(toDate),
                        CurrentNrOfPassedDaysWithoutFullPayment = x.GetNrOfPassedDaysWithoutFullPaymentSinceNotification(toDate),
                        CurrentNrOfPassedDaysWithoutFullPaymentYesterday = x.GetNrOfPassedDaysWithoutFullPaymentSinceNotification(toDate.AddDays(-1)),
                        InitialAmount = x.GetInitialAmount(toDate),
                        WrittenOffAmount = x.GetWrittenOffAmount(toDate),
                        PaidAmount = x.GetPaidAmount(toDate)
                    })
                    .GroupBy(x => x.CreditNr)
                    .Select(n => new
                    {
                        CreditNr = n.Key,
                        CurrentNrOfPassedDaysWithoutFullPayment = n.Max(x => x.CurrentNrOfPassedDaysWithoutFullPayment),
                        CurrentNrOfPassedDaysWithoutFullPaymentYesterday = n.Max(x => x.CurrentNrOfPassedDaysWithoutFullPaymentYesterday),
                        Notifications = n.ToList()
                    })
                    .ToList();

                var delayedRepayments = creditsData
                    .Where(x => (x.CurrentNrOfPassedDaysWithoutFullPayment >= 60))
                    .Select(x => new DelayedRepayment
                    {
                        ReportReference = $"dr_{x.CreditNr}",
                        ReportType = ReportType.NewReport,
                        LoanNumber = new LoanNumber
                        {
                            Type = LoanNumberType.Other,
                            Number = x.CreditNr.ToString()
                        },
                        DelayedAmounts = x
                            .Notifications
                            .Where(n => n.CurrentNrOfPassedDaysWithoutFullPayment >= 60)
                            .Select(n =>
                                new DelayedAmountDto
                                {
                                    DelayedInstalment = Math.Round((decimal)(n.InitialAmount - n.PaidAmount - n.WrittenOffAmount), 2),
                                    OriginalDueDate = n.DueDate.ToString("yyyy-MM-dd"),
                                })
                            .ToList(),
                        IsDelay = true,
                        IsForeclosed = false
                    }).ToList();

                //If a previously delayed repayment was paid
                //Report with IsDelay=False
                var wasDelayedButNowPaid = creditsData
                    .Where(x => x.CurrentNrOfPassedDaysWithoutFullPayment < 60 && x.CurrentNrOfPassedDaysWithoutFullPaymentYesterday >= 60)
                      .Select(x => new DelayedRepayment
                      {
                          ReportReference = $"drf_{x.CreditNr}",
                          ReportType = ReportType.NewReport,
                          LoanNumber = new LoanNumber
                          {
                              Type = LoanNumberType.Other,
                              Number = x.CreditNr.ToString()
                          },
                          IsDelay = false,
                          IsForeclosed = false
                      })
                    .ToList();

                fields.DelayedRepayments = delayedRepayments.Concat(wasDelayedButNowPaid).ToList();

                foreach(var payment in fields.DelayedRepayments)
                {
                    payment.LoanNumber.Number = transformService.TransformLoanNr(payment.LoanNumber.Number);
                }

                return fields;
            }
        }
    }
}
