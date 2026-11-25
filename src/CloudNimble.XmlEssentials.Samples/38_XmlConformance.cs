using System;
using System.Buffers;
using System.IO;
using System.Text;
using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 38: shows the XML rules the reader and writer apply, and the constructs they reject.
    /// </summary>
    public static class XmlConformance
    {

        #region Public Methods

        /// <summary>
        /// Reads the same text under XML 1.0 and XML 1.1 to show how each version normalizes line ends.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            ReadOnlySpan<byte> xml10 = "<note>one\r\ntwo\rthree\u0085four</note>"u8;
            ReadOnlySpan<byte> xml11 = "<?xml version=\"1.1\"?><note>one\r\ntwo\rthree\u0085four</note>"u8;

            output.WriteLine($"XML 1.0: {ReadText(xml10)}");
            output.WriteLine($"XML 1.1: {ReadText(xml11)}");
        }

        /// <summary>
        /// Reads an attribute whose value holds a tab, a line end, and a character reference to a line feed.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void NormalizeAttributeValues(TextWriter output)
        {
            ReadOnlySpan<byte> xml = "<item note=\"a\tb\r\nc&#xA;d\"/>"u8;

            var reader = new Utf8XmlReader(xml);
            reader.Read();
            reader.MoveToNextAttribute();

            output.WriteLine(MakeVisible(reader.GetString()!));
        }

        /// <summary>
        /// Reads one indented document with each <see cref="XmlWhitespaceHandling"/> value.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void HandleWhitespace(TextWriter output)
        {
            XmlWhitespaceHandling[] choices =
            [
                XmlWhitespaceHandling.Insignificant,
                XmlWhitespaceHandling.Significant,
                XmlWhitespaceHandling.None,
            ];

            foreach (XmlWhitespaceHandling choice in choices)
            {
                var options = new XmlReaderOptions { WhitespaceHandling = choice };
                output.WriteLine($"{choice}: {ReadTokens("<list>\n  <item> a </item>\n</list>"u8, options)}");
            }
        }

        /// <summary>
        /// Reads the five predefined entities and two numeric character references.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ExpandReferences(TextWriter output)
        {
            ReadOnlySpan<byte> xml = "<text>&lt;p&gt; &amp; &quot;q&quot; &apos;a&apos; &#65;&#x42;</text>"u8;

            var reader = new Utf8XmlReader(xml);
            reader.Read();
            reader.Read();

            output.WriteLine(reader.GetString());
        }

        /// <summary>
        /// Reads documents that use a DTD, an entity the document would have to declare, and an encoding other than UTF-8.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void RejectUnsupported(TextWriter output)
        {
            var options = new XmlReaderOptions();

            output.WriteLine(ReadTokens("<!DOCTYPE note SYSTEM \"note.dtd\"><note/>"u8, options));
            output.WriteLine(ReadTokens("<note>&nbsp;</note>"u8, options));
            output.WriteLine(ReadTokens("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?><note/>"u8, options));
        }

        /// <summary>
        /// Reads two constructs that XML 1.1 allows and XML 1.0 does not.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void CompareVersions(TextWriter output)
        {
            var options = new XmlReaderOptions();

            output.WriteLine($"1.0 control: {ReadTokens("<a>&#x1;</a>"u8, options)}");
            output.WriteLine($"1.1 control: {ReadTokens("<?xml version=\"1.1\"?><a>&#x1;</a>"u8, options)}");
            output.WriteLine($"1.0 unbind:  {ReadTokens("<a xmlns:p=\"urn:p\"><b xmlns:p=\"\"/></a>"u8, options)}");
            output.WriteLine($"1.1 unbind:  {ReadTokens("<?xml version=\"1.1\"?><a xmlns:p=\"urn:p\"><b xmlns:p=\"\"/></a>"u8, options)}");
        }

        /// <summary>
        /// Writes values that need escaping and reads them back unchanged.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void EscapeOnWrite(TextWriter output)
        {
            const string attributeValue = "say \"hi\"\tthen\r\nleave";
            const string textValue = "1 < 2 && ]]> ends\r";

            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true }))
            {
                writer.WriteStartElement("note");
                writer.WriteAttributeString("title", attributeValue);
                writer.WriteStringValue(textValue);
                writer.WriteEndElement();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));

            var reader = new Utf8XmlReader(buffer.WrittenSpan);
            reader.Read();
            reader.MoveToNextAttribute();
            output.WriteLine(reader.GetString() == attributeValue);
            reader.MoveToElement();
            reader.Read();
            output.WriteLine(reader.GetString() == textValue);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Replaces control characters with their C# escapes, so that a printed value shows them.
        /// </summary>
        /// <param name="value">The value to print.</param>
        /// <returns>The value with <c>\n</c> in place of each line feed and <c>\uXXXX</c> in place of every other control character.</returns>
        private static string MakeVisible(string value)
        {
            var visible = new StringBuilder();
            foreach (char c in value)
            {
                if (c is '\n')
                {
                    visible.Append("\\n");
                }
                else if (char.IsControl(c))
                {
                    visible.Append($"\\u{(int)c:X4}");
                }
                else
                {
                    visible.Append(c);
                }
            }

            return visible.ToString();
        }

        /// <summary>
        /// Reads the first text token of a document.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 document.</param>
        /// <returns>The text, with its line ends made visible.</returns>
        private static string ReadText(ReadOnlySpan<byte> utf8Xml)
        {
            var reader = new Utf8XmlReader(utf8Xml);
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.Text)
                {
                    return MakeVisible(reader.GetString()!);
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Reads a whole document and lists its tokens, or returns the message of the exception that stopped the reader.
        /// </summary>
        /// <param name="utf8Xml">The UTF-8 document.</param>
        /// <param name="options">The reader options.</param>
        /// <returns>The token types separated by spaces, with the value of each text token in quotation marks, or the error.</returns>
        private static string ReadTokens(ReadOnlySpan<byte> utf8Xml, XmlReaderOptions options)
        {
            var tokens = new StringBuilder();
            try
            {
                var reader = new Utf8XmlReader(utf8Xml, options);
                while (reader.Read())
                {
                    tokens.Append(reader.TokenType);
                    if (reader.TokenType is XmlTokenType.Text)
                    {
                        tokens.Append($"(\"{MakeVisible(reader.GetString()!)}\")");
                    }

                    tokens.Append(' ');
                }
            }
            catch (XmlException ex)
            {
                return $"XmlException at line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}";
            }

            return tokens.ToString().TrimEnd();
        }

        #endregion

    }

}
