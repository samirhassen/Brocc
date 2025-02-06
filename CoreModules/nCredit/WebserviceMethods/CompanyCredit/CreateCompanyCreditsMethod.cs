using nCredit.DbModel.BusinessEvents.NewCredit;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure.NTechWs;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
namespace nCredit.WebserviceMethods
{
    public class CreateCompanyCreditsMethod : TypedWebserviceMethod<CreateCompanyCreditsMethod.Request, CreateCompanyCreditsMethod.Response>
    {
        public override string Path => "CompanyCredit/Create-Batch";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var user = requestContext.CurrentUserMetadata();

            var services = requestContext.Service();
            var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceHttpContextUser.SharedInstance, NEnv.ServiceRegistry);
            var mgr = new NewCreditBusinessEventManager(user, services.LegalInterestCeiling, services.CreditCustomerListService,
                services.GetEncryptionService(requestContext.CurrentUserMetadata()), new CoreClock(), NEnv.ClientCfgCore, customerClient,
                services.CreateOcrPaymentReferenceGenerator(user), NEnv.EnvSettings, services.PaymentAccount, services.CustomCostType);

            List<string> creditNrs;
            using (var context = new CreditContextExtended(user, requestContext.Clock()))
            {
                creditNrs = new List<string>();
                context.BeginTransaction();
                try
                {
                    var model = new DomainModel.SharedDatedValueDomainModel(context);
                    var getReferenceInterest = new Lazy<decimal>(() => model.GetReferenceInterestRatePercent(requestContext.Clock().Today));

                    foreach (var creditRequest in request.Credits)
                    {
                        var r = CreateCompanyCreditMethod.ConvertRequestToInternal(creditRequest);

                        var credit = mgr.CreateNewCredit(context, r, getReferenceInterest);

                        context.SaveChanges();
                        context.CommitTransaction();

                        creditNrs.Add(credit.CreditNr);
                    }
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }

            try
            {
                if (creditNrs.Count > 0)
                {
                    services.CustomerRelationsMerge.MergeLoansToCustomerRelations(onlyTheseCreditNrs: creditNrs.ToHashSet());
                }
            }
            catch (Exception ex)
            {
                NLog.Error("CreateCompanyCreditsMethod, failed to merge customer relations", ex);
            }

            return new Response
            {
                CreditNrs = creditNrs
            };
        }

        public class Request
        {
            [Required]
            public List<CreateCompanyCreditMethod.Request> Credits { get; set; }
        }

        public class Response
        {
            public List<string> CreditNrs { get; set; }
        }
    }
}