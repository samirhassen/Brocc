using System;
using System.ServiceModel.Dispatcher;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.Xml.Linq;

namespace nCreditReport.Code
{
    public class LoggedSoapServiceClient<TChannel, TClient> 
        where TClient : ClientBase<TChannel>
        where TChannel: class
    {
        private readonly string endpointUrl;
        private readonly Func<Binding, EndpointAddress, TClient> createClient;

        public LoggedSoapServiceClient(string endpointUrl, Func<Binding, EndpointAddress, TClient> createClient)
        {
            this.endpointUrl = endpointUrl;
            this.createClient = createClient;
        }

        public (TResult Result, XDocument RawRequest, XDocument RawResponse) ExecuteLogged<TResult>(Func<TClient, TResult> execute)
        {
            HttpBindingBase binding;
            EndpointAddress address;
            var b = new BasicHttpsBinding();
            b.MaxReceivedMessageSize = 2000000000L;
            b.Security.Mode = BasicHttpsSecurityMode.Transport;
            binding = b;
            address = new EndpointAddress(endpointUrl);
            var client = createClient(binding, address);
            var inspector = new RawMessageInspector();
            client.Endpoint.EndpointBehaviors.Add(new RawMessageEndpointBehavior
            {
                Inspector = inspector
            });
            var result = execute(client);
            return (Result: result, RawRequest: inspector.RawXmlRequest, RawResponse: inspector.RawXmlResponse);
        }

        private class RawMessageInspector : IClientMessageInspector
        {
            public XDocument RawXmlRequest { get; set; }
            public XDocument RawXmlResponse { get; set; }            

            public void AfterReceiveReply(ref System.ServiceModel.Channels.Message reply, object correlationState)
            {
                var buffer = reply.CreateBufferedCopy(int.MaxValue);
                RawXmlResponse = XDocument.Parse(buffer.CreateMessage().GetReaderAtBodyContents().ReadOuterXml());
                reply = buffer.CreateMessage();
            }

            public object BeforeSendRequest(ref System.ServiceModel.Channels.Message request, IClientChannel channel)
            {
                MessageBuffer buffer = request.CreateBufferedCopy(int.MaxValue);
                RawXmlRequest = XDocument.Parse(buffer.CreateMessage().GetReaderAtBodyContents().ReadOuterXml());
                request = buffer.CreateMessage();
                return null;
            }
        }

        private class RawMessageBehaviorExtensionElement : BehaviorExtensionElement
        {
            public RawMessageInspector Inspector { get; set; } = new RawMessageInspector();

            public override Type BehaviorType
            {
                get { return typeof(RawMessageEndpointBehavior); }
            }

            protected override object CreateBehavior()
            {
                var b = new RawMessageEndpointBehavior();
                b.Inspector = Inspector;
                return b;
            }
        }

        private class RawMessageEndpointBehavior : IEndpointBehavior
        {

            public RawMessageInspector Inspector { get; set; }

            public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
            {
                clientRuntime.ClientMessageInspectors.Add(Inspector);
            }

            public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
            {
            }

            public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
            {
            }

            public void Validate(ServiceEndpoint endpoint)
            {
            }
        }
    }
}