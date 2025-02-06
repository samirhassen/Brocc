using NTech.Core.Credit.Shared.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Code.Services
{
    public class CreditCustomerListService : ICreditCustomerListService
    {
        private readonly ICreditCustomerListServiceComposable customerListServiceComposable;
        private readonly Func<ICreditContextExtended> createContext;

        public CreditCustomerListService(ICreditCustomerListServiceComposable customerListServiceComposable, Func<ICreditContextExtended> createContext)
        {
            this.customerListServiceComposable = customerListServiceComposable;
            this.createContext = createContext;
        }

        public void SetMemberStatus(string listName, bool isMember, int customerId,
            string creditNr = null, CreditHeader credit = null,
            BusinessEvent evt = null, int? businessEventId = null,
            Action<bool> observeStatusChange = null)
        {
            using (var context = createContext())
            {
                SetMemberStatusComposable(context, listName, isMember, customerId, creditNr: creditNr, credit: credit, evt: evt, businessEventId: businessEventId, observeStatusChange: observeStatusChange);
                context.SaveChanges();
            }
        }

        public List<int> GetMemberCustomerIds(string creditNr, string listName)
        {
            using (var context = createContext())
            {
                return GetMemberCustomerIdsComposable(context, creditNr, listName).ToList();
            }
        }

        public void SetMemberStatusComposable(ICreditContextExtended context, string listName, bool isMember, int customerId, string creditNr = null, CreditHeader credit = null, BusinessEvent evt = null, int? businessEventId = null, Action<bool> observeStatusChange = null)
        {
            this.customerListServiceComposable.SetMemberStatusComposable(context, listName, isMember, customerId, creditNr: creditNr, credit: credit, evt: evt, businessEventId: businessEventId, observeStatusChange: observeStatusChange);
        }

        public IQueryable<int> GetMemberCustomerIdsComposable(ICreditContextExtended context, string creditNr, string listName)
        {
            return GetMemberCustomerIdsComposable(context, creditNr, listName);
        }
    }

    public class CreditCustomerListServiceComposable : ICreditCustomerListServiceComposable
    {
        public CreditCustomerListServiceComposable()
        {

        }

        public IQueryable<int> GetMemberCustomerIdsComposable(ICreditContextExtended context, string creditNr, string listName)
        {
            return context.CreditCustomerListMembersQueryable.Where(x => x.CreditNr == creditNr && x.ListName == listName).Select(x => x.CustomerId);
        }

        public void SetMemberStatusComposable(ICreditContextExtended context, string listName, bool isMember, int customerId,
            string creditNr = null, CreditHeader credit = null,
            BusinessEvent evt = null, int? businessEventId = null,
            Action<bool> observeStatusChange = null)
        {
            if (creditNr == null && credit == null)
                throw new Exception("creditNr or credit must be supplied");

            var nr = creditNr ?? credit.CreditNr;

            void LogOperation(bool isAddOperation)
            {
                context.AddCreditCustomerListOperation(context.FillInfrastructureFields(new CreditCustomerListOperation
                {
                    Credit = credit,
                    CreditNr = creditNr,
                    CustomerId = customerId,
                    ListName = listName,
                    ByEvent = evt,
                    ByUserId = context.CurrentUser.UserId,
                    ByEventId = businessEventId,
                    IsAdd = isAddOperation,
                    OperationDate = context.CoreClock.Now
                }));
                observeStatusChange?.Invoke(isAddOperation);
            }

            if (nr == null)
            {
                //New credit so there is no prior state to consider
                if (isMember)
                {
                    context.AddCreditCustomerListMember(context.FillInfrastructureFields(new CreditCustomerListMember
                    {
                        Credit = credit,
                        CustomerId = customerId,
                        ListName = listName
                    }));
                    LogOperation(true);
                }
            }
            else
            {
                var existingListMember = context.CreditCustomerListMembersQueryable.SingleOrDefault(x => x.CreditNr == nr && x.ListName == listName && x.CustomerId == customerId);
                if (isMember && existingListMember == null)
                {
                    context.AddCreditCustomerListMember(context.FillInfrastructureFields(new CreditCustomerListMember
                    {
                        Credit = credit,
                        CreditNr = nr,
                        CustomerId = customerId,
                        ListName = listName
                    }));
                    LogOperation(true);
                }
                else if (!isMember && existingListMember != null)
                {
                    context.RemoveCreditCustomerListMember(existingListMember);
                    LogOperation(false);
                }
            }
        }
    }

    public interface ICreditCustomerListService : ICreditCustomerListServiceComposable
    {
        void SetMemberStatus(string listName, bool isMember, int customerId,
            string creditNr = null, CreditHeader credit = null,
            BusinessEvent evt = null, int? businessEventId = null,
            Action<bool> observeStatusChange = null);
        List<int> GetMemberCustomerIds(string creditNr, string listName);
    }
}