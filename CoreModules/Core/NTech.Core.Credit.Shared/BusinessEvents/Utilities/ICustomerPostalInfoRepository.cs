using System.Collections.Generic;

namespace nCredit.DbModel.BusinessEvents
{
    public interface ICustomerPostalInfoRepository
    {
        SharedCustomerPostalInfo GetCustomerPostalInfo(int customerId);
        void PreFetchCustomerPostalInfo(ISet<int> customerIds);
    }
}