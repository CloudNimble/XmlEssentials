using System;
using System.Linq;
using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Exhaustive well-formed-input coverage for <see cref="Utf8XmlReader"/>.
    /// </summary>
    [TestClass]
    public class Utf8XmlReaderWellFormedTests
    {

        #region Public Methods

        [TestMethod]
        [DataRow("<e/>", DisplayName = "empty element")]
        [DataRow("<e></e>", DisplayName = "start and end")]
        [DataRow("<e>text</e>", DisplayName = "simple text")]
        [DataRow("<e>leading </e>", DisplayName = "trailing space in value")]
        [DataRow("<e> a </e>", DisplayName = "significant leaf whitespace")]
        [DataRow("<a><b/></a>", DisplayName = "nested empty child")]
        [DataRow("<a><b></b></a>", DisplayName = "nested start-end child")]
        [DataRow("<a><b>x</b><c>y</c></a>", DisplayName = "two children")]
        [DataRow("<a><b><c/></b></a>", DisplayName = "depth three")]
        [DataRow("<e a=\"1\"/>", DisplayName = "double-quoted attribute")]
        [DataRow("<e a='1'/>", DisplayName = "single-quoted attribute")]
        [DataRow("<e a=\"1\" b='2'/>", DisplayName = "mixed quotes")]
        [DataRow("<e a=\"&amp;&lt;&gt;&quot;&apos;\"/>", DisplayName = "five entities in attribute")]
        [DataRow("<e>&amp;&lt;&gt;&quot;&apos;</e>", DisplayName = "five entities in text")]
        [DataRow("<e>a&amp;b</e>", DisplayName = "mixed text and entity")]
        [DataRow("<e>&#65;&#x42;</e>", DisplayName = "decimal and hex character references")]
        [DataRow("<e>&#x4a;</e>", DisplayName = "hex mixed case")]
        [DataRow("<e a=\"x\ty\"/>", DisplayName = "tab in attribute")]
        [DataRow("<p:e xmlns:p=\"urn:x\"/>", DisplayName = "prefixed empty element")]
        [DataRow("<p:e xmlns:p=\"urn:x\"></p:e>", DisplayName = "prefixed start-end matching prefix")]
        [DataRow("<?xml version=\"1.0\"?><e/>", DisplayName = "declaration version 1.0")]
        [DataRow("<?xml version=\"1.1\"?><e/>", DisplayName = "declaration version 1.1")]
        [DataRow("<?xml version=\"1.0\" encoding=\"utf-8\"?><e/>", DisplayName = "utf-8 encoding")]
        [DataRow("<?xml version=\"1.0\" encoding=\"UTF-8\"?><e/>", DisplayName = "UTF-8 encoding")]
        [DataRow("<?xml version=\"1.0\" encoding='utf8'?><e/>", DisplayName = "utf8 alias single quotes")]
        [DataRow("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><e/>", DisplayName = "standalone yes")]
        [DataRow("<!--c--><e/>", DisplayName = "leading comment skipped")]
        [DataRow("<e/><!--c-->", DisplayName = "trailing comment skipped")]
        [DataRow("<?pi data?><e/>", DisplayName = "leading PI")]
        [DataRow("<e><?pi data?></e>", DisplayName = "PI inside element")]
        [DataRow("<e><![CDATA[x]]></e>", DisplayName = "CDATA")]
        [DataRow("<e><![CDATA[<>&]]></e>", DisplayName = "CDATA with markup")]
        [DataRow("<e>hello <b>world</b></e>", DisplayName = "mixed content")]
        [DataRow(" <e/>", DisplayName = "whitespace before root")]
        [DataRow("<e/> ", DisplayName = "whitespace after root")]
        [DataRow("<e />", DisplayName = "space before empty closer")]
        [DataRow("<e></e >", DisplayName = "space before end tag closer")]
        [DataRow("<e a = \"1\"/>", DisplayName = "spaces around attribute equals")]
        [DataRow("<e\na=\"1\"\n/>", DisplayName = "newlines in start tag")]
        [DataRow("<?xml version=\"1.1\"?><e>&#x1;</e>", DisplayName = "1.1 RestrictedChar as NCR")]
        [DataRow("<e>_.:-</e>", DisplayName = "name punctuation in text")]
        [DataRow("<a>\n<b/>\n</a>", DisplayName = "insignificant whitespace between elements")]
        [DataRow("<a/><b/>", DisplayName = "fragment two roots")]
        public void Read_WellFormedDocument_Succeeds(string xml)
        {
            var options = xml is "<a/><b/>"
                ? new XmlReaderOptions { AllowMultipleRoots = true }
                : new XmlReaderOptions();
            XmlReaderHarness.MustRead(xml, options);
        }

        [TestMethod]
        public void Read_WellFormed_UnprefixedPrefixSpanIsEmpty()
        {
            var element = new Utf8XmlReader("<e a=\"1\"/>"u8);
            element.Read();
            element.PrefixSpan.IsEmpty.Should().BeTrue();
            element.MoveToNextAttribute();
            element.PrefixSpan.IsEmpty.Should().BeTrue();
        }

        [TestMethod]
        public void Read_Declaration_MalformedEncoding_Throws()
        {
            XmlReaderHarness.MustThrow("<?xml version=\"1.0\" encoding=?><e/>");
            XmlReaderHarness.MustThrow("<?xml version=\"1.0\" encoding=\"\"?><e/>");
        }

        [TestMethod]
        public void Read_WellFormed_EmptyVsStartEnd()
        {
            var empty = new Utf8XmlReader("<e/>"u8);
            empty.Read().Should().BeTrue();
            empty.TokenType.Should().Be(XmlTokenType.StartElement);
            empty.IsEmptyElement.Should().BeTrue();
            empty.Read().Should().BeFalse();

            var pair = new Utf8XmlReader("<e></e>"u8);
            pair.Read().Should().BeTrue();
            pair.IsEmptyElement.Should().BeFalse();
            pair.Read().Should().BeTrue();
            pair.TokenType.Should().Be(XmlTokenType.EndElement);
            pair.Read().Should().BeFalse();
        }

        [TestMethod]
        public void Read_WellFormed_BomThenRoot()
        {
            var xml = new byte[] { 0xEF, 0xBB, 0xBF }.Concat("<root/>"u8.ToArray()).ToArray();
            XmlReaderHarness.MustRead(xml);
        }

        [TestMethod]
        public void Read_ByteOrderMarkBytesAfterTheStartOfTheDocument_AreTheCharacterTheyEncode()
        {
            // U+FEFF is a legal character in content. Only the first three bytes of a document are a byte order mark,
            // wherever a buffer happens to begin.
            const string mark = "\uFEFF";
            var xml = Encoding.UTF8.GetBytes("<e a=\"" + mark + "v\">" + mark + "text" + mark + "<!--" + mark + "--></e>");
            var options = new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow };
            var expected = XmlReaderHarness.Tokens(xml, options);
            expected.Should().Equal("StartElement :e{} open @:a{}=[" + mark + "v]", "Text :{} [" + mark + "text" + mark + "]", "Comment :{} [" + mark + "]", "EndElement :e{}");
            for (var split = 1; split < xml.Length; split++)
            {
                XmlReaderHarness.TokensSplitAt(xml, split, options).Should().Equal(expected, "the tokens must not depend on a split at byte {0}", split);
            }
        }

        [TestMethod]
        public void Read_WellFormed_BomThenDeclaration()
        {
            var xml = new byte[] { 0xEF, 0xBB, 0xBF }.Concat("<?xml version=\"1.0\"?><e/>"u8.ToArray()).ToArray();
            var reader = new Utf8XmlReader(xml);
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.XmlDeclaration);
            reader.Read().Should().BeTrue();
            reader.LocalNameEquals("e").Should().BeTrue();
        }

        [TestMethod]
        public void Read_WellFormed_CommentAllowReportsToken()
        {
            var reader = new Utf8XmlReader("<!--c--><e/>"u8, new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow });
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.Comment);
            reader.GetComment().Should().Be("c");
        }

        [TestMethod]
        public void Read_WellFormed_SignificantWhitespaceBetweenElements()
        {
            var reader = new Utf8XmlReader("<a>\n<b/>\n</a>"u8, new XmlReaderOptions { WhitespaceHandling = XmlWhitespaceHandling.Significant });
            reader.Read().Should().BeTrue();
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.Text);
        }

        [TestMethod]
        public void Read_WellFormed_Xml11NelInText()
        {
            var xml = "<?xml version=\"1.1\"?><e>a"u8.ToArray().Concat(new byte[] { 0xC2, 0x85 }).Concat("b</e>"u8.ToArray()).ToArray();
            XmlReaderHarness.MustRead(xml);
        }

        [TestMethod]
        public void Read_WellFormed_CrlfInText()
        {
            XmlReaderHarness.MustRead("<e>a\r\nb</e>");
        }

        [TestMethod]
        public void Read_WellFormed_NonAsciiName()
        {
            var xml = "<"u8.ToArray().Concat("é"u8.ToArray()).Concat("/>"u8.ToArray()).ToArray();
            XmlReaderHarness.MustRead(xml);
        }

        [TestMethod]
        public void Read_EmptyInput_WhenMultipleRootsAreAllowed_HasNoTokens()
        {
            XmlReaderHarness.Tokens(""u8, new XmlReaderOptions { AllowMultipleRoots = true }).Should().BeEmpty();
        }

        [TestMethod]
        public void Read_WellFormed_ZeroLimitsUseDefaults()
        {
            XmlReaderHarness.MustRead(
                "<a b=\"1\"><!--c--><b>text</b></a>",
                new XmlReaderOptions
                {
                    MaxDepth = 0,
                    MaxAttributeCount = 0,
                    MaxNameByteCount = 0,
                    MaxCommentByteCount = 0,
                    MaxTextByteCount = 0,
                    MaxDocumentByteCount = 0,
                });
        }

        #endregion

    }

}
