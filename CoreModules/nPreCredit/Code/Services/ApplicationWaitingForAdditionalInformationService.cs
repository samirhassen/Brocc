using NTech;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class ApplicationWaitingForAdditionalInformationService : IApplicationWaitingForAdditionalInformationService
    {
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;
        private readonly IClock clock;

        public ApplicationWaitingForAdditionalInformationService(INTechCurrentUserMetadata ntechCurrentUserMetadata, IClock clock)
        {
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
            this.clock = clock;
        }

        public SetIsWaitingForAdditionalInformationResult SetIsWaitingForAdditionalInformation(string applicationNr, bool isWaitingForAdditionalInformation)
        {
            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
            {
                var app = context.CreditApplicationHeaders.Single(x => x.ApplicationNr == applicationNr);
                app.WaitingForAdditionalInformationDate = isWaitingForAdditionalInformation ? new DateTimeOffset?(context.Clock.Now) : null;

                var commentText = "Waiting for additional information " + (isWaitingForAdditionalInformation ? "set" : "removed");
                var eventType = "WaitingForAdditionalInformation" + (isWaitingForAdditionalInformation ? "Set" : "Removed");

                var c = context.CreateAndAddComment(commentText, eventType, applicationNr: applicationNr);

                context.SaveChanges();

                return new SetIsWaitingForAdditionalInformationResult
                {
                    IsWaitingForAdditionalInformation = isWaitingForAdditionalInformation,
                    AddedCommentId = c?.Id
                };
            }
        }
    }

    public interface IApplicationWaitingForAdditionalInformationService
    {
        SetIsWaitingForAdditionalInformationResult SetIsWaitingForAdditionalInformation(string applicationNr, bool isWaitingForAdditionalInformation);
    }

    public class SetIsWaitingForAdditionalInformationResult
    {
        public bool IsWaitingForAdditionalInformation { get; set; }
        public int? AddedCommentId { get; set; }
    }
}