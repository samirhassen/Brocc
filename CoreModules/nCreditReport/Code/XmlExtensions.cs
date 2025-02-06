using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using InvalidOperationException = System.InvalidOperationException;

namespace nCreditReport.Code
{
    public static class XmlExtensions
    {

        /// <summary>
        /// Get a single XElement from the direct descendants of the root XElement. 
        /// </summary>
        /// <param name="root">Root XElement. </param>
        /// <param name="name">The local name of the descendant to get. </param>
        /// <returns></returns>
        public static XElement Child(this XElement root, string name)
        {
            try
            {
                var parts = name.Split('/');
                if (parts.Length > 1)
                {
                    return root.Child(parts.First());
                }
                return root.Elements().Single(x => x.Name.LocalName == name);
            }
            catch (InvalidOperationException ex) when (ex.Message.Equals("Sequence contains more than one matching element"))
            {
                throw new Exception($@"Found more than one element named '{name}' inside '{root.Name.LocalName}' ");
            }
            catch
            {
                throw new Exception($@"Could not find '{name}' inside '{root.Name.LocalName}' ");
            }
        }

        public static XElement OptionalChild(this XElement root, string name)
        {
            try
            {
                var parts = name.Split('/');
                if (parts.Length > 1)
                {
                    return root.Child(parts.First());
                }
                return root.Elements().Single(x => x.Name.LocalName == name);
            }
            catch
            {
                return null;
            }
        }

        public static IEnumerable<XElement> Children(this XElement root, string name)
        {
            try
            {
                var parts = name.Split('/');
                if (parts.Length > 1)
                {
                    return root.Children(parts.First());
                }
                return root.Elements().Where(x => x.Name.LocalName == name);
            }
            catch
            {
                throw new Exception($@"Could not find '{name}' inside '{root.Name.LocalName}' ");
            }
        }

        public static XElement FindElementByPath(this XDocument doc, string path)
        {
            var firstElement = doc.Descendants().First();
            return firstElement.FindElementByPath(path);
        }

        /// <summary>
        /// Return value from the specific path if it exists, or null. 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetValueIfExistsByPath(this XElement root, string name)
        {
            try
            {
                var element = root.FindElementByPath(name);
                return string.IsNullOrEmpty(element.Value) ? null : element.Value;
            }
            catch
            {
                return null;
            }
        }

        public static string GetRequiredValue(this XElement root, string name)
        {
            try
            {
                var element = root.FindElementByPath(name);
                return element.Value;
            }
            catch
            {
                throw new Exception($@"Could not find '{name}' inside '{root.Name.LocalName}' ");
            }
        }

        public static string GetOptionalValue(this XElement root, string name)
        {
            try
            {
                var element = root.FindElementByPath(name);
                return element.Value;
            }
            catch
            {
                // Ignored 
            }

            return null;
        }

        /// <summary>
        /// Get a single XElement from the descendants of the root, using the path sent in. 
        /// </summary>
        /// <param name="element">THe root element to search from. </param>
        /// <param name="path">A slash-separated string. Ex. response/consumer/personData. </param>
        /// <returns></returns>
        public static XElement FindElementByPath(this XElement element, string path)
        {
            var parts = path.Split('/');

            XElement result = element;
            foreach (var part in parts)
            {
                try
                {
                    // Go down in the element-tree
                    result = result.Child(part);
                }
                catch (InvalidOperationException)
                {
                    throw new Exception($@"Could not find '{part}' inside '{result.Name.LocalName}' ");
                }
            }
            return result;
        }

        public static XElement Find(this XElement element, string name)
        {
            try
            {
                return element.Descendants().Single(x => x.Name.LocalName == name);
            }
            catch (InvalidOperationException ex) when (ex.Message.Equals("Sequence contains more than one matching element"))
            {
                throw new Exception($@"Found more than one element named '{name}' inside '{element.Name.LocalName}'. ");
            }
            catch
            {
                throw new Exception($@"Could not find '{name}' {element.Name.LocalName}'. ");
            }
        }

        public static XElement Find(this XDocument document, string name, bool mustExist = true)
        {

            try
            {
                if (mustExist)
                {
                    return document.Descendants().Single(x => x.Name.LocalName == name);
                }
                else
                {
                    return document.Descendants().SingleOrDefault(x => x.Name.LocalName == name);
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Equals("Sequence contains more than one matching element"))
            {
                throw new Exception($@"Found more than one element named '{name}' inside xml-document. ");
            }
            catch
            {
                throw new Exception($@"Could not find '{name}' inside xml-document. ");
            }
        }

    }
}