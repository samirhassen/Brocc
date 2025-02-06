using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nCredit;
using nCredit.Code;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.DbModel.DomainModel;
using NTech.Core.Credit.Database;
using NTech.Core.Credit.Services;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Models;
using NTech.Core.Credit.Shared.Repository;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Credit.Shared.Services.PositiveCreditRegister;
using NTech.Core.Credit.Shared.Services.SwedishMortgageLoans;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System.Reflection;

namespace NTech.Core.Credit
{
    public class CreditNTechModule : NTechModule
    {
        public override string ServiceName => "nCredit";

        public override void AddServices(IServiceCollection services, NEnv env)
        {
            services.AddScoped<ICreditEnvSettings, CreditEnvSettings>();
            services.AddScoped(x =>
            {
                var user = x.GetRequiredService<INTechCurrentUserMetadata>();
                var clock = x.GetRequiredService<ICoreClock>();
                return new CreditContextFactory(() => new CreditContextExtended(user, clock));
            });
            services.AddScoped<CreditSearchService>();
            services.AddScoped<CreditCustomerSearchSourceService>();
            services.AddScoped<TerminationLetterInactivationService>();
            services.AddScoped<CreditTerminationLettersInactivationBusinessEventManager>();
            services.AddScoped<ICreditCustomerListServiceComposable, CreditCustomerListServiceComposable>();
            services.AddScoped<NewMortgageLoanBusinessEventManager>();
            services.AddScoped<SwedishMortageLoanCreationService>();
            services.AddScoped<OcrPaymentReferenceGenerator>();
            services.AddScoped<MortgageLoanCollateralService>();
            services.AddScoped<CreditCommentService>();
            services.AddScoped<MlStandardSeRevaluationService>();
            services.AddScoped<MortageLoanOwnerManagementService>();
            services.AddScoped<CustomerRelationsMergeService>();
            services.AddScoped<BoundInterestExpirationReminderService>();
            services.AddScoped<PositiveCreditRegisterExportService>();
            services.AddScoped<PcrLoggingService>();
            services.AddScoped<INotificationProcessSettingsFactory, NotificationProcessSettingsFactoryCore>();
            services.AddScoped<AnnexTwoEsmaReportService>();
            services.AddScoped<AnnexTwelveEsmaReportService>();
            services.AddScoped<FundOwnerReportService>();
            services.AddScoped<CustomerCreditHistoryCoreRepository>();
            services.AddScoped<SwedishMortgageLoanImportService>();
            services.AddScoped<ChangeCreditCustomersService>();
            services.AddScoped<AlternatePaymentPlanService>();
            services.AddScoped<BookkeepingReconciliationReportService>();
            services.AddScoped<IKeyValueStoreService, KeyValueStoreService>(x =>
                new KeyValueStoreService(x.GetRequiredService<CreditContextFactory>().CreateContext));
            services.AddScoped<AmortizationPlanService>();
            services.AddScoped<PaymentOrderAndCostTypeCache>();
            services.AddScoped<PaymentOrderService>();
            services.AddScoped<CustomCostTypeService>();          
            services.AddScoped<MultiCreditPlacePaymentBusinessEventManager>();
            services.AddScoped<PaymentAccountService>();
            services.AddScoped<PaymentFileImportService>();
            services.AddScoped<CreditSettlementSuggestionBusinessEventManager>();
            services.AddScoped<SwedishMortgageLoanRseService>();
            services.AddScoped<RepayPaymentBusinessEventManager>();
            services.AddScoped<NewCreditTerminationLettersBusinessEventManager>();
            services.AddScoped<TerminationLetterCandidateService>();
            services.AddScoped<DebtCollectionCandidateService>();
            services.AddScoped<AlternatePaymentPlanReportsService>();
            services.AddTransient<LoanSettledSecureMessageService>();
        }

        public override void OnApplicationStarted(ILogger logger)
        {

        }

        public override List<Assembly> ExtraDocumentationAssemblies => new List<Assembly> 
        { 
            //Add NTech.Core.Credit.Shared
            typeof(FromDateToDateReportRequest).Assembly 
        };
    }
}