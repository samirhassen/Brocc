using NTech.Core.Credit.Shared.Database;
using System;
using System.Linq;

namespace nCredit.Code.Services
{
    public interface ICreditCustomerListServiceComposable
    {
        void SetMemberStatusComposable(ICreditContextExtended context, string listName, bool isMember, int customerId,
            string creditNr = null, CreditHeader credit = null,
            BusinessEvent evt = null, int? businessEventId = null,
            Action<bool> observeStatusChange = null);
        IQueryable<int> GetMemberCustomerIdsComposable(ICreditContextExtended context, string creditNr, string listName);
    }
}