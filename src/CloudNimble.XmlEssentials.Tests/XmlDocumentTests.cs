using System;
using System.Buffers;
using System.IO;
using System.Text;
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
        public void GetDateTime_Guid_Decimal_Int64()
        {
            using var dt = XmlDocument.Parse("<e>2020-01-02T03:04:05Z</e>");
            dt.RootElement.GetDateTime().Year.Should().Be(2020);
            dt.RootElement.GetDateTimeOffset().Year.Should().Be(2020);
            using var g = XmlDocument.Parse("<e>00112233-4455-6677-8899-aabbccddeeff</e>");
            g.RootElement.GetGuid().Should().NotBe(Guid.Empty);
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
