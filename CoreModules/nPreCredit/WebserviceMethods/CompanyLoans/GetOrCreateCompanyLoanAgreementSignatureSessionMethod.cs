using Newtonsoft.Json;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class GetOrCreateCompanyLoanAgreementSignatureSessionMethod : TypedWebserviceMethod<GetOrCreateCompanyLoanAgreementSignatureSessionMethod.Request, GetOrCreateCompanyLoanAgreementSignatureSessionMethod.Response>
    {
        public override string Path => "CompanyLoan/GetOrCreate-Agreement-Signature-Session";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();

            var ai = resolver.Resolve<ApplicationInfoService>().GetApplicationInfo(request.ApplicationNr);

            var s = requestContext.Resolver().Resolve<ICompanyLoanAgreementSignatureService>();

            Func<CompanyLoanSignatureSessionModel, bool, CompanyLoanSignatureSessionModel> intercept = (session, wasNewSignicatSessionCreated) =>
               {
                   if (!request.SupressSendingSignatureLinks.GetValueOrDefault())
                   {
                       if (wasNewSignicatSessionCreated)
                           session = s.SendAgreementSignatureEmails(ai);
                       else if (request.ResendLinkOnExistingCustomerIds != null && request.ResendLinkOnExistingCustomerIds.Count > 0)
                           session = s.SendAgreementSignatureEmails(ai, request.ResendLinkOnExistingCustomerIds.ToHashSet());
                   }

                   return session;
               };

            var model = s.CreateOrGetSignatureModel(ai,
                refreshSignatureSessionIfNeeded: request.RefreshSignatureSessionIfNeeded.GetValueOrDefault(),
                disableCheckForNewSignatures: request.DisableCheckForNewSignatures.GetValueOrDefault(),
                intercept: intercept);

            return new Response
            {
                Session = model
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
            public bool? RefreshSignatureSessionIfNeeded { get; set; }
            public bool? DisableCheckForNewSignatures { get; set; }
            public bool? SupressSendingSignatureLinks { get; set; }
            public List<int> ResendLinkOnExistingCustomerIds { get; set; }
        }

        public class Response
        {
            public CompanyLoanSignatureSessionModel Session { get; set; }
        }
    }
}