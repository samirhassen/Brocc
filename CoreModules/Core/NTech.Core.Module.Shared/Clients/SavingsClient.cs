using NTech.Core.Module.Shared.Infrastructure;
using System.Threading.Tasks;

namespace NTech.Core.Module.Shared.Clients
{
    public class SavingsClient : ISavingsClient
    {
        private ServiceClient client;

        public SavingsClient(INHttpServiceUser httpServiceUser, ServiceClientFactory serviceClientFactory)
        {
            client = serviceClientFactory.CreateClient(httpServiceUser, "nSavings");
        }

        public Task<CreateSavingsCommentResponse> CreateCommentAsync(string savingsAccountNr, string commentText, string eventType,
            bool? dontReturnComment, string attachedFileAsDataUrl, string attachedFileName, int? customerSecureMessageId) => client.Call(
                x => x.PostJson("Api/SavingsAccountComment/Create", new { savingsAccountNr, commentText, eventType, dontReturnComment, attachedFileAsDataUrl, attachedFileName, customerSecureMessageId }),
                x => x.ParseJsonAs<CreateSavingsCommentResponse>());

        public CreateSavingsCommentResponse CreateComment(string savingsAccountNr, string commentText, string eventType,
            bool? dontReturnComment, string attachedFileAsDataUrl, string attachedFileName, int? customerSecureMessageId) =>
            client.ToSync(() => CreateCommentAsync(savingsAccountNr, commentText, eventType, dontReturnComment, attachedFileAsDataUrl, attachedFileName, customerSecureMessageId));
    }
}
