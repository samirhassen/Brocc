using nCredit.DbModel.BusinessEvents;
using nCredit.DomainModel;
using nCredit.WebserviceMethods.MortgageLoan;
using NTech.Core;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCredit.WebserviceMethods
{
    public class CreateMortgageLoanBulkMethod : TypedWebserviceMethod<CreateMortgageLoanBulkMethod.Request, CreateMortgageLoanBulkMethod.Response>
    {
        public override string Path => "MortgageLoans/Create-Bulk";

        public override bool IsEnabled => NEnv.IsMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var u = requestContext.CurrentUserMetadata();

            var services = requestContext.Service();
            var mgr = services.NewMortgageLoanManager;
            var credits = new List<CreditHeader>();
            using (var context = new CreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                context.DoUsingTransaction(() =>
                {
                    var sharedValuesModel = new SharedDatedValueDomainModel(context);
                    var currentReferenceInterestRate = new Lazy<decimal>(() => sharedValuesModel.GetReferenceInterestRatePercent(mgr.Clock.Today));

                    foreach (var loanRequest in request.Loans)
                    {
                        credits.Add(mgr.CreateNewMortgageLoan(context, loanRequest, currentReferenceInterestRate));
                    }

                    context.SaveChanges();
                });
            }

            requestContext.Service().CustomerRelationsMerge.MergeLoansToCustomerRelations(onlyTheseCreditNrs: credits.Select(x => x.CreditNr).ToHashSet());

            return new Response();
        }

        public class Request
        {
            [Required]
            public ValidatableMortgageLoanRequest[] Loans { get; set; }
        }

        public class Response
        {
        }
    }
}