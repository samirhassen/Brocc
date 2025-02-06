using nPreCredit;
using nPreCredit.Code.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.IO;

namespace NTech.Core.PreCredit.Shared.Code.PetrusOnlyScoringService
{
    public class PetrusOnlyScoringServiceFactory
    {
        private readonly IPreCreditEnvSettings envSettings;
        private readonly CachedSettingsService settingsService;
        private readonly IServiceClientSyncConverter serviceClientSyncConverter;
        private readonly IDocumentClient documentClient;
        private readonly IApplicationCommentService commentService;

        public PetrusOnlyScoringServiceFactory(IPreCreditEnvSettings envSettings, CachedSettingsService settingsService, IServiceClientSyncConverter serviceClientSyncConverter,
            IDocumentClient documentClient, IApplicationCommentService commentService)
        {
            this.envSettings = envSettings;
            this.settingsService = settingsService;
            this.serviceClientSyncConverter = serviceClientSyncConverter;
            this.documentClient = documentClient;
            this.commentService = commentService;
        }

        public IPetrusOnlyScoringService OverrideScoringService { get; set; } = null;

        public IPetrusOnlyScoringService GetService()
        {
            if (OverrideScoringService != null)
                return OverrideScoringService;

            var settings = settingsService.LoadSettings("petrusScoringSettings");

            var url = settings.Opt("url");

            if (url.EqualsIgnoreCase("none") || url.EqualsIgnoreCase("mock"))
            {
                if (envSettings.IsProduction)
                    throw new Exception(url.EqualsIgnoreCase("none") ? "Missing url for petrus" : "Using petrus mock is not allowed in production");
                return new MockPetrusOnlyScoringService(commentService, envSettings, documentClient);                
            }

            NTechRotatingLogFile logFile = null;
            if(settings.Opt("isRequestLoggingEnabled") == "true" && envSettings.LogFolder != null)
            {
                var logFolder = Path.Combine(envSettings.LogFolder.FullName, "PetrusRequests");
                logFile = new NTechRotatingLogFile(logFolder, "requests");
            }

            return new RealPetrusOnlyScoringService(
                new Uri(settings.Req("url")),
                settings.Req("username"),
                settings.Req("password"), 
                serviceClientSyncConverter, 
                logFile);
        }
    }
}
