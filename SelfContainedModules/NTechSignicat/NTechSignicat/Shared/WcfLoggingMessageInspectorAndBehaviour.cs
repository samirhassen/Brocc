using Microsoft.Extensions.Logging;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading.Tasks;
using System.Xml;

namespace NTechSignicat.Shared
{
    //From: https://stackoverflow.com/questions/38868940/wcf-client-logging-dotnet-core
    public class WcfLoggingMessageInspectorAndBehaviour : IClientMessageInspector, IEndpointBehavior
    {
        private readonly INEnv env;

        public WcfLoggingMessageInspectorAndBehaviour(ILogger<WcfLoggingMessageInspectorAndBehaviour> logger, INEnv env)
        {
            Logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            this.env = env;
        }

        public ILogger<WcfLoggingMessageInspectorAndBehaviour> Logger { get; }

        private void Log(string context, string message)
        {
            var now = DateTimeOffset.Now;
            var msg = $"--------{DateTimeOffset.Now}---------{Environment.NewLine}{message}{Environment.NewLine}";
            Logger.LogTrace(msg);
            var logFolder = LogFolder;
            if (logFolder != null)
            {
                Directory.CreateDirectory(logFolder.FullName);
                var logFile = Path.Combine(logFolder.FullName, $"{now.ToString("yyyyMMdd")}{context}-logs.txt");
                File.AppendAllText(logFile, msg);
            }
        }

        private DirectoryInfo LogFolder
        {
            get
            {
                if (!env.IsVerboseLoggingEnabled)
                    return null;
                var f = env.LogFolder;
                if (f == null)
                    return null;
                return new DirectoryInfo(Path.Combine(f.FullName, "NTechSignicat"));
            }
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            using (var buffer = reply.CreateBufferedCopy(int.MaxValue))
            {
                var document = GetDocument(buffer.CreateMessage());
                Log("response", document.OuterXml);

                reply = buffer.CreateMessage();
            }
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            using (var buffer = request.CreateBufferedCopy(int.MaxValue))
            {
                var document = GetDocument(buffer.CreateMessage());
                Log("request", document.OuterXml);

                request = buffer.CreateMessage();
                return null;
            }
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(this);
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }

        private XmlDocument GetDocument(Message request)
        {
            var document = new XmlDocument();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // write request to memory stream
                XmlWriter writer = XmlWriter.Create(memoryStream);
                request.WriteMessage(writer);
                writer.Flush();
                memoryStream.Position = 0;

                // load memory stream into a document
                document.Load(memoryStream);
            }

            return document;
        }
    }
}
