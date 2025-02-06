using nCustomer.Code.Services;
using NTech.Banking.Conversion;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCustomer.WebserviceMethods.Messages
{
    public class FindCustomerChannelsMethod : TypedWebserviceMethod<FindCustomerChannelsMethod.Request, FindCustomerChannelsMethod.Response>
    {
        public override string Path => "CustomerMessage/FindCustomerChannels";

        public override bool IsEnabled => NEnv.IsSecureMessagesEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            CustomerMessageChannelSearchTypeCode? searchTypeCode;
            if (string.IsNullOrWhiteSpace(request.SearchType))
                searchTypeCode = CustomerMessageChannelSearchTypeCode.Omni;
            else
                searchTypeCode = Enums.Parse<CustomerMessageChannelSearchTypeCode>(request.SearchType);

            if (!searchTypeCode.HasValue)
            {
                return Error($"Invalid SearchType. Must be one of: {string.Join(", ", Enums.GetAllValues<CustomerMessageChannelSearchTypeCode>().Select(x => x.ToString()))}", errorCode: "invalidSearchType");
            }
            var s = requestContext.Service().CustomerMessage;
            var result = s.FindChannels(searchTypeCode.Value, request.SearchText, requestContext.CurrentUserMetadata(), request.IncludeGeneralChannel.GetValueOrDefault());

            return new Response
            {
                CustomerChannels = s.SortChannels(result).Select(x => new Response.MessageChannelModel
                {
                    ChannelId = x.ChannelId,
                    ChannelType = x.ChannelType,
                    CustomerId = x.CustomerId,
                    IsRelation = x.IsRelation,
                    RelationEndDate = x.RelationEndDate,
                    RelationStartDate = x.RelationStartDate
                }).ToList()
            };
        }

        public class Request
        {
            [Required]
            public string SearchText { get; set; }
            public string SearchType { get; set; }
            public bool? IncludeGeneralChannel { get; set; }
        }

        public enum SearchTypeCode
        {
            Omni,
            Email,
            CustomerName,
            OrgOrCivicRegNr,
            RelationId
        }

        public class Response
        {
            public class MessageChannelModel
            {
                public int CustomerId { get; set; }
                public string ChannelType { get; set; }
                public string ChannelId { get; set; }
                public bool IsRelation { get; set; } //General is not
                public DateTime? RelationStartDate { get; set; }
                public DateTime? RelationEndDate { get; set; }
            }
            public List<MessageChannelModel> CustomerChannels { get; set; }
        }
    }
}