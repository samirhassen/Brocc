using nCustomer.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.WebserviceMethods.Messages
{
    public class GetCustomerMessagesMethod : TypedWebserviceMethod<GetCustomerMessagesMethod.Request, GetCustomerMessagesMethod.Response>
    {
        public override string Path => "CustomerMessage/GetMessages";

        public override bool IsEnabled => NEnv.IsSecureMessagesEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (!request.CustomerId.HasValue && string.IsNullOrWhiteSpace(request.ChannelType) && string.IsNullOrWhiteSpace(request.ChannelId) && !request.IsHandled.HasValue)
                return Error("Either CustomerId,ChannelType, ChannelId or IsHandled must be specified", errorCode: "invalidRequest");

            var s = requestContext.Service().CustomerMessage;

            var fetchCustomerMessageModels = s.GetCustomerMessages(
              request.CustomerId,
               request.ChannelType,
               request.ChannelId,
               request.IncludeMessageTexts ?? false,
               request.SkipCount, request.TakeCount, request.IsHandled, request.IsFromCustomer,
               request.OnlyTheseChannelTypes);

            var r = new Response
            {
                TotalMessageCount = fetchCustomerMessageModels.TotalMessageCount,
                AreMessageTextsIncluded = fetchCustomerMessageModels.AreMessageTextsIncluded,
                Messages = fetchCustomerMessageModels.CustomerMessageModels
                    .ToList()
                    .Select(x => ToCustomerMessageModel(x))
                    .ToList()
            };

            if (request.IncludeChannels.GetValueOrDefault())
            {
                if (!request.CustomerId.HasValue)
                    return Error("IncludeChannels requires CustomerId", errorCode: "includeChannelsWithoutCustomerId");
                var includeGeneral = request.OnlyTheseChannelTypes == null || request.OnlyTheseChannelTypes.Count == 0 || request.OnlyTheseChannelTypes.Contains(CustomerMessageService.GeneralChannelType);
                r.CustomerChannels = s.SortChannels(s.GetCustomerChannels(request.CustomerId.Value, includeGeneral, request.OnlyTheseChannelTypes)).Select(x => new Response.MessageChannelModel
                {
                    ChannelId = x.ChannelId,
                    ChannelType = x.ChannelType,
                    IsRelation = x.IsRelation,
                    CustomerId = x.CustomerId,
                    RelationEndDate = x.RelationEndDate,
                    RelationStartDate = x.RelationStartDate
                }).ToList();
            }

            return r;
        }

        public Response.MessageModel ToCustomerMessageModel(CustomerMessageModel c)
        {
            return new Response.MessageModel
            {
                Id = c.Id,
                Text = c.Text,
                TextFormat = c.TextFormat,
                CustomerId = c.CustomerId,
                ChannelType = c.ChannelType,
                ChannelId = c.ChannelId,
                IsFromCustomer = c.IsFromCustomer,
                CreationDate = c.CreationDate,
                CreatedByUserId = c.CreatedByUserId,
                HandledDate = c.HandledDate,
                HandledByUserId = c.HandledByUserId,
                CustomerMessageAttachedDocuments = c.AttachedDocuments
            };

        }

        public class Request
        {
            public int? CustomerId { get; set; }
            public string ChannelType { get; set; }
            public string ChannelId { get; set; }
            public bool? IncludeMessageTexts { get; set; }
            public int? SkipCount { get; set; }
            public int? TakeCount { get; set; }
            public bool? IncludeChannels { get; set; }
            public bool? IsHandled { get; set; }
            public bool? IsFromCustomer { get; set; }
            public List<string> OnlyTheseChannelTypes { get; set; }
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
                public List<CustomerMessageAttachedDocumentModel> CustomerMessageAttachedDocuments { get; set; }
            }

            public class MessageChannelModel
            {
                public int CustomerId { get; set; }
                public string ChannelType { get; set; }
                public string ChannelId { get; set; }
                public bool IsRelation { get; set; } //General is not
                public DateTime? RelationStartDate { get; set; }
                public DateTime? RelationEndDate { get; set; }
            }

            public int TotalMessageCount { get; set; }
            public bool AreMessageTextsIncluded { get; set; }
            public List<MessageModel> Messages { get; set; }
            public List<MessageChannelModel> CustomerChannels { get; set; }
        }
    }
}