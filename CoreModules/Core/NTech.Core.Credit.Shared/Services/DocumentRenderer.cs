using Newtonsoft.Json;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;

namespace nCredit.Code.Services
{

    public class DocumentRenderer : IDocumentRenderer, IDisposable
    {
        private readonly IDocumentClient documentClient;
        private readonly bool useDelayedDocuments;
        private readonly ICreditEnvSettings envSettings;
        private readonly ILoggingService loggingService;
        private readonly PdfTemplateReader pdfTemplateReader;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly Dictionary<string, string> batchIdByTemplateName;
        private Lazy<string> contextLogFolder;

        public DocumentRenderer(IDocumentClient documentClient, bool useDelayedDocuments, ICreditEnvSettings envSettings, ILoggingService loggingService, 
            PdfTemplateReader pdfTemplateReader, IClientConfigurationCore clientConfiguration)
        {
            this.documentClient = documentClient;
            this.useDelayedDocuments = useDelayedDocuments;
            this.envSettings = envSettings;
            this.loggingService = loggingService;
            this.pdfTemplateReader = pdfTemplateReader;
            this.clientConfiguration = clientConfiguration;
            this.batchIdByTemplateName = new Dictionary<string, string>();
            this.contextLogFolder = new Lazy<string>(() =>
            {
                var f = envSettings.TemplatePrintContextLogFolder;
                if (f == null)
                    return null;
                System.IO.Directory.CreateDirectory(f);
                return f;
            });
        }

        public string RenderDocumentToArchive(string templateName, IDictionary<string, object> context, string archiveFilename)
        {
            if (contextLogFolder.Value != null)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(contextLogFolder.Value, $"{DateTimeOffset.Now.ToString("yyyyMMddHHmmSS")}_{archiveFilename}_{templateName}.json"), JsonConvert.SerializeObject(context));
            }

            if (!batchIdByTemplateName.ContainsKey(templateName))
            {
                if (useDelayedDocuments)
                    batchIdByTemplateName[templateName] = documentClient.BatchRenderDelayedBegin(GetPdfTemplate(templateName));
                else
                    batchIdByTemplateName[templateName] = documentClient.BatchRenderBegin(GetPdfTemplate(templateName));
            }

            var batchId = batchIdByTemplateName[templateName];

            if (useDelayedDocuments)
                return documentClient.BatchRenderDelayedDocumentToArchive(batchId, archiveFilename, context);
            else
                return documentClient.BatchRenderDocumentToArchive(batchId, archiveFilename, context);
        }

        private byte[] GetPdfTemplate(string templateName) => 
            pdfTemplateReader.GetPdfTemplate(templateName, clientConfiguration.Country.BaseCountry, envSettings.IsTemplateCacheDisabled);

        public void Dispose()
        {
            try
            {
                foreach (var batchId in batchIdByTemplateName.Values)
                {
                    if (useDelayedDocuments)
                        documentClient.BatchRenderDelayedEnd(batchId);
                    else
                        documentClient.BatchRenderEnd(batchId);
                }
            }
            catch (Exception ex)
            {
                loggingService.Error(ex, "Failed to cleanup document render batches. Just restart nDocument if the system slows down.");
            }
            batchIdByTemplateName.Clear();
        }
    }
}