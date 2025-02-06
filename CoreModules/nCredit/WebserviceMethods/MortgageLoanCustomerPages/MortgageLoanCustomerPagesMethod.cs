using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;
namespace nCredit.WebserviceMethods
{
    public abstract class MortgageLoanCustomerPagesMethod<TRequest, TResponse> : TypedWebserviceMethod<TRequest, TResponse>
        where TRequest : MortgageLoanCustomerPagesRequestBase, new()
        where TResponse : class, new()
    {
        public override bool IsEnabled => NEnv.IsMortgageLoansEnabled;

        public override string Path => $"MortageLoan/CustomerPages/{MethodName}";

        protected override TResponse DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, TRequest request)
        {
            ValidateUsingAnnotations(request);

            return DoCustomerLockedExecuteTyped(requestContext, request, request.CustomerId.Value);
        }

        protected abstract string MethodName { get; }

        protected abstract TResponse DoCustomerLockedExecuteTyped(NTechWebserviceMethodRequestContext requestContext, TRequest request, int customerPagesUserCustomerId);
    }

    public abstract class MortgageLoanCustomerPagesRequestBase
    {
        [Required]
        public int? CustomerId { get; set; }
    }
}