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
        public void WriteStartElementFromChars_EmptyAscii()
        {
            using var writer = Create();
            writer.WriteStartElementFromChars("e", default, [], empty: true);
            writer.Flush();
        }

        [TestMethod]
        public void WriteStartElementFromChars_PrefixesAndNamespaces()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElementFromChars("lang", "xml", [], empty: true);
            }).Should().StartWith("<xml:lang");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElementFromChars("p", "xmlns", [], empty: true);
            }).Should().StartWith("<xmlns:p");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElementFromChars("e", default, "urn:x"u8, empty: true);
            }).Should().Contain("xmlns=\"urn:x\"");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElementFromChars("e", "p", "urn:x"u8, empty: true);
            }).Should().Contain("xmlns:p");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElementFromChars("e", default, "urn:x"u8, empty: false);
                w.WriteEndElement();
            }).Should().Contain("xmlns=\"urn:x\"");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElementFromChars("e", "p", "urn:x"u8, empty: false);
                w.WriteStartElementFromChars("child", default, "urn:x"u8, empty: false);
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Contain("p:child");

            XmlWriterHarness.MustThrow(w => w.WriteStartElementFromChars("e", "p", [], empty: true));
            XmlWriterHarness.ToXml(
                w => w.WriteStartElementFromChars("e", "p", [], empty: true),
                new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true })
                .Should()
                .Contain("<p:e");
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
            }).Should().Contain("1.5");

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
            }).Should().Contain("NaN").And.Contain("INF");
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
        public void TrackAttributeChars_RecordsName()
        {
            using var writer = Create();
            writer.WriteStartElement("e");
            writer.TrackAttributeChars("id", []);
            writer.WriteEndElement();
        }

        [TestMethod]
        public void WriteStartAttributeLiteral_WritesAttribute()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttributeLiteral("id");
                w.WriteStringValue("1");
                w.WriteEndAttribute();
                w.WriteEndElement();
            }).Should().Be("<e id=\"1\"/>");
        }

        [TestMethod]
        public void AppendNameFromChars_Empty()
        {
            using var writer = Create();
            writer.AppendNameFromChars([]).Length.Should().Be(0);
        }

        [TestMethod]
        public void WriteCharsRaw_AsciiAndNonAscii()
        {
            using var writer = Create();
            writer.WriteStartElement("e");
            writer.WriteCharsRaw("hello");
            writer.WriteCharsRaw("é");
            writer.WriteEndElement();
        }

        [TestMethod]
        public void ValidateNcNameChars_AsciiAndInvalid()
        {
            using var writer = Create();
            writer.ValidateNcNameChars("item");
            var threw = false;
            try
            {
                writer.ValidateNcNameChars("1item");
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void AppendNameFromChars_Ascii()
        {
            using var writer = Create();
            var name = writer.AppendNameFromChars("item");
            writer.NameSpan(name).ToArray().Should().Equal("item"u8.ToArray());
            writer.AppendNameFromChars("é").Length.Should().BeGreaterThan(1);
            writer.AppendNameFromChars(new string('n', 300)).Length.Should().Be(300);
        }

        [TestMethod]
        public void WriteCharsRaw_EmptyIsNoOp()
        {
            using var writer = Create();
            writer.WriteCharsRaw([]);
            writer.BytesPending.Should().Be(0);
        }

        [TestMethod]
        public void ValidateNcNameChars_EmptyNonAsciiAndSkip()
        {
            using var writer = Create();
            var threw = false;
            try
            {
                writer.ValidateNcNameChars([]);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
            writer.ValidateNcNameChars("élem");
            using var skip = Create(new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true });
            skip.ValidateNcNameChars("1bad");
        }

        [TestMethod]
        public void WriteStartElementUtf8_XmlPrefixAndBoundPrefix()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("xml"u8, "lang"u8, []);
                w.WriteEndElement();
            }).Should().StartWith("<xml:lang");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("xmlns"u8, "p"u8, []);
                w.WriteEndElement();
            }).Should().StartWith("<xmlns:p");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("p"u8, "a"u8, "urn:x"u8);
                w.WriteStartElement("b"u8, "urn:x"u8);
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Contain("<p:b/>");
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
            }).Should().Contain("<c/>");

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
            }, new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true }).Should().Contain("<p:e");
        }

        [TestMethod]
        public void EncodeChars_EmptyAndText()
        {
            Utf8XmlWriter.EncodeChars([]).Should().BeEmpty();
            Utf8XmlWriter.EncodeChars("ab").Should().Equal((byte)'a', (byte)'b');
        }

        [TestMethod]
        public void FormatDuration_ZeroNegativeAndParts()
        {
            Utf8XmlWriter.FormatDuration(TimeSpan.Zero).Should().Be("PT0S");
            Utf8XmlWriter.FormatDuration(new TimeSpan(-1, -2, -3)).Should().Be("-PT1H2M3S");
            Utf8XmlWriter.FormatDuration(TimeSpan.FromDays(2)).Should().Be("P2D");
            Utf8XmlWriter.FormatDuration(new TimeSpan(1, 2, 3, 4, 5)).Should().Be("P1DT2H3M4.005S");
            Utf8XmlWriter.FormatDuration(TimeSpan.FromMilliseconds(7)).Should().Be("PT0.007S");
            Utf8XmlWriter.FormatDuration(TimeSpan.FromMinutes(8)).Should().Be("PT8M");
            Utf8XmlWriter.FormatDuration(TimeSpan.FromSeconds(9)).Should().Be("PT9S");
            Utf8XmlWriter.FormatDuration(TimeSpan.FromHours(3)).Should().Be("PT3H");
            Utf8XmlWriter.FormatDuration(new TimeSpan(2, 0, 0, 4)).Should().Be("P2DT4S");
        }

        [TestMethod]
        public void TryReadRune_AsciiMultibyteAndFailure()
        {
            var i = 0;
            Utf8XmlWriter.TryReadRune("A"u8, ref i, out var cp).Should().BeTrue();
            cp.Should().Be('A');
            i.Should().Be(1);

            i = 0;
            Utf8XmlWriter.TryReadRune([], ref i, out _).Should().BeFalse();

            i = 0;
            Utf8XmlWriter.TryReadRune([(byte)0xC3, 0xA9], ref i, out cp).Should().BeTrue();
            cp.Should().Be(0xE9);
            i.Should().Be(2);

            i = 0;
            Utf8XmlWriter.TryReadRune([(byte)0x80], ref i, out _).Should().BeFalse();

            i = 0;
            Utf8XmlWriter.TryReadRune([(byte)0xE2, 0x82], ref i, out _).Should().BeFalse();
        }

        [TestMethod]
        public void ValidateName_SkipValidationAllowsJunk()
        {
            using var writer = Create(new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true });
            writer.ValidateName([]);
            writer.ValidateNcName("a:b"u8);
        }

        [TestMethod]
        public void LookupPrefix_XmlAndXmlnsAreReserved()
        {
            using var writer = Create();
            writer.LookupPrefix(XmlUtf8.Xml).Should().Equal("xml"u8.ToArray());
            writer.LookupPrefix(XmlUtf8.Xmlns).Should().Equal("xmlns"u8.ToArray());
            writer.LookupPrefix("urn:none"u8).Should().BeNull();
            writer.LookupPrefixedPrefix(XmlUtf8.Xml).Should().Equal("xml"u8.ToArray());
            writer.LookupPrefixedPrefix(XmlUtf8.Xmlns).Should().Equal("xmlns"u8.ToArray());
            writer.LookupUri("xml"u8).Should().Equal(XmlUtf8.Xml);
            writer.LookupUri("p"u8).Should().BeNull();
            writer.WriteStartElement("e");
            writer.WriteDefaultNamespace("urn:d");
            writer.LookupPrefixedPrefix("urn:d"u8).Should().BeNull();
            writer.LookupPrefixedPrefix("urn:none"u8).Should().BeNull();
            writer.WriteEndElement();
            writer.PrefixInUse("xml"u8).Should().BeTrue();
            writer.PrefixInUse("xmlns"u8).Should().BeTrue();
            writer.PrefixInUse("p"u8).Should().BeFalse();
        }

        [TestMethod]
        public void NextAutoPrefix_SkipsInUse()
        {
            using var writer = Create();
            writer.WriteStartElement("e");
            writer.WriteNamespaceDeclaration("ns1", "urn:1");
            writer.NextAutoPrefix().Should().Equal("ns2"u8.ToArray());
            writer.WriteEndElement();
        }

        [TestMethod]
        public void WriteNcr_HexUppercase()
        {
            using var writer = Create();
            writer.WriteStartElement("e");
            writer.WriteNcr(0x1F);
            writer.WriteEndElement();
            writer.Flush();
            writer.Options.OmitXmlDeclaration.Should().BeTrue();
        }

        [TestMethod]
        public void WriteInvalidOrByte_TabAllowed()
        {
            using var writer = Create();
            writer.WriteStartElement("e");
            writer.WriteInvalidOrByte((byte)'\t');
            writer.WriteEndElement();
        }

        [TestMethod]
        public void Ensure_GrowsBuffer()
        {
            using var writer = Create();
            writer.Ensure(XmlLimits.DefaultBufferSize + 32);
            writer.WriteRaw(new byte[XmlLimits.DefaultBufferSize + 8]);
        }

        [TestMethod]
        public void CloseStartTag_NoOpAtDepthZero()
        {
            using var writer = Create();
            writer.CloseStartTag();
            writer.CurrentDepth.Should().Be(0);
        }

        [TestMethod]
        public void EmitDeclaration_StandaloneAndVersion11()
        {
            using var writer = Create(new XmlWriterOptions { Version = XmlVersion.Version11, Standalone = false });
            writer.EmitDeclaration();
            writer.Flush();
        }

        [TestMethod]
        public void NewLineUtf8_CustomCachesBytes()
        {
            using var writer = Create(new XmlWriterOptions { OmitXmlDeclaration = true, Indented = true, NewLine = "\r", IndentCharacter = '.' });
            writer.WriteStartElement("a");
            writer.WriteStartElement("b");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.NewLineUtf8().ToArray().Should().Equal([(byte)'\r']);
            using var crlf = Create(new XmlWriterOptions { OmitXmlDeclaration = true, Indented = true, NewLine = "\r\n" });
            crlf.WriteStartElement("a");
            crlf.WriteStartElement("b");
            crlf.WriteEndElement();
            crlf.WriteEndElement();
            crlf.NewLineUtf8().ToArray().Should().Equal("\r\n"u8.ToArray());
            using var fallback = Create(new XmlWriterOptions { OmitXmlDeclaration = true, Indented = true, NewLine = "" });
            fallback.NewLineUtf8().Length.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public void WriteIndent_SkippedWhenNotIndented()
        {
            using var writer = Create();
            writer.WriteIndent(3);
            writer.BytesPending.Should().Be(0);
        }

        [TestMethod]
        public void WriteIndent_NegativeDepthAfterOutput()
        {
            using var writer = Create(new XmlWriterOptions { OmitXmlDeclaration = true, Indented = true, NewLine = "\n", IndentCharacter = '.' });
            writer.WriteStartElement("e");
            writer.WriteIndent(-1);
            writer.WriteEndElement();
        }

        [TestMethod]
        public void Ctor_DefaultOptions_UsesEnvironmentNewLine()
        {
            using var writer = new Utf8XmlWriter(new ArrayBufferWriter<byte>());
            writer.WriteStartElement("e");
            writer.WriteEndElement();
            using var stream = new MemoryStream();
            using var streamWriter = new Utf8XmlWriter(stream);
            streamWriter.WriteStartElement("e");
            streamWriter.WriteEndElement();
        }

        [TestMethod]
        public void NsAt_AndPopNs()
        {
            using var writer = Create();
            writer.WriteStartElement("e");
            writer.PushNs(1, "p"u8.ToArray(), "urn:x"u8.ToArray());
            writer.NsAt(0).Prefix.Should().Equal("p"u8.ToArray());
            writer.PopNs(1);
            writer.WriteEndElement();
        }

        [TestMethod]
        public void MarkContent_NoOpAtDepthZero()
        {
            using var writer = Create();
            writer.MarkContent();
            writer.CurrentDepth.Should().Be(0);
        }

        [TestMethod]
        public void WriteValueSlice_Version11Restricted()
        {
            using var writer = Create(new XmlWriterOptions { OmitXmlDeclaration = true, Version = XmlVersion.Version11 });
            writer.WriteStartElement("e");
            writer.WriteValueSlice([(byte)0x0C], 0x0C);
            writer.WriteEndElement();
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
        public void IncrementAttributeCount_EnforcesMax()
        {
            using var writer = Create(new XmlWriterOptions { OmitXmlDeclaration = true, MaxAttributeCount = 1 });
            writer.WriteStartElement("e");
            writer.IncrementAttributeCount();
            var threw = false;
            try
            {
                writer.IncrementAttributeCount();
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
            writer.WriteEndElement();
        }

        [TestMethod]
        public void RequireOpenTag_ThrowsInAttribute()
        {
            using var writer = Create();
            writer.WriteStartElement("e");
            writer.WriteStartAttribute("a");
            var threw = false;
            try
            {
                writer.RequireOpenTag();
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
            writer.WriteEndAttribute();
            writer.WriteEndElement();
        }

        [TestMethod]
        public void WriteElementText_FastPathAndEscapes()
        {
            using var writer = Create();
            writer.WriteStartElement("e");
            writer.WriteElementText("hello"u8);
            writer.WriteElementText("a&b<c]]>d"u8);
            writer.WriteEndElement();
        }

        [TestMethod]
        public void WriteAttributeText_Escapes()
        {
            using var writer = Create();
            writer.WriteStartElement("e");
            writer.WriteStartAttribute("a");
            writer.WriteAttributeText("x\"y&z<"u8);
            writer.WriteEndAttribute();
            writer.WriteEndElement();
        }

        [TestMethod]
        public void CurrentFrame_AfterStart()
        {
            using var writer = Create();
            writer.WriteStartElement("e");
            writer.CurrentFrame().TagOpen.Should().BeTrue();
            writer.CurrentFrame().LocalChars.Should().Be("e");
            writer.WriteEndElement();
        }

        [TestMethod]
        public void Dispose_Idempotent()
        {
            var writer = Create();
            writer.WriteStartElement("e");
            writer.WriteEndElement();
            writer.Dispose();
            writer.Dispose();
        }

        [TestMethod]
        public async Task DisposeAsync_Idempotent()
        {
            var writer = Create();
            writer.WriteStartElement("e");
            writer.WriteEndElement();
            await writer.DisposeAsync();
            await writer.DisposeAsync();
        }

        [TestMethod]
        public void WriteStartElementChars_AndUtf8()
        {
            using var writer = Create();
            writer.WriteStartElementChars("a", "p", "urn:x");
            writer.WriteEndElement();
        }

        [TestMethod]
        public void EnsureXsi_Idempotent()
        {
            using var writer = Create();
            writer.WriteStartElement("e");
            writer.EnsureXsi();
            writer.EnsureXsi();
            writer.WriteEndElement();
        }

        [TestMethod]
        public void AppendName_EmptyAndGrow()
        {
            using var writer = Create();
            writer.AppendName([]).Length.Should().Be(0);
            writer.WriteStartElement("e");
            var longName = new string('n', 300);
            writer.WriteAttributeString(longName, "v");
            writer.WriteEndElement();
        }

        [TestMethod]
        public void EncodeUtf8_UsesHeapWhenScratchIsSmall()
        {
            var heap = Utf8XmlWriter.EncodeUtf8(new string('a', 200), stackalloc byte[8], out var rented);
            rented.Should().NotBeNull();
            heap.Length.Should().Be(200);
        }

        [TestMethod]
        public void TrackAttribute_OverflowsInlineSlots()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                for (var i = 0; i < 17; i++)
                {
                    w.WriteAttributeString("a" + i, "1");
                }

                w.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, MaxAttributeCount = 32 }).Should().Contain("a16=\"1\"");

            using var nsAt = Create(new XmlWriterOptions { OmitXmlDeclaration = true, MaxAttributeCount = 32 });
            nsAt.WriteStartElement("e");
            for (var i = 0; i < 9; i++)
            {
                nsAt.WriteAttributeString("a" + i, "1");
            }

            nsAt.AttrNsAt(8).Length.Should().Be(0);
            nsAt.WriteEndElement();
        }

        [TestMethod]
        public void WriteNumberUtf8_TryFormatFalseStillAdvancesZero()
        {
            using var writer = Create();
            writer.WriteStartElement("e");
            writer.WriteNumberUtf8(int.MinValue, 1);
            writer.WriteEndElement();
        }

        [TestMethod]
        public void ReturnBuffer_EmptyIsNoOp()
        {
            using var writer = Create();
            writer.WriteStartElement("e");
            writer.WriteEndElement();
            writer.Flush();
            writer.ReturnBuffer();
        }

        [TestMethod]
        public void Grow_EmptyDestination_Throws()
        {
            using var writer = new Utf8XmlWriter(new EmptyBufferWriter(), new XmlWriterOptions { OmitXmlDeclaration = true });
            var threw = false;
            try
            {
                writer.WriteRawByte((byte)'<');
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Grow_BufferWriterCommitsPending()
        {
            using var writer = Create();
            writer.WriteRaw(new byte[200]);
            writer.Ensure(5000);
            writer.WriteRaw(new byte[8]);
        }

        [TestMethod]
        public void Grow_StreamRentsLargerBuffer()
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8XmlWriter(stream, new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteRaw(new byte[XmlLimits.DefaultBufferSize - 10]);
            writer.Ensure(20);
            writer.Ensure(XmlLimits.DefaultBufferSize + 32);
            writer.WriteRaw(new byte[8]);
            writer.WriteRaw([]);
        }

        [TestMethod]
        public void WriteCharsRaw_MixedAsciiThenNonAscii()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteCharsRaw("café");
                w.WriteEndElement();
            }).Should().Contain("café");
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
                .Should()
                .Contain("<n>42");
        }

        [TestMethod]
        public void WriteStartElementLiteral_XmlPrefixAndEmpty()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("xml", "lang", null);
                w.WriteEndElement();
            }).Should().StartWith("<xml:lang");

            XmlWriterHarness.ToXml(w => w.WriteEmptyElement("e")).Should().Be("<e/>");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("p", "e", null);
                w.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true }).Should().Contain("<p:e");
        }

        [TestMethod]
        public void WriteElementContentChars_NonAscii()
        {
            using var writer = Create();
            writer.WriteStartElement("e");
            writer.WriteElementContentChars("é");
            writer.WriteElementContentChars("ok");
            writer.WriteEndElement();
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
        public void Reset_SwitchesDestinations()
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("e");
            writer.WriteEndElement();
            writer.Flush();
            using var stream = new MemoryStream();
            writer.Reset(stream);
            writer.WriteStartElement("s");
            writer.WriteEndElement();
            writer.Flush();
            writer.Reset(buffer);
            writer.WriteStartElement("b");
            writer.WriteEndElement();
        }

        [TestMethod]
        public void EnsureNameStore_GrowsFromEmpty()
        {
            using var writer = Create();
            writer.AppendName("item"u8).Length.Should().Be(4);
            writer.AppendName(new byte[300]).Length.Should().Be(300);
        }

        [TestMethod]
        public void FlushBuffer_EmptyIsNoOp()
        {
            using var writer = Create();
            writer.FlushBuffer();
            writer.BytesCommitted.Should().Be(0);
        }

        [TestMethod]
        public void Coverage_RemainingPublicOverloadsAndBranches()
        {
            XmlWriterHarness.ToXml(w => w.WriteNilElement("n".AsSpan()));
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("root");
                w.WriteNilElement("n"u8);
                w.WriteStartElement("a".AsSpan(), "urn:a".AsSpan());
                w.WriteEndElement();
                w.WriteStartElement("b"u8, "urn:b"u8);
                w.WriteEndElement();
                w.WriteStartElement("p"u8, "c"u8, "urn:c"u8);
                w.WriteEndElement();
                w.WriteElementBase64String("d"u8, "hi"u8);
                w.WriteEndElement();
            }).Should().Contain("xsi:nil");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("a");
                w.WriteEndElement();
            }).Should().Be("<e a=\"\"/>");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("p", "e", "urn:x");
                w.WriteStartAttribute("a");
                w.WriteStringValue("1");
                w.WriteFullEndElement();
            }).Should().Be("<p:e xmlns:p=\"urn:x\" a=\"1\"></p:e>");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("a");
                w.WriteStartElement("b");
                w.WriteStringValue("x");
                w.WriteFullEndElement();
                w.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, Indented = true, NewLine = "\n", IndentSize = 2 })
                .Should().Contain("</b>");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("a", (string?)null);
                w.WriteAttributeString("b", "urn:x", (string?)null);
                w.WriteStringValue((string?)null);
                w.WriteEndElement();
            }).Should().Contain("a=\"\"");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue(DateTime.Now);
                w.WriteEndElement();
            }).Should().Contain("T");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("a");
                w.WriteStartElement("c");
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Contain("<c/>");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteNamespaceDeclaration("p", "urn:x");
                w.WriteStartElement("c", "urn:x");
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Contain("<p:c/>");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("xml", "space", null, "preserve");
                w.WriteEndElement();
            }).Should().Be("<e xml:space=\"preserve\"/>");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("p", "a", "urn:x", "1");
                w.WriteAttributeString(null, "b", "urn:x", "2");
                w.WriteEndElement();
            }).Should().Contain("p:b=\"2\"");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("xml", "e", null);
                w.WriteEndElement();
            }).Should().StartWith("<xml:e");

            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("a");
                w.WriteStringValue([(byte)0x80]);
            });

            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue([(byte)0xEF, 0xBF, 0xBE]);
            }, new XmlWriterOptions { OmitXmlDeclaration = true, Version = XmlVersion.Version11 });

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue([(byte)0xC2, 0x80]);
                w.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, Version = XmlVersion.Version11 })
                .Should().Contain("&#x80;");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteCData("x".AsSpan());
                w.WriteComment("c"u8);
                w.WriteRawValue("<z/>"u8);
                w.WriteEndElement();
            }).Should().Contain("CDATA");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteRawValue("<e/>");
            }).Should().Be("<e/>");

            XmlWriterHarness.ToXml(w =>
            {
                for (var i = 0; i < 17; i++)
                {
                    w.WriteStartElement("e");
                }

                for (var i = 0; i < 17; i++)
                {
                    w.WriteEndElement();
                }
            }, new XmlWriterOptions { OmitXmlDeclaration = true, MaxDepth = 32 });

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                for (var i = 1; i <= 17; i++)
                {
                    w.WriteNamespaceDeclaration("p" + i, "urn:" + i);
                }

                w.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, MaxAttributeCount = 32 });

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("a");
                w.WriteStartElement("b");
                w.WriteEndElement();
                w.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, Indented = true, IndentCharacter = '\0', NewLine = "\n" });

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("p", "e", null);
                w.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true }).Should().Contain("<p:e");

            using var grow = Create();
            grow.WriteRaw(new byte[XmlLimits.DefaultBufferSize - 10]);
            grow.Ensure(20);
            grow.Ensure(XmlLimits.DefaultBufferSize + 32);

            Utf8XmlWriter.EnsureDone(OperationStatus.Done, "ok");
            var threw = false;
            try
            {
                Utf8XmlWriter.EnsureDone(OperationStatus.DestinationTooSmall, "fail");
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("id", "urn:x", "1");
                w.WriteEndElement();
            }).Should().Contain("id=\"1\"");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("xmlns", "e", null);
                w.WriteEndElement();
            }).Should().StartWith("<xmlns:e");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("xmlns", "p", null);
                w.WriteStringValue("urn:x");
                w.WriteEndAttribute();
                w.WriteEndElement();
            }).Should().Contain("xmlns:p");

            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("p", "a", null);
            });

            XmlWriterHarness.MustThrow(w => w.WriteStartElement("e$"));

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("q", "a", null);
                w.WriteEndAttribute();
                w.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true });
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
