using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Samples.Models.Migration;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 44: what a developer who knows <c>System.Text.Json</c> meets first in XML.
    /// </summary>
    public static class ComingFromSystemTextJson
    {

        #region Public Methods

        /// <summary>
        /// Serializes a <see langword="null"/> string and an empty string, which XML writes differently, and reads both back.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void NullAndEmpty(TextWriter output)
        {
            var item = new CatalogItem { Id = 7, Description = null, DisplayName = "" };

            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n" };
            string xml = XmlSerializer.Serialize(item, options);
            output.WriteLine(xml);

            CatalogItem? copy = XmlSerializer.Deserialize<CatalogItem>(xml, options);
            output.WriteLine($"Description is null: {copy!.Description is null}");
            output.WriteLine($"DisplayName is empty: {copy.DisplayName == ""}");
        }

        /// <summary>
        /// Serializes and deserializes with options set the way <c>JsonSerializerOptions</c> is set.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var item = new CatalogItem
            {
                Id = 7,
                DisplayName = "Desk lamp",
                UnitPrice = 34.95m,
                InStock = true,
                Description = null,
                Tags = ["office", "lighting"],
            };

            var options = new XmlSerializerOptions
            {
                DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull,
                NewLine = "\n",
                PropertyNamingPolicy = XmlNamingPolicy.CamelCase,
                WriteIndented = true,
            };

            string xml = XmlSerializer.Serialize(item, options);
            output.WriteLine(xml);

            CatalogItem? copy = XmlSerializer.Deserialize<CatalogItem>(xml, options);
            output.WriteLine($"{copy!.DisplayName}: {copy.Tags.Count} tags");
        }

        /// <summary>
        /// Reads numbers and Booleans from a document. XML stores both as text, so the type comes from the method you call.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void TextValues(TextWriter output)
        {
            using XmlDocument document = XmlDocument.Parse("""<Reading sensor="A7"><Count>3</Count><Active>true</Active></Reading>""");
            XmlElement reading = document.RootElement;

            XmlElement count = reading.GetChild("Count");
            output.WriteLine($"Count is {count.ValueKind}: text '{count.GetString()}', number {count.GetInt32() + 1}");

            XmlElement active = reading.GetChild("Active");
            output.WriteLine($"Active is {active.ValueKind}: text '{active.GetString()}', Boolean {active.GetBoolean()}");

            XmlElement sensor = reading.GetAttribute("sensor");
            output.WriteLine($"sensor is {sensor.ValueKind}: text '{sensor.GetString()}'");
        }

        /// <summary>
        /// Serializes an <see cref="Article"/> whose attributes choose a root name, a namespace, an XML attribute, element text, and a wrapped list.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void XmlShapes(TextWriter output)
        {
            var article = new Article
            {
                Id = 12,
                Title = "Reading XML as UTF-8",
                Author = new ArticleAuthor { Email = "ada@contoso.com", Name = "Ada" },
                Tags = ["xml", "dotnet"],
            };

            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n" };
            output.WriteLine(XmlSerializer.Serialize(article, options));
        }

        #endregion

    }

}
