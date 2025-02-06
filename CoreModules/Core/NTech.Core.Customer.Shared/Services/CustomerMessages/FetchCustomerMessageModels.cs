using System.Collections.Generic;

namespace nCustomer.Code.Services
{
    public class FetchCustomerMessageModels
    {
        public List<CustomerMessageModel> CustomerMessageModels { get; set; }
        public int TotalMessageCount { get; set; }
        public bool AreMessageTextsIncluded { get; set; }
    }
}