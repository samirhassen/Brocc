using Microsoft.Extensions.DependencyInjection;
using Moq;
using nCredit.Code;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.DbModel.BusinessEvents.NewCredit;
using nCredit.DbModel.DomainModel;
using nCredit.Excel;
using nPreCredit.Code;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Credit.Shared.Services.PositiveCreditRegister;
using NTech.Core.Credit.Shared.Services.SwedishMortgageLoans;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.TestSupport;
using static NTech.Core.Host.IntegrationTests.Credits;

namespace NTech.Core.Host.IntegrationTests.Shared.Services
{
    internal static class SharedCredit
    {        
        public static void RegisterServices(ISupportSharedCredit support, ServiceCollection services, Func<ServiceProvider> getProvider)
        {
            var supportShared = (support as SupportShared)!;

            var documentId = 1;
            var documentClient = new Mock<IDocumentClient>(MockBehavior.Strict);
            documentClient.Setup(x => x.ArchiveStore(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>())).Returns(() => $"test-{documentId++}");
            documentClient.Setup(x => x.CreateXlsxToArchive(It.IsAny<DocumentClientExcelRequest>(), It.IsAny<string>())).Returns(() => $"test-excel-{documentId++}");
            services.AddTransient<IDocumentClient>(_ => documentClient.Object);
            services.AddTransient(_ => support.CreateCreditContextFactory());
            services.AddTransient<MortageLoanOwnerManagementService>();
            services.AddTransient(_ => support.CreditEnvSettings);
            services.AddTransient<AnnexTwoEsmaReportService>();
            services.AddTransient<AnnexTwelveEsmaReportService>();
            services.AddTransient(_ => support.GetNotificationProcessSettingsFactory() as INotificationProcessSettingsFactory);
            services.AddTransient<MortgageLoanCollateralService>();
            services.AddTransient<MlStandardSeRevaluationService>();
            services.AddTransient<FundOwnerReportService>();
            services.AddTransient<IKeyValueStoreService>(x => new KeyValueStoreService(() => support.CreateCreditContextFactory().CreateContext()));
            services.AddTransient<AlternatePaymentPlanService>();
            services.AddTransient<ISecureMessageService>(_ =>
            {
                var s = new Mock<ISecureMessageService>(MockBehavior.Loose);
                return s.Object;
            });
            services.AddTransient<PaymentAccountService>();
            services.AddTransient<CreditTerminationLettersInactivationBusinessEventManager>();
            services.AddTransient<AlternatePaymentPlanSecureMessagesService>();
            services.AddTransient<NewCreditBusinessEventManager>();
            services.AddTransient<LegalInterestCeilingService>();
            services.AddTransient<ICreditCustomerListServiceComposable, CreditCustomerListServiceComposable>();
            services.AddTransient<IOcrPaymentReferenceGenerator, OcrPaymentReferenceGenerator>();
            services.AddTransient<NotificationService>();
            services.AddTransient<ISnailMailLoanDeliveryService, TestSnailMailLoanDeliveryService>();
            services.AddSingleton<NotificationRenderer>();
            services.AddSingleton<INotificationDocumentBatchRenderer>(x => x.GetRequiredService<NotificationRenderer>());
            services.AddSingleton<PaymentOrderAndCostTypeCache>(x => new PaymentOrderAndCostTypeCache());
            services.AddSingleton<PaymentOrderService>(x => new PaymentOrderService(support.CreateCreditContextFactory(), 
                x.GetRequiredService<CustomCostTypeService>(),
                x.GetRequiredService<PaymentOrderAndCostTypeCache>(), supportShared.ClientConfiguration));
            services.AddSingleton<CustomCostTypeService>(x => new CustomCostTypeService(support.CreateCreditContextFactory(), x.GetRequiredService<PaymentOrderAndCostTypeCache>()));
            services.AddTransient(x =>
            {
                var dc = new Mock<IDocumentClient>(MockBehavior.Strict);
                dc
                    .Setup(x => x.ArchiveStore(It.IsAny<byte[]>(), "application/zip", It.IsAny<string>()))
                    .Returns("someid123.zip");
                var pc = supportShared.CreatePaymentAccountService(support.CreditEnvSettings);
                return new DirectDebitNotificationDeliveryService(dc.Object, pc, support.CreateCreditContextFactory(), support.CreditEnvSettings,
                    supportShared.GetRequiredService<PaymentOrderService>(), supportShared.ClientConfiguration);                
            });
            services.AddTransient<ICreditClient, DatabaseCreditClient>();
            services.AddTransient<MortgageLoansCreditTermsChangeBusinessEventManager>();
            services.AddTransient(x =>
            {
                var documentClient = new Mock<IDocumentClient>(MockBehavior.Strict);
                documentClient
                    .Setup(x => x.TryFetchRaw(It.IsAny<string>()))
                    .Returns<string>(archiveKey =>
                    {
                        if (archiveKey.EndsWith(".pdf"))
                            return (IsSuccess: true, ContentType: "application/pdf", FileName: archiveKey, FileData: TestPdfs.GetMinimalPdfBytes($"Test: {archiveKey}"));
                        else
                            throw new NotImplementedException();
                    });
                documentClient
                    .Setup(x => x.ArchiveStoreFile(It.IsAny<FileInfo>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<FileInfo, string, string>((_, __, ___) => Guid.NewGuid().ToString().Replace("-", "") + ".pdf");
                return new ZippedPdfsLoanDeliveryService(documentClient.Object, support.CreateCreditContextFactory(), support.CreditEnvSettings, supportShared.ClientConfiguration);
            });
            services.AddTransient<NewOutgoingPaymentFileBusinessEventManager>();
            services.AddTransient<MultiCreditPlacePaymentBusinessEventManager>();
            services.AddTransient<SwedishMortageLoanCreationService>();
            services.AddTransient<NewMortgageLoanBusinessEventManager>();
            services.AddTransient<OcrPaymentReferenceGenerator>();
            services.AddTransient<CustomerRelationsMergeService>();
            services.AddTransient<CreditSettlementSuggestionBusinessEventManager>();
            services.AddTransient<SwedishMortgageLoanRseService>();
            services.AddTransient<RepayPaymentBusinessEventManager>();
            services.AddTransient<TerminationLetterCandidateService>();
            services.AddTransient<DebtCollectionCandidateService>();
            services.AddTransient<TerminationLetterInactivationService>();
            services.AddTransient<NewCreditTerminationLettersBusinessEventManager>();
            services.AddTransient<AmortizationPlanService>();
            services.AddTransient<PositiveCreditRegisterExportService>();
        }
    }
}
