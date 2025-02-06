using System;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace nCreditReport.Code
{
    public static class XmlSerializationUtil
    {
        public static Func<XDocument, T> CreateDeserializer<T>()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));

            return x =>
            {
                using (var reader = x.Root.CreateReader())
                {
                    return (T)xmlSerializer.Deserialize(reader);
                }
            };
        }

        public static T Deserialize<T>(XDocument doc)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));

            using (var reader = doc.Root.CreateReader())
            {
                return (T)xmlSerializer.Deserialize(reader);
            }
        }
        public static T Deserialize<T>(XElement element)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));

            using (var reader = element.CreateReader())
            {
                return (T)xmlSerializer.Deserialize(reader);
            }
        }

        public static XDocument Serialize<T>(T value)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));

            XDocument doc = new XDocument();
            using (var writer = doc.CreateWriter())
            {
                xmlSerializer.Serialize(writer, value);
            }

            return doc;
        }
    }
}