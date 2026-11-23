using System.Buffers;
using System.IO;
using System.Text;
using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 42: the <see cref="Utf8XmlWriter"/> code that replaces <c>System.Xml.XmlWriter</c> code.
    /// </summary>
    public static class MigrateFromXmlWriter
    {

        #region Public Methods

        /// <summary>
        /// Writes a fragment with no XML declaration into a buffer.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Fragment(TextWriter output)
        {
            var buffer = new ArrayBufferWriter<byte>();
            var options = new XmlWriterOptions { OmitXmlDeclaration = true };

            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                writer.WriteStartElement("Item");
                writer.WriteAttributeString("sku", "KB-01");
                writer.WriteStringValue("Keyboard");
                writer.WriteEndElement();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
        }

        /// <summary>
        /// Writes an indented inventory document to a stream: a namespace, an attribute, typed values, a comment, and a CDATA section.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            const string ns = "urn:contoso:inventory";

            using var stream = new MemoryStream();
            var options = new XmlWriterOptions { Indented = true, NewLine = "\n" };

            using (var writer = new Utf8XmlWriter(stream, options))
            {
                writer.WriteStartElement("inv", "Inventory", ns);

                writer.WriteStartElement("inv", "Item", ns);
                writer.WriteAttributeString("sku", "KB-01");
                writer.WriteElementString("Name", ns, "Keyboard & stand");

                writer.WriteStartElement("Price", ns);
                writer.WriteNumberValue(49.90m);
                writer.WriteEndElement();

                writer.WriteStartElement("InStock", ns);
                writer.WriteBooleanValue(true);
                writer.WriteEndElement();

                writer.WriteStartElement("Notes", ns);
                writer.WriteCData("<b>Ships in 2 days</b>");
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.Flush();
            }

            output.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
        }

        /// <summary>
        /// Disposes a writer that still has open elements. <c>System.Xml.XmlWriter</c> closes them; this writer throws.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void UnclosedElementIsRejected(TextWriter output)
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8XmlWriter(buffer);

            writer.WriteStartElement("Order");
            writer.WriteStartElement("Item");

            try
            {
                writer.Dispose();
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        #endregion

    }

}
