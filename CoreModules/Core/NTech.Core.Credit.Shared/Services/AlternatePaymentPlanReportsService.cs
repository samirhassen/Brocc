using nCredit;
using nCredit.Excel;
using Newtonsoft.Json;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NTech.Core.Credit.Shared.Services
{
    public class AlternatePaymentPlanReportsService
    {
        private readonly CreditContextFactory contextFactory;
        private readonly IDocumentClient documentClient;

        public AlternatePaymentPlanReportsService(CreditContextFactory contextFactory, IDocumentClient documentClient)
        {
            this.contextFactory = contextFactory;
            this.documentClient = documentClient;
        }

        public static bool IsReportEnabled(IClientConfigurationCore clientConfig) => clientConfig.IsFeatureEnabled("ntech.feature.paymentplan");

        public async Task<(MemoryStream ReportData, string ReportFileName)> CreateAlternatePaymentPlansExcelReportAsync(AlternatePaymentPlansReportRequest request)
        {
            var toDate = request.Date;

            using (var context = contextFactory.CreateContext())
            {
                var paymentPlans = GetActivePaymentPlans(toDate);

                var excelRequest = new DocumentClientExcelRequest
                {
                    Sheets = new DocumentClientExcelRequest.Sheet[]
                    {
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = "Payment plans"
                        }
                    }
                };

                string GetStatusText(ActivePaymentPlanModel s)
                {
                    if (!s.EndDate.HasValue)
                        return "Active";
                    if (s.IsFullyPaid)
                        return s.IsPrePaid ? "Prepaid" : "Completed";
                    if (s.IsCancelled)
                        return s.IsManuallyCancelled ? "Cancelled manually" : "Cancelled missed payment";
                    return "Unknown"; //This should not be possible but just in case.
                }

                excelRequest.Sheets[0].SetColumnsAndData(paymentPlans,
                    paymentPlans.Col(x => x.CreditNr, ExcelType.Text, "Credit nr"),
                    paymentPlans.Col(x => x.StartDate, ExcelType.Date, "Start date"),
                    paymentPlans.Col(x => x.EndDate, ExcelType.Date, "End date"),
                    paymentPlans.Col(x => GetStatusText(x), ExcelType.Text, "Status"));

                var reportData = await documentClient.CreateXlsxAsync(excelRequest);
                return (ReportData: reportData, ReportFileName: $"AlternatePaymentPlans_{toDate:yyyy-MM-dd}.xlsx");
            }
        }

        private List<ActivePaymentPlanModel> GetActivePaymentPlans(DateTime toDate)
        {
            using (var context = contextFactory.CreateContext())
            {
                var credits = context
                    .CreditHeadersQueryable
                    .Where(x => x.CreatedByEvent.TransactionDate <= toDate)
                    .Select(x => new
                    {
                        CreditNr = x.CreditNr,
                        CreditStatus = x
                            .DatedCreditStrings
                            .Where(y => y.TransactionDate <= toDate && y.Name == DatedCreditStringCode.CreditStatus.ToString())
                            .OrderByDescending(y => y.Id)
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                        LatestPaymentPlan = x.AlternatePaymentPlans.Where(y => y.CreatedByEvent.TransactionDate <= toDate).OrderByDescending(y => y.Id).Select(y => new
                        {
                            StartDate = y.CreatedByEvent.TransactionDate,
                            y.CancelledByEvent,
                            y.FullyPaidByEvent,
                            LastDueDate = y.Months.Max(z => z.DueDate)
                        }).FirstOrDefault()
                    })
                    .Where(x => x.LatestPaymentPlan != null && x.CreditStatus == CreditStatus.Normal.ToString())
                    .ToList();


                return credits.OrderBy(x => x.CreditNr).Select(x =>
                {
                    var p = x.LatestPaymentPlan;
                    bool isManuallyCancelled = false;
                    bool isCancelled = false;
                    bool isPrePaid = false;
                    bool isFullyPaid = false;
                    DateTime? endDate = null;

                    if(p.FullyPaidByEvent != null && p.FullyPaidByEvent.TransactionDate <= toDate)
                    {
                        isFullyPaid = true;
                        //We use one month here somewhat arbitrarily. It at least seems less bad than say things like "the day before the last duedate is prepaid".
                        isPrePaid = p.FullyPaidByEvent.TransactionDate <= p.LastDueDate.AddMonths(-1);
                        endDate = p.FullyPaidByEvent.TransactionDate;
                    }

                    if(p.CancelledByEvent != null && p.CancelledByEvent.TransactionDate <= toDate)
                    {
                        isCancelled = true;
                        isManuallyCancelled = p.CancelledByEvent.EventType == BusinessEventType.AlternatePaymentPlanCancelledManually.ToString();
                        endDate = p.CancelledByEvent.TransactionDate;
                    }

                    return new ActivePaymentPlanModel
                    {
                        CreditNr = x.CreditNr,
                        StartDate = p.StartDate,
                        EndDate = endDate,
                        IsCancelled = isCancelled,
                        IsManuallyCancelled = isManuallyCancelled,
                        IsFullyPaid = isFullyPaid,
                        IsPrePaid = isPrePaid
                    };
                }).ToList();
            }
        }

        private class ActivePaymentPlanModel
        {
            public string CreditNr { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public bool IsManuallyCancelled { get; set; }
            public bool IsCancelled { get; set; }
            public bool IsPrePaid { get; set; }
            public bool IsFullyPaid { get; set; }
        }
    }

    public class AlternatePaymentPlansReportRequest
    {
        [Required]
        public DateTime Date { get; set; }
    }
}
