using System;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Namespace stack: default xmlns, prefixes, unprefixed attributes, 1.1 undeclaration.
    /// </summary>
    [TestClass]
    public class Utf8XmlReaderNamespaceTests
    {

        #region Public Methods

        [TestMethod]
        public void PrefixedAttribute_OnChildWithoutXmlns_Resolves()
        {
            XmlReaderHarness.MustRead("<p:r xmlns:p=\"urn:x\"><p:e p:a=\"1\"/></p:r>");
        }

        [TestMethod]
        [DataRow("<a:e xmlns:a=\"urn:x\" xmlns:b=\"urn:x\"></b:e>", DisplayName = "another prefix bound to the same namespace")]
        [DataRow("<a:e xmlns:a=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:b=\"http://www.w3.org/2001/XMLSchema-instance\"></b:e>", DisplayName = "another prefix bound to a namespace the reader knows")]
        [DataRow("<e xmlns=\"urn:x\" xmlns:b=\"urn:x\"></b:e>", DisplayName = "a prefix where the start tag used the default namespace")]
        [DataRow("<b:e xmlns=\"urn:x\" xmlns:b=\"urn:x\"></e>", DisplayName = "no prefix where the start tag had one")]
        [DataRow("<a:r xmlns:a=\"urn:x\" xmlns:b=\"urn:x\"><a:e>1</b:e></a:r>", DisplayName = "a child element")]
        public void EndTag_WithADifferentPrefixBoundToTheSameNamespace_IsRejected(string xml)
        {
            // XML 1.0 section 3, WFC "Element Type Match": the Name in the end tag must match the start tag's.
            string? message = null;
            try
            {
                XmlReaderHarness.Drain(xml);
            }
            catch (XmlException ex)
            {
                message = ex.Message;
            }

            message.Should().Be("End tag does not match start tag.");
        }

        [TestMethod]
        public void EndTag_WithADifferentPrefix_IsRejectedWhenTheStartTagWasInAnEarlierBuffer()
        {
            var xml = "<a:r xmlns:a=\"urn:x\" xmlns:b=\"urn:x\"><a:e>text</b:e></a:r>"u8.ToArray();
            for (var split = 1; split < xml.Length; split++)
            {
                string? message = null;
                try
                {
                    var first = new Utf8XmlReader(xml.AsSpan(0, split), isFinalBlock: false, new XmlReaderState(new XmlReaderOptions()));
                    while (first.Read())
                    {
                    }

                    var second = new Utf8XmlReader(xml.AsSpan((int)first.BytesConsumed), isFinalBlock: true, first.CurrentState);
                    while (second.Read())
                    {
                    }
                }
                catch (XmlException ex)
                {
                    message = ex.Message;
                }

                message.Should().Be("End tag does not match start tag.", "a split at byte {0} must not hide the mismatch", split);
            }
        }

        [TestMethod]
        public void EndTag_WithTheSamePrefix_MatchesWhenTheStartTagWasInAnEarlierBuffer()
        {
            var xml = "<a:r xmlns:a=\"urn:x\" xmlns:b=\"urn:y\"><b:e>text</b:e><f/></a:r>"u8.ToArray();
            var options = new XmlReaderOptions();
            var expected = XmlReaderHarness.Tokens(xml, options);
            expected.Should().Equal(
                "StartElement a:r{urn:x} open @xmlns:a{http://www.w3.org/2000/xmlns/}=[urn:x] @xmlns:b{http://www.w3.org/2000/xmlns/}=[urn:y]",
                "StartElement b:e{urn:y} open",
                "Text :{} [text]",
                "EndElement b:e{urn:y}",
                "StartElement :f{} empty",
                "EndElement a:r{urn:x}");
            for (var split = 1; split < xml.Length; split++)
            {
                XmlReaderHarness.TokensSplitAt(xml, split, options).Should().Equal(expected, "the tokens must not depend on a split at byte {0}", split);
            }
        }

        [TestMethod]
        public void DefaultXmlns_AppliesToUnprefixedElementNotAttribute()
        {
            var reader = new Utf8XmlReader("<e xmlns=\"urn:x\" a=\"1\"/>"u8);
            reader.Read();
            reader.NamespaceUriEquals("urn:x").Should().BeTrue();
            reader.MoveToNextAttribute();
            reader.LocalNameEquals("xmlns").Should().BeTrue();
            reader.NamespaceUriEquals(XmlNamespaces.Xmlns).Should().BeTrue();
            reader.MoveToNextAttribute();
            reader.LocalNameEquals("a").Should().BeTrue();
            reader.NamespaceUriSpan.IsEmpty.Should().BeTrue();
        }

        [TestMethod]
        public void DuplicateLocalDifferentNamespace_Allowed()
        {
            XmlReaderHarness.MustRead("<e xmlns:p=\"urn:p\" a=\"1\" p:a=\"2\"/>");
        }

        [TestMethod]
        public void DuplicateSameNamespace_Throws()
        {
            XmlReaderHarness.MustThrow("<e xmlns:p=\"urn:p\" xmlns:q=\"urn:p\" p:a=\"1\" q:a=\"2\"/>");
        }

        [TestMethod]
        public void DuplicateXmlnsPrefix_Throws()
        {
            XmlReaderHarness.MustThrow("<e xmlns:p=\"urn:a\" xmlns:p=\"urn:b\"/>");
            XmlReaderHarness.MustThrow("<e xmlns=\"urn:a\" xmlns=\"urn:b\"/>");
        }

        [TestMethod]
        public void EmptyElement_KeepsNamespaceUntilNextRead()
        {
            var reader = new Utf8XmlReader("<a xmlns=\"urn:a\"><b xmlns:p=\"urn:p\" p:c=\"1\"/></a>"u8);
            reader.Read();
            reader.Read();
            reader.IsEmptyElement.Should().BeTrue();
            reader.NamespaceUriEquals("urn:a").Should().BeTrue();
            reader.MoveToNextAttribute();
            reader.MoveToNextAttribute();
            reader.LocalNameEquals("c").Should().BeTrue();
            reader.NamespaceUriEquals("urn:p").Should().BeTrue();
            reader.MoveToElement();
            reader.Read();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            reader.NamespaceUriEquals("urn:a").Should().BeTrue();
        }

        [TestMethod]
        public void EndElement_KeepsNamespaceUriOfEndedElement()
        {
            var reader = new Utf8XmlReader("<a xmlns=\"urn:a\">x</a>"u8);
            reader.Read();
            reader.NamespaceUriEquals("urn:a").Should().BeTrue();
            reader.Read();
            reader.TokenType.Should().Be(XmlTokenType.Text);
            reader.Read();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            reader.LocalNameEquals("a").Should().BeTrue();
            reader.NamespaceUriEquals("urn:a").Should().BeTrue();
            reader.CurrentDepth.Should().Be(0);
        }

        [TestMethod]
        public void EndTagUriMismatch_Throws()
        {
            XmlReaderHarness.MustThrow("<p:e xmlns:p=\"urn:x\" xmlns:q=\"urn:y\"></q:e>");
            XmlReaderHarness.MustThrow("<a xmlns=\"urn:a\"></b>");
        }

        [TestMethod]
        public void GetNil_IgnoresUnprefixedAndFalse()
        {
            var xsi = new Utf8XmlReader("<e xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>"u8);
            xsi.Read();
            xsi.GetNil().Should().BeTrue();

            var one = new Utf8XmlReader("<e xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"1\"/>"u8);
            one.Read();
            one.GetNil().Should().BeTrue();

            var local = new Utf8XmlReader("<e nil=\"true\"/>"u8);
            local.Read();
            local.GetNil().Should().BeFalse();

            var falsy = new Utf8XmlReader("<e xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"false\"/>"u8);
            falsy.Read();
            falsy.GetNil().Should().BeFalse();
        }

        [TestMethod]
        public void GetNil_RequiresXsiNamespace()
        {
            var xsi = new Utf8XmlReader("<e xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>"u8);
            xsi.Read();
            xsi.GetNil().Should().BeTrue();
            var local = new Utf8XmlReader("<e nil=\"true\"/>"u8);
            local.Read();
            local.GetNil().Should().BeFalse();
        }

        [TestMethod]
        public void NestedScope_InnerDefaultHidesOuterUntilEnd()
        {
            var reader = new Utf8XmlReader("<a xmlns=\"urn:a\"><b xmlns=\"urn:b\"/><c/></a>"u8);
            reader.Read();
            reader.NamespaceUriEquals("urn:a").Should().BeTrue();
            reader.Read();
            reader.LocalNameEquals("b").Should().BeTrue();
            reader.NamespaceUriEquals("urn:b").Should().BeTrue();
            reader.Read();
            reader.LocalNameEquals("c").Should().BeTrue();
            reader.NamespaceUriEquals("urn:a").Should().BeTrue();
            reader.Read();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            reader.LocalNameEquals("a").Should().BeTrue();
            reader.NamespaceUriEquals("urn:a").Should().BeTrue();
        }

        [TestMethod]
        public void NestedScope_PrefixShadowThenRestore()
        {
            var reader = new Utf8XmlReader("<a xmlns:p=\"urn:a\"><p:b xmlns:p=\"urn:b\"><p:c/></p:b><p:d/></a>"u8);
            reader.Read();
            reader.Read();
            reader.LocalNameEquals("b").Should().BeTrue();
            reader.NamespaceUriEquals("urn:b").Should().BeTrue();
            reader.Read();
            reader.LocalNameEquals("c").Should().BeTrue();
            reader.NamespaceUriEquals("urn:b").Should().BeTrue();
            reader.Read();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            reader.NamespaceUriEquals("urn:b").Should().BeTrue();
            reader.Read();
            reader.LocalNameEquals("d").Should().BeTrue();
            reader.NamespaceUriEquals("urn:a").Should().BeTrue();
        }

        [TestMethod]
        public void NoNamespace_UnprefixedDocument()
        {
            var reader = new Utf8XmlReader("<e a=\"1\"></e>"u8);
            reader.Read();
            reader.NamespaceUriSpan.IsEmpty.Should().BeTrue();
            reader.MoveToNextAttribute();
            reader.NamespaceUriSpan.IsEmpty.Should().BeTrue();
            reader.Read();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            reader.NamespaceUriSpan.IsEmpty.Should().BeTrue();
        }

        [TestMethod]
        public void Prefix_BindsElementAndAttribute()
        {
            var reader = new Utf8XmlReader("<p:e xmlns:p=\"urn:p\" p:a=\"1\"/>"u8);
            reader.Read();
            reader.LocalNameEquals("e").Should().BeTrue();
            reader.NamespaceUriEquals("urn:p").Should().BeTrue();
            while (reader.MoveToNextAttribute())
            {
                if (reader.LocalNameEquals("a"))
                {
                    reader.NamespaceUriEquals("urn:p").Should().BeTrue();
                    return;
                }
            }

            Assert.Fail("missing p:a");
        }

        [TestMethod]
        public void PrefixUndeclare_OnPrefixedElement_Throws()
        {
            XmlReaderHarness.MustThrow("<?xml version=\"1.1\"?><p:e xmlns:p=\"\"/>");
            XmlReaderHarness.MustThrow("<p:e xmlns:p=\"\"/>");
        }

        [TestMethod]
        public void PrefixUndeclare_Xml10_Throws()
        {
            XmlReaderHarness.MustThrow("<e xmlns:p=\"urn:x\" xmlns:p=\"\"/>");
            XmlReaderHarness.MustThrow("<a xmlns:p=\"urn:x\"><b xmlns:p=\"\"/></a>");
        }

        [TestMethod]
        public void PrefixUndeclare_Xml11_Unbinds()
        {
            XmlReaderHarness.MustRead("<?xml version=\"1.1\"?><a xmlns:p=\"urn:x\"><b xmlns:p=\"\"><c/></b></a>");
            XmlReaderHarness.MustThrow("<?xml version=\"1.1\"?><a xmlns:p=\"urn:x\"><b xmlns:p=\"\"><p:c/></b></a>");
        }

        [TestMethod]
        public void UndeclaredPrefix_Throws()
        {
            XmlReaderHarness.MustThrow("<p:e/>");
            XmlReaderHarness.MustThrow("<e p:a=\"1\"/>");
            XmlReaderHarness.MustThrow("<a xmlns:p=\"urn:x\"><q:b/></a>");
        }

        [TestMethod]
        public void XmlnsEmpty_UnbindsDefault()
        {
            var reader = new Utf8XmlReader("<a xmlns=\"urn:a\"><b xmlns=\"\"/></a>"u8);
            reader.Read();
            reader.Read();
            reader.LocalNameEquals("b").Should().BeTrue();
            reader.NamespaceUriSpan.IsEmpty.Should().BeTrue();
            reader.Read();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            reader.NamespaceUriEquals("urn:a").Should().BeTrue();
        }

        [TestMethod]
        public void XmlnsPrefixMustNotBeDeclared()
        {
            XmlReaderHarness.MustThrow("<e xmlns:xmlns=\"urn:x\"/>");
        }

        [TestMethod]
        public void XmlPrefix_IsAlwaysBound()
        {
            var reader = new Utf8XmlReader("<e xml:lang=\"en\"/>"u8);
            reader.Read();
            reader.MoveToNextAttribute();
            reader.LocalNameEquals("lang").Should().BeTrue();
            reader.NamespaceUriEquals(XmlNamespaces.Xml).Should().BeTrue();
        }

        [TestMethod]
        public void XmlPrefixRebound_SameUri_Succeeds()
        {
            XmlReaderHarness.MustRead("<e xmlns:xml=\"http://www.w3.org/XML/1998/namespace\" xml:lang=\"en\"/>");
        }

        [TestMethod]
        public void XmlPrefixRebound_Throws()
        {
            XmlReaderHarness.MustThrow("<e xmlns:xml=\"urn:x\"/>");
        }

        [TestMethod]
        [DataRow("<p:e/>")]
        [DataRow("<e p:a=\"1\"/>")]
        [DataRow("<e xmlns:p=\"urn:x\" xmlns:p=\"urn:y\"/>")]
        [DataRow("<e xmlns:xmlns=\"urn:x\"/>")]
        [DataRow("<e xmlns:xml=\"urn:x\"/>")]
        [DataRow("<a xmlns:p=\"urn:x\"><b xmlns:p=\"\"/></a>")]
        [DataRow("<p:a xmlns:p=\"urn:x\"></q:a>")]
        [DataRow("<a xmlns=\"urn:a\"></b>")]
        public void NamespacedDocument_Malformed_Throws(string xml)
        {
            XmlReaderHarness.MustThrow(xml);
        }

        #endregion

    }

}
