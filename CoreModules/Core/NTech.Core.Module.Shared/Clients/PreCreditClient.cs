using NTech.Core.Module.Shared.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTech.Core.Module.Shared.Clients
{
    public class PreCreditClient : IPreCreditClient
    {
        private ServiceClient client;

        public PreCreditClient(INHttpServiceUser httpServiceUser, ServiceClientFactory serviceClientFactory)
        {
            client = serviceClientFactory.CreateClient(httpServiceUser, "nPreCredit");
        }

        public Task AddCommentToApplicationAsync(string applicationNr, string commentText, int? customerSecureMessageId) =>
            client.CallVoid(
                x => x.PostJson("api/ApplicationComments/Add", new
                {
                    applicationNr,
                    commentText,
                    customerSecureMessageId
                }),
                x => x.EnsureSuccessStatusCode());

        public void AddCommentToApplication(string applicationNr, string commentText, int? customerSecureMessageId) =>
            client.ToSync(() => AddCommentToApplicationAsync(applicationNr, commentText, customerSecureMessageId));

        public Task ReportKycQuestionSessionCompletedAsync(string sessionId) => client.CallVoid(
            x => x.PostJson("Api/PreCredit/UnsecuredLoanStandard/OnKycQuestionSessionCompleted", new { sessionId }),
            x => x.EnsureSuccessStatusCode(), isCoreHosted: true);

        public void ReportKycQuestionSessionCompleted(string sessionId) => client.ToSync(() => ReportKycQuestionSessionCompletedAsync(sessionId));

        public Task LoanStandardApproveKycStepAsync(string applicationNr, bool isApproved, bool isAutomatic) => client.CallVoid(
            x => x.PostJson("api/LoanStandard/Kyc/Set-Approved-Step", new { applicationNr, isApproved, isAutomatic }),
            x => x.EnsureSuccessStatusCode());

        public void LoanStandardApproveKycStep(string applicationNr, bool isApproved, bool isAutomatic) => client.ToSync(() => 
            LoanStandardApproveKycStepAsync(applicationNr, isApproved, isAutomatic));

        public async Task<IDictionary<string, string>> GetApplicationNrsByCreditNrsAsync(ISet<string> creditNrs)
        {
            var rr = await client.Call(
                x => x.PostJson("api/GetApplicationNrByCreditNr", new
                {
                    creditNrs = creditNrs.ToList()
                }),
                x => x.ParseJsonAs<GetApplicationNrByCreditNrResult>());

            return (rr.Hits ?? new List<GetApplicationNrByCreditNrResult.Item>()).ToDictionary(x => x.CreditNr, x => x.ApplicationNr);
        }

        public IDictionary<string, string> GetApplicationNrsByCreditNrs(ISet<string> creditNrs) => client.ToSync(() => GetApplicationNrsByCreditNrsAsync(creditNrs));

        public Task<Dictionary<string, Dictionary<string, Dictionary<string, string>>>> BulkFetchCreditApplicationItemsAsync(BulkFetchCreditApplicationItemsRequest request) =>
            client.Call(
                x => x.PostJson("Api/PreCredit/CreditApplicationItems/Bulk-Fetch", request),
                x => x.ParseJsonAs<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(),
                isCoreHosted: true);

        public Dictionary<string, Dictionary<string, Dictionary<string, string>>> BulkFetchCreditApplicationItems(BulkFetchCreditApplicationItemsRequest request) =>
            client.ToSync(() => BulkFetchCreditApplicationItemsAsync(request));

        private class GetApplicationNrByCreditNrResult
        {
            public int CustomerId { get; set; }

            public class Item
            {
                public string CreditNr { get; set; }
                public string ApplicationNr { get; set; }
            }

            public List<Item> Hits { get; set; }
        }
    }
}
