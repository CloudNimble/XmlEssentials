using System;
using System.Buffers;
using System.IO;
using System.Text;
using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 24: writes namespaces, the XML declaration, indentation, comments, CDATA sections, processing
    /// instructions, and raw XML with <see cref="Utf8XmlWriter"/>.
    /// </summary>
    public static class WriteNamespacesAndMarkup
    {

        #region Public Methods

        /// <summary>
        /// Writes elements and attributes in namespaces, with prefixes you choose and prefixes the writer chooses.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            const string atom = "http://www.w3.org/2005/Atom";
            const string dublinCore = "http://purl.org/dc/elements/1.1/";
            const string shop = "urn:example:shop";

            var buffer = new ArrayBufferWriter<byte>();
            var options = new XmlWriterOptions { Indented = true, NewLine = "\n", OmitXmlDeclaration = true };

            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                // The writer declares the default namespace because nothing in scope is bound to it.
                writer.WriteStartElement("feed", atom);

                // Declare a prefix on the root so that every descendant can use it.
                writer.WriteNamespaceDeclaration("dc", dublinCore);

                // The namespace is the default one in scope, so the element needs no prefix.
                writer.WriteElementString("title", atom, "Orders");

                // The namespace is bound to dc, so the writer uses that prefix.
                writer.WriteElementString("creator", dublinCore, "Ada");

                // A prefix and a namespace that are not in scope: the writer declares them on this element.
                writer.WriteStartElement("s", "order", shop);
                writer.WriteAttributeString("s", "id", shop, "1042");
                writer.WriteEndElement();

                // An attribute in a namespace that has no prefix in scope gets a generated one.
                writer.WriteStartElement("entry", atom);
                writer.WriteAttributeString("priority", shop, "high");
                writer.WriteAttributeString("lang", "en");
                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
        }

        /// <summary>
        /// Writes the same element with each form of the XML declaration.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void WriteDeclaration(TextWriter output)
        {
            output.WriteLine(WriteOrder(new XmlWriterOptions()));
            output.WriteLine(WriteOrder(new XmlWriterOptions { OmitXmlDeclaration = true }));
            output.WriteLine(WriteOrder(new XmlWriterOptions { Standalone = true }));
            output.WriteLine(WriteOrder(new XmlWriterOptions { Version = XmlVersion.Version11 }));

            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { EmitByteOrderMark = true }))
            {
                writer.WriteEmptyElement("Order");
            }

            output.WriteLine($"With a byte order mark, the output starts with: {Convert.ToHexString(buffer.WrittenSpan[..3])}");
        }

        /// <summary>
        /// Writes one document with three indentation settings.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void WriteIndented(TextWriter output)
        {
            output.WriteLine("Not indented:");
            output.WriteLine(WriteParagraph(new XmlWriterOptions { OmitXmlDeclaration = true }));

            output.WriteLine("Indented:");
            output.WriteLine(WriteParagraph(new XmlWriterOptions { OmitXmlDeclaration = true, Indented = true, NewLine = "\n" }));

            output.WriteLine("Indented with four spaces:");
            output.WriteLine(WriteParagraph(new XmlWriterOptions { OmitXmlDeclaration = true, Indented = true, IndentSize = 4, NewLine = "\n" }));
        }

        /// <summary>
        /// Writes a processing instruction, comments, a CDATA section, and XML that is already formatted.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void WriteMarkup(TextWriter output)
        {
            var buffer = new ArrayBufferWriter<byte>();
            var options = new XmlWriterOptions { Indented = true, NewLine = "\n" };

            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                writer.WriteProcessingInstruction("xml-stylesheet", "href=\"order.xsl\" type=\"text/xsl\"");
                writer.WriteComment(" Exported by the order service ");

                writer.WriteStartElement("Order");
                writer.WriteComment(" One line per item ");

                writer.WriteStartElement("Script");
                writer.WriteCData("if (a < b && b > c) { ship(); }");
                writer.WriteEndElement();

                writer.WriteStartElement("Address");
                writer.WriteRawValue("<City>London</City><Country>UK</Country>"u8);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
        }

        /// <summary>
        /// Writes a control character and a next-line character as XML 1.1 so that they are read back unchanged.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void WriteVersion11(TextWriter output)
        {
            var buffer = new ArrayBufferWriter<byte>();

            using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { Version = XmlVersion.Version11 }))
            {
                writer.WriteElementString("Note", "bell\u0007 next line\u0085end");
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));

            try
            {
                using var writer = new Utf8XmlWriter(new ArrayBufferWriter<byte>());
                writer.WriteElementString("Note", "bell\u0007");
            }
            catch (XmlException ex)
            {
                output.WriteLine($"XML 1.0: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Writes an empty <c>Order</c> element with the specified options.
        /// </summary>
        /// <param name="options">The options to write with.</param>
        /// <returns>The document as a string.</returns>
        private static string WriteOrder(XmlWriterOptions options)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                writer.WriteEmptyElement("Order");
            }

            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        /// <summary>
        /// Writes a small document that has an element with children only and an element that mixes text with a child.
        /// </summary>
        /// <param name="options">The options to write with.</param>
        /// <returns>The document as a string.</returns>
        private static string WriteParagraph(XmlWriterOptions options)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                writer.WriteStartElement("Order");
                writer.WriteStartElement("Item");
                writer.WriteElementString("Name", "Keyboard");
                writer.WriteEndElement();
                writer.WriteStartElement("Note");
                writer.WriteStringValue("Leave with ");
                writer.WriteElementString("b", "Ada");
                writer.WriteStringValue(" only.");
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        #endregion

    }

}
