using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Exhaustive malformed-input coverage for <see cref="Utf8XmlReader"/> well-formedness and reject rules.
    /// </summary>
    [TestClass]
    public class Utf8XmlReaderMalformedTests
    {

        #region Public Methods

        [TestMethod]
        [DataRow("", DisplayName = "empty document")]
        [DataRow("   ", DisplayName = "whitespace only")]
        [DataRow("\r\n\t", DisplayName = "EOL only")]
        [DataRow("nope", DisplayName = "bare text")]
        [DataRow("nope<root/>", DisplayName = "text before root")]
        [DataRow("<root/>trailing", DisplayName = "text after root")]
        [DataRow("<a/><b/>", DisplayName = "two roots")]
        [DataRow("<a></a><b/>", DisplayName = "two roots start-end then empty")]
        [DataRow("</root>", DisplayName = "end tag with no start")]
        [DataRow("<a></b>", DisplayName = "mismatched end tag")]
        [DataRow("<a><b></a>", DisplayName = "crossed tags")]
        [DataRow("<a><b></b>", DisplayName = "unclosed parent")]
        [DataRow("<a>", DisplayName = "unclosed root")]
        [DataRow("<a><b>", DisplayName = "unclosed nested")]
        [DataRow("<", DisplayName = "lone less-than")]
        [DataRow("<e", DisplayName = "unterminated start tag")]
        [DataRow("<e ", DisplayName = "unterminated start tag with space")]
        [DataRow("<e/", DisplayName = "unterminated empty tag")]
        [DataRow("<e/ >", DisplayName = "space inside empty closer")]
        [DataRow("<>", DisplayName = "empty name")]
        [DataRow("</>", DisplayName = "empty end name")]
        [DataRow("<1e/>", DisplayName = "name starts with digit")]
        [DataRow("<-e/>", DisplayName = "name starts with hyphen")]
        [DataRow("<.e/>", DisplayName = "name starts with dot")]
        [DataRow("<e$/>", DisplayName = "name contains dollar")]
        [DataRow("<e><", DisplayName = "cut start of child")]
        [DataRow("<a:b:c/>", DisplayName = "multiple colons")]
        [DataRow("<p:1/>", DisplayName = "prefix then non-NameStart")]
        [DataRow("<e></e extra>", DisplayName = "junk in end tag")]
        [DataRow("<e></e", DisplayName = "unterminated end tag")]
        [DataRow("<e a/>", DisplayName = "attribute without value")]
        [DataRow("<e a=1/>", DisplayName = "unquoted attribute")]
        [DataRow("<e a=>", DisplayName = "attribute missing quotes and value")]
        [DataRow("<e a=\"1/>", DisplayName = "unclosed double quote")]
        [DataRow("<e a='1/>", DisplayName = "unclosed single quote")]
        [DataRow("<e a=\"<\"/>", DisplayName = "less-than in attribute")]
        [DataRow("<e 1=\"1\"/>", DisplayName = "attribute name starts with digit")]
        [DataRow("<e a=\"1\" a=\"2\"/>", DisplayName = "duplicate attribute")]
        [DataRow("<e a='1' a='2'/>", DisplayName = "duplicate attribute single quotes")]
        [DataRow("<e =\"1\"/>", DisplayName = "attribute missing name")]
        [DataRow("<e a \"1\"/>", DisplayName = "attribute missing equals")]
        [DataRow("<e>&foo;</e>", DisplayName = "undefined entity")]
        [DataRow("<e>&lt</e>", DisplayName = "unterminated entity")]
        [DataRow("<e>&;</e>", DisplayName = "empty entity")]
        [DataRow("<e>&#;</e>", DisplayName = "empty decimal reference")]
        [DataRow("<e>&#x;</e>", DisplayName = "empty hex reference")]
        [DataRow("<e>&#xG;</e>", DisplayName = "invalid hex reference")]
        [DataRow("<e>&#x110000;</e>", DisplayName = "reference past Unicode")]
        [DataRow("<e>&#0;</e>", DisplayName = "NUL character reference")]
        [DataRow("<e>&#x0;</e>", DisplayName = "NUL hex reference")]
        [DataRow("<e>&#x1;</e>", DisplayName = "C0 character reference in 1.0")]
        [DataRow("<e>&#8;</e>", DisplayName = "C0 decimal reference in 1.0")]
        [DataRow("<e>&#xD800;</e>", DisplayName = "surrogate character reference")]
        [DataRow("<!DOCTYPE e><e/>", DisplayName = "DOCTYPE")]
        [DataRow("<!ENTITY e \"x\"><e/>", DisplayName = "ENTITY")]
        [DataRow("<!ELEMENT e EMPTY><e/>", DisplayName = "ELEMENT")]
        [DataRow("<!ATTLIST e a CDATA #IMPLIED><e/>", DisplayName = "ATTLIST")]
        [DataRow("<!NOTATION n SYSTEM \"x\"><e/>", DisplayName = "NOTATION")]
        [DataRow("<!FOO><e/>", DisplayName = "unknown declaration")]
        [DataRow("<!-- a -- b --><e/>", DisplayName = "comment contains double hyphen")]
        [DataRow("<!--c", DisplayName = "unterminated comment")]
        [DataRow("<!--c--", DisplayName = "unterminated comment closer")]
        [DataRow("<e><![CDATA[x</e>", DisplayName = "unterminated CDATA")]
        [DataRow("<e><![CDATA[x", DisplayName = "cut CDATA")]
        [DataRow("<?pi data", DisplayName = "unterminated PI")]
        [DataRow("<? >", DisplayName = "PI missing target")]
        [DataRow("<?xml version=\"1.0\"?><?xml version=\"1.0\"?><e/>", DisplayName = "second XML declaration")]
        [DataRow("<!--c--><?xml version=\"1.0\"?><e/>", DisplayName = "declaration after comment")]
        [DataRow("<?pi?><?xml version=\"1.0\"?><e/>", DisplayName = "declaration after PI")]
        [DataRow("<e/><?xml version=\"1.0\"?>", DisplayName = "declaration after root")]
        [DataRow("<?xml version=\"1.0\" encoding=\"utf-16\"?><e/>", DisplayName = "utf-16 encoding")]
        [DataRow("<?xml version=\"1.0\" encoding=\"iso-8859-1\"?><e/>", DisplayName = "latin-1 encoding")]
        [DataRow("<?xml version=\"1.0\" encoding=\"ascii\"?><e/>", DisplayName = "ascii encoding")]
        public void Read_MalformedDocument_Throws(string xml)
        {
            XmlReaderHarness.MustThrow(xml);
        }

        [TestMethod]
        public void Read_Malformed_DeclarationAfterWhitespace_Throws()
        {
            XmlReaderHarness.MustThrow(" <?xml version=\"1.0\"?><e/>");
        }

        [TestMethod]
        public void Read_Malformed_XmlPiInBody_Throws()
        {
            XmlReaderHarness.MustThrow("<e><?xml version=\"1.0\"?></e>");
        }

        [TestMethod]
        public void Read_Malformed_NulLiteral_Throws()
        {
            XmlReaderHarness.MustThrow([(byte)'<', (byte)'e', (byte)'>', 0x00, (byte)'<', (byte)'/', (byte)'e', (byte)'>']);
        }

        [TestMethod]
        public void Read_Malformed_InvalidUtf8InName_Throws()
        {
            XmlReaderHarness.MustThrow([(byte)'<', (byte)'e', 0xFF, (byte)'/', (byte)'>']);
        }

        [TestMethod]
        public void Read_Malformed_InvalidUtf8InText_Throws()
        {
            XmlReaderHarness.MustThrow([(byte)'<', (byte)'e', (byte)'>', 0xC0, 0x00, (byte)'<', (byte)'/', (byte)'e', (byte)'>']);
        }

        [TestMethod]
        public void Read_Malformed_TruncatedUtf8_Throws()
        {
            XmlReaderHarness.MustThrow([(byte)'<', (byte)'e', (byte)'>', 0xC2, (byte)'<', (byte)'/', (byte)'e', (byte)'>']);
        }

        [TestMethod]
        public void Read_Malformed_Xml11RestrictedCharLiteral_Throws()
        {
            var xml = "<?xml version=\"1.1\"?><e>"u8.ToArray().Concat(new byte[] { 0x01 }).Concat("</e>"u8.ToArray()).ToArray();
            XmlReaderHarness.MustThrow(xml);
        }

        [TestMethod]
        public void Read_Malformed_MaxDepth_Throws()
        {
            XmlReaderHarness.MustThrow("<a><b/></a>", new XmlReaderOptions { MaxDepth = 1 });
        }

        [TestMethod]
        public void Read_Malformed_MaxNameByteCount_Throws()
        {
            XmlReaderHarness.MustThrow("<abcdef/>", new XmlReaderOptions { MaxNameByteCount = 2 });
        }

        [TestMethod]
        public void Read_Malformed_MaxAttributeCount_Throws()
        {
            XmlReaderHarness.MustThrow("<e a=\"1\" b=\"2\"/>", new XmlReaderOptions { MaxAttributeCount = 1 });
        }

        [TestMethod]
        public void Read_Malformed_MaxCommentByteCount_Throws()
        {
            XmlReaderHarness.MustThrow("<!--abcd--><e/>", new XmlReaderOptions { MaxCommentByteCount = 2, CommentHandling = XmlCommentHandling.Allow });
        }

        [TestMethod]
        public void Read_Malformed_MaxTextByteCount_Throws()
        {
            XmlReaderHarness.MustThrow("<e>abcd</e>", new XmlReaderOptions { MaxTextByteCount = 2 });
        }

        [TestMethod]
        public void Read_Malformed_MaxCDataByteCount_Throws()
        {
            XmlReaderHarness.MustThrow("<e><![CDATA[abcd]]></e>", new XmlReaderOptions { MaxTextByteCount = 2 });
        }

        [TestMethod]
        public void Read_Malformed_WhitespaceNone_Throws()
        {
            XmlReaderHarness.MustThrow("<a>\n<b/>\n</a>", new XmlReaderOptions { WhitespaceHandling = XmlWhitespaceHandling.None });
        }

        [TestMethod]
        public void Read_Malformed_AmpersandAtEndOfText_Throws()
        {
            XmlReaderHarness.MustThrow("<e>&</e>");
        }

        [TestMethod]
        public void Read_Malformed_HashWithoutNumber_Throws()
        {
            XmlReaderHarness.MustThrow("<e>&#xZZ;</e>");
        }

        [TestMethod]
        public void Read_Malformed_EndTagLocalNameMismatch_Throws()
        {
            XmlReaderHarness.MustThrow("<a:b></c:d>");
        }

        [TestMethod]
        public void Read_Malformed_IncompleteFinalBlock_Throws()
        {
            var act = () => XmlReaderHarness.Drain("<e");
            act.Should().Throw<XmlException>();
        }

        [TestMethod]
        public void Read_Malformed_CommentDisallow_Throws()
        {
            XmlReaderHarness.MustThrow("<!--c--><e/>", new XmlReaderOptions { CommentHandling = XmlCommentHandling.Disallow });
        }

        [TestMethod]
        [DataRow("<e>&")]
        [DataRow("<a></")]
        [DataRow("<a></a")]
        [DataRow("<e><?")]
        [DataRow("<e a")]
        [DataRow("<e a ")]
        [DataRow("<e a=")]
        [DataRow("<e a='")]
        [DataRow("<e a='&")]
        [DataRow("<e/")]
        [DataRow("<p:")]
        [DataRow("<e p:")]
        [DataRow("<!")]
        [DataRow("<!--")]
        [DataRow("<e><![CDATA[")]
        public void Read_IncompleteNonFinal_ReturnsFalse(string xml)
        {
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml), false, default);
            while (reader.Read())
            {
            }
        }

        [TestMethod]
        public void Read_Malformed_Xml11NonChar_Throws()
        {
            var xml = "<?xml version=\"1.1\"?><e>"u8.ToArray().Concat(new byte[] { 0xEF, 0xBF, 0xBE }).Concat("</e>"u8.ToArray()).ToArray();
            XmlReaderHarness.MustThrow(xml);
        }

        [TestMethod]
        public void Read_Malformed_DurationOverflow_IsFalse()
        {
            OnText("<e>P999999999999999999D</e>").TryGetTimeSpan(out _).Should().BeFalse();
            OnText("<e>-</e>").TryGetTimeSpan(out _).Should().BeFalse();
            OnText("<e>-X</e>").TryGetTimeSpan(out _).Should().BeFalse();
            OnText("<e>PT.</e>").TryGetTimeSpan(out _).Should().BeFalse();
            OnText("<e>P1</e>").TryGetTimeSpan(out _).Should().BeFalse();
            OnText("<e>PT</e>").TryGetTimeSpan(out _).Should().BeFalse();
        }

        [TestMethod]
        public void Read_Malformed_AttributeCrlfIsNormalized()
        {
            var reader = new Utf8XmlReader("<e a=\"x\r\ny\"/>"u8);
            reader.Read();
            reader.MoveToNextAttribute();
            reader.GetString().Should().Be("x y");
        }

        #endregion

        #region Private Methods

        private static Utf8XmlReader OnText(string xml)
        {
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml));
            reader.Read();
            reader.Read();
            return reader;
        }

        #endregion

    }

}
