using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace System.Xml.Linq
{
    public static class XDocuments
    {
        /// <summary>
        /// Alternative to XDocument.Parse that prohibits unsafe entity expansion unless explicitly requested
        /// </summary>
        public static XDocument Parse(string content, bool allowEntityExpansion = false)
        {
            return HandleShared(content, allowEntityExpansion, XDocument.Parse, x => new StringReader(x));
        }

        /// <summary>
        /// Alternative to XDocument.Load that prohibits unsafe entity expansion unless explicitly requested
        /// </summary>
        public static XDocument Load(Stream content, bool allowEntityExpansion = false)
        {
            return HandleShared(content, allowEntityExpansion, XDocument.Load, x => new StreamReader(x));
        }

        /// <summary>
        /// Alternative to XDocument.Load that prohibits unsafe entity expansion unless explicitly requested
        /// </summary>
        public static XDocument Load(string path, bool allowEntityExpansion = false)
        {
            return HandleShared(path, allowEntityExpansion, XDocument.Load, x => new StreamReader(x));
        }

        private static XDocument HandleShared<TContent>(TContent content, bool allowEntityExpansion, Func<TContent, XDocument> handleDirect, Func<TContent, TextReader> createReader)
        {
            if (allowEntityExpansion)
                return handleDirect(content);
            else
                using (var sr = createReader(content))
                using (var xr = XmlReader.Create(sr, new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    IgnoreWhitespace = true
                }))
                {
                    //Based on: https://docs.microsoft.com/en-us/archive/msdn-magazine/2009/november/xml-denial-of-service-attacks-and-defenses
                    //NOTE: This is quite a brutal fix. The linked article has some more measured responses if needed in specific places.
                    return XDocument.Load(xr);
                }
        }

        public static XElement GetSingleDescendant(XElement parent, string name, bool mustExist)
        {
            var elements = GetDescendants(parent, name);
            if (elements.Count > 1)
                throw new Exception($"{parent.Name.LocalName}: Has {elements.Count} instances of {name} but expected {(mustExist ? "exactly one" : "at most one")} .");
            else if(mustExist && elements.Count == 0)
                throw new Exception($"{parent.Name.LocalName}: Is missing element {name}.");
            return mustExist ? elements.Single() : elements.SingleOrDefault();
        }

        public static List<XElement> GetDescendants(XElement parent, string name) =>
            parent.Descendants().Where(x => x.Name.LocalName == name).ToList();
    }
}