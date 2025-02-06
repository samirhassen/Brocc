using System.Xml;
using System.Xml.Linq;

namespace NTech.DocumentArchiveMigrator.DiskToAws
{
    public class DiskArchiveProvider
    {
        public static XDocument LoadXDocument(string path, bool allowEntityExpansion = false)
        {
            return HandleShared(path, allowEntityExpansion, XDocument.Load, (string x) => new StreamReader(x));
        }

        private static XDocument HandleShared<TContent>(TContent content, bool allowEntityExpansion, Func<TContent, XDocument> handleDirect, Func<TContent, TextReader> createReader)
        {
            if (allowEntityExpansion)
            {
                return handleDirect(content);
            }

            using TextReader input = createReader(content);
            using XmlReader reader = XmlReader.Create(input, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreWhitespace = true
            });
            return XDocument.Load(reader);
        }

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
        }

        public class ArchiveOptionalData
        {
            public string DelayedDocumentType { get; set; }
            public string DelayedDocumentTemplateArchiveKey { get; set; }
            /// <summary>
            /// Something that ties this to a purpose in the system.
            /// The intent is to allow figuring out if documents can be removed
            /// without looking up if anything points to it.
            /// Could for instance be CreditApplicationDocumentAttachment for all documents
            /// that have been attached to a credit application. In this case SourceId would be
            /// application nr.
            /// </summary>
            public string SourceType { get; set; }
            public string SourceId { get; set; }

            public static ArchiveOptionalData Parse(Func<string, string> getOptionalData)
            {
                return new ArchiveOptionalData
                {
                    DelayedDocumentTemplateArchiveKey = getOptionalData("delayedDocumentTemplateArchiveKey"),
                    DelayedDocumentType = getOptionalData("delayedDocumentType"),
                    SourceType = getOptionalData("sourceType"),
                    SourceId = getOptionalData("sourceId")
                };
            }

            public void SetOptionalData(Action<string, string> setOptionalData)
            {
                void Set(string name, string value)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        setOptionalData(name, value);
                    }
                }

                Set("delayedDocumentTemplateArchiveKey", DelayedDocumentTemplateArchiveKey);
                Set("delayedDocumentType", DelayedDocumentType);
                Set("sourceType", SourceType);
                Set("sourceId", SourceId);
            }
        }
    }
}
