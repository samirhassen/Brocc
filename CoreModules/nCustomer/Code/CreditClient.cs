using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Code
{
    public class CreditClient : AbstractServiceClient
    {
        protected override string ServiceName => "nCredit";

        public CreateCreditCommentResponse CreateCreditComment(string creditNr, string commentText, string eventType, bool? dontReturnComment, string attachedFileAsDataUrl, string attachedFileName, int? customerSecureMessageId)
        {
            return Begin()
                .PostJson("Api/CreditComment/Create", new { creditNr, commentText, eventType, dontReturnComment, attachedFileAsDataUrl, attachedFileName, customerSecureMessageId })
                .ParseJsonAs<CreateCreditCommentResponse>();
        }

        public ISet<int> FetchCustomerIdsThatCanBeArchived(ISet<int> candidateCustomerIds)
        {
            return Begin()
                .PostJson("Api/Credit/Fetch-CustomerIds-That-Can-Be-Archived", new { candidateCustomerIds })
                .ParseJsonAsAnonymousType(new { ArchivableCustomerIds = (List<int>)null })
                ?.ArchivableCustomerIds
                ?.ToHashSet();
        }

        public class CreateCreditCommentResponse
        {
            public int Id { get; set; }
            public CommentModel comment { get; set; }

            public class CommentModel
            {
                public string EventType { get; set; }
                public DateTimeOffset CommentDate { get; set; }
                public string CommentText { get; set; }
                public List<string> ArchiveLinks { get; set; }
                public string DisplayUserName { get; set; }
                public int? CustomerSecureMessageId { get; set; }
            }
        }
    }
}