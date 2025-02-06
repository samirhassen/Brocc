using Newtonsoft.Json;
using NTech;
using NTech.Banking.Autogiro;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class MortgageLoanApplicationDirectDebitCheckService : IMortgageLoanApplicationDirectDebitCheckService
    {
        private readonly IApplicationDocumentService applicationDocumentService;
        private readonly AutogiroPaymentNumberGenerator autogiroPaymentNumberGenerator;
        private readonly ICustomerInfoService customerInfoService;
        private readonly CivicRegNumberParser civicRegNumberParser;
        private readonly KeyValueStore directDebitStatusStateStore;
        private readonly IKeyValueStoreService keyValueStoreService;

        public MortgageLoanApplicationDirectDebitCheckService(
            IApplicationDocumentService applicationDocumentService,
            AutogiroPaymentNumberGenerator autogiroPaymentNumberGenerator, IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository,
            ICustomerInfoService customerInfoService, CivicRegNumberParser civicRegNumberParser, IKeyValueStoreService keyValueStoreService,
            UpdateCreditApplicationRepository updateCreditApplicationRepository, IClock clock,
            IServiceRegistryUrlService serviceRegistryUrlService,
            IMortgageLoanWorkflowService mortgageLoanWorkflowService)
        {
            this.applicationDocumentService = applicationDocumentService;
            this.autogiroPaymentNumberGenerator = autogiroPaymentNumberGenerator;
            this.customerInfoService = customerInfoService;
            this.civicRegNumberParser = civicRegNumberParser;
            this.clock = clock;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
            this.updateCreditApplicationRepository = updateCreditApplicationRepository;
            this.serviceRegistryUrlService = serviceRegistryUrlService;
            this.mortgageLoanWorkflowService = mortgageLoanWorkflowService;
            this.keyValueStoreService = keyValueStoreService;
            this.directDebitStatusStateStore = new KeyValueStore(KeyValueStoreKeySpaceCode.DirectDebitStatusState, keyValueStoreService);
        }

        public const string WorkflowStepName = "MortgageLoanDirectDebitCheck";

        public bool IsEditAllowed(ApplicationInfoModel applicationInfo)
        {
            return applicationInfo.IsActive
                && mortgageLoanWorkflowService.AreAllStepsBeforeComplete(WorkflowStepName, applicationInfo.ListNames)
                && !applicationInfo.IsPartiallyApproved
                && !applicationInfo.IsFinalDecisionMade
                && !applicationInfo.IsWaitingForAdditionalInformation;
        }

        public MortgageLoanApplicationDirectDebitStatusModel FetchStatus(ApplicationInfoModel applicationInfo)
        {
            var m = new MortgageLoanApplicationDirectDebitStatusModel
            {
                IsEditAllowed = IsEditAllowed(applicationInfo)
            };

            var arePreviousStepsCompleted = mortgageLoanWorkflowService.AreAllStepsBeforeComplete(WorkflowStepName, applicationInfo.ListNames);

            //Try to fetch the signed direct debit consent document (only on edit to keep performance reasonable since this calls out to the archive)
            if (m.IsEditAllowed)
            {
                var d = applicationDocumentService
                    .FetchForApplication(applicationInfo.ApplicationNr, new List<string> { CreditApplicationDocumentTypeCode.SignedDirectDebitConsent.ToString() })
                    ?.FirstOrDefault();
                m.SignedDirectDebitConsentDocumentDownloadUrl = d == null ? null : this.serviceRegistryUrlService.ArchiveDocumentUrl(d.DocumentArchiveKey).ToString();
            }

            //Applicants
            if (arePreviousStepsCompleted)
            {
                //We require this to make sure a creditnr has been generated (which it is for the binding agreement if not before)

                var appModel = partialCreditApplicationModelRepository.Get(applicationInfo.ApplicationNr, new PartialCreditApplicationModelRequest
                {
                    ErrorIfGetNonLoadedField = true,
                    ApplicantFields = new List<string> { "customerId" },
                    ApplicationFields = new List<string> { "creditnr", "directDebitStatusStateId", "bankAccountNr", "bankAccountNrOwnerApplicantNr" }
                });

                var creditNr = appModel.Application.Get("creditnr").StringValue.Optional;
                if (string.IsNullOrWhiteSpace(creditNr))
                {
                    var c = new CreditClient();
                    creditNr = c.NewCreditNumber();

                    updateCreditApplicationRepository.UpdateApplication(applicationInfo.ApplicationNr, new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest
                    {
                        StepName = "MortgageLoanDirectDebitCheck",
                        Items = new List<UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem>
                            {
                                new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                                {
                                    GroupName = "application",
                                    Name = "creditnr",
                                    Value = creditNr
                                }
                            }
                    });
                }

                m.AdditionalQuestionsBankAccountNr = appModel.Application.Get("bankAccountNr").StringValue.Optional;
                if (m.AdditionalQuestionsBankAccountNr != null)
                    m.AdditionalQuestionsBankName = BankAccountNumberSe.Parse(m.AdditionalQuestionsBankAccountNr).BankName;
                m.AdditionalQuestionAccountOwnerApplicantNr = appModel.Application.Get("bankAccountNrOwnerApplicantNr").IntValue.Optional;

                var customerIdByApplicantNr = new Dictionary<int, int>();
                appModel.DoForEachApplicant(applicantNr =>
                {
                    customerIdByApplicantNr[applicantNr] = appModel.Applicant(applicantNr).Get("customerId").IntValue.Required;
                });

                var customerInfoByCustomerId = customerInfoService.GetContactInfoByCustomerIds(new HashSet<int>(customerIdByApplicantNr.Values));

                appModel.DoForEachApplicant(applicantNr =>
                {
                    var a = new MortgageLoanApplicationDirectDebitStatusModel.ApplicantModel();
                    var customerInfo = customerInfoByCustomerId[customerIdByApplicantNr[applicantNr]];

                    a.BirthDate = this.civicRegNumberParser.Parse(customerInfo.CivicRegNr).BirthDate;
                    a.FirstName = customerInfo.FirstName;

                    a.StandardPaymentNumber = autogiroPaymentNumberGenerator.GenerateNr(creditNr, applicantNr);

                    if (applicantNr == 1)
                        m.Applicant1 = a;
                    else if (applicantNr == 2)
                        m.Applicant2 = a;
                    else
                        throw new NotImplementedException();
                });

                m.DirectDebitCheckStatus = applicationInfo.MortgageLoanDirectDebitCheckStatus;

                var directDebitStatusStateId = appModel.Application.Get("directDebitStatusStateId").StringValue.Optional;
                if (directDebitStatusStateId != null)
                {
                    var state = FetchDirectDebitStatusState(this.keyValueStoreService, directDebitStatusStateId);
                    if (state != null)
                    {
                        m.DirectDebitCheckStatusDate = state.StatusDate;
                        m.DirectDebitCheckAccountOwnerApplicantNr = state.BankAccountOwnerApplicantNr;
                        m.DirectDebitCheckBankAccountNr = state.BankAccountNr;
                        if (state.BankAccountNr != null)
                            m.DirectDebitCheckBankName = BankAccountNumberSe.Parse(state.BankAccountNr).BankName;
                    }
                }
            }

            return m;
        }

        public bool TryUpdateDirectDebitCheckStatusState(string applicationNr, string newStatus, string bankAccountNr, int? bankAccountOwnerApplicantNr, out string failedMessage)
        {
            failedMessage = null;

            if (string.IsNullOrWhiteSpace(applicationNr))
            {
                failedMessage = "Missing applicationNr";
                return false;
            }

            if (string.IsNullOrWhiteSpace(newStatus))
            {
                failedMessage = "Missing newStatus";
                return false;
            }

            if (!newStatus.IsOneOf("Initial", "Pending", "Accepted"))
            {
                failedMessage = "Invalid newStatus";
                return false;
            }

            BankAccountNumberSe b = null;
            if (!string.IsNullOrWhiteSpace(bankAccountNr))
            {
                if (!bankAccountOwnerApplicantNr.HasValue || bankAccountOwnerApplicantNr.Value <= 0)
                {
                    failedMessage = "Missing bankAccountOwnerApplicantNr";
                    return false;
                }

                string bankAccountFailedMessage;
                if (!BankAccountNumberSe.TryParse(bankAccountNr, out b, out bankAccountFailedMessage))
                {
                    failedMessage = $"Invalid bankAccountNr - {bankAccountFailedMessage}";
                    return false;
                }
            }

            if (newStatus != "Initial")
            {
                if (b == null)
                {
                    failedMessage = "Missing bankAccountNr";
                    return false;
                }
                if (!bankAccountOwnerApplicantNr.HasValue)
                {
                    failedMessage = "Missing bankAccountOwnerApplicantNr";
                    return false;
                }
            }

            var appModel = partialCreditApplicationModelRepository.Get(applicationNr, new PartialCreditApplicationModelRequest
            {
                ErrorIfGetNonLoadedField = true,
                ApplicationFields = new List<string> { CreditApplicationItemName }
            });

            var key = appModel.Application.Get(CreditApplicationItemName).StringValue.Optional ?? Guid.NewGuid().ToString();

            directDebitStatusStateStore.SetValue(key, JsonConvert.SerializeObject(new MortgageLoanApplicationDirectDebitStatusState
            {
                BankAccountNr = newStatus == "Initial" ? null : b?.PaymentFileFormattedNr,
                BankAccountOwnerApplicantNr = newStatus == "Initial" ? null : bankAccountOwnerApplicantNr,
                StatusDate = clock.Now.DateTime
            }));

            updateCreditApplicationRepository.UpdateApplication(applicationNr, new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest
            {
                StepName = "MortgageLoanDirectDebitCheck",
                Items = new List<UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem>
                    {
                        new UpdateCreditApplicationRepository.CreditApplicationUpdateRequest.CreditApplicationItem
                        {
                            GroupName = "application",
                            Name = CreditApplicationItemName,
                            Value = key
                        }
                    }
            }, context =>
            {
                var m = context.MortgageLoanCreditApplicationHeaderExtensionsQueryable.Single(x => x.ApplicationNr == applicationNr);
                m.DirectDebitCheckStatus = newStatus;
                mortgageLoanWorkflowService.ChangeStepStatusComposable(
                    context,
                    WorkflowStepName,
                    newStatus == "Initial" ? mortgageLoanWorkflowService.InitialStatusName : mortgageLoanWorkflowService.AcceptedStatusName, applicationNr: applicationNr);
            });

            return true;
        }

        public const string CreditApplicationItemName = "directDebitStatusStateId";
        private readonly IClock clock;
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;
        private readonly UpdateCreditApplicationRepository updateCreditApplicationRepository;
        private readonly IServiceRegistryUrlService serviceRegistryUrlService;
        private readonly IMortgageLoanWorkflowService mortgageLoanWorkflowService;

        public static MortgageLoanApplicationDirectDebitStatusState FetchDirectDebitStatusState(IKeyValueStoreService keyValueStoreService, string key)
        {
            var value = keyValueStoreService.GetValue(key, KeyValueStoreKeySpaceCode.DirectDebitStatusState.ToString());
            if (value == null)
                return null;

            return JsonConvert.DeserializeObject<MortgageLoanApplicationDirectDebitStatusState>(value);
        }
    }

    public interface IMortgageLoanApplicationDirectDebitCheckService
    {
        MortgageLoanApplicationDirectDebitStatusModel FetchStatus(ApplicationInfoModel applicationInfo);

        bool TryUpdateDirectDebitCheckStatusState(string applicationNr, string newStatus, string bankAccountNr, int? bankAccountOwnerApplicantNr, out string failedMessage);
    }

    public class MortgageLoanApplicationDirectDebitStatusState
    {
        public DateTime? StatusDate { get; set; }
        public string BankAccountNr { get; set; }
        public int? BankAccountOwnerApplicantNr { get; set; }
    }

    public class MortgageLoanApplicationDirectDebitStatusModel
    {
        public bool IsEditAllowed { get; set; }
        public string AdditionalQuestionsBankAccountNr { get; set; }
        public string AdditionalQuestionsBankName { get; set; }
        public int? AdditionalQuestionAccountOwnerApplicantNr { get; set; }

        public ApplicantModel Applicant1 { get; set; }
        public ApplicantModel Applicant2 { get; set; }

        public string SignedDirectDebitConsentDocumentDownloadUrl { get; set; }

        public string DirectDebitCheckStatus { get; set; }
        public DateTime? DirectDebitCheckStatusDate { get; set; }

        public string DirectDebitCheckBankAccountNr { get; set; }
        public string DirectDebitCheckBankName { get; set; }
        public int? DirectDebitCheckAccountOwnerApplicantNr { get; set; }

        public class ApplicantModel
        {
            public string FirstName { get; set; }
            public DateTime? BirthDate { get; set; }
            public string StandardPaymentNumber { get; set; }
        }
    }
}