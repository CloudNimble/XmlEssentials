using System;
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
            var value = "aé☃" + nextLine + "𝄞" + lineSeparator + "ü<é";
            var version10 = new XmlWriterOptions { OmitXmlDeclaration = true, Version = XmlVersion.Version10 };
            var version11 = new XmlWriterOptions { OmitXmlDeclaration = true, Version = XmlVersion.Version11 };

            XmlWriterHarness.ToXml(w => w.WriteElementString("e", value), version10)
                .Should().Be("<e>" + "aé☃" + nextLine + "𝄞" + lineSeparator + "ü&lt;é</e>");
            XmlWriterHarness.ToXml(w => w.WriteElementString("e", value), version11)
                .Should().Be("<e>aé☃&#x85;𝄞&#x2028;ü&lt;é</e>");
            XmlWriterHarness.ToXml(
                w =>
                {
                    w.WriteStartElement("e");
                    w.WriteAttributeString("a", value);
                    w.WriteEndElement();
                },
                version11).Should().Be("<e a=\"aé☃&#x85;𝄞&#x2028;ü&lt;é\"/>");
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

        #endregion

    }

}
