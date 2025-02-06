using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace nBackOffice
{
    public class CrossModuleNavigationService
    {
        private Lazy<Dictionary<string, TargetsFileModel.TargetModel>> targets = new Lazy<Dictionary<string, TargetsFileModel.TargetModel>>(() =>
        {
            var d = new Dictionary<string, TargetsFileModel.TargetModel>();

            Action<Stream> handle = s =>
            {
                using (var r = new StreamReader(s, Encoding.UTF8))
                {
                    var tf = JsonConvert.DeserializeObject<TargetsFileModel>(r.ReadToEnd());
                    foreach (var target in tf.Targets)
                    {
                        d[target.Name] = target;
                    }
                }
            };

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("nBackOffice.Resources.CrossModuleNavigationTargets.json"))
            {
                handle(stream);
            }

            var f = NEnv.ExtraCrossModuleNavigationTargetsFile;
            if (f != null)
            {
                //Note since we set by name these can overwrite ones from the base file. This is intentional to allow a client to redirect
                //some navigation if they so desire.
                using (var r = File.OpenRead(f))
                {
                    handle(r);
                }
            }

            return d;
        });

        public bool TryGetUrlFromCrossModuleNavigationToken(string targetCode, NTechServiceRegistry serviceRegistry, out Uri url, out string failedMessage, Action<string> observeTargetModuleName = null, string backTargetCode = null)
        {
            url = null;
            failedMessage = null;
            if (!NTechNavigationTarget.TryParseCrossModuleNavigationTargetCode(targetCode, out var targetName, out var targetContext))
            {
                failedMessage = "Invalid target code";
                return false;
            }

            var m = this.targets.Value;
            if (!m.ContainsKey(targetName))
            {
                failedMessage = "No such target";
                return false;
            }

            var t = m[targetName];

            if (t.RequireFeaturesAll != null && !t.RequireFeaturesAll.All(NEnv.ClientCfg.IsFeatureEnabled))
            {
                failedMessage = "Missing feature for target";
                return false;
            }

            if (t.RequireFeaturesAny != null && t.RequireFeaturesAny.Count > 0 && !t.RequireFeaturesAny.Any(NEnv.ClientCfg.IsFeatureEnabled))
            {
                failedMessage = "Missing feature for target";
                return false;
            }

            //Support things like /s/application/:applicationNr instead of /s/application?applicationNr=<...>
            var urlParameters = t.UrlParameters ?? new List<string>();
            var localUrl = t.LocalUrl;
            var inUrlParameters = new HashSet<string>();
            foreach (var urlParameter in urlParameters)
            {
                var token = $":{urlParameter}";
                if (localUrl.Contains(token))
                {
                    localUrl = localUrl.Replace(token, targetContext?.Opt(urlParameter) ?? "");
                    inUrlParameters.Add(urlParameter);
                }
            }

            var queryParameters = new List<Tuple<string, string>>();

            queryParameters
                .AddRange((t.UrlParameters ?? new List<string>())
                    .Where(x => !inUrlParameters.Contains(x))
                    .Select(x => Tuple.Create(x, targetContext?.Opt(x)))
                    .ToArray());

            if (backTargetCode != null)
            {
                queryParameters.Add(Tuple.Create("backTarget", backTargetCode));
            }

            url = serviceRegistry.External.ServiceUrl(
                t.Module,
                localUrl,
                queryParameters.ToArray());

            observeTargetModuleName?.Invoke(t.Module);

            return true;
        }

        private class TargetsFileModel
        {
            public List<TargetModel> Targets { get; set; }

            public class TargetModel
            {
                public string Name { get; set; }
                public string Module { get; set; }
                public string LocalUrl { get; set; }
                public List<string> UrlParameters { get; set; }
                public List<string> RequireFeaturesAll { get; set; }
                public List<string> RequireFeaturesAny { get; set; }
            }
        }
    }
}