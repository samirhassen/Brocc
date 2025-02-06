using NTech.Banking.ScoringEngine;
using System.Collections.Generic;

namespace nPreCredit
{
    public interface ICustomerServiceRepository
    {
        List<HistoricalApplication> FindByCustomerId(int customerId);
        Dictionary<int, List<HistoricalApplication>> FindByCustomerIds(params int[] customerIds);
        int[] GetComplexApplicationListCustomerIds(string applicationNr);
        IDictionary<int, HashSet<string>> FindApplicationNrsPerCustomerId(IList<int> customerIds);
        Dictionary<int, List<HistoricalApplication>> FindApplicationObjectsByCustomerIds(IList<int> customerIds);
    }
}