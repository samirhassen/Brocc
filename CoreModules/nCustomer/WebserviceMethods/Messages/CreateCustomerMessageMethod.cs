using NTech.Core.Customer.Shared.Services;
using NTech.Legacy.Module.Shared.Services;
using NTech.Services.Infrastructure.NTechWs;

namespace nCustomer.WebserviceMethods.Messages
{
    public class CreateCustomerMessageMethod : TypedWebserviceMethod<CustomerMessageSendingService.Request, CustomerMessageSendingService.Response>
    {
        public override string Path => "CustomerMessage/CreateMessage";

        public override bool IsEnabled => NEnv.IsSecureMessagesEnabled;

        protected override CustomerMessageSendingService.Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, CustomerMessageSendingService.Request request)
        {
            ValidateUsingAnnotations(request);
            var service = new CustomerMessageSendingService(requestContext.Service().CustomerMessage, requestContext.CurrentUserMetadata().CoreUser, new SerilogLoggingService());
            return service.SendMessage(request);
        }
    }
}