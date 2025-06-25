using Microsoft.VisualStudio.TestTools.UnitTesting;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.NewUnsecuredLoans.Waterfall;
using System;
using System.Linq;

namespace TestsnPreCredit
{
    [TestClass]
    public class WaterfallReportUnsecuredLoanStandard
    {
        [TestMethod]
        public void CreditCheckCurrent()
        {
            var workflowService = new WorkflowServiceReadBase(UnsecuredLoanStandardWorkflowService.StandardWorkflow);
            var model = CreateApplication();

            var state = model.GetWaterfallState(workflowService,
                UnsecuredLoanStandardWorkflowService.CreditCheckStep.Name);

            Assert.AreEqual(StepWaterfallStateCode.Current, state);
        }

        [TestMethod]
        public void RejectedOnCreditCheck()
        {
            var workflowService = new WorkflowServiceReadBase(UnsecuredLoanStandardWorkflowService.StandardWorkflow);
            var model = CreateApplication(x =>
            {
                x.IsActive = false;
                x.IsRejected = true;
                x.MemberOfListNames =
                    Enumerables.Singleton(
                        $"{UnsecuredLoanStandardWorkflowService.CreditCheckStep.Name}_{workflowService.RejectedStatusName}");
            });

            var state = model.GetWaterfallState(workflowService,
                UnsecuredLoanStandardWorkflowService.CreditCheckStep.Name);

            Assert.AreEqual(StepWaterfallStateCode.Rejected, state);
        }

        [TestMethod]
        public void CancelledOnCreditCheck()
        {
            var workflowService = new WorkflowServiceReadBase(UnsecuredLoanStandardWorkflowService.StandardWorkflow);
            var model = CreateApplication(x =>
            {
                x.IsActive = false;
                x.IsCancelled = true;
            });

            var state = model.GetWaterfallState(workflowService,
                UnsecuredLoanStandardWorkflowService.CreditCheckStep.Name);

            Assert.AreEqual(StepWaterfallStateCode.Cancelled, state);
        }


        [TestMethod]
        public void CustomerOfferDecisionCurrent()
        {
            var workflowService = new WorkflowServiceReadBase(UnsecuredLoanStandardWorkflowService.StandardWorkflow);
            var model = CreateApplication(x =>
            {
                x.MemberOfListNames = new[]
                {
                    $"{UnsecuredLoanStandardWorkflowService.CreditCheckStep.Name}_{workflowService.AcceptedStatusName}",
                    $"{UnsecuredLoanStandardWorkflowService.CustomerOfferDecisionStep.Name}_{workflowService.InitialStatusName}",
                };
            });

            var creditCheckState = model.GetWaterfallState(workflowService,
                UnsecuredLoanStandardWorkflowService.CreditCheckStep.Name);
            var customerOfferDecisionState = model.GetWaterfallState(workflowService,
                UnsecuredLoanStandardWorkflowService.CustomerOfferDecisionStep.Name);

            Assert.AreEqual(StepWaterfallStateCode.Accepted, creditCheckState);
            Assert.AreEqual(StepWaterfallStateCode.Current, customerOfferDecisionState);
        }

        [TestMethod]
        public void CustomerOfferDecisionCancelled()
        {
            var workflowService = new WorkflowServiceReadBase(UnsecuredLoanStandardWorkflowService.StandardWorkflow);
            var model = CreateApplication(x =>
            {
                x.MemberOfListNames = new[]
                {
                    $"{UnsecuredLoanStandardWorkflowService.CreditCheckStep.Name}_{workflowService.AcceptedStatusName}",
                    $"{UnsecuredLoanStandardWorkflowService.CustomerOfferDecisionStep.Name}_{workflowService.InitialStatusName}",
                };
                x.IsActive = false;
                x.IsCancelled = true;
            });

            var creditCheckState = model.GetWaterfallState(workflowService,
                UnsecuredLoanStandardWorkflowService.CreditCheckStep.Name);
            var customerOfferDecisionState = model.GetWaterfallState(workflowService,
                UnsecuredLoanStandardWorkflowService.CustomerOfferDecisionStep.Name);

            Assert.AreEqual(StepWaterfallStateCode.Accepted, creditCheckState);
            Assert.AreEqual(StepWaterfallStateCode.Cancelled, customerOfferDecisionState);
        }

        [TestMethod]
        public void PaidOutLoan()
        {
            var workflowService = new WorkflowServiceReadBase(UnsecuredLoanStandardWorkflowService.StandardWorkflow);
            var model = CreateApplication(x =>
            {
                x.MemberOfListNames = workflowService.GetStepOrder()
                    .Select(y => $"{y}_{workflowService.AcceptedStatusName}");
                x.IsActive = false;
                x.IsFinalDecisionMade = true;
            });

            Assert.AreEqual(
                true,
                workflowService.GetStepOrder().All(x =>
                    model.GetWaterfallState(workflowService, x) == StepWaterfallStateCode.Accepted));
        }

        private WaterfallApplicationModel CreateApplication(Action<WaterfallApplicationModel> modify = null)
        {
            var model = new WaterfallApplicationModel()
            {
                ApplicationDate = DateTimeOffset.Now,
                ApplicationNr = "A1000",
                IsActive = true,
                IsCancelled = false,
                IsRejected = false,
                IsFinalDecisionMade = false,
                ProviderName = "self",
                MemberOfListNames = new string[] { "CreditCheck_Initial" }
            };
            modify?.Invoke(model);
            return model;
        }
    }
}