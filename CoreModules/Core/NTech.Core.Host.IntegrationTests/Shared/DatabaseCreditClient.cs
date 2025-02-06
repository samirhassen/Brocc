using nCredit.DbModel.BusinessEvents.NewCredit;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Clients;

namespace NTech.Core.Host.IntegrationTests.Shared
{
    internal class DatabaseCreditClient : ICreditClient
    {
        private readonly CreditContextFactory creditContextFactory;

        public DatabaseCreditClient(CreditContextFactory creditContextFactory)
        {
            this.creditContextFactory = creditContextFactory;
        }
        public CreateCreditCommentResponse CreateCreditComment(string creditNr, string commentText, string eventType, bool? dontReturnComment, string attachedFileAsDataUrl, string attachedFileName, int? customerSecureMessageId)
        {
            throw new NotImplementedException();
        }

        public int CreateCreditCommentCore(string creditNr, string commentText, string eventType, string attachedFileAsDataUrl, string attachedFileName, int? customerSecureMessageId)
        {
            throw new NotImplementedException();
        }

        public decimal GetCurrentReferenceInterest()
        {
            using (var context = creditContextFactory.CreateContext())
            {
                var model = new nCredit.DomainModel.SharedDatedValueDomainModel(context);
                return model.GetReferenceInterestRatePercent(context.CoreClock.Today);
            }
        }

        public Task<decimal> GetCurrentReferenceInterestAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<CustomCostClientItem>> GetCustomCostsAsync() => Task.FromResult(GetCustomCosts());

        public List<CustomCostClientItem> GetCustomCosts() =>
            new CustomCostTypeService(creditContextFactory, new PaymentOrderAndCostTypeCache()).GetCustomCosts().Select(x => new CustomCostClientItem
            {
                Code = x.Code,
                Text = x.Text
            }).ToList();

        public List<HistoricalCreditExtended> GetCustomerCreditHistory(List<int> customerIds)
        {
            throw new NotImplementedException();
        }

        public List<HistoricalCreditExtended> GetCustomerCreditHistoryByCreditNrs(List<string> creditNrs)
        {
            throw new NotImplementedException();
        }

        public Task<List<HistoricalCreditExtended>> GetCustomerCreditHistoryByCreditNrsAsync(List<string> creditNrs)
        {
            throw new NotImplementedException();
        }

        public string NewCreditNumber()
        {
            throw new NotImplementedException();
        }

        public Task CreateCreditsAsync(NewCreditRequest[] newCreditRequests, NewAdditionalLoanRequest[] additionalLoanRequests)
        {
            throw new NotImplementedException();
        }

        public void CreateCredits(NewCreditRequest[] newCreditRequests, NewAdditionalLoanRequest[] additionalLoanRequests)
        {
            throw new NotImplementedException();
        }
    }
}
