using System;
using System.Collections.Generic;

namespace nPreCredit.Code.Services.SharedStandard
{
    public interface ILoanStandardCustomerRelationService
    {
        void AddNewApplication(IEnumerable<int> customerIds, string applicationNr, DateTimeOffset applicationDate);
    }
}