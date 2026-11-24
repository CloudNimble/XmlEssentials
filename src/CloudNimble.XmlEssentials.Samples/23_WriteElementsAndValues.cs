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
        /// Writes one document three ways to show the settings that lay out the output.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ControlLayout(TextWriter output)
        {
            var asSystemXml = new XmlWriterOptions
            {
                EmptyElementStyle = XmlEmptyElementStyle.SelfClosingWithSpace,
                Indented = true,
                NewLine = "\n",
                OmitXmlDeclaration = true,
            };
            var onePerLine = asSystemXml with { AttributeStyle = XmlAttributeStyle.PerLine };
            var twoPerLine = onePerLine with { AttributesPerLine = 2, EmptyElementStyle = XmlEmptyElementStyle.StartAndEndTag };

            foreach (XmlWriterOptions options in new[] { asSystemXml, onePerLine, twoPerLine })
            {
                var buffer = new ArrayBufferWriter<byte>();
                using (var writer = new Utf8XmlWriter(buffer, options))
                {
                    writer.WriteStartElement("Order");
                    writer.WriteAttributeString("id", "1042");
                    writer.WriteAttributeString("status", "open");
                    writer.WriteAttributeString("region", "EU");
                    writer.WriteElementString("Customer", "Ada Lovelace");
                    writer.WriteStartElement("Notes");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
            }
        }

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
        /// Writes elements with no content as a start tag and an end tag.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ChooseEmptyElementForm(TextWriter output)
        {
            var buffer = new ArrayBufferWriter<byte>();
            var options = new XmlWriterOptions
            {
                EmptyElementStyle = XmlEmptyElementStyle.StartAndEndTag,
                Indented = true,
                NewLine = "\n",
                OmitXmlDeclaration = true,
            };

            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                writer.WriteStartElement("a");
                writer.WriteStartElement("b");
                writer.WriteEndElement();
                writer.WriteEmptyElement("c");
                writer.WriteEndElement();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
        }

        /// <summary>
        /// Writes a line break in text with the line ending the options name.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ChooseLineEnding(TextWriter output)
        {
            var buffer = new ArrayBufferWriter<byte>();
            var options = new XmlWriterOptions { NewLine = "\r\n", OmitXmlDeclaration = true };

            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                writer.WriteStartElement("r");
                writer.WriteStringValue("a\nb");
                writer.WriteEndElement();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan).Replace("\r", "\\r"));
        }

        /// <summary>
        /// Writes the attributes of each start tag one to a line, and then two to a line.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void PutAttributesOnLines(TextWriter output)
        {
            var onePerLine = new XmlWriterOptions
            {
                AttributeStyle = XmlAttributeStyle.PerLine,
                Indented = true,
                NewLine = "\n",
                OmitXmlDeclaration = true,
            };

            foreach (XmlWriterOptions options in new[] { onePerLine, onePerLine with { AttributesPerLine = 2 } })
            {
                var buffer = new ArrayBufferWriter<byte>();
                using (var writer = new Utf8XmlWriter(buffer, options))
                {
                    writer.WriteStartElement("order");
                    writer.WriteAttributeString("a", "1");
                    writer.WriteAttributeString("b", "2");
                    writer.WriteAttributeString("c", "3");
                    writer.WriteStartElement("line");
                    writer.WriteAttributeString("x", "1");
                    writer.WriteAttributeString("y", "2");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
            }
        }

        /// <summary>
        /// Shows how the writer escapes text and attribute values.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void EscapeValues(TextWriter output)
        {
            var buffer = new ArrayBufferWriter<byte>();
            var options = new XmlWriterOptions { NewLine = "\n", OmitXmlDeclaration = true };

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
