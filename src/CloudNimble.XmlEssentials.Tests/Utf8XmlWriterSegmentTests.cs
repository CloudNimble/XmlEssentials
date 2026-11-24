using System;
using System.Buffers;
using System.IO;
using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A value written in segments is the value written in one call, wherever the segments are cut: inside a
    /// multi-byte character, between the halves of a surrogate pair, inside a base64 group, inside <c>]]&gt;</c>.
    /// </summary>
    [TestClass]
    public class Utf8XmlWriterSegmentTests
    {

        #region Fields

        private const string OpenMessage = "A value is being written in segments. Write its final segment before anything else.";

        #endregion

        #region Public Methods

        [TestMethod]
        public void Base64Segments_OneByteEach_WriteOneValue()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteBase64StringSegment([1], false);
                w.WriteBase64StringSegment([2], true);
                w.WriteEndElement();
            }).Should().Be("<e>AQI=</e>");
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataRow(6)]
        [DataRow(7)]
        [DataRow(100)]
        public void Base64Segments_AtEverySplit_MatchOneCall(int length)
        {
            var data = new byte[length];
            for (var i = 0; i < length; i++)
            {
                data[i] = (byte)((i * 37) + 11);
            }

            foreach (var attribute in new[] { false, true })
            {
                var expected = (attribute ? "<e a=\"" : "<e>") + Convert.ToBase64String(data) + (attribute ? "\"/>" : "</e>");
                if (length == 0 && !attribute)
                {
                    // An element that was given an empty value has content, so it is not written as <e/>.
                    expected = "<e></e>";
                }

                Outcome(w => InValue(w, attribute, () => w.WriteBase64StringValue(data)), XmlVersion.Version10)
                    .Should().Be(expected);
                for (var split = 0; split <= length; split++)
                {
                    var cut = split;
                    Outcome(w => InValue(w, attribute, () =>
                    {
                        w.WriteBase64StringSegment(data.AsSpan(0, cut), false);
                        w.WriteBase64StringSegment(data.AsSpan(cut), true);
                    }), XmlVersion.Version10).Should().Be(expected, "a split at byte {0} must not change the value", cut);
                }

                Outcome(w => InValue(w, attribute, () =>
                {
                    for (var i = 0; i < data.Length; i++)
                    {
                        w.WriteBase64StringSegment(data.AsSpan(i, 1), false);
                    }

                    w.WriteBase64StringSegment([], true);
                }), XmlVersion.Version10).Should().Be(expected, "one byte in each segment must not change the value");
            }
        }

        [TestMethod]
        public void BracketsThenGreaterThan_AcrossCalls_NeverWriteTheCDataEnd()
        {
            // "]]>" must not appear in character data (XML 1.0 and 1.1 section 2.4), whichever calls its
            // three characters arrive in.
            foreach (var text in new[] { "]]>", "a]]]>>", "]]>]]>", "]>]]>" })
            {
                var utf8 = Encoding.UTF8.GetBytes(text);
                var expected = XmlWriterHarness.ToXml(w =>
                {
                    w.WriteStartElement("e");
                    w.WriteStringValue(utf8);
                    w.WriteEndElement();
                });
                for (var split = 0; split <= text.Length; split++)
                {
                    var cut = split;
                    XmlWriterHarness.ToXml(w =>
                    {
                        w.WriteStartElement("e");
                        w.WriteStringValue(utf8.AsSpan(0, cut));
                        w.WriteStringValue(utf8.AsSpan(cut));
                        w.WriteEndElement();
                    }).Should().Be(expected, "UTF-8 calls split at {0} of '{1}'", cut, text);
                    XmlWriterHarness.ToXml(w =>
                    {
                        w.WriteStartElement("e");
                        w.WriteStringValue(text.AsSpan(0, cut));
                        w.WriteStringValue(text.AsSpan(cut));
                        w.WriteEndElement();
                    }).Should().Be(expected, "character calls split at {0} of '{1}'", cut, text);
                    XmlWriterHarness.ToXml(w =>
                    {
                        w.WriteStartElement("e");
                        w.WriteStringValue(text[..cut]);
                        w.WriteStringValue(utf8.AsSpan(cut));
                        w.WriteEndElement();
                    }).Should().Be(expected, "a string call and a UTF-8 call split at {0} of '{1}'", cut, text);
                }
            }
        }

        [TestMethod]
        public void BracketsThenGreaterThan_ExactOutput()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue("]]");
                w.WriteStringValue(">");
                w.WriteEndElement();
            }).Should().Be("<e>]]&gt;</e>");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue("]");
                w.WriteStringValue("]>");
                w.WriteEndElement();
            }).Should().Be("<e>]]&gt;</e>");
        }

        [TestMethod]
        public void BracketsThenGreaterThan_WithSomethingBetween_IsNotEscaped()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue("]]");
                w.WriteEmptyElement("b");
                w.WriteStringValue(">");
                w.WriteEndElement();
            }).Should().Be("<e>]]<b/>></e>");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue("]]");
                w.WriteNumberValue(1);
                w.WriteStringValue(">");
                w.WriteEndElement();
            }).Should().Be("<e>]]1></e>");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("a");
                w.WriteStringValue("]]");
                w.WriteStringValue(">");
                w.WriteEndElement();
            }).Should().Be("<e a=\"]]>\"/>");
        }

        [TestMethod]
        public void BracketsThenGreaterThan_AcrossAFlush_IsEscaped()
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8XmlWriter(stream, new XmlWriterOptions { OmitXmlDeclaration = true }))
            {
                writer.WriteStartElement("e");
                writer.WriteStringValue("]]");
                writer.Flush();
                writer.WriteStringValue(">");
                writer.WriteEndElement();
            }

            Encoding.UTF8.GetString(stream.ToArray()).Should().Be("<e>]]&gt;</e>");
        }

        [TestMethod]
        public void BracketsThenGreaterThan_AfterReset_IsNotEscaped()
        {
            var first = new ArrayBufferWriter<byte>();
            var second = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(first, new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true });
            writer.WriteStartElement("e");
            writer.WriteStringValue("x]]");
            writer.Flush();
            writer.Reset(second);
            writer.WriteStartElement("e");
            writer.WriteStringValue(">");
            writer.WriteEndElement();
            writer.Flush();
            Encoding.UTF8.GetString(first.WrittenSpan).Should().Be("<e>x]]");
            Encoding.UTF8.GetString(second.WrittenSpan).Should().Be("<e>></e>");
        }

        [TestMethod]
        [DataRow("plain text", DisplayName = "ASCII")]
        [DataRow("é中😀", DisplayName = "two, three and four byte characters")]
        [DataRow("aéb中c😀d", DisplayName = "multi-byte characters between ASCII")]
        [DataRow("a{85}b{2028}c{A0}d", DisplayName = "NEL, LS and a character that shares their lead bytes")]
        [DataRow("x\r\ny\rz\n\tw", DisplayName = "line ends and a tab")]
        [DataRow("a]]>b ]] > ]]]> ]", DisplayName = "the CDATA end and its parts")]
        [DataRow("<&\"'> &amp;", DisplayName = "markup characters")]
        [DataRow("a\u0001b\u007Fc\u009Fd", DisplayName = "characters XML 1.0 rejects and XML 1.1 writes as references")]
        [DataRow("", DisplayName = "empty")]
        public void CharSegments_AtEverySplit_MatchOneCall(string encoded)
        {
            // A line separator cannot be written in a string literal, so a row gives such a character as
            // its code point in braces.
            var text = encoded.Replace("{85}", ((char)0x85).ToString()).Replace("{2028}", ((char)0x2028).ToString()).Replace("{A0}", ((char)0xA0).ToString());
            foreach (var version in new[] { XmlVersion.Version10, XmlVersion.Version11 })
            {
                foreach (var attribute in new[] { false, true })
                {
                    var expected = Outcome(w => InValue(w, attribute, () => w.WriteStringValue(text.AsSpan())), version);
                    for (var split = 0; split <= text.Length; split++)
                    {
                        var cut = split;
                        Outcome(w => InValue(w, attribute, () =>
                        {
                            w.WriteStringValueSegment(text.AsSpan(0, cut), false);
                            w.WriteStringValueSegment(text.AsSpan(cut), true);
                        }), version).Should().Be(expected, "a split at character {0} must not change the value ({1}, attribute: {2})", cut, version, attribute);
                    }

                    Outcome(w => InValue(w, attribute, () =>
                    {
                        for (var i = 0; i < text.Length; i++)
                        {
                            w.WriteStringValueSegment(text.AsSpan(i, 1), false);
                        }

                        w.WriteStringValueSegment(ReadOnlySpan<char>.Empty, true);
                    }), version).Should().Be(expected, "one character in each segment must not change the value ({0}, attribute: {1})", version, attribute);
                }
            }
        }

        [TestMethod]
        public void CharSegments_SurrogatePairSplit_WritesTheCharacter()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValueSegment("a\uD83D".AsSpan(), false);
                w.WriteStringValueSegment("\uDE00b".AsSpan(), true);
                w.WriteEndElement();
            }).Should().Be("<e>a😀b</e>");
        }

        [TestMethod]
        [DataRow("D83D", "0078", DisplayName = "a high surrogate, then a character that is not its other half")]
        [DataRow("D83D", "", DisplayName = "a high surrogate at the end of the value")]
        [DataRow("DE00", "0078", DisplayName = "a low surrogate with nothing before it")]
        [DataRow("D83D", "D83DDE00", DisplayName = "a high surrogate, then a whole pair")]
        [DataRow("0061D83D", "D83D0062", DisplayName = "two high surrogates, one at the end of each segment")]
        public void CharSegments_UnpairedSurrogate_IsRejectedAsOneCallRejectsIt(string firstUnits, string secondUnits)
        {
            // An attribute argument cannot hold half of a surrogate pair, so the rows give UTF-16 code units
            // in hexadecimal. Half a pair is not a character, in one call or across segments.
            var first = Units(firstUnits);
            var second = Units(secondUnits);
            const string expected = "XmlException: The text contains half of a surrogate pair.";
            Outcome(w => InValue(w, false, () => w.WriteStringValue((first + second).AsSpan())), XmlVersion.Version10).Should().Be(expected);
            Outcome(w => InValue(w, false, () =>
            {
                w.WriteStringValueSegment(first.AsSpan(), false);
                w.WriteStringValueSegment(second.AsSpan(), true);
            }), XmlVersion.Version10).Should().Be(expected);
        }

        [TestMethod]
        public void CharSegments_EmptySegmentInsideASurrogatePair_KeepsTheFirstHalf()
        {
            var high = Units("D83D");
            var low = Units("DE00");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValueSegment(high.AsSpan(), false);
                w.WriteStringValueSegment(ReadOnlySpan<char>.Empty, false);
                w.WriteStringValueSegment(low.AsSpan(), true);
                w.WriteEndElement();
            }).Should().Be("<e>" + high + low + "</e>");
        }

        [TestMethod]
        public void OpenSegment_AllowsFlush()
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8XmlWriter(stream, new XmlWriterOptions { OmitXmlDeclaration = true }))
            {
                writer.WriteStartElement("e");
                writer.WriteStringValueSegment([(byte)'a', 0xC3], false);
                writer.Flush();
                Encoding.UTF8.GetString(stream.ToArray()).Should().Be("<e>a");
                writer.WriteStringValueSegment([0xA9], true);
                writer.WriteEndElement();
            }

            Encoding.UTF8.GetString(stream.ToArray()).Should().Be("<e>aé</e>");
        }

        [TestMethod]
        public void OpenSegment_IsClosedByReset()
        {
            var first = new ArrayBufferWriter<byte>();
            var second = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(first, new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true });
            writer.WriteStartElement("e");
            writer.WriteBase64StringSegment([1], false);
            writer.Flush();
            writer.Reset(second);
            writer.WriteStartElement("f");
            writer.WriteBase64StringSegment([2], true);
            writer.WriteEndElement();
            writer.Flush();
            Encoding.UTF8.GetString(first.WrittenSpan).Should().Be("<e>");
            Encoding.UTF8.GetString(second.WrittenSpan).Should().Be("<f>Ag==</f>");
        }

        [TestMethod]
        [DataRow("WriteStringValue(string)")]
        [DataRow("WriteStringValue(chars)")]
        [DataRow("WriteStringValue(utf8)")]
        [DataRow("WriteStringValue(DateTime)")]
        [DataRow("WriteNumberValue")]
        [DataRow("WriteBooleanValue")]
        [DataRow("WriteBase64StringValue")]
        [DataRow("WriteNilValue")]
        [DataRow("WriteRawValue")]
        [DataRow("WriteStartElement(string)")]
        [DataRow("WriteStartElement(string, string)")]
        [DataRow("WriteStartElement(chars)")]
        [DataRow("WriteStartElement(utf8)")]
        [DataRow("WriteEmptyElement")]
        [DataRow("WriteElementString")]
        [DataRow("WriteEndElement")]
        [DataRow("WriteFullEndElement")]
        [DataRow("WriteStartAttribute(string)")]
        [DataRow("WriteAttributeString")]
        [DataRow("WriteNamespaceDeclaration")]
        [DataRow("WriteDefaultNamespace")]
        [DataRow("WriteComment")]
        [DataRow("WriteCData")]
        [DataRow("WriteProcessingInstruction")]
        [DataRow("WriteStringValueSegment(chars)")]
        [DataRow("WriteBase64StringSegment")]
        public void OpenSegment_RejectsEveryOtherWrite(string call)
        {
            // The first segment ends inside a character, so the element's start tag is still open and an
            // attribute or a namespace declaration would otherwise be accepted.
            XmlWriterHarness.ThrownMessage(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValueSegment([0xC3], false);
                Invoke(w, call);
            }).Should().Be(OpenMessage);
        }

        [TestMethod]
        [DataRow("WriteStringValue(utf8)")]
        [DataRow("WriteEndAttribute")]
        [DataRow("WriteEndElement")]
        [DataRow("WriteStartElement(string)")]
        [DataRow("WriteStringValueSegment(chars)")]
        [DataRow("WriteBase64StringSegment")]
        public void OpenSegment_InAnAttribute_RejectsEveryOtherWrite(string call)
        {
            XmlWriterHarness.ThrownMessage(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("a");
                w.WriteStringValueSegment("x"u8, false);
                Invoke(w, call);
            }).Should().Be(OpenMessage);
        }

        [TestMethod]
        public void OpenSegment_OfAnotherKind_IsRejected()
        {
            XmlWriterHarness.ThrownMessage(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValueSegment("x".AsSpan(), false);
                w.WriteStringValueSegment("y"u8, true);
            }).Should().Be(OpenMessage);
            XmlWriterHarness.ThrownMessage(w =>
            {
                w.WriteStartElement("e");
                w.WriteBase64StringSegment([1], false);
                w.WriteStringValueSegment("y"u8, true);
            }).Should().Be(OpenMessage);
        }

        [TestMethod]
        public void Segments_AtTheDocumentLevel_AreRejected()
        {
            const string message = "Text is not allowed at the document level.";
            XmlWriterHarness.ThrownMessage(w => w.WriteStringValueSegment([0xC3], false)).Should().Be(message);
            XmlWriterHarness.ThrownMessage(w => w.WriteStringValueSegment("\uD83D".AsSpan(), false)).Should().Be(message);
            XmlWriterHarness.ThrownMessage(w => w.WriteBase64StringSegment([1], false)).Should().Be(message);
        }

        [TestMethod]
        public void StringValue_AtTheDocumentLevel_IsRejected()
        {
            // Text needs an element or an attribute to be in, whichever overload brings it and whether or
            // not it needs escaping.
            const string message = "Text is not allowed at the document level.";
            XmlWriterHarness.ThrownMessage(w => w.WriteStringValue("plain")).Should().Be(message);
            XmlWriterHarness.ThrownMessage(w => w.WriteStringValue("a<b")).Should().Be(message);
            XmlWriterHarness.ThrownMessage(w => w.WriteStringValue("plain".AsSpan())).Should().Be(message);
            XmlWriterHarness.ThrownMessage(w => w.WriteStringValue("a<b".AsSpan())).Should().Be(message);
            XmlWriterHarness.ThrownMessage(w => w.WriteStringValue("plain"u8)).Should().Be(message);
            XmlWriterHarness.ThrownMessage(w => w.WriteStringValue("a<b"u8)).Should().Be(message);
            XmlWriterHarness.ThrownMessage(w =>
            {
                w.WriteStartElement("e");
                w.WriteEndElement();
                w.WriteStringValue("after"u8);
            }).Should().Be(message);
        }

        [TestMethod]
        public void Segments_AfterTheFinalOne_AnotherValueCanStart()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("a");
                w.WriteBase64StringSegment([1, 2], false);
                w.WriteBase64StringSegment([3, 4], true);
                w.WriteEndAttribute();
                w.WriteStringValueSegment("a"u8, false);
                w.WriteStringValueSegment("b"u8, true);
                w.WriteStringValueSegment("c".AsSpan(), false);
                w.WriteStringValueSegment("d".AsSpan(), true);
                w.WriteStringValue("e");
                w.WriteEndElement();
            }).Should().Be("<e a=\"AQIDBA==\">abcde</e>");
        }

        [TestMethod]
        [DataRow("706C61696E", DisplayName = "ASCII")]
        [DataRow("C3A9E4B8ADF09F9880", DisplayName = "two, three and four byte characters")]
        [DataRow("61C3A962E4B8AD63F09F988064", DisplayName = "multi-byte characters between ASCII")]
        [DataRow("61C28562E280A863C2A064", DisplayName = "NEL, LS and a character that shares their lead bytes")]
        [DataRow("780D0A790D7A0A0977", DisplayName = "line ends and a tab")]
        [DataRow("615D5D3E62205D5D203E205D5D5D3E205D", DisplayName = "the CDATA end and its parts")]
        [DataRow("3C2622273E2026616D703B", DisplayName = "markup characters")]
        [DataRow("610162C29F64", DisplayName = "characters XML 1.0 rejects and XML 1.1 writes as references")]
        [DataRow("61C3", DisplayName = "ends inside a two byte character")]
        [DataRow("61F09F98", DisplayName = "ends inside a four byte character")]
        [DataRow("61C3C3A9", DisplayName = "a lead byte followed by another lead byte")]
        [DataRow("61A962", DisplayName = "a continuation byte with no lead byte")]
        [DataRow("61EDA08062", DisplayName = "a surrogate encoded as UTF-8")]
        [DataRow("61EFBFBE62", DisplayName = "a noncharacter")]
        [DataRow("", DisplayName = "empty")]
        public void Utf8Segments_AtEverySplit_MatchOneCall(string hex)
        {
            var utf8 = Convert.FromHexString(hex);
            foreach (var version in new[] { XmlVersion.Version10, XmlVersion.Version11 })
            {
                foreach (var attribute in new[] { false, true })
                {
                    var expected = Outcome(w => InValue(w, attribute, () => w.WriteStringValue(utf8)), version);
                    for (var split = 0; split <= utf8.Length; split++)
                    {
                        var cut = split;
                        Outcome(w => InValue(w, attribute, () =>
                        {
                            w.WriteStringValueSegment(utf8.AsSpan(0, cut), false);
                            w.WriteStringValueSegment(utf8.AsSpan(cut), true);
                        }), version).Should().Be(expected, "a split at byte {0} must not change the value ({1}, attribute: {2})", cut, version, attribute);
                    }

                    Outcome(w => InValue(w, attribute, () =>
                    {
                        for (var i = 0; i < utf8.Length; i++)
                        {
                            w.WriteStringValueSegment(utf8.AsSpan(i, 1), false);
                        }

                        w.WriteStringValueSegment(ReadOnlySpan<byte>.Empty, true);
                    }), version).Should().Be(expected, "one byte in each segment must not change the value ({0}, attribute: {1})", version, attribute);
                }
            }
        }

        [TestMethod]
        public void Utf8Segments_CharacterSplit_WritesTheCharacter()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValueSegment([(byte)'a', 0xF0, 0x9F], false);
                w.WriteStringValueSegment([0x98], false);
                w.WriteStringValueSegment([0x80, (byte)'b'], true);
                w.WriteEndElement();
            }).Should().Be("<e>a😀b</e>");
        }

        [TestMethod]
        public void Utf8Segments_Xml11NelSplit_WritesTheReference()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValueSegment([(byte)'a', 0xC2], false);
                w.WriteStringValueSegment([0x85, (byte)'b', 0xE2, 0x80], false);
                w.WriteStringValueSegment([0xA8], true);
                w.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, Version = XmlVersion.Version11 }).Should().Be("<e>a&#x85;b&#x2028;</e>");
        }

        [TestMethod]
        public void Utf8Segments_FinalSegmentEndsInsideACharacter_Throws()
        {
            XmlWriterHarness.ThrownMessage(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValueSegment([(byte)'a', 0xC3], true);
            }).Should().Be("Invalid UTF-8 in XML value.");
            XmlWriterHarness.ThrownMessage(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValueSegment([(byte)'a', 0xC3], false);
                w.WriteStringValueSegment(ReadOnlySpan<byte>.Empty, true);
            }).Should().Be("Invalid UTF-8 in XML value.");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Writes element <c>e</c> with a value in its content or in attribute <c>a</c>.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="attribute">Whether the value goes in an attribute.</param>
        /// <param name="value">Writes the value.</param>
        private static void InValue(Utf8XmlWriter writer, bool attribute, Action value)
        {
            writer.WriteStartElement("e");
            if (attribute)
            {
                writer.WriteStartAttribute("a");
            }

            value();
            writer.WriteEndElement();
        }

        private static void Invoke(Utf8XmlWriter writer, string call)
        {
            switch (call)
            {
                case "WriteStringValue(string)": writer.WriteStringValue("x"); break;
                case "WriteStringValue(chars)": writer.WriteStringValue("x".AsSpan()); break;
                case "WriteStringValue(utf8)": writer.WriteStringValue("x"u8); break;
                case "WriteStringValue(DateTime)": writer.WriteStringValue(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)); break;
                case "WriteNumberValue": writer.WriteNumberValue(1); break;
                case "WriteBooleanValue": writer.WriteBooleanValue(true); break;
                case "WriteBase64StringValue": writer.WriteBase64StringValue([1]); break;
                case "WriteNilValue": writer.WriteNilValue(); break;
                case "WriteRawValue": writer.WriteRawValue("x"u8); break;
                case "WriteStartElement(string)": writer.WriteStartElement("b"); break;
                case "WriteStartElement(string, string)": writer.WriteStartElement("b", "urn:b"); break;
                case "WriteStartElement(chars)": writer.WriteStartElement("b".AsSpan()); break;
                case "WriteStartElement(utf8)": writer.WriteStartElement("b"u8); break;
                case "WriteEmptyElement": writer.WriteEmptyElement("b"); break;
                case "WriteElementString": writer.WriteElementString("b", "x"); break;
                case "WriteEndElement": writer.WriteEndElement(); break;
                case "WriteFullEndElement": writer.WriteFullEndElement(); break;
                case "WriteStartAttribute(string)": writer.WriteStartAttribute("b"); break;
                case "WriteAttributeString": writer.WriteAttributeString("b", "x"); break;
                case "WriteEndAttribute": writer.WriteEndAttribute(); break;
                case "WriteNamespaceDeclaration": writer.WriteNamespaceDeclaration("p", "urn:p"); break;
                case "WriteDefaultNamespace": writer.WriteDefaultNamespace("urn:d"); break;
                case "WriteComment": writer.WriteComment("c"); break;
                case "WriteCData": writer.WriteCData("c"); break;
                case "WriteProcessingInstruction": writer.WriteProcessingInstruction("p", "d"); break;
                case "WriteStringValueSegment(chars)": writer.WriteStringValueSegment("x".AsSpan(), true); break;
                case "WriteBase64StringSegment": writer.WriteBase64StringSegment([1], true); break;
                default: throw new ArgumentOutOfRangeException(nameof(call), call, "No such call.");
            }
        }

        /// <summary>
        /// Builds a string from UTF-16 code units given as four hexadecimal digits each.
        /// </summary>
        /// <param name="hex">The code units.</param>
        /// <returns>The string, which may hold half of a surrogate pair.</returns>
        private static string Units(string hex)
        {
            var units = new char[hex.Length / 4];
            for (var i = 0; i < units.Length; i++)
            {
                units[i] = (char)Convert.ToUInt16(hex.Substring(i * 4, 4), 16);
            }

            return new string(units);
        }

        /// <summary>
        /// Returns what <paramref name="write"/> wrote, or the message of the <see cref="XmlException"/> it threw.
        /// </summary>
        /// <param name="write">Writes a document.</param>
        /// <param name="version">The XML version to write.</param>
        /// <returns>The document, or <c>XmlException: </c> and the message.</returns>
        private static string Outcome(Action<Utf8XmlWriter> write, XmlVersion version)
        {
            var options = new XmlWriterOptions { OmitXmlDeclaration = true, Version = version };
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8XmlWriter(buffer, options);
            try
            {
                write(writer);
                writer.Dispose();
            }
            catch (XmlException ex)
            {
                return "XmlException: " + ex.Message;
            }

            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        #endregion

    }

}
