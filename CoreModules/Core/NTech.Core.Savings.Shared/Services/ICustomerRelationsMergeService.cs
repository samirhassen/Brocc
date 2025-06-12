using System.Collections.Generic;

namespace NTech.Core.Savings.Shared.Services
{
    public interface ICustomerRelationsMergeService
    {
        void MergeSavingsAccountsToCustomerRelations(ISet<string> onlySavingsAccountNrs = null);
    }
}