using System.Collections.Generic;

namespace nSavings.Code.Services
{
    public interface ICustomerRelationsMergeService
    {
        void MergeSavingsAccountsToCustomerRelations(ISet<string> onlySavingsAccountNrs = null);
    }
}