using System;
using System.Buffers;
using System.IO;
using System.Text;
using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 23: writes elements, attributes, text, and typed values with <see cref="Utf8XmlWriter"/>.
    /// </summary>
    public static class WriteElementsAndValues
    {

        #region Public Methods

        /// <summary>
        /// Writes a document to an <see cref="IBufferWriter{T}"/> with start tags, attributes, text, and end tags.
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

                writer.WriteStartElement("Customer");
                writer.WriteStringValue("Ada Lovelace");
                writer.WriteEndElement();

                writer.WriteStartElement("Total");
                writer.WriteAttributeString("currency", "EUR");
                writer.WriteNumberValue(129.90m);
                writer.WriteEndElement();

                writer.WriteStartElement("Notes");
                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
        }

        /// <summary>
        /// Writes a document to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void WriteToStream(TextWriter output)
        {
            using var stream = new MemoryStream();

            using (var writer = new Utf8XmlWriter(stream))
            {
                writer.WriteStartElement("Order");
                writer.WriteAttributeString("id", "1042");
                writer.WriteElementString("Item", "Keyboard");
                writer.WriteEndElement();
            }

            output.WriteLine($"{stream.Length} bytes");
            output.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
        }

        /// <summary>
        /// Writes numbers, Booleans, dates, times, durations, GUIDs, and binary data as element and attribute values.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void WriteTypedValues(TextWriter output)
        {
            var buffer = new ArrayBufferWriter<byte>();
            var options = new XmlWriterOptions { Indented = true, NewLine = "\n", OmitXmlDeclaration = true };

            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                writer.WriteStartElement("Shipment");
                writer.WriteAttributeNumber("boxes", 3);
                writer.WriteStartAttribute("insured");
                writer.WriteBooleanValue(true);
                writer.WriteEndAttribute();

                writer.WriteStartElement("Id");
                writer.WriteStringValue(new Guid("3f2504e0-4f89-11d3-9a0c-0305e82c3301"));
                writer.WriteEndElement();

                writer.WriteStartElement("Sent");
                writer.WriteStringValue(new DateTimeOffset(2026, 3, 14, 9, 30, 0, TimeSpan.FromHours(1)));
                writer.WriteEndElement();

                writer.WriteStartElement("Due");
                writer.WriteStringValue(new DateOnly(2026, 3, 17));
                writer.WriteEndElement();

                writer.WriteStartElement("Cutoff");
                writer.WriteStringValue(new TimeOnly(17, 0, 0));
                writer.WriteEndElement();

                writer.WriteStartElement("Transit");
                writer.WriteStringValue(TimeSpan.FromHours(36));
                writer.WriteEndElement();

                writer.WriteStartElement("WeightKg");
                writer.WriteNumberValue(2.5);
                writer.WriteEndElement();

                writer.WriteStartElement("MaxWeightKg");
                writer.WriteNumberValue(double.PositiveInfinity);
                writer.WriteEndElement();

                writer.WriteStartElement("Fragile");
                writer.WriteBooleanValue(false);
                writer.WriteEndElement();

                writer.WriteStartElement("Signature");
                writer.WriteBase64StringValue([0x58, 0x4D, 0x4C, 0x21]);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
        }

        /// <summary>
        /// Writes complete elements with one call each.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void WriteCompleteElements(TextWriter output)
        {
            var buffer = new ArrayBufferWriter<byte>();
            var options = new XmlWriterOptions { Indented = true, NewLine = "\n", OmitXmlDeclaration = true };

            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                writer.WriteStartElement("Order");
                writer.WriteElementString("Item", "Keyboard");
                writer.WriteElementNumber("Quantity", 2);
                writer.WriteElementNumber("Total", 129.90m);
                writer.WriteElementBoolean("Rush", true);
                writer.WriteElementString("Placed", new DateTime(2026, 3, 14, 9, 30, 0, DateTimeKind.Utc));
                writer.WriteElementBase64String("Signature", [0x58, 0x4D, 0x4C, 0x21]);
                writer.WriteElementString("Comment", null);
                writer.WriteEmptyElement("Gift");
                writer.WriteNilElement("Discount");

                writer.WriteStartElement("Notes");
                writer.WriteFullEndElement();

                writer.WriteEndElement();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
        }

        /// <summary>
        /// Shows how the writer escapes text and attribute values.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void EscapeValues(TextWriter output)
        {
            var buffer = new ArrayBufferWriter<byte>();
            var options = new XmlWriterOptions { OmitXmlDeclaration = true };

            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                writer.WriteStartElement("Note");
                writer.WriteAttributeString("title", "Tom & \"Jerry\" <draft>\tline one\nline two");
                writer.WriteStringValue("if (a < b && b > c) { x = \"y\"; } ]]> done\r\n");
                writer.WriteEndElement();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));

            var reader = new Utf8XmlReader(buffer.WrittenSpan);
            reader.Read();
            reader.MoveToNextAttribute();
            output.WriteLine($"Attribute read back unchanged: {reader.GetString() == "Tom & \"Jerry\" <draft>\tline one\nline two"}");
            reader.Read();
            output.WriteLine($"Text read back unchanged: {reader.GetString() == "if (a < b && b > c) { x = \"y\"; } ]]> done\r\n"}");
        }

        #endregion

    }

}
