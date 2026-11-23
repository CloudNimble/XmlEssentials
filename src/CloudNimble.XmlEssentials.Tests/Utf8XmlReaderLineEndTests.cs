using System;
using System.Collections.Generic;
using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// End-of-line handling (XML 1.0 section 2.11 and XML 1.1 section 2.11) and the values it leaves behind,
    /// in text, attribute values, CDATA sections, comments, and processing instructions.
    /// </summary>
    [TestClass]
    public class Utf8XmlReaderLineEndTests
    {

        #region Public Methods

        [TestMethod]
        [DataRow("a\rb", "a\nb", DisplayName = "CR")]
        [DataRow("a\r\nb", "a\nb", DisplayName = "CR LF")]
        [DataRow("a\r\rb", "a\n\nb", DisplayName = "two CR")]
        [DataRow("a\r\n\r\nb", "a\n\nb", DisplayName = "two CR LF")]
        [DataRow("a\r", "a\n", DisplayName = "CR last")]
        [DataRow("\rb", "\nb", DisplayName = "CR first")]
        [DataRow("a\r\u0085b", "a\n\u0085b", DisplayName = "CR NEL: NEL is an ordinary character")]
        [DataRow("a\u0085b\u2028c", "a\u0085b\u2028c", DisplayName = "NEL and LS are ordinary characters")]
        [DataRow("a&amp;\rb&#13;c\r\n", "a&\nb\rc\n", DisplayName = "a CR written as a reference is kept")]
        [DataRow("1 &lt; 2\r\n&amp;&amp; 3 &gt; 2\r", "1 < 2\n&& 3 > 2\n", DisplayName = "references and line ends in turn")]
        public void Text_Version10_LineEndsBecomeOneLineFeed(string content, string expected)
        {
            ReadText("<r>" + content + "</r>").Should().Be(expected);
        }

        [TestMethod]
        [DataRow("a\rb", "a\nb", DisplayName = "CR")]
        [DataRow("a\r\nb", "a\nb", DisplayName = "CR LF")]
        [DataRow("a\u0085b", "a\nb", DisplayName = "NEL")]
        [DataRow("a\u2028b", "a\nb", DisplayName = "LS")]
        [DataRow("a\r\u0085b", "a\nb", DisplayName = "CR NEL is one line end")]
        [DataRow("a\r\u2028b", "a\n\nb", DisplayName = "CR LS is two line ends")]
        [DataRow("a\u0085\rb", "a\n\nb", DisplayName = "NEL CR is two line ends")]
        [DataRow("a\r\u0085\r\u0085b", "a\n\nb", DisplayName = "two CR NEL")]
        [DataRow("a\r©b", "a\n©b", DisplayName = "CR before another character whose first byte is NEL's")]
        [DataRow("a©b…c", "a©b…c", DisplayName = "characters that share a first byte with NEL and LS")]
        [DataRow("a&amp;\u0085b\u2028c&lt;", "a&\nb\nc<", DisplayName = "references before and after NEL and LS")]
        [DataRow("a&#x85;b&#x2028;c\u0085", "a\u0085b\u2028c\n", DisplayName = "NEL and LS written as references are kept")]
        public void Text_Version11_LineEndsBecomeOneLineFeed(string content, string expected)
        {
            ReadText("<?xml version=\"1.1\"?><r>" + content + "</r>").Should().Be(expected);
        }

        [TestMethod]
        [DataRow("", "a\rb", "a b", DisplayName = "CR")]
        [DataRow("", "a\r\nb", "a b", DisplayName = "CR LF")]
        [DataRow("", "a\n\tb", "a  b", DisplayName = "LF and TAB")]
        [DataRow("", "a\r\u0085b", "a \u0085b", DisplayName = "CR NEL in XML 1.0: NEL is an ordinary character")]
        [DataRow("<?xml version=\"1.1\"?>", "a\u0085b\u2028c", "a b c", DisplayName = "XML 1.1 NEL and LS")]
        [DataRow("<?xml version=\"1.1\"?>", "a\r\u0085b", "a b", DisplayName = "XML 1.1 CR NEL is one line end")]
        [DataRow("<?xml version=\"1.1\"?>", "a\r\u0085\r\nb&amp;", "a  b&", DisplayName = "XML 1.1 CR NEL, CR LF, and a reference")]
        [DataRow("<?xml version=\"1.1\"?>", "a\r©b", "a ©b", DisplayName = "XML 1.1 CR before another character whose first byte is NEL's")]
        public void AttributeValue_LineEndsBecomeOneSpace(string declaration, string value, string expected)
        {
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(declaration + "<r a=\"" + value + "\"/>"));
            while (reader.TokenType is not XmlTokenType.StartElement)
            {
                reader.Read().Should().BeTrue();
            }

            reader.MoveToNextAttribute().Should().BeTrue();
            reader.GetString().Should().Be(expected);
        }

        [TestMethod]
        [DataRow("", "a\rb\r\nc\r", "a\nb\nc\n", DisplayName = "CR and CR LF")]
        [DataRow("", "a\r\u0085b\u2028c", "a\n\u0085b\u2028c", DisplayName = "XML 1.0: NEL and LS are ordinary characters")]
        [DataRow("<?xml version=\"1.1\"?>", "a\r\u0085b", "a\nb", DisplayName = "XML 1.1 CR NEL is one line end")]
        [DataRow("<?xml version=\"1.1\"?>", "a\u0085b\u2028c\rd\r\ne", "a\nb\nc\nd\ne", DisplayName = "XML 1.1 every line end")]
        [DataRow("<?xml version=\"1.1\"?>", "©a…\r©", "©a…\n©", DisplayName = "XML 1.1 characters that share a first byte with NEL and LS")]
        [DataRow("<?xml version=\"1.1\"?>", "a &amp; b\u0085", "a &amp; b\n", DisplayName = "XML 1.1 an ampersand is an ordinary character")]
        public void CDataCommentAndProcessingInstruction_LineEndsBecomeOneLineFeed(string declaration, string content, string expected)
        {
            var options = new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow };
            var xml = Encoding.UTF8.GetBytes(declaration + "<r><![CDATA[" + content + "]]><!--" + content + "--><?p " + content + "?></r>");
            var reader = new Utf8XmlReader(xml, options);
            var values = new List<string>();
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.CData or XmlTokenType.Comment or XmlTokenType.ProcessingInstruction)
                {
                    values.Add(reader.TokenType + ":" + reader.GetString());
                }
            }

            values.Should().Equal("CData:" + expected, "Comment:" + expected, "ProcessingInstruction:" + expected);
        }

        [TestMethod]
        [DataRow("", "\u0085", true, DisplayName = "XML 1.0: NEL is a character, so the text is kept")]
        [DataRow("<?xml version=\"1.1\"?>", "\u0085", false, DisplayName = "XML 1.1: NEL is a line end, so the text is white space")]
        [DataRow("<?xml version=\"1.1\"?>", " \u2028\r\u0085 ", false, DisplayName = "XML 1.1: LS and CR NEL among blanks")]
        [DataRow("<?xml version=\"1.1\"?>", "\r\n", false, DisplayName = "CR LF")]
        public void Text_OnlyLineEnds_IsWhiteSpaceWhenTheVersionReadsThemAsLineEnds(string declaration, string content, bool reported)
        {
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(declaration + "<r>" + content + "<e/></r>"), new XmlReaderOptions { WhitespaceHandling = XmlWhitespaceHandling.Insignificant });
            var tokens = new List<XmlTokenType>();
            while (reader.Read())
            {
                if (reader.TokenType is not XmlTokenType.XmlDeclaration)
                {
                    tokens.Add(reader.TokenType);
                }
            }

            if (reported)
            {
                tokens.Should().Equal(XmlTokenType.StartElement, XmlTokenType.Text, XmlTokenType.StartElement, XmlTokenType.EndElement);
            }
            else
            {
                tokens.Should().Equal(XmlTokenType.StartElement, XmlTokenType.StartElement, XmlTokenType.EndElement);
            }
        }

        [TestMethod]
        public void Text_Version11_SignificantWhiteSpaceOfLineEnds_IsReportedAsLineFeeds()
        {
            var options = new XmlReaderOptions { WhitespaceHandling = XmlWhitespaceHandling.Significant };
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes("<?xml version=\"1.1\"?><r>\u0085\r\u0085\u2028<e/></r>"), options);
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.XmlDeclaration);
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.StartElement);
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.Text);
            reader.GetString().Should().Be("\n\n\n");
        }

        [TestMethod]
        [DataRow("", "a\r\u0085b", 2L, DisplayName = "XML 1.0: CR ends a line and NEL does not")]
        [DataRow("<?xml version=\"1.1\"?>", "a\r\u0085b", 2L, DisplayName = "XML 1.1: CR NEL ends one line")]
        [DataRow("<?xml version=\"1.1\"?>", "a\r\u2028b", 3L, DisplayName = "XML 1.1: CR LS ends two lines")]
        [DataRow("", "a\r\n\rb&amp;", 3L, DisplayName = "CR LF and CR after decoding has started")]
        public void LineNumber_CountsEachLineEndOnce(string declaration, string content, long expectedLine)
        {
            var xml = Encoding.UTF8.GetBytes(declaration + "<r>" + content + "<1/></r>");
            long? line = null;
            try
            {
                var reader = new Utf8XmlReader(xml);
                while (reader.Read())
                {
                }
            }
            catch (XmlException ex)
            {
                line = ex.LineNumber;
            }

            line.Should().Be(expectedLine);
        }

        [TestMethod]
        public void Text_WithReferencesOverTheLimit_ThrowsBeforeItIsBuffered()
        {
            var xml = Encoding.UTF8.GetBytes("<r>&amp;" + new string('x', 200_000) + "</r>");
            var options = new XmlReaderOptions { MaxTextByteCount = 64 };
            Measure(xml, options).Should().Be("MaxTextByteCount exceeded (64).");
            var before = GC.GetAllocatedBytesForCurrentThread();
            var message = Measure(xml, options);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            message.Should().Be("MaxTextByteCount exceeded (64).");
            allocated.Should().BeLessThan(4096, "a text node over the limit must be refused, not copied into a buffer of its size");
        }

        [TestMethod]
        public void Text_Limit_CountsTheBytesInTheDocumentNotTheDecodedValue()
        {
            var options = new XmlReaderOptions { MaxTextByteCount = 8 };
            Measure("<r>&amp;&amp;&amp;</r>"u8.ToArray(), options).Should().Be("MaxTextByteCount exceeded (8).");
            var reader = new Utf8XmlReader("<r>&amp;&lt;&gt;</r>"u8.ToArray(), new XmlReaderOptions { MaxTextByteCount = 13 });
            reader.Read().Should().BeTrue();
            reader.Read().Should().BeTrue();
            reader.GetString().Should().Be("&<>");
        }

        [TestMethod]
        [DataRow("<r>a\r\nb&amp;c\rd</r>", "a\nb&c\nd", DisplayName = "XML 1.0")]
        [DataRow("<?xml version=\"1.1\"?><r>a\r\u0085b&amp;c\u2028d\r</r>", "a\nb&c\nd\n", DisplayName = "XML 1.1")]
        public void Text_SplitAtEveryByte_DecodesTheSame(string xml, string expected)
        {
            var bytes = Encoding.UTF8.GetBytes(xml);
            for (var split = 1; split < bytes.Length; split++)
            {
                var first = new Utf8XmlReader(bytes.AsSpan(0, split), isFinalBlock: false, new XmlReaderState(new XmlReaderOptions()));
                string? text = null;
                while (first.Read())
                {
                    text = first.TokenType is XmlTokenType.Text ? first.GetString() : text;
                }

                var second = new Utf8XmlReader(bytes.AsSpan((int)first.BytesConsumed), isFinalBlock: true, first.CurrentState);
                while (second.Read())
                {
                    text = second.TokenType is XmlTokenType.Text ? second.GetString() : text;
                }

                text.Should().Be(expected, "the text must not depend on a split at byte {0}", split);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Drains <paramref name="xml"/> and returns the message of the <see cref="XmlException"/> it throws.
        /// </summary>
        /// <param name="xml">The document.</param>
        /// <param name="options">Reader options.</param>
        /// <returns>The message, or null when the document was read to its end.</returns>
        internal static string? Measure(byte[] xml, XmlReaderOptions options)
        {
            try
            {
                var reader = new Utf8XmlReader(xml, options);
                while (reader.Read())
                {
                }
            }
            catch (XmlException ex)
            {
                return ex.Message;
            }

            return null;
        }

        /// <summary>
        /// Reads the first text node of <paramref name="xml"/>.
        /// </summary>
        /// <param name="xml">The document.</param>
        /// <returns>The text as the reader reports it.</returns>
        internal static string ReadText(string xml)
        {
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml));
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.Text)
                {
                    return reader.GetString() ?? "";
                }
            }

            return "";
        }

        #endregion

    }

}
