using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class OtherApplicationsService : IOtherApplicationsService
    {
        private readonly IServiceRegistryUrlService serviceRegistryUrlService;
        private readonly IHttpContextUrlService httpContextUrlService;
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;
        private readonly ICustomerServiceRepository customerServiceRepository;
        private readonly ICreditClient creditClient;

        public OtherApplicationsService(
            IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository,
            ICustomerServiceRepository customerServiceRepository,
            ICreditClient creditClient,
            IServiceRegistryUrlService serviceRegistryUrlService,
            IHttpContextUrlService httpContextUrlService)
        {

            this.serviceRegistryUrlService = serviceRegistryUrlService;
            this.httpContextUrlService = httpContextUrlService;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
            this.customerServiceRepository = customerServiceRepository;
            this.creditClient = creditClient;
        }

        public OtherApplicationsModel FetchByCustomerIds(int[] customerIds, string applicationNr, bool includeApplicationObjects = false)
        {
            var application = partialCreditApplicationModelRepository.Get(applicationNr, new PartialCreditApplicationModelRequest
            {
                ApplicantFields = new List<string> { "customerId" },
                ErrorIfGetNonLoadedField = true
            });

            var customerIdsByComplexApplicationList = customerServiceRepository.GetComplexApplicationListCustomerIds(applicationNr);
            var applicationsByCustomerId = customerServiceRepository.FindByCustomerIds(customerIdsByComplexApplicationList);

            if (includeApplicationObjects)
            {
                var applicationObjectsByCustIds = customerServiceRepository.FindApplicationObjectsByCustomerIds(customerIdsByComplexApplicationList);
                applicationsByCustomerId = applicationObjectsByCustIds.Concat(applicationsByCustomerId)
                    .ToLookup(x => x.Key, x => x.Value)
                    .ToDictionary(x => x.Key, g => g.First());
            }

            var customerCreditHistories = creditClient.GetCustomerCreditHistory(customerIdsByComplexApplicationList.ToList());

            var m = new OtherApplicationsModel
            {
                ApplicationNr = applicationNr,
                Applicants = new List<OtherApplicationsModel.ApplicantModel>()
            };

            foreach (var customerId in customerIds)
            {
                var credits = customerCreditHistories
                    .Where(y => y.CustomerIds.Contains(customerId))
                    .Select(x => new OtherApplicationsModel.CreditModel
                    {
                        ApplicationNr = x.ApplicationNr,
                        Balance = x.CapitalBalance,
                        CreditNr = x.CreditNr,
                        CreditStatus = x.Status,
                        CustomerIds = x.CustomerIds,
                    })
                    .ToList();
                var applications = applicationsByCustomerId
                    .Opt(customerId)
                    ?.Where(x => x.ApplicationNr != applicationNr && !credits.Any(y => y.CreditNr == x.CreditNr))
                    ?.Select(x => new OtherApplicationsModel.ApplicationModel
                    {
                        ApplicationNr = x.ApplicationNr,
                        ApplicationDate = x.ApplicationDate,
                        IsActive = x.IsActive,
                        IsMortgageLoanApplication = x.IsMortgageLoanApplication
                    })
                    ?.ToList() ?? new List<OtherApplicationsModel.ApplicationModel>();
                m.Applicants.Add(new OtherApplicationsModel.ApplicantModel
                {
                    ApplicantNr = 0,
                    CustomerId = customerId,
                    Applications = applications,
                    Credits = credits
                });
            };
            return m;
        }


        public OtherApplicationsModel Fetch(string applicationNr)
        {
            var application = partialCreditApplicationModelRepository.Get(applicationNr, new PartialCreditApplicationModelRequest
            {
                ApplicantFields = new List<string> { "customerId" },
                ErrorIfGetNonLoadedField = true
            });

            var customerIdByApplicantNr = new Dictionary<int, int>();
            application.DoForEachApplicant(applicantNr =>
            {
                customerIdByApplicantNr[applicantNr] = application.Applicant(applicantNr).Get("customerId").IntValue.Required;
            });

            var applicationsByCustomerId = customerServiceRepository.FindByCustomerIds(customerIdByApplicantNr.Values.ToArray());
            var customerCreditHistories = creditClient.GetCustomerCreditHistory(customerIdByApplicantNr.Values.ToList());

            var m = new OtherApplicationsModel
            {
                ApplicationNr = applicationNr,
                Applicants = new List<OtherApplicationsModel.ApplicantModel>()
            };

            application.DoForEachApplicant(applicantNr =>
            {
                var customerId = customerIdByApplicantNr[applicantNr];
                var credits = customerCreditHistories
                    .Where(y => y.CustomerIds.Contains(customerId))
                    .Select(x => new OtherApplicationsModel.CreditModel
                    {
                        ApplicationNr = x.ApplicationNr,
                        Balance = x.CapitalBalance,
                        CreditNr = x.CreditNr,
                        CreditStatus = x.Status,
                        CustomerIds = x.CustomerIds,
                        CreditUrl = this.serviceRegistryUrlService.CreditUrl(x.CreditNr),
                        ApplicationUrl = x.ApplicationNr == null ? null : this.httpContextUrlService.ApplicationUrl(x.ApplicationNr, x.IsMortgageLoan)
                    })
                    .ToList();
                var applications = applicationsByCustomerId
                    .Opt(customerId)
                    ?.Where(x => x.ApplicationNr != applicationNr && !credits.Any(y => y.CreditNr == x.CreditNr))
                    ?.Select(x => new OtherApplicationsModel.ApplicationModel
                    {
                        ApplicationNr = x.ApplicationNr,
                        ApplicationDate = x.ApplicationDate,
                        IsActive = x.IsActive,
                        IsMortgageLoanApplication = x.IsMortgageLoanApplication,
                        ApplicationUrl = this.httpContextUrlService.ApplicationUrl(x.ApplicationNr, x.IsMortgageLoanApplication)
                    })
                    ?.ToList() ?? new List<OtherApplicationsModel.ApplicationModel>();
                m.Applicants.Add(new OtherApplicationsModel.ApplicantModel
                {
                    ApplicantNr = applicantNr,
                    CustomerId = customerId,
                    Applications = applications,
                    Credits = credits
                });
            });
            return m;
        }
    }

    public class OtherApplicationsModel
    {
        public string ApplicationNr { get; set; }
        public List<ApplicantModel> Applicants { get; set; }

        public class ApplicantModel
        {
            public int CustomerId { get; set; }
            public int ApplicantNr { get; set; }

            public List<ApplicationModel> Applications { get; set; }
            public List<CreditModel> Credits { get; set; }
        }

        public class ApplicationModel
        {
            public string ApplicationNr { get; set; }
            public DateTimeOffset ApplicationDate { get; set; }
            public bool IsActive { get; set; }
            public bool IsMortgageLoanApplication { get; set; }
            public Enum Status { get; set; }
            public string ApplicationUrl { get; set; }
        }

        public class CreditModel
        {
            public string CreditNr { get; set; }
            public string CreditStatus { get; set; }
            public string ApplicationNr { get; set; }
            public decimal Balance { get; set; }
            public List<int> CustomerIds { get; set; }
            public string CreditUrl { get; set; }
            public string ApplicationUrl { get; set; }
        }
    }

    public interface IOtherApplicationsService
    {
        OtherApplicationsModel Fetch(string applicationNr);

        OtherApplicationsModel FetchByCustomerIds(int[] customerIds, string applicationNr, bool includeApplicationObjects);
    }
}