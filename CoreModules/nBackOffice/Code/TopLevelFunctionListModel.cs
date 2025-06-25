using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace nBackOffice.Code
{
    public class TopLevelFunctionListModel
    {
        private TopLevelFunctionListModel()
        {
        }

        private List<Function> Functions { get; set; }
        public List<string> SubGroupOrder { get; set; }

        private class Function
        {
            public string SystemName { get; set; }
            public string GroupName { get; set; }
            public string MenuGroup { get; set; }
            public string MenuSubGroup { get; set; }
            public string MenuName { get; set; }
            public string Module { get; set; }
            public string Url { get; set; }
        }

        private static ISet<string> ParseStringList(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return new HashSet<string>();
            return new HashSet<string>(s.Split('|').Select(x => x.Trim()));
        }

        private static bool IsFeatureEnabled(string name)
        {
            var v = NTechEnvironment.Instance.Setting(name, false);
            if (!string.IsNullOrWhiteSpace(v))
                return v?.ToLowerInvariant() == "true";
            return NEnv.ClientCfg.IsFeatureEnabled(name);
        }

        private static bool IsFunctionShown(XElement f, ISet<string> activeModuleNames)
        {
            var anyToggles = ParseStringList(f.Attribute("featureToggle")?.Value);
            anyToggles.UnionWith(ParseStringList(f.Element("RequireFeaturesAny")?.Value));

            if (anyToggles.Any() && !anyToggles.Any(IsFeatureEnabled))
                return false;

            var allToggles = ParseStringList(f.Element("RequireFeaturesAll")?.Value);

            if (allToggles.Any() && !allToggles.All(IsFeatureEnabled))
                return false;

            var allModules = ParseStringList(f.Element("RequiresOtherModulesAll")?.Value);

            if (allModules.Any() && !allModules.All(x => activeModuleNames.Contains(x)))
                return false;

            var notAllowedIfFeaturesAny = ParseStringList(f.Element("NotAllowedIfFeaturesAny")?.Value);
            if (notAllowedIfFeaturesAny.Any(IsFeatureEnabled))
                return false;

            return true;
        }

        public static TopLevelFunctionListModel FromXDocument(XDocument document, ISet<string> activeModuleNames)
        {
            var functions = new List<Function>();

            foreach (var wrapper in document.Root.Descendants().Where(x => x.Name.LocalName == "Functions"))
            {
                functions.AddRange(wrapper.Descendants()
                    .Where(x => x.Name.LocalName == "Function" && IsFunctionShown(x, activeModuleNames))
                    .Select(f => new Function
                    {
                        SystemName = wrapper.Attribute("systemName")?.Value,
                        GroupName = wrapper.Attribute("groupName").Value,
                        MenuGroup = f.Descendants().Single(x => x.Name.LocalName == "MenuGroup").Value,
                        MenuSubGroup =
                            f.Descendants().SingleOrDefault(x => x.Name.LocalName == "MenuSubGroup")?.Value ?? "Core",
                        MenuName = f.Descendants().Single(x => x.Name.LocalName == "MenuName").Value,
                        Module = f.Descendants().Single(x => x.Name.LocalName == "Module").Value,
                        Url = f.Descendants().Single(x => x.Name.LocalName == "Url").Value
                    }));
            }

            var subGroupOrder = document.Root.Descendants().Where(x => x.Name.LocalName == "SgOrderItem")
                .Select(x => x.Value).ToList();

            return new TopLevelFunctionListModel
            {
                Functions = functions,
                SubGroupOrder = subGroupOrder
            };
        }

        public static TopLevelFunctionListModel FromEmbeddedResource()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"nBackOffice.Resources.TopLevelFunctionList.xml";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                var doc = XDocuments.Load(stream);
                return FromXDocument(doc, GetActiveModuleNames());
            }
        }

        private static ISet<string> GetActiveModuleNames()
        {
            var activeModuleNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var k in NEnv.ServiceRegistry.Internal)
                activeModuleNames.Add(k.Key);
            foreach (var k in NEnv.ServiceRegistry.External)
                activeModuleNames.Add(k.Key);
            return activeModuleNames;
        }

        public Tuple<List<MenuGroup>, List<string>> GetMenuWithSubGroupOrder()
        {
            var activeModuleNames = GetActiveModuleNames();

            var menuGroups = Functions
                .Where(x => activeModuleNames.Contains(x.Module))
                .GroupBy(x => x.MenuGroup)
                .Select(x => new
                {
                    GroupName = x.Key,
                    Items = x.Select(y => new
                    {
                        Item = new MenuGroup.Item
                        {
                            FunctionName = y.MenuName,
                            RequiredRoleName = GetRequiredRoleName(y),
                            AbsoluteUri = new Uri(new Uri(NEnv.ServiceRegistry.External[y.Module]), y.Url),
                        },
                        SubGroupName = string.IsNullOrWhiteSpace(y.MenuSubGroup) ? null : y.MenuSubGroup.Trim()
                    }).ToList()
                })
                .Select(x => new MenuGroup
                {
                    GroupName = x.GroupName,
                    Items = x.Items.Where(y => y.SubGroupName == null).Select(y => y.Item).ToList(),
                    SubGroups = x
                        .Items
                        .Where(y => y.SubGroupName != null)
                        .Select(y => y.SubGroupName)
                        .Distinct()
                        .Select(subGroupName =>
                            new MenuGroup.SubGroup
                            {
                                SubGroupName = subGroupName,
                                Items = x.Items.Where(y => y.SubGroupName == subGroupName).Select(y => y.Item).ToList()
                            })
                        .ToList()
                })
                .ToList();

            return Tuple.Create(menuGroups, this.SubGroupOrder);
        }

        /// <summary>
        /// Required role ([system].[group]) or null to indicate just being logged in is enough.
        /// </summary>
        private string GetRequiredRoleName(Function f)
        {
            if (f.SystemName?.ToLowerInvariant() != "all" && f.GroupName?.ToLowerInvariant() != "all")
                return f.SystemName == null ? f.GroupName : $"{f.SystemName}.{f.GroupName}";
            if (f.SystemName?.ToLowerInvariant() != f.GroupName?.ToLowerInvariant())
                throw new Exception("If either SystemName or GroupName is All then both must be");
            return null;
        }

        public class MenuGroup
        {
            public string GroupName { get; set; }
            public string Icon { get; set; }
            public List<Item> Items { get; set; }
            public List<SubGroup> SubGroups { get; set; }

            public class Item
            {
                public string FunctionName { get; set; }
                public Uri AbsoluteUri { get; set; }
                public string RequiredRoleName { get; set; }
            }

            public class SubGroup
            {
                public string SubGroupName { get; set; }
                public List<Item> Items { get; set; }
            }
        }
    }
}