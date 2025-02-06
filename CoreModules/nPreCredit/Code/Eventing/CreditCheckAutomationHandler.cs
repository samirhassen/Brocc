using Newtonsoft.Json;
using nPreCredit.Code.Clients;
using NTech.Services.Infrastructure.Eventing;
using Serilog;
using System;
using System.Threading;

namespace nPreCredit.Code
{
    public class CreditCheckAutomationHandler : EventSubscriberBase, IEventSubscriber
    {
        public static bool IsAutomationSuspended { get; set; }

        public CreditCheckAutomationHandler()
        {

        }

        public void OnStartup(Func<string, Action<string, CancellationToken>, string> subscribe)
        {
            Subscribe(PreCreditEventCode.CreditApplicationCreated, OnCreditApplicationCreated, subscribe);
        }

        private void OnCreditApplicationCreated(string data, CancellationToken ct)
        {
            try
            {
                var dataParsed = JsonConvert.DeserializeAnonymousType(data, new { applicationNr = "", disableAutomation = (bool?)null });
                if ((dataParsed?.disableAutomation ?? false))
                {
                    NLog.Information("OnCreditApplicationCreated skipped on {applicationNr} since the caller opted out of automation.", dataParsed?.applicationNr);
                    return;
                }
                if (IsAutomationSuspended)
                {
                    NLog.Information("OnCreditApplicationCreated skipped on {applicationNr} since automation is suspended.", dataParsed?.applicationNr);
                    return;
                }

                if (NEnv.IsCreditCheckAutomationEnabled)
                {
                    var client = new nPreCreditClient(AquireBearerToken);

                    if (string.IsNullOrWhiteSpace(dataParsed?.applicationNr))
                        throw new Exception("Missing applicationNr");

                    client.AutomaticCreditCheck(dataParsed.applicationNr, true, NEnv.IsAllowedToAutoFollowAcceptedCreditDecisions);
                }
                else
                {
                    NLog.Debug("OnCreditApplicationCreated skipped on {applicationNr} since credit check automation is not enabled.", dataParsed?.applicationNr);
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "CreditCheckAutomationHandler error handling new application: {data}", data);
            }
        }
    }
}