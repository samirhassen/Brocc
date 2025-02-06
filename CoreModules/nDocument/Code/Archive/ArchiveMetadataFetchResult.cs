using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace nDocument.Code.Archive
{
    public class ArchiveMetadataFetchResult
    {
        public string ContentType { get; set; }
        public string FileName { get; set; }
        public ArchiveOptionalData OptionalData { get; set; }

        private static void ParseMetadata(XDocument d, out string contentType, out string filename, out ArchiveOptionalData optionalData)
        {
            contentType = d.Descendants().Where(x => x.Name.LocalName == "contentType").Single().Value;
            filename = d.Descendants().Where(x => x.Name.LocalName == "filename").Single().Value;

            string GetOptionalData(string name) =>
                d.Descendants().Where(x => x.Name.LocalName == name).SingleOrDefault()?.Value;

            optionalData = ArchiveOptionalData.Parse(GetOptionalData);
        }

        public static ArchiveMetadataFetchResult CreateFromXml(XDocument d)
        {
            string contentType;
            string filename;
            ArchiveOptionalData optionalData;
            ParseMetadata(d, out contentType, out filename, out optionalData);

            return new ArchiveMetadataFetchResult
            {
                FileName = filename,
                ContentType = contentType,
                OptionalData = optionalData
            };
        }

        public static XDocument CreateMetadataXml(string key, string contentType, string filename, ArchiveOptionalData optionalData)
        {
            var elements = new List<XElement>()
            {
                new XElement("key", key),
                new XElement("version", "2"),
                new XElement("creationDate", DateTimeOffset.Now.ToString("o")),
                new XElement("contentType", contentType),
                new XElement("filename", filename)
            };

            optionalData?.SetOptionalData((x, y) => elements.Add(new XElement(x, y)));

            return new XDocument(new XElement("meta", elements));
        }
    }
}