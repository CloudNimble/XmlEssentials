using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Behavior of <see cref="Utf8XmlWriter"/> overloads and states, each stated as exact output.
    /// </summary>
    [TestClass]
    public class Utf8XmlWriterBehaviorTests
    {

        #region Public Methods

        [TestMethod]
        public void NilElement_FromCharsAndUtf8()
        {
            const string expected = "<n xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>";
            XmlWriterHarness.ToXml(w => w.WriteNilElement("n".AsSpan())).Should().Be(expected);
            XmlWriterHarness.ToXml(w => w.WriteNilElement("n"u8)).Should().Be(expected);
        }

        [TestMethod]
        public void StartElement_CharsWithNamespace_DeclaresDefault()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e".AsSpan(), "urn:x".AsSpan());
                w.WriteEndElement();
            }).Should().Be("<e xmlns=\"urn:x\"/>");
        }

        [TestMethod]
        public void OpenAttribute_IsClosedByTheNextStructuralCall()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("a");
                w.WriteStringValue("1");
                w.WriteEndElement();
            }).Should().Be("<e a=\"1\"/>");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("a");
                w.WriteStringValue("1");
                w.WriteFullEndElement();
            }).Should().Be("<e a=\"1\"></e>");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("a");
                w.WriteStringValue("1");
                w.WriteStartElement("c");
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Be("<e a=\"1\"><c/></e>");

            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("a");
                w.WriteStringValue("1");
                w.WriteStartElement("c"u8);
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Be("<e a=\"1\"><c/></e>");
        }

        [TestMethod]
        public void Base64Element_Utf8Name()
        {
            XmlWriterHarness.ToXml(w => w.WriteElementBase64String("b"u8, "hi"u8)).Should().Be("<b>aGk=</b>");
        }

        [TestMethod]
        public void StringValue_Null_WritesNothing()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue((string?)null);
                w.WriteEndElement();
            }).Should().Be("<e/>");
        }

        [TestMethod]
        [DataRow(1, 1, 2, 0, 0, 0, 0L, "0001-01-02T00:00:00", DisplayName = "a year of one digit is padded to four")]
        [DataRow(2020, 1, 2, 3, 4, 5, 0L, "2020-01-02T03:04:05", DisplayName = "a whole second has no fraction")]
        [DataRow(2020, 1, 2, 3, 4, 5, 5_000_000L, "2020-01-02T03:04:05.5", DisplayName = "one fraction digit")]
        [DataRow(2020, 1, 2, 3, 4, 5, 1_200_000L, "2020-01-02T03:04:05.12", DisplayName = "trailing zeros are dropped")]
        [DataRow(2020, 1, 2, 3, 4, 5, 1L, "2020-01-02T03:04:05.0000001", DisplayName = "one tick keeps its leading zeros")]
        [DataRow(2020, 12, 31, 23, 59, 59, 1_234_567L, "2020-12-31T23:59:59.1234567", DisplayName = "seven fraction digits")]
        [DataRow(9999, 12, 30, 23, 59, 59, 9_999_999L, "9999-12-30T23:59:59.9999999", DisplayName = "the last tick of a second in the last year")]
        public void DateAndTimeValues_AreWrittenInTheirSchemaForm(int year, int month, int day, int hour, int minute, int second, long fractionTicks, string expected)
        {
            var value = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified).AddTicks(fractionTicks);
            Element(w => w.WriteStringValue(value)).Should().Be($"<e>{expected}</e>");
            Element(w => w.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc))).Should().Be($"<e>{expected}Z</e>");
            Element(w => w.WriteStringValue(new DateTimeOffset(value, TimeSpan.Zero))).Should().Be($"<e>{expected}+00:00</e>");
            Element(w => w.WriteStringValue(new DateTimeOffset(value, new TimeSpan(5, 30, 0)))).Should().Be($"<e>{expected}+05:30</e>");
            Element(w => w.WriteStringValue(new DateTimeOffset(value, new TimeSpan(-8, 0, 0)))).Should().Be($"<e>{expected}-08:00</e>");
            Element(w => w.WriteStringValue(DateOnly.FromDateTime(value))).Should().Be($"<e>{expected[..10]}</e>");
            Element(w => w.WriteStringValue(TimeOnly.FromDateTime(value))).Should().Be($"<e>{expected[11..]}</e>");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("a");
                w.WriteStringValue(value);
                w.WriteEndAttribute();
                w.WriteEndElement();
            }).Should().Be($"<e a=\"{expected}\"/>");
            XmlLexical.TryParseDateTime(Encoding.UTF8.GetBytes(expected), out var parsed).Should().BeTrue();
            parsed.Should().Be(value);
        }

        [TestMethod]
        public void DateTime_KindSelectsTheZone()
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
                w.WriteStringValue(new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1_234_500));
                w.WriteEndElement();
            }).Should().Be("<e>2020-01-02T03:04:05.12345Z</e>");

            var local = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Local);
            var offset = TimeZoneInfo.Local.GetUtcOffset(local);
            var zone = (offset < TimeSpan.Zero ? "-" : "+") + offset.ToString(@"hh\:mm", System.Globalization.CultureInfo.InvariantCulture);
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue(local);
                w.WriteEndElement();
            }).Should().Be("<e>2020-01-02T03:04:05" + zone + "</e>");
        }

        [TestMethod]
        public void ElementPrefix_Xmlns_IsRejectedUnlessValidationIsOff()
        {
            XmlWriterHarness.MustThrow(w => w.WriteStartElement("xmlns"u8, "e"u8, "urn:x"u8));
            XmlWriterHarness.MustThrow(w => w.WriteStartElement("xmlns"u8, "e"u8, []));
            XmlWriterHarness.MustThrow(w => w.WriteStartElement("xmlns", "e", "urn:x"));
            XmlWriterHarness.MustThrow(w => w.WriteStartElement("xmlns", "e", null));
            XmlWriterHarness.MustThrow(w => w.WriteEmptyElement("xmlns", "e", "urn:x"));
            XmlWriterHarness.MustThrow(w => w.WriteEmptyElement("xmlns"u8, "e"u8, []));

            var unchecked1 = new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true };
            XmlWriterHarness.ToXml(
                w =>
                {
                    w.WriteStartElement("xmlns"u8, "e"u8, []);
                    w.WriteEndElement();
                },
                unchecked1).Should().Be("<xmlns:e/>");
            XmlWriterHarness.ToXml(
                w =>
                {
                    w.WriteStartElement("xmlns", "e", null);
                    w.WriteEndElement();
                },
                unchecked1).Should().Be("<xmlns:e/>");
            XmlWriterHarness.ToXml(w => w.WriteEmptyElement("xmlns", "e", null), unchecked1).Should().Be("<xmlns:e/>");
        }

        [TestMethod]
        public void Attribute_WithReservedPrefixAndNoNamespace_UsesTheReservedNamespace()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("xml", "lang", null, "en");
                w.WriteAttributeString("xmlns", "p", null, "urn:x");
                w.WriteEndElement();
            }).Should().Be("<e xml:lang=\"en\" xmlns:p=\"urn:x\"/>");
        }

        [TestMethod]
        public void MultiByteText_IsCopiedExceptCharactersThatNeedAReference()
        {
            var nextLine = ((char)0x85).ToString();
            var lineSeparator = ((char)0x2028).ToString();
            var value = "a\u00E9☃" + nextLine + "𝄞" + lineSeparator + "ü<\u00E9";
            var version10 = new XmlWriterOptions { OmitXmlDeclaration = true, Version = XmlVersion.Version10 };
            var version11 = new XmlWriterOptions { OmitXmlDeclaration = true, Version = XmlVersion.Version11 };

            XmlWriterHarness.ToXml(w => w.WriteElementString("e", value), version10)
                .Should().Be("<e>" + "a\u00E9☃" + nextLine + "𝄞" + lineSeparator + "ü&lt;\u00E9</e>");
            XmlWriterHarness.ToXml(w => w.WriteElementString("e", value), version11)
                .Should().Be("<e>a\u00E9☃&#x85;𝄞&#x2028;ü&lt;\u00E9</e>");
            XmlWriterHarness.ToXml(
                w =>
                {
                    w.WriteStartElement("e");
                    w.WriteAttributeString("a", value);
                    w.WriteEndElement();
                },
                version11).Should().Be("<e a=\"a\u00E9☃&#x85;𝄞&#x2028;ü&lt;\u00E9\"/>");
        }

        [TestMethod]
        public void PrefixedAttributes_ReuseBindingsAcrossShadowingAndScopeExit()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("p", "r", "urn:1");
                w.WriteAttributeString("p", "x", "urn:1", "1");
                w.WriteStartElement("p", "a", "urn:2");
                w.WriteAttributeString("p", "x", "urn:2", "2");
                w.WriteEndElement();
                w.WriteStartElement("p", "b", "urn:1");
                w.WriteAttributeString("p", "x", "urn:1", "3");
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Be("<p:r xmlns:p=\"urn:1\" p:x=\"1\"><p:a xmlns:p=\"urn:2\" p:x=\"2\"/><p:b p:x=\"3\"/></p:r>");
        }

        [TestMethod]
        public void SiblingNamespaceDeclarations_BeyondTheReuseLimit_AreWrittenCorrectly()
        {
            var expected = new StringBuilder("<r>");
            var xml = XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("r");
                for (var i = 0; i < 80; i++)
                {
                    w.WriteStartElement("p" + i, "e", "urn:" + i);
                    w.WriteEndElement();
                }

                w.WriteStartElement("p0", "e", "urn:0");
                w.WriteEndElement();
                w.WriteStartElement("p79", "e", "urn:79");
                w.WriteEndElement();
                w.WriteEndElement();
            });
            for (var i = 0; i < 80; i++)
            {
                expected.Append("<p").Append(i).Append(":e xmlns:p").Append(i).Append("=\"urn:").Append(i).Append("\"/>");
            }

            expected.Append("<p0:e xmlns:p0=\"urn:0\"/><p79:e xmlns:p79=\"urn:79\"/></r>");
            xml.Should().Be(expected.ToString());
        }

        [TestMethod]
        public void AttributeValue_InvalidUtf8_Throws()
        {
            byte[] truncated = [(byte)'a', (byte)'&', 0xC3];
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("a"u8, truncated);
            });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue(truncated);
            });
        }

        [TestMethod]
        public void Value_NonCharacter_Throws_InBothVersions()
        {
            var nonCharacter = ((char)0xFFFF).ToString();
            foreach (var version in new[] { XmlVersion.Version10, XmlVersion.Version11 })
            {
                var options = new XmlWriterOptions { OmitXmlDeclaration = true, Version = version };
                XmlWriterHarness.MustThrow(w => w.WriteElementString("e", nonCharacter), options);
                XmlWriterHarness.MustThrow(
                    w =>
                    {
                        w.WriteStartElement("e");
                        w.WriteAttributeString("a", nonCharacter);
                    },
                    options);
            }
        }

        [TestMethod]
        public void Indent_DefaultStruct_UsesTwoSpaces()
        {
            var options = default(XmlWriterOptions);
            options.Indented = true;
            options.NewLine = "\n";
            options.OmitXmlDeclaration = true;
            XmlWriterHarness.ToXml(
                w =>
                {
                    w.WriteStartElement("a");
                    w.WriteStartElement("b");
                    w.WriteEmptyElement("c");
                    w.WriteEndElement();
                    w.WriteEndElement();
                },
                options).Should().Be("<a>\n  <b>\n    <c/>\n  </b>\n</a>");
        }

        [TestMethod]
        public void DeepNesting_BeyondInlineFrames()
        {
            var expected = new StringBuilder();
            var xml = XmlWriterHarness.ToXml(w =>
            {
                for (var i = 0; i < 40; i++)
                {
                    w.WriteStartElement("e" + i);
                }

                w.WriteStringValue("x");
                for (var i = 0; i < 40; i++)
                {
                    w.WriteEndElement();
                }
            });
            for (var i = 0; i < 40; i++)
            {
                expected.Append("<e").Append(i).Append('>');
            }

            expected.Append('x');
            for (var i = 39; i >= 0; i--)
            {
                expected.Append("</e").Append(i).Append('>');
            }

            xml.Should().Be(expected.ToString());
        }

        [TestMethod]
        public void Options_ReturnsTheOptionsTheWriterWasCreatedWith()
        {
            var options = new XmlWriterOptions { Indented = true, IndentSize = 3, MaxDepth = 7, OmitXmlDeclaration = true, Version = XmlVersion.Version11 };
            using var writer = new Utf8XmlWriter(new ArrayBufferWriter<byte>(), options);
            writer.Options.Should().Be(options);
        }

        [TestMethod]
        [DataRow(10, DisplayName = "a value that fits the buffer the writer starts with")]
        [DataRow(40_000, DisplayName = "a value larger than the buffer the writer starts with")]
        [DataRow(300_000, DisplayName = "a value several times larger")]
        public void Stream_ValuesOfAnySize_AreWrittenWhole(int length)
        {
            var text = new string('x', length) + "<&>" + new string('\u00E9', length);
            var expected = "<r a=\"" + new string('x', length) + "&lt;&amp;>" + new string('\u00E9', length) + "\"><e>" + new string('x', length) + "&lt;&amp;>" + new string('\u00E9', length) + "</e><e>" + new string('x', length) + "&lt;&amp;>" + new string('\u00E9', length) + "</e></r>";
            using var stream = new MemoryStream();
            using (var writer = new Utf8XmlWriter(stream, new XmlWriterOptions { OmitXmlDeclaration = true }))
            {
                writer.WriteStartElement("r");
                writer.WriteAttributeString("a", text);
                writer.WriteElementString("e", text);
                writer.WriteStartElement("e");
                writer.WriteStringValue(text);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            Encoding.UTF8.GetString(stream.ToArray()).Should().Be(expected);
        }

        [TestMethod]
        public void BufferWriter_ADocumentLargerThanOneBlock_IsWrittenWhole()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var expected = new StringBuilder("<r>");
            using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true }))
            {
                writer.WriteStartElement("r");
                for (var i = 0; i < 5000; i++)
                {
                    writer.WriteStartElement("item");
                    writer.WriteAttributeString("id", i.ToString(CultureInfo.InvariantCulture));
                    writer.WriteStringValue("value " + i);
                    writer.WriteEndElement();
                    expected.Append("<item id=\"").Append(i).Append("\">value ").Append(i).Append("</item>");
                }

                writer.WriteEndElement();
            }

            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be(expected.Append("</r>").ToString());
        }

        [TestMethod]
        public void WriteNilElement_WhenTheSchemaInstanceNamespaceIsInScope_DoesNotDeclareItAgain()
        {
            const string xsi = "http://www.w3.org/2001/XMLSchema-instance";
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("xsi", "root", xsi);
                writer.WriteNilElement("a");
                writer.WriteStartElement("b");
                writer.WriteNilElement("c");
                writer.WriteEndElement();
                writer.WriteEndElement();
            }).Should().Be("<xsi:root xmlns:xsi=\"" + xsi + "\"><a xsi:nil=\"true\"/><b><c xsi:nil=\"true\"/></b></xsi:root>");
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("root");
                writer.WriteNilElement("a");
                writer.WriteNilElement("b");
                writer.WriteEndElement();
            }).Should().Be("<root><a xmlns:xsi=\"" + xsi + "\" xsi:nil=\"true\"/><b xmlns:xsi=\"" + xsi + "\" xsi:nil=\"true\"/></root>");
        }

        [TestMethod]
        public void DocumentLevel_CommentsAndProcessingInstructions_AreWrittenAroundTheRoot()
        {
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteComment("before");
                writer.WriteProcessingInstruction("p", "one");
                writer.WriteStartElement("r");
                writer.WriteEndElement();
                writer.WriteComment("after");
                writer.WriteProcessingInstruction("q", null);
            }).Should().Be("<!--before--><?p one?><r/><!--after--><?q?>");
        }

        [TestMethod]
        public void Names_ThatStartInAsciiAndGoOnOutsideIt_AreWrittenWhole()
        {
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("abc\u00E9", "urn:n");
                writer.WriteAttributeString("pr\u00E9", "nom\u00E9", "urn:a", "v");
                writer.WriteStartElement("pr\u00E9", "local\u4E2D", "urn:a");
                writer.WriteEndElement();
                writer.WriteEndElement();
            }).Should().Be("<abc\u00E9 xmlns=\"urn:n\" xmlns:pr\u00E9=\"urn:a\" pr\u00E9:nom\u00E9=\"v\"><pr\u00E9:local\u4E2D/></abc\u00E9>");
        }

        [TestMethod]
        public void TenAttributesInNamespaces_AreAllWrittenAndARepeatAmongThemIsRejected()
        {
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("r");
                for (var i = 0; i < 10; i++)
                {
                    writer.WriteAttributeString("p", "a" + i, "urn:p", i.ToString(CultureInfo.InvariantCulture));
                }

                writer.WriteEndElement();
            }).Should().Be("<r xmlns:p=\"urn:p\" p:a0=\"0\" p:a1=\"1\" p:a2=\"2\" p:a3=\"3\" p:a4=\"4\" p:a5=\"5\" p:a6=\"6\" p:a7=\"7\" p:a8=\"8\" p:a9=\"9\"/>");
            XmlWriterHarness.ThrownMessage(writer =>
            {
                writer.WriteStartElement("r");
                for (var i = 0; i < 10; i++)
                {
                    writer.WriteAttributeString("p", "a" + i, "urn:p", "v");
                }

                writer.WriteAttributeString("p", "a9", "urn:p", "again");
            }).Should().Be("Duplicate attribute.");
        }

        [TestMethod]
        [DataRow("\n", DisplayName = "LF")]
        [DataRow("\r\n", DisplayName = "CR LF")]
        public void Indented_UsesTheNewLineItIsGiven(string newLine)
        {
            var options = new XmlWriterOptions { OmitXmlDeclaration = true, Indented = true, IndentSize = 1, NewLine = newLine };
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("a");
                writer.WriteStartElement("b");
                writer.WriteStartElement("c");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
            }, options).Should().Be("<a>" + newLine + " <b>" + newLine + "  <c/>" + newLine + " </b>" + newLine + "</a>");
        }

        [TestMethod]
        public void Indented_WithNoNewLineGiven_UsesTheEnvironmentOne()
        {
            // As System.Text.Json has it: the value is never null, in default(XmlWriterOptions) either.
            foreach (var options in new[] { default, new XmlWriterOptions() })
            {
                XmlWriterHarness.ToXml(writer =>
                {
                    writer.WriteStartElement("a");
                    writer.WriteStartElement("b");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }, options with { OmitXmlDeclaration = true, Indented = true, IndentSize = 2 }).Should().Be("<a>" + Environment.NewLine + "  <b/>" + Environment.NewLine + "</a>");
            }
        }

        [TestMethod]
        [DataRow("", DisplayName = "an empty string")]
        [DataRow("\r", DisplayName = "a carriage return alone")]
        [DataRow("\n\n", DisplayName = "two line feeds")]
        [DataRow(" | ", DisplayName = "any other text")]
        public void NewLine_ThatIsNotALineEnding_IsRejected(string newLine)
        {
            // The two values System.Text.Json accepts. The writer uses the value for the line breaks in content
            // too, and these are the only two a reader gives back as a line feed.
            FluentActions.Invoking(() => new XmlWriterOptions { NewLine = newLine })
                .Should().ThrowExactly<ArgumentOutOfRangeException>().Which.Message.Should().Be("The line ending must be \"\\n\" or \"\\r\\n\". (Parameter 'value')");
            FluentActions.Invoking(() => new XmlSerializerOptions { NewLine = newLine })
                .Should().ThrowExactly<ArgumentOutOfRangeException>().Which.Message.Should().Be("The line ending must be \"\\n\" or \"\\r\\n\". (Parameter 'value')");
            FluentActions.Invoking(() => new XmlWriterOptions { NewLine = null! })
                .Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("value");
        }

        [TestMethod]
        public void NamespaceGivenWithoutAPrefix_UsesAPrefixAlreadyBoundToIt()
        {
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("p", "r", "urn:p");
                writer.WriteAttributeString("a", "urn:p", "1");
                writer.WriteAttributeString("lang", XmlNamespaces.Xml, "en");
                writer.WriteEndElement();
            }).Should().Be("<p:r xmlns:p=\"urn:p\" p:a=\"1\" xml:lang=\"en\"/>");
        }

        [TestMethod]
        public void NameTakingMethods_WriteNamesThatStartInAsciiAndGoOnOutsideIt()
        {
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("r\u00E9");
                writer.WriteAttributeString("a\u00E9", "1");
                writer.WriteElementString("e\u00E9", "v");
                writer.WriteEmptyElement("m\u00E9");
                writer.WriteNilElement("n\u00E9");
                writer.WriteElementNumber("i\u00E9", 5);
                writer.WriteElementBoolean("b\u00E9", true);
                writer.WriteProcessingInstruction("t\u00E9", "d");
                writer.WriteEndElement();
            }).Should().Be("<r\u00E9 a\u00E9=\"1\"><e\u00E9>v</e\u00E9><m\u00E9/><n\u00E9 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><i\u00E9>5</i\u00E9><b\u00E9>true</b\u00E9><?t\u00E9 d?></r\u00E9>");
        }

        [TestMethod]
        public void AttributeInTheXmlnsNamespace_GivenWithoutAPrefix_IsWrittenAsTheDeclarationItIs()
        {
            // The xmlns namespace holds namespace declarations and nothing else (Namespaces in XML, section 3),
            // so an attribute x in it is the declaration xmlns:x.
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("r");
                writer.WriteAttributeString("x", XmlNamespaces.Xmlns, "urn:x");
                writer.WriteEndElement();
            }).Should().Be("<r xmlns:x=\"urn:x\"/>");
        }

        [TestMethod]
        public void SeventeenAttributes_AreWrittenInOrder()
        {
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("e");
                for (var i = 0; i < 17; i++)
                {
                    writer.WriteAttributeString("a" + i, "1");
                }

                writer.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, MaxAttributeCount = 32 }).Should().Be("<e a0=\"1\" a1=\"1\" a2=\"1\" a3=\"1\" a4=\"1\" a5=\"1\" a6=\"1\" a7=\"1\" a8=\"1\" a9=\"1\" a10=\"1\" a11=\"1\" a12=\"1\" a13=\"1\" a14=\"1\" a15=\"1\" a16=\"1\"/>");
        }

        [TestMethod]
        public void UnboundAttributeNamespace_UsesTheNextFreeNsPrefix()
        {
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("e");
                writer.WriteNamespaceDeclaration("ns1", "urn:1");
                writer.WriteAttributeString("a", "urn:2", "1");
                writer.WriteEndElement();
            }).Should().Be("<e xmlns:ns1=\"urn:1\" xmlns:ns2=\"urn:2\" ns2:a=\"1\"/>");
        }

        [TestMethod]
        public void AttributeInADefaultOnlyNamespace_GetsAGeneratedPrefix()
        {
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("e");
                writer.WriteDefaultNamespace("urn:d");
                writer.WriteAttributeString("a", "urn:d", "1");
                writer.WriteEndElement();
            }).Should().Be("<e xmlns=\"urn:d\" xmlns:ns1=\"urn:d\" ns1:a=\"1\"/>");
        }

        [TestMethod]
        public void LeafElement_InsideAnElement_IsWrittenBetweenTheTagsItIsGiven()
        {
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("r");
                writer.WriteLeafStart("<a>"u8);
                writer.WriteNumberValue(1);
                writer.WriteLeafEnd("</a>"u8);
                writer.WriteLeafStart("<b>"u8);
                writer.WriteStringValue("x<y");
                writer.WriteLeafEnd("</b>"u8);
                writer.WriteEndElement();
            }).Should().Be("<r><a>1</a><b>x&lt;y</b></r>");
        }

        [TestMethod]
        public void LeafElement_OutsideAnyElement_IsRejected()
        {
            XmlWriterHarness.ThrownMessage(writer => writer.WriteLeafStart("<a>"u8)).Should().Be("A leaf element must be written inside an element.");
            XmlWriterHarness.ThrownMessage(writer => writer.WriteLeafEnd("</a>"u8)).Should().Be("A leaf element must be written inside an element.");
            XmlWriterHarness.ThrownMessage(writer =>
            {
                writer.WriteStartElement("r");
                writer.WriteEndElement();
                writer.WriteLeafStart("<a>"u8);
            }).Should().Be("A leaf element must be written inside an element.");
        }

        [TestMethod]
        public void EmptyName_GivenAsUtf8_IsRejected()
        {
            XmlWriterHarness.ThrownMessage(writer => writer.WriteStartElement(""u8)).Should().Be("Name is empty.");
            XmlWriterHarness.ThrownMessage(writer => writer.WriteStartElement("")).Should().Be("Name is empty.");
        }

        [TestMethod]
        public void BufferWriter_ThatHandsBackNoMemory_IsReportedAsTooSmall()
        {
            FluentActions.Invoking(() =>
            {
                using var writer = new Utf8XmlWriter(new EmptyBufferWriter(), new XmlWriterOptions { OmitXmlDeclaration = true });
                writer.WriteStartElement("e");
                writer.WriteEndElement();
            }).Should().ThrowExactly<XmlException>().WithMessage("The destination buffer is too small.");
        }

        [TestMethod]
        public void SkipValidation_WritesANameThatIsNotAName_AsItIsGiven()
        {
            var options = new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true };
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("1 name that is no Name at all"u8);
                writer.WriteEndElement();
            }, options).Should().Be("<1 name that is no Name at all/>");
            string? message = null;
            try
            {
                XmlWriterHarness.ToXml(writer => writer.WriteStartElement("1 name that is no Name at all"u8));
            }
            catch (XmlException ex)
            {
                message = ex.Message;
            }

            message.Should().Be("Invalid XML name.");
        }

        [TestMethod]
        public void SkipValidation_WritesAnAttributeNameThatIsNotAName_AsItIsGiven()
        {
            var options = new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true };
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("e");
                writer.WriteAttributeString("1 a", "v");
                writer.WriteAttributeNumber("2 b", 7);
                writer.WriteEndElement();
            }, options).Should().Be("<e 1 a=\"v\" 2 b=\"7\"/>");
            XmlWriterHarness.ThrownMessage(writer =>
            {
                writer.WriteStartElement("e");
                writer.WriteAttributeString("1 a", "v");
            }).Should().Be("Invalid XML name.");
        }

        [TestMethod]
        public void SkipValidation_StillEscapesValues_AndKeepsTheChecksThatAreNotAboutNames()
        {
            var options = new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true };
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("r");
                writer.WriteAttributeString("a", "<\"&");
                writer.WriteStringValue("a<b&c");
                writer.WriteEndElement();
            }, options).Should().Be("<r a=\"&lt;&quot;&amp;\">a&lt;b&amp;c</r>");

            static void InRoot(Utf8XmlWriter writer, Action<Utf8XmlWriter> write)
            {
                writer.WriteStartElement("r");
                write(writer);
            }

            XmlWriterHarness.ThrownMessage(writer => InRoot(writer, w => w.WriteStringValue("a\u0001b")), options)
                .Should().Be("Illegal character in XML value.");
            XmlWriterHarness.ThrownMessage(writer => InRoot(writer, w => w.WriteAttributeString("a", "a\u0001b")), options)
                .Should().Be("Illegal character in XML value.");
            XmlWriterHarness.ThrownMessage(writer => InRoot(writer, w => w.WriteComment("a--b")), options)
                .Should().Be("Comments must not contain '--' or end with '-'.");
            XmlWriterHarness.ThrownMessage(writer => InRoot(writer, w => w.WriteCData("a]]>b")), options)
                .Should().Be("CDATA must not contain ']]>'.");
            XmlWriterHarness.ThrownMessage(writer => InRoot(writer, w => w.WriteProcessingInstruction("t", "a?>b")), options)
                .Should().Be("Processing instruction data must not contain '?>'.");

            XmlWriterHarness.ThrownMessage(writer =>
            {
                writer.WriteEmptyElement("a");
                writer.WriteEmptyElement("b");
            }, options).Should().Be("Multiple root elements are not allowed.");
            XmlWriterHarness.ThrownMessage(writer => writer.WriteStringValue("x"), options)
                .Should().Be("Text is not allowed at the document level.");
            XmlWriterHarness.ThrownMessage(writer => writer.WriteCData("x"), options)
                .Should().Be("CDATA is not allowed at the document level.");

            XmlWriterHarness.ThrownMessage(writer =>
            {
                writer.WriteStartElement("a");
                writer.WriteStartElement("a");
                writer.WriteStartElement("a");
            }, options with { MaxDepth = 2 }).Should().Be("MaxDepth exceeded (2).");
            XmlWriterHarness.ThrownMessage(writer =>
            {
                writer.WriteStartElement("a");
                writer.WriteAttributeString("x", "1");
                writer.WriteAttributeString("y", "1");
                writer.WriteAttributeString("z", "1");
            }, options with { MaxAttributeCount = 2 }).Should().Be("MaxAttributeCount exceeded (2).");
        }

        [TestMethod]
        public void SkipValidation_WritesWhatTheOtherChecksWouldRefuse_AsItIsGiven()
        {
            var options = new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true };
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("r");
                writer.WriteAttributeString("a", "1");
                writer.WriteAttributeString("a", "2");
                writer.WriteComment("a\u0001b");
                writer.WriteCData("c\u0001d");
                writer.WriteProcessingInstruction("xml", "e\u0001f");
                writer.WriteRawValue("<open>");
                writer.WriteEndElement();
            }, options).Should().Be("<r a=\"1\" a=\"2\"><!--a\u0001b--><![CDATA[c\u0001d]]><?xml e\u0001f?><open></r>");
            XmlWriterHarness.ToXml(writer => writer.WriteEmptyElement(new string('a', 40)), options with { MaxNameByteCount = 8 })
                .Should().Be("<" + new string('a', 40) + "/>");
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("r");
                writer.WriteStartElement("open");
            }, options).Should().Be("<r><open");
        }

        [TestMethod]
        [DataRow(XmlVersion.Version10)]
        [DataRow(XmlVersion.Version11)]
        public void Names_OfAnyLength_AreWrittenTheSameByEveryOverload(XmlVersion version)
        {
            // A short ASCII name is checked as it is copied, and a longer one is checked on its own first.
            // The lengths here are on both sides of where one way gives over to the other.
            var options = new XmlWriterOptions { OmitXmlDeclaration = true, Version = version };
            foreach (var length in (int[])[1, 2, 15, 16, 17, 31, 32, 33, 64, 300])
            {
                var name = "n" + new string('a', length - 1);
                var utf8Name = Encoding.UTF8.GetBytes(name);
                var encodedName = XmlEncodedText.Encode(name);
                var element = "<" + name + "/>";
                XmlWriterHarness.ToXml(writer =>
                {
                    writer.WriteStartElement(name);
                    writer.WriteEndElement();
                }, options).Should().Be(element);
                XmlWriterHarness.ToXml(writer =>
                {
                    writer.WriteStartElement(name.AsSpan());
                    writer.WriteEndElement();
                }, options).Should().Be(element);
                XmlWriterHarness.ToXml(writer =>
                {
                    writer.WriteStartElement(utf8Name);
                    writer.WriteEndElement();
                }, options).Should().Be(element);
                XmlWriterHarness.ToXml(writer =>
                {
                    writer.WriteStartElement(encodedName);
                    writer.WriteEndElement();
                }, options).Should().Be(element);
                XmlWriterHarness.ToXml(writer => writer.WriteEmptyElement(name), options).Should().Be(element);
                var number = "<r><" + name + ">7</" + name + "></r>";
                XmlWriterHarness.ToXml(writer => WriteInRoot(writer, w => w.WriteElementNumber(name, 7)), options).Should().Be(number);
                XmlWriterHarness.ToXml(writer => WriteInRoot(writer, w => w.WriteElementNumber(name.AsSpan(), 7)), options).Should().Be(number);
                XmlWriterHarness.ToXml(writer => WriteInRoot(writer, w => w.WriteElementNumber(utf8Name, 7)), options).Should().Be(number);
                XmlWriterHarness.ToXml(writer => WriteInRoot(writer, w => w.WriteElementNumber(encodedName, 7)), options).Should().Be(number);
                var text = "<r><" + name + ">v</" + name + "></r>";
                XmlWriterHarness.ToXml(writer => WriteInRoot(writer, w => w.WriteElementString(name, "v")), options).Should().Be(text);
                XmlWriterHarness.ToXml(writer => WriteInRoot(writer, w => w.WriteElementString(encodedName, "v")), options).Should().Be(text);
                XmlWriterHarness.ToXml(writer =>
                {
                    writer.WriteStartElement(name);
                    writer.WriteStartElement(name);
                    writer.WriteStringValue("v");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }, options).Should().Be("<" + name + "><" + name + ">v</" + name + "></" + name + ">");
                XmlWriterHarness.ToXml(writer =>
                {
                    writer.WriteStartElement("e");
                    writer.WriteAttributeString(name, "v");
                    writer.WriteAttributeNumber(name + "b", 7);
                    writer.WriteStartAttribute(name + "c");
                    writer.WriteStringValue("w");
                    writer.WriteEndAttribute();
                    writer.WriteEndElement();
                }, options).Should().Be("<e " + name + "=\"v\" " + name + "b=\"7\" " + name + "c=\"w\"/>");
                XmlWriterHarness.ThrownMessage(writer =>
                {
                    writer.WriteStartElement("e");
                    writer.WriteAttributeString(name, "v");
                    writer.WriteAttributeNumber(name, 7);
                }, options).Should().Be("Duplicate attribute.");
                XmlWriterHarness.ThrownMessage(writer =>
                {
                    writer.WriteStartElement("e");
                    writer.WriteAttributeString(name + " ", "v");
                }, options).Should().Be("Invalid XML name.");
                XmlWriterHarness.ThrownMessage(writer => writer.WriteStartElement(name + " "), options).Should().Be("Invalid XML name.");
            }
        }

        [TestMethod]
        [DataRow(XmlVersion.Version10)]
        [DataRow(XmlVersion.Version11)]
        public void ElementsWrittenWhole_TakeANameOutsideAscii_InEveryNameForm(XmlVersion version)
        {
            var options = new XmlWriterOptions { OmitXmlDeclaration = true, Version = version };
            const string name = "café";
            var utf8Name = Encoding.UTF8.GetBytes(name);
            var encodedName = XmlEncodedText.Encode(name);
            const string number = "<r><café>7</café></r>";
            XmlWriterHarness.ToXml(writer => WriteInRoot(writer, w => w.WriteElementNumber(name, 7)), options).Should().Be(number);
            XmlWriterHarness.ToXml(writer => WriteInRoot(writer, w => w.WriteElementNumber(name.AsSpan(), 7)), options).Should().Be(number);
            XmlWriterHarness.ToXml(writer => WriteInRoot(writer, w => w.WriteElementNumber(utf8Name, 7)), options).Should().Be(number);
            XmlWriterHarness.ToXml(writer => WriteInRoot(writer, w => w.WriteElementNumber(encodedName, 7)), options).Should().Be(number);
            const string text = "<r><café>v</café></r>";
            XmlWriterHarness.ToXml(writer => WriteInRoot(writer, w => w.WriteElementString(name, "v")), options).Should().Be(text);
            XmlWriterHarness.ToXml(writer => WriteInRoot(writer, w => w.WriteElementString(encodedName, "v")), options).Should().Be(text);
            XmlWriterHarness.ThrownMessage(writer => WriteInRoot(writer, w => w.WriteElementNumber("café "u8, 7)), options).Should().Be("Invalid XML name.");
            XmlWriterHarness.ThrownMessage(writer => WriteInRoot(writer, w => w.WriteElementString(XmlEncodedText.Encode("café "), "v")), options).Should().Be("Invalid XML name.");
        }

        [TestMethod]
        public void NameBuffer_HoldsOnlyTheNamesOfOpenElements()
        {
            // A name is kept from the start tag to the end tag and then let go. A writer that kept every name
            // it was given would grow with the document.
            using var writer = new Utf8XmlWriter(new ArrayBufferWriter<byte>(), new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("root");
            writer.NameBytes.Should().Be(4);
            for (var i = 0; i < 1_000; i++)
            {
                writer.WriteStartElement("item"u8);
                writer.WriteAttributeString("name", "v");
                writer.WriteAttributeNumber("count", i);
                writer.NameBytes.Should().Be(4 + 4 + 4 + 5);
                writer.WriteStartElement("child");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.NameBytes.Should().Be(4);
            }

            writer.WriteEndElement();
            writer.NameBytes.Should().Be(0);
        }

        [TestMethod]
        public void Attribute_AfterDispose_IsRejected()
        {
            // The start tag is still open when the writer is disposed, so only the disposed check stands
            // between these calls and a buffer that has been given back.
            var writer = new Utf8XmlWriter(new ArrayBufferWriter<byte>(), new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true });
            writer.WriteStartElement("e");
            writer.Dispose();
            FluentActions.Invoking(() => writer.WriteStartAttribute("a")).Should().ThrowExactly<ObjectDisposedException>();
            FluentActions.Invoking(() => writer.WriteStartAttribute(null, "a", "urn:a")).Should().ThrowExactly<ObjectDisposedException>();
            FluentActions.Invoking(() => writer.WriteAttributeString("a", "v")).Should().ThrowExactly<ObjectDisposedException>();
            FluentActions.Invoking(() => writer.WriteAttributeString("a"u8, "v"u8)).Should().ThrowExactly<ObjectDisposedException>();
            FluentActions.Invoking(() => writer.WriteAttributeNumber("a", 1)).Should().ThrowExactly<ObjectDisposedException>();
            FluentActions.Invoking(() => writer.WriteNamespaceDeclaration("p", "urn:p")).Should().ThrowExactly<ObjectDisposedException>();
            FluentActions.Invoking(() => writer.WriteDefaultNamespace("urn:d")).Should().ThrowExactly<ObjectDisposedException>();
            FluentActions.Invoking(writer.WriteNilValue).Should().ThrowExactly<ObjectDisposedException>();
        }

        [TestMethod]
        public void CommentsAndProcessingInstructions_WhenIndented_GoOnTheirOwnLines()
        {
            var indented = new XmlWriterOptions { OmitXmlDeclaration = true, Indented = true, NewLine = "\n", IndentSize = 2 };
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteComment("top");
                w.WriteProcessingInstruction("style", "x");
                w.WriteStartElement("a");
                w.WriteComment("in");
                w.WriteEmptyElement("b");
                w.WriteProcessingInstruction("p", "d");
                w.WriteStartElement("c");
                w.WriteComment("only");
                w.WriteEndElement();
                w.WriteEndElement();
                w.WriteComment("tail");
            }, indented).Should().Be(
                "<!--top-->\n" +
                "<?style x?>\n" +
                "<a>\n" +
                "  <!--in-->\n" +
                "  <b/>\n" +
                "  <?p d?>\n" +
                "  <c>\n" +
                "    <!--only-->\n" +
                "  </c>\n" +
                "</a>\n" +
                "<!--tail-->");
        }

        [TestMethod]
        public void CommentsAndProcessingInstructions_AfterTheDeclaration_GoOnTheirOwnLines()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteComment("top"u8);
                w.WriteEmptyElement("a");
            }, new XmlWriterOptions { Indented = true, NewLine = "\n" }).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<!--top-->\n<a/>");
        }

        [TestMethod]
        public void CommentsAndProcessingInstructions_NextToText_AreNotIndented()
        {
            // White space next to character data is part of the data, so an element that holds text is
            // written on one line, comments included.
            var indented = new XmlWriterOptions { OmitXmlDeclaration = true, Indented = true, NewLine = "\n" };
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("a");
                w.WriteStringValue("text");
                w.WriteComment("c");
                w.WriteProcessingInstruction("p", "d");
                w.WriteEndElement();
            }, indented).Should().Be("<a>text<!--c--><?p d?></a>");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("a");
                w.WriteComment("c");
                w.WriteStringValue("text");
                w.WriteEndElement();
            }, indented).Should().Be("<a>\n  <!--c-->text</a>");
        }

        [TestMethod]
        public void CommentsAndProcessingInstructions_WhenNotIndented_AreWrittenInPlace()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteComment("top");
                w.WriteStartElement("a");
                w.WriteComment("in");
                w.WriteProcessingInstruction("p", "d");
                w.WriteEndElement();
            }).Should().Be("<!--top--><a><!--in--><?p d?></a>");
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Dispose_WithAnElementOpen_WritesWhatItHasAndThenThrows()
        {
            // The caller gets the error and keeps the output written so far, in the stream or the buffer.
            using var stream = new MemoryStream();
            var toStream = new Utf8XmlWriter(stream, new XmlWriterOptions { OmitXmlDeclaration = true });
            toStream.WriteStartElement("a");
            toStream.WriteStringValue("text");
            FluentActions.Invoking(toStream.Dispose).Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The document has unclosed elements.");
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be("<a>text");

            var buffer = new ArrayBufferWriter<byte>();
            var toBuffer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            toBuffer.WriteStartElement("a");
            toBuffer.WriteStringValue("text");
            FluentActions.Invoking(toBuffer.Dispose).Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The document has unclosed elements.");
            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<a>text");

            using var asyncStream = new MemoryStream();
            var asyncWriter = new Utf8XmlWriter(asyncStream, new XmlWriterOptions { OmitXmlDeclaration = true });
            asyncWriter.WriteStartElement("a");
            asyncWriter.WriteStringValue("text");
            (await FluentActions.Awaiting(async () => await asyncWriter.DisposeAsync()).Should().ThrowExactlyAsync<XmlException>()).Which.Message.Should().Be("The document has unclosed elements.");
            Encoding.UTF8.GetString(asyncStream.ToArray()).Should().Be("<a>text");
            FluentActions.Invoking(() => asyncWriter.WriteEndElement()).Should().ThrowExactly<ObjectDisposedException>();
        }

        [TestMethod]
        [DataRow('x')]
        [DataRow('\n')]
        [DataRow('\r')]
        [DataRow(' ')]
        [DataRow(' ')]
        public void IndentCharacter_ThatIsNotASpaceOrATab_IsRejected(char character)
        {
            // Anything else would be written into the document as text, or cut down to one byte.
            FluentActions.Invoking(() => new XmlWriterOptions { IndentCharacter = character })
                .Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlSerializerOptions { IndentCharacter = character })
                .Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
        }

        [TestMethod]
        public void IndentCharacter_SpaceOrTab_IsUsed()
        {
            foreach (var character in new[] { ' ', '\t' })
            {
                var indent = new string(character, 3);
                XmlWriterHarness.ToXml(w =>
                {
                    w.WriteStartElement("a");
                    w.WriteEmptyElement("b");
                    w.WriteEndElement();
                }, new XmlWriterOptions { OmitXmlDeclaration = true, Indented = true, NewLine = "\n", IndentSize = 3, IndentCharacter = character })
                    .Should().Be("<a>\n" + indent + "<b/>\n</a>");
                XmlSerializer.Serialize(new int[] { 1 }, new XmlSerializerOptions { OmitXmlDeclaration = true, WriteIndented = true, NewLine = "\n", IndentSize = 3, IndentCharacter = character })
                    .Should().Be("<ArrayOfInt>\n" + indent + "<int>1</int>\n</ArrayOfInt>");
            }
        }

        [TestMethod]
        public void Limits_ThatAreNegative_AreRejected()
        {
            // 0 means the default. A negative limit means nothing, and used to surface as "MaxDepth exceeded (-1)".
            FluentActions.Invoking(() => new XmlWriterOptions { MaxDepth = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlWriterOptions { MaxAttributeCount = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlWriterOptions { MaxNameByteCount = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlWriterOptions { IndentSize = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlReaderOptions { MaxDepth = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlReaderOptions { MaxAttributeCount = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlReaderOptions { MaxCommentByteCount = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlReaderOptions { MaxDocumentByteCount = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlReaderOptions { MaxNameByteCount = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlReaderOptions { MaxTextByteCount = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlDocumentOptions { MaxDepth = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlDocumentOptions { MaxAttributeCount = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlDocumentOptions { MaxCommentByteCount = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlDocumentOptions { MaxDocumentByteCount = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlDocumentOptions { MaxNameByteCount = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlDocumentOptions { MaxTextByteCount = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlSerializerOptions { MaxDepth = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlSerializerOptions { MaxAttributeCount = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlSerializerOptions { MaxCommentByteCount = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlSerializerOptions { MaxDocumentByteCount = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlSerializerOptions { MaxNameByteCount = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlSerializerOptions { MaxTextByteCount = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlSerializerOptions { DefaultBufferSize = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
            FluentActions.Invoking(() => new XmlSerializerOptions { IndentSize = -1 }).Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("value");
        }

        [TestMethod]
        public void Limits_LeftAtZero_AreTheDefaults()
        {
            new XmlWriterOptions { MaxDepth = 0, MaxAttributeCount = 0, MaxNameByteCount = 0, IndentSize = 0 }.MaxDepth.Should().Be(0);
            XmlWriterHarness.ThrownMessage(w =>
            {
                for (var i = 0; i < 65; i++)
                {
                    w.WriteStartElement("d");
                }
            }, new XmlWriterOptions { OmitXmlDeclaration = true, MaxDepth = 0 }).Should().Be("MaxDepth exceeded (64).");
        }

        [TestMethod]
        [DataRow("D83D", DisplayName = "a high surrogate alone")]
        [DataRow("DE00", DisplayName = "a low surrogate alone")]
        [DataRow("0061D83D0062", DisplayName = "a high surrogate between letters")]
        [DataRow("DE00D83D", DisplayName = "a pair in the wrong order")]
        [DataRow("0061D83D", DisplayName = "a high surrogate at the end")]
        public void HalfASurrogatePair_IsRejectedWhereverTextIsWritten(string units)
        {
            // Half a pair is not a character and has no UTF-8 form. It used to be written as U+FFFD, so the
            // document read back differed from the text that was written, with no error.
            const string message = "The text contains half of a surrogate pair.";
            var text = new string(Array.ConvertAll(Enumerable.Range(0, units.Length / 4).ToArray(), i => (char)Convert.ToUInt16(units.Substring(i * 4, 4), 16)));
            XmlWriterHarness.ThrownMessage(w => { w.WriteStartElement("e"); w.WriteStringValue(text); }).Should().Be(message);
            XmlWriterHarness.ThrownMessage(w => { w.WriteStartElement("e"); w.WriteStringValue(text.AsSpan()); }).Should().Be(message);
            XmlWriterHarness.ThrownMessage(w => { w.WriteStartElement("e"); w.WriteStringValueSegment(text.AsSpan(), true); }).Should().Be(message);
            XmlWriterHarness.ThrownMessage(w => { w.WriteStartElement("e"); w.WriteAttributeString("a", text); }).Should().Be(message);
            XmlWriterHarness.ThrownMessage(w => { w.WriteStartElement("e"); w.WriteStartAttribute("a"); w.WriteStringValue(text); }).Should().Be(message);
            XmlWriterHarness.ThrownMessage(w => w.WriteElementString("e", text)).Should().Be(message);
            XmlWriterHarness.ThrownMessage(w => w.WriteComment(text)).Should().Be(message);
            XmlWriterHarness.ThrownMessage(w => { w.WriteStartElement("e"); w.WriteCData(text); }).Should().Be(message);
            XmlWriterHarness.ThrownMessage(w => w.WriteProcessingInstruction("p", text)).Should().Be(message);
            XmlWriterHarness.ThrownMessage(w => { w.WriteStartElement("e"); w.WriteRawValue(text, skipInputValidation: true); }).Should().Be(message);
            FluentActions.Invoking(() => XmlSerializer.Serialize(text)).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message + " Path: /string.");
            FluentActions.Invoking(() => XmlSerializer.Serialize(new POCOs.Person { Name = text })).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message + " Path: /Person.");
        }

        [TestMethod]
        public void SurrogatePair_IsWrittenAsOneCharacter()
        {
            var pair = new string([(char)0xD83D, (char)0xDE00]);
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("a", pair);
                w.WriteStringValue("x" + pair + "y");
                w.WriteEndElement();
            }).Should().Be("<e a=\"" + pair + "\">x" + pair + "y</e>");
        }

        [TestMethod]
        public void StringValue_AfterDispose_IsRejected()
        {
            // Every overload checks, including the ones that copy text that needs no escaping straight through.
            var writer = new Utf8XmlWriter(new ArrayBufferWriter<byte>(), new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true });
            writer.WriteStartElement("e");
            writer.Dispose();
            FluentActions.Invoking(() => writer.WriteStringValue("plain")).Should().ThrowExactly<ObjectDisposedException>();
            FluentActions.Invoking(() => writer.WriteStringValue("a<b")).Should().ThrowExactly<ObjectDisposedException>();
            FluentActions.Invoking(() => writer.WriteStringValue("plain".AsSpan())).Should().ThrowExactly<ObjectDisposedException>();
            FluentActions.Invoking(() => writer.WriteStringValue("a<b".AsSpan())).Should().ThrowExactly<ObjectDisposedException>();
            FluentActions.Invoking(() => writer.WriteStringValue("plain"u8)).Should().ThrowExactly<ObjectDisposedException>();
            FluentActions.Invoking(() => writer.WriteStringValueSegment("plain"u8, true)).Should().ThrowExactly<ObjectDisposedException>();
            FluentActions.Invoking(() => writer.WriteStringValueSegment("plain".AsSpan(), true)).Should().ThrowExactly<ObjectDisposedException>();
            FluentActions.Invoking(() => writer.WriteBase64StringSegment([1], true)).Should().ThrowExactly<ObjectDisposedException>();
        }

        [TestMethod]
        [DataRow(false, DisplayName = "to a buffer writer")]
        [DataRow(true, DisplayName = "to a stream")]
        public void StringValue_LongerThanTheBuffer_IsWrittenWhole(bool toStream)
        {
            // Text that is copied as it is, text outside ASCII, and text that is escaped each take their own way
            // into the output, and each is longer than the writer's buffer here.
            var plain = new string('a', 100_000);
            var accented = string.Concat(Enumerable.Repeat("é", 50_000));
            var escaped = string.Concat(Enumerable.Repeat("a&b<", 25_000));
            var escapedAs = string.Concat(Enumerable.Repeat("a&amp;b&lt;", 25_000));
            var options = new XmlWriterOptions { OmitXmlDeclaration = true };

            using var stream = new MemoryStream();
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = toStream ? new Utf8XmlWriter(stream, options) : new Utf8XmlWriter(buffer, options))
            {
                writer.WriteStartElement("e");
                writer.WriteAttributeString("a", escaped);
                writer.WriteStartElement("plain");
                writer.WriteStringValue(plain);
                writer.WriteEndElement();
                writer.WriteStartElement("accented");
                writer.WriteStringValue(accented.AsSpan());
                writer.WriteEndElement();
                writer.WriteStartElement("escaped");
                writer.WriteStringValue(escaped);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            var written = Encoding.UTF8.GetString(toStream ? stream.ToArray() : buffer.WrittenSpan);
            written.Should().Be($"<e a=\"{escapedAs}\"><plain>{plain}</plain><accented>{accented}</accented><escaped>{escapedAs}</escaped></e>");
        }

        [TestMethod]
        public void StringValue_WithASurrogatePairWhereAPartEnds_KeepsThePair()
        {
            // Escaped text is transcoded a part at a time. A pair is never cut in two, wherever it falls.
            for (var before = 330; before <= 350; before++)
            {
                var text = new string('a', before) + "\U0001F600&z";
                var buffer = new ArrayBufferWriter<byte>();
                using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true }))
                {
                    writer.WriteStartElement("e");
                    writer.WriteAttributeString("a", text);
                    writer.WriteStringValue(text);
                    writer.WriteEndElement();
                }

                var expected = new string('a', before) + "\U0001F600&amp;z";
                Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be($"<e a=\"{expected}\">{expected}</e>", "{0} characters come before the pair", before);
            }

            var half = new string('a', 340) + "\uD83D&z";
            var broken = new Utf8XmlWriter(new ArrayBufferWriter<byte>(), new XmlWriterOptions { OmitXmlDeclaration = true });
            broken.WriteStartElement("e");
            FluentActions.Invoking(() => broken.WriteStringValue(half))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The text contains half of a surrogate pair.");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Writes one value as the content of an <c>e</c> element.
        /// </summary>
        /// <param name="write">Writes the value.</param>
        /// <returns>The document.</returns>
        private static string Element(Action<Utf8XmlWriter> write)
        {
            return XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                write(w);
                w.WriteEndElement();
            });
        }

        /// <summary>
        /// Writes an <c>r</c> element around what <paramref name="write"/> writes.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="write">Writes the content.</param>
        private static void WriteInRoot(Utf8XmlWriter writer, Action<Utf8XmlWriter> write)
        {
            writer.WriteStartElement("r");
            write(writer);
            writer.WriteEndElement();
        }

        #endregion

    }

}
