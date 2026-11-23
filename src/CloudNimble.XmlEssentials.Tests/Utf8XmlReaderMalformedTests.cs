using System;
using System.Linq;
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
        [DataRow("<p:e/>", DisplayName = "undeclared element prefix")]
        [DataRow("<e p:a=\"1\"/>", DisplayName = "undeclared attribute prefix")]
        [DataRow("<e xmlns:p=\"\"/>", DisplayName = "prefix undeclaration in 1.0")]
        [DataRow("<e xmlns:xmlns=\"urn:x\"/>", DisplayName = "xmlns prefix declared")]
        [DataRow("<e xmlns:xml=\"urn:x\"/>", DisplayName = "xml prefix rebound")]
        [DataRow("<a::b/>", DisplayName = "double colon in name")]
        [DataRow("<item></items>", DisplayName = "end tag shares a prefix of the start name")]
        [DataRow("<p:e xmlns:p=\"urn:x\"></p:f>", DisplayName = "end tag local mismatch with prefix")]
        [DataRow("<p:e xmlns:p=\"urn:x\" xmlns:q=\"urn:y\"></q:e>", DisplayName = "end tag URI mismatch")]
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
        public void Read_CommentInsideAnElement_WhenCommentsAreDisallowed_Throws()
        {
            var reader = new Utf8XmlReader("<a><!--c--></a>"u8, new XmlReaderOptions { CommentHandling = XmlCommentHandling.Disallow });
            reader.Read().Should().BeTrue();
            string? message = null;
            try
            {
                reader.Read();
            }
            catch (XmlException ex)
            {
                message = ex.Message;
            }

            message.Should().Be("Comments are not allowed.");
        }

        [TestMethod]
        public void Read_IncompleteEndTagAtDepthZero_NeedsMoreThenThrows()
        {
            var partial = new Utf8XmlReader("</e"u8, isFinalBlock: false, state: default);
            partial.Read().Should().BeFalse();

            var final = new Utf8XmlReader("</e"u8);
            string? message = null;
            try
            {
                while (final.Read())
                {
                }
            }
            catch (XmlException ex)
            {
                message = ex.Message;
            }

            message.Should().Be("Unexpected end of XML.");
        }

        [TestMethod]
        [DataRow("<e>&", 1, 3)]
        [DataRow("<a></", 1, 3)]
        [DataRow("<a></a", 1, 3)]
        [DataRow("<e><?", 1, 3)]
        [DataRow("<e a", 0, 0)]
        [DataRow("<e a ", 0, 0)]
        [DataRow("<e a=", 0, 0)]
        [DataRow("<e a='", 0, 0)]
        [DataRow("<e a='&", 0, 0)]
        [DataRow("<e/", 0, 0)]
        [DataRow("<p:", 0, 0)]
        [DataRow("<e p:", 0, 0)]
        [DataRow("<!", 0, 0)]
        [DataRow("<!--", 0, 0)]
        [DataRow("<e><![CDATA[", 1, 3)]
        public void Read_IncompleteNonFinal_ReturnsFalseAtTheStartOfTheIncompleteToken(string xml, int tokensBefore, int bytesConsumed)
        {
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml), false, default);
            var tokens = 0;
            while (reader.Read())
            {
                tokens++;
            }

            tokens.Should().Be(tokensBefore);
            reader.BytesConsumed.Should().Be(bytesConsumed);
        }

        [TestMethod]
        [DataRow("<p:", DisplayName = "after the colon of a prefixed start tag")]
        [DataRow("<r><p:", DisplayName = "after the colon of a prefixed child")]
        [DataRow("<r x:", DisplayName = "after the colon of a prefixed attribute")]
        [DataRow("<r></p:", DisplayName = "after the colon of a prefixed end tag")]
        [DataRow("<r><?p:", DisplayName = "after a colon in a processing instruction target")]
        [DataRow("<\u00E9:", DisplayName = "after the colon of a prefix that is not ASCII")]
        [DataRow("<r></\u00E9:", DisplayName = "after the colon of a prefix that is not ASCII, in an end tag")]
        public void Read_BufferEndingInsideTheFirstCharacterOfALocalName_WaitsForTheRestOrRejectsTheBrokenCharacter(string before)
        {
            // The first byte of a two-byte character: the name cannot be judged until the second arrives.
            byte[] xml = [.. Encoding.UTF8.GetBytes(before), 0xC3];
            var waiting = new Utf8XmlReader(xml, isFinalBlock: false, default);
            var tokens = 0;
            while (waiting.Read())
            {
                tokens++;
            }

            tokens.Should().Be(before.StartsWith("<r>", StringComparison.Ordinal) ? 1 : 0);
            string? message = null;
            try
            {
                var final = new Utf8XmlReader(xml);
                while (final.Read())
                {
                }
            }
            catch (XmlException ex)
            {
                message = ex.Message;
            }

            message.Should().Be("Invalid UTF-8.");
        }

        [TestMethod]
        [DataRow("<p:\u00D7 xmlns:p=\"urn:p\"/>", DisplayName = "a multiplication sign, which is not a name character")]
        [DataRow("<p:\u00B7x xmlns:p=\"urn:p\"/>", DisplayName = "a middle dot, which may continue a name but not start one")]
        [DataRow("<r xmlns:p=\"urn:p\" p:\u00B7=\"1\"/>", DisplayName = "a middle dot starting an attribute's local name")]
        public void Read_LocalNameThatStartsWithACharacterNoNameCanStartWith_Throws(string xml)
        {
            string? message = null;
            try
            {
                XmlReaderHarness.Drain(xml);
            }
            catch (XmlException ex)
            {
                message = ex.Message;
            }

            message.Should().Be("Invalid name.");
        }

        [TestMethod]
        [DataRow("<e>&#x4A;&#x4a;&#74;</e>", "JJJ", DisplayName = "hexadecimal digits in either case, and decimal")]
        [DataRow("<e>&#xABCD;&#xabcd;&#xAbCd;</e>", "ꯍꯍꯍ", DisplayName = "every hexadecimal letter case")]
        [DataRow("<e>&#x10FFFF;&#x9;</e>", "\U0010FFFF\t", DisplayName = "the largest character and a tab")]
        public void Read_CharacterReferences_AreDecoded(string xml, string expected)
        {
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml));
            reader.Read().Should().BeTrue();
            reader.Read().Should().BeTrue();
            reader.GetString().Should().Be(expected);
        }

        [TestMethod]
        [DataRow("<e>&#4A;</e>", DisplayName = "a hexadecimal letter in a decimal reference")]
        [DataRow("<e>&#xG;</e>", DisplayName = "a letter that is not a hexadecimal digit")]
        [DataRow("<e>&#xg;</e>", DisplayName = "a lowercase letter that is not a hexadecimal digit")]
        [DataRow("<e>&#x/;</e>", DisplayName = "a character before the digits")]
        [DataRow("<e>&#x:;</e>", DisplayName = "a character between the digits and the letters")]
        [DataRow("<e>&#x@;</e>", DisplayName = "the character before A")]
        [DataRow("<e>&#x`;</e>", DisplayName = "the character before a")]
        [DataRow("<e>&#x;</e>", DisplayName = "no digits")]
        [DataRow("<e>&#;</e>", DisplayName = "no digits, decimal")]
        [DataRow("<e>&#x110000;</e>", DisplayName = "past the last character")]
        [DataRow("<e>&#x0;</e>", DisplayName = "NUL")]
        public void Read_CharacterReferenceThatIsNotANumberOfACharacter_Throws(string xml)
        {
            XmlReaderHarness.MustThrow(xml);
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
