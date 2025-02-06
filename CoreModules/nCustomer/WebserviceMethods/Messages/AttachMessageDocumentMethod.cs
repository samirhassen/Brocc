using nCustomer.Code;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.ComponentModel.DataAnnotations;

namespace nCustomer.WebserviceMethods.Messages
{
    public class AttachMessageDocumentMethod : TypedWebserviceMethod<AttachMessageDocumentMethod.Request, AttachMessageDocumentMethod.Response>
    {
        public override string Path => "CustomerMessage/AttachMessageDocument";

        public override bool IsEnabled => NEnv.IsSecureMessagesEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (string.IsNullOrWhiteSpace(request.AttachedFileAsDataUrl))
            {
                if (string.IsNullOrWhiteSpace(request.AttachedFileArchiveKey))
                    return Error("One of AttachedFileAsDataUrl+AttachedFileName and AttachedFileArchiveKey is required");
            }
            else if (string.IsNullOrWhiteSpace(request.AttachedFileName))
            {
                return Error("AttachedFileAsDataUrl requires AttachedFileName");
            }

            string mimeType = "", attachedFileName = "", attachedFileArchiveDocumentKey = "";
            var client = new DocumentClient();

            if (request.AttachedFileArchiveKey == null)
            {
                var attachedFileDataUrlAndFileName = (!string.IsNullOrWhiteSpace(request.AttachedFileAsDataUrl) && !string.IsNullOrWhiteSpace(request.AttachedFileName))
                ? Tuple.Create(request.AttachedFileAsDataUrl, request.AttachedFileName) : null;

                if (attachedFileDataUrlAndFileName != null)
                {
                    var attachedFileAsDataUrl = attachedFileDataUrlAndFileName.Item1;
                    attachedFileName = attachedFileDataUrlAndFileName.Item2;
                    byte[] fileData;

                    if (!FileUtilities.TryParseDataUrl(attachedFileAsDataUrl, out mimeType, out fileData))
                    {
                        return Error($"Invalid attached file");
                    }
                    attachedFileArchiveDocumentKey = client.ArchiveStore(fileData, mimeType, attachedFileName);
                }
            }
            else
            {
                var metaData = client.FetchMetadata(request.AttachedFileArchiveKey);
                mimeType = metaData.ContentType;
                attachedFileName = metaData.FileName;
                attachedFileArchiveDocumentKey = request.AttachedFileArchiveKey;
            }

            var s = requestContext.Service().CustomerMessage;

            var fetchCustomerMessageAttachedDocumentModels = s.SaveCustomerMessageAttachedDocument(
              request.MessageId,
               attachedFileName,
               mimeType,
               attachedFileArchiveDocumentKey);

            var r = new Response
            {
                Id = fetchCustomerMessageAttachedDocumentModels.Id
            };

            return r;
        }

        public class Request
        {
            [Required]
            public int MessageId { get; set; }
            public string AttachedFileAsDataUrl { get; set; }
            public string AttachedFileName { get; set; }
            public string AttachedFileArchiveKey { get; set; }
        }

        public class Response
        {
            public int Id { get; set; }

        }
    }
}