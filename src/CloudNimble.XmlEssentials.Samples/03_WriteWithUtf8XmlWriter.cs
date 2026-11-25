using System.Buffers;
using System.IO;
using System.Text;
using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 3: writes a UTF-8 document with <see cref="Utf8XmlWriter"/>.
    /// </summary>
    public static class WriteWithUtf8XmlWriter
    {

        #region Public Methods

        /// <summary>
        /// Writes an indented order with an attribute, text elements, and a number.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var buffer = new ArrayBufferWriter<byte>();
            var options = new XmlWriterOptions { Indented = true, NewLine = "\n" };

            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                writer.WriteStartElement("Order");
                writer.WriteAttributeString("id", "1042");
                writer.WriteElementString("Item", "Keyboard");
                writer.WriteElementString("Item", "Mouse & pad");
                writer.WriteElementNumber("Total", 129.90m);
                writer.WriteEndElement();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
        }

        #endregion

    }

}
