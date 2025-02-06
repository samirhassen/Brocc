using nPreCredit.Code.Services;
using nPreCredit.Code.Services.SharedStandard;
using nPreCredit.Code.Services.UnsecuredLoans;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;

namespace nPreCredit.Code.StandardPolicyFilters.DataSources
{
    /// <summary>
    /// - So the datasource cant take an applicationnr as parameter to load or the scoring engine will be tied to the data directly which we dont want
    /// - This means we cant register UnsecuredLoanStandardApplicationPolicyFilterDataSource for DI since applicationNr cannot be set
    /// - Thus this factory class which is not great by slightly better than filling in all the parameters by hand
    /// - An alternative would be to have a public ApplicationNr property on UnsecuredLoanStandardApplicationPolicyFilterDataSource
    ///   that is set before use but that is a bit spooky used with injection since it opens up the possibility of leaks between calls
    /// </summary>
    public class UnsecuredLoanStandardApplicationPolicyFilterDataSourceFactory
    {
        public UnsecuredLoanStandardApplicationPolicyFilterDataSourceFactory(UnsecuredLoanLtlAndDbrService ltlAndDbrService,
            ICoreClock clock, LoanApplicationCreditReportService creditReportService,
            IPreCreditContextFactoryService contextFactoryService, ICustomerClient customerClient, NTech.Core.Module.Shared.Clients.ICreditClient creditClient,
            NTech.Core.Module.Shared.Clients.ICreditReportClient creditReportClient, IClientConfigurationCore clientConfiguration, ILtlDataTables ltlDataTables)
        {
            //Func here to avoid having to declare instance variables for everything
            createDataSource = (applicationNr, forceLoadAllVariables, isAllowedToBuyNewCreditReports) => new UnsecuredLoanStandardApplicationPolicyFilterDataSource(
                applicationNr, forceLoadAllVariables, isAllowedToBuyNewCreditReports,
                ltlAndDbrService, clock, creditReportService,
            contextFactoryService, customerClient, creditClient,
            creditReportClient, clientConfiguration, ltlDataTables);
        }

        private Func<string, bool, bool, IPolicyFilterDataSource> createDataSource;

        public IPolicyFilterDataSource CreateDataSource(string applicationNr, bool forceLoadAllVariables, bool isAllowedToBuyNewCreditReports) => createDataSource(applicationNr, forceLoadAllVariables, isAllowedToBuyNewCreditReports);
    }
}