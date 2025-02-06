using Newtonsoft.Json;
using NTech;
using NTech.Core;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared.Services;
using NTech.Services.Infrastructure.Email;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services.CompanyLoans
{
    public class CompanyLoanApplicationApprovalService : ICompanyLoanApplicationApprovalService
    {
        private readonly INTechCurrentUserMetadata ntechCurrentUserMetadata;
        private readonly IClock clock;
        private readonly INTechEmailService emailService;
        private readonly IPublishEventService publishEventService;
        private readonly ICreditClient creditClient;
        private readonly IPartialCreditApplicationModelRepository creditApplicationModelRepository;
        private readonly ICompanyLoanWorkflowService companyLoanWorkflowService;
        private readonly IUserDisplayNameService userDisplayNameService;
        private readonly IHttpContextUrlService httpContextUrlService;

        public CompanyLoanApplicationApprovalService(INTechCurrentUserMetadata ntechCurrentUserMetadata, IClock clock, INTechEmailService emailService, IPublishEventService publishEventService, ICreditClient creditClient, IPartialCreditApplicationModelRepository creditApplicationModelRepository, ICompanyLoanWorkflowService companyLoanWorkflowService, IUserDisplayNameService userDisplayNameService, IHttpContextUrlService httpContextUrlService)
        {
            this.ntechCurrentUserMetadata = ntechCurrentUserMetadata;
            this.clock = clock;
            this.emailService = emailService;
            this.publishEventService = publishEventService;
            this.creditClient = creditClient;
            this.creditApplicationModelRepository = creditApplicationModelRepository;
            this.companyLoanWorkflowService = companyLoanWorkflowService;
            this.userDisplayNameService = userDisplayNameService;
            this.httpContextUrlService = httpContextUrlService;
        }

        public string ApproveApplicationAndCreateCredit(string applicationNr)
        {
            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
            {
                context.BeginTransaction();
                try
                {
                    var aa = context
                                       .CreditApplicationHeaders
                                       .Include("Items")
                                       .Include("CurrentCreditDecision")
                                       .Include("CustomerListMemberships")
                                       .Include("Documents")
                                       .Where(x => x.ApplicationNr == applicationNr)
                                       .Select(x => new
                                       {
                                           x.CurrentCreditDecision,
                                           App = x
                                       })
                                       .Single();
                    var a = aa.App;
                    var currentCreditDecision = aa.CurrentCreditDecision;

                    if (!a.IsActive)
                        throw new NTechWebserviceMethodException("Application is not active")
                        {
                            ErrorCode = "applicationNotActive"
                        };

                    if (a.IsFinalDecisionMade)
                        throw new NTechWebserviceMethodException("The credit has already been created")
                        {
                            ErrorCode = "creditAlreadyCreated"
                        };

                    if (a.CreditCheckStatus != "Accepted")
                        throw new NTechWebserviceMethodException("CreditCheckStatus is not Accepted")
                        {
                            ErrorCode = "creditCheckStatusNotAccepted"
                        };

                    if (a.FraudCheckStatus != "Accepted")
                        throw new NTechWebserviceMethodException("FraudCheckStatus is not Accepted")
                        {
                            ErrorCode = "fraudCheckStatusNotAccepted"
                        };

                    if (a.CustomerCheckStatus != "Accepted")
                        throw new NTechWebserviceMethodException("CustomerCheckStatus is not Accepted")
                        {
                            ErrorCode = "customerCheckStatusNotAccepted"
                        };

                    if (a.AgreementStatus != "Accepted")
                        throw new NTechWebserviceMethodException("AgreementStatus is not Accepted")
                        {
                            ErrorCode = "agreementCheckStatusNotAccepted"
                        };

                    var creditDecision = currentCreditDecision as AcceptedCreditDecision;
                    if (creditDecision == null)
                        throw new NTechWebserviceMethodException("No current credit decision exists")
                        {
                            ErrorCode = "missingCreditDecision"
                        };

                    var decisionModel = CreditDecisionModelParser.ParseCompanyLoanCreditDecision(creditDecision.AcceptedDecisionModel);
                    var offer = decisionModel.CompanyLoanOffer;

                    if (offer == null)
                        throw new NTechWebserviceMethodException("Credit decision has no offer")
                        {
                            ErrorCode = "missingOffer"
                        };

                    var now = clock.Now;

                    var repo = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>();

                    var appModel = creditApplicationModelRepository.Get(a.ApplicationNr, new PartialCreditApplicationModelRequest
                    {
                        ErrorIfGetNonLoadedField = true,
                        ApplicationFields = new List<string>
                            {
                                "applicantEmail",
                                "companyCustomerId",
                                "applicantCustomerId",
                                NEnv.ClientCfg.Country.BaseCountry == "SE" ? "bankaccountnr" : "iban",
                                "bankAccountNrType",
                                "creditnr",
                                "providerApplicationId",
                                "campaignCode",
                                "sourceChannel",
                                "signed_initial_agreement_key"
                            }
                    });

                    bool wasPartiallyApproved = false;
                    if (!a.IsPartiallyApproved)
                    {
                        //NOTE: When this has been in production for a while this if can probably be remove as all application will need to be partially approved
                        //      During the transition though applictions that were partially approved but not created need this scaffolding.
                        wasPartiallyApproved = true;
                        string commentEmailPart = "";
                        var email = appModel.Application.Get("applicantEmail").StringValue.Optional;

                        a.IsPartiallyApproved = true;
                        a.PartiallyApprovedById = ntechCurrentUserMetadata.UserId;
                        a.PartiallyApprovedDate = now;

                        var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
                        if (!string.IsNullOrWhiteSpace(email) && emailValidator.IsValid(email))
                        {
                            emailService.SendTemplateEmail(new List<string> { email }, "creditapproval-letter-general", null, $"Reason=CreditApproval, ApplicationNr={applicationNr}");
                            commentEmailPart = " and approval email sent to the applicant";
                        }
                        else
                        {
                            commentEmailPart = " without and approval email to the applicant since no valid email was present";
                        }

                        var comment = context.FillInfrastructureFields(new CreditApplicationComment
                        {
                            ApplicationNr = applicationNr,
                            CommentText = "Application approved " + commentEmailPart,
                            CommentDate = now,
                            CommentById = ntechCurrentUserMetadata.UserId,
                            EventType = "CompanyLoanApplicationApproved",
                        });

                        context.CreditApplicationComments.Add(comment);

                        companyLoanWorkflowService.ChangeStepStatusComposable(context, "Approval", "Accepted", applicationNr: applicationNr);
                    }

                    a.IsActive = false;
                    a.IsFinalDecisionMade = true;
                    a.FinalDecisionById = ntechCurrentUserMetadata.UserId;
                    a.FinalDecisionDate = clock.Now;

                    var creditNr = appModel.Application.Get("creditnr").StringValue.Optional;

                    if (string.IsNullOrWhiteSpace(creditNr))
                    {
                        creditNr = creditClient.NewCreditNumber();
                        context.AddOrUpdateCreditApplicationItems(a, new List<PreCreditContextExtended.CreditApplicationItemModel>
                    {
                        new PreCreditContextExtended.CreditApplicationItemModel
                        {
                            GroupName = "application",
                            IsEncrypted = false,
                            Name = "creditnr",
                            Value = creditNr
                        }
                    }, "CreateCredit");
                    }

                    Func<List<CreditApplicationCustomerListMember>, string, List<int>> getCustomerList = (c, n) =>
                        c.Where(y => y.ListName == n).Select(y => y.CustomerId).ToList();

                    var r = new CreateCompanyCreditsRequest.Credit
                    {
                        ApplicationNr = a.ApplicationNr,
                        AnnuityAmount = offer.AnnuityAmount,
                        CreditAmount = offer.LoanAmount.Value,
                        CreditNr = creditNr,
                        MarginInterestRatePercent = offer.NominalInterestRatePercent.Value,
                        DrawnFromLoanAmountInitialFeeAmount = offer.InitialFeeAmount,
                        NotificationFee = offer.MonthlyFeeAmount,
                        AgreementPdfArchiveKey = appModel.Application.Get("signed_initial_agreement_key").StringValue.Optional,
                        CapitalizedInitialFeeAmount = null,
                        ProviderName = a.ProviderName,
                        Iban = NEnv.ClientCfg.Country.BaseCountry == "FI" ? appModel.Application.Get("iban").StringValue.Optional : null,
                        BankAccountNr = NEnv.ClientCfg.Country.BaseCountry == "SE" ? appModel.Application.Get("bankaccountnr").StringValue.Required : null,
                        BankAccountNrType = appModel.Application.Get("bankAccountNrType").StringValue.Optional,
                        CampaignCode = appModel.Application.Get("campaignCode").StringValue.Optional,
                        ProviderApplicationId = appModel.Application.Get("providerApplicationId").StringValue.Optional,
                        CompanyCustomerId = appModel.Application.Get("companyCustomerId").IntValue.Required,
                        SourceChannel = appModel.Application.Get("sourceChannel").StringValue.Optional,
                        CompanyLoanApplicantCustomerIds = new List<int> { appModel.Application.Get("applicantCustomerId").IntValue.Required },
                        CompanyLoanCollateralCustomerIds = getCustomerList(a.CustomerListMemberships, "companyLoanCollateral"),
                        CompanyLoanAuthorizedSignatoryCustomerIds = getCustomerList(a.CustomerListMemberships, "companyLoanAuthorizedSignatory"),
                        CompanyLoanBeneficialOwnerCustomerIds = getCustomerList(a.CustomerListMemberships, "companyLoanBeneficialOwner"),
                        ApplicationFreeformDocumentArchiveKeys = a.Documents.Where(x => x.DocumentType == "Freeform" && !x.RemovedByUserId.HasValue).Select(x => x.DocumentArchiveKey).ToList(),
                        SniKodSe = decisionModel?.Recommendation?.ScoringData?.GetString("companyCreditReportSnikod", null)
                    };

                    context.SaveChanges(); //Save to a db-serverside transaction to leave as small a windows as possible where nCredit and nPreCredit can disagree on the state

                    var result = creditClient.CreateCompanyCredits(new CreateCompanyCreditsRequest
                    {
                        Credits = new List<CreateCompanyCreditsRequest.Credit> { r }
                    });

                    context.CommitTransaction();

                    if (wasPartiallyApproved)
                        publishEventService.Publish(PreCreditEventCode.CreditApplicationPartiallyApproved.ToString(), JsonConvert.SerializeObject(new { applicationNr = applicationNr }));

                    return result.Single();
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }
        }

        public List<HistoricalCompanyLoanFinalDecisionBatchModel> FetchHistoricalDecisionBatches(DateTime fromDate, DateTime toDate)
        {
            using (var context = new PreCreditContext())
            {
                var fd = fromDate.Date;
                var td = toDate.Date.AddDays(1); //Since ApprovedDate also has time
                return context
                    .CreditApprovalBatchHeaders
                    .Where(x => x.ApprovedDate >= fd && x.ApprovedDate < td)
                    .OrderByDescending(x => x.Id)
                    .Select(x => new HistoricalCompanyLoanFinalDecisionBatchModel
                    {
                        Id = x.Id,
                        ApprovedDate = x.ApprovedDate,
                        TotalCount = x.Items.Count(),
                        TotalAmount = x.Items.Sum(y => (decimal?)y.ApprovedAmount) ?? 0m
                    })
                    .ToList();
            }
        }

        public List<HistoricalCompanyLoanFinalDecisionBatchItemModel> FetchHistoricalDecisionBatchItems(int batchId)
        {
            using (var context = new PreCreditContext())
            {
                var result = context
                    .CreditApprovalBatchHeaders
                    .Where(x => x.Id == batchId)
                    .Select(x => new
                    {
                        items = x.Items.Select(y => new
                        {
                            y.Id,
                            y.DecisionById,
                            y.ApplicationNr,
                            y.ApprovedAmount,
                            y.CreditNr,
                            y.ApprovalType
                        })
                    })
                    .Single();

                return result.items.Select(x => new HistoricalCompanyLoanFinalDecisionBatchItemModel
                {
                    Id = x.Id,
                    HandlerUserId = x.DecisionById,
                    HandlerDisplayName = userDisplayNameService.GetUserDisplayNameByUserId(x.DecisionById.ToString()),
                    Amount = x.ApprovedAmount,
                    ApplicationNr = x.ApplicationNr,
                    CreditNr = x.CreditNr,
                    ApplicationUrl = httpContextUrlService.ActionStrict("CompanyLoanApplication", "CompanyLoansMiddleSharedUi", new { applicationNr = x.ApplicationNr }),
                    LoanUrl = x.CreditNr == null ? null : NEnv.ServiceRegistry.External.ServiceUrl("nCredit", "Ui/Credit", Tuple.Create("creditNr", x.CreditNr)).ToString(),
                    TypeName = x.ApprovalType
                }).ToList();
            }
        }

        public string CreateCredit(string applicationNr)
        {
            using (var context = new PreCreditContextExtended(ntechCurrentUserMetadata, clock))
            {
                return context.UsingTransaction(() =>
                {
                    var ct = CreditApplicationTypeCode.companyLoan.ToString();
                    if (!context.CreditApplicationHeaders.Any(x => x.ApplicationType == ct && x.IsActive && x.IsPartiallyApproved && !x.IsFinalDecisionMade))
                        throw new NTechWebserviceMethodException($"Cannot create credit from application {applicationNr} since it's not pending final decision") { ErrorCode = "applicationNotPendingFinalDecision" };

                    var h = context.FillInfrastructureFields(new CreditApprovalBatchHeader
                    {
                        ApprovedById = ntechCurrentUserMetadata.UserId,
                        ApprovedDate = clock.Now
                    });

                    var ch = context
                        .CreditApplicationHeaders
                        .Include("Items")
                        .Include("CurrentCreditDecision")
                        .Include("CustomerListMemberships")
                        .Include("Documents")
                        .Single(x => x.ApplicationNr == applicationNr);

                    ch.IsActive = false;
                    ch.IsFinalDecisionMade = true;
                    ch.FinalDecisionById = ntechCurrentUserMetadata.UserId;
                    ch.FinalDecisionDate = clock.Now;

                    var decisionModel = CreditDecisionModelParser.ParseCompanyLoanCreditDecision((ch.CurrentCreditDecision as AcceptedCreditDecision).AcceptedDecisionModel);
                    var appModel = creditApplicationModelRepository.Get(ch.ApplicationNr, new PartialCreditApplicationModelRequest
                    {
                        ErrorIfGetNonLoadedField = true,
                        ApplicationFields = new List<string>
                    {
                        "companyCustomerId",
                        "applicantCustomerId",
                        NEnv.ClientCfg.Country.BaseCountry == "SE" ? "bankaccountnr" : "iban",
                        "bankAccountNrType",
                        "creditnr",
                        "providerApplicationId",
                        "campaignCode",
                        "sourceChannel",
                        "signed_initial_agreement_key"
                    }
                    });

                    var creditNr = appModel.Application.Get("creditnr").StringValue.Optional;

                    if (string.IsNullOrWhiteSpace(creditNr))
                    {
                        creditNr = creditClient.NewCreditNumber();
                        context.AddOrUpdateCreditApplicationItems(ch, new List<PreCreditContextExtended.CreditApplicationItemModel>
                        {
                            new PreCreditContextExtended.CreditApplicationItemModel
                            {
                                GroupName = "application",
                                IsEncrypted = false,
                                Name = "creditnr",
                                Value = creditNr
                            }
                        }, "CreateCredit");
                    }

                    //Approval item
                    var offer = decisionModel.CompanyLoanOffer;

                    Func<List<CreditApplicationCustomerListMember>, string, List<int>> getCustomerList = (c, n) =>
                        c.Where(y => y.ListName == n).Select(y => y.CustomerId).ToList();

                    var r = new CreateCompanyCreditsRequest.Credit
                    {
                        ApplicationNr = ch.ApplicationNr,
                        AnnuityAmount = offer.AnnuityAmount,
                        CreditAmount = offer.LoanAmount.Value,
                        CreditNr = creditNr,
                        MarginInterestRatePercent = offer.NominalInterestRatePercent.Value,
                        DrawnFromLoanAmountInitialFeeAmount = offer.InitialFeeAmount,
                        NotificationFee = offer.MonthlyFeeAmount,
                        AgreementPdfArchiveKey = appModel.Application.Get("signed_initial_agreement_key").StringValue.Optional,
                        CapitalizedInitialFeeAmount = null,
                        ProviderName = ch.ProviderName,
                        Iban = NEnv.ClientCfg.Country.BaseCountry == "FI" ? appModel.Application.Get("iban").StringValue.Optional : null,
                        BankAccountNr = NEnv.ClientCfg.Country.BaseCountry == "SE" ? appModel.Application.Get("bankaccountnr").StringValue.Required : null,
                        BankAccountNrType = appModel.Application.Get("bankAccountNrType").StringValue.Optional,
                        CampaignCode = appModel.Application.Get("campaignCode").StringValue.Optional,
                        ProviderApplicationId = appModel.Application.Get("providerApplicationId").StringValue.Optional,
                        CompanyCustomerId = appModel.Application.Get("companyCustomerId").IntValue.Required,
                        SourceChannel = appModel.Application.Get("sourceChannel").StringValue.Optional,
                        CompanyLoanApplicantCustomerIds = new List<int> { appModel.Application.Get("applicantCustomerId").IntValue.Required },
                        CompanyLoanCollateralCustomerIds = getCustomerList(ch.CustomerListMemberships, "companyLoanCollateral"),
                        CompanyLoanAuthorizedSignatoryCustomerIds = getCustomerList(ch.CustomerListMemberships, "companyLoanBeneficialOwner"),
                        CompanyLoanBeneficialOwnerCustomerIds = getCustomerList(ch.CustomerListMemberships, "companyLoanAuthorizedSignatory"),
                        ApplicationFreeformDocumentArchiveKeys = ch.Documents.Where(x => x.DocumentType == "Freeform" && !x.RemovedByUserId.HasValue).Select(x => x.DocumentArchiveKey).ToList(),
                        SniKodSe = decisionModel?.Recommendation?.ScoringData?.GetString("companyCreditReportSnikod", null)
                    };

                    context.SaveChanges(); //Save to a db-serverside transaction to leave as small a windows as possible where nCredit and nPreCredit can disagree on the state

                    var result = creditClient.CreateCompanyCredits(new CreateCompanyCreditsRequest
                    {
                        Credits = new List<CreateCompanyCreditsRequest.Credit> { r }
                    });

                    return result.Single();
                });
            }
        }
    }

    public interface ICompanyLoanApplicationApprovalService
    {
        string ApproveApplicationAndCreateCredit(string applicationNr);

        string CreateCredit(string applicationNr);

        List<HistoricalCompanyLoanFinalDecisionBatchModel> FetchHistoricalDecisionBatches(DateTime fromDate, DateTime toDate);

        List<HistoricalCompanyLoanFinalDecisionBatchItemModel> FetchHistoricalDecisionBatchItems(int batchId);
    }

    public class HistoricalCompanyLoanFinalDecisionBatchModel
    {
        public int Id { get; set; }
        public DateTimeOffset ApprovedDate { get; set; }
        public int TotalCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class HistoricalCompanyLoanFinalDecisionBatchItemModel
    {
        public int Id { get; set; }
        public int HandlerUserId { get; set; }
        public string HandlerDisplayName { get; set; }
        public decimal Amount { get; set; }
        public string ApplicationNr { get; set; }
        public string CreditNr { get; set; }
        public string ApplicationUrl { get; set; }
        public string LoanUrl { get; set; }
        public string TypeName { get; set; }
    }
}