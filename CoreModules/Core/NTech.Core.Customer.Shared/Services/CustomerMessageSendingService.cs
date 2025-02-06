using nCustomer.Code.Services;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.ComponentModel.DataAnnotations;

namespace NTech.Core.Customer.Shared.Services
{
    public class CustomerMessageSendingService
    {
        private readonly INonSearchCustomerMessageService customerMessageService;
        private readonly INTechCurrentUserMetadata user;
        private readonly ILoggingService loggingService;

        public CustomerMessageSendingService(INonSearchCustomerMessageService customerMessageService, INTechCurrentUserMetadata user, ILoggingService loggingService)
        {
            this.customerMessageService = customerMessageService;
            this.user = user;
            this.loggingService = loggingService;
        }

        public Response SendMessage(Request request)
        {
            TransformRequest(request);

            var userid = user.UserId;

            var messageModel = customerMessageService.SaveCustomerMessage(
                  request.CustomerId,
                   request.ChannelType,
                   request.ChannelId,
                   request.Text,
                   request.TextFormat,
                   request.IsFromCustomer, userid);

            if (!request.IsFromCustomer && request.FlagPreviousMessagesAsHandled.GetValueOrDefault())
            {
                customerMessageService.FlagMessagesBeforeInChannelAsHandled(messageModel.Id, messageModel.CustomerId, messageModel.ChannelType, messageModel.ChannelId, !messageModel.IsFromCustomer, userid);
            }

            var wasNotificationEmailSent = false;
            if (!request.IsFromCustomer && request.NotifyCustomerByEmail.GetValueOrDefault())
            {
                wasNotificationEmailSent = TryNotifyCustomerByEmail(messageModel, customerMessageService, user);
            }

            return new Response
            {
                CreatedMessage = new Response.MessageModel
                {
                    Id = messageModel.Id,
                    CustomerId = messageModel.CustomerId,
                    ChannelType = messageModel.ChannelType,
                    ChannelId = messageModel.ChannelId,
                    CreatedByUserId = messageModel.CreatedByUserId,
                    CreationDate = messageModel.CreationDate,
                    HandledByUserId = messageModel.HandledByUserId,
                    HandledDate = messageModel.HandledDate,
                    IsFromCustomer = messageModel.IsFromCustomer,
                    Text = messageModel.Text,
                    TextFormat = messageModel.TextFormat,
                },
                WasNotificationEmailSent = wasNotificationEmailSent
            };
        }


        private void TransformRequest(Request request)
        {
            if (request?.TextFormat == "markdown")
            {
                request.Text = CommonMark.CommonMarkConverter.Convert(request.Text);
                request.TextFormat = "html";
            }
        }

        private bool TryNotifyCustomerByEmail(CustomerMessageModel message, INonSearchCustomerMessageService messageService, INTechCurrentUserMetadata currentUser)
        {
            try
            {
                messageService.SendNewMessageNotification(message, currentUser);
                return true;
            }
            catch (Exception ex)
            {
                loggingService.Error(ex, "Error in CreateCustomerMessageMethodTryNotifyCustomerByEmail. Message still sent just not notified.");
                return false;
            }
        }

        public class Request
        {
            [Required]
            public int CustomerId { get; set; }
            [Required]
            public string ChannelType { get; set; }
            [Required]
            public string ChannelId { get; set; }
            [Required]
            public string Text { get; set; }
            public string TextFormat { get; set; }
            [Required]
            public bool IsFromCustomer { get; set; }
            public bool? FlagPreviousMessagesAsHandled { get; set; }
            public bool? NotifyCustomerByEmail { get; set; }
        }

        public class Response
        {
            public class MessageModel
            {
                public int Id { get; set; }
                public string Text { get; set; }
                public string TextFormat { get; set; }
                public int CustomerId { get; set; }
                public bool IsFromCustomer { get; set; }
                public DateTime CreationDate { get; set; }
                public int CreatedByUserId { get; set; }
                public DateTime? HandledDate { get; set; }
                public int? HandledByUserId { get; set; }
                public string ChannelType { get; set; }
                public string ChannelId { get; set; }
            }

            public MessageModel CreatedMessage { get; set; }
            public bool WasNotificationEmailSent { get; set; }
        }
    }
}
