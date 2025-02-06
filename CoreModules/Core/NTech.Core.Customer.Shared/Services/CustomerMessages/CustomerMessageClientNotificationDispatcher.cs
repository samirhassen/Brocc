using nCustomer.Code.Services.Settings;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure.Email;
using System;
using System.Collections.Generic;

namespace nCustomer.Code.Services.CustomerMessages
{
    public class CustomerMessageClientNotificationDispatcher
    {
        private readonly ReadonlySettingsService settingsService;
        private readonly INTechEmailServiceFactory emailServiceFactory;
        private readonly ILoggingService loggingService;
        private readonly INTechServiceRegistry serviceRegistry;

        public CustomerMessageClientNotificationDispatcher(ReadonlySettingsService settingsService, INTechEmailServiceFactory emailServiceFactory, ILoggingService loggingService,
            INTechServiceRegistry serviceRegistry)
        {
            this.settingsService = settingsService;
            this.emailServiceFactory = emailServiceFactory;
            this.loggingService = loggingService;
            this.serviceRegistry = serviceRegistry;
        }

        public bool Notify(CustomerMessageModel message, out bool isFailedToSend)
        {
            isFailedToSend = false;

            if (!message.IsFromCustomer)
                return false;

            if (!emailServiceFactory.HasEmailProvider)
                return false;

            var settings = settingsService.LoadSettingsValues("clientIncomingSecureMessageNotifications");
            var isEnabled = settings.Opt("isEnabled") == "true";
            if (!isEnabled)
                return false;

            var notificationBackOfficeUrl = serviceRegistry.ExternalServiceUrl(
                "nBackOffice", "s/secure-messages/channel",
                Tuple.Create("channelId", message.ChannelId),
                Tuple.Create("channelType", message.ChannelType),
                Tuple.Create("customerId", message.CustomerId.ToString())).ToString();

            var emailDataContext = new Dictionary<string, object>
            {
                { "notificationBackOfficeUrl", notificationBackOfficeUrl },
                { "channelType", message.ChannelType },
                { "channelId", message.ChannelId }
            };

            var email = settings["clientGroupEmail"];
            try
            {
                emailServiceFactory.CreateEmailService().SendRawEmail(
                    new List<string> { email },
                    settings["notificationTemplateSubjectText"],
                    settings["notificationTemplateBodyText"],
                    emailDataContext,
                    $"nCustomer.ClientIncomingSecureMessageNotification: m = {message.Id}, c = {message.CustomerId}, n = {message.ChannelType}/{message.ChannelId}");
                return true;
            }
            catch (Exception ex)
            {
                loggingService.Error(ex, "Error in CustomerMessageClientNotificationDispatcher.Notify. Notification to client not sent.");
                isFailedToSend = true;
                return false;
            }
        }
    }
}