using System;
using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Shared drain helpers for well-formed and malformed reader tests.
    /// </summary>
    internal static class XmlReaderHarness
    {

        #region Public Methods

        public static void Drain(string xml, XmlReaderOptions? options = null)
        {
            Drain(Encoding.UTF8.GetBytes(xml), options ?? new XmlReaderOptions());
        }

        public static void Drain(byte[] xml, XmlReaderOptions? options = null)
        {
            var reader = new Utf8XmlReader(xml, options ?? new XmlReaderOptions());
            while (reader.Read())
            {
            }
        }

        /// <summary>
        /// Appends one line per remaining token: type, prefix, local name, namespace URI, value, and attributes.
        /// </summary>
        /// <param name="reader">The reader to drain.</param>
        /// <param name="tokens">The destination list.</param>
        public static void AppendTokens(ref Utf8XmlReader reader, System.Collections.Generic.List<string> tokens)
        {
            while (reader.Read())
            {
                var line = new StringBuilder();
                line.Append(reader.TokenType).Append(' ');
                AppendName(ref reader, line);
                if (reader.TokenType is XmlTokenType.StartElement)
                {
                    line.Append(reader.IsEmptyElement ? " empty" : " open");
                    while (reader.MoveToNextAttribute())
                    {
                        line.Append(" @");
                        AppendName(ref reader, line);
                        line.Append("=[").Append(reader.GetString()).Append(']');
                    }

                    reader.MoveToElement();
                }
                else if (reader.TokenType is not XmlTokenType.EndElement)
                {
                    line.Append(" [").Append(reader.GetString()).Append(']');
                }

                tokens.Add(line.ToString());
            }
        }

        /// <summary>
        /// Reads <paramref name="xml"/> in one pass and returns its token lines.
        /// </summary>
        /// <param name="xml">The document.</param>
        /// <param name="options">Reader options.</param>
        /// <returns>One line per token.</returns>
        public static System.Collections.Generic.List<string> Tokens(ReadOnlySpan<byte> xml, XmlReaderOptions options)
        {
            var tokens = new System.Collections.Generic.List<string>();
            var reader = new Utf8XmlReader(xml, options);
            AppendTokens(ref reader, tokens);
            return tokens;
        }

        /// <summary>
        /// Reads <paramref name="xml"/> as two buffers split at <paramref name="split"/>, resuming from the saved state.
        /// </summary>
        /// <param name="xml">The document.</param>
        /// <param name="split">The size of the first buffer.</param>
        /// <param name="options">Reader options.</param>
        /// <returns>One line per token across both buffers.</returns>
        public static System.Collections.Generic.List<string> TokensSplitAt(ReadOnlySpan<byte> xml, int split, XmlReaderOptions options)
        {
            var tokens = new System.Collections.Generic.List<string>();
            var first = new Utf8XmlReader(xml[..split], false, new XmlReaderState(options));
            AppendTokens(ref first, tokens);
            var second = new Utf8XmlReader(xml[(int)first.BytesConsumed..], true, first.CurrentState);
            AppendTokens(ref second, tokens);
            return tokens;
        }

        /// <summary>
        /// Reads <paramref name="xml"/> as a stream of <paramref name="chunkSize"/>-byte arrivals, resuming after each.
        /// </summary>
        /// <param name="xml">The document.</param>
        /// <param name="chunkSize">The number of new bytes made available per step.</param>
        /// <param name="options">Reader options.</param>
        /// <returns>One line per token across all buffers.</returns>
        /// <remarks>
        /// Each reader sees the bytes the previous one did not consume plus the next chunk, which is how a
        /// caller feeds a socket or pipe.
        /// </remarks>
        public static System.Collections.Generic.List<string> TokensChunked(ReadOnlySpan<byte> xml, int chunkSize, XmlReaderOptions options)
        {
            var tokens = new System.Collections.Generic.List<string>();
            var state = new XmlReaderState(options);
            var start = 0;
            var end = 0;
            while (true)
            {
                end = Math.Min(xml.Length, end + chunkSize);
                var isFinal = end == xml.Length;
                var reader = new Utf8XmlReader(xml[start..end], isFinal, state);
                AppendTokens(ref reader, tokens);
                start += (int)reader.BytesConsumed;
                state = reader.CurrentState;
                if (isFinal)
                {
                    return tokens;
                }
            }
        }

        public static void MustRead(string xml, XmlReaderOptions? options = null)
        {
            MustRead(Encoding.UTF8.GetBytes(xml), options);
        }

        public static void MustRead(byte[] xml, XmlReaderOptions? options = null)
        {
            // Reading without an error is the least of it: the document has to hold an element, and what is
            // read must not depend on where the input happens to be cut.
            var settings = options ?? new XmlReaderOptions();
            var expected = Tokens(xml, settings);
            expected.FindAll(static token => token.StartsWith("StartElement ", StringComparison.Ordinal)).Count.Should().BePositive("a well-formed document has a document element");
            for (var split = 1; split < xml.Length; split++)
            {
                TokensSplitAt(xml, split, settings).Should().Equal(expected, "the tokens must not depend on a split at byte {0}", split);
            }
        }

        public static void MustThrow(string xml, XmlReaderOptions? options = null)
        {
            var threw = false;
            try
            {
                Drain(xml, options);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue("expected XmlException for {0}", xml);
        }

        public static void MustThrow(byte[] xml, XmlReaderOptions? options = null)
        {
            var threw = false;
            try
            {
                Drain(xml, options);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue("expected XmlException for a binary payload of {0} bytes", xml.Length);
        }

        #endregion

        #region Private Methods

        private static void AppendName(ref Utf8XmlReader reader, StringBuilder line)
        {
            line.Append(Encoding.UTF8.GetString(reader.PrefixSpan)).Append(':')
                .Append(Encoding.UTF8.GetString(reader.LocalNameSpan))
                .Append('{').Append(Encoding.UTF8.GetString(reader.NamespaceUriSpan)).Append('}');
        }

        #endregion

    }

}
