using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Tests for XmlName, XmlException, XmlChars, and XmlLimits.
    /// </summary>
    [TestClass]
    public class XmlTypeTests
    {

        #region Public Methods

        [TestMethod]
        public void XmlChars_NameAndCharProductions()
        {
            XmlChars.IsNameStartChar(':', XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameStartChar('A', XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameStartChar('1', XmlVersion.Version10).Should().BeFalse();
            XmlChars.IsNameStartChar(0xC0, XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameStartChar(0xD8, XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameStartChar(0xF8, XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameStartChar(0x370, XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameStartChar(0x37F, XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameStartChar(0x200C, XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameStartChar(0x2070, XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameStartChar(0x2C00, XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameStartChar(0x3001, XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameStartChar(0xF900, XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameStartChar(0xFDF0, XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameStartChar(0x10000, XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameChar('1', XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameChar('-', XmlVersion.Version11).Should().BeTrue();
            XmlChars.IsNameChar(0x0300, XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameChar(0x203F, XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsXml10Char(0).Should().BeFalse();
            XmlChars.IsXml10Char(0x9).Should().BeTrue();
            XmlChars.IsXml10Char('A').Should().BeTrue();
            XmlChars.IsXml10Char(0xE000).Should().BeTrue();
            XmlChars.IsXml10Char(0x10000).Should().BeTrue();
            XmlChars.IsXml11Char(0).Should().BeFalse();
            XmlChars.IsXml11Char(1).Should().BeTrue();
            XmlChars.IsXml11Char(0xE000).Should().BeTrue();
            XmlChars.IsXml11Char(0x10000).Should().BeTrue();
            XmlChars.IsXml11RestrictedChar(1).Should().BeTrue();
            XmlChars.IsXml11RestrictedChar(0xB).Should().BeTrue();
            XmlChars.IsXml11RestrictedChar(0xE).Should().BeTrue();
            XmlChars.IsXml11RestrictedChar(0x7F).Should().BeTrue();
            XmlChars.IsXml11RestrictedChar(0x86).Should().BeTrue();
            XmlChars.IsXml11RestrictedChar('A').Should().BeFalse();
            XmlChars.IsWhiteSpace((byte)' ').Should().BeTrue();
            XmlChars.IsWhiteSpace((byte)'\t').Should().BeTrue();
            XmlChars.IsWhiteSpace((byte)'\n').Should().BeTrue();
            XmlChars.IsWhiteSpace((byte)'\r').Should().BeTrue();
            XmlChars.IsWhiteSpace((byte)'a').Should().BeFalse();
        }

        [TestMethod]
        public void XmlException_StoresLocation()
        {
            var inner = new InvalidOperationException("x");
            var simple = new XmlException("m");
            simple.Message.Should().Be("m");
            var wrapped = new XmlException("m", inner);
            wrapped.InnerException.Should().BeSameAs(inner);
            var located = new XmlException("m", 10, 2, 3, "/e");
            located.BytePosition.Should().Be(10);
            located.LineNumber.Should().Be(2);
            located.LinePosition.Should().Be(3);
            located.Path.Should().Be("/e");
        }

        [TestMethod]
        public void XmlLimits_ZeroMeansDefault()
        {
            XmlLimits.Resolve(0, 64).Should().Be(64);
            XmlLimits.Resolve(3, 64).Should().Be(3);
            XmlLimits.DefaultBufferSize.Should().Be(16_384);
            XmlLimits.DefaultMaxAttributeCount.Should().Be(1000);
            XmlLimits.DefaultMaxCommentByteCount.Should().Be(1_048_576);
            XmlLimits.DefaultMaxDepth.Should().Be(64);
            XmlLimits.DefaultMaxDocumentByteCount.Should().Be(0);
            XmlLimits.DefaultMaxNameByteCount.Should().Be(1024);
            XmlLimits.DefaultMaxTextByteCount.Should().Be(16_777_216);
        }

        [TestMethod]
        public void XmlName_ToStringAndOperators()
        {
            var local = new XmlName("n");
            local.ToString().Should().Be("n");
            local.Equals((object)local).Should().BeTrue();
            local.Equals((object)"n").Should().BeFalse();
            local.GetHashCode().Should().Be(new XmlName("n").GetHashCode());
            (local != new XmlName("x")).Should().BeTrue();

            var ns = new XmlName("n", "urn:x");
            ns.ToString().Should().Be("{urn:x}n");

            var whitespace = () => new XmlName(" ");
            whitespace.Should().Throw<ArgumentException>();
            var empty = () => new XmlName("");
            empty.Should().Throw<ArgumentException>();
            var nullName = () => new XmlName(null!);
            nullName.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void XmlNamespacesAndAttributes_HaveExpectedValues()
        {
            XmlNamespaces.Xml.Should().NotBeNullOrWhiteSpace();
            XmlNamespaces.Xmlns.Should().NotBeNullOrWhiteSpace();
            XmlNamespaces.Xsi.Should().NotBeNullOrWhiteSpace();
            XmlNamespaces.Xsd.Should().NotBeNullOrWhiteSpace();
            XmlAttributes.Nil.Should().Be("nil");
            XmlAttributes.Type.Should().Be("type");

            var options = new XmlReaderOptions();
            options.CommentHandling.Should().Be(XmlCommentHandling.Skip);
            options.WhitespaceHandling.Should().Be(XmlWhitespaceHandling.Insignificant);
            options.AllowMultipleRoots = true;
            options.MaxAttributeCount = 1;
            options.MaxCommentByteCount = 2;
            options.MaxDepth = 3;
            options.MaxDocumentByteCount = 4;
            options.MaxNameByteCount = 5;
            options.MaxTextByteCount = 6;
            options.AllowMultipleRoots.Should().BeTrue();
            options.MaxAttributeCount.Should().Be(1);
            options.MaxCommentByteCount.Should().Be(2);
            options.MaxDepth.Should().Be(3);
            options.MaxDocumentByteCount.Should().Be(4);
            options.MaxNameByteCount.Should().Be(5);
            options.MaxTextByteCount.Should().Be(6);

            var state = new XmlReaderState(options);
            state.Options.MaxDepth.Should().Be(3);
        }

        #endregion

    }

}
