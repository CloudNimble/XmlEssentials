using System;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// The element helpers of <see cref="Utf8XmlWriter"/> take a name as a string, as characters, as UTF-8
    /// bytes and as an <see cref="XmlEncodedText"/>, as the property helpers of <c>Utf8JsonWriter</c> do.
    /// Every form of a name writes the same element.
    /// </summary>
    [TestClass]
    public class Utf8XmlWriterElementOverloadTests
    {

        #region Fields

        private static readonly XmlEncodedText Encoded = XmlEncodedText.Encode("n");

        #endregion

        #region Public Methods

        [TestMethod]
        public void WriteElementBase64String_AndWriteElementBoolean_TakeAnEncodedName()
        {
            WritesTheSame("<n>aGk=</n>",
                w => w.WriteElementBase64String("n", "hi"u8),
                w => w.WriteElementBase64String("n".AsSpan(), "hi"u8),
                w => w.WriteElementBase64String("n"u8, "hi"u8),
                w => w.WriteElementBase64String(Encoded, "hi"u8));
            WritesTheSame("<n>true</n>",
                w => w.WriteElementBoolean("n", true),
                w => w.WriteElementBoolean("n".AsSpan(), true),
                w => w.WriteElementBoolean("n"u8, true),
                w => w.WriteElementBoolean(Encoded, true));
        }

        [TestMethod]
        public void WriteElementNumber_EveryNameForm_WritesTheSameElement()
        {
            WritesTheSame("<n>-42</n>",
                w => w.WriteElementNumber("n", -42),
                w => w.WriteElementNumber("n".AsSpan(), -42),
                w => w.WriteElementNumber("n"u8, -42),
                w => w.WriteElementNumber(Encoded, -42));
            WritesTheSame("<n>-9223372036854775808</n>",
                w => w.WriteElementNumber("n", long.MinValue),
                w => w.WriteElementNumber("n".AsSpan(), long.MinValue),
                w => w.WriteElementNumber("n"u8, long.MinValue),
                w => w.WriteElementNumber(Encoded, long.MinValue));
            WritesTheSame("<n>4294967295</n>",
                w => w.WriteElementNumber("n", uint.MaxValue),
                w => w.WriteElementNumber("n".AsSpan(), uint.MaxValue),
                w => w.WriteElementNumber("n"u8, uint.MaxValue),
                w => w.WriteElementNumber(Encoded, uint.MaxValue));
            WritesTheSame("<n>18446744073709551615</n>",
                w => w.WriteElementNumber("n", ulong.MaxValue),
                w => w.WriteElementNumber("n".AsSpan(), ulong.MaxValue),
                w => w.WriteElementNumber("n"u8, ulong.MaxValue),
                w => w.WriteElementNumber(Encoded, ulong.MaxValue));
            WritesTheSame("<n>12.50</n>",
                w => w.WriteElementNumber("n", 12.50m),
                w => w.WriteElementNumber("n".AsSpan(), 12.50m),
                w => w.WriteElementNumber("n"u8, 12.50m),
                w => w.WriteElementNumber(Encoded, 12.50m));
            WritesTheSame("<n>1.5</n>",
                w => w.WriteElementNumber("n", 1.5d),
                w => w.WriteElementNumber("n".AsSpan(), 1.5d),
                w => w.WriteElementNumber("n"u8, 1.5d),
                w => w.WriteElementNumber(Encoded, 1.5d));
            WritesTheSame("<n>0.25</n>",
                w => w.WriteElementNumber("n", 0.25f),
                w => w.WriteElementNumber("n".AsSpan(), 0.25f),
                w => w.WriteElementNumber("n"u8, 0.25f),
                w => w.WriteElementNumber(Encoded, 0.25f));
        }

        [TestMethod]
        public void WriteElementNumber_NamedFloatingPointValues_AreWrittenTheSameInEveryNameForm()
        {
            WritesTheSame("<n>NaN</n>",
                w => w.WriteElementNumber("n", double.NaN),
                w => w.WriteElementNumber("n".AsSpan(), double.NaN),
                w => w.WriteElementNumber("n"u8, double.NaN),
                w => w.WriteElementNumber(Encoded, double.NaN));
            WritesTheSame("<n>INF</n>",
                w => w.WriteElementNumber("n", double.PositiveInfinity),
                w => w.WriteElementNumber("n".AsSpan(), double.PositiveInfinity),
                w => w.WriteElementNumber("n"u8, double.PositiveInfinity),
                w => w.WriteElementNumber(Encoded, double.PositiveInfinity));
            WritesTheSame("<n>-INF</n>",
                w => w.WriteElementNumber("n", double.NegativeInfinity),
                w => w.WriteElementNumber("n".AsSpan(), double.NegativeInfinity),
                w => w.WriteElementNumber("n"u8, double.NegativeInfinity),
                w => w.WriteElementNumber(Encoded, double.NegativeInfinity));
            WritesTheSame("<n>NaN</n>",
                w => w.WriteElementNumber("n", float.NaN),
                w => w.WriteElementNumber("n".AsSpan(), float.NaN),
                w => w.WriteElementNumber("n"u8, float.NaN),
                w => w.WriteElementNumber(Encoded, float.NaN));
            WritesTheSame("<n>INF</n>",
                w => w.WriteElementNumber("n", float.PositiveInfinity),
                w => w.WriteElementNumber("n".AsSpan(), float.PositiveInfinity),
                w => w.WriteElementNumber("n"u8, float.PositiveInfinity),
                w => w.WriteElementNumber(Encoded, float.PositiveInfinity));
            WritesTheSame("<n>-INF</n>",
                w => w.WriteElementNumber("n", float.NegativeInfinity),
                w => w.WriteElementNumber("n".AsSpan(), float.NegativeInfinity),
                w => w.WriteElementNumber("n"u8, float.NegativeInfinity),
                w => w.WriteElementNumber(Encoded, float.NegativeInfinity));
        }

        [TestMethod]
        public void WriteElementString_DatesAndGuids_AreWrittenTheSameInEveryNameForm()
        {
            var dateTime = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            WritesTheSame("<n>2020-01-02T03:04:05Z</n>",
                w => w.WriteElementString("n", dateTime),
                w => w.WriteElementString("n".AsSpan(), dateTime),
                w => w.WriteElementString("n"u8, dateTime),
                w => w.WriteElementString(Encoded, dateTime));

            var offset = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.FromHours(-8));
            WritesTheSame("<n>2020-01-02T03:04:05-08:00</n>",
                w => w.WriteElementString("n", offset),
                w => w.WriteElementString("n".AsSpan(), offset),
                w => w.WriteElementString("n"u8, offset),
                w => w.WriteElementString(Encoded, offset));

            var guid = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e");
            WritesTheSame("<n>0f8fad5b-d9cb-469f-a165-70867728950e</n>",
                w => w.WriteElementString("n", guid),
                w => w.WriteElementString("n".AsSpan(), guid),
                w => w.WriteElementString("n"u8, guid),
                w => w.WriteElementString(Encoded, guid));
        }

        [TestMethod]
        public void WriteElementString_NullName_IsRefused()
        {
            XmlWriterHarness.ToXml(w =>
            {
                FluentActions.Invoking(() => w.WriteElementString((string)null!, "v".AsSpan())).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("localName");
                FluentActions.Invoking(() => w.WriteElementString((string)null!, "v"u8)).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("localName");
                w.WriteEmptyElement("r");
            }).Should().Be("<r/>");
        }

        [TestMethod]
        public void WriteElementString_NullValue_WritesAnEmptyElementInTheStyleOfTheOptions()
        {
            foreach (var (style, expected) in new[]
            {
                (XmlEmptyElementStyle.SelfClosing, "<n/>"),
                (XmlEmptyElementStyle.SelfClosingWithSpace, "<n />"),
                (XmlEmptyElementStyle.StartAndEndTag, "<n></n>"),
            })
            {
                var options = new XmlWriterOptions { OmitXmlDeclaration = true, EmptyElementStyle = style };
                XmlWriterHarness.ToXml(w => w.WriteElementString("n", (string?)null), options).Should().Be(expected);
                XmlWriterHarness.ToXml(w => w.WriteElementString("n".AsSpan(), (string?)null), options).Should().Be(expected);
                XmlWriterHarness.ToXml(w => w.WriteElementString("n"u8, (string?)null), options).Should().Be(expected);
                XmlWriterHarness.ToXml(w => w.WriteElementString(Encoded, (string?)null), options).Should().Be(expected);
            }
        }

        [TestMethod]
        public void WriteElementString_Text_IsEscapedTheSameInEveryNameAndValueForm()
        {
            WritesTheSame("<n>a&lt;b &amp; é</n>",
                w => w.WriteElementString("n", "a<b & é"),
                w => w.WriteElementString("n", "a<b & é".AsSpan()),
                w => w.WriteElementString("n", "a<b & é"u8),
                w => w.WriteElementString("n".AsSpan(), "a<b & é"),
                w => w.WriteElementString("n".AsSpan(), "a<b & é".AsSpan()),
                w => w.WriteElementString("n".AsSpan(), "a<b & é"u8),
                w => w.WriteElementString("n"u8, "a<b & é"),
                w => w.WriteElementString("n"u8, "a<b & é".AsSpan()),
                w => w.WriteElementString("n"u8, "a<b & é"u8),
                w => w.WriteElementString(Encoded, "a<b & é"),
                w => w.WriteElementString(Encoded, "a<b & é".AsSpan()),
                w => w.WriteElementString(Encoded, "a<b & é"u8));
        }

        [TestMethod]
        public void WriteElementString_TextThatIsEmpty_WritesTheSameElementInEveryNameAndValueForm()
        {
            WritesTheSame("<n></n>",
                w => w.WriteElementString("n", string.Empty),
                w => w.WriteElementString("n", ReadOnlySpan<char>.Empty),
                w => w.WriteElementString("n", ReadOnlySpan<byte>.Empty),
                w => w.WriteElementString("n".AsSpan(), string.Empty),
                w => w.WriteElementString("n".AsSpan(), ReadOnlySpan<char>.Empty),
                w => w.WriteElementString("n".AsSpan(), ReadOnlySpan<byte>.Empty),
                w => w.WriteElementString("n"u8, string.Empty),
                w => w.WriteElementString("n"u8, ReadOnlySpan<char>.Empty),
                w => w.WriteElementString("n"u8, ReadOnlySpan<byte>.Empty),
                w => w.WriteElementString(Encoded, string.Empty),
                w => w.WriteElementString(Encoded, ReadOnlySpan<char>.Empty),
                w => w.WriteElementString(Encoded, ReadOnlySpan<byte>.Empty));
        }

        [TestMethod]
        public void WriteNilElement_TakesAnEncodedName()
        {
            const string expected = "<n xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>";
            XmlWriterHarness.ToXml(w => w.WriteNilElement("n")).Should().Be(expected);
            XmlWriterHarness.ToXml(w => w.WriteNilElement(Encoded)).Should().Be(expected);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks that every one of <paramref name="forms"/> writes <paramref name="element"/>: as the document
        /// element, inside an element that declares a default namespace, and in an indented document.
        /// </summary>
        /// <param name="element">The element every form is expected to write.</param>
        /// <param name="forms">The calls that write the element, one for each form of its name and its value.</param>
        private static void WritesTheSame(string element, params Action<Utf8XmlWriter>[] forms)
        {
            var indented = new XmlWriterOptions { OmitXmlDeclaration = true, Indented = true, NewLine = "\n" };
            foreach (var form in forms)
            {
                XmlWriterHarness.ToXml(form).Should().Be(element);
                XmlWriterHarness.ToXml(w => InsideADefaultNamespace(w, form)).Should().Be("<r xmlns=\"urn:d\">" + element + element + "</r>");
                XmlWriterHarness.ToXml(w => InsideADefaultNamespace(w, form), indented).Should().Be("<r xmlns=\"urn:d\">\n  " + element + "\n  " + element + "\n</r>");
            }
        }

        /// <summary>
        /// Writes an element in a default namespace and calls <paramref name="form"/> twice inside it.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="form">The call that writes one child element.</param>
        private static void InsideADefaultNamespace(Utf8XmlWriter writer, Action<Utf8XmlWriter> form)
        {
            writer.WriteStartElement("r", "urn:d");
            form(writer);
            form(writer);
            writer.WriteEndElement();
        }

        #endregion

    }

}
