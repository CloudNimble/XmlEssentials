using System;
using System.Linq;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Curated XML 1.0 / 1.1 utf-8 conformance cases (no DTD) from the xmlconf themes.
    /// </summary>
    [TestClass]
    public class Utf8XmlReaderXmlConfTests
    {

        #region Public Methods

        [TestMethod]
        [DataRow("<doc/>")]
        [DataRow("<?xml version=\"1.0\"?><doc/>")]
        [DataRow("<?xml version=\"1.0\" encoding=\"UTF-8\"?><doc/>")]
        [DataRow("<doc></doc>")]
        [DataRow("<doc>text</doc>")]
        [DataRow("<doc a=\"v\"/>")]
        [DataRow("<doc a='v'/>")]
        [DataRow("<doc xmlns=\"urn:n\"/>")]
        [DataRow("<n:doc xmlns:n=\"urn:n\"/>")]
        [DataRow("<doc xmlns:n=\"urn:n\" n:a=\"v\"/>")]
        [DataRow("<doc xml:lang=\"en\"/>")]
        [DataRow("<a xmlns=\"urn:a\"><b xmlns=\"\"/></a>")]
        [DataRow("<?xml version=\"1.1\"?><doc/>")]
        [DataRow("<?xml version=\"1.1\"?><doc>&#x1;</doc>")]
        [DataRow("<?xml version=\"1.1\"?><a xmlns:p=\"urn:p\"><b xmlns:p=\"\"/></a>")]
        [DataRow("<doc><?pi data?></doc>")]
        [DataRow("<doc><![CDATA[<>&]]></doc>")]
        [DataRow("<doc>&amp;&lt;&gt;&quot;&apos;</doc>")]
        [DataRow("<!--c--><doc/>")]
        [DataRow("<_e.1-2/>")]
        public void XmlConf_ValidUtf8_Succeeds(string xml)
        {
            XmlReaderHarness.MustRead(xml);
        }

        [TestMethod]
        [DataRow("<")]
        [DataRow("<>")]
        [DataRow("</doc>")]
        [DataRow("<doc>")]
        [DataRow("<doc></DOC>")]
        [DataRow("<doc a=v/>")]
        [DataRow("<doc a=\"v/>")]
        [DataRow("<1doc/>")]
        [DataRow("<doc>&foo;</doc>")]
        [DataRow("<doc>&#x0;</doc>")]
        [DataRow("<doc>&#x1;</doc>")]
        [DataRow("<!DOCTYPE doc><doc/>")]
        [DataRow("<?xml version=\"1.0\"?><?xml version=\"1.0\"?><doc/>")]
        [DataRow("<n:doc/>")]
        [DataRow("<doc xmlns:p=\"urn:p\" xmlns:p=\"urn:q\"/>")]
        [DataRow("<a xmlns:p=\"urn:p\"><b xmlns:p=\"\"/></a>")]
        [DataRow("<?xml version=\"1.1\"?><doc>&#x0;</doc>")]
        [DataRow("<doc xmlns:xmlns=\"urn:x\"/>")]
        [DataRow("<doc xmlns:xml=\"urn:x\"/>")]
        [DataRow("<p:e xmlns:p=\"urn:x\"></p:f>")]
        public void XmlConf_Invalid_Throws(string xml)
        {
            XmlReaderHarness.MustThrow(xml);
        }

        [TestMethod]
        public void XmlConf_AttributeCrlfBecomesSpace()
        {
            var reader = new Utf8XmlReader("<e a=\"x\r\ny\"/>"u8);
            reader.Read();
            reader.MoveToNextAttribute();
            reader.GetString().Should().Be("x y");
        }

        [TestMethod]
        public void XmlConf_EolCrlfBecomesLfInText()
        {
            var reader = new Utf8XmlReader("<e>a\r\nb</e>"u8);
            reader.Read();
            reader.Read();
            reader.GetString().Should().Be("a\nb");
        }

        [TestMethod]
        public void XmlConf_EolLoneCrBecomesLfInText()
        {
            var reader = new Utf8XmlReader("<e>a\rb</e>"u8);
            reader.Read();
            reader.Read();
            reader.GetString().Should().Be("a\nb");
        }

        [TestMethod]
        public void XmlConf_MaxDocumentByteCount()
        {
            XmlReaderHarness.MustThrow("<e/>", new XmlReaderOptions { MaxDocumentByteCount = 2 });
            XmlReaderHarness.MustRead("<e/>", new XmlReaderOptions { MaxDocumentByteCount = 0 });
        }

        [TestMethod]
        public void XmlConf_NameStartCharSupplementary()
        {
            var name = new byte[] { 0xF0, 0x90, 0x80, 0x80 };
            var xml = new byte[] { (byte)'<' }.Concat(name).Concat("/>"u8.ToArray()).ToArray();
            XmlReaderHarness.MustRead(xml);
        }

        [TestMethod]
        public void XmlConf_Xml10NelStaysNel()
        {
            var nel = "<e>a"u8.ToArray().Concat(new byte[] { 0xC2, 0x85 }).Concat("b</e>"u8.ToArray()).ToArray();
            var reader = new Utf8XmlReader(nel);
            reader.Read();
            reader.Read();
            reader.GetString().Should().Be("a\u0085b");
        }

        [TestMethod]
        public void XmlConf_Xml11NelAndLsBecomeLf()
        {
            var nel = "<?xml version=\"1.1\"?><e>a"u8.ToArray().Concat(new byte[] { 0xC2, 0x85 }).Concat("b</e>"u8.ToArray()).ToArray();
            var reader = new Utf8XmlReader(nel);
            reader.Read();
            reader.Read();
            reader.Read();
            reader.GetString().Should().Be("a\nb");

            var ls = "<?xml version=\"1.1\"?><e>a"u8.ToArray().Concat(new byte[] { 0xE2, 0x80, 0xA8 }).Concat("b</e>"u8.ToArray()).ToArray();
            var lsReader = new Utf8XmlReader(ls);
            lsReader.Read();
            lsReader.Read();
            lsReader.Read();
            lsReader.GetString().Should().Be("a\nb");
        }

        [TestMethod]
        public void XmlConf_Xml11NelInAttributeBecomesSpace()
        {
            var xml = "<?xml version=\"1.1\"?><e a=\"x"u8.ToArray()
                .Concat(new byte[] { 0xC2, 0x85 })
                .Concat("y\"/>"u8.ToArray())
                .ToArray();
            var reader = new Utf8XmlReader(xml);
            reader.Read();
            reader.Read();
            reader.MoveToNextAttribute();
            reader.GetString().Should().Be("x y");
        }

        [TestMethod]
        public void XmlConf_Xml11LsInAttributeBecomesSpace()
        {
            var xml = "<?xml version=\"1.1\"?><e a=\"x"u8.ToArray()
                .Concat(new byte[] { 0xE2, 0x80, 0xA8 })
                .Concat("y\"/>"u8.ToArray())
                .ToArray();
            var reader = new Utf8XmlReader(xml);
            reader.Read();
            reader.Read();
            reader.MoveToNextAttribute();
            reader.GetString().Should().Be("x y");
        }

        #endregion

    }

}
