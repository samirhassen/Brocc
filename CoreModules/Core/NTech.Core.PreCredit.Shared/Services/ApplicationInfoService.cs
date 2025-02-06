using NTech.Core.Module.Shared.Clients;
using NTech.Core.PreCredit.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class ApplicationInfoService
    {
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;
        private readonly IPreCreditEnvSettings preCreditEnvSettings;
        private readonly IPreCreditContextFactoryService preCreditContextFactory;
        private readonly ICustomerClient customerClient;

        public const string UcbvServiceCreditApplicationItemName = "ucbvValuationId";
        public const string MortgageLoanLeadsComplexListName = "Lead";

        public ApplicationInfoService(IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository, IPreCreditEnvSettings preCreditEnvSettings,
            IPreCreditContextFactoryService preCreditContextFactory, ICustomerClient customerClient)
        {
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
            this.preCreditEnvSettings = preCreditEnvSettings;
            this.preCreditContextFactory = preCreditContextFactory;
            this.customerClient = customerClient;
        }

        public ApplicationApplicantsModel GetApplicationApplicants(string applicationNr, Action<int, string> includeCivicRegNr = null)
        {
            if (preCreditEnvSettings.IsStandardUnsecuredLoansEnabled || preCreditEnvSettings.IsStandardMortgageLoansEnabled)
            {
                return GetApplicationApplicantsStandard(applicationNr, includeCivicRegNr: includeCivicRegNr);
            }
            else if (preCreditEnvSettings.IsCompanyLoansEnabled)
            {
                if (includeCivicRegNr != null)
                    throw new NotImplementedException();
                return GetApplicationApplicantsCompanyLoan(applicationNr);
            }
            else
            {
                if (includeCivicRegNr != null)
                    throw new NotImplementedException();
                return GetApplicationApplicantsUlOrMlLegacy(applicationNr);
            }
        }

        private ApplicationApplicantsModel GetApplicationApplicantsCompanyLoan(string applicationNr)
        {
            var appModel = partialCreditApplicationModelRepository
                .Get(applicationNr, new PartialCreditApplicationModelRequest { ApplicationFields = new List<string> { "applicantCustomerId", "companyCustomerId" }, ErrorIfGetNonLoadedField = true });

            var applicantCustomerId = appModel.Application.Get("applicantCustomerId").IntValue.Required;
            var companyCustomerId = appModel.Application.Get("companyCustomerId").IntValue.Required;

            var customerIdByApplicantNr = new Dictionary<int, int> { { 1, applicantCustomerId } };
            var r = new ApplicationApplicantsModel
            {
                ApplicationNr = applicationNr,
                NrOfApplicants = 1,
                CustomerIdByApplicantNr = customerIdByApplicantNr,
                ApplicantInfoByApplicantNr = GetApplicantInfoByApplicantNr(customerIdByApplicantNr),
                AllConnectedCustomerIdsWithRoles = new Dictionary<int, HashSet<string>>
                {
                    { applicantCustomerId, new HashSet<string> { "Applicant" } },
                    { companyCustomerId, new HashSet<string> { "Company" } }
                }
            };

            return r;
        }


        private ApplicationApplicantsModel GetApplicationApplicantsUlOrMlLegacy(string applicationNr)
        {
            var appModel = partialCreditApplicationModelRepository
                .Get(applicationNr, new PartialCreditApplicationModelRequest { ApplicantFields = new List<string> { "customerId" }, ErrorIfGetNonLoadedField = true });
            var customerIdByApplicantNr = new Dictionary<int, int>();
            var allConnectedCustomerIdsWithRoles = new Dictionary<int, HashSet<string>>();
            appModel.DoForEachApplicant(applicantNr =>
            {
                var customerId = appModel.Applicant(applicantNr).Get("customerId").IntValue.Required;
                customerIdByApplicantNr[applicantNr] = customerId;
                allConnectedCustomerIdsWithRoles[customerId] = new HashSet<string> { "Applicant" };
            });

            var r = new ApplicationApplicantsModel
            {
                ApplicationNr = applicationNr,
                NrOfApplicants = appModel.NrOfApplicants,
                CustomerIdByApplicantNr = customerIdByApplicantNr,
                ApplicantInfoByApplicantNr = GetApplicantInfoByApplicantNr(customerIdByApplicantNr),
                AllConnectedCustomerIdsWithRoles = allConnectedCustomerIdsWithRoles
            };

            if (preCreditEnvSettings.IsMortgageLoansEnabled)
            {
                using (var context = preCreditContextFactory.CreateExtended())
                {
                    var applicationObjectCustomerIdsRaw = context
                        .CreditApplicationHeadersQueryable
                        .Where(x => x.ApplicationNr == applicationNr)
                        .SelectMany(x => x.ComplexApplicationListItems.Where(y => y.IsRepeatable && y.ItemName == "customerIds" && y.ListName == "ApplicationObject").Select(y => y.ItemValue))
                        .ToList();
                    foreach (var applicationObjectCustomerIdRaw in applicationObjectCustomerIdsRaw)
                    {
                        var applicationObjectCustomerId = int.Parse(applicationObjectCustomerIdRaw);
                        if (!r.AllConnectedCustomerIdsWithRoles.ContainsKey(applicationObjectCustomerId))
                            r.AllConnectedCustomerIdsWithRoles[applicationObjectCustomerId] = new HashSet<string>();

                        r.AllConnectedCustomerIdsWithRoles[applicationObjectCustomerId].Add("ApplicationObject");
                    }
                }
            }

            return r;
        }

        private ApplicationApplicantsModel GetApplicationApplicantsStandard(string applicationNr, Action<int, string> includeCivicRegNr = null)
        {
            using (var context = preCreditContextFactory.CreateExtended())
            {
                var result = context
                    .CreditApplicationHeadersQueryable
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new
                    {
                        Applicants = x.ComplexApplicationListItems
                            .Where(y => y.ApplicationNr == applicationNr && y.ListName == "Applicant" && y.ItemName == "customerId")
                            .Select(y => new
                            {
                                ApplicantNr = y.Nr,
                                CustomerId = y.ItemValue
                            }),
                        ListMembers = x.CustomerListMemberships.Select(y => new { y.CustomerId, y.ListName })
                    })
                    .Single();

                var customerIdByApplicantNr = result
                    .Applicants
                    .ToDictionary(x => x.ApplicantNr, x => int.Parse(x.CustomerId));

                Dictionary<int, HashSet<string>> allConnectedCustomerIdsWithRoles;
                if (preCreditEnvSettings.IsStandardMortgageLoansEnabled)
                {
                    //NOTE: This is probably better for UL also, just need to take the time to test that introducing this
                    //      doesnt cause unitended sideeffects
                    allConnectedCustomerIdsWithRoles = result
                        .ListMembers
                        .GroupBy(x => x.CustomerId)
                        .ToDictionary(x => x.Key, x => x.Select(y => y.ListName).ToHashSetShared());
                }
                else
                    allConnectedCustomerIdsWithRoles = customerIdByApplicantNr.Values.ToDictionary(x => x, _ => new HashSet<string> { "Applicant" });

                return new ApplicationApplicantsModel
                {
                    ApplicationNr = applicationNr,
                    NrOfApplicants = customerIdByApplicantNr.Count,
                    CustomerIdByApplicantNr = customerIdByApplicantNr,
                    ApplicantInfoByApplicantNr = GetApplicantInfoByApplicantNr(customerIdByApplicantNr, includeCivicRegNr: includeCivicRegNr),
                    AllConnectedCustomerIdsWithRoles = allConnectedCustomerIdsWithRoles
                };
            }
        }

        private Dictionary<int, ApplicantInfoModel> GetApplicantInfoByApplicantNr(Dictionary<int, int> customerIdByApplicantNr, Action<int, string> includeCivicRegNr = null)
        {
            var propertyNames = new[]
            {
                "firstName", "lastName", "birthDate", "email", "addressStreet", "addressZipcode", "addressCity", "addressCountry"
            };

            if (includeCivicRegNr != null)
                propertyNames = propertyNames.Concat(Enumerables.Singleton("civicRegNr")).ToArray();

            var customerDataByApplicantNr = customerClient
                .BulkFetchPropertiesByCustomerIdsD(customerIdByApplicantNr.Values.ToHashSetShared(), propertyNames)
                .ToDictionary(x => customerIdByApplicantNr.Where(y => y.Value == x.Key).Select(z => z.Key).First(), x => x.Value);

            var applicantInfosByApplicantNr = new Dictionary<int, ApplicantInfoModel>();
            foreach (var custItem in customerIdByApplicantNr)
            {
                var applicantNr = custItem.Key;
                var customerId = custItem.Value;
                var items = customerDataByApplicantNr[applicantNr];
                applicantInfosByApplicantNr[applicantNr] = new ApplicantInfoModel
                {
                    CustomerId = customerId,
                    FirstName = items.Opt("firstName"),
                    LastName = items.Opt("lastName"),
                    BirthDate = items.Opt("birthDate"),
                    Email = items.Opt("email"),
                    AddressStreet = items.Opt("addressStreet"),
                    AddressZipcode = items.Opt("addressZipcode"),
                    AddressCity = items.Opt("addressCity"),
                    AddressCountry = items.Opt("addressCountry")
                };
                if (includeCivicRegNr != null) //We do this to avoid having a conditional member in ApplicantInfoModel since those tend to lead to NPEs when the object is passed around and the consumer doesnt know if it was fetched with or without the conditional field.
                    includeCivicRegNr(applicantNr, items.Opt("civicRegNr"));

            }

            return applicantInfosByApplicantNr;
        }

        public static IQueryable<ApplicationInfoModel> GetApplicationInfoQueryable(IPreCreditContextExtended context, Func<IQueryable<CreditApplicationHeader>, IQueryable<CreditApplicationHeader>> preFilter = null)
        {

            var pre = context
                .CreditApplicationHeadersQueryable;

            if (preFilter != null)
                pre = preFilter(pre);

            return pre
                .Select(x => new
                {
                    x.ApplicationNr,
                    x.ApplicationType,
                    x.CreditCheckStatus,
                    x.CustomerCheckStatus,
                    x.AgreementStatus,
                    x.FraudCheckStatus,
                    x.NrOfApplicants,
                    x.IsActive,
                    x.WaitingForAdditionalInformationDate,
                    x.IsFinalDecisionMade,
                    x.FinalDecisionDate,
                    x.IsCancelled,
                    x.IsRejected,
                    x.ProviderName,
                    x.ApplicationDate,
                    x.IsPartiallyApproved,
                    MortgageLoanDocumentCheckStatus = x.MortgageLoanExtension.DocumentCheckStatus,
                    MortgageLoanInitialCreditCheckStatus = x.MortgageLoanExtension.InitialCreditCheckStatus,
                    MortgageLoanFinalCreditCheckStatus = x.MortgageLoanExtension.FinalCreditCheckStatus,
                    MortgageLoanAdditionalQuestionsStatus = x.MortgageLoanExtension.AdditionalQuestionsStatus,
                    MortgageLoanDirectDebitCheckStatus = x.MortgageLoanExtension.DirectDebitCheckStatus,
                    IsMortgageLoanApplication = x.MortgageLoanExtension != null,
                    IsMortgageLoanValuationAccepted = x.Items.Any(y => y.Name == UcbvServiceCreditApplicationItemName && y.GroupName == "application"),
                    HasAnsweredCompanyLoanAdditionalQuestions = x.Items.Any(y => y.Name == "additionalQuestionsAnswerDate" && y.GroupName == "application" && (y.Value ?? "pending") != "pending"),
                    HasMortgageLoanAmortizationModel = (context.KeyValueItemsQueryable.Any(y => y.Key == x.ApplicationNr && y.KeySpace == KeyValueStoreKeySpaceCode.MortgageLoanAmortizationModelV1.ToString())),
                    ListNames = x.ListMemberships.Select(y => y.ListName),
                    WorkflowVersion1 = x.Items.Where(y => y.GroupName == "application" && y.Name == "workflowVersion").Select(y => y.Value).FirstOrDefault(),
                    WorkflowVersion2 = x.ComplexApplicationListItems.Where(y => y.ListName == "Application" && y.Nr == 1 && y.ItemName == "workflowVersion").Select(y => y.ItemValue).FirstOrDefault(),
                    HasLockedAgreement = (context.KeyValueItemsQueryable.Any(y => y.Key == x.ApplicationNr && y.KeySpace == KeyValueStoreKeySpaceCode.LockedAgreementV1.ToString())),
                    IsLead = x.ComplexApplicationListItems.Any(y => y.Nr == 1 && y.ListName == MortgageLoanLeadsComplexListName && y.ItemName == "IsLead" && y.ItemValue == "true")
                })
                .Select(h => new
                {
                    h.ApplicationNr,
                    h.ApplicationType,
                    h.CustomerCheckStatus,
                    h.NrOfApplicants,
                    h.CreditCheckStatus,
                    h.FraudCheckStatus,
                    h.AgreementStatus,
                    h.MortgageLoanDocumentCheckStatus,
                    h.MortgageLoanAdditionalQuestionsStatus,
                    h.MortgageLoanInitialCreditCheckStatus,
                    h.MortgageLoanFinalCreditCheckStatus,
                    h.IsMortgageLoanValuationAccepted,
                    h.HasMortgageLoanAmortizationModel,
                    h.MortgageLoanDirectDebitCheckStatus,
                    h.IsActive,
                    h.WaitingForAdditionalInformationDate,
                    h.IsFinalDecisionMade,
                    h.FinalDecisionDate,
                    h.IsCancelled,
                    h.IsRejected,
                    h.ProviderName,
                    h.ApplicationDate,
                    h.IsPartiallyApproved,
                    h.IsMortgageLoanApplication,
                    IsUnsecuredLoanApproveAllowed = h.IsActive
                        && !h.WaitingForAdditionalInformationDate.HasValue
                        && !h.IsPartiallyApproved
                        && !h.IsFinalDecisionMade
                        && h.CreditCheckStatus == "Accepted"
                        && h.CustomerCheckStatus == "Accepted"
                        && h.FraudCheckStatus == "Accepted",
                    IsUnsecuredLoanRejectAllowed = h.IsActive
                        && !h.WaitingForAdditionalInformationDate.HasValue
                        && (h.CreditCheckStatus == "Rejected" || h.CustomerCheckStatus == "Rejected" || h.FraudCheckStatus == "Rejected"),
                    h.HasAnsweredCompanyLoanAdditionalQuestions,
                    ListNames = h.ListNames,
                    WorkflowVersion = h.WorkflowVersion1 ?? h.WorkflowVersion2,
                    h.HasLockedAgreement,
                    h.IsLead
                })
                .Select(h => new ApplicationInfoModel
                {
                    ApplicationNr = h.ApplicationNr,
                    ApplicationType = h.ApplicationType,
                    CustomerCheckStatus = h.CustomerCheckStatus,
                    NrOfApplicants = h.NrOfApplicants,
                    CreditCheckStatus = h.CreditCheckStatus,
                    FraudCheckStatus = h.FraudCheckStatus,
                    AgreementStatus = h.AgreementStatus,
                    IsActive = h.IsActive,
                    IsWaitingForAdditionalInformation = h.WaitingForAdditionalInformationDate.HasValue,
                    IsFinalDecisionMade = h.IsFinalDecisionMade,
                    FinalDecisionDate = h.FinalDecisionDate,
                    IsCancelled = h.IsCancelled,
                    IsRejected = h.IsRejected,
                    ProviderName = h.ProviderName,
                    ProviderDisplayName = h.ProviderName,
                    ApplicationDate = h.ApplicationDate,
                    IsPartiallyApproved = h.IsPartiallyApproved,
                    IsRejectAllowed = h.IsMortgageLoanApplication ? false : h.IsUnsecuredLoanRejectAllowed,
                    IsApproveAllowed = h.IsMortgageLoanApplication ? false : h.IsUnsecuredLoanApproveAllowed,
                    IsMortgageLoanApplication = h.IsMortgageLoanApplication,
                    IsSettlementAllowed = !h.IsFinalDecisionMade && (h.IsPartiallyApproved || (h.IsMortgageLoanApplication ? false : h.IsUnsecuredLoanApproveAllowed)),
                    CompanyLoanAdditionalQuestionsStatus = h.HasAnsweredCompanyLoanAdditionalQuestions ? "Accepted" : "Initial",
                    ListNames = h.ListNames,
                    WorkflowVersion = h.WorkflowVersion,
                    HasLockedAgreement = h.HasLockedAgreement,
                    IsLead = h.IsLead
                });
        }

        public ApplicationInfoModel GetApplicationInfo(string applicationNr) => GetApplicationInfo(applicationNr, false);

        public ApplicationInfoModel GetApplicationInfo(string applicationNr, bool returnNullIfMissing)
        {
            ApplicationInfoModel i;
            using (var context = preCreditContextFactory.CreateExtended())
            {
                if (returnNullIfMissing)
                    i = GetApplicationInfoQueryable(context).SingleOrDefault(x => x.ApplicationNr == applicationNr);
                else
                    i = GetApplicationInfoQueryable(context).Single(x => x.ApplicationNr == applicationNr);
            }
            if (i != null)
            {
                AssignNonDataModelApplicationInfoProperties(i);
            }
            return i;
        }

        public Dictionary<string, ApplicationInfoModel> GetApplicationInfoBatch(ISet<string> applicationNrs)
        {
            Dictionary<string, ApplicationInfoModel> models = new Dictionary<string, ApplicationInfoModel>();
            using (var context = preCreditContextFactory.CreateExtended())
            {
                GetApplicationInfoQueryable(context)
                    .Where(x => applicationNrs.Contains(x.ApplicationNr))
                    .ToList()
                    .ForEach(x =>
                    {
                        AssignNonDataModelApplicationInfoProperties(x);
                        models[x.ApplicationNr] = x;
                    });
            }
            var missingApplicationNrs = applicationNrs.Except(models.Keys).ToList();
            if (missingApplicationNrs.Any())
                throw new Exception($"Missing application info for: {string.Join(",", missingApplicationNrs)}");
            return models;
        }

        private void AssignNonDataModelApplicationInfoProperties(ApplicationInfoModel model)
        {
            model.ProviderDisplayName = preCreditEnvSettings.GetAffiliateModel(model.ProviderName, allowMissing: true)?.DisplayToEnduserName ?? model.ProviderName;
            model.CreditReportProviderName = preCreditEnvSettings.CreditReportProviderName;
            model.ListCreditReportProviders = preCreditEnvSettings.ListCreditReportProviders;
        }
    }

    public class ApplicationApplicantsModel
    {
        public string ApplicationNr { get; set; }
        public int NrOfApplicants { get; set; }
        public Dictionary<int, int> CustomerIdByApplicantNr { get; set; }
        public Dictionary<int, ApplicantInfoModel> ApplicantInfoByApplicantNr { get; set; }
        public Dictionary<int, HashSet<string>> AllConnectedCustomerIdsWithRoles { get; set; }
    }

    public class ApplicantInfoModel
    {
        public int CustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string BirthDate { get; set; }
        public string Email { get; set; }
        public string AddressStreet { get; set; }
        public string AddressZipcode { get; set; }
        public string AddressCity { get; set; }
        public string AddressCountry { get; set; }
    }
}