using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class MortgageLoanStandardWorkflowService : WorkflowServiceBase, IMortgageLoanStandardWorkflowService
    {
        public MortgageLoanStandardWorkflowService(CreditApplicationListService creditApplicationListService, IPreCreditContextFactoryService preCreditContextFactoryService) : base(creditApplicationListService, NEnv.MortgageLoanStandardWorkflow, preCreditContextFactoryService)
        {

        }

        public static WorkflowServiceReadBase GetReadBase() => new WorkflowServiceReadBase(StandardWorkflow);

        public static readonly WorkflowModel StandardWorkflow = new WorkflowModel
        {
            WorkflowVersion = 202206071,
            SeparatedWorkLists = new List<WorkflowModel.SeparatedWorkListModel>(),
            Steps = new List<WorkflowModel.StepModel>
                {
                    new WorkflowModel.StepModel
                    {
                        Name = "InitialCreditCheck",
                        ComponentName = "InitialCreditCheckStandardComponent",
                        DisplayName = "Initial credit check"
                    },
                    new WorkflowModel.StepModel
                    {
                        Name = "WaitingForAdditionalInfo",
                        ComponentName = "WaitingForAdditionalInfoStandardComponent",
                        DisplayName = "Waiting for additional information"
                    },
                    new WorkflowModel.StepModel
                    {
                        Name = "Collateral",
                        ComponentName = "CollateralStandardComponent",
                        DisplayName = "Collateral"
                    },
                    new WorkflowModel.StepModel
                    {
                        Name = "Kyc",
                        ComponentName = "KycStandardComponent",
                        DisplayName = "Kyc"
                    },
                    new WorkflowModel.StepModel
                    {
                        Name = "FinalCreditCheck",
                        ComponentName = "FinalCreditCheckStandardComponent",
                        DisplayName = "Final credit check"
                    },
                    new WorkflowModel.StepModel
                    {
                        Name = "AuditAgreement",
                        ComponentName = "AuditAgreementStandardComponent",
                        DisplayName = "Audit agreement"
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

        public static WorkflowModel.StepModel InitialCreditCheckStep => StandardWorkflow.Steps.Single(x => x.Name == "InitialCreditCheck");
        public static WorkflowModel.StepModel WaitingForAdditionalInfoStep => StandardWorkflow.Steps.Single(x => x.Name == "WaitingForAdditionalInfo");
        public static WorkflowModel.StepModel CollateralStep => StandardWorkflow.Steps.Single(x => x.Name == "Collateral");
        public static WorkflowModel.StepModel KycStep => StandardWorkflow.Steps.Single(x => x.Name == "Kyc");
        public static WorkflowModel.StepModel FinalCreditCheckStep => StandardWorkflow.Steps.Single(x => x.Name == "FinalCreditCheck");
        public static WorkflowModel.StepModel AuditAgreementStep => StandardWorkflow.Steps.Single(x => x.Name == "AuditAgreement");
        public static WorkflowModel.StepModel AgreementStep => StandardWorkflow.Steps.Single(x => x.Name == "Agreement");
        public static WorkflowModel.StepModel PaymentStep => StandardWorkflow.Steps.Single(x => x.Name == "Payment");

        public static bool IsRevertAllowed(ApplicationInfoModel ai, IMortgageLoanStandardWorkflowService wf, string currentStepName, out string revertNotAllowedMessage)
        {
            var isDecisionStepAccepted = wf.IsStepStatusAccepted(currentStepName, ai.ListNames);
            if (!isDecisionStepAccepted)
            {
                revertNotAllowedMessage = "Step must be approved to be reverted";
                return false;
            }

            var nextStepName = wf.GetNextStepNameAfter(currentStepName);

            if (!wf.IsStepStatusInitial(nextStepName, ai.ListNames))
            {
                revertNotAllowedMessage = "Only the last accepted step can be reverted";
                return false;
            }

            revertNotAllowedMessage = null;
            return true;
        }
    }

    public interface IMortgageLoanStandardWorkflowService : ISharedWorkflowService
    {

    }
}