using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 25: flushes, reuses, and disposes a <see cref="Utf8XmlWriter"/>, and shows what its validation
    /// and limits reject.
    /// </summary>
    public static class ManageWriterLifetime
    {

        #region Fields

        private static readonly XmlEncodedText ItemName = XmlEncodedText.Encode("Item");

        private static readonly XmlEncodedText SoldOutName = XmlEncodedText.Encode("SoldOut"u8);

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows how <see cref="Utf8XmlWriter.BytesPending"/> and <see cref="Utf8XmlWriter.BytesCommitted"/> change
        /// when the writer is flushed.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });

            writer.WriteStartElement("Order");
            writer.WriteElementString("Item", "Keyboard");
            output.WriteLine($"Before Flush: pending = {writer.BytesPending}, committed = {writer.BytesCommitted}, in the buffer = {buffer.WrittenCount}");

            writer.Flush();
            output.WriteLine($"After Flush: pending = {writer.BytesPending}, committed = {writer.BytesCommitted}, in the buffer = {buffer.WrittenCount}");
            output.WriteLine($"The buffer holds: {Encoding.UTF8.GetString(buffer.WrittenSpan)}");

            writer.WriteEndElement();
            writer.Flush();
            output.WriteLine($"After the end tag: pending = {writer.BytesPending}, committed = {writer.BytesCommitted}, depth = {writer.CurrentDepth}");
            output.WriteLine($"The buffer holds: {Encoding.UTF8.GetString(buffer.WrittenSpan)}");
        }

        /// <summary>
        /// Writes to a stream and flushes it without blocking a thread.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static async Task FlushToStreamAsync(TextWriter output)
        {
            using var stream = new MemoryStream();

            await using (var writer = new Utf8XmlWriter(stream, new XmlWriterOptions { OmitXmlDeclaration = true }))
            {
                writer.WriteStartElement("Orders");
                for (var id = 1042; id <= 1044; id++)
                {
                    writer.WriteStartElement("Order");
                    writer.WriteAttributeNumber("id", id);
                    writer.WriteEndElement();

                    await writer.FlushAsync();
                    output.WriteLine($"Order {id} flushed: the stream holds {stream.Length} bytes");
                }

                writer.WriteEndElement();
            }

            output.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
        }

        /// <summary>
        /// Writes several documents with one writer by calling <see cref="Utf8XmlWriter.Reset()"/> between them.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReuseWriter(TextWriter output)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });

            string[] items = ["Keyboard", "Mouse", "Monitor"];
            foreach (var item in items)
            {
                writer.WriteStartElement("Item");
                writer.WriteStringValue(item);
                writer.WriteEndElement();
                writer.Flush();

                output.WriteLine($"{Encoding.UTF8.GetString(buffer.WrittenSpan)} ({writer.BytesCommitted} bytes)");

                buffer.ResetWrittenCount();
                writer.Reset();
            }

            var other = new ArrayBufferWriter<byte>();
            writer.Reset(other);
            writer.WriteEmptyElement("Done");
            writer.Flush();
            output.WriteLine($"Second buffer: {Encoding.UTF8.GetString(other.WrittenSpan)}");
        }

        /// <summary>
        /// Writes element names that were encoded as UTF-8 once, ahead of time.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void WriteEncodedNames(TextWriter output)
        {
            var buffer = new ArrayBufferWriter<byte>();
            string?[] items = ["Keyboard", "Mouse", null];

            using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true }))
            {
                writer.WriteStartElement("Items"u8);
                foreach (var item in items)
                {
                    if (item is null)
                    {
                        writer.WriteEmptyElement(SoldOutName);
                        continue;
                    }

                    writer.WriteStartElement(ItemName);
                    writer.WriteStringValue(item);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
            output.WriteLine($"{ItemName} is {ItemName.EncodedUtf8Bytes.Length} bytes");
        }

        /// <summary>
        /// Shows the mistakes that the writer's validation catches, and the message of each exception.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ValidateOutput(TextWriter output)
        {
            output.WriteLine(Try(writer => writer.WriteStartElement("order id")));

            output.WriteLine(Try(writer =>
            {
                writer.WriteStartElement("Order");
                writer.WriteAttributeString("id", "1042");
                writer.WriteAttributeString("id", "1043");
            }));

            output.WriteLine(Try(writer =>
            {
                writer.WriteEmptyElement("Order");
                writer.WriteEmptyElement("Order");
            }));

            output.WriteLine(Try(writer => writer.WriteStringValue("text before the root")));

            output.WriteLine(Try(writer =>
            {
                writer.WriteStartElement("Order");
                writer.WriteStringValue("Keyboard");
                writer.WriteAttributeString("id", "1042");
            }));

            output.WriteLine(Try(writer =>
            {
                writer.WriteStartElement("Order");
                writer.WriteComment("ends with a dash -");
            }));

            output.WriteLine(Try(writer =>
            {
                writer.WriteStartElement("Order");
                writer.WriteCData("a ]]> b");
            }));

            output.WriteLine(Try(writer =>
            {
                writer.WriteStartElement("Order");
                writer.WriteStringValue("null character: \0");
            }));

            output.WriteLine(Try(writer =>
            {
                writer.WriteEmptyElement("Order");
                writer.WriteEndElement();
            }));

            output.WriteLine(Try(writer =>
            {
                writer.WriteStartElement("Order");
                writer.Dispose();
            }));
        }

        /// <summary>
        /// Shows that an exception from <see cref="Utf8XmlWriter.Dispose"/> replaces the exception that left the
        /// document unfinished when the writer is in a <see langword="using"/> statement.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void DisposeAfterAnError(TextWriter output)
        {
            try
            {
                using var writer = new Utf8XmlWriter(new ArrayBufferWriter<byte>());
                writer.WriteStartElement("Order");
                writer.WriteStartElement("order id");
            }
            catch (XmlException ex)
            {
                output.WriteLine($"Caught: {ex.Message}");
            }
        }

        /// <summary>
        /// Writes with <see cref="XmlWriterOptions.SkipValidation"/> and shows what the writer no longer checks.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void SkipValidation(TextWriter output)
        {
            var buffer = new ArrayBufferWriter<byte>();
            var options = new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true };

            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                writer.WriteStartElement("Order");
                writer.WriteAttributeString("id", "1042");
                writer.WriteAttributeString("id", "1043");
                writer.WriteElementString("2nd item", "Fish & chips");
                writer.WriteEndElement();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));

            try
            {
                var reader = new Utf8XmlReader(buffer.WrittenSpan);
                while (reader.Read())
                {
                }
            }
            catch (XmlException ex)
            {
                output.WriteLine($"Reading it back: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets limits on the writer and shows the exception that each one throws.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void EnforceLimits(TextWriter output)
        {
            output.WriteLine(Try(
                writer =>
                {
                    writer.WriteStartElement("Order");
                    writer.WriteStartElement("Customer");
                    writer.WriteStartElement("Name");
                },
                new XmlWriterOptions { MaxDepth = 2 }));

            output.WriteLine(Try(
                writer =>
                {
                    writer.WriteStartElement("Order");
                    writer.WriteAttributeString("id", "1042");
                    writer.WriteAttributeString("rush", "true");
                },
                new XmlWriterOptions { MaxAttributeCount = 1 }));

            output.WriteLine(Try(
                writer => writer.WriteStartElement("Order"),
                new XmlWriterOptions { MaxNameByteCount = 4 }));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Runs some writes against a new writer and reports the exception they throw.
        /// </summary>
        /// <param name="write">The writes to run.</param>
        /// <param name="options">The options to create the writer with.</param>
        /// <returns>The type and message of the exception, or a line that says nothing was thrown.</returns>
        private static string Try(Action<Utf8XmlWriter> write, XmlWriterOptions options = default)
        {
            var writer = new Utf8XmlWriter(new ArrayBufferWriter<byte>(), options);
            try
            {
                write(writer);
                return "No exception.";
            }
            catch (XmlException ex)
            {
                return $"XmlException: {ex.Message}";
            }
        }

        #endregion

    }

}
