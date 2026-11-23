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

        #endregion

    }

}
