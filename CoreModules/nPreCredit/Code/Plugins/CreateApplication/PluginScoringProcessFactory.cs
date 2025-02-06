using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code
{
    public class PluginScoringProcessFactory
    {
        private Lazy<Dictionary<string, PluginScoringProcess>> scoringProcesses = new Lazy<Dictionary<string, PluginScoringProcess>>(() =>
        {
            var types = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => !p.IsAbstract && p.IsSubclassOf(typeof(PluginScoringProcess)));

            var d = new Dictionary<string, PluginScoringProcess>();
            foreach (var t in types)
            {
                var i = t.GetConstructor(Type.EmptyTypes).Invoke(null) as PluginScoringProcess;
                if (i.Name.Equals("CompanyLoanInitial") && NEnv.ForceManualControlOnInitialScoring)
                    i.ForceManualControlOnInitialScoring = true;
                d[i.Name] = i;
            }

            return d;
        });

        public PluginScoringProcess GetScoringProcess(string name)
        {
            if (!scoringProcesses.Value.ContainsKey(name))
                throw new Exception($"No scoringprocess named '{name}' found. A missing plugin?");
            return scoringProcesses.Value[name];
        }
    }
}