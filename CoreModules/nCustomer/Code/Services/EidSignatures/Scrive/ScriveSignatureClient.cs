using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace nCustomer.Services.EidSignatures.Scrive
{
    public class ScriveSignatureClient : ScriveSignatureClientBase
    {
        public ScriveSignatureClient(NTechSimpleSettings settings) : base(settings)
        {

        }

        public Uri ApiEndpoint => this.apiEndpoint;

        public byte[] DownloadSignedPdf(string documentId)
        {
            byte[] pdfData = null;
            PostOrGetSignature(
              $"api/v2/documents/{documentId}/files/main", null, HttpMethod.Get, downloadFile: x => pdfData = x);
            return pdfData;
        }

        public EditableScriveDocument NewDocument(byte[] pdfData, string pdfFileName)
        {
            var content = CreateMultipartFormDataContent(
                nakedStrings: new Dictionary<string, string> { { "saved", "true" } }, //true means it shows up in the ui
                file: (Data: pdfData, FileName: pdfFileName, Name: "file", ContentType: "application/pdf"));
            var rawDocument = PostOrGetSignature("api/v2/documents/new", content, HttpMethod.Post);
            return new EditableScriveDocument(rawDocument);
        }

        public EditableScriveDocument CancelDocument(string id)
        {
            var content = CreateMultipartFormDataContent();
            var rawDocument = PostOrGetSignature($"api/v2/documents/{id}/cancel", content, HttpMethod.Post);
            return new EditableScriveDocument(rawDocument);
        }

        public EditableScriveDocument UpdateDocument(EditableScriveDocument document)
        {
            var content = CreateMultipartFormDataContent(jsonItem: (Data: document.RawDocument, Name: "document"));
            var rawDocument = PostOrGetSignature($"api/v2/documents/{document.GetId()}/update", content, HttpMethod.Post);
            return new EditableScriveDocument(rawDocument);
        }

        public EditableScriveDocument GetDocument(string id)
        {
            var rawDocument = PostOrGetSignature($"api/v2/documents/{id}/get", null, HttpMethod.Get);
            return new EditableScriveDocument(rawDocument);
        }

        public EditableScriveDocument StartSignatureProcess(string id)
        {
            var content = CreateMultipartFormDataContent(nakedStrings: new Dictionary<string, string> { { "strict_validations", "true" } });
            var rawDocument = PostOrGetSignature($"api/v2/documents/{id}/start", content, HttpMethod.Post);
            return new EditableScriveDocument(rawDocument);
        }

        public void TriggerApiCallback(string id)
        {
            PostOrGetSignature($"/api/v2/documents/{id}/callback", null, HttpMethod.Post, skipParsingResponse: true);
        }
    }
}