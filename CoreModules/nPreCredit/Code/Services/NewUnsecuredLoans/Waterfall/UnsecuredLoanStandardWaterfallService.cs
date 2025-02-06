using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services.NewUnsecuredLoans.Waterfall
{
    public class UnsecuredLoanStandardWaterfallService
    {
        private readonly WorkflowServiceReadBase workflowService;

        public UnsecuredLoanStandardWaterfallService()
        {
            workflowService = UnsecuredLoanStandardWorkflowService.GetReadBase();
        }

        public (List<WaterfallApplicationModel> Applications, DateTime FromDate, DateTime ToDate) GetApplications(DateTime fromInPeriodDate, DateTime toInPeriodDate, WaterfallPeriodTypeCode periodTypeCode, string providerName = null)
        {
            var fromDate = WaterfallApplicationModel.GetPeriodDateComposable(periodTypeCode, fromInPeriodDate, false);
            var dayAfterToDate = WaterfallApplicationModel.GetPeriodDateComposable(periodTypeCode, toInPeriodDate, true).AddDays(1);
            using (var context = new PreCreditContext())
            {
                var applicationsPre = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationDate >= fromDate && x.ApplicationDate < dayAfterToDate);
                if (!string.IsNullOrWhiteSpace(providerName))
                    applicationsPre = applicationsPre.Where(x => x.ProviderName == providerName);

                var applications = applicationsPre
                    .Select(x => new
                    {
                        x.ApplicationNr,
                        x.ApplicationDate,
                        x.ProviderName,
                        x.IsActive,
                        x.IsCancelled,
                        x.IsRejected,
                        x.IsFinalDecisionMade,
                        MemberOfListNames = x.ListMemberships.Select(y => y.ListName),
                        FinalPaymentItems = x.ComplexApplicationListItems.Where(y =>
                            (y.ListName == "FinalLoanPayments" && y.ItemName == "paymentAmount")
                            || (y.ListName == "FinalLoanTerms" && y.ItemName == "paidToCustomerAmount"))
                    })
                    .ToList()
                    .Select(x => new WaterfallApplicationModel
                    {
                        ApplicationNr = x.ApplicationNr,
                        ApplicationDate = x.ApplicationDate,
                        ProviderName = x.ProviderName,
                        IsActive = x.IsActive,
                        IsCancelled = x.IsCancelled,
                        IsRejected = x.IsRejected,
                        IsFinalDecisionMade = x.IsFinalDecisionMade,
                        MemberOfListNames = x.MemberOfListNames,
                        FinalPaymentItems = x.FinalPaymentItems
                    })
                    .ToList();

                return (Applications: applications, FromDate: fromDate, ToDate: dayAfterToDate.AddDays(-1));
            }
        }

        public (List<DateTime> PeriodDates, List<AggregateRow> Rows) ComputeAggregates(List<WaterfallApplicationModel> models, WaterfallPeriodTypeCode periodCode)
        {
            var aggregatesByDate = models
                .GroupBy(x => x.GetPeriodStartDate(periodCode))
                .ToDictionary(x => x.Key, x => x.ToList());

            var rows = new List<AggregateRow>();

            void AddRow(string desc, Func<List<WaterfallApplicationModel>, decimal> getValue, DocumentClientExcelRequest.StyleData style = null)
            {
                rows.Add(new AggregateRow { Description = desc, GetValue = x => getValue(aggregatesByDate[x]), StyleOverride = style });
            }

            decimal Fraction(int x, int y) => (y == 0 ? 0m : Math.Round((decimal)x / (decimal)y, 4));

            AddRow("Total nr of applications", x => x.Count());
            var hasPassedCreditCheck = false;
            foreach (var stepName in workflowService.GetStepOrder())
            {
                AddRow($"Current {stepName}", x => x.Count(y => y.GetWaterfallState(workflowService, stepName) == StepWaterfallStateCode.Current));
                if (!hasPassedCreditCheck)
                {
                    AddRow($"Rejected {stepName}", x => x.Count(y => y.GetWaterfallState(workflowService, stepName) == StepWaterfallStateCode.Rejected));
                }
                AddRow($"Cancelled {stepName}", x => x.Count(y => y.GetWaterfallState(workflowService, stepName) == StepWaterfallStateCode.Cancelled));
                AddRow($"Accepted {stepName}", x => x.Count(y => y.GetWaterfallState(workflowService, stepName) == StepWaterfallStateCode.Accepted));
                if (stepName == UnsecuredLoanStandardWorkflowService.CreditCheckStep.Name)
                    hasPassedCreditCheck = true;
            }

            AddRow("Paid out loans amount", x => x.Sum(y => y.GetPaidOutAmount() ?? 0m));
            AddRow("Total cancelled %", x => Fraction(x.Count(y => y.IsCancelled), x.Count()), style: new DocumentClientExcelRequest.Column
            {
                IsPercent = true,
                NrOfDecimals = 2
            });
            AddRow("Total accepted offer %", x => Fraction(x.Count(y => y.HasAcceptedOffer(workflowService)), x.Count()), style: new DocumentClientExcelRequest.Column
            {
                IsPercent = true,
                NrOfDecimals = 2
            });
            AddRow("Total signed agreement %", x => Fraction(x.Count(y => y.HasSignedAgreement(workflowService)), x.Count()), style: new DocumentClientExcelRequest.Column
            {
                IsPercent = true,
                NrOfDecimals = 2
            });
            AddRow("Take up rate applications %", x => Fraction(x.Count(y => y.IsFinalDecisionMade), x.Count()), style: new DocumentClientExcelRequest.Column
            {
                IsPercent = true,
                NrOfDecimals = 2
            });
            AddRow("Take up rate accepted offer %", x => Fraction(x.Count(y => y.IsFinalDecisionMade), x.Count(y => y.HasAcceptedOffer(workflowService))), style: new DocumentClientExcelRequest.Column
            {
                IsPercent = true,
                NrOfDecimals = 2
            });
            AddRow("Take up rate signed agreement %", x => Fraction(x.Count(y => y.IsFinalDecisionMade), x.Count(y => y.HasSignedAgreement(workflowService))), style: new DocumentClientExcelRequest.Column
            {
                IsPercent = true,
                NrOfDecimals = 2
            });

            return (PeriodDates: aggregatesByDate.Keys.ToList(), rows);
        }

        public class AggregateRow
        {
            public string Description { get; set; }
            public Func<DateTime, decimal> GetValue { get; set; }
            public DocumentClientExcelRequest.StyleData StyleOverride { get; set; }
        }
    }
}