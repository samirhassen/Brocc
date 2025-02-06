using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Code
{
    public class SavingsClient : AbstractServiceClient
    {
        protected override string ServiceName => "nSavings";

        public CreateCommentResponse CreateComment(string savingsAccountNr, string commentText, string eventType,
            bool? dontReturnComment, string attachedFileAsDataUrl, string attachedFileName, int? customerSecureMessageId)
        {
            return Begin()
                .PostJson("Api/SavingsAccountComment/Create", new { savingsAccountNr, commentText, eventType, dontReturnComment, attachedFileAsDataUrl, attachedFileName, customerSecureMessageId })
                .ParseJsonAs<CreateCommentResponse>();
        }

        public ISet<int> FetchCustomerIdsThatCanBeArchived(ISet<int> candidateCustomerIds)
        {
            return Begin()
                .PostJson("Api/Savings/Fetch-CustomerIds-That-Can-Be-Archived", new { candidateCustomerIds })
                .ParseJsonAsAnonymousType(new { ArchivableCustomerIds = (List<int>)null })
                ?.ArchivableCustomerIds
                ?.ToHashSet();
        }

        public class CreateCommentResponse
        {
            public int Id { get; set; }
            public CommentModel comment { get; set; }

            public class CommentModel
            {
                public DateTimeOffset CommentDate { get; set; }
                public string CommentText { get; set; }
                public List<string> ArchiveLinks { get; set; }
                public string DisplayUserName { get; set; }
                public int? CustomerSecureMessageId { get; set; }
            }
        }
    }
}