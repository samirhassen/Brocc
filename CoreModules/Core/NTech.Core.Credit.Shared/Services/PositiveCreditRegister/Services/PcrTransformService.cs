using nCredit;
using NTech.Banking.CivicRegNumbers.Fi;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Models.NewOrChangedLoansRequestModel;

namespace NTech.Core.Credit.Shared.Services.PositiveCreditRegister.Services
{
    public class PcrTransformService
    {
        private readonly ICustomerClient customerClient;
        private readonly INTechEnvironment environment;
        private readonly ICreditEnvSettings envSettings;

        public PcrTransformService(ICustomerClient customerClient, INTechEnvironment environment, ICreditEnvSettings envSettings)
        {
            this.customerClient = customerClient;
            this.environment = environment;
            this.envSettings = envSettings;
        }

        public Dictionary<int, string> GetCivicRegNrsByCustomerId(HashSet<int> customerIds)
        {
            var customerProperties = customerClient.BulkFetchPropertiesByCustomerIdsD(customerIds, "civicRegNr");
            customerProperties = ReplaceWithPcrTestPersons(customerIds, customerProperties);
            return customerProperties
                .Where(x => x.Value.ContainsKey("civicRegNr"))
                .ToDictionary(x => x.Key, x => x.Value.Req("civicRegNr"));
        }

        private Dictionary<int, Dictionary<string, string>> ReplaceWithPcrTestPersons(HashSet<int> customerIds, Dictionary<int, Dictionary<string, string>> customerProperties)
        {
            if (envSettings.IsProduction)
                return customerProperties;

            var pcrSettings = envSettings.PositiveCreditRegisterSettings;
            if (!pcrSettings.UsePcrTestCivicRegNrs)
                return customerProperties;

            //File with 1 civicregnr per line. We just cycle these. The testset is small. 500 at the time of writing so
            //we will be assigning the same civicregnr to loans that actually have different civicregnrs in our system.
            var testPersonsFile = environment.StaticResourceFile("ntech.credit.pcrtestpersonsfile", "positive-credit-register-testpersons.txt", true);

            var pcrCivicRegNrs = File.ReadAllLines(testPersonsFile.FullName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Where(x => CivicRegNumberFi.IsValid(x)) //They have a bunch of invalid ones that their own api rejects ... no clue why they would include these
                .ToList();

            return customerIds.ToDictionary(
                x => x, 
                x => new Dictionary<string, string> { ["civicRegNr"] = pcrCivicRegNrs[x % pcrCivicRegNrs.Count] });
        }

        /// <summary>
        /// PCRs testenvironment cannot be reset so we use this to allow retesting with the same loans.
        /// </summary>
        public string TransformLoanNr(string loanNr)
        {
            if (envSettings.IsProduction)
                return loanNr;

            var pcrSettings = envSettings.PositiveCreditRegisterSettings;

            return pcrSettings.CreditNrTestSuffix == null ? loanNr : $"{loanNr}{pcrSettings.CreditNrTestSuffix}";
        }

        public void FixLumpSumLoan(LumpSumLoan loan)
        {
            if (loan == null)
                return;

            //Prevent error code D02 The AmountPaid may not be more than the AmountIssued.
            if (loan.AmountPaid > loan.AmountIssued)
                loan.AmountIssued = loan.AmountPaid;            
        }
    }
}
