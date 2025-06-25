using System.Collections.Generic;
using NTech.Banking.CivicRegNumbers;

namespace nSavings.Code;

public class CreditReportClient : AbstractServiceClient
{
    protected override string ServiceName => "nCreditReport";

    public class FetchNameAndAddressResult
    {
        public bool Success { get; set; }
        public bool IsInvalidCredentialsError { get; set; }
        public bool IsTimeoutError { get; set; }
        public List<Item> Items { get; set; }

        public class Item
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }

    public FetchNameAndAddressResult FetchNameAndAddress(string providerName, ICivicRegNumber civicRegNr,
        List<string> requestedItemNames, int customerId)
    {
        return Begin()
            .PostJson("PersonInfo/FetchNameAndAddress", new
            {
                providerName = providerName,
                civicRegNr = civicRegNr.NormalizedValue,
                requestedItemNames,
                customerId
            })
            .ParseJsonAs<FetchNameAndAddressResult>();
    }
}