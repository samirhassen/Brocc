using System;
using System.Collections.Generic;
using System.Linq;

namespace nCreditReport.Code.UcSe
{
    internal class UcReplyHelper
    {
        public UcReplyHelper(UcSeService2.report source)
        {
            this.source = source;
        }

        private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly UcSeService2.report source;

        public void MapSingleOptionalGroup(string groupName, Action<UcSeService2.group> a, params Tuple<string, string>[] onGroupMissing)
        {
            var g = source
                ?.group
                ?.Where(x => x.id.Equals(groupName, StringComparison.OrdinalIgnoreCase) && (x.index == "0" || string.IsNullOrWhiteSpace(x.index)))
                ?.FirstOrDefault();
            if (g != null)
                a(g);
            else if (onGroupMissing != null)
            {
                foreach (var v in onGroupMissing)
                    values[v.Item1] = v.Item2;
            }
        }

        public void HandleRepeatingGroup(string groupName, Action<UcSeService2.group> a)
        {
            var gs = source
                ?.group
                ?.Where(x => x.id.Equals(groupName, StringComparison.OrdinalIgnoreCase))
                ?.ToList();
            foreach (var g in gs)
                a(g);
        }

        public void Add(string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                values[name] = value;
        }

        public void Add(string name, Func<string> valueFactory)
        {
            var value = valueFactory();
            if (!string.IsNullOrWhiteSpace(value))
                values[name] = value;
        }

        public Dictionary<string, string> Values { get { return this.values; } }
    }
}