using System;
using System.Linq;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// IsFinalBlock resume across splits inside a namespaced start tag, an attribute, and text.
    /// </summary>
    [TestClass]
    public class Utf8XmlReaderResumeTests
    {

        #region Public Methods

        [TestMethod]
        public void Resume_BetweenTokensKeepsDepth()
        {
            var first = "<a><b/>"u8.ToArray();
            var second = "</a>"u8.ToArray();
            var reader = new Utf8XmlReader(first, false, default);
            while (reader.Read())
            {
            }

            reader.CurrentDepth.Should().Be(1);
            var combined = ConcatRemainder(reader, first, second);
            var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
            resumed.Read().Should().BeTrue();
            resumed.TokenType.Should().Be(XmlTokenType.EndElement);
            resumed.LocalNameEquals("a").Should().BeTrue();
        }

        [TestMethod]
        public void Resume_EverySplitOffset_DrainsNamespacedDocument()
        {
            var xml = "<?xml version=\"1.0\"?><p:a xmlns:p=\"urn:x\" p:id=\"1\">hello</p:a>"u8.ToArray();
            for (var split = 1; split < xml.Length; split++)
            {
                DrainSplit(xml, split);
            }
        }

        [TestMethod]
        public void Resume_SplitInsideAttributeValue()
        {
            var first = "<e a=\"he"u8.ToArray();
            var second = "llo\"/>"u8.ToArray();
            var reader = new Utf8XmlReader(first, false, default);
            reader.Read().Should().BeFalse();
            var combined = ConcatRemainder(reader, first, second);
            var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
            resumed.Read().Should().BeTrue();
            resumed.MoveToNextAttribute();
            resumed.GetString().Should().Be("hello");
        }

        [TestMethod]
        public void Resume_SplitInsideNamespacedStartTag()
        {
            var first = "<p:e xmlns:p=\"urn:x\""u8.ToArray();
            var second = " a=\"1\"><c/></p:e>"u8.ToArray();
            var reader = new Utf8XmlReader(first, false, default);
            reader.Read().Should().BeFalse();
            var combined = first[(int)reader.BytesConsumed..].ToArray().Concat(second).ToArray();
            var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
            resumed.Read().Should().BeTrue();
            resumed.LocalNameEquals("e").Should().BeTrue();
            resumed.NamespaceUriEquals("urn:x").Should().BeTrue();
            while (resumed.Read())
            {
            }
        }

        [TestMethod]
        public void Resume_SplitInsidePrefix()
        {
            var first = "<p"u8.ToArray();
            var second = ":e xmlns:p=\"urn:x\"/>"u8.ToArray();
            var reader = new Utf8XmlReader(first, false, default);
            reader.Read().Should().BeFalse();
            var combined = ConcatRemainder(reader, first, second);
            var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
            resumed.Read().Should().BeTrue();
            resumed.LocalNameEquals("e").Should().BeTrue();
            resumed.NamespaceUriEquals("urn:x").Should().BeTrue();
        }

        [TestMethod]
        public void Resume_SplitInsideTextThenMatchEndTag()
        {
            var first = "<a xmlns=\"urn:a\">he"u8.ToArray();
            var second = "llo</a>"u8.ToArray();
            var reader = new Utf8XmlReader(first, false, default);
            reader.Read().Should().BeTrue();
            reader.Read().Should().BeFalse();
            var combined = ConcatRemainder(reader, first, second);
            var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
            resumed.Read().Should().BeTrue();
            resumed.TokenType.Should().Be(XmlTokenType.Text);
            resumed.GetString().Should().Be("hello");
            resumed.Read().Should().BeTrue();
            resumed.TokenType.Should().Be(XmlTokenType.EndElement);
            resumed.LocalNameEquals("a").Should().BeTrue();
            resumed.NamespaceUriEquals("urn:a").Should().BeTrue();
            resumed.Read().Should().BeFalse();
        }

        [TestMethod]
        public void Resume_SplitInsideXmlnsValue()
        {
            var first = "<p:e xmlns:p=\"ur"u8.ToArray();
            var second = "n:x\"/>"u8.ToArray();
            var reader = new Utf8XmlReader(first, false, default);
            reader.Read().Should().BeFalse();
            var combined = ConcatRemainder(reader, first, second);
            var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
            resumed.Read().Should().BeTrue();
            resumed.NamespaceUriEquals("urn:x").Should().BeTrue();
        }

        [TestMethod]
        public void Resume_SplitOnEndTagKeepsNamespace()
        {
            var first = "<a xmlns=\"urn:a\">x</"u8.ToArray();
            var second = "a>"u8.ToArray();
            var reader = new Utf8XmlReader(first, false, default);
            while (reader.Read())
            {
            }

            var combined = ConcatRemainder(reader, first, second);
            var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
            resumed.Read().Should().BeTrue();
            resumed.TokenType.Should().Be(XmlTokenType.EndElement);
            resumed.LocalNameEquals("a").Should().BeTrue();
            resumed.NamespaceUriEquals("urn:a").Should().BeTrue();
        }

        [TestMethod]
        public void Resume_Xml11AfterDeclaration_KeepsVersion()
        {
            var first = "<?xml version=\"1.1\"?><e>a"u8.ToArray();
            var second = "b</e>"u8.ToArray();
            var reader = new Utf8XmlReader(first, false, default);
            while (reader.Read())
            {
            }

            reader.CurrentState.Snapshot!.Version.Should().Be(XmlVersion.Version11);
            var combined = ConcatRemainder(reader, first, second);
            var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
            resumed.Read().Should().BeTrue();
            resumed.Version.Should().Be(XmlVersion.Version11);
            while (resumed.Read())
            {
            }
        }

        #endregion

        #region Private Methods

        private static byte[] ConcatRemainder(Utf8XmlReader reader, byte[] first, byte[] second)
        {
            var consumed = (int)reader.BytesConsumed;
            return first[consumed..].Concat(second).ToArray();
        }

        private static void DrainSplit(byte[] xml, int split)
        {
            try
            {
                var first = xml[..split];
                var second = xml[split..];
                var reader = new Utf8XmlReader(first, false, default);
                while (reader.Read())
                {
                }

                var combined = ConcatRemainder(reader, first, second);
                var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
                while (resumed.Read())
                {
                }
            }
            catch (XmlException ex)
            {
                throw new XmlException($"split {split}: {ex.Message}", ex);
            }
        }

        #endregion

    }

}
