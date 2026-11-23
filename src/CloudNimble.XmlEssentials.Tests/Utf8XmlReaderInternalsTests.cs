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
