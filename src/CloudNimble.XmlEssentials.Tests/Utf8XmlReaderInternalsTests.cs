using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Internal preconditions on <see cref="Utf8XmlReader"/> that public paths never violate.
    /// </summary>
    [TestClass]
    public class Utf8XmlReaderInternalsTests
    {

        #region Public Methods

        [TestMethod]
        [DataRow("<a>t</a>", true, DisplayName = "text and the end tag")]
        [DataRow("<a>t</a", false, DisplayName = "an end tag that is cut")]
        [DataRow("<a>t<", false, DisplayName = "the block ends on the start of markup")]
        [DataRow("<a>text", false, DisplayName = "no markup after the start tag")]
        [DataRow("<a/>", true, DisplayName = "an empty element has no end tag")]
        [DataRow("<a><b/></a>", true, DisplayName = "an empty child does not open a level")]
        [DataRow("<a><b></b>", false, DisplayName = "the child is closed and the element is not")]
        [DataRow("<a><a></a>", false, DisplayName = "a child of the same name is the one that is closed")]
        [DataRow("<a><a></a></a>", true, DisplayName = "a child of the same name and then the element")]
        [DataRow("<a><b x=\">\"></b></a>", true, DisplayName = "a quoted > does not end a start tag")]
        [DataRow("<a><b x='>'/></a>", true, DisplayName = "a quoted > before the end of an empty element")]
        [DataRow("<a><b x=\"/\"></b></a>", true, DisplayName = "a value that ends in a slash is not an empty element")]
        [DataRow("<a><b x=\"</a>", false, DisplayName = "an attribute value that is cut")]
        [DataRow("<a><b x='1'", false, DisplayName = "a start tag that is cut")]
        [DataRow("<a><!-- </a> --></a>", true, DisplayName = "an end tag inside a comment is not one")]
        [DataRow("<a><!-- </a>", false, DisplayName = "a comment that is cut")]
        [DataRow("<a><![CDATA[</a>]]></a>", true, DisplayName = "an end tag inside CDATA is not one")]
        [DataRow("<a><![CDATA[</a>", false, DisplayName = "a CDATA section that is cut")]
        [DataRow("<a><?pi </a>?></a>", true, DisplayName = "an end tag inside a processing instruction is not one")]
        [DataRow("<a><?pi </a>", false, DisplayName = "a processing instruction that is cut")]
        [DataRow("<a><!", false, DisplayName = "the block ends before a comment can be told from CDATA")]
        [DataRow("<a><!DOCTYPE x></a>", false, DisplayName = "other markup is left to the reader")]
        public void IsElementInBlock_CountsTagsToFindTheEndTag(string block, bool expected)
        {
            // The block is not the last one, so an element whose end tag is not in it may still be completed.
            var reader = new Utf8XmlReader(System.Text.Encoding.UTF8.GetBytes(block), isFinalBlock: false, default);
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.StartElement);
            reader.IsElementInBlock().Should().Be(expected);

            // Asking moves nothing: the reader is still on the start tag, and reads on from it.
            reader.TokenType.Should().Be(XmlTokenType.StartElement);
            reader.LocalNameEquals("a").Should().BeTrue();
        }

        [TestMethod]
        public void IsElementInBlock_InTheFinalBlock_IsTrueWhateverTheBlockHolds()
        {
            // Nothing more is coming, so there is nothing to wait for: reading the element reports what is missing.
            var reader = new Utf8XmlReader("<a>text"u8, isFinalBlock: true, default);
            reader.Read().Should().BeTrue();
            reader.IsElementInBlock().Should().BeTrue();
        }

        [TestMethod]
        public void TryMatchEndName_AtDepthZero_IsFalse()
        {
            var reader = new Utf8XmlReader("<e/>"u8);
            reader.TryMatchEndName(out var nameStart, out var nameLength, out var prefixLength).Should().BeFalse();
            nameStart.Should().Be(0);
            nameLength.Should().Be(0);
            prefixLength.Should().Be(0);
        }

        [TestMethod]
        public void TryReadLeafContent_LeavesNoValueOnTheEndTag()
        {
            // The serializer reads a scalar element's text and end tag in one step. The reader is then on the
            // end tag, and the value of the token before the element must not show through.
            var reader = new Utf8XmlReader("<r>before<n>42</n></r>"u8);
            reader.Read();
            reader.Read();
            reader.GetString().Should().Be("before");
            reader.Read();
            reader.TryReadLeafContent(out var content).Should().BeTrue();
            System.Text.Encoding.UTF8.GetString(content).Should().Be("42");
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            reader.GetString().Should().Be("");
            reader.ValueSpan.Length.Should().Be(0);
            reader.ValueIsEscaped.Should().BeFalse();
        }

        #endregion

    }

}
