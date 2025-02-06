using NTech;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services.MortgageLoans
{
    public class MortgageLoanLeadsWorkListService : IMortgageLoanLeadsWorkListService
    {
        private readonly IWorkListService workListService;
        private readonly PreCreditContextFactoryService contextFactoryService;
        private readonly IClock clock;

        public const string WorkListName = "Leads";

        public MortgageLoanLeadsWorkListService(IWorkListService workListService, PreCreditContextFactoryService contextFactoryService, IClock clock)
        {
            this.workListService = workListService;
            this.contextFactoryService = contextFactoryService;
            this.clock = clock;
        }

        private string ComplexListName => ApplicationInfoService.MortgageLoanLeadsComplexListName;

        public (int? WorkListId, bool NoLeadsMatchFilter) TryCreateWorkList()
        {
            using (var context = contextFactoryService.Create())
            {
                var today = clock.Today;

                var workListItems = context
                    .CreditApplicationHeaders
                    .Select(x => new
                    {
                        x.ApplicationNr,
                        x.ApplicationDate,
                        x.IsActive,
                        LeadItems = x.ComplexApplicationListItems.Where(y =>
                            y.Nr == 1 && y.ListName == ComplexListName && !y.IsRepeatable)
                    })
                    .Where(x => x.IsActive && x.LeadItems.Any(y => y.ItemName == "IsLead" && y.ItemValue == "true"))
                    .ToList()
                    .Select(x =>
                    {
                        var leadItems = x.LeadItems.ToDictionary(y => y.ItemName, y => y.ItemValue);
                        var tryLaterDaysRaw = leadItems.Opt("TryLaterDays");
                        var tryLaterDateRaw = leadItems.Opt("TryLaterDate");
                        return new
                        {
                            x.ApplicationNr,
                            x.ApplicationDate,
                            TryLaterDays = tryLaterDaysRaw == null ? new int?() : int.Parse(tryLaterDaysRaw),
                            TryLaterDate = Dates.ParseDateTimeExactOrNull(tryLaterDateRaw, "yyyy-MM-dd")
                        };
                    })
                    .Where(x => !x.TryLaterDate.HasValue || x.TryLaterDate.Value <= today)
                    .OrderBy(x => x.TryLaterDays ?? -1)
                    .ThenBy(x => x.ApplicationDate)
                    .Select(x => (ItemId: x.ApplicationNr, Properties: new List<(string Name, string DataTypeName, string Value)>
                    {
                        workListService.CreateProperty("TryLaterDays", x.TryLaterDays.HasValue ? x.TryLaterDays.Value.ToString() : "None"),
                        workListService.CreateProperty("TryLaterDate", x.TryLaterDate.HasValue ? x.TryLaterDate.Value.ToString("yyyy-MM-dd") : "None"),
                        workListService.CreateProperty("ApplicationDate", x.ApplicationDate.DateTime)
                    }))
                    .ToList();

                if (workListItems.Count == 0)
                    return (WorkListId: null, NoLeadsMatchFilter: true);

                var filters = new List<(string Name, string Value)>
                {
                    (Name: "ExcludingTryLater", Value: "true")
                };

                var result = workListService.CreateOrAddToWorkList(null, false, WorkListName, filters, workListItems);
                if (!result.WasCreated || !result.WorkListId.HasValue)
                    throw new Exception("workListService did not create worklist when it should");

                return (WorkListId: result.WorkListId.Value, NoLeadsMatchFilter: false);
            }
        }


        public bool TryScheduleTryLater(ApplicationInfoModel ai, int tryLaterDays)
        {
            DateTime tryLaterDate = clock.Today.AddDays(tryLaterDays);
            using (var context = contextFactoryService.CreateExtendedConcrete())
            {
                Func<string, string, ComplexApplicationListOperation> i = (name, value) =>
                    new ComplexApplicationListOperation
                    {
                        ApplicationNr = ai.ApplicationNr,
                        IsDelete = false,
                        ListName = ComplexListName,
                        Nr = 1,
                        ItemName = name,
                        UniqueValue = value
                    };
                ComplexApplicationListService.ChangeListComposable(new List<ComplexApplicationListOperation>
                {
                    i("TryLaterDate", tryLaterDate.ToString("yyyy-MM-dd")),
                    i("TryLaterDays", tryLaterDays.ToString())
                }, context);

                context.CreateAndAddComment($"Lead scheduled for followup at {tryLaterDate.ToString("yyyy-MM-dd")}", "LeadScheduledTryLater", applicationNr: ai.ApplicationNr);

                context.SaveChanges();
            }

            return true;
        }

        public bool TryCancelLead(ApplicationInfoModel ai)
        {
            if (!ai.IsLead || !ai.IsActive)
                return false;

            using (var context = contextFactoryService.CreateExtendedConcrete())
            {
                Func<string, string, ComplexApplicationListOperation> i = (name, value) =>
                    new ComplexApplicationListOperation
                    {
                        ApplicationNr = ai.ApplicationNr,
                        IsDelete = false,
                        ListName = ComplexListName,
                        Nr = 1,
                        ItemName = name,
                        UniqueValue = value
                    };
                ComplexApplicationListService.ChangeListComposable(new List<ComplexApplicationListOperation>
                {
                    i("WasCancelled", "true"),
                    i("CancelledBy", context.CurrentUserId.ToString()),
                    i("CancelledDate", clock.Now.ToString("o")),
                }, context);

                var a = context.CreditApplicationHeaders.Single(x => x.ApplicationNr == ai.ApplicationNr);

                a.IsActive = false;
                a.IsCancelled = true;
                a.CancelledBy = context.CurrentUserId;
                a.CancelledDate = clock.Now;
                a.CancelledState = "Lead";

                context.CreateAndAddComment("Lead cancelled", "LeadCancelled", creditApplicationHeader: a);

                context.SaveChanges();
            }

            return true;
        }

        public bool TryRejectLead(ApplicationInfoModel ai, List<string> rejectionReasonCodes, string rejectionReasonOtherText)
        {
            if (!ai.IsLead || !ai.IsActive)
                return false;

            using (var context = contextFactoryService.CreateExtendedConcrete())
            {
                Func<string, string, ComplexApplicationListOperation> i = (name, value) =>
                    new ComplexApplicationListOperation
                    {
                        ApplicationNr = ai.ApplicationNr,
                        IsDelete = false,
                        ListName = ComplexListName,
                        Nr = 1,
                        ItemName = name,
                        UniqueValue = value
                    };

                var hasOther = rejectionReasonCodes.Contains("other");
                var otherRejectionReasonText = hasOther ? rejectionReasonOtherText ?? "other" : null;
                ComplexApplicationListService.ChangeListComposable(Enumerables.SkipNulls(
                    i("WasRejected", "true"),
                    i("RejectedBy", context.CurrentUserId.ToString()),
                    i("RejectedDate", clock.Now.ToString("o")),
                    otherRejectionReasonText != null ? i("OtherRejectionReasonText", otherRejectionReasonText) : null,
                    new ComplexApplicationListOperation
                    {
                        ApplicationNr = ai.ApplicationNr,
                        ListName = ComplexListName,
                        Nr = 1,
                        IsDelete = false,
                        ItemName = "RejectionReasons",
                        RepeatedValue = rejectionReasonCodes

                    }
                ).ToList(), context);

                var a = context.CreditApplicationHeaders.Single(x => x.ApplicationNr == ai.ApplicationNr);

                a.IsActive = false;
                a.IsRejected = true;
                a.RejectedById = context.CurrentUserId;
                a.RejectedDate = clock.Now;

                context.CreateAndAddComment("Lead rejected", "LeadRejected", creditApplicationHeader: a);

                context.SaveChanges();
            }

            return true;
        }

        public bool TryChangeToQualifiedLead(ApplicationInfoModel ai)
        {
            if (!ai.IsLead || !ai.IsActive)
                return false;

            using (var context = contextFactoryService.CreateExtendedConcrete())
            {
                Func<string, string, ComplexApplicationListOperation> i = (name, value) =>
                    new ComplexApplicationListOperation
                    {
                        ApplicationNr = ai.ApplicationNr,
                        IsDelete = false,
                        ListName = ComplexListName,
                        Nr = 1,
                        ItemName = name,
                        UniqueValue = value
                    };
                ComplexApplicationListService.ChangeListComposable(new List<ComplexApplicationListOperation>
                {
                    i("IsLead", "false"),
                    i("WasAccepted", "true"),
                    i("AcceptedBy", context.CurrentUserId.ToString()),
                    i("AcceptedDate", clock.Now.ToString("o")),
                }, context);

                context.CreateAndAddComment("Lead changed to qualified lead", "LeadChangedToQualifiedLead",
                    applicationNr: ai.ApplicationNr);

                context.SaveChanges();
            }

            return true;
        }

        public bool IsLead(string applicationNr)
        {
            using (var context = this.contextFactoryService.Create())
            {
                return context.ComplexApplicationListItems.Any(x =>
                    x.ApplicationNr == applicationNr && x.ListName == ComplexListName && x.Nr == 1 &&
                    x.ItemName == "IsLead" && x.ItemValue == "true" && !x.IsRepeatable);
            }
        }
    }

    public interface IMortgageLoanLeadsWorkListService
    {
        (int? WorkListId, bool NoLeadsMatchFilter) TryCreateWorkList();
        bool TryChangeToQualifiedLead(ApplicationInfoModel ai);
        bool TryCancelLead(ApplicationInfoModel ai);
        bool TryRejectLead(ApplicationInfoModel ai, List<string> rejectionReasonCodes, string rejectionReasonOtherText);
        bool TryScheduleTryLater(ApplicationInfoModel ai, int tryLaterDays);
        bool IsLead(string applicationNr);
    }
}