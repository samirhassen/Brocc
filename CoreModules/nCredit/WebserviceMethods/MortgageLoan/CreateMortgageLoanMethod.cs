using nCredit.DbModel.BusinessEvents;
using nCredit.WebserviceMethods.MortgageLoan;
using NTech.Core;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;

namespace nCredit.WebserviceMethods
{
    public class CreateMortgageLoanMethod : TypedWebserviceMethod<ValidatableMortgageLoanRequest, CreateMortgageLoanMethod.Response>
    {
        public override string Path => "MortgageLoans/Create";

        public override bool IsEnabled => NEnv.IsMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, ValidatableMortgageLoanRequest request)
        {
            ValidateUsingAnnotations(request);

            var u = requestContext.CurrentUserMetadata();
            var services = requestContext.Service();
            var mgr = services.NewMortgageLoanManager;

            Response result;
            using (var context = new CreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var createdCredit = context.UsingTransaction(() =>
                {
                    var credit = mgr.CreateNewMortgageLoan(context, request);
                    context.SaveChanges();
                    return credit;
                });

                result = new Response
                {
                    CreditNr = createdCredit.CreditNr
                };
            }

            requestContext.Service().CustomerRelationsMerge.MergeLoansToCustomerRelations(onlyTheseCreditNrs: new HashSet<string> { result.CreditNr });

            return result;
        }

        public class Response
        {
            public string CreditNr { get; set; }
        }
    }
}