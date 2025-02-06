using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomerPages.Code
{
    public class CustomerLockedCustomerClient : AbstractSystemUserServiceClient
    {
        private int customerId;
        private SystemUserCustomerClient client;
        protected override string ServiceName => "nCustomer";

        public CustomerLockedCustomerClient(int customerId)
        {
            this.customerId = customerId;
            this.client = new SystemUserCustomerClient();
        }

        public class ContactInfoResult
        {
            public AddressResult Address { get; set; }
            public class AddressResult
            {
                public string Street { get; set; }
                public string Zipcode { get; set; }
                public string City { get; set; }
                public string Country { get; set; }
            }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        public ContactInfoResult GetContactInfo()
        {
            var c = this.client.GetCustomerCardItems(
                this.customerId,
                "addressStreet", "addressZipcode", "addressCity", "addressCountry", "email", "phone", "firstName", "lastName");

            var result = new ContactInfoResult
            {
                Email = c.Opt("email"),
                Phone = c.Opt("phone")
            };
            result.Address = new ContactInfoResult.AddressResult
            {
                Street = c.Opt("addressStreet"),
                Zipcode = c.Opt("addressZipcode"),
                City = c.Opt("addressCity"),
                Country = c.Opt("addressCountry")
            };
            result.FirstName = c.Opt("firstName");
            result.LastName = c.Opt("lastName");
            return result;
        }

        public GetMessagesResponse GetMessages(GetMessagesRequest request)
        {
            return Begin()
                .PostJson("Api/CustomerMessage/GetMessages", new
                {
                    CustomerId = customerId,
                    request.IncludeChannels,
                    request.IncludeMessageTexts,
                    request.TakeCount,
                    request.SkipCount,
                    request.OnlyTheseChannelTypes
                })
                .ParseJsonAs<GetMessagesResponse>();
        }

        public class MessageModel
        {
            public int Id { get; set; }
            public string Text { get; set; }
            public string TextFormat { get; set; }
            public bool IsFromCustomer { get; set; }
            public DateTime CreationDate { get; set; }
            public string ChannelId { get; set; }
            public string ChannelType { get; set; }
            public List<CustomerMessageAttachedDocumentModel> CustomerMessageAttachedDocuments { get; set; }
        }

        public class MessageChannelModel
        {
            public string ChannelType { get; set; }
            public string ChannelId { get; set; }
            public bool IsRelation { get; set; } //General is not
        }

        public class GetMessagesResponse
        {
            public int TotalMessageCount { get; set; }
            public List<MessageModel> Messages { get; set; }
            public List<MessageChannelModel> CustomerChannels { get; set; }
        }

        public class CustomerMessageAttachedDocumentModel
        {
            public int Id { get; set; }
            public string FileName { get; set; }
            public string ArchiveKey { get; set; }
            public string ContentTypeMimetype { get; set; }
        }

        public class GetMessagesRequest
        {
            public int? SkipCount { get; set; }
            public int? TakeCount { get; set; }
            public bool? IncludeChannels { get; set; }
            public bool? IncludeMessageTexts { get; set; }
            public List<string> OnlyTheseChannelTypes { get; set; }
            public string ChannelType { get; set; }
            public string ChannelId { get; set; }
        }

        public class SendMessageRequest
        {
            public string Text { get; set; }
            public string TextFormat { get; set; }
            public string ChannelType { get; set; }
            public string ChannelId { get; set; }
        }

        public class AttachMessageDocumentRequest
        {
            public int MessageId { get; set; }
            public string AttachedFileAsDataUrl { get; set; }
            public string AttachedFileName { get; set; }

        }

        public class SendMessageResponse
        {
            public MessageModel CreatedMessage { get; set; }
        }

        public class AttachMessageDocumentResponse
        {
            public int Id { get; set; }

        }
        public SendMessageResponse SendMessage(SendMessageRequest request)
        {
            //Call get messages here with channels included and make sure that the channel the customer specified is included in that list
            //so they cant manipulate the client call to send messages to someone else:s loan/savings account and so on.
            GetMessagesRequest getMessagesRequest = new GetMessagesRequest
            {
                IncludeChannels = true,
                IncludeMessageTexts = false
            };

            var getMessagesResponse = GetMessages(getMessagesRequest);
            if (!getMessagesResponse.CustomerChannels.Any(x => x.ChannelId == request.ChannelId && x.ChannelType == request.ChannelType))
                throw new Exception("ChannelId & ChannelType does not exist on customerid:" + customerId);

            //Ensure that customerid, channeltype and channelid are all included on the call since these are not required on the other end
            if (string.IsNullOrWhiteSpace(request.Text))
                throw new Exception("Text must be included. customerid:" + customerId);

            return Begin()
               .PostJson("Api/CustomerMessage/CreateMessage", new { CustomerId = customerId, ChannelType = request.ChannelType, ChannelId = request.ChannelId, Text = request.Text, TextFormat = request.TextFormat, IsFromCustomer = true })
               .ParseJsonAs<SendMessageResponse>();
        }

        public AttachMessageDocumentResponse AttachMessage(AttachMessageDocumentRequest request)
        {

            if (string.IsNullOrWhiteSpace(request.AttachedFileAsDataUrl))
                throw new Exception("AttachedFileAsDataUrl must be included. MessageId:" + request.MessageId);
            if (string.IsNullOrWhiteSpace(request.AttachedFileName))
                throw new Exception("AttachedFileName must be included. MessageId:" + request.MessageId);

            return Begin()
               .PostJson("Api/CustomerMessage/AttachMessageDocument", new { AttachedFileAsDataUrl = request.AttachedFileAsDataUrl, AttachedFileName = request.AttachedFileName, MessageId = request.MessageId })
               .ParseJsonAs<AttachMessageDocumentResponse>();
        }

        public void MarkMessagesAsReadByCustomer(string readContext, int latestReadMessageId) =>
            Begin()
                .PostJson("Api/CustomerMessage/MarkAsReadByCustomer", new
                {
                    CustomerId = customerId,
                    ReadContext = readContext,
                    LatestReadMessageId = latestReadMessageId
                })
                .EnsureSuccessStatusCode();

        public int GetUnreadByCustomerCount(string readContext, List<string> onlyTheseChannelTypes, string channelType, string channelId) =>
            Begin()
                .PostJson("Api/CustomerMessage/GetUnreadByCustomerCount", new
                {
                    CustomerId = customerId,
                    ReadContext = readContext,
                    OnlyTheseChannelTypes = onlyTheseChannelTypes,
                    ChannelType = channelType,
                    ChannelId = channelId
                })
                .ParseJsonAsAnonymousType(new { UnreadCount = new int?() })
                ?.UnreadCount ?? 0;
    }
}