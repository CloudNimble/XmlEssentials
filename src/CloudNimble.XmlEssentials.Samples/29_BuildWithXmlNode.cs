using System.Buffers;
using System.IO;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 29: builds a document from nothing with the mutable <see cref="XmlNode"/> tree and writes it.
    /// </summary>
    public static class BuildWithXmlNode
    {

        #region Public Methods

        /// <summary>
        /// Builds an order from elements, attributes, and text, and prints it.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var order = new XmlElementNode("Order");
            order.SetAttribute("id", "1042");

            XmlElementNode customer = order.AddElement("Customer");
            customer.Add(new XmlTextNode("Ada Lovelace"));

            order.AddElement("Item").InnerText = "Keyboard";
            order.AddElement("Item").InnerText = "Mouse & pad";
            order.AddElement("Notes");

            output.WriteLine(order.ToString());
        }

        /// <summary>
        /// Writes a tree to a <see cref="Utf8XmlWriter"/> with an XML declaration and indentation.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Declaration(TextWriter output)
        {
            var order = new XmlElementNode("Order");
            order.SetAttribute("id", "1042");
            order.AddElement("Item").InnerText = "Keyboard";

            var buffer = new ArrayBufferWriter<byte>();
            var options = new XmlWriterOptions { Indented = true, NewLine = "\n" };
            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                order.WriteTo(writer);
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));

            buffer.ResetWrittenCount();
            options = new XmlWriterOptions { OmitXmlDeclaration = true };
            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                new XmlDeclarationNode { Version = "1.0", Standalone = true }.WriteTo(writer);
                order.WriteTo(writer);
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
        }

        /// <summary>
        /// Puts elements and attributes in namespaces, with a default namespace and with prefixes.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Namespaces(TextWriter output)
        {
            const string atom = "http://www.w3.org/2005/Atom";
            const string dc = "http://purl.org/dc/elements/1.1/";

            var feed = new XmlElementNode("feed", atom);
            feed.AddElement("title", atom).InnerText = "Release notes";

            var creator = new XmlElementNode("creator", dc, "dc");
            creator.InnerText = "Ada Lovelace";
            feed.Add(creator);

            XmlElementNode link = feed.AddElement("link", atom);
            link.SetAttribute("href", "https://example.com/");
            link.Attributes.Add(new XmlAttributeNode("language", "en", dc, "dc"));

            output.WriteLine(feed.ToString());

            feed.Attributes.Add(new XmlAttributeNode("dc", dc, XmlNamespaces.Xmlns, "xmlns"));
            output.WriteLine(feed.ToString());

            output.WriteLine($"{creator.Prefix}:{creator.LocalName} is in {creator.NamespaceUri}");
        }

        /// <summary>
        /// Adds each kind of node an element can hold.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void NodeKinds(TextWriter output)
        {
            var note = new XmlElementNode("Note");
            note.Add(new XmlCommentNode(" reviewed "));
            note.Add(new XmlTextNode("Use <b> & </b> for bold. "));
            note.Add(new XmlCDataNode("<b>Raw</b> & unescaped"));
            note.Add(new XmlProcessingInstructionNode("print", "copies=\"2\""));
            note.AddElement("Signature");

            output.WriteLine(note.ToString());

            foreach (XmlNode child in note.Children)
            {
                output.WriteLine($"{child.GetType().Name}: {child.ValueKind}");
            }

            output.WriteLine($"InnerText: {note.InnerText}");
        }

        /// <summary>
        /// Shows the prefix the writer makes up for a namespace that has none, and the error for a prefix that has no namespace.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Prefixes(TextWriter output)
        {
            var item = new XmlElementNode("Item");
            item.SetAttribute("unit", "kg", "urn:example:units");
            output.WriteLine(item.ToString());

            var broken = new XmlElementNode("Item", namespaceUri: null, prefix: "u");
            try
            {
                broken.ToString();
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Calls <see cref="XmlNode.ToString"/> on nodes that are not elements.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ToStringOnParts(TextWriter output)
        {
            output.WriteLine(new XmlCommentNode(" reviewed ").ToString());
            output.WriteLine(new XmlProcessingInstructionNode("print", "copies=\"2\"").ToString());
            output.WriteLine(new XmlDeclarationNode { Version = "1.1" }.ToString());

            try
            {
                new XmlTextNode("Keyboard").ToString();
            }
            catch (XmlException ex)
            {
                output.WriteLine($"Text: {ex.Message}");
            }

            try
            {
                new XmlAttributeNode("id", "1042").ToString();
            }
            catch (XmlException ex)
            {
                output.WriteLine($"Attribute: {ex.Message}");
            }
        }

        #endregion

    }

}
