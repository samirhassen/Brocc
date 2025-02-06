using nPreCredit.Code.Services.SharedStandard;
using nPreCredit.WebserviceMethods.SharedStandard;
using NTech.Banking.PluginApis.CreateApplication;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class SharedCreateApplicationService
    {
        private readonly IPreCreditContextFactoryService preCreditContextFactoryService;
        private readonly IPreCreditEnvSettings preCreditEnvSettings;
        private readonly EncryptionService encryptionService;
        private readonly IDocumentClient documentClient;
        private readonly ICampaignCodeService campaignCodeService;
        private readonly CreditApplicationCustomerListService customerListMemberService;
        private readonly ILoanStandardCustomerRelationService loanStandardCustomerRelationService;

        public SharedCreateApplicationService(
            IPreCreditContextFactoryService preCreditContextFactoryService,
            IPreCreditEnvSettings preCreditEnvSettings,
            EncryptionService encryptionService,
            IDocumentClient documentClient,
            ICampaignCodeService campaignCodeService,
            CreditApplicationCustomerListService customerListMemberService,
            ILoanStandardCustomerRelationService loanStandardCustomerRelationService)
        {
            this.preCreditContextFactoryService = preCreditContextFactoryService;
            this.preCreditEnvSettings = preCreditEnvSettings;
            this.encryptionService = encryptionService;
            this.documentClient = documentClient;
            this.campaignCodeService = campaignCodeService;
            this.customerListMemberService = customerListMemberService;
            this.loanStandardCustomerRelationService = loanStandardCustomerRelationService;
        }

        public SharedCreateApplicationResponse CreateApplication(
            CreateApplicationRequestModel request,
            CreditApplicationTypeCode creditApplicationTypeCode,
            ISharedWorkflowService workflowService,
            CreditApplicationEventCode creationEventTypeCode)
        {

            SharedCreateApplicationResponse response;
            CreateApplicationRequestModelExtended extendedRequest;
            CreditApplicationHeader newCredit;

            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                context.BeginTransaction();
                try
                {

                    var h = new CreditApplicationHeader
                    {
                        ProviderName = request.ProviderName,
                        ApplicationNr = request.ApplicationNr,
                        ApplicationType = creditApplicationTypeCode.ToString(),
                        NrOfApplicants = request.NrOfApplicants,
                        ChangedById = context.CurrentUserId,
                        ChangedDate = context.CoreClock.Now,
                        ApplicationDate = request.ApplicationDate ?? context.CoreClock.Now,
                        IsActive = true,
                        AgreementStatus = CreditApplicationMarkerStatusName.Initial,
                        CreditCheckStatus = CreditApplicationMarkerStatusName.Initial,
                        FraudCheckStatus = CreditApplicationMarkerStatusName.Initial,
                        CustomerCheckStatus = CreditApplicationMarkerStatusName.Initial,
                        IsFinalDecisionMade = false,
                        InformationMetaData = context.InformationMetadata,
                        Items = new List<CreditApplicationItem>(),
                        SearchTerms = new List<CreditApplicationSearchTerm>(),
                        HideFromManualListsUntilDate = request.HideFromManualListsUntilDate,
                        CanSkipAdditionalQuestions = false
                    };

                    var newComplexApplicationItems = new Lazy<List<CreateApplicationRequestModel.ComplexItem>>();
                    if (request.ComplexApplicationItems != null && request.ComplexApplicationItems.Any())
                    {
                        newComplexApplicationItems.Value.AddRange(request.ComplexApplicationItems);
                    }

                    var wasCreatedAsLead = false;
                    if (creditApplicationTypeCode == CreditApplicationTypeCode.mortgageLoan && !preCreditEnvSettings.IsStandardMortgageLoansEnabled)
                    {
                        var m = context.FillInfrastructureFields(new MortgageLoanCreditApplicationHeaderExtension());
                        m.CustomerOfferStatus = MortgageLoanCustomerOfferStatusCode.Initial.ToString();
                        m.DocumentCheckStatus = MortgageLoanDocumentCheckStatus.Initial.ToString();
                        m.Application = h;
                        context.AddMortgageLoanCreditApplicationHeaderExtensions(m);

                        var useLeads = request.UseLeads ?? (preCreditEnvSettings.GetAffiliateModel(request.ProviderName, allowMissing: true)?.UseLeads ?? false);
                        if (useLeads)
                        {
                            newComplexApplicationItems.Value.Add(new CreateApplicationRequestModel.ComplexItem
                            {
                                ListName = ApplicationInfoService.MortgageLoanLeadsComplexListName,
                                Nr = 1,
                                UniqueValues = new Dictionary<string, string>
                            {
                                { "exists", "true" }, //Javascript helper to find which nrs are defined
                                { "IsLead", "true" }
                            }
                            });
                            wasCreatedAsLead = true;
                        }
                    }

                    if (newComplexApplicationItems.IsValueCreated)
                    {
                        newComplexApplicationItems.Value.AddRange(
                            campaignCodeService.MatchCampaignOnCreateApplication(newComplexApplicationItems.Value));
                    }

                    context.AddCreditApplicationHeaders(h);

                    var evt = context.CreateAndAddEvent(creationEventTypeCode, null, h);

                    if (!request.ApplicationItems.Any(x => x.ItemName == "workflowVersion") && !preCreditEnvSettings.IsStandardUnsecuredLoansEnabled)
                    {
                        request.AddApplicationItem("workflowVersion", workflowService.Version.ToString(), groupName: "application");
                    }

                    foreach (var i in request.ApplicationItems.Where(x => !string.IsNullOrWhiteSpace(x.Value)))
                    {
                        h.Items.Add(new CreditApplicationItem
                        {
                            ApplicationNr = request.ApplicationNr,
                            Name = i.ItemName,
                            GroupName = i.GroupName,
                            Value = i.Value,
                            IsEncrypted = i.IsEncrypted,
                            ChangedById = context.CurrentUserId,
                            ChangedDate = h.ChangedDate,
                            AddedInStepName = "Initial",
                            InformationMetaData = context.InformationMetadata
                        });
                    }

                    extendedRequest = request as CreateApplicationRequestModelExtended;
                    if (extendedRequest != null)
                    {
                        if (preCreditEnvSettings.IsStandardUnsecuredLoansEnabled || preCreditEnvSettings.IsStandardMortgageLoansEnabled)
                        {
                            extendedRequest.SetUniqueComplexApplicationItem("Application", 1, "workflowVersion", workflowService.Version.ToString());
                        }

                        foreach (var customerList in extendedRequest.CustomerListMembers)
                        {
                            foreach (var customerId in customerList.Value)
                            {
                                customerListMemberService.SetMemberStatusComposable(context, customerList.Key, true, customerId, application: h, evt: evt);
                            }
                        }
                    }

                    if (newComplexApplicationItems.IsValueCreated && newComplexApplicationItems.Value.Any())
                    {
                        var ops = new List<ComplexApplicationListOperation>();
                        foreach (var i in newComplexApplicationItems.Value)
                        {
                            if (i.RepeatingValues != null)
                            {
                                foreach (var r in i.RepeatingValues.Where(x => x.Value != null && x.Value.Count != 0))
                                {
                                    ops.Add(new ComplexApplicationListOperation
                                    {
                                        ApplicationNr = request.ApplicationNr,
                                        IsDelete = false,
                                        Nr = i.Nr,
                                        ItemName = r.Key,
                                        ListName = i.ListName,
                                        RepeatedValue = r.Value,
                                        UniqueValue = null
                                    });
                                }
                            }
                            if (i.UniqueValues != null)
                            {
                                foreach (var r in i.UniqueValues.Where(x => !string.IsNullOrWhiteSpace(x.Value)))
                                {
                                    ops.Add(new ComplexApplicationListOperation
                                    {
                                        ApplicationNr = request.ApplicationNr,
                                        IsDelete = false,
                                        Nr = i.Nr,
                                        ItemName = r.Key,
                                        RepeatedValue = null,
                                        ListName = i.ListName,
                                        UniqueValue = r.Value
                                    });
                                }
                            }
                        }
                        if (ops.Any())
                            ComplexApplicationListService.ChangeListComposable(ops, context, getEvent: _ => evt);
                    }

                    var encryptedItems = h.Items.Where(x => x.IsEncrypted).ToArray();

                    if (encryptedItems.Any())
                    {
                        encryptionService.SaveEncryptItems(encryptedItems, x => x.Value, (x, v) => x.Value = v.ToString(), context);
                    }

                    if (request.ApplicationComment != null)
                    {
                        var c = request.ApplicationComment;
                        var creationMessage = c.Text ?? $"{(wasCreatedAsLead ? "Lead" : "Application")} created";

                        var attachment = c.CustomerIpAddress == null ? null : CommentAttachment.CreateMetadataOnly(requestIpAddress: c.CustomerIpAddress);

                        if (!ApplicationCommentHelper.TryCreateCommentWithAttachment(request.ApplicationNr, creationMessage, evt.EventType, attachment, documentClient, context, out var fm, out var createdComment))
                            throw new Exception(fm);

                        context.AddCreditApplicationComments(createdComment);
                    }

                    workflowService.ChangeStepStatusComposable(context, workflowService.GetStepOrder().First(), workflowService.InitialStatusName, application: h, evt: evt);

                    context.SaveChanges();
                    context.CommitTransaction();

                    response = new SharedCreateApplicationResponse
                    {
                        ApplicationNr = h.ApplicationNr
                    };
                    newCredit = h;
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }

            ReplicateToCustomerModule(extendedRequest, newCredit);

            return response;
        }

        private void ReplicateToCustomerModule(CreateApplicationRequestModelExtended request, CreditApplicationHeader h)
        {
            if (request == null)
                return;

            var customerIds = request.CustomerListMembers["Applicant"];

            loanStandardCustomerRelationService.AddNewApplication(customerIds, h.ApplicationNr, h.ApplicationDate);
        }
    }
}