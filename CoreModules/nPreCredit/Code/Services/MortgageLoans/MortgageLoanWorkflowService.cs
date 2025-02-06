namespace nPreCredit.Code.Services
{
    public class MortgageLoanWorkflowService : WorkflowServiceBase, IMortgageLoanWorkflowService
    {
        public MortgageLoanWorkflowService(IPreCreditContextFactoryService preCreditContextFactoryService, CreditApplicationListService creditApplicationListService) : base(creditApplicationListService, NEnv.MortgageLoanWorkflow, preCreditContextFactoryService)
        {

        }
    }

    public interface IMortgageLoanWorkflowService : ISharedWorkflowService
    {

    }
}