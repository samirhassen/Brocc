using Newtonsoft.Json;
using nPreCredit.Code.Plugins;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using Serilog;
using System;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoans
{
    public class CreateMortgageLoanApplicationMethod : RawRequestWebserviceMethod<CreateMortgageLoanApplicationMethod.Response>
    {
        public override string Path => "mortgageloan/create-application";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

        public override Type RequestType
        {
            get
            {
                return PluginMortgageLoanApplicationRequestTranslator.GetRequestType();
            }
        }

        protected override Response DoExecuteRaw(NTechWebserviceMethodRequestContext requestContext, string jsonRequest)
        {
            var request = JsonConvert.DeserializeObject(jsonRequest, RequestType);

            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();

            var tr = resolver.Resolve<PluginMortgageLoanApplicationRequestTranslator>();
            if (!tr.TranslateApplicationRequest(request, out var internalRequest, out var errorCodeAndMessage))
                return Error(errorCodeAndMessage.Item2, errorCode: errorCodeAndMessage.Item1);

            string failedMessage;
            string applicationNr;
            if (requestContext.Resolver().Resolve<IMortgageLoanApplicationCreationService>().TryCreateApplication(internalRequest, out failedMessage, out applicationNr))
            {
                MortgageLoanInitialScoringResponse.Offer acceptedOffer = null;
                MortgageLoanInitialScoringResponse.RejectionDetails rejectedDetails = null;
                if (!(internalRequest.SkipInitialScoring))
                {
                    try
                    {
                        MortgageLoanInitialScoringResponse result;
                        string directScoringFailedMessage;
                        if (requestContext.Resolver().Resolve<IMortgageLoanApplicationInitialCreditCheckService>().TryDoInitialScoring(applicationNr, true, out result, out directScoringFailedMessage))
                        {
                            if (result.AcceptedOffer != null)
                            {
                                using (var context = new PreCreditContext())
                                {
                                    var m = context.MortgageLoanCreditApplicationHeaderExtensions.Single(x => x.ApplicationNr == applicationNr);
                                    m.CustomerOfferStatus = MortgageLoanCustomerOfferStatusCode.OfferSent.ToString();
                                    context.SaveChanges();
                                    acceptedOffer = result.AcceptedOffer;
                                }
                            }
                            else
                                rejectedDetails = result.RejectedDetails;
                        }
                        else
                        {
                            //Report error on app
                            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata().UserId, requestContext.Clock(), requestContext.CurrentUserMetadata().InformationMetadata))
                            {
                                context.CreateAndAddComment($"Direct scoring failed: {directScoringFailedMessage}", "InitialScoringFailed", applicationNr: applicationNr);
                                context.SaveChanges();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorId = Guid.NewGuid().ToString();
                        NLog.Error(ex, $"Direct scoring crashed. ApplicationNr={applicationNr} ErrorId = {errorId}");
                        using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata().UserId, requestContext.Clock(), requestContext.CurrentUserMetadata().InformationMetadata))
                        {
                            context.CreateAndAddComment($"Direct scoring crashed. Look for '{errorId}' in the error log", "InitialScoringFailed", applicationNr: applicationNr);
                            context.SaveChanges();
                        }
                    }
                }

                var directScoringResult = acceptedOffer != null || rejectedDetails != null
                    ? new Response.DirectScoringResultModel
                    {
                        IsAccepted = acceptedOffer != null,
                        AcceptedOffer = acceptedOffer,
                        RejectedDetails = rejectedDetails
                    } : null;

                return new Response
                {
                    ApplicationNr = applicationNr,
                    DirectScoringResult = directScoringResult
                };
            }
            else
                return Error(failedMessage);
        }

        public class Response
        {
            public string ApplicationNr { get; set; }
            public DirectScoringResultModel DirectScoringResult { get; set; }

            public class DirectScoringResultModel
            {
                public bool IsAccepted { get; set; }
                public MortgageLoanInitialScoringResponse.Offer AcceptedOffer { get; set; }
                public MortgageLoanInitialScoringResponse.RejectionDetails RejectedDetails { get; set; }
            }
        }
    }
}