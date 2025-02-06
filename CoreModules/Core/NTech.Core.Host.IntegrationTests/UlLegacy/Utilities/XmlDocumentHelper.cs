using System.Xml.Linq;

namespace NTech.Core.Host.IntegrationTests.UlLegacy.Utilities
{
    public class XmlDocumentHelper
    {
        private readonly XDocument document;

        public XmlDocumentHelper(XDocument document)
        {
            this.document = document;
        }
        
        public string GetElementValue(params ElementSelector[] selectors)
        {
            XElement? current = document.Root;
            ElementSelector? lastSelector = null;
            foreach (var selector in selectors)
            {
                lastSelector = selector;

                if (current == null)
                    break;

                current = current.Elements().Where(x => x.Name.LocalName == selector.Name).Skip(selector.Index).FirstOrDefault();
            }
            if (current == null || string.IsNullOrWhiteSpace(current?.Value))
            {
                throw new Exception($"Missing: {string.Join(", ", selectors.Select(x => x.ToString()))}. Last selector: {lastSelector?.ToString()}");
            }
            return current.Value;
        }

        public class ElementSelector
        {
            public ElementSelector(string name, int index)
            {
                Name = name;
                Index = index;
            }

            public string Name { get; }
            public int Index { get; }
            public static implicit operator ElementSelector(string name)
            {
                if (name.EndsWith("]"))
                {
                    var bracketIndex = name.LastIndexOf("[");
                    var actualName = name.Substring(0, bracketIndex);
                    var index = int.Parse(name.Substring(bracketIndex + 1).TrimEnd(']'));
                    return new ElementSelector(actualName, index);
                }
                else
                {
                    return new ElementSelector(name, 0);
                }
            }
            public override string ToString() => $"{Name}[{Index}]";
        }
    }
}
