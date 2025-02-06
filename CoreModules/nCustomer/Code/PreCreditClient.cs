using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Code
{
    public class PreCreditClient : AbstractServiceClient
    {
        protected override string ServiceName => "nPreCredit";

        public ISet<int> FetchCustomerIdsThatCanBeArchived(ISet<int> candidateCustomerIds)
        {
            return Begin()
                .PostJson("Api/Application/Fetch-CustomerIds-That-Can-Be-Archived", new { candidateCustomerIds })
                .ParseJsonAsAnonymousType(new { ArchivableCustomerIds = (List<int>)null })
                ?.ArchivableCustomerIds
                ?.ToHashSet();
        }

        public Tuple<HashSet<int>, string> StreamArchiveCandidateCustomerIds(int batchSize, string lastStreamPositionToken)
        {
            var result = Begin()
                .PostJson("Api/Application/Stream-ArchiveCandidate-CustomerIds", new { batchSize, lastStreamPositionToken })
                .ParseJsonAsAnonymousType(new { LastStreamPositionToken = "", ArchiveCandidateCustomerIds = (List<int>)null });
            return Tuple.Create(result?.ArchiveCandidateCustomerIds?.ToHashSet(), result?.LastStreamPositionToken);
        }

        public void AddCommentToApplication(string applicationNr, string commentText, int? customerSecureMessageId)
        {
            Begin()
                .PostJson("api/ApplicationComments/Add", new
                {
                    applicationNr,
                    commentText,
                    customerSecureMessageId
                })
                .EnsureSuccessStatusCode();
        }
    }
}