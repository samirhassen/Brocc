using NTech;
using NTech.Banking.PluginApis.AlterApplication;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared.Services;
using System;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class MortgageLoanApplicationAlterationService : IMortgageLoanApplicationAlterationService
    {
        private readonly IPublishEventService publishEventService;
        private readonly IMortgageLoanWorkflowService mortgageLoanWorkflowService;
        private readonly IClock clock;
        private readonly INTechCurrentUserMetadata currentUserMetadata;
        private readonly IComplexApplicationListService complexApplicationListService;
        private readonly ICustomerClient customerClient;

        public MortgageLoanApplicationAlterationService(
            IPublishEventService publishEventService,
            IMortgageLoanWorkflowService mortgageLoanWorkflowService,
            IClock clock,
            INTechCurrentUserMetadata currentUserMetadata,
            IComplexApplicationListService complexApplicationListService,
            ICustomerClient customerClient)
        {
            this.publishEventService = publishEventService;
            this.mortgageLoanWorkflowService = mortgageLoanWorkflowService;
            this.clock = clock;
            this.currentUserMetadata = currentUserMetadata;
            this.complexApplicationListService = complexApplicationListService;
            this.customerClient = customerClient;
        }

        public bool TryAlterApplication(AlterApplicationRequestModel request,
            out string failedMessage)
        {
            using (var context = new PreCreditContextExtended(currentUserMetadata, clock))
            {
                context.BeginTransaction();
                try
                {
                    var h = context
                        .CreditApplicationHeaders
                        .Include("Items")
                        .Include("ListMemberships")
                        .SingleOrDefault(x => x.ApplicationNr == request.ApplicationNr);

                    if (h == null)
                    {
                        failedMessage = "No such application exists";
                        return false;
                    }

                    if (request.EnsureConnectedCustomerIds != null && request.EnsureConnectedCustomerIds.Any())
                    {
                        var custs = context
                            .CreditApplicationHeaders
                            .Where(x => x.ApplicationNr == request.ApplicationNr)
                            .Select(x => new
                            {
                                CustomerIds1 = x.Items.Where(y => y.Name == "customerId").Select(y => y.Value),
                                CustomerIds2 = x.ComplexApplicationListItems.Where(y => y.IsRepeatable && y.ItemName == "customerIds").Select(y => y.ItemValue)
                            })
                            .Single();
                        var actualCustomerIds = custs.CustomerIds1.Concat(custs.CustomerIds2).Select(int.Parse).ToHashSet();
                        if (!request.EnsureConnectedCustomerIds.IsSubsetOf(actualCustomerIds))
                        {
                            failedMessage = "EnsureConnectedCustomerIds check failed";
                            return false;
                        }
                    }

                    if (request.ComplexApplicationItems != null && request.ComplexApplicationItems.Any())
                    {
                        throw new NotImplementedException();
                    }

                    if (request.AdditionalQuestionsDocument != null)
                    {
                        var answerDate = clock.Now.DateTime;
                        var customerSets = request
                            .AdditionalQuestionsDocument
                            .Items
                            .Where(x => x.CustomerId.HasValue)
                            .GroupBy(x => x.CustomerId.Value)
                            .Select(x => new CustomerQuestionsSet
                            {
                                AnswerDate = answerDate,
                                CustomerId = x.Key,
                                Source = "AdditionalQuestions",
                                Items = x.Select(y => new CustomerQuestionsSetItem
                                {
                                    AnswerCode = y.AnswerCode,
                                    AnswerText = y.AnswerText,
                                    QuestionCode = y.QuestionCode,
                                    QuestionText = y.QuestionText
                                }).ToList()
                            });
                        foreach (var s in customerSets)
                        {
                            customerClient.AddCustomerQuestionsSet(s, "MortgageLoanApplication", request.ApplicationNr);
                        }
                    }

                    var curentStepName = mortgageLoanWorkflowService.GetCurrentListName(h.ListMemberships.Select(x => x.ListName));

                    if (request.ApplicationItems != null && request.ApplicationItems.Any())
                    {
                        context.AddOrUpdateCreditApplicationItems(h,
                            request.ApplicationItems.Select(x => new PreCreditContextExtended.CreditApplicationItemModel
                            {
                                GroupName = x.GroupName,
                                IsEncrypted = x.IsEncrypted,
                                Name = x.ItemName,
                                Value = x.Value
                            }).ToList(), curentStepName);
                    }

                    context.SaveChanges();

                    context.CommitTransaction();
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }

            failedMessage = null;
            return true;
        }
    }

    public interface IMortgageLoanApplicationAlterationService
    {
        bool TryAlterApplication(AlterApplicationRequestModel request,
            out string failedMessage);
    }
}