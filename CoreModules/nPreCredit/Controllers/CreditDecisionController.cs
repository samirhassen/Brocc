using nCredit.DbModel.BusinessEvents.NewCredit;
using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Banking.LoanModel;
using NTech.Core.PreCredit.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    [NTechAuthorizeCreditHigh]
    [RoutePrefix("CreditDecision")]
    public class CreditDecisionController : NController
    {
        private readonly IAdServiceIntegrationService adServiceIntegrationService;

        protected override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled && !NEnv.IsStandardUnsecuredLoansEnabled;

        public CreditDecisionController(IAdServiceIntegrationService adServiceIntegrationService)
        {
            this.adServiceIntegrationService = adServiceIntegrationService;
        }

        private IQueryable<CreditApplicationHeader> ApplicationsToApprove(PreCreditContext context)
        {
            var ct = CreditApplicationTypeCode.companyLoan.ToString();
            return context.CreditApplicationHeaders.Where(x => x.IsActive && x.IsPartiallyApproved && !x.IsFinalDecisionMade && x.MortgageLoanExtension == null && x.ApplicationType != ct);
        }

        private List<DecisionItem> FetchDecisionItems(ScoringSetupModel scoringModel, NTechNavigationTarget backNavigationTarget, List<string> onlyTheseApplicationNrs = null)
        {
            using (var context = new PreCreditContext())
            {
                var appsBase = ApplicationsToApprove(context);

                if (onlyTheseApplicationNrs != null)
                    appsBase = appsBase.Where(x => onlyTheseApplicationNrs.Contains(x.ApplicationNr));

                var applicationsReadyToApprove = appsBase
                    .Select(x => new
                    {
                        x.ApplicationNr,
                        x.CurrentCreditDecision,
                        CustomerIds = x
                            .Items.Where(y => y.Name == "customerId")
                            .Select(y => y.Value),
                        CreditNr = x.Items.Where(y => y.GroupName == "application" && y.Name == "creditnr").Select(y => y.Value).FirstOrDefault(),
                        HasCreditApplicationChangeLogItems = (context.CreditApplicationChangeLogItems.Any(y => x.ApplicationNr == y.ApplicationNr))
                    })
                    .ToList()
                    .Select(x =>
                    {
                        var ad = x.CurrentCreditDecision as AcceptedCreditDecision;

                        if (ad == null)
                            throw new Exception($"Trying to approve {x.ApplicationNr} which doesn't have an approved credit decision");

                        var newLoanOffer = CreditDecisionModelParser.ParseAcceptedNewCreditOffer(ad.AcceptedDecisionModel);
                        var additionalLoanOffer = CreditDecisionModelParser.ParseAcceptedAdditionalLoanOffer(ad.AcceptedDecisionModel);
                        if (newLoanOffer != null && additionalLoanOffer != null)
                            throw new Exception($"Application {x.ApplicationNr} has both a new loan offer and an additional loan offer");

                        if (newLoanOffer == null && additionalLoanOffer == null)
                            throw new Exception($"Application {x.ApplicationNr} has no offers");

                        var recommendation = CreditDecisionModelParser.ParseRecommendation(ad.AcceptedDecisionModel);

                        return new
                        {
                            x.ApplicationNr,
                            NewLoanOffer = newLoanOffer,
                            AdditionalLoanOffer = additionalLoanOffer,
                            SystemRecommendation = recommendation,
                            ApplicationCreditNr = x.CreditNr,
                            HandlerUserId = x.CurrentCreditDecision.DecisionById,
                            CustomerIds = x.CustomerIds.Select(y => int.Parse(y)),
                            HasCreditApplicationChangeLogItems = x.HasCreditApplicationChangeLogItems
                        };
                    })
                    .ToList();

                var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);

                var customerIdsWithActiveCheckpoints = ApplicationCheckpointService.GetCustomersWithActiveCheckpoints(
                    new HashSet<int>(applicationsReadyToApprove.SelectMany(x => x.CustomerIds)), NEnv.ClientCfgCore, customerClient);

                var allCustomerIds = applicationsReadyToApprove.SelectMany(x => x.CustomerIds).Distinct().ToList();
                var creditClient = new CreditClient();
                var historicalCredits = creditClient.GetCustomerCreditHistory(allCustomerIds);
                var handlerLimitEngine = DependancyInjection.Services.Resolve<HandlerLimitEngine>();

                var applications = applicationsReadyToApprove.Select(app =>
                {
                    var applicationNr = app.ApplicationNr;
                    var decsionByHandlerId = app.HandlerUserId;
                    string typeName;
                    string creditNr;
                    decimal amount;
                    bool isOverridingSystemRecommendation;
                    bool isOverridingGlobalLimit;
                    bool isOverridingHandlerLimit;
                    bool isOverridingOneApplicationRule;

                    var currentBalance = historicalCredits
                        .Where(x => x.CustomerIds.Intersect(app.CustomerIds).Any())
                        .Aggregate(0m, (x, y) => (x + y.CapitalBalance));

                    var r = app.SystemRecommendation;
                    if (app.NewLoanOffer != null)
                    {
                        //New loan
                        typeName = "NewLoan";
                        creditNr = app.ApplicationCreditNr;
                        amount = app.NewLoanOffer.amount.Value;
                        decsionByHandlerId = app.HandlerUserId;

                        //Overrides - Manual
                        isOverridingSystemRecommendation = !(
                            r.HasOffer
                            && r.OfferedAdditionalLoanCreditNr == null
                            && r.OfferedAmount == amount
                            && r.OfferedInterestRatePercent == app.NewLoanOffer.marginInterestRatePercent
                            && r.OfferedRepaymentTimeInMonths == app.NewLoanOffer.repaymentTimeInMonths
                            && r.OfferedInitialFeeAmount == app.NewLoanOffer.initialFeeAmount);
                    }
                    else if (app.AdditionalLoanOffer != null)
                    {
                        //Addional loan
                        typeName = "AdditionalLoan";
                        creditNr = app.AdditionalLoanOffer.creditNr;
                        amount = app.AdditionalLoanOffer.amount.Value;
                        decsionByHandlerId = app.HandlerUserId;

                        //Overrides - Manual
                        isOverridingSystemRecommendation = !(
                            r.HasOffer
                            && r.OfferedAdditionalLoanCreditNr == app.AdditionalLoanOffer.creditNr
                            && r.OfferedAmount == amount
                            && (!app.AdditionalLoanOffer.newMarginInterestRatePercent.HasValue || r.OfferedAdditionalLoanNewMarginInterestPercent == app.AdditionalLoanOffer.newMarginInterestRatePercent)
                            && (!app.AdditionalLoanOffer.newAnnuityAmount.HasValue || r.OfferedAdditionalLoanNewAnnuityAmount == app.AdditionalLoanOffer.newAnnuityAmount));
                    }
                    else
                        throw new NotImplementedException();

                    //Overrides - Handler limit
                    bool? _;
                    handlerLimitEngine.CheckHandlerLimits(amount, currentBalance, decsionByHandlerId, out isOverridingHandlerLimit, out _);

                    //Overrides - Global limit
                    isOverridingGlobalLimit = false;

                    //Overrides - Same applicant
                    var otherApplicationsWithSharedApplicant = applicationsReadyToApprove
                        .Where(x => x.CustomerIds.Intersect(app.CustomerIds).Any() && x.ApplicationNr != app.ApplicationNr)
                        .Select(x => x.ApplicationNr)
                        .ToList();
                    isOverridingOneApplicationRule = otherApplicationsWithSharedApplicant.Any();

                    var appCustomerIdsWithCheckpoint = customerIdsWithActiveCheckpoints.Intersect(app.CustomerIds).ToList();

                    return new DecisionItem
                    {
                        applicationNr = applicationNr,
                        amount = amount,
                        creditNr = creditNr,
                        applicationUrl = Url.Action("CreditApplication", "CreditManagement", new { applicationNr = applicationNr, backTarget = backNavigationTarget?.GetBackTargetOrNull() }),
                        loanUrl = creditNr == null ? null : NEnv.ServiceRegistry.External.ServiceUrl("nCredit", "Ui/Credit",
                            Tuple.Create("creditNr", creditNr),
                            Tuple.Create("backTarget", backNavigationTarget?.GetBackTargetOrNull())).ToString(),
                        typeName = typeName,
                        overrides = new OverrideItem[]
                                {
                                    isOverridingSystemRecommendation ? new OverrideItem { code = "systemRecommendation" } : null,
                                    isOverridingHandlerLimit ? new OverrideItem { code = "handlerLimit" } : null,
                                    isOverridingGlobalLimit ? new OverrideItem { code = "globalLimit" } : null,
                                    isOverridingOneApplicationRule ? new OverrideItem { code = "oneApplication", applicationNrs = otherApplicationsWithSharedApplicant } : null,
                                    appCustomerIdsWithCheckpoint.Any() ? new OverrideItem { code = "checkpoint" } : null,
                                    app.HasCreditApplicationChangeLogItems ? new OverrideItem { code = "decisionBasisEdited" } : null,
                                }
                                .Where(x => x != null)
                                .ToArray(),
                        handlerUserId = decsionByHandlerId,
                        handlerDisplayName = GetUserDisplayNameByUserId(decsionByHandlerId.ToString())
                    };
                });
                return applications.ToList();
            }
        }

        [Route("CreditApplicationsToApprove")]
        public ActionResult CreditApplicationsToApprove()
        {
            var applications = FetchDecisionItems(
                NEnv.ScoringSetup,
                NTechNavigationTarget.CreateCrossModuleNavigationTarget("UnsecuredCreditApplicationsToApprove", null));
            SetInitialData(new
            {
                applications = applications,
                approveUrl = Url.Action("CreateCredits")
            });
            return View();
        }

        private static List<string> S(params string[] args)
        {
            return args.ToList();
        }

        private static IEnumerable<IEnumerable<T>> SplitIntoGroupsOfN<T>(T[] array, int n)
        {
            for (var i = 0; i < (float)array.Length / n; i++)
            {
                yield return array.Skip(i * n).Take(n);
            }
        }

        [NTechApi]
        [HttpPost]
        [Route("CreateCredits")]
        public ActionResult CreateCredits(List<string> applicationNrs, bool? approveAllPending)
        {
            if (applicationNrs == null && approveAllPending == null)
                return new HttpStatusCodeResult(HttpStatusCode.OK);

            var now = Clock.Now;
            var repo = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>();

            int? creditApprovalBatchHeaderId = null;
            using (var context = new PreCreditContext())
            {
                if (approveAllPending.HasValue && approveAllPending.Value)
                {
                    applicationNrs = ApplicationsToApprove(context).Select(x => x.ApplicationNr).ToList();
                }

                if (applicationNrs.Count > 0)
                {
                    var h = new CreditApprovalBatchHeader
                    {
                        ApprovedById = CurrentUserId,
                        ApprovedDate = now,
                        ChangedById = CurrentUserId,
                        ChangedDate = now,
                        InformationMetaData = InformationMetadata,
                    };
                    context.CreditApprovalBatchHeaders.Add(h);
                    context.SaveChanges();
                    creditApprovalBatchHeaderId = h.Id;
                }
            }
            var scoringModel = NEnv.ScoringSetup;
            foreach (var applicationNrGroup in SplitIntoGroupsOfN(applicationNrs.ToArray(), 20))
            {
                var decisionApplications = FetchDecisionItems(scoringModel, null, onlyTheseApplicationNrs: applicationNrGroup.ToList()).ToDictionary(x => x.applicationNr);
                var kycQuestionCopyTasks = new List<KycQuestionCopyTask>();

                using (var context = new PreCreditContext())
                {
                    var tx = context.Database.BeginTransaction();
                    try
                    {
                        var newCreditRequests = new List<NewCreditRequest>();
                        var additionaLoanRequests = new List<NewAdditionalLoanRequest>();
                        var h = context.CreditApprovalBatchHeaders.Single(x => x.Id == creditApprovalBatchHeaderId.Value);
                        foreach (var nr in applicationNrGroup)
                        {
                            try
                            {
                                var ch = context.CreditApplicationHeaders.Include("CurrentCreditDecision").Single(x => x.ApplicationNr == nr);

                                if (ch.IsFinalDecisionMade)
                                    throw new Exception("Application has already been transferred to the credit module: " + ch.ApplicationNr);

                                if (!(ch.ApplicationType == null || ch.ApplicationType == CreditApplicationTypeCode.unsecuredLoan.ToString()))
                                    throw new Exception("Invalid application type");

                                //Get the credit decision
                                var creditDecision = ch.CurrentCreditDecision as AcceptedCreditDecision;
                                if (creditDecision == null)
                                    throw new Exception("Application has no approved credit decision: " + nr);

                                var offer = CreditDecisionModelParser.ParseAcceptedNewCreditOffer(creditDecision.AcceptedDecisionModel);
                                var additionalLoanOffer = CreditDecisionModelParser.ParseAcceptedAdditionalLoanOffer(creditDecision.AcceptedDecisionModel);

                                if (offer != null && additionalLoanOffer != null)
                                    throw new Exception($"Application {nr} has both a new loan offer and an additional loan offer");

                                if (offer == null && additionalLoanOffer == null)
                                    throw new Exception("Missing offer on Credit decsion");

                                if (offer != null)
                                {
                                    HandleNewCredit(now, repo, context, newCreditRequests, nr, ch, offer, kycQuestionCopyTasks);
                                }
                                else if (additionalLoanOffer != null)
                                {
                                    HandleAdditionalLoan(now, repo, context, additionaLoanRequests, nr, ch, additionalLoanOffer, kycQuestionCopyTasks);
                                }
                                else
                                    throw new NotImplementedException();

                                ch.IsActive = false;
                                ch.IsFinalDecisionMade = true;
                                ch.FinalDecisionById = CurrentUserId;
                                ch.FinalDecisionDate = now;

                                //Approval item
                                var decisionApp = decisionApplications[nr];
                                var ai = new CreditApprovalBatchItem
                                {
                                    ApplicationNr = nr,
                                    ApprovalType = decisionApp.typeName,
                                    ApprovedAmount = decisionApp.amount,
                                    ApprovedById = h.ApprovedById,
                                    ChangedById = h.ChangedById,
                                    ChangedDate = now,
                                    CreditApprovalBatch = h,
                                    CreditNr = decisionApp.creditNr,
                                    DecisionById = decisionApp.handlerUserId
                                };
                                ai.Overrides = decisionApp.overrides.Select(y => new CreditApprovalBatchItemOverride
                                {
                                    BatchItem = ai,
                                    ChangedById = h.ChangedById,
                                    CodeName = y.code,
                                    ContextData = JsonConvert.SerializeObject(new { applicationNrs = y.applicationNrs })
                                }).ToList();

                                context.CreditApprovalBatchItems.Add(ai);
                                context.CreditApprovalBatchItemOverrides.AddRange(ai.Overrides);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"CreateCredit failed on application {nr}", ex);
                            }
                        }

                        context.SaveChanges(); //Save to a db-serverside transaction to leave as small a windows as possible where nCredit and nPreCredit can disagree on the state

                        var c = new CreditClient();
                        c.CreateCredits(newCreditRequests.ToArray(), additionaLoanRequests.ToArray());

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }

                    var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
                    KycQuestionCopyService.CopyUnsecuredLoanKycQuestions(kycQuestionCopyTasks, customerClient);
                }
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        private void HandleAdditionalLoan(DateTimeOffset now, IPartialCreditApplicationModelRepository repo, PreCreditContext context, List<NewAdditionalLoanRequest> additionaLoanRequests, string nr, CreditApplicationHeader ch, CreditDecisionModelParser.AcceptedAdditionalCreditOffer additionalLoanOffer, List<KycQuestionCopyTask> kycQuestionCopyTasks)
        {
            if (additionalLoanOffer.amount.GetValueOrDefault() <= 0m)
                throw new Exception("Missing amount on on additional loan offer");
            if (string.IsNullOrWhiteSpace(additionalLoanOffer.creditNr))
                throw new Exception("Missing creditnr on additional loan offer");

            var appModel = repo.Get(
                nr,
                applicantFields: S("customerId"),
                applicationFields: S(
                    NEnv.ClientCfg.Country.BaseCountry == "SE" ? "bankaccountnr" : "iban",
                    "providerApplicationId",
                    "campaignCode"),
                documentFields: S("signed_initial_agreement_key")); //TODO: Rename key?

            var request = new NewAdditionalLoanRequest
            {
                AdditionalLoanAmount = additionalLoanOffer.amount.Value,
                CreditNr = additionalLoanOffer.creditNr,
                ApplicationNr = ch.ApplicationNr,
                Iban = NEnv.ClientCfg.Country.BaseCountry != "SE" ? appModel.Application.Get("iban").StringValue.Required : null,
                BankAccountNr = NEnv.ClientCfg.Country.BaseCountry == "SE" ? appModel.Application.Get("bankaccountnr").StringValue.Required : null,
                ProviderName = ch.ProviderName,
                ProviderApplicationId = appModel.Application.Get("providerApplicationId").StringValue.Optional,
                NewAnnuityAmount = additionalLoanOffer.newAnnuityAmount,
                NewMarginInterestRatePercent = additionalLoanOffer.newMarginInterestRatePercent,
                NewNotificationFeeAmount = additionalLoanOffer.newNotificationFeeAmount,
                Agreements = Enumerable.Range(1, appModel.NrOfApplicants).Select(applicantNr => new NewAdditionalLoanRequest.Agreement
                {
                    AgreementPdfArchiveKey = appModel.Document(applicantNr).Get("signed_initial_agreement_key").StringValue.Required,
                    CustomerId = appModel.Applicant(applicantNr).Get("customerId").IntValue.Required
                }).ToList(),
                CampaignCode = appModel.Application.Get("campaignCode").StringValue.Optional
            };

            additionaLoanRequests.Add(request);
            kycQuestionCopyTasks.Add(new KycQuestionCopyTask
            {
                CreditNr = additionalLoanOffer.creditNr,
                ApplicationNr = ch.ApplicationNr,
                ApplicationDate = ch.ApplicationDate.DateTime,
                CustomerIds = request.Agreements.Select(x => x.CustomerId).ToHashSetShared()
            });

            var comment = new CreditApplicationComment
            {
                ApplicationNr = ch.ApplicationNr,
                CommentText = $"Additional loan created",
                CommentById = CurrentUserId,
                ChangedById = CurrentUserId,
                CommentDate = now,
                ChangedDate = now,
                EventType = "ApplicationAdditionalLoanCreated",
                InformationMetaData = InformationMetadata
            };

            context.CreditApplicationComments.Add(comment);
        }

        private void HandleNewCredit(DateTimeOffset now, IPartialCreditApplicationModelRepository repo, PreCreditContext context, List<NewCreditRequest> newCreditRequests, string nr, CreditApplicationHeader ch, CreditDecisionModelParser.AcceptedNewCreditOffer offer, List<KycQuestionCopyTask> kycQuestionCopyTasks)
        {
            if (offer.amount.GetValueOrDefault() <= 0m)
                throw new Exception("Missing amount on Credit decision");
            if (offer.repaymentTimeInMonths.GetValueOrDefault() <= 0m)
                throw new Exception("Missing repaymentTimeInMonths on Credit decsion");
            if (offer.marginInterestRatePercent.GetValueOrDefault() <= 0m)
                throw new Exception("Missing marginInterestRatePercent on Credit decsion");

            var adServicesFields = adServiceIntegrationService.GetUsedExternalVariableNames();

            //Get the application
            var appModel = repo.Get(
                nr,
                applicantFields: S("customerId"),
                applicationFields: S(
                    NEnv.ClientCfg.Country.BaseCountry == "SE" ? "bankaccountnr" : "iban",
                    "creditnr",
                    "providerApplicationId",
                    "campaignCode"),
                externalFields: adServicesFields.ToList(),
                documentFields: S("signed_initial_agreement_key"));

            decimal annuityAmount;
            if (offer.annuityAmount.HasValue)
            {
                annuityAmount = offer.annuityAmount.Value;
            }
            else
            {
                var terms = PaymentPlanCalculation
                    .BeginCreateWithRepaymentTime(offer.amount.Value, offer.repaymentTimeInMonths.Value, offer.marginInterestRatePercent.Value + (offer.referenceInterestRatePercent ?? 0m), true, null, NEnv.CreditsUse360DayInterestYear)
                    .WithInitialFeeCapitalized(offer.initialFeeAmount.Value)
                    .WithMonthlyFee(offer.notificationFeeAmount.Value)
                    .EndCreate();
                annuityAmount = terms.AnnuityAmount;
            }

            var r = new NewCreditRequest
            {
                CreditNr = appModel.Application.Get("creditnr").StringValue.Required,
                Iban = NEnv.ClientCfg.Country.BaseCountry != "SE" ? appModel.Application.Get("iban").StringValue.Required : null,
                BankAccountNr = NEnv.ClientCfg.Country.BaseCountry == "SE" ? appModel.Application.Get("bankaccountnr").StringValue.Required : null,
                AnnuityAmount = annuityAmount,
                Applicants = Enumerable.Range(1, appModel.NrOfApplicants).Select(applicantNr => new NewCreditRequest.Applicant
                {
                    ApplicantNr = applicantNr,
                    AgreementPdfArchiveKey = appModel.Document(applicantNr).Get("signed_initial_agreement_key").StringValue.Required,
                    CustomerId = appModel.Applicant(applicantNr).Get("customerId").IntValue.Required
                }).ToList(),
                CapitalizedInitialFeeAmount = offer.initialFeeAmount.Value,
                CreditAmount = offer.amount.Value,
                NrOfApplicants = appModel.NrOfApplicants,
                MarginInterestRatePercent = offer.marginInterestRatePercent.Value,
                NotificationFee = offer.notificationFeeAmount.Value,
                ProviderName = ch.ProviderName,
                ProviderApplicationId = appModel.Application.Get("providerApplicationId").StringValue.Optional,
                ApplicationNr = ch.ApplicationNr,
                CampaignCode = appModel.Application.Get("campaignCode").StringValue.Optional,
                SourceChannel = adServicesFields.Any(x => appModel.External.Get(x).StringValue.HasValue) ? "adservices" : null
            };

            newCreditRequests.Add(r);
            kycQuestionCopyTasks.Add(new KycQuestionCopyTask
            {
                CreditNr = r.CreditNr,
                ApplicationNr = ch.ApplicationNr,
                ApplicationDate = ch.ApplicationDate.DateTime,
                CustomerIds = r.Applicants.Select(x => x.CustomerId).ToHashSetShared()
            });

            var comment = new CreditApplicationComment
            {
                ApplicationNr = ch.ApplicationNr,
                CommentText = "Credit created",
                CommentById = CurrentUserId,
                ChangedById = CurrentUserId,
                CommentDate = now,
                ChangedDate = now,
                EventType = "ApplicationCreditCreated",
                InformationMetaData = InformationMetadata
            };

            context.CreditApplicationComments.Add(comment);
        }

        private class DecisionItem
        {
            public int handlerUserId { get; set; }
            public string applicationNr { get; set; }
            public decimal amount { get; set; }
            public string creditNr { get; set; }
            public string applicationUrl { get; set; }
            public string loanUrl { get; set; }
            public string typeName { get; set; }
            public OverrideItem[] overrides { get; set; }
            public string handlerDisplayName { get; set; }
        }

        private class OverrideItem
        {
            public string code { get; set; }
            public List<string> applicationNrs { get; set; }
        }

        [HttpPost]
        [NTechApi]
        [Route("FindHistoricalDecisions")]
        public ActionResult FindHistoricalDecisionBatches(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing fromDate or toDate");

            var fd = fromDate.Value.Date;
            var td = toDate.Value.Date;
            if (td < fd || td.Subtract(fd) > TimeSpan.FromDays(180))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "toDate cannot be before fromDate and the max interval is 180 days");

            td = td.AddDays(1); //To handle ApprovedDate not being just date but date and time
            using (var context = new PreCreditContext())
            {
                var result = context
                    .CreditApprovalBatchHeaders
                    .Where(x => x.ApprovedDate >= fd && x.ApprovedDate < td)
                    .OrderByDescending(x => x.Id)
                    .Select(x => new
                    {
                        x.Id,
                        x.ApprovedDate,
                        TotalCount = x.Items.Count(),
                        TotalAmount = x.Items.Sum(y => (decimal?)y.ApprovedAmount) ?? 0m,
                        OverridesCount = x.Items.Where(y => y.Overrides.Any()).Count()
                    })
                    .ToList();
                return Json2(new { batches = result });
            }
        }

        [HttpPost]
        [NTechApi]
        [Route("GetBatchDetails")]
        public ActionResult FindHistoricalDecisionBatches(int? batchId)
        {
            if (!batchId.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing batchId");

            using (var context = new PreCreditContext())
            {
                var result = context
                    .CreditApprovalBatchHeaders
                    .Where(x => x.Id == batchId)
                    .Select(x => new
                    {
                        items = x.Items.Select(y => new
                        {
                            y.DecisionById,
                            y.ApplicationNr,
                            y.ApprovedAmount,
                            y.CreditNr,
                            y.ApprovalType,
                            Overrides = y.Overrides.Select(z => new
                            {
                                z.CodeName,
                                z.ContextData
                            })
                        })
                    })
                    .Single();
                Func<string, List<string>> parseOverrideApplicationNrs = s =>
                {
                    if (s == null)
                        return null;
                    return JsonConvert.DeserializeAnonymousType(s, new { applicationNrs = (List<string>)null })?.applicationNrs;
                };

                var batchItems = result.items.Select(x => new DecisionItem
                {
                    handlerUserId = x.DecisionById,
                    handlerDisplayName = GetUserDisplayNameByUserId(x.DecisionById.ToString()),
                    amount = x.ApprovedAmount,
                    applicationNr = x.ApplicationNr,
                    creditNr = x.CreditNr,
                    applicationUrl = Url.Action("CreditApplication", "CreditManagement", new { applicationNr = x.ApplicationNr }),
                    loanUrl = x.CreditNr == null ? null : NEnv.ServiceRegistry.External.ServiceUrl("nCredit", "Ui/Credit", Tuple.Create("creditNr", x.CreditNr)).ToString(),
                    typeName = x.ApprovalType,
                    overrides = x.Overrides.Select(y => new OverrideItem
                    {
                        code = y.CodeName,
                        applicationNrs = parseOverrideApplicationNrs(y.ContextData)
                    }).ToArray()
                });

                return Json2(new
                {
                    batchItems = batchItems
                });
            }
        }
    }
}