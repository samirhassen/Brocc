using nCredit.DbModel.BusinessEvents.NewCredit;
using NTech.Core.Module.Shared.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTech.Core.Module.Shared.Clients
{
    public class CreditClient : ICreditClient
    {
        private ServiceClient client;

        public CreditClient(INHttpServiceUser httpServiceUser, ServiceClientFactory serviceClientFactory)
        {
            client = serviceClientFactory.CreateClient(httpServiceUser, "nCredit");
        }

        public async Task<List<HistoricalCreditExtended>> GetCustomerCreditHistoryAsync(List<int> customerIds)
        {
            if (customerIds != null && customerIds.Count == 0)
                return new List<HistoricalCreditExtended>();
            return (await client.Call(x => x.PostJson("Api/CustomerCreditHistoryBatch", new { customerIds = customerIds }),
                x => x.ParseJsonAsAnonymousType(new { Credits = (List<HistoricalCreditExtended>)null })))?.Credits?.ToList();
        }

        public List<HistoricalCreditExtended> GetCustomerCreditHistory(List<int> customerIds) => client.ToSync(() =>
            GetCustomerCreditHistoryAsync(customerIds));
        
        public async Task<string> NewCreditNumberAsync() => (await client.Call(
                x => x.PostJson("Api/NewCreditNumber", new { }),
                x => x.ParseJsonAsAnonymousType(new { Nr = (string)null })
                ))?.Nr;

        public string NewCreditNumber() => client.ToSync(() => NewCreditNumberAsync());

        public async Task<List<HistoricalCreditExtended>> GetCustomerCreditHistoryByCreditNrsAsync(List<string> creditNrs)
        {
            if (creditNrs != null && creditNrs.Count == 0)
                return new List<HistoricalCreditExtended>();
            var result = await client.Call(x => x.PostJson("Api/CustomerCreditHistoryByCreditNrs", new { creditNrs = creditNrs }),
                x => x.ParseJsonAsAnonymousType(new { Credits = (List<HistoricalCreditExtended>)null }));
            return result.Credits;
        }

        public List<HistoricalCreditExtended> GetCustomerCreditHistoryByCreditNrs(List<string> creditNrs) =>
            client.ToSync(() => GetCustomerCreditHistoryByCreditNrsAsync(creditNrs));

        private class GetCurrentReferenceInterestResult
        {
            public decimal ReferenceInterestRatePercent { get; set; }
        }

        public async Task<decimal> GetCurrentReferenceInterestAsync() => (await client.Call(
            x => x.PostJson("Api/ReferenceInterest/GetCurrent", new { }),
            x => x.ParseJsonAs<GetCurrentReferenceInterestResult>())).ReferenceInterestRatePercent;

        public decimal GetCurrentReferenceInterest() =>
            client.ToSync(() => GetCurrentReferenceInterestAsync());

        public Task<CreateCreditCommentResponse> CreateCreditCommentAsync(string creditNr, string commentText, string eventType, bool? dontReturnComment, string attachedFileAsDataUrl, string attachedFileName, int? customerSecureMessageId) =>
            client.Call(x => x.PostJson("Api/CreditComment/Create", new { creditNr, commentText, eventType, dontReturnComment, attachedFileAsDataUrl, attachedFileName, customerSecureMessageId }),
                x => x.ParseJsonAs<CreateCreditCommentResponse>());

        public CreateCreditCommentResponse CreateCreditComment(string creditNr, string commentText, string eventType, bool? dontReturnComment, string attachedFileAsDataUrl, string attachedFileName, int? customerSecureMessageId) =>
            client.ToSync(() => CreateCreditCommentAsync(creditNr, commentText, eventType, dontReturnComment, attachedFileAsDataUrl, attachedFileName, customerSecureMessageId));

        public int CreateCreditCommentCore(string creditNr, string commentText, string eventType, string attachedFileAsDataUrl,
            string attachedFileName, int? customerSecureMessageId) => client.ToSync(() => CreateCreditCommentCoreAsync(creditNr, commentText,
                eventType, attachedFileAsDataUrl, attachedFileName, customerSecureMessageId));

        public async Task<int> CreateCreditCommentCoreAsync(string creditNr, string commentText, string eventType, string attachedFileAsDataUrl,
            string attachedFileName, int? customerSecureMessageId)
        {
            var result = await client.Call(
                x => x.PostJson("Api/Credit/Comment/Create", new { creditNr, commentText, eventType, attachedFileAsDataUrl, attachedFileName, customerSecureMessageId }),
                x => x.ParseJsonAsAnonymousType(new { CommentId = 0 }), isCoreHosted: true);

            return result.CommentId;
        }

        public async Task<List<CustomCostClientItem>> GetCustomCostsAsync() =>
           await client.Call(
                x => x.PostJson("Api/Credit/CustomCosts/All", new { }), 
                x => x.ParseJsonAs<List<CustomCostClientItem>>(), 
                isCoreHosted: true);

        public List<CustomCostClientItem> GetCustomCosts() => client.ToSync(GetCustomCostsAsync);

        public Task CreateCreditsAsync(NewCreditRequest[] newCreditRequests, NewAdditionalLoanRequest[] additionalLoanRequests) => client.CallVoid(
            x => x.PostJson("Api/CreateCredits", new { newCreditRequests = newCreditRequests, additionalLoanRequests = additionalLoanRequests }),
            x => x.EnsureSuccessStatusCode());

        public void CreateCredits(NewCreditRequest[] newCreditRequests, NewAdditionalLoanRequest[] additionalLoanRequests) =>
            client.ToSync(() => CreateCreditsAsync(newCreditRequests, additionalLoanRequests));
    }
}
