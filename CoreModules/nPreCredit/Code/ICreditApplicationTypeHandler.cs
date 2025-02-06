using nPreCredit.Code.Services;
using System;
using System.Collections.Generic;

namespace nPreCredit.Code
{
    public interface ICreditApplicationTypeHandler
    {
        string GetNewCreditCheckUrl(IServiceRegistryUrlService urlService, string applicationNr);
        string GetViewCreditDecisionUrl(IServiceRegistryUrlService urlService, int decisionId);
        bool ShouldCheckAgreementUpdatesOnEnterApplication();
        AgreementSigningStatusWithPending GetAgreementSigningStatusWithPending(string applicationNr, IEnumerable<CreditApplicationOneTimeToken> tokens, out IList<CreditApplicationOneTimeToken> currentlyPendingTokens);
        IDictionary<string, string> GetRejectionReasonToDisplayNameMapping();
    }

    public class AgreementSigningStatusWithPending
    {
        public class Applicant
        {
            public string status { get; set; }
            public string signedDocumentUrl { get; set; }
            public DateTimeOffset? signedDate { get; set; }
            public string failureMessage { get; set; }
            public DateTimeOffset? sentDate { get; set; }
        }

        public Applicant applicant1 { get; set; }
        public Applicant applicant2 { get; set; }
        public bool isSendAllowed { get; set; }
    }
}
