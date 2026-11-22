using System;
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
        public void XmlChars_AsciiClassifiers_MatchProductions()
        {
            for (var b = 0; b <= byte.MaxValue; b++)
            {
                var letter = b is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z');
                var digit = b is >= '0' and <= '9';
                XmlChars.IsAsciiNameStart((byte)b).Should().Be(letter || b == '_' || b == ':', $"byte {b}");
                XmlChars.IsAsciiNcNameChar((byte)b).Should().Be(letter || digit || b == '_' || b == '-' || b == '.', $"byte {b}");
            }
        }

        [TestMethod]
        public void XmlChars_NameStartChar_RangeBoundaries()
        {
            (int First, int Last)[] ranges =
            [
                (':', ':'), ('A', 'Z'), ('_', '_'), ('a', 'z'), (0xC0, 0xD6), (0xD8, 0xF6), (0xF8, 0x2FF),
                (0x370, 0x37D), (0x37F, 0x1FFF), (0x200C, 0x200D), (0x2070, 0x218F), (0x2C00, 0x2FEF),
                (0x3001, 0xD7FF), (0xF900, 0xFDCF), (0xFDF0, 0xFFFD), (0x10000, 0xEFFFF),
            ];
            foreach (var (first, last) in ranges)
            {
                XmlChars.IsNameStartChar(first, XmlVersion.Version10).Should().BeTrue($"U+{first:X}");
                XmlChars.IsNameStartChar(last, XmlVersion.Version11).Should().BeTrue($"U+{last:X}");
            }

            int[] outside =
            [
                0, '9', ';', '@', '[', '^', '`', '{', 0xBF, 0xD7, 0xF7, 0x300, 0x36F, 0x37E, 0x2000, 0x200B, 0x200E,
                0x206F, 0x2190, 0x2BFF, 0x2FF0, 0x3000, 0xD800, 0xF8FF, 0xFDD0, 0xFDEF, 0xFFFE, 0xFFFF, 0xF0000, 0x10FFFF,
            ];
            foreach (var cp in outside)
            {
                XmlChars.IsNameStartChar(cp, XmlVersion.Version10).Should().BeFalse($"U+{cp:X}");
            }
        }

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
            XmlChars.IsAsciiNameStart((byte)'A').Should().BeTrue();
            XmlChars.IsAsciiNameStart((byte)'_').Should().BeTrue();
            XmlChars.IsAsciiNameStart((byte)':').Should().BeTrue();
            XmlChars.IsAsciiNameStart((byte)'1').Should().BeFalse();
            XmlChars.IsAsciiNameStart((byte)'Z').Should().BeTrue();
            XmlChars.IsAsciiNameStart((byte)'a').Should().BeTrue();
            XmlChars.IsAsciiNameStart((byte)'z').Should().BeTrue();
            XmlChars.IsAsciiNameStart((byte)'@').Should().BeFalse();
            XmlChars.IsAsciiNcNameChar((byte)'A').Should().BeTrue();
            XmlChars.IsAsciiNcNameChar((byte)'Z').Should().BeTrue();
            XmlChars.IsAsciiNcNameChar((byte)'a').Should().BeTrue();
            XmlChars.IsAsciiNcNameChar((byte)'z').Should().BeTrue();
            XmlChars.IsAsciiNcNameChar((byte)'0').Should().BeTrue();
            XmlChars.IsAsciiNcNameChar((byte)'9').Should().BeTrue();
            XmlChars.IsAsciiNcNameChar((byte)'_').Should().BeTrue();
            XmlChars.IsAsciiNcNameChar((byte)'.').Should().BeTrue();
            XmlChars.IsAsciiNcNameChar((byte)'1').Should().BeTrue();
            XmlChars.IsAsciiNcNameChar((byte)'-').Should().BeTrue();
            XmlChars.IsAsciiNcNameChar((byte)':').Should().BeFalse();
            XmlChars.IsAsciiNcNameChar((byte)'/').Should().BeFalse();
            foreach (var cp in new[]
                     {
                         (int)':', 'A', 'Z', '_', 'a', 'z',
                         0xBF, 0xC0, 0xD6, 0xD7, 0xD8, 0xF6, 0xF7, 0xF8, 0x2FF, 0x300,
                         0x36F, 0x370, 0x37D, 0x37E, 0x37F, 0x1FFF, 0x200B, 0x200C, 0x200D, 0x200E,
                         0x206F, 0x2070, 0x218F, 0x2190, 0x2BFF, 0x2C00, 0x2FEF, 0x2FF0,
                         0x3000, 0x3001, 0xD7FF, 0xD800, 0xF8FF, 0xF900, 0xFDCF, 0xFDD0,
                         0xFDEF, 0xFDF0, 0xFFFD, 0xFFFE, 0x10000, 0xEFFFF, 0xF0000,
                     })
            {
                _ = XmlChars.IsNameStartChar(cp, XmlVersion.Version10);
                _ = XmlChars.IsNameStartChar(cp, XmlVersion.Version11);
            }

            XmlChars.IsNameChar('.', XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameChar('0', XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameChar('9', XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameChar(0xB7, XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameChar(0x036F, XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameChar(0x2040, XmlVersion.Version10).Should().BeTrue();
            XmlChars.IsNameChar(0x2041, XmlVersion.Version10).Should().BeFalse();
            XmlChars.IsXml10Char(0x8).Should().BeFalse();
            XmlChars.IsXml10Char(0xA).Should().BeTrue();
            XmlChars.IsXml10Char(0xD).Should().BeTrue();
            XmlChars.IsXml10Char(0x20).Should().BeTrue();
            XmlChars.IsXml10Char(0xD7FF).Should().BeTrue();
            XmlChars.IsXml10Char(0xD800).Should().BeFalse();
            XmlChars.IsXml10Char(0xFFFD).Should().BeTrue();
            XmlChars.IsXml10Char(0xFFFE).Should().BeFalse();
            XmlChars.IsXml10Char(0x10FFFF).Should().BeTrue();
            XmlChars.IsXml10Char(0x110000).Should().BeFalse();
            XmlChars.IsXml11Char(0xD7FF).Should().BeTrue();
            XmlChars.IsXml11Char(0xD800).Should().BeFalse();
            XmlChars.IsXml11Char(0xFFFD).Should().BeTrue();
            XmlChars.IsXml11Char(0xFFFE).Should().BeFalse();
            XmlChars.IsXml11Char(0x10FFFF).Should().BeTrue();
            XmlChars.IsXml11Char(0x110000).Should().BeFalse();
            XmlChars.IsXml11RestrictedChar(0).Should().BeFalse();
            XmlChars.IsXml11RestrictedChar(8).Should().BeTrue();
            XmlChars.IsXml11RestrictedChar(9).Should().BeFalse();
            XmlChars.IsXml11RestrictedChar(0xC).Should().BeTrue();
            XmlChars.IsXml11RestrictedChar(0xD).Should().BeFalse();
            XmlChars.IsXml11RestrictedChar(0x1F).Should().BeTrue();
            XmlChars.IsXml11RestrictedChar(0x20).Should().BeFalse();
            XmlChars.IsXml11RestrictedChar(0x84).Should().BeTrue();
            XmlChars.IsXml11RestrictedChar(0x85).Should().BeFalse();
            XmlChars.IsXml11RestrictedChar(0x9F).Should().BeTrue();
            XmlChars.IsXml11RestrictedChar(0xA0).Should().BeFalse();
            XmlChars.AsciiNcNameChars.Contains((byte)'Z').Should().BeTrue();
            XmlChars.AsciiTextChars.Contains((byte)'W').Should().BeTrue();
            XmlChars.AsciiTextChars.Contains((byte)'<').Should().BeFalse();
            XmlChars.EndTagNameStops.Contains((byte)':').Should().BeTrue();
            XmlChars.EndTagNameStops.Contains((byte)'>').Should().BeTrue();
            XmlChars.TextStops10.Contains((byte)'<').Should().BeTrue();
            XmlChars.TextStops11.Contains((byte)0xC2).Should().BeTrue();
            XmlChars.AttrStopsDouble.Contains((byte)'"').Should().BeTrue();
            XmlChars.AttrStopsSingle.Contains((byte)'\'').Should().BeTrue();
        }

        [TestMethod]
        public void XmlUtf8_TryInternKnown()
        {
            XmlUtf8.TryInternKnown([]).Should().BeSameAs(XmlUtf8.Empty);
            XmlUtf8.TryInternKnown(XmlUtf8.Xml).Should().BeSameAs(XmlUtf8.Xml);
            XmlUtf8.TryInternKnown(XmlUtf8.Xmlns).Should().BeSameAs(XmlUtf8.Xmlns);
            XmlUtf8.TryInternKnown(XmlUtf8.Xsi).Should().BeSameAs(XmlUtf8.Xsi);
            XmlUtf8.TryInternKnown("urn:nope"u8).Should().BeNull();
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
