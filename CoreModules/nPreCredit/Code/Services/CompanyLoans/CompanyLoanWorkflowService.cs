namespace nPreCredit.Code.Services.CompanyLoans
{
    public class CompanyLoanWorkflowService : WorkflowServiceBase, ICompanyLoanWorkflowService
    {
        public CompanyLoanWorkflowService(CreditApplicationListService creditApplicationListService, IPreCreditContextFactoryService preCreditContextFactoryService)
            : base(creditApplicationListService, NEnv.CompanyLoanWorkflow, preCreditContextFactoryService)
        {

        }
    }

    public interface ICompanyLoanWorkflowService : ISharedWorkflowService
    {

    }
}