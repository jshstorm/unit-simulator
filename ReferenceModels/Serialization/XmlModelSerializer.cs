using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace ReferenceModels.Serialization
{
    public static class XmlModelSerializer
    {
        // T 리스트를 지정한 루트 이름으로 직렬화
        public static string SerializeListToXml<T>(IEnumerable<T> items, string? rootName = null)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            rootName ??= typeof(T).Name + "s";

            var wrapper = new ListWrapper<T> { Items = new List<T>(items) }; 

            var ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, string.Empty);

            var serializer = string.IsNullOrWhiteSpace(rootName)
                ? new XmlSerializer(typeof(ListWrapper<T>))
                : new XmlSerializer(typeof(ListWrapper<T>), new XmlRootAttribute(rootName));

            using var sw = new StringWriter();
            using var xw = XmlWriter.Create(sw, new XmlWriterSettings { Indent = true, Encoding = System.Text.Encoding.UTF8 });
            serializer.Serialize(xw, wrapper, ns);
            return sw.ToString();
        }

        public static void SerializeListToFile<T>(IEnumerable<T> items, string filePath, string? rootName = null)
        {
            var xml = SerializeListToXml(items, rootName);
            File.WriteAllText(filePath, xml, System.Text.Encoding.UTF8);
        }

        public static IList<T> DeserializeListFromXml<T>(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml)) return new List<T>();

            var serializer = new XmlSerializer(typeof(ListWrapper<T>));
            using var sr = new StringReader(xml);
            var wrapper = (ListWrapper<T>?)serializer.Deserialize(sr);
            return wrapper?.Items ?? new List<T>();
        }

        [XmlRoot("Items")]
        public class ListWrapper<T>
        {
            [XmlElement(ElementName = "Item")]
            public List<T> Items { get; set; } = new();
        }
    }
}