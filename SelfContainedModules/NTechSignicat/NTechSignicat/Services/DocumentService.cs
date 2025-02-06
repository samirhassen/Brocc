using NTech.Services.Infrastructure;
using NTech.Shared.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTechSignicat.Services
{
    public interface IDocumentService
    {
        StoredDocument Store(byte[] documentBytes, string mimeType, TimeSpan? deleteAfter, string documentDownloadName = null, Dictionary<string, string> customData = null);
        StoredDocument Get(string documentKey);
        Uri GetExternalDocumentUri(string documentKey);
    }

    public class DocumentService : IDocumentService
    {
        private readonly IDocumentDatabaseService documentDatabaseService;
        private readonly SignicatSettings signicatSettings;

        const string DocumentKeySpace = "StoredDocumentV1";

        public DocumentService(IDocumentDatabaseService documentDatabaseService, SignicatSettings signicatSettings)
        {
            this.documentDatabaseService = documentDatabaseService;
            this.signicatSettings = signicatSettings;
        }

        public StoredDocument Get(string documentKey)
        {
            return this.documentDatabaseService.Get<StoredDocument>(DocumentKeySpace, documentKey);
        }

        public StoredDocument Store(byte[] documentBytes, string mimeType, TimeSpan? deleteAfter, string documentDownloadName = null, Dictionary<string, string> customData = null)
        {
            var d = new StoredDocument
            {
                DocumentKey = OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken(),
                DocumentDownloadName = documentDownloadName,
                CustomData = customData,
                DocumentDataBase64 = Convert.ToBase64String(documentBytes),
                DocumentMimeType = mimeType
            };
            this.documentDatabaseService.Set(DocumentKeySpace, d.DocumentKey, d, deleteAfter);
            return d;
        }

        public Uri GetExternalDocumentUri(string documentKey)
        {
            return NTechServiceRegistry.CreateUrl(signicatSettings.SelfExternalUrl, $"api/document/{documentKey}");            
        }
    }

    public class StoredDocument
    {
        public string DocumentDataBase64 { get; set; }
        public string DocumentMimeType { get; set; }
        public string DocumentDownloadName { get; set; }
        public string DocumentKey { get; set; }
        public Dictionary<string, string> CustomData { get; set; }
        public byte[] GetDocumentData()
        {
            return Convert.FromBase64String(DocumentDataBase64);
        }
        public string GetCustomDataOpt(string key)
        {
            return CustomData?.GetValueOrDefault(key);
        }
    }
}
