using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class ApplicationCheckpointService
    {
        private readonly ApplicationInfoService applicationInfoService;
        private readonly ICustomerClient customerClient;
        private readonly IClientConfigurationCore clientConfiguration;

        public const string FeatureName = "ntech.feature.customercheckpoints";
        public const string ApplicationCheckpointCode = "CreditApplicationCheckpoint";

        public ApplicationCheckpointService(ApplicationInfoService applicationInfoService,
            ICustomerClient customerClient,
            IClientConfigurationCore clientConfiguration)
        {
            this.applicationInfoService = applicationInfoService;
            this.customerClient = customerClient;
            this.clientConfiguration = clientConfiguration;
        }

        public static bool IsEnabled(IClientConfigurationCore clientConfiguration) => clientConfiguration.IsFeatureEnabled(FeatureName);

        public IList<CheckpointJsModel> GetCheckpointsForApplication(string applicationNr)
        {
            if (!IsEnabled(clientConfiguration))
                return new List<CheckpointJsModel>();

            return GetCustomerCheckpoints(GetApplicationCustomers(applicationNr));
        }

        public string GetReasonText(int checkpointId)
        {
            return customerClient.FetchCheckpointReasonText(checkpointId);
        }

        public bool DoesAnyApplicationHaveAnActiveCheckpoint(string applicationNr)
        {
            if (!IsEnabled(clientConfiguration))
                return false;
            return GetCustomerCheckpoints(GetApplicationCustomers(applicationNr)).Any();
        }

        public static HashSet<int> GetCustomersWithActiveCheckpoints(HashSet<int> customerIds, IClientConfigurationCore clientConfiguration, ICustomerClient customerClient)
        {
            if (!IsEnabled(clientConfiguration))
                return new HashSet<int>();

            var checkpointResult = customerClient.GetActiveCheckpointIdsOnCustomerIds(
                new HashSet<int>(customerIds), new List<string> { ApplicationCheckpointCode });
            return new HashSet<int>(checkpointResult.CheckPointByCustomerId.Keys);
        }

        private IList<CheckpointJsModel> GetCustomerCheckpoints(params CheckpointCustomer[] customers)
        {
            var customerIds = customers.Select(x => x.CustomerId).ToHashSetShared();
            var checkPointsIdsByCustomerId = customerClient.GetActiveCheckpointIdsOnCustomerIds(customerIds, new List<string> { ApplicationCheckpointCode }).CheckPointByCustomerId;

            return customers
                .Where(x => checkPointsIdsByCustomerId.ContainsKey(x.CustomerId))
                .Select(x => new CheckpointJsModel
                {
                    applicantNr = x.ApplicantNr,
                    checkpointId = checkPointsIdsByCustomerId[x.CustomerId].CheckPointId,
                    checkpointUrl = checkPointsIdsByCustomerId[x.CustomerId].CheckpointUrl,
                    customerId = x.CustomerId,
                    customerRoleCode = x.CustomerRoleCode,
                    allCustomerRoleCodes = x.AllCustomerRoleCodes
                })
                .ToList();
        }

        private CheckpointCustomer[] GetApplicationCustomers(string applicationNr)
        {
            var applicants = applicationInfoService.GetApplicationApplicants(applicationNr);

            int? GetApplicantNrByCustomerId(int customerId)
            {
                return applicants
                    .CustomerIdByApplicantNr
                    .Select(x => new { ApplicantNr = x.Key, CustomerId = x.Value })
                    .Where(x => x.CustomerId == customerId)
                    .FirstOrDefault()?.ApplicantNr;
            }
            return applicants.AllConnectedCustomerIdsWithRoles.Select(x => new CheckpointCustomer
            {
                AllCustomerRoleCodes = x.Value.ToList(),
                ApplicantNr = GetApplicantNrByCustomerId(x.Key),
                CustomerId = x.Key,
                CustomerRoleCode = x.Value.FirstOrDefault() ?? "Applicant"
            }).ToArray();
        }

        private class CompanyLoanCustomersData : PartialCreditApplicationModelExtendedCustomDataBase
        {
            public IEnumerable<ListCustomer> ListCustomers { get; set; }

            public static CompanyLoanCustomersData Load(string applicationNr, IPreCreditContextExtended context)
            {
                return context
                    .CreditApplicationHeadersQueryable
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new CompanyLoanCustomersData
                    {
                        NrOfApplicants = x.NrOfApplicants,
                        ListCustomers = x.CustomerListMemberships.Select(y => new ListCustomer
                        {
                            ListName = y.ListName,
                            CustomerId = y.CustomerId
                        })
                    })
                    .Single();
            }

            public class ListCustomer
            {
                public int CustomerId { get; set; }
                public string ListName { get; set; }
            }
        }

        private class CustomerIdData
        {
            public string CustomerIdRaw { get; set; }
            public string GroupName { get; set; }
        }
    }

    public class CheckpointCustomer
    {
        public int CustomerId { get; set; }
        public string CustomerRoleCode { get; set; }
        public List<string> AllCustomerRoleCodes { get; set; }
        public int? ApplicantNr { get; set; }
    }

    public class CheckpointJsModel
    {
        public int? applicantNr { get; set; }
        public string customerRoleCode { get; set; }
        public List<string> allCustomerRoleCodes { get; set; }
        public int customerId { get; set; }
        public int checkpointId { get; set; }
        public string checkpointUrl { get; set; }
    }
}