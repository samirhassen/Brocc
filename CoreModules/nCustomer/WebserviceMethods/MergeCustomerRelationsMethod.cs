using NTech.Core.Customer.Shared.Services;
using NTech.Services.Infrastructure.NTechWs;

namespace nCustomer.WebserviceMethods
{
    public class MergeCustomerRelationsMethod : TypedWebserviceMethod<MergeCustomerRelationsRequest, MergeCustomerRelationsResponse>
    {
        public override string Path => "CustomerRelations/Merge";

        protected override MergeCustomerRelationsResponse DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, MergeCustomerRelationsRequest request)
        {
            var service = new MergeCustomerRelationsService(new System.Lazy<string>(() =>
                System.Configuration.ConfigurationManager.ConnectionStrings["CustomersContext"].ConnectionString));

            return service.MergeCustomerRelation(request);
        }
    }
}