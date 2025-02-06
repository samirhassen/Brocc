using NTech.Banking.ScoringEngine;
using System.Collections.Generic;

namespace nPreCredit.Code.Services.BluestepFi
{
    //TODO: Implement dynamically
    public class BluestepFiScoringDataSource : IPluginScoringProcessDataSource
    {
        private readonly PluginScoringProcessModelWithInternalHistory d;

        public BluestepFiScoringDataSource(PluginScoringProcessModelWithInternalHistory d)
        {
            this.d = d;
        }

        public ScoringDataModel GetItems(string objectId, ISet<string> applicationItems, ISet<string> applicantItems)
        {
            return d.ScoringData;
        }

        public PluginScoringProcessModelWithInternalHistory GetItemsWithInternalHistory(string objectId, ISet<string> applicationItems, ISet<string> applicantItems)
        {
            return d;
        }
    }
}