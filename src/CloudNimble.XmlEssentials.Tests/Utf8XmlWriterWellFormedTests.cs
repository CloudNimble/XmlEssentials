using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Well-formed output coverage for <see cref="Utf8XmlWriter"/>.
    /// </summary>
    [TestClass]
    public class Utf8XmlWriterWellFormedTests
    {

        #region Public Methods

        [TestMethod]
        public void WriteEmptyElement_Overloads()
        {
            XmlWriterHarness.ToXml(w => w.WriteEmptyElement("e")).Should().Be("<e/>");
            XmlWriterHarness.ToXml(w => w.WriteEmptyElement("e".AsSpan())).Should().Be("<e/>");
            XmlWriterHarness.ToXml(w => w.WriteEmptyElement("e"u8)).Should().Be("<e/>");
            XmlWriterHarness.ToXml(w => w.WriteEmptyElement(XmlEncodedText.Encode("e"))).Should().Be("<e/>");
            XmlWriterHarness.ToXml(w => w.WriteEmptyElement("e", "urn:x")).Should().Be("<e xmlns=\"urn:x\"/>");
            XmlWriterHarness.ToXml(w => w.WriteEmptyElement("e".AsSpan(), "urn:x".AsSpan())).Should().Be("<e xmlns=\"urn:x\"/>");
            XmlWriterHarness.ToXml(w => w.WriteEmptyElement("e"u8, "urn:x"u8)).Should().Be("<e xmlns=\"urn:x\"/>");
            XmlWriterHarness.ToXml(w => w.WriteEmptyElement("p", "e", "urn:x")).Should().Be("<p:e xmlns:p=\"urn:x\"/>");
            XmlWriterHarness.ToXml(w => w.WriteEmptyElement("p"u8, "e"u8, "urn:x"u8)).Should().Be("<p:e xmlns:p=\"urn:x\"/>");
        }

        [TestMethod]
        public void WriteEmptyElement_NestedAndReusesPrefix()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("p", "a", "urn:x");
                w.WriteEmptyElement("p", "b", "urn:x");
                w.WriteEndElement();
            }).Should().Be("<p:a xmlns:p=\"urn:x\"><p:b/></p:a>");
        }

        [TestMethod]
        public void Write_Declaration_DefaultVersion10()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteEndElement();
            }, omitDeclaration: false).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?><e/>");
        }

        [TestMethod]
        public void Write_Declaration_Version11AndStandalone()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteEndElement();
            }, new XmlWriterOptions { Version = XmlVersion.Version11, Standalone = true }).Should()
                .Be("<?xml version=\"1.1\" encoding=\"utf-8\" standalone=\"yes\"?><e/>");
        }

        [TestMethod]
        public void Write_Declaration_StandaloneNo()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteEndElement();
            }, new XmlWriterOptions { Standalone = false }).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?><e/>");
        }

        [TestMethod]
        public void Write_Bom_EmitsUtf8BomOnce()
        {
            var bytes = XmlWriterHarness.ToBytes(w =>
            {
                w.WriteStartElement("e");
                w.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, EmitByteOrderMark = true });
            bytes[0].Should().Be(0xEF);
            bytes[1].Should().Be(0xBB);
            bytes[2].Should().Be(0xBF);
            Encoding.UTF8.GetString(bytes.AsSpan(3)).Should().Be("<e/>");
        }

        [TestMethod]
        public void Write_CommentThenRoot_DeclarationFirst()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteComment("c");
                w.WriteStartElement("e");
                w.WriteEndElement();
            }, omitDeclaration: false).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?><!--c--><e/>");
        }

        [TestMethod]
        public void Write_ProcessingInstruction_BeforeRoot()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteProcessingInstruction("pi", "data");
                w.WriteStartElement("e");
                w.WriteEndElement();
            }).Should().Be("<?pi data?><e/>");
        }

        [TestMethod]
        public void Write_MixedContent()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("p");
                w.WriteStringValue("hello ");
                w.WriteStartElement("b");
                w.WriteStringValue("world");
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Be("<p>hello <b>world</b></p>");
        }

        [TestMethod]
        public void Write_PrefixedAttribute_AutoDeclaresNs()
        {
            var xml = XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString(null, "id", "urn:x", "1");
                w.WriteEndElement();
            });
            xml.Should().Be("<e xmlns:ns1=\"urn:x\" ns1:id=\"1\"/>");
            xml.Should().Be("<e xmlns:ns1=\"urn:x\" ns1:id=\"1\"/>");
        }

        [TestMethod]
        public void Write_ExplicitNamespaceDeclaration()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteNamespaceDeclaration("p", "urn:x");
                w.WriteStartElement("p", "c", "urn:x");
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Be("<e xmlns:p=\"urn:x\"><p:c/></e>");
        }

        [TestMethod]
        public void Write_DefaultNamespaceUnbind()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("a", "urn:x");
                w.WriteStartElement("b");
                w.WriteDefaultNamespace("");
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Be("<a xmlns=\"urn:x\"><b xmlns=\"\"/></a>");
        }

        [TestMethod]
        public void Write_XmlPrefix_DoesNotDeclareXmlns()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("xml", "lang", null, "en");
                w.WriteEndElement();
            }).Should().Be("<e xml:lang=\"en\"/>");
        }

        [TestMethod]
        public void Write_NestedReuseOfPrefix()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("p", "a", "urn:x");
                w.WriteStartElement("p", "b", "urn:x");
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Be("<p:a xmlns:p=\"urn:x\"><p:b/></p:a>");
        }

        [TestMethod]
        public void Write_TypedValues_RoundTripThroughReader()
        {
            var guid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var xml = XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("root");
                w.WriteElementString("d", new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc));
                w.WriteElementString("g", guid);
                w.WriteElementString("s", "hi");
                w.WriteElementNumber("n", 42);
                w.WriteElementNumber("l", 99L);
                w.WriteElementNumber("dec", 1.5m);
                w.WriteElementBoolean("b", false);
                w.WriteEndElement();
            }, omitDeclaration: false);
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml));
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.XmlDeclaration);
            AdvanceTo(ref reader, "d");
            reader.Read();
            reader.GetDateTime().Kind.Should().Be(DateTimeKind.Utc);
            AdvanceTo(ref reader, "g");
            reader.Read();
            reader.GetGuid().Should().Be(guid);
            AdvanceTo(ref reader, "n");
            reader.Read();
            reader.GetInt32().Should().Be(42);
            AdvanceTo(ref reader, "b");
            reader.Read();
            reader.GetBoolean().Should().BeFalse();
        }

        [TestMethod]
        public void Write_DateTimeKinds()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue(new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Unspecified));
                w.WriteEndElement();
            }).Should().Be("<e>2020-01-02T03:04:05</e>");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue(new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc));
                w.WriteEndElement();
            }).Should().Be("<e>2020-01-02T03:04:05Z</e>");
        }

        [TestMethod]
        public void Write_DateOnlyTimeOnlyDurationGuid()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue(new DateOnly(2020, 1, 2));
                w.WriteEndElement();
            }).Should().Be("<e>2020-01-02</e>");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue(new TimeOnly(1, 2, 3));
                w.WriteEndElement();
            }).Should().Be("<e>01:02:03</e>");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue(TimeSpan.FromHours(1));
                w.WriteEndElement();
            }).Should().Be("<e>PT1H</e>");
        }

        [TestMethod]
        public void Write_InfinityAndNaN()
        {
            XmlWriterHarness.ToXml(w => w.WriteElementNumber("n", double.PositiveInfinity)).Should().Be("<n>INF</n>");
            XmlWriterHarness.ToXml(w => w.WriteElementNumber("n", double.NegativeInfinity)).Should().Be("<n>-INF</n>");
            XmlWriterHarness.ToXml(w => w.WriteElementNumber("n", double.NaN)).Should().Be("<n>NaN</n>");
            XmlWriterHarness.ToXml(w => w.WriteElementNumber("n", float.PositiveInfinity)).Should().Be("<n>INF</n>");
            XmlWriterHarness.ToXml(w => w.WriteElementNumber("n", float.NegativeInfinity)).Should().Be("<n>-INF</n>");
            XmlWriterHarness.ToXml(w => w.WriteElementNumber("n", float.NaN)).Should().Be("<n>NaN</n>");
        }

        [TestMethod]
        public void Write_UnsignedAndFloat()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeNumber("i", 7);
                w.WriteAttributeNumber("l", 8L);
                w.WriteAttributeNumber("d", 1.5d);
                w.WriteStartAttribute("n");
                w.WriteNumberValue(9);
                w.WriteEndAttribute();
                w.WriteEndElement();
            }).Should().Be("<e i=\"7\" l=\"8\" d=\"1.5\" n=\"9\"/>");
            XmlWriterHarness.ToXml(w => w.WriteElementNumber("é", 1)).Should().Be("<\u00E9>1</\u00E9>");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeNumber("é", 2);
                w.WriteEndElement();
            }).Should().Be("<e \u00E9=\"2\"/>");
            XmlWriterHarness.ToXml(w => w.WriteElementNumber("n", 3u)).Should().Be("<n>3</n>");
            XmlWriterHarness.ToXml(w => w.WriteElementNumber("n", 4ul)).Should().Be("<n>4</n>");
            XmlWriterHarness.ToXml(w => w.WriteElementNumber("n", 1.5d)).Should().Be("<n>1.5</n>");
            XmlWriterHarness.ToXml(w => w.WriteElementNumber("n", 1.5f)).Should().Be("<n>1.5</n>");
        }

        [TestMethod]
        public void Write_SpanAndUtf8Overloads()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("root".AsSpan());
                w.WriteAttributeString("id".AsSpan(), "1".AsSpan());
                w.WriteElementString("n"u8, "42"u8);
                w.WriteElementBoolean("b".AsSpan(), true);
                w.WriteElementBoolean("c"u8, false);
                w.WriteElementBase64String("d".AsSpan(), "hi"u8);
                w.WriteEndElement();
            }).Should().Be("<root id=\"1\"><n>42</n><b>true</b><c>false</c><d>aGk=</d></root>");
        }

        [TestMethod]
        public void Write_SegmentsAndRaw()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValueSegment("he"u8, false);
                w.WriteStringValueSegment("llo".AsSpan(), true);
                w.WriteEndElement();
            }).Should().Be("<e>hello</e>");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteRawValue("<x/>", skipInputValidation: true);
                w.WriteEndElement();
            }).Should().Be("<e><x/></e>");
        }

        [TestMethod]
        public void Write_Base64Segment()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteBase64StringSegment("hi"u8, true);
                w.WriteEndElement();
            }).Should().Be("<e>aGk=</e>");
        }

        [TestMethod]
        public void Write_NilAfterAttributes()
        {
            var xml = XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("id", "1");
                w.WriteNilValue();
                w.WriteEndElement();
            });
            xml.Should().Be("<e id=\"1\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>");
            xml.Should().Be("<e id=\"1\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>");
        }

        [TestMethod]
        public void Write_NilElementWithNamespace()
        {
            var xml = XmlWriterHarness.ToXml(w => w.WriteNilElement("e", "urn:x"));
            xml.Should().Be("<e xmlns=\"urn:x\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>");
            xml.Should().Be("<e xmlns=\"urn:x\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>");
        }

        [TestMethod]
        public void Write_IndentedAfterDeclaration()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("a");
                w.WriteStartElement("b");
                w.WriteEndElement();
                w.WriteEndElement();
            }, new XmlWriterOptions { Indented = true, NewLine = "\n", IndentSize = 2 }).Should()
                .Be("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<a>\n  <b/>\n</a>");
        }

        [TestMethod]
        public void Write_IndentCharacterTab()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("a");
                w.WriteStartElement("b");
                w.WriteEndElement();
                w.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, Indented = true, NewLine = "\n", IndentCharacter = '\t', IndentSize = 1 })
                .Should().Be("<a>\n\t<b/>\n</a>");
        }

        [TestMethod]
        public void Write_Version11_RestrictedCharAsNcr()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue([(byte)0x01]);
                w.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, Version = XmlVersion.Version11 })
                .Should().Be("<e>&#x1;</e>");
        }

        [TestMethod]
        public void Write_GreaterThanNotEscapedExceptCdataCloser()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue("a>b");
                w.WriteEndElement();
            }).Should().Be("<e>a>b</e>");
        }

        [TestMethod]
        public void Write_Prefix11_Undeclare()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteNamespaceDeclaration("p", "urn:x");
                w.WriteStartElement("c");
                w.WriteNamespaceDeclaration("p", "");
                w.WriteEndElement();
                w.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, Version = XmlVersion.Version11 })
                .Should().Be("<e xmlns:p=\"urn:x\"><c xmlns:p=\"\"/></e>");
        }

        [TestMethod]
        public void Write_Reset_StartsNewDocument()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("a");
            writer.WriteEndElement();
            writer.Flush();
            writer.Reset();
            writer.BytesCommitted.Should().Be(0);
            writer.WriteStartElement("b");
            writer.WriteEndElement();
            writer.Dispose();
            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<a/><b/>");
        }

        [TestMethod]
        public void Write_BytesPendingAndCommitted()
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("e");
            writer.BytesPending.Should().Be(2);
            writer.Flush();
            writer.BytesPending.Should().Be(0);
            writer.BytesCommitted.Should().Be(2);
            writer.WriteEndElement();
        }

        [TestMethod]
        public async Task Write_FlushAsyncAndDisposeAsync()
        {
            var stream = new MemoryStream();
            await using (var writer = new Utf8XmlWriter(stream, new XmlWriterOptions { OmitXmlDeclaration = true }))
            {
                writer.WriteStartElement("e");
                writer.WriteEndElement();
                await writer.FlushAsync();
            }

            Encoding.UTF8.GetString(stream.ToArray()).Should().Be("<e/>");
        }

        [TestMethod]
        public void Write_SkipValidation_AllowsUnclosedDispose()
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true }))
            {
                writer.WriteStartElement("e");
            }

            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<e");
        }

        [TestMethod]
        public void Write_DepthNine_UsesHeapFrames()
        {
            XmlWriterHarness.ToXml(w =>
            {
                for (var i = 0; i < 9; i++)
                {
                    w.WriteStartElement("e");
                }

                for (var i = 0; i < 9; i++)
                {
                    w.WriteEndElement();
                }
            }).Should().Be("<e><e><e><e><e><e><e><e><e/></e></e></e></e></e></e></e></e>");
        }

        [TestMethod]
        public void Write_NineNamespaces_UsesHeapBindings()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                for (var i = 1; i <= 9; i++)
                {
                    w.WriteNamespaceDeclaration("p" + i, "urn:" + i);
                }

                w.WriteEndElement();
            }).Should().Be("<e xmlns:p1=\"urn:1\" xmlns:p2=\"urn:2\" xmlns:p3=\"urn:3\" xmlns:p4=\"urn:4\" xmlns:p5=\"urn:5\" xmlns:p6=\"urn:6\" xmlns:p7=\"urn:7\" xmlns:p8=\"urn:8\" xmlns:p9=\"urn:9\"/>");
        }

        [TestMethod]
        public void Write_ElementStringOverloads()
        {
            XmlWriterHarness.ToXml(w => w.WriteElementString("e", "urn:x", "v")).Should().Be("<e xmlns=\"urn:x\">v</e>");
            XmlWriterHarness.ToXml(w => w.WriteElementString("e".AsSpan(), "v".AsSpan())).Should().Be("<e>v</e>");
            XmlWriterHarness.ToXml(w => w.WriteElementString("e", (string?)null)).Should().Be("<e/>");
            XmlWriterHarness.ToXml(w => w.WriteElementString("e", DateTimeOffset.UnixEpoch)).Should().Be("<e>1970-01-01T00:00:00+00:00</e>");
        }

        [TestMethod]
        public void Write_AttributeUtf8AndPrefix()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("id"u8, "1"u8);
                w.WriteAttributeString("p", "n", "urn:x", "v");
                w.WriteEndElement();
            }).Should().Be("<e id=\"1\" xmlns:p=\"urn:x\" p:n=\"v\"/>");
        }

        [TestMethod]
        public void Write_CDataCommentPiInsideElement()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteComment("c");
                w.WriteCData("x<y");
                w.WriteProcessingInstruction("pi", null);
                w.WriteEndElement();
            }).Should().Be("<e><!--c--><![CDATA[x<y]]><?pi?></e>");
        }

        [TestMethod]
        public void Write_ResetToNewDestinations()
        {
            var first = new ArrayBufferWriter<byte>();
            var writer = new Utf8XmlWriter(first, new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("a");
            writer.WriteEndElement();
            var second = new ArrayBufferWriter<byte>();
            writer.Reset(second);
            writer.WriteStartElement("b");
            writer.WriteEndElement();
            writer.Dispose();
            Encoding.UTF8.GetString(first.WrittenSpan).Should().Be("<a/>");
            Encoding.UTF8.GetString(second.WrittenSpan).Should().Be("<b/>");

            using var stream = new MemoryStream();
            var streamWriter = new Utf8XmlWriter(new ArrayBufferWriter<byte>(), new XmlWriterOptions { OmitXmlDeclaration = true });
            streamWriter.Reset(stream);
            streamWriter.WriteStartElement("c");
            streamWriter.WriteEndElement();
            streamWriter.Dispose();
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be("<c/>");
        }

        #endregion

        #region Private Methods

        private static void AdvanceTo(ref Utf8XmlReader reader, string local)
        {
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.StartElement && reader.LocalNameEquals(local))
                {
                    return;
                }
            }

            Assert.Fail("missing " + local);
        }

        #endregion

    }

}
