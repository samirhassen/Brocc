using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;
using static nCustomer.WebserviceMethods.Messages.GetCustomerMessagesByChannelMethod.Response;

namespace nCustomer.WebserviceMethods.Messages
{
    public class GetCustomerMessagesByChannelMethod : TypedWebserviceMethod<GetCustomerMessagesByChannelMethod.Request, GetCustomerMessagesByChannelMethod.Response>
    {
        public override string Path => "CustomerMessage/GetCustomerMessagesByChannel";

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
               null);

            var r = new Response
            {
                TotalMessageCount = fetchCustomerMessageModels.TotalMessageCount,
                AreMessageTextsIncluded = fetchCustomerMessageModels.AreMessageTextsIncluded,
                GroupedMessages = fetchCustomerMessageModels.CustomerMessageModels
                .GroupBy(y => new { ChannelType = y.ChannelType, ChannelId = y.ChannelId, y.CustomerId })
                .Select(x => new MessageModel
                {

                    ChannelType = x.Key.ChannelType,
                    ChannelId = x.Key.ChannelId,
                    CreationDate = x.OrderBy(y => y.CreationDate).Select(y => y.CreationDate).First(),
                    CustomerId = x.Key.CustomerId
                })
                .ToList()

            };

            return r;
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
        }

        public class Response
        {
            public class MessageModel
            {

                public DateTime CreationDate { get; set; }

                public string ChannelType { get; set; }
                public string ChannelId { get; set; }
                public int CustomerId { get; set; }
            }

            public int TotalMessageCount { get; set; }
            public bool AreMessageTextsIncluded { get; set; }
            public List<MessageModel> GroupedMessages { get; set; }
        }
    }
}