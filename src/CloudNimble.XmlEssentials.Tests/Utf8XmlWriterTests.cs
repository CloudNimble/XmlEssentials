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
    /// Well-formed output, escaping, and reader round-trip for <see cref="Utf8XmlWriter"/>.
    /// </summary>
    [TestClass]
    public class Utf8XmlWriterTests
    {

        #region Public Methods

        [TestMethod]
        public void Write_EmptyElement_WritesSlashCloser()
        {
            ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteEndElement();
            }, omitDeclaration: true).Should().Be("<e/>");
        }

        [TestMethod]
        public void WriteEmptyElement_WritesSlashCloser()
        {
            ToXml(w => w.WriteEmptyElement("e"), omitDeclaration: true).Should().Be("<e/>");
        }

        [TestMethod]
        public void Write_StartEnd_WritesPair()
        {
            ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteFullEndElement();
            }, omitDeclaration: true).Should().Be("<e></e>");
        }

        [TestMethod]
        public void Write_AttributeAndText()
        {
            ToXml(w =>
            {
                w.WriteStartElement("root");
                w.WriteAttributeString("id", "1");
                w.WriteStartElement("n");
                w.WriteNumberValue(42);
                w.WriteEndElement();
                w.WriteEndElement();
            }, omitDeclaration: true).Should().Be("<root id=\"1\"><n>42</n></root>");
        }

        [TestMethod]
        public void Write_EscapesTextAndAttribute()
        {
            ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("a", "x\"y&z");
                w.WriteStringValue("a<b>c&d]]>");
                w.WriteEndElement();
            }, omitDeclaration: true).Should().Be("<e a=\"x&quot;y&amp;z\">a&lt;b>c&amp;d]]&gt;</e>");
        }

        [TestMethod]
        public void Write_CommentAndCData()
        {
            ToXml(w =>
            {
                w.WriteComment("hi");
                w.WriteStartElement("e");
                w.WriteCData("x<y");
                w.WriteEndElement();
            }, omitDeclaration: true).Should().Be("<!--hi--><e><![CDATA[x<y]]></e>");
        }

        [TestMethod]
        public void Write_DeclarationAndNamespace()
        {
            var xml = ToXml(w =>
            {
                w.WriteStartElement("p", "e", "urn:x");
                w.WriteEndElement();
            });
            xml.Should().StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            xml.Should().Contain("<p:e xmlns:p=\"urn:x\"/>");
        }

        [TestMethod]
        public void Write_DefaultNamespace()
        {
            ToXml(w =>
            {
                w.WriteStartElement("e", "urn:x");
                w.WriteEndElement();
            }, omitDeclaration: true).Should().Be("<e xmlns=\"urn:x\"/>");
        }

        [TestMethod]
        public void Write_NilElement()
        {
            var xml = ToXml(w => w.WriteNilElement("e"), omitDeclaration: true);
            xml.Should().Contain("xsi:nil=\"true\"");
            xml.Should().Contain("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
        }

        [TestMethod]
        public void Write_RoundTripWithReader()
        {
            var xml = ToXml(w =>
            {
                w.WriteStartElement("root");
                w.WriteAttributeString("id", "1");
                w.WriteElementString("n", "42");
                w.WriteEndElement();
            });
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml));
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.XmlDeclaration);
            reader.Read().Should().BeTrue();
            reader.LocalNameEquals("root").Should().BeTrue();
            reader.MoveToNextAttribute();
            reader.GetString().Should().Be("1");
            reader.Read();
            reader.LocalNameEquals("n").Should().BeTrue();
            reader.Read();
            reader.GetInt32().Should().Be(42);
        }

        [TestMethod]
        public void Write_CDataWithCloser_Throws()
        {
            var threw = false;
            try
            {
                ToXml(w =>
                {
                    w.WriteStartElement("e");
                    w.WriteCData("a]]>b");
                }, omitDeclaration: true);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Write_UnclosedDispose_Throws()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var threw = false;
            try
            {
                using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
                writer.WriteStartElement("e");
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void XmlEncodedText_RoundTrip()
        {
            var encoded = XmlEncodedText.Encode("root");
            encoded.ToString().Should().Be("root");
            encoded.Equals(XmlEncodedText.Encode("root"u8)).Should().BeTrue();
            ToXml(w =>
            {
                w.WriteStartElement(encoded);
                w.WriteEndElement();
            }, omitDeclaration: true).Should().Be("<root/>");
        }

        [TestMethod]
        public void Write_BooleanAndBase64()
        {
            ToXml(w => w.WriteElementBoolean("b", true), omitDeclaration: true).Should().Be("<b>true</b>");
            ToXml(w => w.WriteElementBase64String("b", "hi"u8), omitDeclaration: true).Should().Contain("aGk=");
        }

        [TestMethod]
        public void Write_Indented()
        {
            var xml = ToXml(w =>
            {
                w.WriteStartElement("a");
                w.WriteStartElement("b");
                w.WriteEndElement();
                w.WriteEndElement();
            }, new XmlWriterOptions { OmitXmlDeclaration = true, Indented = true, NewLine = "\n", IndentSize = 2 });
            xml.Should().Be("<a>\n  <b/>\n</a>");
        }

        [TestMethod]
        public void Write_StreamFlush()
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8XmlWriter(stream, new XmlWriterOptions { OmitXmlDeclaration = true }))
            {
                writer.WriteStartElement("e");
                writer.WriteEndElement();
            }

            Encoding.UTF8.GetString(stream.ToArray()).Should().Be("<e/>");
        }

        #endregion

        #region Private Methods

        private static string ToXml(Action<Utf8XmlWriter> write, bool omitDeclaration = false)
        {
            return ToXml(write, new XmlWriterOptions { OmitXmlDeclaration = omitDeclaration });
        }

        private static string ToXml(Action<Utf8XmlWriter> write, XmlWriterOptions options)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                write(writer);
            }

            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        #endregion

    }

}
