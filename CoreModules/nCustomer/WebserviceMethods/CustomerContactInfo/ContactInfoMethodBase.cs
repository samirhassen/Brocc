using NTech.Services.Infrastructure.NTechWs;

namespace nCustomer.WebserviceMethods
{
    public abstract class ContactInfoMethodBase<TRequest, TResponse> : TypedWebserviceMethod<TRequest, TResponse>
        where TRequest : class, new()
        where TResponse : class, new()
    {
        protected CustomerWriteRepository CreateRepo(NTechWebserviceMethodRequestContext requestContext, DbModel.CustomersContext db)
        {
            return new CustomerWriteRepository(db,
                requestContext.CurrentUserMetadata().CoreUser,
                requestContext.Clock(),
                requestContext.Service().EncryptionService, NEnv.ClientCfgCore);
        }
    }
}