using System.Buffers;
using System.Globalization;
using System.IO;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;
using CloudNimble.XmlEssentials.Samples.Models.Dom;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 28: writes a parsed <see cref="XmlDocument"/> back out, and converts it to a mutable tree and to objects.
    /// </summary>
    public static class WriteAndConvertXmlDocument
    {

        #region Public Methods

        /// <summary>
        /// Parses a document, writes it to a <see cref="Utf8XmlWriter"/>, and compares the result with the input.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            const string xml = """<?xml version="1.0" encoding="utf-8"?><!-- exported nightly --><c:Catalog xmlns:c="urn:example:catalog" updated="2026-03-14"><c:Product sku="KB-1">Keyboard</c:Product><c:Discontinued></c:Discontinued><c:Spare/></c:Catalog>""";

            using XmlDocument document = XmlDocument.Parse(xml);

            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer))
            {
                document.WriteTo(writer);
            }

            string written = Encoding.UTF8.GetString(buffer.WrittenSpan);
            output.WriteLine(written);
            output.WriteLine($"Same as the input: {written == xml}");
        }

        /// <summary>
        /// Copies a parsed element into a mutable tree that outlives the document.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void CloneToNode(TextWriter output)
        {
            XmlElementNode product;

            using (XmlDocument document = XmlDocument.Parse("""<Catalog><Product sku="KB-1"><Name>Keyboard</Name></Product></Catalog>"""))
            {
                product = (XmlElementNode)document.RootElement.GetChild("Product").CloneNode();
            }

            product.SetAttribute("sku", "KB-2");
            product.AddElement("Price").InnerText = "49.90";

            output.WriteLine(product.ToString());
        }

        /// <summary>
        /// Writes a document that has no XML declaration to a writer that adds one and to a writer that does not,
        /// and then a document that has one to a writer that omits its own.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Declarations(TextWriter output)
        {
            using XmlDocument document = XmlDocument.Parse("""<Order id="1042"><Item>Keyboard</Item></Order>""");

            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer))
            {
                document.WriteTo(writer);
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));

            buffer.ResetWrittenCount();
            using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true }))
            {
                document.WriteTo(writer);
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));

            using XmlDocument declared = XmlDocument.Parse("""<?xml version="1.0" encoding="utf-8" standalone="yes"?><Order id="1042"/>""");

            buffer.ResetWrittenCount();
            using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true }))
            {
                declared.WriteTo(writer);
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
        }

        /// <summary>
        /// Deserializes a whole document, and then one element at a time, with <see cref="XmlSerializer"/>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Deserialize(TextWriter output)
        {
            using (XmlDocument single = XmlDocument.Parse("<Product><Name>Keyboard</Name><Price>49.90</Price></Product>"))
            {
                Product? product = XmlSerializer.Deserialize<Product>(single);
                output.WriteLine($"{product!.Name}: {product.Price.ToString(CultureInfo.InvariantCulture)}");
            }

            const string xml = """
                <Catalog>
                  <Product><Name>Mouse</Name><Price>19.90</Price></Product>
                  <Product><Name>Monitor</Name><Price>249.00</Price></Product>
                </Catalog>
                """;

            using XmlDocument catalog = XmlDocument.Parse(xml);
            foreach (XmlElement element in catalog.RootElement.EnumerateChildren())
            {
                if (element.ValueKind is not (XmlValueKind.Element or XmlValueKind.EmptyElement or XmlValueKind.Nil))
                {
                    continue;
                }

                Product? product = XmlSerializer.Deserialize<Product>(element);
                output.WriteLine($"{product!.Name}: {product.Price.ToString(CultureInfo.InvariantCulture)}");
            }
        }

        /// <summary>
        /// Writes back a document whose spelling differs from the writer's, to show what a round trip keeps and what it normalizes.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Layout(TextWriter output)
        {
            const string xml =
                "<?xml version='1.0'?>\r\n" +
                "<!-- exported nightly -->\r\n" +
                "<Order  id='1042'   status=\"open\">\r\n" +
                "  <!-- rush -->\r\n" +
                "  <Item>Keyboard &#65;</Item>\r\n" +
                "  <Notes />\r\n" +
                "</Order>\r\n";

            using XmlDocument document = XmlDocument.Parse(xml);

            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { NewLine = "\n" }))
            {
                document.WriteTo(writer);
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan).Replace("\r", "\\r"));
        }

        /// <summary>
        /// Serializes an object straight into a document, without text in between.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void SerializeToDocument(TextWriter output)
        {
            var product = new Product { Name = "Keyboard", Price = 49.90m };

            using XmlDocument document = XmlSerializer.SerializeToDocument(product);
            XmlElement root = document.RootElement;

            output.WriteLine(root.GetRawXml());
            output.WriteLine($"Price as a decimal: {root.GetChild("Price").GetDecimal().ToString(CultureInfo.InvariantCulture)}");
        }

        /// <summary>
        /// Writes one element of a document inside new markup.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void WriteElement(TextWriter output)
        {
            const string xml = """<c:Catalog xmlns:c="urn:example:catalog"><c:Product sku="KB-1">Keyboard</c:Product><c:Product sku="MS-2">Mouse</c:Product></c:Catalog>""";

            using XmlDocument document = XmlDocument.Parse(xml);

            var buffer = new ArrayBufferWriter<byte>();
            var options = new XmlWriterOptions { Indented = true, NewLine = "\n", OmitXmlDeclaration = true };
            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                writer.WriteStartElement("Picked");
                foreach (XmlElement product in document.RootElement.EnumerateChildren())
                {
                    if (product.GetAttribute("sku").GetString() == "MS-2")
                    {
                        product.WriteTo(writer);
                    }
                }

                writer.WriteEndElement();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
        }

        #endregion

    }

}
