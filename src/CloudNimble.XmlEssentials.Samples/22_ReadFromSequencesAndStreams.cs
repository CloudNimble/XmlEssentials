using System;
using System.Buffers;
using System.IO;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Models.ReaderWriter;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 22: gives <see cref="Utf8XmlReader"/> its input as a sequence, as blocks read from a stream, and
    /// with a caller-supplied scratch buffer.
    /// </summary>
    public static class ReadFromSequencesAndStreams
    {

        #region Public Methods

        /// <summary>
        /// Reads a document that arrives as a <see cref="ReadOnlySequence{T}"/> made of two blocks.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var first = new BufferSegment("<Order><Item>Keyb"u8.ToArray());
            var last = first.Append("oard</Item><Item>Mouse</Item></Order>"u8.ToArray());
            var sequence = new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);

            var reader = new Utf8XmlReader(sequence);
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.Text)
                {
                    output.WriteLine($"Item: {reader.GetString()}");
                }
            }

            output.WriteLine($"Single segment: {sequence.IsSingleSegment}");
            output.WriteLine($"HasValueSequence: {reader.HasValueSequence}");
            output.WriteLine($"Consumed {reader.BytesConsumed} of {sequence.Length} bytes");
        }

        /// <summary>
        /// Reads a document from a stream a block at a time, carrying the reader's state from one block to the next.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadStream(TextWriter output)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(
                "<Orders><Order id=\"1042\"><Item>Keyboard</Item><Item>Mouse &amp; pad</Item></Order><Order id=\"1043\"><Item>Monitor stand</Item></Order></Orders>"));

            // The buffer is small so that the sample has to refill it and grow it. Use 4096 bytes or more in an application.
            var buffer = new byte[16];
            var buffered = 0;
            var isFinalBlock = false;
            var state = new XmlReaderState(new XmlReaderOptions());

            // What the application knows about its place in the document. The reader does not keep this between blocks.
            var inItem = false;
            var blocks = 0;

            while (!isFinalBlock)
            {
                var read = stream.Read(buffer, buffered, buffer.Length - buffered);
                buffered += read;
                isFinalBlock = read == 0;
                blocks++;

                var reader = new Utf8XmlReader(buffer.AsSpan(0, buffered), isFinalBlock, state);
                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case XmlTokenType.StartElement when reader.LocalNameEquals("Order"u8):
                            while (reader.MoveToNextAttribute())
                            {
                                if (reader.LocalNameEquals("id"u8))
                                {
                                    output.WriteLine($"Order {reader.GetInt32()}");
                                }
                            }

                            break;

                        case XmlTokenType.StartElement:
                            inItem = reader.LocalNameEquals("Item"u8);
                            break;

                        case XmlTokenType.Text when inItem:
                            output.WriteLine($"  Item: {reader.GetString()}");
                            break;

                        case XmlTokenType.EndElement:
                            inItem = false;
                            break;
                    }
                }

                // Keep the bytes the reader did not consume. They are the start of a token that is not complete yet.
                state = reader.CurrentState;
                var consumed = (int)reader.BytesConsumed;
                buffer.AsSpan(consumed, buffered - consumed).CopyTo(buffer);
                buffered -= consumed;

                // A full buffer with nothing consumed means the next token is larger than the buffer.
                if (buffered == buffer.Length)
                {
                    Array.Resize(ref buffer, buffer.Length * 2);
                }
            }

            output.WriteLine($"Read {stream.Length} bytes in {blocks} blocks. The buffer grew to {buffer.Length} bytes.");
        }

        /// <summary>
        /// Gives the reader a scratch buffer from the stack so that reading the document allocates nothing.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadWithScratch(TextWriter output)
        {
            var xml = "<Orders><Order a=\"1\" b=\"2\" c=\"3\" d=\"4\" e=\"5\" f=\"6\" g=\"7\" h=\"8\" i=\"9\"><Item>Fish &amp; chips</Item><Item>Tea &amp; toast</Item></Order></Orders>"u8;
            Span<byte> scratch = stackalloc byte[1024];

            // Run each path once first, so that the measurement does not include one-time setup.
            CountEscapedValues(xml);
            CountEscapedValues(xml, scratch);

            var before = GC.GetAllocatedBytesForCurrentThread();
            var count = CountEscapedValues(xml);
            var withoutScratch = GC.GetAllocatedBytesForCurrentThread() - before;

            before = GC.GetAllocatedBytesForCurrentThread();
            count += CountEscapedValues(xml, scratch);
            var withScratch = GC.GetAllocatedBytesForCurrentThread() - before;

            output.WriteLine($"Escaped values found in two passes: {count}");
            output.WriteLine($"Without a scratch buffer, the reader allocated: {(withoutScratch > 0 ? "yes" : "no")}");
            output.WriteLine($"With a scratch buffer, the reader allocated: {withScratch} bytes");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Counts the text values that hold a reference, with a reader that allocates the memory it needs.
        /// </summary>
        /// <param name="utf8Xml">The document.</param>
        /// <returns>The number of text values that the reader had to decode.</returns>
        private static int CountEscapedValues(ReadOnlySpan<byte> utf8Xml)
        {
            var reader = new Utf8XmlReader(utf8Xml, new XmlReaderOptions());
            return CountEscapedValues(ref reader);
        }

        /// <summary>
        /// Counts the text values that hold a reference, with a reader that takes its memory from a scratch buffer.
        /// </summary>
        /// <param name="utf8Xml">The document.</param>
        /// <param name="scratch">The memory the reader uses before it allocates any.</param>
        /// <returns>The number of text values that the reader had to decode.</returns>
        private static int CountEscapedValues(ReadOnlySpan<byte> utf8Xml, Span<byte> scratch)
        {
            var reader = new Utf8XmlReader(utf8Xml, scratch, new XmlReaderOptions());
            return CountEscapedValues(ref reader);
        }

        /// <summary>
        /// Reads to the end of the document and counts the text values that hold a reference.
        /// </summary>
        /// <param name="reader">The reader, passed by reference.</param>
        /// <returns>The number of text values that the reader had to decode.</returns>
        private static int CountEscapedValues(ref Utf8XmlReader reader)
        {
            var count = 0;
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.Text && reader.ValueIsEscaped)
                {
                    count++;
                }
            }

            return count;
        }

        #endregion

    }

}
