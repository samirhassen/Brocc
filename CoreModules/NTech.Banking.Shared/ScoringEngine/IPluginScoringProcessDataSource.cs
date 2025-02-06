using System.Collections.Generic;

namespace NTech.Banking.ScoringEngine
{
    public interface IPluginScoringProcessDataSource
    {
        ScoringDataModel GetItems(string objectId, ISet<string> applicationItems, ISet<string> applicantItems);
        PluginScoringProcessModelWithInternalHistory GetItemsWithInternalHistory(string objectId, ISet<string> applicationItems, ISet<string> applicantItems);
    }

    public class PluginScoringProcessModelWithInternalHistory
    {
        public ScoringDataModel ScoringData { get; set; }
        public List<HistoricalApplication> HistoricalApplications { get; set; }
        public List<HistoricalCredit> HistoricalCredits { get; set; }
    }
}
