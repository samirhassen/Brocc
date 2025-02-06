using nCredit.Code;
using nCredit.DbModel.DomainModel;
using nCredit.DbModel.Repository;
using nCredit.DomainModel;
using NTech.Core.Credit.Shared.Services;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechAuthorizeCreditMiddle]
    public class PreCollectionController : NController
    {
        protected override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled && !NEnv.IsStandardUnsecuredLoansEnabled;

        [HttpGet]
        [Route("Ui/PreCollection/WorkList")]
        public ActionResult WorkList(int? testUserId)
        {
            return RedirectToAction("WorkLists", new { testUserId });
        }

        [HttpGet]
        [Route("Ui/PreCollection/PhoneList")]
        public ActionResult PhoneList()
        {
            var date = Clock.Today;

            SetInitialData(new
            {
                today = date,
                workListUrl = Url.Action("WorkList", new { })
            });

            return View();
        }

        private class WorkListStatusModel
        {
            public int WorkListHeaderId { get; set; }
            public int? ClosedByUserId { get; set; }
            public DateTime CreationDate { get; set; }
            public int CreatedByUserId { get; set; }
            public DateTime? ClosedDate { get; set; }
            public int TotalCount { get; set; }
            public int CompletedCount { get; set; }
            public string CurrentUserActiveItemId { get; set; }
            public int TakenCount { get; set; }
            public int TakeOrCompletedByCurrentUserCount { get; set; }
            public bool IsTakePossible { get; set; }
        }

        private IQueryable<WorkListStatusModel> GetWorkListStatusModels(CreditContext context, int userId)
        {
            return context
                    .WorkListHeaders
                    .Select(x => new WorkListStatusModel
                    {
                        WorkListHeaderId = x.Id,
                        CreationDate = x.CreationDate,
                        CreatedByUserId = x.CreatedByUserId,
                        ClosedByUserId = x.ClosedByUserId,
                        ClosedDate = x.ClosedDate,
                        TotalCount = x.Items.Count(),
                        TakenCount = x.Items.Where(y => y.TakenByUserId.HasValue && !y.CompletedDate.HasValue).Count(),
                        CompletedCount = x.Items.Where(y => y.TakenByUserId.HasValue && y.CompletedDate.HasValue).Count(),
                        TakeOrCompletedByCurrentUserCount = x.Items.Where(y => y.TakenByUserId == userId).Count(),
                        CurrentUserActiveItemId = x
                            .Items
                            .Where(y => y.TakenByUserId == userId && !y.CompletedDate.HasValue)
                            .Select(y => y.ItemId)
                            .FirstOrDefault(),
                        IsTakePossible = !x.ClosedByUserId.HasValue && x.Items.Any(y => !y.TakenByUserId.HasValue)
                    });
        }

        private List<ExpandoObject> GetWorklistsModel(CreditContext context, int userId)
        {
            var today = Clock.Today;
            var tomorrow = Clock.Today.AddDays(1);

            var visibleWorklists = context
                .WorkListHeaders
                .Where(x =>
                    !x.IsUnderConstruction
                    && !x.ClosedByUserId.HasValue
                    && x.ListType == "PreCollection1")
                .Select(x => new { x.Id, x.ClosedByUserId })
                .ToList();

            var activeWorklistIds = visibleWorklists.Where(x => !x.ClosedByUserId.HasValue).Select(x => x.Id).ToList();
            var allIds = visibleWorklists.Select(x => x.Id).ToList();

            var workListSummaries = GetWorkListStatusModels(context, userId)
                .Where(x => activeWorklistIds.Contains(x.WorkListHeaderId))
                .ToList()
                .ToDictionary(x => x.WorkListHeaderId, x => x);

            //Only include filter state on open lists to limit how far back we need to support the data model
            var filterSummaries = context
                .WorkListHeaders
                .Where(x => activeWorklistIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.CustomData,
                    Filters = x.FilterItems.Select(y => new
                    {
                        y.Name,
                        y.Value
                    }),
                    ItemCountByPassedDueDates = x
                        .Items
                        .SelectMany(y => y.Properties)
                        .Where(y => y.Name == "NrOfPassedDueDatesWithoutFullPaymentSinceNotification")
                        .GroupBy(y => y.Value)
                        .Select(y => new
                        {
                            NrOfPassedDueDatesWithoutFullPaymentSinceNotification = y.Key,
                            Count = y.Count()
                        })
                })
                .ToList()
                .ToDictionary(x => x.Id, d => new
                {
                    Filters = d.Filters.ToList(),
                    FilterDataNrOfDueDatesPassed = d
                        .ItemCountByPassedDueDates
                        .Select(x => new
                        {
                            NrOfPassedDueDatesWithoutFullPaymentSinceNotification = int.Parse(x.NrOfPassedDueDatesWithoutFullPaymentSinceNotification),
                            Count = x.Count
                        })
                        .ToList()
                });

            var worklists = new List<ExpandoObject>();
            foreach (var s in workListSummaries.OrderBy(x => x.Key))
            {
                var e = new ExpandoObject();
                var ed = e as IDictionary<string, object>;
                ed["workListSummary"] = s.Value;
                if (filterSummaries.ContainsKey(s.Key))
                {
                    ed["filterSummary"] = filterSummaries[s.Key];
                }

                worklists.Add(e);
            }
            return worklists;
        }

        [HttpGet]
        [Route("Ui/PreCollection/WorkLists")]
        public ActionResult WorkLists(int? testUserId)
        {
            using (var context = new CreditContext())
            {
                var userId = !NEnv.IsProduction && testUserId.HasValue ? testUserId.Value : CurrentUserId;

                SetInitialData(new
                {
                    userId = userId,
                    today = Clock.Today.ToString("yyyy-MM-dd"),
                    loadCommentsUrl = Url.Action("LoadForCredit", "ApiCreditComments"),
                    createCommentUrl = Url.Action("Create", "ApiCreditComments"),
                    takeWorkListItemUrl = Url.Action("TryTakeWorkListItem", "ApiWorkList"),
                    replaceWorkListItemUrl = Url.Action("TryReplaceWorkListItem", "ApiWorkList"),
                    openWorkListItemUrl = Url.Action("OpenWorkListItem"),
                    completeWorkListItemUrl = Url.Action("CompleteWorkListItem"),
                    closeWorkListUrl = Url.Action("CloseWorkListApi"),
                    workListPortalUrl = Url.Action("WorkList", new { testUserId }),
                    phoneListUrl = Url.Action("PhoneList", new { testUserId }),
                    customerCardUrlPattern = CreditCustomerClient.GetCustomerCardUri(99999).ToString().Replace("99999", "NNNNN"),
                    testUserId = testUserId,
                    calculateWorkListUrl = Url.Action("CalculateWorkList"),
                    createWorkListUrl = Url.Action("CreateWorkListApi"),
                    worklists = GetWorklistsModel(context, userId),
                    isAlternatePaymentPlansEnabled = NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.paymentplan")
                });
                return View();
            }
        }

        private List<PreCollectionCandidateModel> GetWorkListData(CreditContext context, List<string> nrOfDueDatesPassedFilter, 
            bool filterOutCreditsWithActiveTermChanges,
            bool includeActiveAlternatePaymentPlans)
        {
            var date = Clock.Today;

            var baseResult = GetPreCollectionCandidateModel(context);

            Expression<Func<PreCollectionCandidateModel, bool>> nrOfDueDatesPassedFilterPredicate = null;

            if (nrOfDueDatesPassedFilter != null && nrOfDueDatesPassedFilter.Count > 0)
            {
                nrOfDueDatesPassedFilterPredicate = x => false;
                foreach (var f in nrOfDueDatesPassedFilter)
                {
                    if (f.EndsWith("+"))
                    {
                        var nr = int.Parse(f.Substring(0, f.Length - 1));
                        nrOfDueDatesPassedFilterPredicate = nrOfDueDatesPassedFilterPredicate.Or(x => x.NrOfPassedDueDatesWithoutFullPaymentSinceNotification >= nr);
                    }
                    else
                    {
                        var nr = int.Parse(f);
                        nrOfDueDatesPassedFilterPredicate = nrOfDueDatesPassedFilterPredicate.Or(x => x.NrOfPassedDueDatesWithoutFullPaymentSinceNotification == nr);
                    }
                }
            }

            if (nrOfDueDatesPassedFilterPredicate != null)
            {
                baseResult = baseResult.Where(nrOfDueDatesPassedFilterPredicate);
            }

            if (!includeActiveAlternatePaymentPlans)
            {
                baseResult = baseResult.Where(x => !x.HasActiveAlternatePaymentPlan);
            }

            var today = Clock.Today.Date;
            var tomorrow = today.AddDays(1);
            var creditsWithTryLaterToday = new HashSet<string>(context
                .CreditComments
                .Where(x => x.EventType.StartsWith(TryAgainLaterType) && x.CommentDate >= today && x.CommentDate < tomorrow)
                .Select(x => x.CreditNr)
                .ToList());

            var r = baseResult
                .ToList()
                .Where(x =>
                    //Förfallet belopp måste vara större än 10 €
                    x.BalanceUnpaidOverdueNotifications > 10

                    //Lånet får inte ha ett Promise to pay datum satt idag och framåt
                    && (!(x.PromisedToPayDate.HasValue && x.PromisedToPayDate.Value.Date >= Clock.Today.Date))

                    //Lånet får inte ha ett ’Expected to settle date’
                    && (!(x.ExpectedSettlementDate.HasValue && x.ExpectedSettlementDate.Value.Date >= Clock.Today.Date))

                    //Lånet får inte ha Pending change terms(Precollection - Pending change terms)
                    && (!(filterOutCreditsWithActiveTermChanges && x.HasActiveTermsChange))

                    //Lånet får inte ha Try later satt idag. Ska utökas med fler variabler. (Precollection: try later - sms, try later - email).
                    && (!creditsWithTryLaterToday.Contains(x.CreditNr))
                    )
                .ToList();

            return r;
        }

        private class PreCollection1ItemModel
        {
            public string CreditNr { get; internal set; }
            public int NrOfDaysOverdue { get; internal set; }
            public int NrUnpaidOverdueNotifications { get; internal set; }
            public decimal BalanceUnpaidOverdueNotifications { get; internal set; }
            public int NrOfPassedDueDatesWithoutFullPaymentSinceNotification { get; internal set; }
        }

        private WorkListRepository CreateRepository(int actualUserId) => new WorkListRepository(actualUserId, this.Clock, this.InformationMetadata, Service.DocumentClientHttpContext);

        [NTechApi]
        [HttpPost]
        [Route("Api/PreCollection/CloseWorkList")]
        public ActionResult CloseWorkListApi(int workListHeaderId, int? userId, bool? includeWorkListsInResponse)
        {
            var actualUserId = !NEnv.IsProduction && userId.HasValue ? userId.Value : CurrentUserId;

            var repo = CreateRepository(actualUserId);

            var wasClosed = repo.TryCloseWorkList(workListHeaderId);

            List<ExpandoObject> worklists = null;

            if (includeWorkListsInResponse.GetValueOrDefault())
            {
                using (var context = new CreditContext())
                {
                    worklists = GetWorklistsModel(context, actualUserId);
                }
            }

            return Json2(new { wasClosed = wasClosed, worklists = worklists });
        }

        [NTechApi]
        [HttpPost]
        [Route("Api/PreCollection/PreviewWorkListCreditNrs")]
        public ActionResult PreviewWorkListCreditNrs(List<string> nrOfDueDatesPassedFilter, bool? includeActiveAlternatePaymentPlans)
        {
            using (var context = new CreditContext())
            {
                var items = GetWorkListData(context, nrOfDueDatesPassedFilter, 
                    filterOutCreditsWithActiveTermChanges: false,
                    includeActiveAlternatePaymentPlans: includeActiveAlternatePaymentPlans.GetValueOrDefault());
                return Json2(new { creditNrs = items.Select(x => x.CreditNr).ToList() });
            }
        }

        [NTechApi]
        [HttpPost]
        [Route("Api/PreCollection/CreateWorkList")]
        public ActionResult CreateWorkListApi(List<string> nrOfDueDatesPassedFilter, int? testUserId, bool? includeWorkListsInResponse, bool? includeActiveAlternatePaymentPlans)
        {
            var date = Clock.Today;
            List<PreCollection1ItemModel> items;
            using (var context = new CreditContext())
            {
                items = GetWorkListData(context, nrOfDueDatesPassedFilter, 
                    filterOutCreditsWithActiveTermChanges: true,
                    includeActiveAlternatePaymentPlans: includeActiveAlternatePaymentPlans.GetValueOrDefault())
                        //Antal dagar i förfall
                        .OrderByDescending(x => x.NrOfDaysOverdue)
                        //de som aldrig betala någonting(totalt sett på hela lånet)
                        .ThenBy(x => x.LatestPaymentDate.HasValue)
                        //total kapital skuld
                        .ThenByDescending(x => x.TotalCapitalDebt)
                        //minst antal aviseringar gjorda
                        .ThenBy(x => x.NrOfNotifications)
                        //sist kreditnummer
                        .ThenBy(x => x.CreditNr)
                        .Select(x => new PreCollection1ItemModel
                        {
                            CreditNr = x.CreditNr,
                            NrOfDaysOverdue = x.NrOfDaysOverdue,
                            NrUnpaidOverdueNotifications = x.NrUnpaidOverdueNotifications,
                            BalanceUnpaidOverdueNotifications = x.BalanceUnpaidOverdueNotifications,
                            NrOfPassedDueDatesWithoutFullPaymentSinceNotification = x.NrOfPassedDueDatesWithoutFullPaymentSinceNotification
                        })
                        .ToList();
            }

            var filterItems = new List<WorkListRepository.WorkListCreateRequest.FilterItem>();
            if (nrOfDueDatesPassedFilter != null && nrOfDueDatesPassedFilter.Count > 0)
                filterItems.Add(new WorkListRepository.WorkListCreateRequest.FilterItem
                {
                    Name = "NrOfDueDatesPassed",
                    Value = string.Join(",", nrOfDueDatesPassedFilter)
                });

            var repo = CreateRepository(CurrentUserId);
            var workListId = repo.BeginCreate(new WorkListRepository.WorkListCreateRequest
            {
                ListType = "PreCollection1",
                CustomData = new WorkListRepository.WorkListCustomDataV1
                {
                    FilterDescriptors = new List<WorkListRepository.WorkListCustomDataV1.FilterDescriptor>
                        {
                                new WorkListRepository.WorkListCustomDataV1.FilterDescriptor
                                {
                                    Name = "NrOfDueDatesPassed",
                                    DisplayName = "Months overdue"
                                }
                        },
                    PropertyDescriptors = new List<WorkListRepository.WorkListCustomDataV1.PropertyDescriptor>
                        {
                            new WorkListRepository.WorkListCustomDataV1.PropertyDescriptor
                            {
                                Name = "ItemId",
                                DisplayName = "Loan nr",
                                DataTypeName = typeof(string).Name
                            },
                            new WorkListRepository.WorkListCustomDataV1.PropertyDescriptor
                            {
                                Name = "NrUnpaidOverdueNotifications",
                                DisplayName = "Overdue count (creation date)",
                                DataTypeName = typeof(int).Name
                            },
                            new WorkListRepository.WorkListCustomDataV1.PropertyDescriptor
                            {
                                Name = "NrOfDaysOverdue",
                                DisplayName = "Overdue days (creation date)",
                                DataTypeName = typeof(int).Name
                            },
                            new WorkListRepository.WorkListCustomDataV1.PropertyDescriptor
                            {
                                Name = "NrOfPassedDueDatesWithoutFullPaymentSinceNotification",
                                DisplayName = "Passed duedate count (creation date)",
                                DataTypeName = typeof(int).Name
                            },
                            new WorkListRepository.WorkListCustomDataV1.PropertyDescriptor
                            {
                                Name = "BalanceUnpaidOverdueNotifications",
                                DisplayName = "Overdue balance (creation date)",
                                DataTypeName = typeof(decimal).Name
                            }
                        }
                },
                FilterItems = filterItems
            });

            repo.TryAddItems(new WorkListRepository.AddItemsToWorkListRequest
            {
                WorkListId = workListId,
                Items = items.Select(x => new WorkListRepository.AddItemsToWorkListRequest.Item
                {
                    ItemId = x.CreditNr,
                    Properties = new List<WorkListRepository.ItemProperty>
                            {
                                WorkListRepository.ItemProperty.Create("NrOfDaysOverdue", x.NrOfDaysOverdue),
                                WorkListRepository.ItemProperty.Create("NrUnpaidOverdueNotifications", x.NrUnpaidOverdueNotifications),
                                WorkListRepository.ItemProperty.Create("BalanceUnpaidOverdueNotifications", x.BalanceUnpaidOverdueNotifications),
                                WorkListRepository.ItemProperty.Create("NrOfPassedDueDatesWithoutFullPaymentSinceNotification", x.NrOfPassedDueDatesWithoutFullPaymentSinceNotification)
                            }
                }).ToList()
            });

            repo.TryEndCreate(workListId);

            List<ExpandoObject> worklists = null;

            if (includeWorkListsInResponse.GetValueOrDefault())
            {
                var userId = !NEnv.IsProduction && testUserId.HasValue ? testUserId.Value : CurrentUserId;
                using (var context = new CreditContext())
                {
                    worklists = GetWorklistsModel(context, userId);
                }
            }

            return Json2(new
            {
                workListHeaderId = workListId,
                workListUrl = Url.Action("HandleWorkList", new { workListHeaderId = workListId, testUserId }),
                worklists
            });
        }

        [HttpPost]
        [Route("Api/PreCollection/CalculateWorkList")]
        public ActionResult CalculateWorkList(List<string> nrOfDueDatesPassedFilter, bool? includeActiveAlternatePaymentPlans)
        {
            var date = Clock.Today;
            using (var context = new CreditContext())
            {
                var baseResult = GetWorkListData(context, nrOfDueDatesPassedFilter, 
                    filterOutCreditsWithActiveTermChanges: true, 
                    includeActiveAlternatePaymentPlans: includeActiveAlternatePaymentPlans.GetValueOrDefault());

                var result = baseResult
                    .GroupBy(x => x.NrOfPassedDueDatesWithoutFullPaymentSinceNotification)
                    .Select(x => new
                    {
                        NrOfPassedDueDatesWithoutFullPaymentSinceNotification = x.Key,
                        Count = x.Count()
                    })
                    .ToList();

                return Json2(new
                {
                    totalCount = result.Aggregate(0, (x, y) => x + y.Count),
                    countByNrOfPassedDueDatesWithoutFullPaymentSinceNotification = result.ToDictionary(x => x.NrOfPassedDueDatesWithoutFullPaymentSinceNotification.ToString(), x => x.Count)
                });
            }
        }

        public class ActionTaken
        {
            public bool IsSkipped { get; set; }
            public bool HadFuturePromisedToPayDate { get; set; }
            public bool IsSettlementDateAdded { get; set; }
            public bool IsNewTermsSent { get; set; }
            public string TryLaterChoice { get; set; }
        }

        [NTechApi]
        [HttpPost]
        [Route("Api/PreCollection/CompleteWorkListItem")]
        public ActionResult CompleteWorkListItem(int workListHeaderId, string itemId, int? userId, ActionTaken actionTaken)
        {
            var actualUserId = NEnv.IsProduction ? CurrentUserId : (userId ?? CurrentUserId);
            var repo = CreateRepository(CurrentUserId);
            var wasCompleted = repo.TryCompleteWorkListItem(workListHeaderId, itemId);
            if (wasCompleted)
            {
                using (var context = new CreditContext())
                {
                    string commentText = null;
                    string commentType = null;
                    if (actionTaken?.TryLaterChoice != null)
                    {
                        commentType = TryAgainLaterType;
                        commentText = "Precollection - will try again later";
                        if (actionTaken.TryLaterChoice == "tryLaterPlusSms")
                        {
                            commentText += " + sms sent manually";
                            commentType += "_PlusManualSms";
                        }
                        else if (actionTaken.TryLaterChoice == "tryLaterPlusEmail")
                        {
                            commentText += " + email sent manually";
                            commentType += "_PlusManualEmail";
                        }
                    }
                    else if (actionTaken?.IsSkipped ?? false)
                    {
                        commentType = SkippedType;
                        commentText = "Precollection - skipped";
                    }
                    if (commentText != null)
                    {
                        context.CreditComments.Add(new CreditComment
                        {
                            CreditNr = itemId,
                            CommentText = commentText,
                            EventType = commentType,
                            ChangedById = this.CurrentUserId,
                            CommentById = this.CurrentUserId,
                            ChangedDate = Clock.Now,
                            CommentDate = Clock.Now,
                            InformationMetaData = this.InformationMetadata
                        });
                        context.SaveChanges();
                    }
                }
            }
            return Json2(new
            {
                wasCompleted = wasCompleted
            });
        }

        [NTechApi]
        [HttpPost]
        [Route("Api/PreCollection/OpenWorkListItem")]
        public ActionResult OpenWorkListItem(int workListHeaderId, string itemId, int? userId)
        {
            var actualUserId = NEnv.IsProduction ? CurrentUserId : (userId ?? CurrentUserId);
            var paymentOrder = Service.PaymentOrder.GetPaymentOrderItems();

            var applicants = new List<IDictionary<string, string>>();
            using (var context = CreateCreditContext())
            {
                var workListSummaryModels = GetWorkListStatusModels(context, actualUserId);

                var activeItem = context
                    .WorkListItems
                    .Where(x => x.WorkListHeaderId == workListHeaderId && x.ItemId == itemId)
                    .Select(x => new
                    {
                        x.ItemId,
                        x.WorkList.ClosedByUserId,
                        x.WorkList.ClosedDate,
                        x.TakenByUserId,
                        x.TakenDate,
                        x.CompletedDate,
                        StatusSummary = workListSummaryModels.Where(y => y.WorkListHeaderId == x.WorkListHeaderId).FirstOrDefault()
                    })
                    .FirstOrDefault();

                if (activeItem == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No such item");

                if (activeItem.TakenByUserId != actualUserId)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Item is not taken by current user");

                if (activeItem.CompletedDate.HasValue)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Item is completed");

                if (activeItem.ClosedDate.HasValue)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Worklist is closed");

                var oneMonthAgo = Clock.Today.AddDays(-30);

                var creditNr = activeItem.ItemId;
                var credit = context
                    .CreditHeaders
                    .Where(x => x.CreditNr == creditNr)
                    .Select(x => new
                    {
                        HasActiveAlternatePaymentPlan = x.AlternatePaymentPlans.Any(y => y.CancelledByEventId == null && y.FullyPaidByEventId == null),
                        HasRecentlyCancelledAlternatePaymentPlan = x.AlternatePaymentPlans.Any(y => y.CancelledByEvent.TransactionDate >= oneMonthAgo),
                        Customers = x.CreditCustomers.Select(y => new { y.CustomerId })
                    })
                    .Single(); 

                var modelsbyNotificationId = CreditNotificationDomainModel.CreateForCredit(creditNr, context, paymentOrder, onlyFetchOpen: false);
                var unpaidNotifications = CreditNotificationDomainModel
                    .GetNotificationListModel(context, Clock.Today, creditNr, modelsbyNotificationId)
                    .Where(x => !x.IsPaid)
                    .ToList();

                var nrOfOverdueNotifications = unpaidNotifications.Count(x => x.IsOverDue && !x.IsPaid);
                var nrOfDaysPastDueDate = unpaidNotifications.Where(x => !x.IsPaid).Select(x => (int?)Clock.Today.Subtract(x.DueDate).TotalDays).Max();
                var overdueBalance = unpaidNotifications.Where(x => x.IsOverDue && !x.IsPaid).Aggregate(0m, (x, y) => x + y.InitialAmount - y.PaidAmount - y.WrittenOffAmount);

                var creditModel = CreditDomainModel.PreFetchForSingleCredit(itemId, context, NEnv.EnvSettings);
                var totalCapitalDebt = creditModel.GetBalance(CreditDomainModel.AmountType.Capital, Clock.Today);
                var promisedToPayDate = creditModel.GetPromisedToPayDate(Clock.Today);

                var c = new Code.CreditCustomerClient();
                foreach (var applicant in credit.Customers)
                {
                    var ad = new Dictionary<string, string>(
                        c.GetCustomerCardItems(applicant.CustomerId, "firstName", "lastName", "email", "phone"));
                    ad["customerId"] = applicant.CustomerId.ToString();
                    applicants.Add(ad);
                }

                AlternatePaymentPlanService.PaymentPlanDataCompleteOrCancelData alternatePaymentPlanData = null;
                string alternatePaymentPlanStateCode = null;
                if (credit.HasActiveAlternatePaymentPlan)
                {
                    alternatePaymentPlanData = Service.AlternatePaymentPlan.GetActivePaymentPlansCompleteOrCancelData(context, onlyTheseCreditNrs: new List<string> { creditNr }).Single();
                    alternatePaymentPlanStateCode = alternatePaymentPlanData.IsLateOnPayments(Clock.Today, 0)
                        ? "activeButLate"
                        : "active";
                }
                else if (credit.HasRecentlyCancelledAlternatePaymentPlan)
                    alternatePaymentPlanStateCode = "recentlyCancelled";

                return Json2(new
                {
                    itemId = activeItem.ItemId,
                    workListSummary = activeItem.StatusSummary,
                    creditSummary = new
                    {
                        nrOfOverdueNotifications,
                        nrOfDaysPastDueDate,
                        overdueBalance,
                        totalCapitalDebt,
                        promisedToPayDate = promisedToPayDate.HasValue ? promisedToPayDate.Value.ToString("yyyy-MM-dd") : null,
                        creditUrl = Url.Action("Index", "Credit", new { creditNr = activeItem.ItemId }),
                        alternatePaymentPlanStateCode
                    },
                    applicants = applicants,
                    unpaidnotifications = unpaidNotifications
                });
            }
        }

        private const string TryAgainLaterType = "Precollection_TryAgainLater";
        private const string SkippedType = "Precollection_Skipped";

        private class PreCollectionPagingItem
        {
            public string CreditNr { get; internal set; }
            public int NrUnpaidOverdueNotifications { get; internal set; }
            public int NrOfDaysOverdue { get; internal set; }
            public decimal BalanceUnpaidOverdueNotifications { get; internal set; }
            public bool HasTryAgainLater { get; internal set; }
            public decimal TotalCapitalDebt { get; internal set; }
            public string CreditUrl { get; set; }
        }

        private class PreCollectionCandidateModel
        {
            public string CreditNr { get; set; }
            public int NrUnpaidOverdueNotifications { get; set; }
            public int NrOfDaysOverdue { get; set; }
            public decimal BalanceUnpaidOverdueNotifications { get; set; }
            public decimal TotalCapitalDebt { get; set; }
            public DateTime? PromisedToPayDate { get; set; }
            public DateTime? ExpectedSettlementDate { get; set; }
            public int NrOfNotifications { get; set; }
            public DateTime? LatestPaymentDate { get; set; }
            public int NrOfPassedDueDatesWithoutFullPaymentSinceNotification { get; set; }
            public bool HasActiveTermsChange { get; set; }
            public bool HasActiveAlternatePaymentPlan { get; set; }
        }

        private IQueryable<PreCollectionCandidateModel> GetPreCollectionCandidateModel(CreditContext context)
        {
            var today = Clock.Today;
            var openNotifications = CurrentNotificationStateServiceLegacy.GetCurrentOpenNotificationsStateQuery(context, today);
            return context
                  .CreditHeaders
                  .Where(x => x.Status == CreditStatus.Normal.ToString())
                  .Select(x => new
                  {
                      Credit = x,
                      NrOfNotifications = x.Notifications.Where(y => y.CreditNr == x.CreditNr).Count(),
                      OpenNotifications = openNotifications.Where(y => y.CreditNr == x.CreditNr),
                      PromisedToPayDateItem = x.DatedCreditDates.Where(y => y.Name == DatedCreditDateCode.PromisedToPayDate.ToString()).OrderByDescending(y => y.Timestamp).FirstOrDefault(),
                      ExpectedSettlementDate = x
                            .CreditSettlementOffers
                            .Where(y => !y.CancelledByEventId.HasValue && !y.CommitedByEventId.HasValue)
                            .OrderByDescending(y => y.Id)
                            .Select(y => (DateTime?)y.ExpectedSettlementDate)
                            .FirstOrDefault(),
                      HasActiveTermsChange = x.TermsChanges.Any(y => !y.CommitedByEventId.HasValue && !y.CancelledByEventId.HasValue),
                      HasActiveAlternatePaymentPlan = x.AlternatePaymentPlans.Any(y => y.CancelledByEventId == null && y.FullyPaidByEventId == null)
                  })
                  .Select(x => new
                  {
                      CreditNr = x.Credit.CreditNr,
                      PromisedToPayDate = x.PromisedToPayDateItem != null && !x.PromisedToPayDateItem.RemovedByBusinessEventId.HasValue ? x.PromisedToPayDateItem.Value : (DateTime?)null,
                      ExpectedSettlementDate = x.ExpectedSettlementDate,
                      NrUnpaidOverdueNotifications = (int?)x.OpenNotifications.Where(y => y.NrOfPassedDueDatesWithoutFullPaymentSinceNotification > 0).Count(),
                      BalanceUnpaidOverdueNotifications = x.OpenNotifications.Where(y => y.NrOfPassedDueDatesWithoutFullPaymentSinceNotification > 0).Sum(y => (decimal?)y.RemainingAmount) ?? 0m,
                      OldestOpenNotification = x.OpenNotifications.OrderBy(y => y.DueDate).FirstOrDefault(),
                      CapitalDebtAmount = x.Credit.Transactions.Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                      LatestPaymentDate = x.Credit.Transactions.Where(y => y.IncomingPaymentId.HasValue).OrderByDescending(y => y.TransactionDate).Select(y => (DateTime?)y.TransactionDate).FirstOrDefault(),
                      NrOfNotifications = x.NrOfNotifications,
                      x.HasActiveTermsChange,
                      x.HasActiveAlternatePaymentPlan
                  })
                  .Select(x => new PreCollectionCandidateModel
                  {
                      CreditNr = x.CreditNr,
                      BalanceUnpaidOverdueNotifications = x.BalanceUnpaidOverdueNotifications,
                      NrUnpaidOverdueNotifications = x.NrUnpaidOverdueNotifications ?? 0,
                      NrOfDaysOverdue =
                        x.OldestOpenNotification == null ? 0 : x.OldestOpenNotification.NrOfDaysOverdue,
                      NrOfPassedDueDatesWithoutFullPaymentSinceNotification =
                        x.OldestOpenNotification == null ? 0 : x.OldestOpenNotification.NrOfPassedDueDatesWithoutFullPaymentSinceNotification,
                      TotalCapitalDebt = x.CapitalDebtAmount,
                      NrOfNotifications = x.NrOfNotifications,
                      ExpectedSettlementDate = x.ExpectedSettlementDate,
                      LatestPaymentDate = x.LatestPaymentDate,
                      PromisedToPayDate = x.PromisedToPayDate,
                      HasActiveTermsChange = x.HasActiveTermsChange,
                      HasActiveAlternatePaymentPlan = x.HasActiveAlternatePaymentPlan
                  });
        }
    }
}