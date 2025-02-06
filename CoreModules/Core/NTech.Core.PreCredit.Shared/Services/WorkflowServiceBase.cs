using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.PreCredit.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public abstract class WorkflowServiceBase : WorkflowServiceReadBase
    {
        private readonly CreditApplicationListService creditApplicationListService;
        private readonly IPreCreditContextFactoryService preCreditContextFactory;

        public WorkflowServiceBase(CreditApplicationListService creditApplicationListService, WorkflowModel model,
            IPreCreditContextFactoryService preCreditContextFactory) : base(model)
        {
            this.creditApplicationListService = creditApplicationListService;
            this.preCreditContextFactory = preCreditContextFactory;
        }

        private string GetNextStepNameToSetAsInitial(ISet<string> currentListNames)
        {
            //If all the current steps are done, add the list <nextStepName>_Initial so it can be found
            for (var i = 0; i < StepNameOrder.Count; i++)
            {
                var current = currentListNames.FirstOrDefault(x => x.StartsWith($"{StepNameOrder[i]}_"));
                if (current != null)
                {
                    if (current.EndsWith(InitialStatusName))
                        return null; //There is already a not done list so the application will be found there. Change nothing
                }
                else
                    return StepNameOrder[i];
            }
            return null; //All steps are done
        }

        public bool ChangeStepStatusComposable(IPreCreditContextExtended context,
            string stepName,
            string statusName,
            string applicationNr = null,
            CreditApplicationHeader application = null,
            CreditApplicationEvent evt = null,
            int? creditApplicationEventId = null)
        {
            var wasChanged = false;
            var nr = applicationNr ?? application.ApplicationNr;

            var currentListNames = new HashSet<string>();
            currentListNames.AddRange(this.creditApplicationListService.GetListMembershipNamesComposable(context, nr).ToList());

            Action<string, bool> onStatusChange = (listName, isAdd) =>
            {
                wasChanged = true;
                if (isAdd)
                    currentListNames.Add(listName);
                else
                    currentListNames.RemoveAll(x => x == listName);
            };

            creditApplicationListService.SwitchListStatusComposable(context, stepName, statusName,
                applicationNr: applicationNr, application: application, evt: evt, creditApplicationEventId: creditApplicationEventId, observeStatusChange: onStatusChange);
            if (statusName == InitialStatusName && nr != null)
            {
                //When going backwards we remove any extra future initial:s since we dont want them to show in two search filters.
                var stepIndex = stepIndexByStepName[stepName];
                for (var i = stepIndex + 1; i < stepNames.Count; i++)
                {
                    var stepInitialListName = creditApplicationListService.GetListName(stepNames[i], InitialStatusName);
                    if (currentListNames.Contains(stepInitialListName))
                    {
                        this.creditApplicationListService.SetMemberStatusComposable(context, stepInitialListName, false,
                            applicationNr: applicationNr, application: application, evt: evt, creditApplicationEventId: creditApplicationEventId, observeStatusChange: x => onStatusChange(stepInitialListName, x));
                    }
                }
            }
            else if (statusName != InitialStatusName && statusName != RejectedStatusName && nr != null)
            {
                var nextStepName = GetNextStepNameToSetAsInitial(currentListNames.ToHashSetShared());
                if (nextStepName != null)
                {
                    this.creditApplicationListService.SwitchListStatusComposable(context, nextStepName, InitialStatusName,
                        applicationNr: applicationNr, application: application, evt: evt, creditApplicationEventId: creditApplicationEventId);
                }
            }

            return wasChanged;
        }

        public bool ChangeStepStatus(
            string stepName,
            string statusName,
            string applicationNr = null,
            CreditApplicationHeader application = null,
            CreditApplicationEvent evt = null,
            int? creditApplicationEventId = null)
        {
            using (var context = preCreditContextFactory.CreateExtended())
            {
                var wasChanged = ChangeStepStatusComposable(context, stepName, statusName,
                    applicationNr: applicationNr, application: application, evt: evt, creditApplicationEventId: creditApplicationEventId);

                context.SaveChanges();

                return wasChanged;
            }
        }

        public static bool IsRevertAllowed(ApplicationInfoModel ai, ISharedWorkflowService wf, string currentStepName, out string revertNotAllowedMessage)
        {
            var isDecisionStepAccepted = wf.IsStepStatusAccepted(currentStepName, ai.ListNames);
            if (!isDecisionStepAccepted)
            {
                revertNotAllowedMessage = "Step must be approved to be reverted";
                return false;
            }

            var nextStepName = wf.GetNextStepNameAfter(currentStepName);

            if (!wf.IsStepStatusInitial(nextStepName, ai.ListNames))
            {
                revertNotAllowedMessage = "Only the last accepted step can be reverted";
                return false;
            }

            revertNotAllowedMessage = null;
            return true;
        }
    }

    public class WorkflowServiceReadBase
    {
        protected readonly List<string> stepNames;
        protected readonly WorkflowModel model;
        protected Dictionary<string, int> stepIndexByStepName;

        public WorkflowServiceReadBase(WorkflowModel model)
        {
            this.model = model;
            this.stepNames = model.Steps.Select(x => x.Name).ToList();
            stepIndexByStepName = stepNames.Select((x, i) => new { x, i }).ToDictionary(x => x.x, x => x.i);
        }

        public WorkflowModel Model => model;

        public int Version
        {
            get
            {
                return this.model.WorkflowVersion;
            }
        }

        public string InitialStatusName
        {
            get
            {
                return "Initial";
            }
        }

        public string RejectedStatusName
        {
            get
            {
                return "Rejected";
            }
        }

        public string AcceptedStatusName
        {
            get
            {
                return "Accepted";
            }
        }

        protected List<string> StepNameOrder { get { return this.stepNames; } }

        public List<string> GetStepOrder()
        {
            return StepNameOrder;
        }

        public string GetStepStatus(string stepName, IEnumerable<string> listMemberships)
        {
            var acceptedName = GetListName(stepName, AcceptedStatusName);
            var rejectedName = GetListName(stepName, RejectedStatusName);
            foreach (var s in listMemberships)
            {
                if (s.Equals(acceptedName, StringComparison.OrdinalIgnoreCase))
                    return AcceptedStatusName;
                else if (s.Equals(rejectedName, StringComparison.OrdinalIgnoreCase))
                    return RejectedStatusName;
            }
            return InitialStatusName;
        }

        public bool IsStepStatusInitial(string stepName, IEnumerable<string> listMemberships)
        {
            return !(IsStepStatusAccepted(stepName, listMemberships) || IsStepStatusRejected(stepName, listMemberships));
        }

        public bool IsStepStatusAccepted(string stepName, IEnumerable<string> listMemberships)
        {
            return listMemberships.Contains(GetListName(stepName, AcceptedStatusName));
        }

        public bool IsStepStatusRejected(string stepName, IEnumerable<string> listMemberships)
        {
            return listMemberships.Contains(GetListName(stepName, RejectedStatusName));
        }

        public string GetListName(string stepName, string statusName)
        {
            return CreditApplicationListService.GetListNameComposable(stepName, statusName);
        }

        public bool AreAllStepsBeforeComplete(string stepName, IEnumerable<string> listMemberships, string debugContext = null, Action<string> observeLastIncompleteStep = null)
        {
            foreach (var s in GetStepOrder())
            {
                if (s == stepName)
                    return true;
                else if (!listMemberships.Contains(GetListName(s, AcceptedStatusName)) && !listMemberships.Contains(GetListName(s, RejectedStatusName)))
                {
                    observeLastIncompleteStep?.Invoke(s);
                    return false;
                }
            }
            throw new NTechCoreWebserviceException($"Invalid workflow on {debugContext}");
        }

        public string GetNextStepNameAfter(string currentStepName)
        {
            var hasPassedCurrent = false;
            foreach (var s in GetStepOrder())
            {
                if (hasPassedCurrent)
                    return s;
                else if (currentStepName == s)
                    hasPassedCurrent = true;
            }
            return null;
        }

        public string GetStepDisplayName(string stepName)
        {
            if (string.IsNullOrWhiteSpace(stepName))
                return null;
            return stepIndexByStepName.ContainsKey(stepName) ? model.Steps[stepIndexByStepName[stepName]].DisplayName : stepName;
        }

        public string GetEarliestInitialListName(IEnumerable<string> memberOfListNames)
        {
            var s = memberOfListNames.ToHashSetShared();
            foreach (var n in GetStepOrder())
            {
                var listName = $"{n}_{InitialStatusName}";
                if (s.Contains(listName))
                    return listName;
            }
            return null;
        }

        public string GetLatestNoneInitialListName(IEnumerable<string> memberOfListNames)
        {
            foreach (var n in GetStepOrder().AsEnumerable().Reverse())
            {
                if (!IsStepStatusInitial(n, memberOfListNames))
                    return GetListName(n, GetStepStatus(n, memberOfListNames));
            }
            return null;
        }

        public string GetCurrentListName(IEnumerable<string> memberOfListNames)
        {
            return GetEarliestInitialListName(memberOfListNames) ?? GetLatestNoneInitialListName(memberOfListNames);
        }

        public bool TryDecomposeListName(string listName, out Tuple<string, string> stepNameAndStatusName)
        {
            stepNameAndStatusName = null;

            var i = listName.IndexOf('_');
            if (i < 0)
                return false;

            stepNameAndStatusName = Tuple.Create(listName.Substring(0, i), listName.Substring(i + 1));

            return true;
        }
    }

    public class WorkflowModel
    {
        public int WorkflowVersion { get; set; }
        public List<StepModel> Steps { get; set; }

        public class StepModel
        {
            public string Name { get; set; }
            public string ComponentName { get; set; }
            public string DisplayName { get; set; }
            public Newtonsoft.Json.Linq.JObject CustomData { get; set; }
        }

        public StepModel FindNextStep(string stepName)
        {
            var i = Steps.FindIndex(x => x.Name == stepName);
            if (i < 0)
                return null;

            if (i >= Steps.Count)
                return null;

            return Steps[i + 1];
        }

        public StepModel GetSignApplicationStepIfAny()
        {
            return FindStepByCustomData(x => x?.IsSignApplication == "yes", new { IsSignApplication = "" });
        }

        public StepModel GetApproveAgreementStepIfAny()
        {
            return FindStepByCustomData(x => x?.IsApproveAgreement == "yes", new { IsApproveAgreement = "" });
        }

        //Any application that is in one of these lists are not shown in the standard search but can be chosen as a special search subset
        //When switching to one of these subsets the current filter step is set to DefaultFilterName if one is present otherwise the current is kept
        public List<SeparatedWorkListModel> SeparatedWorkLists { get; set; }

        public class SeparatedWorkListModel
        {
            public string ListName { get; set; }
            public string ListDisplayName { get; set; }
        }

        public T GetCustomDataAsAnonymousType<T>(string stepName, T typeObject) where T : class
        {
            var s = Steps?.Where(x => x.Name == stepName)?.FirstOrDefault();
            if (s == null)
                throw new Exception($"The workflow step '{stepName}' does not exist");

            if (s.CustomData == null)
                return null;

            return s.CustomData.ToObject<T>();
        }

        public StepModel FindStepByCustomData<T>(Func<T, bool> predicate, T typeObject) where T : class
        {
            return Steps?.Where(x => predicate(GetCustomDataAsAnonymousType<T>(x.Name, typeObject)))?.FirstOrDefault();
        }
    }

    public interface ISharedWorkflowService : IMinimalSharedWorkflowService
    {
        int Version { get; }
        WorkflowModel Model { get; }
        string InitialStatusName { get; }
        string RejectedStatusName { get; }
        string AcceptedStatusName { get; }

        List<string> GetStepOrder();

        string GetCurrentListName(IEnumerable<string> memberOfListNames);
        string GetNextStepNameAfter(string currentStepName);

        bool ChangeStepStatusComposable(
            IPreCreditContextExtended context,
            string stepName,
            string statusName,
            string applicationNr = null,
            CreditApplicationHeader application = null,
            CreditApplicationEvent evt = null,
            int? creditApplicationEventId = null);

        bool ChangeStepStatus(
            string stepName,
            string statusName,
            string applicationNr = null,
            CreditApplicationHeader application = null,
            CreditApplicationEvent evt = null,
            int? creditApplicationEventId = null);

        bool AreAllStepsBeforeComplete(string stepName, IEnumerable<string> listMemberships, string debugContext = null, Action<string> observeLastIncompleteStep = null);

        bool TryDecomposeListName(string listName, out Tuple<string, string> stepNameAndStatusName);

        bool IsStepStatusAccepted(string stepName, IEnumerable<string> listMemberships);

        bool IsStepStatusInitial(string stepName, IEnumerable<string> listMemberships);

        string GetListName(string stepName, string statusName);
    }

    /// <summary>
    /// The only purpose of this is to solve DI issues with ApplicationCancellationService. 
    /// It has no business meaning beyond being used with the ThrowExceptionMinimalSharedWorkflowService to allow ApplicationCancellationService
    /// to actually work.
    /// </summary>
    public interface IMinimalSharedWorkflowService
    {
        string GetStepDisplayName(string stepName);
        string GetEarliestInitialListName(IEnumerable<string> memberOfListNames);
    }

    public class ThrowExceptionMinimalSharedWorkflowService : IMinimalSharedWorkflowService
    {
        public string GetEarliestInitialListName(IEnumerable<string> memberOfListNames) =>
            throw new NotImplementedException();

        public string GetStepDisplayName(string stepName) =>
            throw new NotImplementedException();
    }
}