using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Well-formed parse, cursor, and WriteTo tests for <see cref="XmlDocument"/>.
    /// </summary>
    [TestClass]
    public class XmlDocumentTests
    {

        #region Public Methods

        [TestMethod]
        public void Parse_EmptyElement_HasNoChildren()
        {
            using var document = XmlDocument.Parse("<e/>");
            document.RootElement.LocalName.Should().Be("e");
            document.RootElement.IsEmptyElement.Should().BeTrue();
            document.RootElement.EnumerateChildren().MoveNext().Should().BeFalse();
        }

        [TestMethod]
        public void Parse_AttributeAndText()
        {
            using var document = XmlDocument.Parse("<e a=\"1\">42</e>");
            document.RootElement.GetAttribute("a").GetInt32().Should().Be(1);
            document.RootElement.GetInt32().Should().Be(42);
            document.RootElement.GetString().Should().Be("42");
        }

        [TestMethod]
        public void Parse_ChildElements()
        {
            using var document = XmlDocument.Parse("<r><a>1</a><b>2</b></r>");
            document.RootElement.GetChild("a").GetInt32().Should().Be(1);
            document.RootElement.GetChild("b").GetString().Should().Be("2");
            document.RootElement.TryGetChild("c", out _).Should().BeFalse();
        }

        [TestMethod]
        public void Parse_NamespacedChild()
        {
            using var document = XmlDocument.Parse("<r xmlns:p=\"urn:x\"><p:a>1</p:a></r>");
            document.RootElement.GetChild("a", "urn:x").GetInt32().Should().Be(1);
        }

        [TestMethod]
        public void Parse_Nil()
        {
            using var document = XmlDocument.Parse("<e xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>");
            document.RootElement.IsNil.Should().BeTrue();
            document.RootElement.GetString().Should().BeNull();
        }

        [TestMethod]
        public void TokenType_ForEveryKindOfNode_IsTheReaderTokenItCameFrom()
        {
            const string xsi = "http://www.w3.org/2001/XMLSchema-instance";
            using var document = XmlDocument.Parse(
                "<r a=\"1\"><full>t</full><empty/><nil xmlns:xsi=\"" + xsi + "\" xsi:nil=\"true\"/>text<![CDATA[c]]><!--m--><?p d?></r>",
                new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Allow });
            var kinds = new List<string> { "r:" + document.RootElement.TokenType, "a:" + document.RootElement.GetAttribute("a").TokenType };
            foreach (var child in document.RootElement.EnumerateChildren())
            {
                kinds.Add(child.TokenType.ToString());
            }

            kinds.Should().Equal("r:StartElement", "a:Attribute", "StartElement", "StartElement", "StartElement", "Text", "CData", "Comment", "ProcessingInstruction");
            document.RootElement.GetRawXml().Should().Be("<r a=\"1\"><full>t</full><empty/><nil xmlns:xsi=\"" + xsi + "\" xsi:nil=\"true\"/>text<![CDATA[c]]><!--m--><?p d?></r>");
        }

        [TestMethod]
        [DataRow("<?xml version=\"1.0\" encoding=\"utf-8\"?>", DisplayName = "1.0")]
        [DataRow("<?xml version=\"1.1\" encoding=\"utf-8\"?>", DisplayName = "1.1")]
        [DataRow("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>", DisplayName = "1.0 standalone")]
        [DataRow("<?xml version=\"1.1\" encoding=\"utf-8\" standalone=\"yes\"?>", DisplayName = "1.1 standalone")]
        [DataRow("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?>", DisplayName = "1.0 not standalone")]
        [DataRow("<?xml version=\"1.1\" encoding=\"utf-8\" standalone=\"no\"?>", DisplayName = "1.1 not standalone")]
        public void WriteTo_ADocumentWithADeclaration_WritesTheDeclarationItWasParsedWith(string declaration)
        {
            using var document = XmlDocument.Parse(declaration + "<e/>");
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer))
            {
                document.WriteTo(writer);
            }

            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be(declaration + "<e/>");
        }

        [TestMethod]
        public void WriteTo_ADeclarationWithoutAnEncoding_NamesTheEncodingItIsWrittenIn()
        {
            using var document = XmlDocument.Parse("<?xml version='1.0'?><e/>");
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer))
            {
                document.WriteTo(writer);
            }

            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?><e/>");
        }

        [TestMethod]
        public void Parse_Declaration()
        {
            using var document = XmlDocument.Parse("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><e/>");
            document.Declaration.Should().NotBeNull();
            document.Declaration!.Value.Version.Should().Be("1.0");
            document.Declaration.Value.Standalone.Should().BeTrue();
            document.RootElement.Prefix.Should().BeEmpty();
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            document.WriteTo(writer);
            writer.Flush();
            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><e/>");

            var raw = new ArrayBufferWriter<byte>();
            using var defaultWriter = new Utf8XmlWriter(raw);
            document.WriteTo(defaultWriter);
            defaultWriter.Flush();
            Encoding.UTF8.GetString(raw.WrittenSpan).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><e/>");
        }

        [TestMethod]
        public void Parse_PrologComment_NotChildOfRoot()
        {
            using var document = XmlDocument.Parse("<!--hi--><e/>", new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Allow });
            document.RootElement.EnumerateChildren().MoveNext().Should().BeFalse();
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            document.WriteTo(writer);
            writer.Flush();
            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<!--hi--><e/>");
        }

        [TestMethod]
        public void Parse_CDataAndComment()
        {
            using var document = XmlDocument.Parse("<e><![CDATA[x]]><!--c--></e>", new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Allow });
            var n = 0;
            foreach (var child in document.RootElement.EnumerateChildren())
            {
                n++;
                if (child.ValueKind is XmlValueKind.CData)
                {
                    child.GetString().Should().Be("x");
                }
            }

            n.Should().Be(2);
        }

        [TestMethod]
        public void Parse_Utf8AndStream()
        {
            using var fromUtf8 = XmlDocument.Parse("<e>1</e>"u8);
            fromUtf8.RootElement.GetInt32().Should().Be(1);
            using var stream = new MemoryStream("<e>2</e>"u8.ToArray());
            using var fromStream = XmlDocument.Parse(stream);
            fromStream.RootElement.GetInt32().Should().Be(2);
        }

        [TestMethod]
        public async Task ParseAsync_AStreamThatArrivesInPieces_IsParsedAsParseParsesIt()
        {
            var xml = "<?xml version=\"1.0\"?><r a=\"1\"><!--c--><e>2</e></r>"u8.ToArray();
            using var expected = XmlDocument.Parse(new MemoryStream(xml));
            expected.RootElement.GetRawXml().Should().Be("<r a=\"1\"><!--c--><e>2</e></r>");

            var stream = new TrickleStream(xml, 3);
            using var document = await XmlDocument.ParseAsync(stream);
            document.RootElement.GetRawXml().Should().Be("<r a=\"1\"><!--c--><e>2</e></r>");
            document.Declaration!.Value.Version.Should().Be("1.0");
            stream.BytesRead.Should().Be(xml.Length);

            using var skipped = await XmlDocument.ParseAsync(new TrickleStream(xml, 3), new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Skip });
            skipped.RootElement.GetRawXml().Should().Be("<r a=\"1\"><e>2</e></r>");
        }

        [TestMethod]
        public async Task ParseAsync_ACanceledToken_CancelsTheTask()
        {
            using var source = new CancellationTokenSource();
            source.Cancel();
            using var stream = new MemoryStream("<e/>"u8.ToArray());
            await FluentActions.Awaiting(() => XmlDocument.ParseAsync(stream, default, source.Token)).Should().ThrowExactlyAsync<TaskCanceledException>();
        }

        [TestMethod]
        public async Task ParseAsync_ADocumentThatIsNotWellFormedOrTooLarge_FailsAsParseFails()
        {
            var malformed = "<e><f></e>"u8.ToArray();
            var syncMessage = FluentActions.Invoking(() => XmlDocument.Parse(new MemoryStream(malformed))).Should().ThrowExactly<XmlException>().Which.Message;
            syncMessage.Should().Be("End tag does not match start tag.");
            (await FluentActions.Awaiting(() => XmlDocument.ParseAsync(new TrickleStream(malformed, 2))).Should().ThrowExactlyAsync<XmlException>()).Which.Message.Should().Be("End tag does not match start tag.");

            var large = "<e>0123456789</e>"u8.ToArray();
            var limit = new XmlDocumentOptions { MaxDocumentByteCount = 8 };
            FluentActions.Invoking(() => XmlDocument.Parse(new MemoryStream(large), limit)).Should().ThrowExactly<XmlException>().Which.Message.Should().Be("MaxDocumentByteCount exceeded (8).");
            (await FluentActions.Awaiting(() => XmlDocument.ParseAsync(new TrickleStream(large, 2), limit)).Should().ThrowExactlyAsync<XmlException>()).Which.Message.Should().Be("MaxDocumentByteCount exceeded (8).");
        }

        [TestMethod]
        public void ParseAsync_ANullStream_IsRefusedBeforeATaskIsCreated()
        {
            FluentActions.Invoking(() => { _ = XmlDocument.ParseAsync(null!); }).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("utf8Xml");
        }

        [TestMethod]
        public void TryParse_Malformed_ReturnsFalse()
        {
            XmlDocument.TryParse("<e>"u8, default, out var document).Should().BeFalse();
            document.Should().BeNull();
        }

        [TestMethod]
        public void TryParse_WellFormed_ReturnsTrue()
        {
            XmlDocument.TryParse("<e a=\"1\"/>"u8, default, out var document).Should().BeTrue();
            using (document)
            {
                document!.RootElement.LocalName.Should().Be("e");
                document.RootElement.GetAttribute("a").GetInnerText().Should().Be("1");
            }
        }

        [TestMethod]
        public void ParseValue_ReadsCurrentElement()
        {
            var reader = new Utf8XmlReader("<r><e>1</e></r>"u8);
            reader.Read();
            reader.Read();
            using var document = XmlDocument.ParseValue(ref reader);
            document.RootElement.LocalName.Should().Be("e");
            document.RootElement.GetInt32().Should().Be(1);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void TryParseValue_AnElementThatIsAllThere_IsParsedAsParseValueParsesIt(bool isFinalBlock)
        {
            var reader = new Utf8XmlReader("<r><!--c--><e a=\"1\">x<f/></e><g/></r>"u8, isFinalBlock, default);
            reader.Read();
            reader.Read();
            reader.TokenType.Should().Be(XmlTokenType.Comment);

            XmlDocument.TryParseValue(ref reader, out var document).Should().BeTrue("the reader moves on to the element that follows the comment");
            using (document)
            {
                document!.RootElement.GetRawXml().Should().Be("<e a=\"1\">x<f/></e>");
            }

            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            reader.LocalNameEquals("e").Should().BeTrue();

            reader.Read();
            XmlDocument.TryParseValue(ref reader, out var empty).Should().BeTrue();
            using (empty)
            {
                empty!.RootElement.GetRawXml().Should().Be("<g/>");
            }

            reader.TokenType.Should().Be(XmlTokenType.StartElement, "an empty element has no end tag to stop on");
            reader.LocalNameEquals("g").Should().BeTrue();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void TryParseValue_FromAnAttribute_ParsesTheElementThatOwnsIt(bool isFinalBlock)
        {
            var reader = new Utf8XmlReader("<r><e a=\"1\">x</e></r>"u8, isFinalBlock, default);
            reader.Read();
            reader.Read();
            reader.MoveToNextAttribute().Should().BeTrue();

            XmlDocument.TryParseValue(ref reader, out var document).Should().BeTrue();
            using (document)
            {
                document!.RootElement.GetRawXml().Should().Be("<e a=\"1\">x</e>");
            }
        }

        [TestMethod]
        public void TryParseValue_AnElementThatIsNotAllThere_ReturnsFalseAndLeavesTheReaderWhereItWas()
        {
            var reader = new Utf8XmlReader("<r><e a=\"1\" b=\"&lt;\">x<f>"u8, isFinalBlock: false, default);
            reader.Read();
            reader.Read();
            var consumed = reader.BytesConsumed;

            XmlDocument.TryParseValue(ref reader, out var document).Should().BeFalse();
            document.Should().BeNull();
            reader.TokenType.Should().Be(XmlTokenType.StartElement);
            reader.LocalNameEquals("e").Should().BeTrue();
            reader.BytesConsumed.Should().Be(consumed);

            // The reader goes on from the start tag as if nothing had been tried.
            reader.MoveToNextAttribute().Should().BeTrue();
            reader.GetString().Should().Be("1");
            reader.MoveToNextAttribute().Should().BeTrue();
            reader.GetString().Should().Be("<");
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.Text);
            reader.GetString().Should().Be("x");
        }

        [TestMethod]
        public void TryParseValue_AnElementWithManyAttributesThatIsNotAllThere_KeepsTheAttributesOfTheStartTag()
        {
            // Nine attributes are more than the reader keeps inline, and each value needs decoding. The child
            // that is read ahead has nine of its own.
            var names = new[] { "a", "b", "c", "d", "e", "f", "g", "h", "i" };
            var own = string.Concat(names.Select(n => " " + n + "=\"&lt;" + n + "\""));
            var inner = string.Concat(names.Select(n => " " + n + "=\"&gt;" + n + n + "\""));
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes("<r><e" + own + "><f" + inner + ">x</f>"), isFinalBlock: false, default);
            reader.Read();
            reader.Read();

            XmlDocument.TryParseValue(ref reader, out var document).Should().BeFalse();
            document.Should().BeNull();
            foreach (var name in names)
            {
                reader.MoveToNextAttribute().Should().BeTrue();
                reader.LocalNameEquals(name).Should().BeTrue();
                reader.GetString().Should().Be("<" + name);
            }

            reader.MoveToNextAttribute().Should().BeFalse();
        }

        [TestMethod]
        public void TryParseValue_BeforeAnElementThatHasNotArrived_ReturnsFalse()
        {
            var reader = new Utf8XmlReader("<r><!--c-->"u8, isFinalBlock: false, default);
            reader.Read();
            reader.Read();
            var consumed = reader.BytesConsumed;

            XmlDocument.TryParseValue(ref reader, out var document).Should().BeFalse();
            document.Should().BeNull();
            reader.TokenType.Should().Be(XmlTokenType.Comment);
            reader.BytesConsumed.Should().Be(consumed);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void TryParseValue_OnAnEndTag_ReturnsFalse(bool isFinalBlock)
        {
            var reader = new Utf8XmlReader("<r><e/></r>"u8, isFinalBlock, default);
            reader.Read();
            reader.Read();
            reader.Read();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            var consumed = reader.BytesConsumed;

            XmlDocument.TryParseValue(ref reader, out var document).Should().BeFalse();
            document.Should().BeNull();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            reader.BytesConsumed.Should().Be(consumed);
        }

        [TestMethod]
        [DataRow("<r>t</r>", 2, true, "End tag without a matching start tag.")]
        [DataRow("<r>t</r>", 2, false, "End tag without a matching start tag.")]
        [DataRow("<r><e>x</f></r>", 1, true, "End tag does not match start tag.")]
        [DataRow("<r><e>x</f></r>", 1, false, "End tag does not match start tag.")]
        [DataRow("<r><e>x", 1, true, "Unexpected end of XML.")]
        [DataRow("<r/><!--c-->", 2, true, "The reader is not on an element and has none left to read.")]
        public void TryParseValue_XmlThatParseValueRefuses_ThrowsAndLeavesTheReaderWhereItWas(string xml, int reads, bool isFinalBlock, string message)
        {
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml), isFinalBlock, default);
            for (var i = 0; i < reads; i++)
            {
                reader.Read();
            }

            var token = reader.TokenType;
            var consumed = reader.BytesConsumed;
            string? thrown = null;
            try
            {
                XmlDocument.TryParseValue(ref reader, out _);
            }
            catch (XmlException ex)
            {
                thrown = ex.Message;
            }

            thrown.Should().Be(message);
            reader.TokenType.Should().Be(token);
            reader.BytesConsumed.Should().Be(consumed);
        }

        [TestMethod]
        public void WriteTo_RoundTripsElement()
        {
            using var document = XmlDocument.Parse("<e a=\"1\"><n>x</n></e>");
            document.RootElement.GetRawXml().Should().Be("<e a=\"1\"><n>x</n></e>");
        }

        [TestMethod]
        public void Dispose_ThenRoot_Throws()
        {
            var document = XmlDocument.Parse("<e/>");
            document.Dispose();
            var threw = false;
            try
            {
                _ = document.RootElement;
            }
            catch (ObjectDisposedException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void CloneNode_ProducesMutableTree()
        {
            using var document = XmlDocument.Parse("<e a=\"1\">x</e>");
            var node = (XmlElementNode)document.RootElement.CloneNode();
            node.LocalName.Should().Be("e");
            node.Attributes["a"]!.Value.Should().Be("1");
            node.InnerText.Should().Be("x");
            node.SetAttribute("b", "2");
            node.AddElement("c").InnerText = "3";
            node.ToString().Should().Be("<e a=\"1\" b=\"2\">x<c>3</c></e>");
        }

        [TestMethod]
        public void XmlNode_Parse_AllowsMutation()
        {
            var root = (XmlElementNode)XmlNode.Parse("<e/>")!;
            root.Add(new XmlTextNode("hi"));
            root.InnerText.Should().Be("hi");
            root.DeepClone().ToString().Should().Be("<e>hi</e>");
        }

        [TestMethod]
        public void GetBoolean_AndBase64()
        {
            using var document = XmlDocument.Parse("<e>true</e>");
            document.RootElement.GetBoolean().Should().BeTrue();
            using var b64 = XmlDocument.Parse("<e>YQ==</e>");
            b64.RootElement.GetBytesFromBase64().Should().Equal((byte)'a');
        }

        [TestMethod]
        public void Parse_ProcessingInstruction()
        {
            using var document = XmlDocument.Parse("<?pi data?><e/>");
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            document.WriteTo(writer);
            writer.Flush();
            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<?pi data?><e/>");
        }

        [TestMethod]
        public void Parse_EpilogComment()
        {
            using var document = XmlDocument.Parse("<e/><!--tail-->", new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Allow });
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            document.WriteTo(writer);
            writer.Flush();
            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<e/><!--tail-->");
        }

        [TestMethod]
        public void Parse_WithoutOptions_KeepsCommentsSoTheDocumentRoundTrips()
        {
            const string xml = "<!--top--><e a=\"1\"><!--in-->x<b/><!--last--></e><!--tail-->";
            using var document = XmlDocument.Parse(xml);
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            document.WriteTo(writer);
            writer.Flush();
            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be(xml);
        }

        [TestMethod]
        public void Options_DefaultAndNew_BothKeepComments()
        {
            default(XmlDocumentOptions).CommentHandling.Should().Be(XmlCommentHandling.Allow);
            new XmlDocumentOptions().CommentHandling.Should().Be(XmlCommentHandling.Allow);
            new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Disallow }.CommentHandling.Should().Be(XmlCommentHandling.Disallow);
        }

        [TestMethod]
        public void Parse_CommentHandlingSkip_LeavesCommentsOutOfTheDocument()
        {
            using var document = XmlDocument.Parse(
                "<!--top--><e a=\"1\"><!--in-->x<b/><!--last--></e><!--tail-->",
                new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Skip });
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            document.WriteTo(writer);
            writer.Flush();
            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<e a=\"1\">x<b/></e>");
        }

        [TestMethod]
        public void GetDateTime_Guid_Decimal_Int64()
        {
            using var dt = XmlDocument.Parse("<e>2020-01-02T03:04:05Z</e>");
            dt.RootElement.GetDateTime().Should().Be(new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            dt.RootElement.GetDateTime().Kind.Should().Be(DateTimeKind.Utc);
            dt.RootElement.GetDateTimeOffset().Should().Be(new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero));
            using var g = XmlDocument.Parse("<e>00112233-4455-6677-8899-aabbccddeeff</e>");
            g.RootElement.GetGuid().Should().Be(Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"));
            using var d = XmlDocument.Parse("<e>1.5</e>");
            d.RootElement.GetDecimal().Should().Be(1.5m);
            using var l = XmlDocument.Parse("<e>99</e>");
            l.RootElement.GetInt64().Should().Be(99);
            using var fl = XmlDocument.Parse("<e>false</e>");
            fl.RootElement.GetBoolean().Should().BeFalse();
        }

        [TestMethod]
        public void PrefixedWriteTo()
        {
            using var document = XmlDocument.Parse("<p:e xmlns:p=\"urn:x\" p:a=\"1\"/>");
            document.RootElement.Prefix.Should().Be("p");
            document.RootElement.GetRawXml().Should().Be("<p:e xmlns:p=\"urn:x\" p:a=\"1\"/>");
        }

        [TestMethod]
        public void DefaultNamespace_WriteTo()
        {
            using var document = XmlDocument.Parse("<e xmlns=\"urn:x\"/>");
            document.RootElement.NamespaceUri.Should().Be("urn:x");
            document.RootElement.GetRawXml().Should().Be("<e xmlns=\"urn:x\"/>");
        }

        [TestMethod]
        public void GetDouble_Numeric()
        {
            using var document = XmlDocument.Parse("<e>3.5</e>");
            document.RootElement.GetDouble().Should().Be(3.5d);
        }

        [TestMethod]
        public void GetChild_AndGetAttribute_TakeTheNameAsCharactersOrAsUtf8()
        {
            using var document = XmlDocument.Parse("<r xmlns:p=\"urn:p\" a=\"1\" p:b=\"2\" \u00E9=\"3\"><x>4</x><p:y>5</p:y><\u00E9>6</\u00E9>t</r>");
            var root = document.RootElement;

            root.GetAttribute("a".AsSpan()).GetInt32().Should().Be(1);
            root.GetAttribute("b".AsSpan()).GetInt32().Should().Be(2, "the name is looked for in any namespace");
            root.GetAttribute("\u00E9".AsSpan()).GetInt32().Should().Be(3);
            root.GetAttribute("a"u8).GetInt32().Should().Be(1);
            root.GetAttribute("b"u8).GetInt32().Should().Be(2);
            root.GetAttribute("\u00E9"u8).GetInt32().Should().Be(3);
            root.GetChild("x".AsSpan()).GetInt32().Should().Be(4);
            root.GetChild("y".AsSpan()).GetInt32().Should().Be(5);
            root.GetChild("\u00E9".AsSpan()).GetInt32().Should().Be(6);
            root.GetChild("x"u8).GetInt32().Should().Be(4);
            root.GetChild("y"u8).GetInt32().Should().Be(5);
            root.GetChild("\u00E9"u8).GetInt32().Should().Be(6);

            root.TryGetAttribute("b".AsSpan(), out var attribute).Should().BeTrue();
            attribute.Should().Be(root.GetAttribute("b"));
            root.TryGetAttribute("b"u8, out attribute).Should().BeTrue();
            attribute.Should().Be(root.GetAttribute("b"));
            root.TryGetChild("y".AsSpan(), out var child).Should().BeTrue();
            child.Should().Be(root.GetChild("y"));
            root.TryGetChild("y"u8, out child).Should().BeTrue();
            child.Should().Be(root.GetChild("y"));
        }

        [TestMethod]
        public void GetChild_AndGetAttribute_WithANameThatIsNotThere_ThrowWhereTheTryMethodsReturnFalse()
        {
            using var document = XmlDocument.Parse("<r a=\"1\"><x>4</x>t</r>");
            var root = document.RootElement;

            root.TryGetAttribute("x".AsSpan(), out var none).Should().BeFalse("x is a child, not an attribute");
            none.Should().Be(default(XmlElement));
            root.TryGetAttribute("x"u8, out none).Should().BeFalse();
            none.Should().Be(default(XmlElement));
            root.TryGetChild("a".AsSpan(), out none).Should().BeFalse("a is an attribute, not a child");
            none.Should().Be(default(XmlElement));
            root.TryGetChild("a"u8, out none).Should().BeFalse();
            none.Should().Be(default(XmlElement));

            FluentActions.Invoking(() => root.GetAttribute("\u00E9".AsSpan())).Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Attribute '\u00E9' was not found.");
            FluentActions.Invoking(() => root.GetAttribute("\u00E9"u8)).Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Attribute '\u00E9' was not found.");
            FluentActions.Invoking(() => root.GetChild("\u00E9".AsSpan())).Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Child element '\u00E9' was not found.");
            FluentActions.Invoking(() => root.GetChild("\u00E9"u8)).Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Child element '\u00E9' was not found.");

            // An attribute has no attributes and no children, so nothing is found on one.
            var a = root.GetAttribute("a");
            a.TryGetAttribute("a".AsSpan(), out _).Should().BeFalse();
            a.TryGetAttribute("a"u8, out _).Should().BeFalse();
            a.TryGetChild("x".AsSpan(), out _).Should().BeFalse();
            a.TryGetChild("x"u8, out _).Should().BeFalse();

            FluentActions.Invoking(() => default(XmlElement).TryGetAttribute("a"u8, out _)).Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be("The XML element is empty.");
            FluentActions.Invoking(() => default(XmlElement).TryGetChild("x"u8, out _)).Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be("The XML element is empty.");
        }

        [TestMethod]
        public void TryGetChild_WithNamespace()
        {
            using var document = XmlDocument.Parse("<r xmlns=\"urn:x\"><a/></r>");
            document.RootElement.TryGetChild("a", "urn:x", out var child).Should().BeTrue();
            child.IsEmptyElement.Should().BeTrue();
            document.RootElement.TryGetChild("a", "urn:no", out _).Should().BeFalse();
        }

        [TestMethod]
        public void TokenType_AndEqualsObject()
        {
            using var document = XmlDocument.Parse("<e a=\"1\">x</e>");
            document.RootElement.TokenType.Should().Be(XmlTokenType.StartElement);
            document.RootElement.Equals((object)document.RootElement).Should().BeTrue();
            document.RootElement.Equals("no").Should().BeFalse();
            default(XmlElement).ValueKind.Should().Be(XmlValueKind.Undefined);
        }

        [TestMethod]
        public void EnumerateAttributes()
        {
            using var document = XmlDocument.Parse("<e a=\"1\" b=\"2\"/>");
            var n = 0;
            foreach (var attr in document.RootElement.EnumerateAttributes())
            {
                n++;
                attr.ValueKind.Should().Be(XmlValueKind.Attribute);
            }

            n.Should().Be(2);
        }

        #endregion

    }

}
