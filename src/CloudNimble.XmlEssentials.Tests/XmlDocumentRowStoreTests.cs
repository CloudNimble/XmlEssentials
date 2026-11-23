using System;
using System.Buffers;
using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Document-order row store, name lookup, value, and write fast-path tests for <see cref="XmlDocument"/>.
    /// </summary>
    [TestClass]
    public class XmlDocumentRowStoreTests
    {

        #region Fields

        private const string XsiDeclaration = "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"";

        #endregion

        #region Public Methods

        [TestMethod]
        public void Children_SkipAttributesAndDescendants()
        {
            using var document = XmlDocument.Parse("<r a=\"1\"><x b=\"2\"><deep><deeper/></deep>t</x>mid<y/><z c=\"3\">9</z></r>");
            var names = new StringBuilder();
            foreach (var child in document.RootElement.EnumerateChildren())
            {
                names.Append(child.ValueKind is XmlValueKind.Text ? "#text" : child.LocalName).Append(',');
            }

            names.ToString().Should().Be("x,#text,y,z,");
            document.RootElement.GetChild("z").GetAttribute("c").GetInt32().Should().Be(3);
            document.RootElement.GetChild("z").GetInt32().Should().Be(9);
            document.RootElement.TryGetChild("deep", out _).Should().BeFalse();
            document.RootElement.GetChild("x").GetChild("deep").GetChild("deeper").IsEmptyElement.Should().BeTrue();
        }

        [TestMethod]
        public void Enumerators_NonElementCursors_AreEmpty()
        {
            using var document = XmlDocument.Parse("<e a=\"1\">t</e>");
            var attribute = document.RootElement.GetAttribute("a");
            attribute.EnumerateChildren().MoveNext().Should().BeFalse();
            attribute.EnumerateAttributes().MoveNext().Should().BeFalse();
            attribute.TryGetChild("x", out _).Should().BeFalse();
            attribute.TryGetAttribute("x", out _).Should().BeFalse();
            default(XmlElement.Enumerator).MoveNext().Should().BeFalse();
            default(XmlElement.AttributeEnumerator).MoveNext().Should().BeFalse();
        }

        [TestMethod]
        public void Enumerators_AfterDispose_Throw()
        {
            var document = XmlDocument.Parse("<e a=\"1\"><c/></e>");
            var children = document.RootElement.EnumerateChildren();
            var attributes = document.RootElement.EnumerateAttributes();
            document.Dispose();
            var childThrew = false;
            try
            {
                children.MoveNext();
            }
            catch (ObjectDisposedException)
            {
                childThrew = true;
            }

            var attributeThrew = false;
            try
            {
                attributes.MoveNext();
            }
            catch (ObjectDisposedException)
            {
                attributeThrew = true;
            }

            childThrew.Should().BeTrue();
            attributeThrew.Should().BeTrue();
        }

        [TestMethod]
        public void GetInnerText_CommentAndProcessingInstruction_AreEmpty()
        {
            using var document = XmlDocument.Parse("<e><!--c--><?pi d?></e>", new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Allow });
            foreach (var child in document.RootElement.EnumerateChildren())
            {
                child.GetInnerText().Should().BeEmpty();
                child.GetString().Should().NotBeNullOrWhiteSpace();
                child.LocalName.Should().Be(child.ValueKind is XmlValueKind.ProcessingInstruction ? "pi" : "");
            }

            document.RootElement.GetString().Should().BeEmpty();
        }

        [TestMethod]
        public void Nil_Detection()
        {
            using var one = XmlDocument.Parse($"<e {XsiDeclaration} xsi:nil=\" 1 \"/>");
            one.RootElement.IsNil.Should().BeTrue();
            one.RootElement.IsEmptyElement.Should().BeTrue();
            one.RootElement.ValueKind.Should().Be(XmlValueKind.Nil);
            using var no = XmlDocument.Parse($"<e {XsiDeclaration} xsi:nil=\"false\" xsi:type=\"t\"/>");
            no.RootElement.IsNil.Should().BeFalse();
            using var unprefixed = XmlDocument.Parse("<e nil=\"true\"/>");
            unprefixed.RootElement.IsNil.Should().BeFalse();
            using var otherNamespace = XmlDocument.Parse("<e xmlns:p=\"urn:p\" p:nil=\"true\"/>");
            otherNamespace.RootElement.IsNil.Should().BeFalse();
            using var nilFirst = XmlDocument.Parse($"<e {XsiDeclaration} xsi:nil=\"true\" xsi:type=\"t\"/>");
            nilFirst.RootElement.IsNil.Should().BeTrue();
            nilFirst.RootElement.AttributeCount.Should().Be(3);
        }

        [TestMethod]
        public void Nil_WithContent_KeepsFollowingSiblings()
        {
            using var document = XmlDocument.Parse($"<r {XsiDeclaration}><e xsi:nil=\"true\">2020-01-02T03:04:05Z</e><f/></r>");
            var count = 0;
            foreach (var child in document.RootElement.EnumerateChildren())
            {
                count++;
            }

            count.Should().Be(2);
            var nil = document.RootElement.GetChild("e");
            nil.IsNil.Should().BeTrue();
            nil.IsEmptyElement.Should().BeFalse();
            nil.GetString().Should().BeNull();
            nil.GetInnerText().Should().Be("2020-01-02T03:04:05Z");
            var threw = 0;
            try
            {
                nil.GetDateTime();
            }
            catch (XmlException)
            {
                threw++;
            }

            try
            {
                nil.GetDateTimeOffset();
            }
            catch (XmlException)
            {
                threw++;
            }

            try
            {
                nil.GetGuid();
            }
            catch (XmlException)
            {
                threw++;
            }

            threw.Should().Be(3);
            document.RootElement.GetChild("f").IsEmptyElement.Should().BeTrue();
        }

        [TestMethod]
        public void NameLookup_NonAscii()
        {
            var longName = new string('é', 150);
            using var document = XmlDocument.Parse($"<r><é>1</é><{longName}>2</{longName}><�>3</�><ab>4</ab></r>");
            var root = document.RootElement;
            root.GetChild("é").GetInt32().Should().Be(1);
            root.GetChild(longName).GetInt32().Should().Be(2);
            root.GetChild("�").GetInt32().Should().Be(3);
            root.GetChild("ab").GetInt32().Should().Be(4);
            root.TryGetChild("ë", out _).Should().BeFalse();
            root.TryGetChild("\uD800", out _).Should().BeFalse();
            root.TryGetChild("Ã©", out _).Should().BeFalse();
            root.TryGetChild("abc", out _).Should().BeFalse();
            root.TryGetChild("a", out _).Should().BeFalse();
            root.TryGetChild("ac", out _).Should().BeFalse();
            root.TryGetChild(new string('é', 149) + "e", out _).Should().BeFalse();
        }

        [TestMethod]
        public void Parse_DeepNamespacedElements()
        {
            var xml = new StringBuilder();
            for (var i = 0; i < 10; i++)
            {
                xml.Append("<p:e xmlns:p=\"urn:p\">");
            }

            xml.Append($"<x:k {XsiDeclaration.Replace("xmlns:xsi", "xmlns:x", StringComparison.Ordinal)}>1</x:k><plain>2</plain>");
            for (var i = 0; i < 10; i++)
            {
                xml.Append("</p:e>");
            }

            using var document = XmlDocument.Parse(xml.ToString());
            var element = document.RootElement;
            for (var i = 0; i < 9; i++)
            {
                element = element.GetChild("e", "urn:p");
            }

            element.GetChild("k", XmlNamespaces.Xsi).GetInt32().Should().Be(1);
            element.GetChild("plain").NamespaceUri.Should().BeEmpty();
            element.GetChild("plain").GetInt32().Should().Be(2);
        }

        [TestMethod]
        public void Reader_NoNamespaceElement_AfterInternedNamespaceSibling_IsEmpty()
        {
            var reader = new Utf8XmlReader("<r><x:k xmlns:x=\"http://www.w3.org/2001/XMLSchema-instance\">1</x:k><plain>2</plain></r>"u8);
            reader.Read();
            reader.Read();
            reader.NamespaceUriEquals(XmlNamespaces.Xsi).Should().BeTrue();
            reader.Read();
            reader.Read();
            reader.Read();
            reader.LocalNameEquals("plain").Should().BeTrue();
            reader.NamespaceUriSpan.IsEmpty.Should().BeTrue();
        }

        [TestMethod]
        public void ParseValue_OnAttribute_ParsesOwningElement()
        {
            var reader = new Utf8XmlReader("<e a=\"1\"><n>2</n></e>"u8);
            reader.Read();
            reader.MoveToNextAttribute();
            using var document = XmlDocument.ParseValue(ref reader);
            document.RootElement.LocalName.Should().Be("e");
            document.RootElement.GetAttribute("a").GetInt32().Should().Be(1);
            document.RootElement.GetChild("n").GetInt32().Should().Be(2);
        }

        [TestMethod]
        public void ParseValue_PartialBuffer_ClosesOpenElements()
        {
            var reader = new Utf8XmlReader("<r><a>1</a><b><c>"u8, false, default);
            using var document = XmlDocument.ParseValue(ref reader);
            document.RootElement.LocalName.Should().Be("r");
            document.RootElement.GetChild("a").GetInt32().Should().Be(1);
            document.RootElement.GetChild("b").GetChild("c").EnumerateChildren().MoveNext().Should().BeFalse();
        }

        [TestMethod]
        public void ParseValue_CopiesFromCallerBuffer()
        {
            var xml = "<r xmlns:p=\"urn:p\"><p:e a=\"1\" p:b=\"x&amp;y\">text<n>5</n></p:e></r>"u8.ToArray();
            var reader = new Utf8XmlReader(xml);
            reader.Read();
            reader.Read();
            using var document = XmlDocument.ParseValue(ref reader);
            xml.AsSpan().Clear();
            var element = document.RootElement;
            element.LocalName.Should().Be("e");
            element.Prefix.Should().Be("p");
            element.NamespaceUri.Should().Be("urn:p");
            element.GetAttribute("a").GetInt32().Should().Be(1);
            element.GetAttribute("b", "urn:p").GetString().Should().Be("x&y");
            element.GetChild("n").GetInt32().Should().Be(5);
            element.GetRawXml().Should().Be("<p:e xmlns:p=\"urn:p\" a=\"1\" p:b=\"x&amp;y\">text<n>5</n></p:e>");
        }

        [TestMethod]
        public void Value_MixedContent_Concatenates()
        {
            using var document = XmlDocument.Parse("<e>1<!--c--><![CDATA[2]]><b>3</b>4</e>", new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Allow });
            document.RootElement.GetInt32().Should().Be(1234);
            document.RootElement.GetInt64().Should().Be(1234);
            document.RootElement.GetDecimal().Should().Be(1234m);
            document.RootElement.GetDouble().Should().Be(1234d);
            document.RootElement.GetString().Should().Be("1234");
            document.RootElement.GetInnerText().Should().Be("1234");
            document.RootElement.GetRawXml().Should().Be("<e>1<!--c--><![CDATA[2]]><b>3</b>4</e>");
        }

        [TestMethod]
        public void Value_ElementWithoutText_IsEmpty()
        {
            using var single = XmlDocument.Parse("<e><b/></e>");
            single.RootElement.GetString().Should().BeEmpty();
            using var several = XmlDocument.Parse("<e><a/><b/></e>");
            several.RootElement.GetInnerText().Should().BeEmpty();
            using var cdata = XmlDocument.Parse("<e><![CDATA[7]]></e>");
            cdata.RootElement.GetInt32().Should().Be(7);
            using var mixedBoolean = XmlDocument.Parse("<e>tr<b/>ue</e>");
            mixedBoolean.RootElement.GetBoolean().Should().BeTrue();
            using var mixedBase64 = XmlDocument.Parse("<e>AQ<b/>ID</e>");
            mixedBase64.RootElement.GetBytesFromBase64().Should().Equal(1, 2, 3);
        }

        [TestMethod]
        public void Value_LongText_UsesPooledScratch()
        {
            var padded = new string(' ', 200) + "2020-01-02T03:04:05Z";
            using var document = XmlDocument.Parse($"<e>{padded}</e>");
            document.RootElement.GetDateTime().Year.Should().Be(2020);
            document.RootElement.GetDateTimeOffset().Year.Should().Be(2020);
            using var guid = XmlDocument.Parse($"<e>{new string(' ', 200)}00112233-4455-6677-8899-aabbccddeeff</e>");
            guid.RootElement.GetGuid().Should().Be(Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"));
        }

        [TestMethod]
        public void WriteTo_LeafElements_RoundTrip()
        {
            RoundTrip("<r><n>v</n><m>line1\nline2</m></r>").Should().Be("<r><n>v</n><m>line1\nline2</m></r>");
            RoundTrip("<r><n>a&amp;b&lt;c]]&gt;d</n></r>").Should().Be("<r><n>a&amp;b&lt;c]]&gt;d</n></r>");
            RoundTrip("<r><n>café</n></r>").Should().Be("<r><n>café</n></r>");
            RoundTrip("<p:r xmlns:p=\"urn:p\"><p:n>v</p:n></p:r>").Should().Be("<p:r xmlns:p=\"urn:p\"><p:n>v</p:n></p:r>");
            RoundTrip("<r xmlns=\"urn:d\"><n>v</n></r>").Should().Be("<r xmlns=\"urn:d\"><n>v</n></r>");
            RoundTrip("<r><n a=\"1\">v</n><m><![CDATA[c]]></m><k>a<b/>c</k></r>").Should().Be("<r><n a=\"1\">v</n><m><![CDATA[c]]></m><k>a<b/>c</k></r>");
        }

        [TestMethod]
        public void WriteTo_PrefixedAttributeNamedXmlns_IsKept()
        {
            const string xml = "<e xmlns:p=\"urn:p\" p:xmlns=\"kept\" xmlns=\"urn:d\"/>";
            using var document = XmlDocument.Parse(xml);
            document.RootElement.GetAttribute("xmlns", "urn:p").GetString().Should().Be("kept");
            var written = document.RootElement.GetRawXml();
            written.Should().Be("<e xmlns:p=\"urn:p\" p:xmlns=\"kept\" xmlns=\"urn:d\"/>");
            var cloned = document.RootElement.CloneNode().ToString();
            cloned.Should().Be("<e xmlns:p=\"urn:p\" p:xmlns=\"kept\" xmlns=\"urn:d\"/>");
            using var reparsed = XmlDocument.Parse(written);
            reparsed.RootElement.AttributeCount.Should().Be(3);
            using var reparsedClone = XmlDocument.Parse(cloned);
            reparsedClone.RootElement.AttributeCount.Should().Be(3);
        }

        [TestMethod]
        public void Reader_EndTags_MatchStartTagBytes()
        {
            Drain("<p:a xmlns:p=\"u\" xmlns:q=\"u\"><p:b>1</p:b ><p:c>2</p:c ><q:d/></p:a>").Should().Be(9);
            Drain("<ab><abc>1</abc><a>2</a></ab>").Should().Be(8);
            var deep = new StringBuilder();
            for (var i = 0; i < 10; i++)
            {
                deep.Append("<p:e xmlns:p=\"u\">");
            }

            for (var i = 0; i < 10; i++)
            {
                deep.Append("</p:e>");
            }

            Drain(deep.ToString()).Should().Be(20);
            foreach (var malformed in new[] { "<p:a xmlns:p=\"u\"></p:ab>", "<p:a xmlns:p=\"u\"></p:a", "<ab></a>", "<a></ab>", "<p:a xmlns:p=\"u\" xmlns:q=\"v\"></q:a>", "<p:a xmlns:p=\"u\" xmlns:q=\"u\"><p:c>2</q:c></p:a>", "<é></éa>" })
            {
                var threw = false;
                try
                {
                    Drain(malformed);
                }
                catch (XmlException)
                {
                    threw = true;
                }

                threw.Should().BeTrue(malformed);
            }
        }

        [TestMethod]
        public void Reader_Text_DecodesOnlyWhenNeeded()
        {
            Text("<e>plain\nline</e>").Should().Be("plain\nline");
            Text("<e>a\r\nb\rc</e>").Should().Be("a\nb\nc");
            Text("<e>a&amp;b</e>").Should().Be("a&b");
            Text("<e>café 中</e>").Should().Be("café 中");
            Text("<?xml version=\"1.1\"?><e>a" + (char)0x85 + "b" + (char)0x2028 + "c</e>").Should().Be("a\nb\nc");
            var reader = new Utf8XmlReader("<r>\r\n<e>plain</e></r>"u8, new XmlReaderOptions { WhitespaceHandling = XmlWhitespaceHandling.Insignificant });
            reader.Read();
            reader.Read();
            reader.Read();
            reader.ValueIsEscaped.Should().BeFalse();
            reader.GetString().Should().Be("plain");
            var attribute = new Utf8XmlReader("<e a=\"x\ty\" b=\"p&lt;q\" c=\"plain\" d=\"l\nm\"/>"u8);
            attribute.Read();
            attribute.MoveToNextAttribute();
            attribute.GetString().Should().Be("x y");
            attribute.MoveToNextAttribute();
            attribute.GetString().Should().Be("p<q");
            attribute.MoveToNextAttribute();
            attribute.ValueIsEscaped.Should().BeFalse();
            attribute.GetString().Should().Be("plain");
            attribute.MoveToNextAttribute();
            attribute.GetString().Should().Be("l m");
        }

        [TestMethod]
        public void WriteTo_Indented_MatchesStartTextEnd()
        {
            using var document = XmlDocument.Parse("<r><n>v</n></r>");
            var expected = XmlWriterHarness.ToXml(
                w =>
                {
                    w.WriteStartElement("r");
                    w.WriteStartElement("n");
                    w.WriteStringValue("v");
                    w.WriteEndElement();
                    w.WriteEndElement();
                },
                new XmlWriterOptions { Indented = true, OmitXmlDeclaration = true });
            XmlWriterHarness.ToXml(document.WriteTo, new XmlWriterOptions { Indented = true, OmitXmlDeclaration = true }).Should().Be(expected);
        }

        [TestMethod]
        public void WriteTo_PrefixedLeafWithoutBinding_DeclaresNamespace()
        {
            var reader = new Utf8XmlReader("<r xmlns:p=\"urn:p\"><p:n>v</p:n></r>"u8);
            reader.Read();
            reader.Read();
            using var document = XmlDocument.ParseValue(ref reader);
            document.RootElement.GetRawXml().Should().Be("<p:n xmlns:p=\"urn:p\">v</p:n>");
        }

        [TestMethod]
        public void Writer_CompleteElementUtf8_Variants()
        {
            XmlWriterHarness.ToXml(w => w.WriteElementString("n"u8, "v"u8)).Should().Be("<n>v</n>");
            XmlWriterHarness.ToXml(w => w.WriteElementString("n"u8, ""u8)).Should().Be("<n></n>");
            XmlWriterHarness.ToXml(w => w.WriteElementString("n"u8, "a<b"u8)).Should().Be("<n>a&lt;b</n>");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("p"u8, "r"u8, "urn:a"u8);
                w.WriteCompleteElementUtf8("n"u8, "p"u8, "urn:a"u8, "v"u8);
                w.WriteCompleteElementUtf8("m"u8, "p"u8, "urn:b"u8, "w"u8);
                w.WriteEndElement();
            }).Should().Be("<p:r xmlns:p=\"urn:a\"><p:n>v</p:n><p:m xmlns:p=\"urn:b\">w</p:m></p:r>");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e"u8);
                w.WriteAttributeString("a"u8, "plain"u8);
                w.WriteAttributeString("b"u8, "q\"&<"u8);
                w.WriteEndElement();
            }).Should().Be("<e a=\"plain\" b=\"q&quot;&amp;&lt;\"/>");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteProcessingInstruction("pi", null);
                w.WriteProcessingInstruction("pj", "d");
                w.WriteStartElement("e");
                w.WriteEndElement();
            }).Should().Be("<?pi?><?pj d?><e/>");
        }

        #endregion

        #region Private Methods

        private static int Drain(string xml)
        {
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml));
            var count = 0;
            while (reader.Read())
            {
                count++;
            }

            return count;
        }

        private static string? Text(string xml)
        {
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml));
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.Text)
                {
                    return reader.GetString();
                }
            }

            return null;
        }

        private static string RoundTrip(string xml)
        {
            using var document = XmlDocument.Parse(xml);
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            document.WriteTo(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        #endregion

    }

}
