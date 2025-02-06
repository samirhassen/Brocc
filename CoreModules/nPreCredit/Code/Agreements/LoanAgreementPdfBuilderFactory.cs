using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.Core.PreCredit.Shared;
using NTech.Legacy.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using CoreDocumentClient = NTech.Core.Module.Shared.Clients.IDocumentClient;
namespace nPreCredit.Code.Agreements
{
    public class LoanAgreementPdfBuilderFactory
    {
        private readonly ICombinedClock clock;
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;
        private readonly ICustomerClient customerClient;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly PreCreditContextFactory contextFactory;
        private readonly ILoggingService loggingService;
        private readonly IPreCreditEnvSettings envSettings;
        private readonly CoreDocumentClient documentClient;
        private readonly Dictionary<string, string> finnishTranslations;
        private readonly Func<string, byte[]> loadPdfTemplate;

        public LoanAgreementPdfBuilderFactory(ICombinedClock clock, IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository, ICustomerClient customerClient,
            IClientConfigurationCore clientConfiguration, PreCreditContextFactory contextFactory, ILoggingService loggingService, IPreCreditEnvSettings envSettings,
            CoreDocumentClient documentClient, Dictionary<string, string> finnishTranslations, Func<string, byte[]> loadPdfTemplate)
        {
            this.clock = clock;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
            this.customerClient = customerClient;
            this.clientConfiguration = clientConfiguration;
            this.contextFactory = contextFactory;
            this.loggingService = loggingService;
            this.envSettings = envSettings;
            this.documentClient = documentClient;
            this.finnishTranslations = finnishTranslations;
            this.loadPdfTemplate = loadPdfTemplate;
        }

        public ILoanAgreementPdfBuilder Create(bool isForAdditionalLoan)
        {
            return isForAdditionalLoan
                ? (ILoanAgreementPdfBuilder)new AdditionalLoanAgreementPdfBuilder(clock, partialCreditApplicationModelRepository)
                : new LoanAgreementPdfBuilder(clock, partialCreditApplicationModelRepository, customerClient, clientConfiguration,
                contextFactory, loggingService, envSettings, documentClient, finnishTranslations, loadPdfTemplate);
        }
    }
}