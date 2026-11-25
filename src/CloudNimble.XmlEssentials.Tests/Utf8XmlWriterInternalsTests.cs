using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Direct tests of <see cref="Utf8XmlWriter"/> internals.
    /// </summary>
    [TestClass]
    public class Utf8XmlWriterInternalsTests
    {

        #region Public Methods

        [TestMethod]
        public void CloseStartTag_AtDepthZero_LeavesTheWriterEmpty()
        {
            using var writer = Create();
            writer.CloseStartTag();
            writer.CurrentDepth.Should().Be(0);
            writer.BytesPending.Should().Be(0);
        }

        [TestMethod]
        public void MarkContent_AtDepthZero_LeavesTheWriterEmpty()
        {
            using var writer = Create();
            writer.MarkContent();
            writer.CurrentDepth.Should().Be(0);
            writer.BytesPending.Should().Be(0);
        }

        [TestMethod]
        public void TryReadRune_EmptyInput_IsFalse()
        {
            var i = 0;
            Utf8XmlWriter.TryReadRune("A"u8, ref i, out var cp).Should().BeTrue();
            cp.Should().Be('A');
            i.Should().Be(1);
            i = 0;
            Utf8XmlWriter.TryReadRune([], ref i, out _).Should().BeFalse();
            i.Should().Be(0);
            i = 0;
            Utf8XmlWriter.TryReadRune([(byte)0xC3, 0xA9], ref i, out cp).Should().BeTrue();
            cp.Should().Be(0xE9);
            i = 0;
            Utf8XmlWriter.TryReadRune([(byte)0x80], ref i, out _).Should().BeFalse();
            i = 0;
            Utf8XmlWriter.TryReadRune([(byte)0xE2, 0x82], ref i, out _).Should().BeFalse();
        }

        [TestMethod]
        public void WriteIndent_WhenNotIndented_WritesNothing()
        {
            using var writer = Create();
            writer.WriteIndent(3);
            writer.BytesPending.Should().Be(0);
        }

        [TestMethod]
        public void WriteNumberValue_AllNumericTypes()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteNumberValue(1L);
                w.WriteNumberValue(1.5d);
                w.WriteNumberValue(1.5f);
                w.WriteNumberValue(1.5m);
                w.WriteNumberValue(3u);
                w.WriteNumberValue(4ul);
                w.WriteEndElement();
            }).Should().Be("<e>11.51.51.534</e>");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteNumberValue(double.NaN);
                w.WriteNumberValue(double.PositiveInfinity);
                w.WriteNumberValue(double.NegativeInfinity);
                w.WriteNumberValue(float.NaN);
                w.WriteNumberValue(float.PositiveInfinity);
                w.WriteNumberValue(float.NegativeInfinity);
                w.WriteEndElement();
            }).Should().Be("<e>NaNINF-INFNaNINF-INF</e>");
        }

        [TestMethod]
        public void WriteElementString_EmptyNamespaceUsesCompletePath()
        {
            XmlWriterHarness.ToXml(w => w.WriteElementString("e", "", "v")).Should().Be("<e>v</e>");
            XmlWriterHarness.ToXml(w => w.WriteElementString("e", (string?)null, "v")).Should().Be("<e>v</e>");
        }

        [TestMethod]
        public void WriteFrameName_PrefixedLiteralWithContent()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("xml", "lang", null);
                w.WriteStringValue("en");
                w.WriteEndElement();
            }).Should().Be("<xml:lang>en</xml:lang>");
        }

        [TestMethod]
        public void WriteStartElementUtf8_XmlPrefixAndBoundPrefix()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("xml"u8, "lang"u8, []);
                w.WriteEndElement();
            }).Should().Be("<xml:lang/>");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("xmlns"u8, "p"u8, []);
                w.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true }).Should().Be("<xmlns:p/>");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("p"u8, "a"u8, "urn:x"u8);
                w.WriteStartElement("b"u8, "urn:x"u8);
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Be("<p:a xmlns:p=\"urn:x\"><p:b/></p:a>");
        }

        [TestMethod]
        public void WriteStartElementUtf8_InAttributeAndLimits()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e"u8);
                w.WriteStartAttribute("a");
                w.WriteStartElement("c"u8);
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Be("<e a=\"\"><c/></e>");

            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("a"u8);
                w.WriteEndElement();
                w.WriteStartElement("b"u8);
            });

            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("a"u8);
                w.WriteStartElement("b"u8);
            }, new XmlWriterOptions { OmitXmlDeclaration = true, MaxDepth = 1 });

            XmlWriterHarness.MustThrow(w => w.WriteStartElement("e$"u8));
            XmlWriterHarness.MustThrow(w => w.WriteStartElement("p"u8, "e"u8, []));
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("p"u8, "e"u8, []);
                w.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true }).Should().Be("<p:e/>");
        }

        [TestMethod]
        [DataRow(0L, "PT0S", DisplayName = "zero")]
        [DataRow(1L, "PT0.0000001S", DisplayName = "one tick")]
        [DataRow(15_000_000L, "PT1.5S", DisplayName = "a fraction has no trailing zeros")]
        [DataRow(70_000L, "PT0.007S", DisplayName = "milliseconds")]
        [DataRow(90_000_000L, "PT9S", DisplayName = "seconds only")]
        [DataRow(4_800_000_000L, "PT8M", DisplayName = "minutes only")]
        [DataRow(108_000_000_000L, "PT3H", DisplayName = "hours only")]
        [DataRow(1_728_000_000_000L, "P2D", DisplayName = "days only")]
        [DataRow(1_728_040_000_000L, "P2DT4S", DisplayName = "days and seconds")]
        [DataRow(937_840_050_000L, "P1DT2H3M4.005S", DisplayName = "every part")]
        [DataRow(-37_230_000_000L, "-PT1H2M3S", DisplayName = "negative")]
        [DataRow(long.MaxValue, "P10675199DT2H48M5.4775807S", DisplayName = "the largest duration")]
        [DataRow(long.MinValue, "-P10675199DT2H48M5.4775808S", DisplayName = "the smallest duration")]
        public void FormatDuration_WritesEveryTick_AndReadsItBack(long ticks, string expected)
        {
            var value = new TimeSpan(ticks);
            Span<byte> buffer = stackalloc byte[32];
            var written = XmlLexical.FormatDuration(value, buffer);
            Encoding.UTF8.GetString(buffer[..written]).Should().Be(expected);
            var xml = XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue(value);
                w.WriteEndElement();
            });
            xml.Should().Be($"<e>{expected}</e>");
            XmlLexical.TryParseDuration(buffer[..written], out var parsed).Should().BeTrue();
            parsed.Should().Be(value);
        }

        [TestMethod]
        public void XmlEncodedText_EqualsOperatorsAndHash()
        {
            var a = XmlEncodedText.Encode("root");
            var b = XmlEncodedText.Encode("root"u8);
            var c = XmlEncodedText.Encode("other");
            (a == b).Should().BeTrue();
            (a != c).Should().BeTrue();
            a.Equals((object)b).Should().BeTrue();
            a.Equals((object)"root").Should().BeFalse();
            a.GetHashCode().Should().Be(b.GetHashCode());
            default(XmlEncodedText).ToString().Should().BeEmpty();
            XmlEncodedText.Encode("").EncodedUtf8Bytes.IsEmpty.Should().BeTrue();
        }

        [TestMethod]
        public void Dispose_Twice_WritesTheDocumentOnce()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("e");
            writer.WriteEndElement();
            writer.Dispose();
            writer.Dispose();
            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<e/>");
        }

        [TestMethod]
        public async Task DisposeAsync_Twice_WritesTheDocumentOnce()
        {
            using var stream = new MemoryStream();
            var writer = new Utf8XmlWriter(stream, new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("e");
            writer.WriteEndElement();
            await writer.DisposeAsync();
            await writer.DisposeAsync();
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be("<e/>");
        }

        [TestMethod]
        public void WriteCompleteElement_EscapesText()
        {
            XmlWriterHarness.ToXml(w => w.WriteElementString("e", "a&b<c]]>")).Should().Be("<e>a&amp;b&lt;c]]&gt;</e>");
            XmlWriterHarness.ToXml(w => w.WriteElementString("e", (string?)null)).Should().Be("<e/>");
            XmlWriterHarness.ToXml(w => w.WriteElementString("e", "")).Should().Be("<e></e>");
            XmlWriterHarness.ToXml(w => w.WriteElementString("e", "urn:x", (string?)null)).Should().Be("<e xmlns=\"urn:x\"/>");
        }

        [TestMethod]
        public void WriteCompleteElement_Indented()
        {
            XmlWriterHarness.ToXml(
                w =>
                {
                    w.WriteStartElement("root");
                    w.WriteElementString("n", "42");
                    w.WriteElementNumber("q", 1);
                    w.WriteEndElement();
                },
                new XmlWriterOptions { OmitXmlDeclaration = true, Indented = true, NewLine = "\n" })
                .Should().Be("<root>\n  <n>42</n>\n  <q>1</q>\n</root>");
        }

        [TestMethod]
        public void WriteStartElementLiteral_XmlPrefixAndEmpty()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("xml", "lang", null);
                w.WriteEndElement();
            }).Should().Be("<xml:lang/>");

            XmlWriterHarness.ToXml(w => w.WriteEmptyElement("e")).Should().Be("<e/>");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("p", "e", null);
                w.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true }).Should().Be("<p:e/>");
        }

        [TestMethod]
        public void Reset_SameDestination_WritesSecondDocument()
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("a");
            writer.WriteEndElement();
            writer.Reset();
            buffer.Clear();
            writer.WriteStartElement("b");
            writer.WriteEndElement();
            writer.Flush();
            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<b/>");
        }

        [TestMethod]
        public async Task FlushAsync_BufferWriter()
        {
            var buffer = new ArrayBufferWriter<byte>();
            await using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("e");
            writer.WriteEndElement();
            await writer.FlushAsync();
            buffer.WrittenCount.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public void WriteXmlPrefixOnAttributeWithoutUri()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("xml", "lang", null);
                w.WriteStringValue("en");
                w.WriteEndAttribute();
                w.WriteEndElement();
            }).Should().Be("<e xml:lang=\"en\"/>");
        }

        #endregion

        #region Private Methods

        private static Utf8XmlWriter Create(XmlWriterOptions? options = null)
        {
            return new Utf8XmlWriter(new ArrayBufferWriter<byte>(), options ?? new XmlWriterOptions { OmitXmlDeclaration = true });
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// An <see cref="IBufferWriter{T}"/> that never supplies memory.
        /// </summary>
        private sealed class EmptyBufferWriter : IBufferWriter<byte>
        {

            /// <inheritdoc />
            public void Advance(int count)
            {
            }

            /// <inheritdoc />
            public Memory<byte> GetMemory(int sizeHint = 0)
            {
                return Memory<byte>.Empty;
            }

            /// <inheritdoc />
            public Span<byte> GetSpan(int sizeHint = 0)
            {
                return Span<byte>.Empty;
            }

        }

        #endregion

    }

}
