using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services.NewUnsecuredLoans.Waterfall
{
    public class WaterfallApplicationModel
    {
        public string ApplicationNr { get; set; }
        public DateTimeOffset ApplicationDate { get; set; }
        public string ProviderName { get; set; }
        public bool IsActive { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsRejected { get; set; }
        public bool IsFinalDecisionMade { get; set; }
        public IEnumerable<string> MemberOfListNames { get; set; }
        public IEnumerable<ComplexApplicationListItemBase> FinalPaymentItems { get; set; }
        public decimal? GetPaidOutAmount()
        {
            if (!IsFinalDecisionMade)
                return null;

            try
            {
                var result = 0m;

                var items = FinalPaymentItems.ToList();

                var finalLoanTerms = ComplexApplicationList.CreateListFromFlattenedItems("FinalLoanTerms", items);
                result += finalLoanTerms.GetRow(1, true).GetUniqueItemDecimal("paidToCustomerAmount") ?? 0m;

                var finalLoanPayments = ComplexApplicationList.CreateListFromFlattenedItems("FinalLoanPayments", items);
                result += finalLoanPayments.GetRows().Sum(x => x.GetUniqueItemDecimal("paymentAmount", require: true).Value);

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"GetPaidOutAmount failed for {ApplicationNr}", ex);
            }
        }

        public bool HasAcceptedOffer(WorkflowServiceReadBase workflowService) =>
            workflowService.IsStepStatusAccepted(UnsecuredLoanStandardWorkflowService.CustomerOfferDecisionStep.Name, MemberOfListNames);

        public bool HasSignedAgreement(WorkflowServiceReadBase workflowService) =>
            workflowService.IsStepStatusAccepted(UnsecuredLoanStandardWorkflowService.AgreementStep.Name, MemberOfListNames);


        public DateTime GetPeriodStartDate(WaterfallPeriodTypeCode code) => GetPeriodDateComposable(code, ApplicationDate.DateTime, false);

        public static DateTime GetPeriodDateComposable(WaterfallPeriodTypeCode code, DateTime date, bool returnEndDate)
        {
            switch (code)
            {
                case WaterfallPeriodTypeCode.Yearly: return returnEndDate ? new DateTime(date.Year, 12, 31) : new DateTime(date.Year, 1, 1);
                case WaterfallPeriodTypeCode.Quarterly: return returnEndDate ? Quarter.ContainingDate(date.Date).ToDate : Quarter.ContainingDate(date.Date).FromDate;
                case WaterfallPeriodTypeCode.Monthly: return returnEndDate ? new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1) : new DateTime(date.Year, date.Month, 1);
                default: throw new NotImplementedException();
            }
        }

        public StepWaterfallStateCode? GetWaterfallState(WorkflowServiceReadBase workflowService, string stepName)
        {
            var stepStatusName = workflowService.GetStepStatus(stepName, MemberOfListNames);
            if (stepStatusName == workflowService.AcceptedStatusName)
                return StepWaterfallStateCode.Accepted;
            else if (stepStatusName == workflowService.RejectedStatusName)
                return StepWaterfallStateCode.Rejected;
            else if (stepStatusName == workflowService.InitialStatusName)
            {
                if (workflowService.GetListName(stepName, workflowService.InitialStatusName) == workflowService.GetEarliestInitialListName(MemberOfListNames))
                {
                    if (IsActive)
                        return StepWaterfallStateCode.Current;
                    else if (IsFinalDecisionMade)
                    {
                        /* Will only happen when steps are added and historical applications exist without it.
                         * Seems most reasonable to view paid out applications as always having accepted all steps
                         */
                        return StepWaterfallStateCode.Accepted;
                    }
                    else if (IsCancelled)
                        return StepWaterfallStateCode.Cancelled;
                    else if (IsRejected)
                    {
                        /*
                         * This means that we have rejections in the system that dont cause any step to be rejected
                         * which is really sketchy.
                         */
                        return StepWaterfallStateCode.Rejected;
                    }
                    else
                        throw new NotImplementedException();
                }
                else
                    return null; //These are steps that have not been reached
            }
            else
                throw new NotImplementedException();
        }

    }
}