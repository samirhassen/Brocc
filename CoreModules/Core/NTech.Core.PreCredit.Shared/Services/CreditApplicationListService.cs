using NTech.Core.PreCredit.Shared;
using System;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class CreditApplicationListService
    {
        private readonly IPreCreditContextFactoryService preCreditContextFactoryService;

        public CreditApplicationListService(IPreCreditContextFactoryService preCreditContextFactoryService)
        {
            this.preCreditContextFactoryService = preCreditContextFactoryService;
        }

        public void SetMemberStatus(string listName, bool isMember,
            string applicationNr = null, CreditApplicationHeader application = null,
            CreditApplicationEvent evt = null, int? creditApplicationEventId = null,
            Action<bool> observeStatusChange = null)
        {
            using (var context = preCreditContextFactoryService.CreateExtended())
            {
                SetMemberStatusComposable(context, listName, isMember, applicationNr: applicationNr, application: application, evt: evt, creditApplicationEventId: creditApplicationEventId, observeStatusChange: observeStatusChange);
                context.SaveChanges();
            }
        }

        public string GetListName(string stepName, string statusName)
        {
            return GetListNameComposable(stepName, statusName);
        }

        public static string GetListNameComposable(string stepName, string statusName)
        {
            return $"{stepName}_{statusName}";
        }

        public void SetMemberStatusComposable(IPreCreditContextExtended context, string listName, bool isMember,
            string applicationNr = null, CreditApplicationHeader application = null,
            CreditApplicationEvent evt = null, int? creditApplicationEventId = null,
            Action<bool> observeStatusChange = null)
        {
            if (applicationNr == null && application == null)
                throw new Exception("applicationNr or application must be supplied");

            var nr = applicationNr ?? application.ApplicationNr;

            Action<bool> logOperation = op =>
            {
                context.AddCreditApplicationListOperations(new CreditApplicationListOperation
                {
                    CreditApplication = application,
                    ApplicationNr = nr,
                    ListName = listName,
                    ByEvent = evt,
                    ByUserId = context.CurrentUserId,
                    CreditApplicationEventId = creditApplicationEventId,
                    IsAdd = op,
                    OperationDate = context.CoreClock.Now
                });
                observeStatusChange?.Invoke(op);
            };

            if (nr == null)
            {
                //New application so there is no prior state to consider
                if (isMember)
                {
                    context.AddCreditApplicationListMembers(new CreditApplicationListMember
                    {
                        CreditApplication = application,
                        ListName = listName
                    });
                    logOperation(true);
                }
            }
            else
            {
                var m = context.CreditApplicationListMembersQueryable.Where(x => x.ApplicationNr == nr && x.ListName == listName).SingleOrDefault();
                if (isMember && m == null)
                {
                    context.AddCreditApplicationListMembers(new CreditApplicationListMember
                    {
                        CreditApplication = application,
                        ApplicationNr = nr,
                        ListName = listName
                    });
                    logOperation(true);
                }
                else if (!isMember && m != null)
                {
                    context.RemoveCreditApplicationListMembers(m);
                    logOperation(false);
                }
            }
        }

        public IQueryable<string> GetListMembershipNamesComposable(IPreCreditContextExtended context, string applicationNr)
        {
            return context.CreditApplicationListMembersQueryable.Where(x => x.ApplicationNr == applicationNr).Select(x => x.ListName);
        }

        public void SwitchListStatusComposable(IPreCreditContextExtended context,
            string listNamePrefix, //like CreditCheck, DocumentCheck
            string statusName, //like Accepted, Rejected. Total name will be prefix_status and all that start with prefix_ will be removed
            string applicationNr = null,
            CreditApplicationHeader application = null,
            CreditApplicationEvent evt = null,
            int? creditApplicationEventId = null,
            Action<string, bool> observeStatusChange = null)
        {
            if (applicationNr == null && application == null)
                throw new Exception("applicationNr or application must be supplied");

            var nr = applicationNr ?? application.ApplicationNr;

            Action<string, bool> logOperation = (ln, op) =>
            {
                context.AddCreditApplicationListOperations(new CreditApplicationListOperation
                {
                    CreditApplication = application,
                    ApplicationNr = nr,
                    ListName = ln,
                    ByEvent = evt,
                    ByUserId = context.CurrentUserId,
                    CreditApplicationEventId = creditApplicationEventId,
                    IsAdd = op,
                    OperationDate = context.CoreClock.Now
                });
                observeStatusChange?.Invoke(ln, op);
            };

            var prefix = $"{listNamePrefix}_";
            var listName = $"{prefix}{statusName}";
            if (nr == null)
            {
                //No history exists
                context.AddCreditApplicationListMembers(new CreditApplicationListMember
                {
                    CreditApplication = application,
                    ListName = listName
                });
                logOperation(listName, true);
            }
            else
            {
                var members = context.CreditApplicationListMembersQueryable.Where(x => x.ApplicationNr == nr && x.ListName.StartsWith(prefix)).ToList();
                var needsAdd = true;
                foreach (var m in members)
                {
                    if (m.ListName == listName)
                        needsAdd = false;
                    else
                    {
                        context.RemoveCreditApplicationListMembers(m);
                        logOperation(m.ListName, false);
                    }
                }
                if (needsAdd)
                {
                    context.AddCreditApplicationListMembers(new CreditApplicationListMember
                    {
                        CreditApplication = application,
                        ApplicationNr = nr,
                        ListName = listName
                    });
                    logOperation(listName, true);
                }
            }
        }
    }
}