using NTech.Core.PreCredit.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class CreditApplicationCustomerListService : ICreditApplicationCustomerListService
    {
        private readonly IPreCreditContextFactoryService creditContextFactoryService;

        public CreditApplicationCustomerListService(IPreCreditContextFactoryService creditContextFactoryService)
        {
            this.creditContextFactoryService = creditContextFactoryService;
        }

        public void SetMemberStatus(string listName, bool isMember, int customerId,
            string applicationNr = null, CreditApplicationHeader application = null,
            CreditApplicationEvent evt = null, int? creditApplicationEventId = null,
            Action<bool> observeStatusChange = null)
        {
            using (var context = creditContextFactoryService.CreateExtended())
            {
                SetMemberStatusComposable(context, listName, isMember, customerId, applicationNr: applicationNr, application: application, evt: evt, creditApplicationEventId: creditApplicationEventId, observeStatusChange: observeStatusChange);
                context.SaveChanges();
            }
        }

        public List<int> GetMemberCustomerIds(string applicationNr, string listName)
        {
            using (var context = creditContextFactoryService.CreateExtended())
            {
                return GetMemberCustomerIdsComposable(context, applicationNr, listName).ToList();
            }
        }

        public IQueryable<int> GetMemberCustomerIdsComposable(IPreCreditContextExtended context, string applicationNr, string listName)
        {
            return context.CreditApplicationCustomerListMembersQueryable.Where(x => x.ApplicationNr == applicationNr && x.ListName == listName).Select(x => x.CustomerId);
        }

        public void SetMemberStatusComposable(IPreCreditContextExtended context, string listName, bool isMember, int customerId,
            string applicationNr = null, CreditApplicationHeader application = null,
            CreditApplicationEvent evt = null, int? creditApplicationEventId = null,
            Action<bool> observeStatusChange = null)
        {
            if (applicationNr == null && application == null)
                throw new Exception("applicationNr or application must be supplied");

            var nr = applicationNr ?? application.ApplicationNr;

            Action<bool> logOperation = op =>
            {
                context.AddCreditApplicationCustomerListOperations(new CreditApplicationCustomerListOperation
                {
                    CreditApplication = application,
                    ApplicationNr = nr,
                    CustomerId = customerId,
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
                    context.AddCreditApplicationCustomerListMembers(new CreditApplicationCustomerListMember
                    {
                        CreditApplication = application,
                        CustomerId = customerId,
                        ListName = listName
                    });
                    logOperation(true);
                }
            }
            else
            {
                var m = context.CreditApplicationCustomerListMembersQueryable.Where(x => x.ApplicationNr == nr && x.ListName == listName && x.CustomerId == customerId).SingleOrDefault();
                if (isMember && m == null)
                {
                    context.AddCreditApplicationCustomerListMembers(new CreditApplicationCustomerListMember
                    {
                        CreditApplication = application,
                        ApplicationNr = nr,
                        CustomerId = customerId,
                        ListName = listName
                    });
                    logOperation(true);
                }
                else if (!isMember && m != null)
                {
                    context.RemoveCreditApplicationCustomerListMembers(m);
                    logOperation(false);
                }
            }
        }
    }

    public interface ICreditApplicationCustomerListService
    {
        void SetMemberStatus(string listName, bool isMember, int customerId,
            string applicationNr = null, CreditApplicationHeader application = null,
            CreditApplicationEvent evt = null, int? creditApplicationEventId = null,
            Action<bool> observeStatusChange = null);
        List<int> GetMemberCustomerIds(string applicationNr, string listName);
    }
}