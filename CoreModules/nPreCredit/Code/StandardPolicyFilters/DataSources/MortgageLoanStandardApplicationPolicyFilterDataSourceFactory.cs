using nPreCredit.Code.Services;
using nPreCredit.Code.Services.SharedStandard;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;

namespace nPreCredit.Code.StandardPolicyFilters.DataSources
{
    public class MortgageLoanStandardApplicationPolicyFilterDataSourceFactory
    {
        public MortgageLoanStandardApplicationPolicyFilterDataSourceFactory(ICoreClock clock, IPreCreditContextFactoryService contextFactoryService,
            LoanApplicationCreditReportService creditReportService, ICustomerClient customerClient, NTech.Core.Module.Shared.Clients.ICreditClient creditClient,
            NTech.Core.Module.Shared.Clients.ICreditReportClient creditReportClient, IClientConfigurationCore clientConfiguration)
        {
            createDataSource = (applicationNr, forceLoadAllVariables, isAllowedToBuyNewCreditReports) => new MortgageLoanStandardApplicationPolicyFilterDataSource(applicationNr, forceLoadAllVariables, isAllowedToBuyNewCreditReports,
                clock, contextFactoryService, creditReportService, customerClient, creditClient, creditReportClient, clientConfiguration);
        }

        private Func<string, bool, bool, IPolicyFilterDataSource> createDataSource;

        public IPolicyFilterDataSource CreateDataSource(string applicationNr, bool forceLoadAllVariables, bool isAllowedToBuyNewCreditReports) => createDataSource(applicationNr, forceLoadAllVariables, isAllowedToBuyNewCreditReports);
    }
}