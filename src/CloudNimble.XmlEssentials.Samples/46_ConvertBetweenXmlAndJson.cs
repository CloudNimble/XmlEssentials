using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Samples.Models.Migration;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 46: converts XML to JSON and JSON to XML with <c>System.Text.Json</c> and XmlEssentials, through a
    /// type both serializers map and through the two mutable trees.
    /// </summary>
    public static class ConvertBetweenXmlAndJson
    {

        #region Fields

        /// <summary>
        /// Specifies the JSON form of the delivery every method in this sample converts.
        /// </summary>
        private const string DeliveryJson = """
            {
              "carrier": "Contoso Freight",
              "id": 1042,
              "items": [
                "Keyboard",
                "Mouse"
              ],
              "signed": true,
              "weight": 2.5
            }
            """;

        /// <summary>
        /// Specifies the XML form of the delivery every method in this sample converts.
        /// </summary>
        private const string DeliveryXml = """
            <delivery id="1042">
              <carrier>Contoso Freight</carrier>
              <item>Keyboard</item>
              <item>Mouse</item>
              <signed>true</signed>
              <weight>2.5</weight>
            </delivery>
            """;

        /// <summary>
        /// The JSON options of this sample, created once and reused.
        /// </summary>
        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            NewLine = "\n",
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        /// <summary>
        /// The XML options of this sample, created once and reused. They mirror the JSON options.
        /// </summary>
        private static readonly XmlSerializerOptions s_xmlOptions = new()
        {
            NewLine = "\n",
            OmitXmlDeclaration = true,
            PropertyNamingPolicy = XmlNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Converts XML to JSON through a type that both serializers map.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            Delivery? delivery = XmlSerializer.Deserialize<Delivery>(DeliveryXml, s_xmlOptions);

            string json = JsonSerializer.Serialize(delivery, s_jsonOptions);
            output.WriteLine(json);
        }

        /// <summary>
        /// Converts JSON to XML through a type that both serializers map.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void JsonToXml(TextWriter output)
        {
            Delivery? delivery = JsonSerializer.Deserialize<Delivery>(DeliveryJson, s_jsonOptions);

            string xml = XmlSerializer.Serialize(delivery, s_xmlOptions);
            output.WriteLine(xml);
        }

        /// <summary>
        /// Converts UTF-8 XML to UTF-8 JSON and back with source-generated metadata, which needs no reflection
        /// and no string of either document.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void SourceGenerated(TextWriter output)
        {
            byte[] utf8Xml = Encoding.UTF8.GetBytes(DeliveryXml);

            Delivery? fromXml = XmlSerializer.Deserialize(utf8Xml, DeliveryXmlContext.Default.Delivery);
            byte[] utf8Json = JsonSerializer.SerializeToUtf8Bytes(fromXml, DeliveryJsonContext.Default.Delivery);
            output.WriteLine(Encoding.UTF8.GetString(utf8Json));

            Delivery? fromJson = JsonSerializer.Deserialize(utf8Json, DeliveryJsonContext.Default.Delivery);
            var buffer = new ArrayBufferWriter<byte>();
            XmlSerializer.Serialize(buffer, fromJson!, DeliveryXmlContext.Default.Delivery);
            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
        }

        /// <summary>
        /// Converts XML to JSON with no type, by walking an <see cref="XmlNode"/> tree into a <see cref="JsonNode"/> tree.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void TreeToJson(TextWriter output)
        {
            var root = (XmlElementNode)XmlNode.Parse(DeliveryXml);

            var json = new JsonObject { [root.LocalName] = ToJson(root) };
            output.WriteLine(json.ToJsonString(s_jsonOptions));
        }

        /// <summary>
        /// Converts JSON to XML with no type, by walking a <see cref="JsonNode"/> tree into an <see cref="XmlNode"/> tree.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void TreeToXml(TextWriter output)
        {
            JsonNode? json = JsonNode.Parse(DeliveryJson);

            XmlElementNode root = ToXml("delivery", json);
            output.WriteLine(Write(root));
        }

        /// <summary>
        /// Takes JSON to XML and back with no type, to show what the trees cannot carry across.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void TreeRoundTrip(TextWriter output)
        {
            const string json = """{"id":1042,"signed":true,"items":["Keyboard"],"note":null}""";
            output.WriteLine(json);

            XmlElementNode xml = ToXml("delivery", JsonNode.Parse(json));
            output.WriteLine(xml.ToString());

            JsonNode? back = ToJson(xml);
            output.WriteLine(back!.ToJsonString());
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds a value to a JSON object under a name, turning the property into an array when the name repeats.
        /// </summary>
        /// <param name="json">The object.</param>
        /// <param name="name">The property name.</param>
        /// <param name="value">The value.</param>
        private static void Append(JsonObject json, string name, JsonNode? value)
        {
            if (!json.TryGetPropertyValue(name, out JsonNode? existing))
            {
                json[name] = value;
                return;
            }

            if (existing is not JsonArray array)
            {
                // The second element of this name: the property becomes an array, where the first one was.
                array = new JsonArray();
                json[name] = array;
                array.Add(existing);
            }

            array.Add(value);
        }

        /// <summary>
        /// Gives an element the content that a JSON value describes.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="json">The JSON value.</param>
        private static void Fill(XmlElementNode element, JsonNode? json)
        {
            if (json is JsonObject properties)
            {
                foreach (KeyValuePair<string, JsonNode?> property in properties)
                {
                    if (property.Key.StartsWith('@'))
                    {
                        element.SetAttribute(property.Key[1..], Text(property.Value));
                    }
                    else if (property.Key == "#text")
                    {
                        element.Add(new XmlTextNode(Text(property.Value)));
                    }
                    else if (property.Value is JsonArray array)
                    {
                        // XML has no array. Each item is an element with the name of the property.
                        foreach (JsonNode? item in array)
                        {
                            element.Add(ToXml(property.Key, item));
                        }
                    }
                    else
                    {
                        element.Add(ToXml(property.Key, property.Value));
                    }
                }
            }
            else if (json is JsonArray items)
            {
                // An array inside an array has no name to give its items.
                foreach (JsonNode? item in items)
                {
                    element.Add(ToXml("item", item));
                }
            }
            else if (json is not null)
            {
                element.Add(new XmlTextNode(Text(json)));
            }
        }

        /// <summary>
        /// Returns the text that XML holds for a JSON string, number, or Boolean.
        /// </summary>
        /// <param name="value">The JSON value.</param>
        /// <returns>The text, which is empty for <see langword="null"/>.</returns>
        private static string Text(JsonNode? value)
        {
            if (value is null)
            {
                return string.Empty;
            }

            return value.GetValueKind() is JsonValueKind.String ? value.GetValue<string>() : value.ToJsonString();
        }

        /// <summary>
        /// Converts an element to a JSON value: a string for an element that holds only text, <see langword="null"/>
        /// for an empty element, and otherwise an object.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The JSON value.</returns>
        private static JsonNode? ToJson(XmlElementNode element)
        {
            bool hasChildElements = element.Children.OfType<XmlElementNode>().Any();
            if (element.Attributes.Count == 0 && !hasChildElements)
            {
                return element.Children.Count == 0 ? null : element.InnerText;
            }

            var json = new JsonObject();
            foreach (XmlAttributeNode attribute in element.Attributes)
            {
                json["@" + attribute.LocalName] = attribute.Value;
            }

            var text = new StringBuilder();
            foreach (XmlNode child in element.Children)
            {
                if (child is XmlElementNode childElement)
                {
                    Append(json, childElement.LocalName, ToJson(childElement));
                }
                else if (child is XmlTextNode textNode)
                {
                    text.Append(textNode.Value);
                }
                else if (child is XmlCDataNode cdata)
                {
                    text.Append(cdata.Value);
                }
            }

            // Text between child elements is usually indentation. Text that says something is kept.
            if (!string.IsNullOrWhiteSpace(text.ToString()))
            {
                json["#text"] = text.ToString();
            }

            return json;
        }

        /// <summary>
        /// Converts a JSON value to an element with the given name.
        /// </summary>
        /// <param name="name">The name of the element. A JSON value has no name of its own.</param>
        /// <param name="json">The JSON value.</param>
        /// <returns>The element.</returns>
        private static XmlElementNode ToXml(string name, JsonNode? json)
        {
            var element = new XmlElementNode(name);
            Fill(element, json);
            return element;
        }

        /// <summary>
        /// Writes a tree as indented XML.
        /// </summary>
        /// <param name="node">The root of the tree.</param>
        /// <returns>The XML.</returns>
        private static string Write(XmlNode node)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { Indented = true, NewLine = "\n", OmitXmlDeclaration = true }))
            {
                node.WriteTo(writer);
            }

            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        #endregion

    }

}
