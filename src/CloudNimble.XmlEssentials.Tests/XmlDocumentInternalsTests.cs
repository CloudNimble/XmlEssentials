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
    /// Internals coverage for <see cref="XmlDocument"/>.
    /// </summary>
    [TestClass]
    public class XmlDocumentInternalsTests
    {

        #region Public Methods

        [TestMethod]
        public void AppendExtra_Grows()
        {
            using var document = XmlDocument.Parse("<e/>");
            var first = document.Locate(new byte[300]);
            var second = document.Locate(new byte[300]);
            first.Should().Be(4);
            second.Should().Be(304);
            document.Slice(second, 300).Length.Should().Be(300);
        }

        [TestMethod]
        public void Locate_PayloadSlice_IsNotCopied()
        {
            using var document = XmlDocument.Parse("<e a=\"1\">&amp;x</e>");
            var root = document.Row(0);
            root.NameStart.Should().Be(1);
            document.Row(1).ValueStart.Should().Be(6);
            document.Row(2).ValueStart.Should().BeGreaterThanOrEqualTo(18);
            document.RootElement.GetString().Should().Be("&x");
        }

        [TestMethod]
        public void LocateNamespace_RepeatedInternedUri_IsCopiedOnce()
        {
            var reader = new Utf8XmlReader("<r xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:a=\"1\" xsi:b=\"2\"><p:c xmlns:p=\"urn:c\"/></r>"u8);
            using var document = XmlDocument.ParseValue(ref reader);
            document.Row(2).NsStart.Should().Be(document.Row(3).NsStart);
            var root = document.RootElement;
            root.GetAttribute("a", XmlNamespaces.Xsi).GetInt32().Should().Be(1);
            root.GetAttribute("b", XmlNamespaces.Xsi).GetInt32().Should().Be(2);
            root.GetChild("c", "urn:c").NamespaceUri.Should().Be("urn:c");
        }

        [TestMethod]
        public void FormatDeclaration_Variants()
        {
            Format("1.0", true).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>");
            Format("1.0", false).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?>");
            Format("1.0", null).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            Format("1.1", true).Should().Be("<?xml version=\"1.1\" encoding=\"utf-8\" standalone=\"yes\"?>");
            Format("1.1", false).Should().Be("<?xml version=\"1.1\" encoding=\"utf-8\" standalone=\"no\"?>");
            Format("1.1", null).Should().Be("<?xml version=\"1.1\" encoding=\"utf-8\"?>");

            static string Format(string version, bool? standalone)
            {
                return Encoding.UTF8.GetString(XmlDocument.FormatDeclaration(new XmlDeclarationInfo(version, "utf-8", standalone)));
            }
        }

        [TestMethod]
        public void Parse_Sequence()
        {
            var seq = new ReadOnlySequence<byte>("<e/>"u8.ToArray());
            using var document = XmlDocument.Parse(seq);
            document.RootElement.IsEmptyElement.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_Version11()
        {
            using var document = XmlDocument.Parse("<?xml version=\"1.1\" encoding=\"utf-8\"?><e/>");
            document.Declaration!.Value.Version.Should().Be("1.1");
        }

        [TestMethod]
        public void GetAttribute_ByNamespace()
        {
            using var document = XmlDocument.Parse("<e xmlns:p=\"urn:x\" p:a=\"1\"/>");
            document.RootElement.GetAttribute("a", "urn:x").GetString().Should().Be("1");
        }

        [TestMethod]
        public void GetDouble_NamedLiterals()
        {
            using var nan = XmlDocument.Parse("<e>NaN</e>");
            double.IsNaN(nan.RootElement.GetDouble()).Should().BeTrue();
            using var inf = XmlDocument.Parse("<e>INF</e>");
            double.IsPositiveInfinity(inf.RootElement.GetDouble()).Should().BeTrue();
            using var ninf = XmlDocument.Parse("<e>-INF</e>");
            double.IsNegativeInfinity(ninf.RootElement.GetDouble()).Should().BeTrue();
        }

        [TestMethod]
        public void Dispose_IsIdempotent()
        {
            var document = XmlDocument.Parse("<e/>");
            document.Dispose();
            document.Dispose();
        }

        [TestMethod]
        public void Parse_StreamGrowsAndCaps()
        {
            var xml = "<e>" + new string('x', 20_000) + "</e>";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            using var document = XmlDocument.Parse(stream);
            document.RootElement.GetInnerText().Length.Should().Be(20_000);

            var threw = false;
            try
            {
                using var small = new MemoryStream(Encoding.UTF8.GetBytes(xml));
                XmlDocument.Parse(small, new XmlDocumentOptions { MaxDocumentByteCount = 100 });
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_MultipleRoots_Throws()
        {
            var threw = false;
            try
            {
                XmlDocument.Parse("<a/><b/>");
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void ParseValue_EmptyElement_Stops()
        {
            var reader = new Utf8XmlReader("<e/><x/>"u8);
            using var document = XmlDocument.ParseValue(ref reader);
            document.RootElement.LocalName.Should().Be("e");
        }

        [TestMethod]
        public void ParseValue_Malformed_Disposes()
        {
            var threw = false;
            try
            {
                var reader = new Utf8XmlReader("<e>"u8);
                XmlDocument.ParseValue(ref reader);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_TwoPrologComments()
        {
            using var document = XmlDocument.Parse("<!--a--><!--b--><e/>", new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Allow });
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            document.WriteTo(writer);
            writer.Flush();
            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<!--a--><!--b--><e/>");
        }

        [TestMethod]
        public void ExtraSpan_Empty()
        {
            using var document = XmlDocument.Parse("<e/>");
            document.Locate([]).Should().Be(0);
            document.LocateNamespace([]).Should().Be(0);
            document.Slice(0, 0).IsEmpty.Should().BeTrue();
        }

        [TestMethod]
        public void AddRow_Grows()
        {
            var xml = new StringBuilder("<r>");
            for (var i = 0; i < 40; i++)
            {
                xml.Append("<e/>");
            }

            xml.Append("</r>");
            using var document = XmlDocument.Parse(xml.ToString());
            document.RootElement.EnumerateChildren().MoveNext().Should().BeTrue();
        }

        [TestMethod]
        public void Getters_ErrorPaths()
        {
            using var document = XmlDocument.Parse("<e a=\"x\">!!!!</e>");
            document.RootElement.AttributeCount.Should().Be(1);
            var text = default(XmlElement);
            foreach (var child in document.RootElement.EnumerateChildren())
            {
                text = child;
            }

            text.AttributeCount.Should().Be(0);
            var threw = false;
            try
            {
                document.RootElement.GetAttribute("z");
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
            threw = false;
            try
            {
                document.RootElement.GetAttribute("a", "urn:no");
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
            threw = false;
            try
            {
                document.RootElement.GetChild("z", "urn:x");
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
            threw = false;
            try
            {
                document.RootElement.GetBoolean();
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
            threw = false;
            try
            {
                document.RootElement.GetBytesFromBase64();
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
            threw = false;
            try
            {
                document.RootElement.GetDateTime();
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
            threw = false;
            try
            {
                document.RootElement.GetDateTimeOffset();
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
            threw = false;
            try
            {
                document.RootElement.GetDecimal();
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
            threw = false;
            try
            {
                document.RootElement.GetDouble();
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
            threw = false;
            try
            {
                document.RootElement.GetGuid();
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
            threw = false;
            try
            {
                document.RootElement.GetInt64();
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void WriteTo_NullWriter_Throws()
        {
            using var document = XmlDocument.Parse("<e/>");
            var threw = false;
            try
            {
                document.WriteTo(null!);
            }
            catch (ArgumentNullException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Stream_ExactBufferCap_Throws()
        {
            var payload = "<e>" + new string('x', XmlLimits.DefaultBufferSize) + "</e>";
            var threw = false;
            try
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
                XmlDocument.Parse(stream, new XmlDocumentOptions { MaxDocumentByteCount = XmlLimits.DefaultBufferSize });
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Attribute_GetRawXml_AndBooleanDigits()
        {
            using var document = XmlDocument.Parse("<e a=\"1\">0</e>");
            document.RootElement.GetAttribute("a").GetRawXml();
            document.RootElement.GetAttribute("a").TokenType.Should().Be(XmlTokenType.Attribute);
            document.RootElement.GetBoolean().Should().BeFalse();
            using var one = XmlDocument.Parse("<e>1</e>");
            one.RootElement.GetBoolean().Should().BeTrue();
            using var b64 = XmlDocument.Parse("<e>AQID</e>");
            b64.RootElement.GetBytesFromBase64().Should().Equal(1, 2, 3);
            using var inf = XmlDocument.Parse("<e>inf</e>");
            double.IsPositiveInfinity(inf.RootElement.GetDouble()).Should().BeTrue();
            using var ninf = XmlDocument.Parse("<e>-inf</e>");
            double.IsNegativeInfinity(ninf.RootElement.GetDouble()).Should().BeTrue();
            using var nan = XmlDocument.Parse("<e>nan</e>");
            double.IsNaN(nan.RootElement.GetDouble()).Should().BeTrue();
            foreach (var child in document.RootElement.EnumerateChildren())
            {
                child.TokenType.Should().Be(XmlTokenType.Text);
                child.CloneNode().Should().BeOfType<XmlTextNode>();
            }
        }

        [TestMethod]
        public void Clone_CommentCDataPi()
        {
            using var document = XmlDocument.Parse("<e><![CDATA[x]]><!--c--><?pi d?></e>", new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Allow });
            document.RootElement.GetRawXml().Should().Be("<e><![CDATA[x]]><!--c--><?pi d?></e>");
            foreach (var child in document.RootElement.EnumerateChildren())
            {
                child.CloneNode();
                _ = child.TokenType;
            }
        }

        [TestMethod]
        public void Equals_SameRow()
        {
            using var document = XmlDocument.Parse("<e/>");
            var a = document.RootElement;
            var b = document.RootElement;
            a.Equals(b).Should().BeTrue();
            (a == b).Should().BeTrue();
            (a != default).Should().BeTrue();
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [TestMethod]
        public void EmptyDocument_RootElement_Throws()
        {
            using var document = new XmlDocument();
            var threw = false;
            try
            {
                _ = document.RootElement;
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void FillFromReader_NoRoot_Throws()
        {
            using var document = new XmlDocument();
            var reader = new Utf8XmlReader(""u8, new XmlReaderOptions { AllowMultipleRoots = true });
            var threw = false;
            try
            {
                document.FillFromReader(ref reader, requireDocumentRoot: true);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void FillFromReader_MultipleRoots_Throws()
        {
            using var document = new XmlDocument();
            var reader = new Utf8XmlReader("<a/><b/>"u8, new XmlReaderOptions { AllowMultipleRoots = true });
            var threw = false;
            try
            {
                document.FillFromReader(ref reader, requireDocumentRoot: true);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void FillFromReader_EndElementAtDepthZero_Throws()
        {
            using var document = new XmlDocument();
            var reader = new Utf8XmlReader("<e></e>"u8);
            reader.Read();
            reader.Read();
            var threw = false;
            try
            {
                document.FillFromReader(ref reader, requireDocumentRoot: true);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void TokenType_UnknownKind_IsNone()
        {
            using var document = XmlDocument.Parse("<e/>");
            var index = document.AddRow(new XmlDbRow { Kind = XmlValueKind.Document });
            new XmlElement(document, index).TokenType.Should().Be(XmlTokenType.None);
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            document.WriteTo(writer);
            writer.Flush();
            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<e/>");
        }

        [TestMethod]
        public void Parse_DeepNesting_GrowsStack()
        {
            var depth = XmlLimits.DefaultMaxDepth + 4;
            var xml = new StringBuilder();
            for (var i = 0; i < depth; i++)
            {
                xml.Append("<e>");
            }

            for (var i = 0; i < depth; i++)
            {
                xml.Append("</e>");
            }

            using var document = XmlDocument.Parse(xml.ToString(), new XmlDocumentOptions { MaxDepth = depth + 1 });
            document.RootElement.LocalName.Should().Be("e");
        }

        [TestMethod]
        public void Stream_ExactCap_Succeeds()
        {
            var inner = XmlLimits.DefaultBufferSize - 7;
            var xml = "<e>" + new string('x', inner) + "</e>";
            xml.Length.Should().Be(XmlLimits.DefaultBufferSize);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            using var document = XmlDocument.Parse(stream, new XmlDocumentOptions { MaxDocumentByteCount = XmlLimits.DefaultBufferSize });
            document.RootElement.GetInnerText().Length.Should().Be(inner);
        }

        #endregion

    }

}
