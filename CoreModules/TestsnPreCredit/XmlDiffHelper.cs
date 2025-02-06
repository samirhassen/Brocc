using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace TestsnPreCredit
{
    public static class XmlDiffHelper
    {
        /// <summary>
        /// BEWARE. This will not work well on documents that have lists of elements
        ///         as it diffs by comparing the content of the same element path in both files of each text element.
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        public static List<string> GetDiffs(XDocument expected, XDocument actual)
        {
            var expectedTextElements = expected
                .Descendants()
                .Where(x => !x.HasElements)
                .ToList();

            var diffs = new List<string>();
            foreach (var e in expectedTextElements)
            {
                var path = GetPathTo(expected.Root, e);
                var pathName = string.Join(".", path.Select(x => x.LocalName.ToString()));
                var expectedValue = e.Value;
                var actualValue = GetValue(actual.Root, path);
                if (actualValue == null)
                {
                    diffs.Add($"{pathName}: missing");
                }
                else if (expectedValue != actualValue)
                {
                    diffs.Add($"{pathName}: {actualValue} != {expectedValue}");
                }
            }

            return diffs;
        }

        private static string GetValue(XElement root, List<XName> path)
        {
            var e = root;
            foreach (var p in path)
            {
                e = e.Elements().Where(x => x.Name == p).SingleOrDefault();
                if (e == null)
                    return null;
            }
            return e.Value ?? "";
        }

        private static List<XName> GetPathTo(XElement root, XElement e)
        {
            var s = new List<XName>();
            var c = e;
            int guard = 0;
            while (c != root && guard++ < 100000)
            {
                s.Add(c.Name);
                c = c.Parent;
            }
            s.Reverse();
            return s;
        }
    }
}
