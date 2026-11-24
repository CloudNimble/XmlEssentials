using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// PR 2a: UTF-8, BOM, declaration, empty vs start+end, text, five entities, DTD reject, DoS 0-semantics.
    /// </summary>
    [TestClass]
    public class Utf8XmlReaderTests
    {

        #region Fields

        private static readonly XmlReaderOptions ValueOptions = new() { CommentHandling = XmlCommentHandling.Allow };

        #endregion

        #region Public Methods

        [TestMethod]
        public void Read_AcceptsUtf8Bom()
        {
            var xml = new byte[] { 0xEF, 0xBB, 0xBF }.Concat("<root/>"u8.ToArray()).ToArray();
            var reader = new Utf8XmlReader(xml);
            reader.Read().Should().BeTrue();
            reader.LocalNameEquals("root").Should().BeTrue();
            reader.IsEmptyElement.Should().BeTrue();
        }

        [TestMethod]
        public void Read_AllowMultipleRoots()
        {
            var reader = new Utf8XmlReader("<a/><b/>"u8, new XmlReaderOptions { AllowMultipleRoots = true });
            reader.Read().Should().BeTrue();
            reader.Read().Should().BeTrue();
            reader.LocalNameEquals("b").Should().BeTrue();
        }

        [TestMethod]
        public void Read_Attributes_EntitiesAndQuotes()
        {
            var reader = new Utf8XmlReader("<e a=\"&amp;\" b='2'/>"u8);
            reader.Read();
            reader.HasAttributes.Should().BeTrue();
            reader.MoveToNextAttribute();
            reader.ValueIsEscaped.Should().BeTrue();
            reader.GetString().Should().Be("&");
            reader.ValueTextEquals("&"u8).Should().BeTrue();
            reader.ValueTextEquals("&").Should().BeTrue();
        }

        [TestMethod]
        public void Read_Attributes_QuotedValues()
        {
            var reader = new Utf8XmlReader("<e a=\"1\" b='2'/>"u8);
            reader.Read();
            reader.AttributeCount.Should().Be(2);
            reader.MoveToNextAttribute().Should().BeTrue();
            reader.LocalNameEquals("a"u8).Should().BeTrue();
            reader.GetString().Should().Be("1");
            reader.MoveToNextAttribute().Should().BeTrue();
            reader.GetInt32().Should().Be(2);
            reader.MoveToNextAttribute().Should().BeFalse();
            reader.MoveToElement();
            reader.TokenType.Should().Be(XmlTokenType.StartElement);
        }

        [TestMethod]
        public void Read_CData_AndCommentAndPi()
        {
            var xml = "<?pi data?><!--c--><e><![CDATA[x]]></e>"u8;
            var reader = new Utf8XmlReader(xml, new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow });
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.ProcessingInstruction);
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.Comment);
            reader.GetComment().Should().Be("c");
            reader.Read().Should().BeTrue();
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.CData);
            reader.GetString().Should().Be("x");
        }

        [TestMethod]
        public void Read_Comment_SkipAndDisallow()
        {
            var skip = new Utf8XmlReader("<!--c--><e/>"u8, new XmlReaderOptions { CommentHandling = XmlCommentHandling.Skip });
            skip.Read().Should().BeTrue();
            skip.TokenType.Should().Be(XmlTokenType.StartElement);

            var act = () =>
            {
                var reader = new Utf8XmlReader("<!--c--><e/>"u8, new XmlReaderOptions { CommentHandling = XmlCommentHandling.Disallow });
                while (reader.Read())
                {
                }
            };
            act.Should().Throw<XmlException>();
        }

        [TestMethod]
        public void Read_CopyString_AndDepth()
        {
            var reader = new Utf8XmlReader("<e>hi</e>"u8);
            reader.Read();
            reader.CurrentDepth.Should().Be(0);
            reader.BytesConsumed.Should().Be(3);
            reader.IsFinalBlock.Should().BeTrue();
            reader.HasValueSequence.Should().BeFalse();
            reader.NamespaceUriSpan.IsEmpty.Should().BeTrue();
            reader.NamespaceUriEquals("").Should().BeTrue();
            reader.NamespaceUriEquals(""u8).Should().BeTrue();
            reader.Read();
            Span<byte> utf8 = stackalloc byte[8];
            reader.CopyString(utf8[..2]).Should().Be(2);
            utf8[..2].SequenceEqual("hi"u8).Should().BeTrue();
            Span<char> chars = stackalloc char[8];
            reader.CopyString(chars).Should().Be(2);
            new string(chars[..2]).Should().Be("hi");
        }

        [TestMethod]
        public void Read_CurrentState_RoundTripsOptions()
        {
            var options = new XmlReaderOptions { MaxDepth = 3 };
            var reader = new Utf8XmlReader("<e/>"u8, options);
            reader.Read();
            reader.CurrentState.Options.MaxDepth.Should().Be(3);
            var resumed = new Utf8XmlReader("<e/>"u8, true, reader.CurrentState);
            resumed.Version.Should().Be(XmlVersion.Version10);
        }

        [TestMethod]
        public void Read_EmptyElement_HasNoEndToken()
        {
            var reader = new Utf8XmlReader("<e/>"u8);
            reader.Read().Should().BeTrue();
            reader.IsEmptyElement.Should().BeTrue();
            reader.Read().Should().BeFalse();
        }

        [TestMethod]
        public void Read_ExpandsFiveEntities()
        {
            var reader = new Utf8XmlReader("<e>&lt;&gt;&amp;&quot;&apos;</e>"u8);
            reader.Read();
            reader.Read();
            reader.GetString().Should().Be("<>&\"'");
            reader.ValueIsEscaped.Should().BeTrue();
        }

        [TestMethod]
        public void Read_Getters_Primitives()
        {
            OnText("<e>true</e>").GetBoolean().Should().BeTrue();
            OnText("<e>1</e>").GetBoolean().Should().BeTrue();
            OnText("<e>false</e>").GetBoolean().Should().BeFalse();
            OnText("<e>0</e>").GetBoolean().Should().BeFalse();
            OnText("<e>7</e>").GetByte().Should().Be(7);
            OnText("<e>AQID</e>").GetBytesFromBase64().Should().Equal(1, 2, 3);
            OnText("<e>2020-01-02</e>").GetDateOnly().Should().Be(new DateOnly(2020, 1, 2));
            OnText("<e>2020-01-02T03:04:05</e>").GetDateTime().Should().Be(new DateTime(2020, 1, 2, 3, 4, 5));
            OnText("<e>2020-01-02T03:04:05Z</e>").GetDateTimeOffset().Should().Be(new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero));
            OnText("<e>1.5</e>").GetDecimal().Should().Be(1.5m);
            double.IsPositiveInfinity(OnText("<e>INF</e>").GetDouble()).Should().BeTrue();
            double.IsNegativeInfinity(OnText("<e>-INF</e>").GetDouble()).Should().BeTrue();
            double.IsNaN(OnText("<e>NaN</e>").GetDouble()).Should().BeTrue();
            OnText("<e>1.25</e>").GetDouble().Should().Be(1.25);
            OnText("<e>aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee</e>").GetGuid().Should().Be(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
            OnText("<e>-2</e>").GetInt16().Should().Be(-2);
            OnText("<e>3</e>").GetInt32().Should().Be(3);
            OnText("<e>4</e>").GetInt64().Should().Be(4);
            OnText("<e>-5</e>").GetSByte().Should().Be(-5);
            OnText("<e>1.5</e>").GetSingle().Should().Be(1.5f);
            OnText("<e>03:04:05</e>").GetTimeOnly().Should().Be(new TimeOnly(3, 4, 5));
            OnText("<e>PT1S</e>").GetTimeSpan().Should().Be(TimeSpan.FromSeconds(1));
            OnText("<e>6</e>").GetUInt16().Should().Be(6);
            OnText("<e>7</e>").GetUInt32().Should().Be(7u);
            OnText("<e>8</e>").GetUInt64().Should().Be(8ul);
        }

        [TestMethod]
        public void Read_Getters_TryFailures()
        {
            var r = OnText("<e>!!!!</e>");
            ShouldThrow(ref r);
            r.TryGetByte(out _).Should().BeFalse();
            r.TryGetBytesFromBase64(out _).Should().BeFalse();
            r.TryGetDateOnly(out _).Should().BeFalse();
            r.TryGetDateTime(out _).Should().BeFalse();
            r.TryGetDateTimeOffset(out _).Should().BeFalse();
            r.TryGetDecimal(out _).Should().BeFalse();
            r.TryGetDouble(out _).Should().BeFalse();
            r.TryGetGuid(out _).Should().BeFalse();
            r.TryGetInt16(out _).Should().BeFalse();
            r.TryGetInt32(out _).Should().BeFalse();
            r.TryGetInt64(out _).Should().BeFalse();
            r.TryGetSByte(out _).Should().BeFalse();
            r.TryGetSingle(out _).Should().BeFalse();
            r.TryGetTimeOnly(out _).Should().BeFalse();
            r.TryGetTimeSpan(out _).Should().BeFalse();
            r.TryGetUInt16(out _).Should().BeFalse();
            r.TryGetUInt32(out _).Should().BeFalse();
            r.TryGetUInt64(out _).Should().BeFalse();
            OnText("<e>1e2</e>").TryGetDecimal(out _).Should().BeFalse();
            OnText("<e>1e1</e>").TryGetDecimal(out _).Should().BeFalse();
        }

        [TestMethod]
        public void Read_GetNil()
        {
            var reader = new Utf8XmlReader("<e xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>"u8);
            reader.Read();
            reader.GetNil().Should().BeTrue();
            var none = new Utf8XmlReader("<e/>"u8);
            none.Read();
            none.GetNil().Should().BeFalse();
            none.Read();
            var threw = false;
            try
            {
                none.GetNil();
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Read_IncompleteBuffer_ReturnsFalse()
        {
            var reader = new Utf8XmlReader("<e"u8, false, default);
            reader.Read().Should().BeFalse();
        }

        [TestMethod]
        [DataRow(64, 1000, 1024, 1_048_576, null, DisplayName = "everything at its default limit")]
        [DataRow(65, 1, 1, 1, "MaxDepth exceeded (64).", DisplayName = "one level too deep")]
        [DataRow(1, 1001, 1, 1, "MaxAttributeCount exceeded (1000).", DisplayName = "one attribute too many")]
        [DataRow(1, 1, 1025, 1, "MaxNameByteCount exceeded (1024).", DisplayName = "a name one byte too long")]
        [DataRow(1, 1, 1, 1_048_577, "MaxCommentByteCount exceeded (1048576).", DisplayName = "a comment one byte too long")]
        public void Read_LimitsLeftAtZero_AreTheDocumentedDefaults(int depth, int attributes, int nameBytes, int commentBytes, string? expectedMessage)
        {
            var options = new XmlReaderOptions
            {
                CommentHandling = XmlCommentHandling.Allow,
                MaxDepth = 0,
                MaxAttributeCount = 0,
                MaxNameByteCount = 0,
                MaxCommentByteCount = 0,
                MaxTextByteCount = 0,
                MaxDocumentByteCount = 0,
            };
            var xml = new StringBuilder();
            var name = new string('n', nameBytes);
            for (var i = 1; i < depth; i++)
            {
                xml.Append("<d>");
            }

            xml.Append('<').Append(name);
            for (var i = 0; i < attributes; i++)
            {
                xml.Append(" a").Append(i).Append("=\"\"");
            }

            xml.Append("><!--").Append('c', commentBytes).Append("--></").Append(name).Append('>');
            for (var i = 1; i < depth; i++)
            {
                xml.Append("</d>");
            }

            string? message = null;
            var tokens = 0;
            try
            {
                var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml.ToString()), options);
                while (reader.Read())
                {
                    tokens++;
                }
            }
            catch (XmlException ex)
            {
                message = ex.Message;
            }

            message.Should().Be(expectedMessage);
            if (expectedMessage is null)
            {
                tokens.Should().Be((2 * depth) + 1);
            }
        }

        [TestMethod]
        public void Read_MaxDepthExceeded()
        {
            var act = () =>
            {
                var reader = new Utf8XmlReader("<a><b/></a>"u8, new XmlReaderOptions { MaxDepth = 1 });
                while (reader.Read())
                {
                }
            };
            act.Should().Throw<XmlException>().WithMessage("MaxDepth exceeded (1).");
        }

        [TestMethod]
        public void Read_MaxNameByteCountExceeded()
        {
            var act = () =>
            {
                var reader = new Utf8XmlReader("<abcdef/>"u8, new XmlReaderOptions { MaxNameByteCount = 2 });
                reader.Read();
            };
            act.Should().Throw<XmlException>().WithMessage("MaxNameByteCount exceeded (2).");
        }

        [TestMethod]
        public void Read_MismatchedEndTag()
        {
            var act = () =>
            {
                var reader = new Utf8XmlReader("<a></b>"u8);
                while (reader.Read())
                {
                }
            };
            act.Should().Throw<XmlException>();
        }

        [TestMethod]
        public void Read_MultipleRoots_Rejected()
        {
            var act = () =>
            {
                var reader = new Utf8XmlReader("<a/><b/>"u8);
                while (reader.Read())
                {
                }
            };
            act.Should().Throw<XmlException>().WithMessage("Multiple root elements are not allowed.");
        }

        [TestMethod]
        public void Read_NumericCharacterReference()
        {
            var reader = new Utf8XmlReader("<e>&#65;&#x42;</e>"u8);
            reader.Read();
            reader.Read();
            reader.GetString().Should().Be("AB");
        }

        [TestMethod]
        public void Read_Prefix_OnEmptyElement()
        {
            var reader = new Utf8XmlReader("<p:e xmlns:p=\"urn:x\"/>"u8);
            reader.Read();
            reader.PrefixSpan.Length.Should().Be(1);
            reader.LocalNameEquals("e").Should().BeTrue();
        }

        [TestMethod]
        public void Read_RejectsDoctype()
        {
            var act = () => Drain("<!DOCTYPE e><e/>");
            act.Should().Throw<XmlException>().WithMessage("DTD is not allowed.");
        }

        [TestMethod]
        public void Read_RejectsNonUtf8Encoding()
        {
            var act = () => Drain("<?xml version=\"1.0\" encoding=\"utf-16\"?><e/>");
            act.Should().Throw<XmlException>().WithMessage("XmlEssentials is UTF-8 only. Declared encoding 'utf-16' is not supported.");
        }

        [TestMethod]
        public void Read_RejectsUndefinedEntity()
        {
            var act = () => Drain("<e>&foo;</e>");
            act.Should().Throw<XmlException>().WithMessage("Undefined entity.");
        }

        [TestMethod]
        public void Read_SequenceConstructor()
        {
            var bytes = "<e/>"u8.ToArray();
            var sequence = new ReadOnlySequence<byte>(bytes);
            var reader = new Utf8XmlReader(sequence);
            reader.Read().Should().BeTrue();
            reader.BytesConsumed.Should().Be(4);
            reader.Position.Should().Be(sequence.GetPosition(reader.BytesConsumed));
            var overSpan = new Utf8XmlReader(bytes);
            overSpan.Read().Should().BeTrue();
            overSpan.Position.Should().Be(default(SequencePosition));
        }

        [TestMethod]
        public void Read_Skip_EmptyAndNested()
        {
            var empty = new Utf8XmlReader("<e/>"u8);
            empty.Read();
            empty.Skip();
            var nested = new Utf8XmlReader("<a><b>x</b></a>"u8);
            nested.Read();
            nested.Skip();
            nested.TokenType.Should().Be(XmlTokenType.EndElement);
        }

        [TestMethod]
        public void Read_StartAndEndElement_ReportsText()
        {
            var reader = new Utf8XmlReader("<e>hi</e>"u8);
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.StartElement);
            reader.IsEmptyElement.Should().BeFalse();
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.Text);
            reader.GetString().Should().Be("hi");
            reader.ValueTextEquals("hi".AsSpan()).Should().BeTrue();
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            reader.Read().Should().BeFalse();
        }

        [TestMethod]
        public void Read_Whitespace_SignificantAndNone()
        {
            var significant = new Utf8XmlReader("<a>\n<b/>\n</a>"u8, new XmlReaderOptions { WhitespaceHandling = XmlWhitespaceHandling.Significant });
            significant.Read();
            significant.Read();
            significant.TokenType.Should().Be(XmlTokenType.Text);

            var none = () => Drain("<a>\n<b/>\n</a>", new XmlReaderOptions { WhitespaceHandling = XmlWhitespaceHandling.None });
            none.Should().Throw<XmlException>();
        }

        [TestMethod]
        public void Read_XmlDeclaration_SetsVersion10()
        {
            var reader = new Utf8XmlReader("<?xml version=\"1.0\" encoding=\"utf-8\"?><e/>"u8);
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.XmlDeclaration);
            reader.Version.Should().Be(XmlVersion.Version10);
        }

        [TestMethod]
        public void Read_XmlDeclaration_SetsVersion11()
        {
            var reader = new Utf8XmlReader("<?xml version=\"1.1\" encoding=\"utf-8\"?><e/>"u8);
            reader.Read().Should().BeTrue();
            reader.Version.Should().Be(XmlVersion.Version11);
        }

        [TestMethod]
        public void Read_XmlDeclaration_WithoutEncoding()
        {
            var reader = new Utf8XmlReader("<?xml version=\"1.0\"?><e/>"u8);
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.XmlDeclaration);
            reader.Version.Should().Be(XmlVersion.Version10);
        }

        [TestMethod]
        public void XmlName_EqualityIgnoresPrefix()
        {
            var a = new XmlName("n", "urn:x", "p");
            var b = new XmlName("n", "urn:x", "q");
            (a == b).Should().BeTrue();
            a.ToString().Should().Be("p:n");
        }

        [TestMethod]
        public void Read_RejectsJunkBeforeRoot()
        {
            var act = () => Drain("nope<e/>");
            act.Should().Throw<XmlException>();
        }

        [TestMethod]
        public void Read_RejectsDuplicateAttribute()
        {
            var act = () => Drain("<e a=\"1\" a=\"2\"/>");
            act.Should().Throw<XmlException>().WithMessage("Duplicate attribute.");
        }

        [TestMethod]
        public void Read_RejectsInvalidUtf8()
        {
            var act = () =>
            {
                var reader = new Utf8XmlReader([(byte)'<', (byte)'e', 0xFF, (byte)'/', (byte)'>']);
                reader.Read();
            };
            act.Should().Throw<XmlException>().WithMessage("Invalid UTF-8.");
        }

        [TestMethod]
        public void Read_RejectsNul()
        {
            var act = () =>
            {
                var reader = new Utf8XmlReader([(byte)'<', (byte)'e', (byte)'>', 0x00, (byte)'<', (byte)'/', (byte)'e', (byte)'>']);
                while (reader.Read())
                {
                }
            };
            act.Should().Throw<XmlException>().WithMessage("NUL is not allowed.");
        }

        [TestMethod]
        public void Read_RejectsC0CharacterReferenceInXml10()
        {
            var act = () => Drain("<e>&#x1;</e>");
            act.Should().Throw<XmlException>();
        }

        [TestMethod]
        public void Read_AcceptsRestrictedCharAsNcrInXml11()
        {
            var reader = new Utf8XmlReader("<?xml version=\"1.1\"?><e>&#x1;</e>"u8);
            reader.Read();
            reader.Read();
            reader.Read();
            reader.GetString().Should().Be("\u0001");
        }

        [TestMethod]
        public void Read_RejectsRestrictedCharLiteralInXml11()
        {
            var xml = "<?xml version=\"1.1\"?><e>"u8.ToArray().Concat(new byte[] { 0x01 }).Concat("</e>"u8.ToArray()).ToArray();
            var act = () =>
            {
                var reader = new Utf8XmlReader(xml);
                while (reader.Read())
                {
                }
            };
            act.Should().Throw<XmlException>().WithMessage("XML 1.1 RestrictedChar must be a character reference.");
        }

        [TestMethod]
        public void Read_MaxAttributeCountExceeded()
        {
            var act = () => Drain("<e a=\"1\" b=\"2\"/>", new XmlReaderOptions { MaxAttributeCount = 1 });
            act.Should().Throw<XmlException>().WithMessage("MaxAttributeCount exceeded (1).");
        }

        [TestMethod]
        public void Read_MaxCommentByteCountExceeded()
        {
            var act = () => Drain("<!--abcd--><e/>", new XmlReaderOptions { MaxCommentByteCount = 2, CommentHandling = XmlCommentHandling.Allow });
            act.Should().Throw<XmlException>().WithMessage("MaxCommentByteCount exceeded (2).");
        }

        [TestMethod]
        public void Read_MaxTextByteCountExceeded()
        {
            var act = () => Drain("<e>abcd</e>", new XmlReaderOptions { MaxTextByteCount = 2 });
            act.Should().Throw<XmlException>().WithMessage("MaxTextByteCount exceeded (2).");
        }

        [TestMethod]
        public void Read_CommentContainsDoubleHyphen()
        {
            var act = () => Drain("<!-- a -- b --><e/>", new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow });
            act.Should().Throw<XmlException>().WithMessage("Comments must not contain '--'.");
        }

        [TestMethod]
        public void Read_MultipleColonsInName()
        {
            var act = () => Drain("<a:b:c/>");
            act.Should().Throw<XmlException>().WithMessage("Multiple colons in name.");
        }

        [TestMethod]
        public void Read_InvalidStartTag()
        {
            var act = () => Drain("<>");
            act.Should().Throw<XmlException>();
        }

        [TestMethod]
        public void Read_AttributeErrors()
        {
            var missingEquals = () => Drain("<e a/>");
            missingEquals.Should().Throw<XmlException>();
            var missingQuote = () => Drain("<e a=1/>");
            missingQuote.Should().Throw<XmlException>();
            var lt = () => Drain("<e a=\"<\">");
            lt.Should().Throw<XmlException>();
            var slash = () => Drain("<e /x>");
            slash.Should().Throw<XmlException>();
        }

        [TestMethod]
        public void Read_EmptyDocument_ThrowsWithoutRoot()
        {
            var act = () => Drain("");
            act.Should().Throw<XmlException>().WithMessage("Document does not have a root element.");
        }

        [TestMethod]
        public void Read_EmptyFragment_Allowed()
        {
            var reader = new Utf8XmlReader(""u8, new XmlReaderOptions { AllowMultipleRoots = true });
            reader.Read().Should().BeFalse();
            reader.Read().Should().BeFalse();
        }

        [TestMethod]
        public void Read_IncompleteFinalBlockThrows()
        {
            var act = () => Drain("<e");
            act.Should().Throw<XmlException>().WithMessage("Unexpected end of XML.");
        }

        [TestMethod]
        public void Read_IncompleteMarkupNeedMore()
        {
            var reader = new Utf8XmlReader("<!"u8, false, default);
            reader.Read().Should().BeFalse();
        }

        [TestMethod]
        public void Read_DtdVariants()
        {
            var doctype = () => Drain("<!DOCTYPE e><e/>");
            doctype.Should().Throw<XmlException>().WithMessage("DTD is not allowed.");
            var entity = () => Drain("<!ENTITY e \"x\"><e/>");
            entity.Should().Throw<XmlException>().WithMessage("DTD is not allowed.");
            var element = () => Drain("<!ELEMENT e EMPTY><e/>");
            element.Should().Throw<XmlException>().WithMessage("DTD is not allowed.");
            var attlist = () => Drain("<!ATTLIST e a CDATA #IMPLIED><e/>");
            attlist.Should().Throw<XmlException>().WithMessage("DTD is not allowed.");
            var notation = () => Drain("<!NOTATION n SYSTEM \"x\"><e/>");
            notation.Should().Throw<XmlException>().WithMessage("DTD is not allowed.");
        }

        [TestMethod]
        public void Read_CrlfAndXml11LineEndings()
        {
            var crlf = new Utf8XmlReader("<e>a\r\nb</e>"u8);
            crlf.Read();
            crlf.Read();
            crlf.GetString().Should().Be("a\nb");

            var nel = "<?xml version=\"1.1\"?><e>a"u8.ToArray().Concat(new byte[] { 0xC2, 0x85 }).Concat("b</e>"u8.ToArray()).ToArray();
            var reader = new Utf8XmlReader(nel);
            reader.Read();
            reader.Read();
            reader.Read();
            reader.GetString().Should().Be("a\nb");

            var ls = "<?xml version=\"1.1\"?><e>a"u8.ToArray().Concat(new byte[] { 0xE2, 0x80, 0xA8 }).Concat("b</e>"u8.ToArray()).ToArray();
            var lsReader = new Utf8XmlReader(ls);
            lsReader.Read();
            lsReader.Read();
            lsReader.Read();
            lsReader.GetString().Should().Be("a\nb");
        }

        [TestMethod]
        public void Read_NineAttributes_AreAllReported()
        {
            var xml = "<e a1=\"1\" a2=\"2\" a3=\"3\" a4=\"4\" a5=\"5\" a6=\"6\" a7=\"7\" a8=\"8\" a9=\"9\"/>";
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml));
            reader.Read();
            reader.AttributeCount.Should().Be(9);
            var attributes = new List<string>();
            while (reader.MoveToNextAttribute())
            {
                attributes.Add(Encoding.UTF8.GetString(reader.LocalNameSpan) + "=" + reader.GetString());
            }

            attributes.Should().Equal("a1=1", "a2=2", "a3=3", "a4=4", "a5=5", "a6=6", "a7=7", "a8=8", "a9=9");

            reader.MoveToElement();
            reader.TokenType.Should().Be(XmlTokenType.StartElement);
        }

        [TestMethod]
        public void Read_AttributeWhitespaceNormalization()
        {
            var reader = new Utf8XmlReader("<e a=\"x\ty\nz\"/>"u8);
            reader.Read();
            reader.MoveToNextAttribute();
            reader.GetString().Should().Be("x y z");
        }

        [TestMethod]
        public void Read_PrefixedAttribute()
        {
            var reader = new Utf8XmlReader("<e xmlns:p=\"urn:x\" p:a=\"1\"/>"u8);
            reader.Read();
            var found = false;
            while (reader.MoveToNextAttribute())
            {
                if (reader.LocalNameEquals("a"))
                {
                    reader.PrefixSpan.Length.Should().Be(1);
                    found = true;
                    break;
                }
            }

            found.Should().BeTrue();
            reader.MoveToNextAttribute().Should().BeFalse();
        }

        [TestMethod]
        public void Read_EqualsHelpers_NullAndMismatch()
        {
            var reader = new Utf8XmlReader("<e>hi</e>"u8);
            reader.Read();
            reader.LocalNameEquals((string?)null).Should().BeFalse();
            reader.NamespaceUriEquals("urn:x").Should().BeFalse();
            reader.NamespaceUriEquals("urn:x".AsSpan()).Should().BeFalse();
            reader.Read();
            reader.ValueTextEquals((string?)null).Should().BeFalse();
            reader.ValueTextEquals("no").Should().BeFalse();
        }

        [TestMethod]
        public void Read_MoveToNextAttribute_WhenNotOnElement()
        {
            var reader = new Utf8XmlReader("<e>hi</e>"u8);
            reader.Read();
            reader.Read();
            reader.MoveToNextAttribute().Should().BeFalse();
            reader.HasAttributes.Should().BeFalse();
        }

        [TestMethod]
        public void Read_TrySkip_OnTextAndIncomplete()
        {
            var text = new Utf8XmlReader("<e>hi</e>"u8);
            text.Read();
            text.Read();
            text.TrySkip().Should().BeTrue();

            var incomplete = new Utf8XmlReader("<a><b"u8, false, default);
            incomplete.Read();
            incomplete.TrySkip().Should().BeFalse();
            var skip = () =>
            {
                var reader = new Utf8XmlReader("<a><b"u8, false, default);
                reader.Read();
                reader.Skip();
            };
            skip.Should().Throw<XmlException>();
        }

        [TestMethod]
        public void Read_Sequence_MultiSegment()
        {
            var first = "<e"u8.ToArray();
            var second = "/>"u8.ToArray();
            var start = new Segment(first, 0);
            start.Append(second);
            var sequence = new ReadOnlySequence<byte>(start, 0, start.Next!, second.Length);
            var reader = new Utf8XmlReader(sequence);
            reader.Read().Should().BeTrue();
            reader.LocalNameEquals("e").Should().BeTrue();
            reader.Position.Should().Be(sequence.GetPosition(reader.BytesConsumed));
            reader.HasValueSequence.Should().BeFalse();
            reader.ValueSequence.IsEmpty.Should().BeTrue();
            reader.TokenStartIndex.Should().Be(0);
        }

        [TestMethod]
        public void Read_Getters_MoreFailuresAndDurations()
        {
            OnText("<e>1e1</e>").TryGetInt32(out _).Should().BeFalse();
            var plus = OnText("<e>1e1</e>");
            var plusThrew = false;
            try
            {
                plus.GetInt32();
            }
            catch (XmlException)
            {
                plusThrew = true;
            }

            plusThrew.Should().BeTrue();
            OnText("<e>1e2</e>").TryGetInt32(out _).Should().BeFalse();
            OnText("<e>256</e>").TryGetByte(out _).Should().BeFalse();
            var overflow = OnText("<e>256</e>");
            var overflowThrew = false;
            try
            {
                overflow.GetByte();
            }
            catch (XmlException)
            {
                overflowThrew = true;
            }

            overflowThrew.Should().BeTrue();
            OnText("<e>PT1H</e>").GetTimeSpan().Should().Be(TimeSpan.FromHours(1));
            OnText("<e>PT1M</e>").GetTimeSpan().Should().Be(TimeSpan.FromMinutes(1));
            OnText("<e>P1D</e>").GetTimeSpan().Should().Be(TimeSpan.FromDays(1));
            OnText("<e>-PT1S</e>").GetTimeSpan().Should().Be(-TimeSpan.FromSeconds(1));
            OnText("<e>P1Y</e>").TryGetTimeSpan(out _).Should().BeFalse();
            OnText("<e>P1M</e>").TryGetTimeSpan(out _).Should().BeFalse();
            OnText("<e>P</e>").TryGetTimeSpan(out _).Should().BeFalse();
            OnText("<e>X</e>").TryGetTimeSpan(out _).Should().BeFalse();
            OnText("<e>PT1X</e>").TryGetTimeSpan(out _).Should().BeFalse();
            OnText("<e></e>").TryGetTimeSpan(out _).Should().BeFalse();
            var duration = OnText("<e>nope</e>");
            var durationThrew = false;
            try
            {
                duration.GetTimeSpan();
            }
            catch (XmlException)
            {
                durationThrew = true;
            }

            durationThrew.Should().BeTrue();
            OnText("<e>nope</e>").TryGetSingle(out _).Should().BeFalse();
            var single = OnText("<e>nope</e>");
            var singleThrew = false;
            try
            {
                single.GetSingle();
            }
            catch (XmlException)
            {
                singleThrew = true;
            }

            singleThrew.Should().BeTrue();
            OnText("<e>Infinity</e>").TryGetDouble(out _).Should().BeFalse();
            OnText("<e>-Infinity</e>").TryGetDouble(out _).Should().BeFalse();
        }

        [TestMethod]
        public void Read_HexCharacterReference_DigitsAnyCase_PrefixLowerCase()
        {
            var reader = new Utf8XmlReader("<e>&#x4a;&#x4A;</e>"u8);
            reader.Read();
            reader.Read();
            reader.GetString().Should().Be("JJ");
            XmlReaderHarness.MustThrow("<e>&#X42;</e>");
            XmlReaderHarness.MustThrow("<e a=\"&#X42;\"/>");
            XmlReaderHarness.MustThrow("<e>&#12x;</e>");
        }

        [TestMethod]
        public void Read_EncodingUtf8Aliases()
        {
            XmlReaderHarness.Tokens("<?xml version=\"1.0\" encoding=\"UTF-8\"?><e/>"u8, new XmlReaderOptions()).Should().Equal("XmlDeclaration :xml{} [version=\"1.0\" encoding=\"UTF-8\"]", "StartElement :e{} empty");
            XmlReaderHarness.Tokens("<?xml version=\"1.0\" encoding='utf8'?><e/>"u8, new XmlReaderOptions()).Should().Equal("XmlDeclaration :xml{} [version=\"1.0\" encoding='utf8']", "StartElement :e{} empty");
        }

        [TestMethod]
        public void Read_NonAsciiName()
        {
            var xml = "<"u8.ToArray().Concat("é"u8.ToArray()).Concat("/>"u8.ToArray()).ToArray();
            var reader = new Utf8XmlReader(xml);
            reader.Read().Should().BeTrue();
        }

        [TestMethod]
        public void Read_GetNilVariants()
        {
            var one = new Utf8XmlReader("<e xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"1\"/>"u8);
            one.Read();
            one.GetNil().Should().BeTrue();
            var no = new Utf8XmlReader("<e xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"false\"/>"u8);
            no.Read();
            no.GetNil().Should().BeFalse();
        }

        [TestMethod]
        public void Read_CDataIncompleteAndPiIncomplete()
        {
            var cdata = () => Drain("<e><![CDATA[x");
            cdata.Should().Throw<XmlException>();
            var pi = () => Drain("<?pi data");
            pi.Should().Throw<XmlException>();
            var comment = () => Drain("<!--c");
            comment.Should().Throw<XmlException>();
            var cdataLimit = () => Drain("<e><![CDATA[abcd]]></e>", new XmlReaderOptions { MaxTextByteCount = 2 });
            cdataLimit.Should().Throw<XmlException>().WithMessage("MaxTextByteCount exceeded (2).");
        }

        [TestMethod]
        public void Read_UnexpectedEndInsideElement()
        {
            var act = () => Drain("<a><b></b>");
            act.Should().Throw<XmlException>().WithMessage("Unexpected end of XML.");
        }

        [TestMethod]
        public void Read_EndTagWithoutMatch()
        {
            var act = () => Drain("</e>");
            act.Should().Throw<XmlException>();
        }

        [TestMethod]
        public void Read_InvalidNameAfterPrefix()
        {
            var act = () => Drain("<p:1/>");
            act.Should().Throw<XmlException>();
        }

        [TestMethod]
        public void Read_InvalidCharacterReference()
        {
            var empty = () => Drain("<e>&#;</e>");
            empty.Should().Throw<XmlException>();
            var huge = () => Drain("<e>&#x110000;</e>");
            huge.Should().Throw<XmlException>();
        }

        [TestMethod]
        public void Read_CurrentState_CapturesDepth()
        {
            var reader = new Utf8XmlReader("<a><b/></a>"u8, new XmlReaderOptions { CommentHandling = XmlCommentHandling.Skip });
            reader.Read();
            reader.Read();
            var state = reader.CurrentState;
            state.Options.CommentHandling.Should().Be(XmlCommentHandling.Skip);
            var resumed = new Utf8XmlReader(""u8, true, state);
            resumed.CurrentDepth.Should().Be(1);
        }

        [TestMethod]
        [DataRow("<?xml version=\"1.0\"?><r/>",
            "XmlDeclaration xml [version=\"1.0\"|version=\"1.0\"|version=\"1.0\"|plain|value]\n" +
            "StartElement r [|||plain|empty]",
            DisplayName = "start tag after a declaration")]
        [DataRow("<!--k--><r/>",
            "Comment  [k|k|k|plain|value]\n" +
            "StartElement r [|||plain|empty]",
            DisplayName = "start tag after a comment")]
        [DataRow("<?p q?><r/>",
            "ProcessingInstruction p [q|q|q|plain|value]\n" +
            "StartElement r [|||plain|empty]",
            DisplayName = "start tag after a processing instruction")]
        [DataRow("<r>text<a/></r>",
            "StartElement r [|||plain|empty]\n" +
            "Text  [text|text|text|plain|value]\n" +
            "StartElement a [|||plain|empty]\n" +
            "EndElement r [|||plain|empty]",
            DisplayName = "empty element after text")]
        [DataRow("<r>a&lt;b<a>c</a></r>",
            "StartElement r [|||plain|empty]\n" +
            "Text  [a<b|a<b|a<b|escaped|value]\n" +
            "StartElement a [|||plain|empty]\n" +
            "Text  [c|c|c|plain|value]\n" +
            "EndElement a [|||plain|empty]\n" +
            "EndElement r [|||plain|empty]",
            DisplayName = "start and end tags after text that was decoded")]
        [DataRow("<r><![CDATA[z]]><a></a></r>",
            "StartElement r [|||plain|empty]\n" +
            "CData  [z|z|z|plain|value]\n" +
            "StartElement a [|||plain|empty]\n" +
            "EndElement a [|||plain|empty]\n" +
            "EndElement r [|||plain|empty]",
            DisplayName = "start and end tags after a CDATA section")]
        [DataRow("<r>x<a b=\"1&amp;2\" c='3'/>y</r>",
            "StartElement r [|||plain|empty]\n" +
            "Text  [x|x|x|plain|value]\n" +
            "StartElement a [|||plain|empty] @b [1&2|1&2|1&2|escaped|value] @c [3|3|3|plain|value] element [|||plain|empty]\n" +
            "Text  [y|y|y|plain|value]\n" +
            "EndElement r [|||plain|empty]",
            DisplayName = "the element again after its attributes")]
        [DataRow("<p:r xmlns:p=\"urn:p\"><p:a>1</p:a><!--k--><p:b/><?p q?></p:r>",
            "StartElement r [|||plain|empty] @p [urn:p|urn:p|urn:p|plain|value] element [|||plain|empty]\n" +
            "StartElement a [|||plain|empty]\n" +
            "Text  [1|1|1|plain|value]\n" +
            "EndElement a [|||plain|empty]\n" +
            "Comment  [k|k|k|plain|value]\n" +
            "StartElement b [|||plain|empty]\n" +
            "ProcessingInstruction p [q|q|q|plain|value]\n" +
            "EndElement r [|||plain|empty]",
            DisplayName = "prefixed names between a comment and a processing instruction")]
        public void ValueMembers_OnStartAndEndTags_AreEmpty(string xml, string expected)
        {
            var bytes = Encoding.UTF8.GetBytes(xml);
            var tokens = new List<string>();
            var reader = new Utf8XmlReader(bytes, ValueOptions);
            AppendValues(ref reader, tokens);
            string.Join("\n", tokens).Should().Be(expected);

            for (var split = 1; split < bytes.Length; split++)
            {
                var resumed = new List<string>();
                var first = new Utf8XmlReader(bytes.AsSpan(0, split), false, new XmlReaderState(ValueOptions));
                AppendValues(ref first, resumed);
                var second = new Utf8XmlReader(bytes.AsSpan((int)first.BytesConsumed), true, first.CurrentState);
                AppendValues(ref second, resumed);
                string.Join("\n", resumed).Should().Be(expected, "the values must not depend on a split at byte {0}", split);
            }
        }

        [TestMethod]
        public void CurrentDepth_IsTheNumberOfElementsTheTokenIsInside()
        {
            // The depth of a token is the number of elements open around it, as XmlReader.Depth and
            // Utf8JsonReader.CurrentDepth have it: a start tag, its end tag, and an empty element written at
            // the same place are all at the same depth.
            var bytes = "<?xml version=\"1.0\"?><!--top--><a x=\"1\"><b/><c>t</c><!--k--><d y=\"2\"/><?p q?><e><f/><g><![CDATA[z]]></g></e></a><!--tail-->"u8.ToArray();
            const string expected =
                "XmlDeclaration:0 Comment:0 StartElement a:0 @x:0 StartElement b:1 StartElement c:1 Text:2 EndElement c:1 Comment:1 " +
                "StartElement d:1 @y:1 ProcessingInstruction:1 StartElement e:1 StartElement f:2 StartElement g:2 CData:3 EndElement g:2 " +
                "EndElement e:1 EndElement a:0 Comment:0";
            var reader = new Utf8XmlReader(bytes, ValueOptions);
            reader.CurrentDepth.Should().Be(0);
            Depths(ref reader).Should().Be(expected);
            reader.CurrentDepth.Should().Be(0);
            for (var split = 1; split < bytes.Length; split++)
            {
                var first = new Utf8XmlReader(bytes.AsSpan(0, split), false, new XmlReaderState(ValueOptions));
                var head = Depths(ref first);
                var second = new Utf8XmlReader(bytes.AsSpan((int)first.BytesConsumed), true, first.CurrentState);
                var tail = Depths(ref second);
                (head.Length > 0 && tail.Length > 0 ? head + " " + tail : head + tail).Should().Be(expected, "the depths must not depend on a split at byte {0}", split);
            }
        }

        [TestMethod]
        public void CurrentDepth_InAStateCapturedOnAStartTag_IsTheDepthOfThatTag()
        {
            // The state says where the reader is. A reader made from it reports the same depth before it reads,
            // whether the state was captured on a start tag or after a read that needed more input.
            var reader = new Utf8XmlReader("<a><b>"u8, false, new XmlReaderState(ValueOptions));
            reader.Read();
            reader.CurrentDepth.Should().Be(0);
            new Utf8XmlReader(""u8, false, reader.CurrentState).CurrentDepth.Should().Be(0);
            reader.Read();
            reader.CurrentDepth.Should().Be(1);
            new Utf8XmlReader(""u8, false, reader.CurrentState).CurrentDepth.Should().Be(1);
            reader.Read().Should().BeFalse();
            reader.CurrentDepth.Should().Be(2, "the reader is now between tokens, inside both elements");
            new Utf8XmlReader(""u8, false, reader.CurrentState).CurrentDepth.Should().Be(2);
        }

        [TestMethod]
        public void CurrentDepth_AfterSkip_IsTheDepthOfTheSkippedElement()
        {
            var reader = new Utf8XmlReader("<a><b><c>t</c></b><d/></a>"u8);
            reader.Read();
            reader.Read();
            reader.CurrentDepth.Should().Be(1);
            reader.Skip();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            reader.CurrentDepth.Should().Be(1);
            reader.Read();
            reader.Skip();
            Encoding.UTF8.GetString(reader.LocalNameSpan).Should().Be("d");
            reader.TokenType.Should().Be(XmlTokenType.StartElement);
            reader.CurrentDepth.Should().Be(1);
        }

        [TestMethod]
        public void Skip_OnATokenThatIsNotAStartTag_DoesNothing()
        {
            // Only a start tag has children to skip. On any other token Skip leaves the reader where it is,
            // as Utf8JsonReader.Skip does on a value.
            var bytes = "<a>text<!--k--><?p q?><![CDATA[z]]><b>in</b></a>"u8.ToArray();
            var reader = new Utf8XmlReader(bytes, ValueOptions);
            reader.Skip();
            reader.TokenType.Should().Be(XmlTokenType.None);
            reader.BytesConsumed.Should().Be(0);
            reader.Read();
            foreach (var expected in new[] { XmlTokenType.Text, XmlTokenType.Comment, XmlTokenType.ProcessingInstruction, XmlTokenType.CData })
            {
                reader.Read();
                reader.TokenType.Should().Be(expected);
                var consumed = reader.BytesConsumed;
                var value = reader.GetString();
                reader.Skip();
                reader.TrySkip().Should().BeTrue();
                reader.TokenType.Should().Be(expected);
                reader.BytesConsumed.Should().Be(consumed);
                reader.GetString().Should().Be(value);
            }

            reader.Read();
            reader.Skip();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            Encoding.UTF8.GetString(reader.LocalNameSpan).Should().Be("b");
            var atEndTag = reader.BytesConsumed;
            reader.Skip();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            Encoding.UTF8.GetString(reader.LocalNameSpan).Should().Be("b");
            reader.BytesConsumed.Should().Be(atEndTag);
            reader.Read();
            reader.Skip();
            Encoding.UTF8.GetString(reader.LocalNameSpan).Should().Be("a");
            reader.BytesConsumed.Should().Be(bytes.Length);
            reader.Read().Should().BeFalse();
            reader.Skip();
            reader.TrySkip().Should().BeTrue();
        }

        [TestMethod]
        public void Skip_OnATokenThatIsNotAStartTag_InABlockThatIsNotFinal_DoesNothing()
        {
            var reader = new Utf8XmlReader("<a>text<b>"u8, false, new XmlReaderState(ValueOptions));
            reader.Read();
            reader.Read();
            reader.TokenType.Should().Be(XmlTokenType.Text);
            var consumed = reader.BytesConsumed;
            reader.TrySkip().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.Text);
            reader.BytesConsumed.Should().Be(consumed);
        }

        [TestMethod]
        public void ValueMembers_OnEndTagAfterSkip_AreEmpty()
        {
            var reader = new Utf8XmlReader("<r><a>text<b>deep</b></a><c/></r>"u8);
            reader.Read();
            reader.Read();
            reader.Skip();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            Encoding.UTF8.GetString(reader.LocalNameSpan).Should().Be("a");
            reader.GetString().Should().Be("");
            reader.ValueSpan.Length.Should().Be(0);
            reader.ValueIsEscaped.Should().BeFalse();
        }

        #endregion

        #region Private Methods

        private static void Drain(string xml, XmlReaderOptions options = default)
        {
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml), options);
            while (reader.Read())
            {
            }
        }

        private static void ShouldThrow(ref Utf8XmlReader reader)
        {
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetBoolean());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetByte());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetBytesFromBase64());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetComment());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetDateOnly());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetDateTime());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetDateTimeOffset());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetDecimal());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetDouble());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetGuid());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetInt16());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetInt32());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetInt64());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetSByte());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetSingle());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetTimeOnly());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetTimeSpan());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetUInt16());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetUInt32());
            ExpectXmlException(ref reader, static (ref Utf8XmlReader r) => r.GetUInt64());
        }

        private delegate void ReaderAction(ref Utf8XmlReader reader);

        private static void ExpectXmlException(ref Utf8XmlReader reader, ReaderAction action)
        {
            var threw = false;
            try
            {
                action(ref reader);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        private sealed class Segment : ReadOnlySequenceSegment<byte>
        {

            public Segment(ReadOnlyMemory<byte> memory, long runningIndex)
            {
                Memory = memory;
                RunningIndex = runningIndex;
            }

            public Segment Append(ReadOnlyMemory<byte> memory)
            {
                var next = new Segment(memory, RunningIndex + Memory.Length);
                Next = next;
                return next;
            }

        }

        /// <summary>
        /// Appends one line per token that holds the token's value as every value member reports it, so a
        /// value left over from an earlier token shows up on the token that has none.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="tokens">Receives the lines.</param>
        private static void AppendValues(ref Utf8XmlReader reader, List<string> tokens)
        {
            while (reader.Read())
            {
                var line = new StringBuilder();
                line.Append(reader.TokenType).Append(' ').Append(Encoding.UTF8.GetString(reader.LocalNameSpan));
                AppendValue(ref reader, line);
                if (reader.TokenType is XmlTokenType.StartElement && reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        line.Append(" @").Append(Encoding.UTF8.GetString(reader.LocalNameSpan));
                        AppendValue(ref reader, line);
                    }

                    reader.MoveToElement();
                    line.Append(" element");
                    AppendValue(ref reader, line);
                }

                tokens.Add(line.ToString());
            }
        }

        /// <summary>
        /// Reads to the end of the input and returns each token with its depth, attributes included.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The tokens, separated by blanks.</returns>
        private static string Depths(ref Utf8XmlReader reader)
        {
            var line = new StringBuilder();
            while (reader.Read())
            {
                line.Append(line.Length > 0 ? " " : "").Append(reader.TokenType);
                if (reader.TokenType is XmlTokenType.StartElement or XmlTokenType.EndElement)
                {
                    line.Append(' ').Append(Encoding.UTF8.GetString(reader.LocalNameSpan));
                }

                line.Append(':').Append(reader.CurrentDepth);
                while (reader.MoveToNextAttribute())
                {
                    line.Append(" @").Append(Encoding.UTF8.GetString(reader.LocalNameSpan)).Append(':').Append(reader.CurrentDepth);
                }

                reader.MoveToElement();
            }

            return line.ToString();
        }

        private static void AppendValue(ref Utf8XmlReader reader, StringBuilder line)
        {
            Span<char> copied = stackalloc char[64];
            var written = reader.CopyString(copied);
            line.Append(" [").Append(reader.GetString()).Append('|')
                .Append(Encoding.UTF8.GetString(reader.ValueSpan)).Append('|')
                .Append(copied[..written]).Append('|')
                .Append(reader.ValueIsEscaped ? "escaped" : "plain").Append('|')
                .Append(reader.ValueTextEquals(""u8) ? "empty" : "value").Append(']');
        }

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
