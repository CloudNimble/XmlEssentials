using System;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// What the encoding in a document means depends on what the document arrives as. Characters have no
    /// encoding left to declare, so a declaration that names one is not checked, as <c>System.Xml</c> has it
    /// for a string. Bytes must be UTF-8, and bytes in another Unicode encoding are reported as that.
    /// </summary>
    [TestClass]
    public class XmlEncodingInputTests
    {

        #region Fields

        private const string NotUtf8 = "XmlEssentials is UTF-8 only. The input is UTF-16 or UTF-32.";

        #endregion

        #region Public Methods

        [TestMethod]
        [DataRow("utf-16", DisplayName = "utf-16, which System.Xml writes to a StringWriter")]
        [DataRow("UTF-16", DisplayName = "UTF-16")]
        [DataRow("iso-8859-1", DisplayName = "an encoding that is not Unicode")]
        [DataRow("utf-8", DisplayName = "utf-8")]
        public void Characters_WithADeclaredEncoding_AreReadWhateverItNames(string encoding)
        {
            var xml = "<?xml version=\"1.0\" encoding=\"" + encoding + "\"?><Person><Name>Ada</Name><Age>36</Age></Person>";
            var options = new XmlSerializerOptions();

            using (var document = XmlDocument.Parse(xml))
            {
                document.RootElement.GetRawXml().Should().Be("<Person><Name>Ada</Name><Age>36</Age></Person>");
            }

            using (var document = XmlDocument.Parse(xml.AsSpan()))
            {
                document.RootElement.GetRawXml().Should().Be("<Person><Name>Ada</Name><Age>36</Age></Person>");
            }

            XmlNode.Parse(xml)!.ToString().Should().Be("<Person><Name>Ada</Name><Age>36</Age></Person>");
            XmlSerializer.Deserialize<Person>(xml)!.Name.Should().Be("Ada");
            XmlSerializer.Deserialize<Person>(xml, options)!.Name.Should().Be("Ada");
            XmlSerializer.Deserialize(xml, options.GetTypeInfo<Person>())!.Name.Should().Be("Ada");
            XmlSerializer.Deserialize<Person>(xml.AsSpan())!.Name.Should().Be("Ada");
            XmlSerializer.Deserialize<Person>(xml.AsSpan(), options)!.Name.Should().Be("Ada");
            XmlSerializer.Deserialize(xml.AsSpan(), options.GetTypeInfo<Person>())!.Name.Should().Be("Ada");
            ((Person)XmlSerializer.Deserialize(xml, typeof(Person))!).Name.Should().Be("Ada");
            ((Person)XmlSerializer.Deserialize(xml.AsSpan(), typeof(Person))!).Name.Should().Be("Ada");
        }

        [TestMethod]
        public void Characters_WithADeclarationThatIsMalformed_AreStillRejected()
        {
            const string xml = "<?xml version=\"1.0\" encoding=\"\"?><a/>";
            FluentActions.Invoking(() => XmlDocument.Parse(xml)).Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The XML declaration is malformed.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Person>(xml)).Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The XML declaration is malformed.");
        }

        [TestMethod]
        [DataRow("utf-16")]
        [DataRow("iso-8859-1")]
        public void Utf8Bytes_WithAnotherDeclaredEncoding_AreRejected(string encoding)
        {
            // The bytes are what they are. A declaration that says otherwise is wrong about them.
            var bytes = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"" + encoding + "\"?><Person><Name>Ada</Name></Person>");
            var message = "XmlEssentials is UTF-8 only. Declared encoding '" + encoding + "' is not supported.";
            FluentActions.Invoking(() => XmlDocument.Parse(bytes)).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message);
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Person>(bytes)).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message);
            FluentActions.Invoking(() => XmlReaderHarness.Drain(bytes)).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message);
        }

        [TestMethod]
        [DataRow("utf-16", true, DisplayName = "UTF-16 little endian with a byte order mark")]
        [DataRow("utf-16", false, DisplayName = "UTF-16 little endian with no byte order mark")]
        [DataRow("utf-16BE", true, DisplayName = "UTF-16 big endian with a byte order mark")]
        [DataRow("utf-16BE", false, DisplayName = "UTF-16 big endian with no byte order mark")]
        [DataRow("utf-32", true, DisplayName = "UTF-32 little endian with a byte order mark")]
        [DataRow("utf-32BE", true, DisplayName = "UTF-32 big endian with a byte order mark")]
        [DataRow("utf-32BE", false, DisplayName = "UTF-32 big endian with no byte order mark")]
        public void BytesInAnotherUnicodeEncoding_AreReportedAsThat(string encodingName, bool withByteOrderMark)
        {
            var encoding = Encoding.GetEncoding(encodingName);
            var text = encoding.GetBytes("<a>text</a>");
            var bytes = withByteOrderMark ? [.. encoding.GetPreamble(), .. text] : text;
            FluentActions.Invoking(() => XmlReaderHarness.Drain(bytes)).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(NotUtf8);
            FluentActions.Invoking(() => XmlDocument.Parse(bytes)).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(NotUtf8);
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Person>(bytes)).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(NotUtf8);
        }

        [TestMethod]
        [DataRow("utf-16", true)]
        [DataRow("utf-16BE", true)]
        [DataRow("utf-16", false)]
        [DataRow("utf-16BE", false)]
        public void BytesInAnotherUnicodeEncoding_OneByteAtATime_AreReportedAsThat(string encodingName, bool withByteOrderMark)
        {
            var encoding = Encoding.GetEncoding(encodingName);
            var text = encoding.GetBytes("<a/>");
            byte[] bytes = withByteOrderMark ? [.. encoding.GetPreamble(), .. text] : text;
            var first = new Utf8XmlReader(bytes.AsSpan(0, 1), false, default);
            first.Read().Should().BeFalse("one byte does not say what the encoding is");
            first.BytesConsumed.Should().Be(0);
            var message = "";
            try
            {
                var second = new Utf8XmlReader(bytes, true, first.CurrentState);
                second.Read();
            }
            catch (XmlException ex)
            {
                message = ex.Message;
            }

            message.Should().Be(NotUtf8);
        }

        [TestMethod]
        public void Utf8Bytes_ThatStartLikeAByteOrderMarkOfAnotherEncoding_GetTheUsualError()
        {
            // 0xFF and 0xFE are not UTF-8 at all, and a lone one is not a byte order mark either.
            FluentActions.Invoking(() => XmlReaderHarness.Drain(new byte[] { 0xFF, (byte)'<', (byte)'a', (byte)'/', (byte)'>' }))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Unexpected content outside the root element.");
            foreach (var second in new byte[] { 0xFF, 0x00, (byte)'<', (byte)'a' })
            {
                // Each first byte of a mark, followed by a byte that does not complete one.
                foreach (var first in new byte[] { 0xFF, 0xFE })
                {
                    if ((first, second) is (0xFF, 0xFE) or (0xFE, 0xFF))
                    {
                        continue;
                    }

                    FluentActions.Invoking(() => XmlReaderHarness.Drain(new[] { first, second, (byte)'<', (byte)'a', (byte)'/', (byte)'>' }))
                        .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Unexpected content outside the root element.", "bytes {0:X2} {1:X2}", first, second);
                }
            }

            FluentActions.Invoking(() => XmlReaderHarness.Drain(new byte[] { 0x00, (byte)'a', (byte)'<', (byte)'a', (byte)'/', (byte)'>' }))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Unexpected content outside the root element.");

            foreach (var only in new byte[] { 0xFF, 0xFE, 0x00 })
            {
                // One byte, and no more to come: it is not a mark, and it is not XML.
                FluentActions.Invoking(() => XmlReaderHarness.Drain(new[] { only }))
                    .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Unexpected content outside the root element.");
            }

            var reader = new Utf8XmlReader("<"u8, false, default);
            reader.Read().Should().BeFalse("a lone '<' is the start of a tag that has not arrived");
        }

        #endregion

    }

}
