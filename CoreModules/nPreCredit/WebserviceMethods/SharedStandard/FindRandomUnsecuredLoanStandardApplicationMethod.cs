using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class FindRandomUnsecuredLoanStandardApplicationMethod : TypedWebserviceMethod<FindRandomUnsecuredLoanStandardApplicationMethod.Request, FindRandomUnsecuredLoanStandardApplicationMethod.Response>
    {
        public override string Path => "LoanStandard/FindRandomApplication";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var q = ApplicationInfoService.GetApplicationInfoQueryable(context);
                if (request.IsCancelled.GetValueOrDefault())
                    q = q.Where(x => x.IsCancelled);
                else if (request.IsRejected.GetValueOrDefault())
                    q = q.Where(x => x.IsRejected);
                else if (request.IsFinalDecisionMade.GetValueOrDefault())
                    q = q.Where(x => x.IsFinalDecisionMade);
                else
                    q = q.Where(x => x.IsActive);

                if (request.NrOfApplicants.HasValue)
                    q = q.Where(x => x.NrOfApplicants == request.NrOfApplicants.Value);

                if (!string.IsNullOrWhiteSpace(request.MemberOfListName))
                    q = q.Where(x => x.ListNames.Contains(request.MemberOfListName));

                var count = q.Count();

                string applicationNr;

                if (count == 0)
                    applicationNr = null;
                else
                {
                    if (count > 1)
                    {
                        var skip = new Random().Next(0, count);
                        q = q.OrderBy(x => x.ApplicationNr).Skip(skip);
                    }
                    applicationNr = q.Select(x => x.ApplicationNr).FirstOrDefault();
                }

                return new Response
                {
                    ApplicationNr = applicationNr
                };
            }
        }

        public class Response
        {
            public string ApplicationNr { get; set; }
        }

        public class Request
        {
            public bool? IsFinalDecisionMade { get; set; }
            public bool? IsRejected { get; set; }
            public bool? IsCancelled { get; set; }
            public int? NrOfApplicants { get; set; }
            public string MemberOfListName { get; set; }
        }
    }
}