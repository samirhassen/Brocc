using System;
using System.Collections.Generic;

namespace nCustomer.Code.Services.Kyc
{
    public interface IKycScreeningService
    {
        Tuple<bool, DateTime?> IsCustomerScreened(int customerId, DateTime? screenDate);
        ListScreenBatchNewResult ListScreenBatchNew(List<int> customerIds, DateTime screenDate, Func<int, bool?> getLatestIsPepQuestionAnswer = null, bool isNonBatchScreen = false);
    }

    public class ListScreenBatchNewResult
    {
        public bool Success { get; set; }
        public List<FailedItemModel> FailedToGetTrapetsDataItems { get; set; }
        public class FailedItemModel
        {
            public int CustomerId { get; set; }
            public string Reason { get; set; }
        }
    }
}