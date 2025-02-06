using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class UnsecuredLoanStandardWorkflowService : WorkflowServiceBase, ISharedWorkflowService
    {
        public UnsecuredLoanStandardWorkflowService(IPreCreditContextFactoryService preCreditContextFactoryService, CreditApplicationListService creditApplicationListService) : base(creditApplicationListService, StandardWorkflow, preCreditContextFactoryService)
        {

        }

        public static WorkflowServiceReadBase GetReadBase() => new WorkflowServiceReadBase(StandardWorkflow);

        public static readonly WorkflowModel StandardWorkflow = new WorkflowModel
        {
            WorkflowVersion = 202106211,
            SeparatedWorkLists = new List<WorkflowModel.SeparatedWorkListModel>(),
            Steps = new List<WorkflowModel.StepModel>
                {
                    new WorkflowModel.StepModel
                    {
                        Name = "CreditCheck",
                        ComponentName = "CreditCheckStandardComponent",
                        DisplayName = "Credit Check"
                    },
                    new WorkflowModel.StepModel
                    {
                        Name = "CustomerOfferDecision",
                        ComponentName = "CustomerOfferDecisionStandardComponent",
                        DisplayName = "Waiting for customer to accept offer"
                    },
                    new WorkflowModel.StepModel
                    {
                        Name = "Kyc",
                        ComponentName = "KycStandardComponent",
                        DisplayName = "Kyc"
                    },
                    new WorkflowModel.StepModel
                    {
                        Name = "Fraud",
                        ComponentName = "FraudStandardComponent",
                        DisplayName = "Fraud"
                    },
                    new WorkflowModel.StepModel
                    {
                        Name = "Agreement",
                        ComponentName = "AgreementStandardComponent",
                        DisplayName = "Agreement"
                    },
                    new WorkflowModel.StepModel
                    {
                        Name = "Payment",
                        ComponentName = "PaymentStandardComponent",
                        DisplayName = "Payment"
                    }
                }
        };

        public static WorkflowModel.StepModel CreditCheckStep => StandardWorkflow.Steps.Single(x => x.Name == "CreditCheck");
        public static WorkflowModel.StepModel CustomerOfferDecisionStep => StandardWorkflow.Steps.Single(x => x.Name == "CustomerOfferDecision");
        public static WorkflowModel.StepModel KycStep => StandardWorkflow.Steps.Single(x => x.Name == "Kyc");
        public static WorkflowModel.StepModel FraudStep => StandardWorkflow.Steps.Single(x => x.Name == "Fraud");
        public static WorkflowModel.StepModel AgreementStep => StandardWorkflow.Steps.Single(x => x.Name == "Agreement");
        public static WorkflowModel.StepModel PaymentStep => StandardWorkflow.Steps.Single(x => x.Name == "Payment");
    }
}