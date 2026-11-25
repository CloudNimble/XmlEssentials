using System;
using System.Buffers;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A member that holds one scalar is written with its tags, or with its attribute name and quotes, in one
    /// reservation of the output. These tests hold that to what the writer produces a call at a time.
    /// </summary>
    [TestClass]
    public class XmlSerializerWholeValueTests
    {

        #region Fields

        private const string ElementBody = "<WholeValues><Boolean>false</Boolean><Byte>255</Byte><SByte>-128</SByte><Int16>-32768</Int16><UInt16>65535</UInt16><Int32>-2147483648</Int32><UInt32>4294967295</UInt32><Int64>-9223372036854775808</Int64><UInt64>18446744073709551615</UInt64><Int128>-170141183460469231731687303715884105728</Int128><UInt128>340282366920938463463374607431768211455</UInt128><Half>-65500</Half><Single>-3.4028235E+38</Single><Double>-1.7976931348623157E+308</Double><Infinity>-INF</Infinity><Decimal>-7.9228162514264337593543950335</Decimal><DateTime>9999-12-31T23:59:59.9999999Z</DateTime><DateTimeOffset>9999-12-31T09:59:59.9999999-14:00</DateTimeOffset><DateOnly>9999-12-31</DateOnly><TimeOnly>23:59:59.9999999</TimeOnly><TimeSpan>-P10675199DT2H48M5.4775808S</TimeSpan><Guid>ffffffff-ffff-ffff-ffff-ffffffffffff</Guid><Nullable>-9223372036854775808</Nullable><Text>plain text of some length</Text></WholeValues>";

        private const string AttributeBody = "<WholeValueAttributes Boolean=\"false\" Byte=\"255\" SByte=\"-128\" Int16=\"-32768\" UInt16=\"65535\" Int32=\"-2147483648\" UInt32=\"4294967295\" Int64=\"-9223372036854775808\" UInt64=\"18446744073709551615\" Int128=\"-170141183460469231731687303715884105728\" UInt128=\"340282366920938463463374607431768211455\" Half=\"-65500\" Single=\"-3.4028235E+38\" Double=\"-1.7976931348623157E+308\" Infinity=\"-INF\" Decimal=\"-7.9228162514264337593543950335\" DateTime=\"9999-12-31T23:59:59.9999999Z\" DateTimeOffset=\"9999-12-31T09:59:59.9999999-14:00\" DateOnly=\"9999-12-31\" TimeOnly=\"23:59:59.9999999\" TimeSpan=\"-P10675199DT2H48M5.4775808S\" Guid=\"ffffffff-ffff-ffff-ffff-ffffffffffff\" Nullable=\"-9223372036854775808\" Text=\"plain text of some length\"/>";

        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true };

        /// <summary>
        /// Text that is plain, text that has to be escaped, and text the two XML versions treat differently.
        /// </summary>
        private static readonly string[] Texts =
        [
            "",
            "plain",
            "a b",
            "a longer run of plain text, past the length that is copied one character at a time",
            " leading and trailing ",
            "tab\there",
            "line\nfeed",
            "carriage\rreturn",
            "both\r\nof them",
            "next line \u0085 here",
            "line separator \u2028 here",
            "a C1 control \u0080 and another \u009F",
            "caf\u00E9",
            "a pair \U0001F600 of surrogates",
            "amp & here",
            "less < than",
            "greater > than",
            "a \"quote\" and an 'apostrophe'",
            "the end of a CDATA section ]]> in text",
            "]",
            "]]",
        ];

        #endregion

        #region Public Methods

        [TestMethod]
        public void ElementMembers_WithTheLongestValues_AreWrittenInFull()
        {
            XmlSerializer.Serialize(new WholeValues(), Bare).Should().Be(ElementBody);
        }

        [TestMethod]
        public void AttributeMembers_WithTheLongestValues_AreWrittenInFull()
        {
            XmlSerializer.Serialize(new WholeValueAttributes(), Bare).Should().Be(AttributeBody);
        }

        [TestMethod]
        public void ElementMembers_AtAnyDistanceFromTheEndOfTheBuffer_AreWrittenInFull()
        {
            WrittenAtEveryDistance(new WholeValues(), ElementBody);
        }

        [TestMethod]
        public void AttributeMembers_AtAnyDistanceFromTheEndOfTheBuffer_AreWrittenInFull()
        {
            WrittenAtEveryDistance(new WholeValueAttributes(), AttributeBody);
        }

        [TestMethod]
        [DataRow(XmlVersion.Version10, "\n")]
        [DataRow(XmlVersion.Version10, "\r\n")]
        [DataRow(XmlVersion.Version11, "\n")]
        [DataRow(XmlVersion.Version11, "\r\n")]
        public void TextMembers_AreWrittenAsTheWriterWritesTextACallAtATime(XmlVersion version, string newLine)
        {
            var options = new XmlSerializerOptions { NewLine = newLine, OmitXmlDeclaration = true, XmlVersion = version };
            var writerOptions = new XmlWriterOptions { NewLine = newLine, OmitXmlDeclaration = true, Version = version };
            foreach (var text in Texts)
            {
                var expected = XmlWriterHarness.ToXml(writer =>
                {
                    writer.WriteStartElement("TextHolder");
                    writer.WriteStartAttribute("Label");
                    writer.WriteStringValue(text);
                    writer.WriteEndAttribute();
                    writer.WriteStartElement("Body");
                    writer.WriteStringValue(text);
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }, writerOptions);
                XmlSerializer.Serialize(new TextHolder { Body = text, Label = text }, options).Should().Be(expected, "the text is {0}", text);

                // An element that is written whole by the writer itself is held to the same thing.
                var element = XmlWriterHarness.ToXml(writer =>
                {
                    writer.WriteStartElement("r");
                    writer.WriteStartElement("e");
                    writer.WriteStringValue(text);
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }, writerOptions);
                XmlWriterHarness.ToXml(writer =>
                {
                    writer.WriteStartElement("r");
                    writer.WriteElementString("e", text);
                    writer.WriteEndElement();
                }, writerOptions).Should().Be(element, "the text is {0}", text);
                XmlWriterHarness.ToXml(writer =>
                {
                    writer.WriteStartElement("r");
                    writer.WriteElementString(XmlEncodedText.Encode("e"), text);
                    writer.WriteEndElement();
                }, writerOptions).Should().Be(element, "the text is {0}", text);
            }
        }

        [TestMethod]
        public void TextMembers_ThatNeedEscaping_AreEscaped()
        {
            // One document pinned in full for each XML version, so that the comparison above has a fixed point.
            var holder = new TextHolder { Body = "a < b & \u0085", Label = "tab\t\"q\"" };
            XmlSerializer.Serialize(holder, new XmlSerializerOptions { NewLine = "\n", OmitXmlDeclaration = true })
                .Should().Be("<TextHolder Label=\"tab&#x9;&quot;q&quot;\"><Body>a &lt; b &amp; \u0085</Body></TextHolder>");
            XmlSerializer.Serialize(holder, new XmlSerializerOptions { NewLine = "\n", OmitXmlDeclaration = true, XmlVersion = XmlVersion.Version11 })
                .Should().Be("<TextHolder Label=\"tab&#x9;&quot;q&quot;\"><Body>a &lt; b &amp; &#x85;</Body></TextHolder>");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Writes <paramref name="value"/> after every amount of text that puts some part of it at the end of
        /// the block the writer was given, into a destination that gives exactly what is asked for.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="body">The document the value is written as.</param>
        private static void WrittenAtEveryDistance<T>(T value, string body)
        {
            // The writer asks for 4,096 bytes at a time. "<root><pad>" is 11 of them.
            const int block = 4_096;
            for (var pad = block - 11 - body.Length - 8; pad <= block - 11; pad++)
            {
                var destination = new ExactBufferWriter();
                var padding = new string('p', pad);
                using (var writer = new Utf8XmlWriter(destination, new XmlWriterOptions { OmitXmlDeclaration = true }))
                {
                    writer.WriteStartElement("root");
                    writer.WriteStartElement("pad");
                    writer.WriteStringValue(padding);
                    writer.WriteEndElement();
                    XmlSerializer.Serialize(writer, value, Bare);
                    writer.WriteEndElement();
                }

                var written = Encoding.UTF8.GetString(destination.ToArray());
                (written == "<root><pad>" + padding + "</pad>" + body + "</root>").Should().BeTrue("the value is written after {0} bytes of text, and the document is {1}", pad, written[(pad + 11)..]);
            }
        }

        #endregion

    }

}
