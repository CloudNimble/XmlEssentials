using System;
using System.Buffers;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// What the node tree and the document accept and write: a tree can only be built into something that is
    /// written as well-formed XML, a change that is refused leaves the tree as it was, and a declaration is
    /// written once.
    /// </summary>
    [TestClass]
    public class XmlNodeTreeRulesTests
    {

        #region Public Methods

        [TestMethod]
        public void AttributeList_ReplacingAnItemWithOneThatIsRefused_KeepsTheOldItem()
        {
            var element = new XmlElementNode("e");
            var kept = element.SetAttribute("a", "1");
            var elsewhere = new XmlElementNode("other").SetAttribute("b", "2");
            FluentActions.Invoking(() => element.Attributes[0] = elsewhere)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The node already has a parent.");
            element.Attributes.Count.Should().Be(1);
            ReferenceEquals(element.Attributes[0], kept).Should().BeTrue();
            ReferenceEquals(kept.Parent, element).Should().BeTrue();
            element.ToString().Should().Be("<e a=\"1\"/>");
        }

        [TestMethod]
        public void AttributeList_ReplacingAnItem_DetachesTheOldOne()
        {
            var element = new XmlElementNode("e");
            var old = element.SetAttribute("a", "1");
            var replacement = new XmlAttributeNode("b", "2");
            element.Attributes[0] = replacement;
            old.Parent.Should().BeNull();
            ReferenceEquals(replacement.Parent, element).Should().BeTrue();
            element.ToString().Should().Be("<e b=\"2\"/>");
        }

        [TestMethod]
        public void Children_AnAttribute_IsRefused()
        {
            // An attribute among the children was written as an attribute of the element, with no error.
            const string message = "An attribute cannot be a child of an element. Add it to Attributes.";
            var element = new XmlElementNode("e");
            element.Children.Add(new XmlTextNode("t"));
            FluentActions.Invoking(() => element.Children.Add(new XmlAttributeNode("a", "1"))).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message);
            FluentActions.Invoking(() => element.Children.Insert(0, new XmlAttributeNode("a", "1"))).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message);
            FluentActions.Invoking(() => element.Children[0] = new XmlAttributeNode("a", "1")).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message);
            element.ToString().Should().Be("<e>t</e>");
        }

        [TestMethod]
        public void Children_ADeclaration_IsRefused()
        {
            // A declaration among the children was written inside the element, which is not well formed.
            const string message = "An XML declaration cannot be a child of an element.";
            var element = new XmlElementNode("e");
            element.Children.Add(new XmlTextNode("t"));
            FluentActions.Invoking(() => element.Children.Add(new XmlDeclarationNode())).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message);
            FluentActions.Invoking(() => element.Children.Insert(0, new XmlDeclarationNode())).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message);
            FluentActions.Invoking(() => element.Children[0] = new XmlDeclarationNode()).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message);
            element.ToString().Should().Be("<e>t</e>");
        }

        [TestMethod]
        public void Children_ReplacingAnItemWithOneThatIsRefused_KeepsTheOldItem()
        {
            var element = new XmlElementNode("e");
            var kept = element.AddElement("kept");
            var elsewhere = new XmlElementNode("other").AddElement("taken");
            FluentActions.Invoking(() => element.Children[0] = elsewhere)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The node already has a parent.");
            element.Children.Count.Should().Be(1);
            ReferenceEquals(element.Children[0], kept).Should().BeTrue();
            ReferenceEquals(kept.Parent, element).Should().BeTrue();
            element.ToString().Should().Be("<e><kept/></e>");
        }

        [TestMethod]
        public void Children_ReplacingAnItem_DetachesTheOldOne()
        {
            var element = new XmlElementNode("e");
            var old = element.AddElement("old");
            var replacement = new XmlElementNode("new");
            element.Children[0] = replacement;
            old.Parent.Should().BeNull();
            ReferenceEquals(replacement.Parent, element).Should().BeTrue();
            element.ToString().Should().Be("<e><new/></e>");
        }

        [TestMethod]
        public void Lists_AssigningTheNodeThatIsAlreadyThere_ChangesNothing()
        {
            // The node has this element as its parent, which is what an assignment refuses. Putting a node
            // where it already is must not be refused, and must not leave it without a parent.
            var element = new XmlElementNode("e");
            var attribute = element.SetAttribute("a", "1");
            var child = element.AddElement("c");
            element.Attributes[0] = attribute;
            element.Children[0] = child;
            ReferenceEquals(attribute.Parent, element).Should().BeTrue();
            ReferenceEquals(child.Parent, element).Should().BeTrue();
            element.ToString().Should().Be("<e a=\"1\"><c/></e>");
        }

        [TestMethod]
        public void DeclarationNode_WrittenBeforeTheRoot_IsTheOnlyDeclaration()
        {
            // The writer writes a declaration of its own before the first element unless it knows one is there.
            foreach (var omit in new[] { false, true })
            {
                var buffer = new ArrayBufferWriter<byte>();
                using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = omit }))
                {
                    new XmlDeclarationNode().WriteTo(writer);
                    new XmlElementNode("r").WriteTo(writer);
                }

                Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?><r/>", "OmitXmlDeclaration = {0}", omit);
            }

            var standalone = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(standalone))
            {
                new XmlDeclarationNode { Version = "1.1", Standalone = true }.WriteTo(writer);
                new XmlElementNode("r").WriteTo(writer);
            }

            Encoding.UTF8.GetString(standalone.WrittenSpan).Should().Be("<?xml version=\"1.1\" encoding=\"utf-8\" standalone=\"yes\"?><r/>");
        }

        [TestMethod]
        public void Document_WriteTo_WritesTheDeclarationTheDocumentHas()
        {
            // The declaration is part of the document. OmitXmlDeclaration decides only whether the writer adds
            // one to a document that has none, so writing with it set gives back what was parsed.
            using var withDeclaration = XmlDocument.Parse("<?xml version=\"1.0\" encoding=\"utf-8\"?><r><c/></r>");
            using var without = XmlDocument.Parse("<r><c/></r>");
            Written(withDeclaration, omit: true).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?><r><c/></r>");
            Written(withDeclaration, omit: false).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?><r><c/></r>");
            Written(without, omit: true).Should().Be("<r><c/></r>");
            Written(without, omit: false).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?><r><c/></r>");
        }

        [TestMethod]
        public void Document_ParseValue_WithNoElementToRead_IsRejected()
        {
            const string message = "The reader is not on an element and has none left to read.";
            FluentActions.Invoking(() =>
            {
                var reader = new Utf8XmlReader("<!--only a comment-->"u8, new XmlReaderOptions { AllowMultipleRoots = true });
                XmlDocument.ParseValue(ref reader).Dispose();
            }).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message);
            FluentActions.Invoking(() =>
            {
                var reader = new Utf8XmlReader(""u8, new XmlReaderOptions { AllowMultipleRoots = true });
                XmlNode.ParseValue(ref reader);
            }).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message);
            FluentActions.Invoking(() =>
            {
                var reader = new Utf8XmlReader("<r/><!--after-->"u8, new XmlReaderOptions { AllowMultipleRoots = true });
                XmlDocument.ParseValue(ref reader).Dispose();
                reader.Read();
                XmlDocument.ParseValue(ref reader).Dispose();
            }).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message);
        }

        [TestMethod]
        public void Document_ParseValue_OnOrBeforeAnElement_ReadsThatElement()
        {
            var reader = new Utf8XmlReader("<r><a>1</a><b>2</b></r>"u8);
            reader.Read();
            reader.Read();
            using (var a = XmlDocument.ParseValue(ref reader))
            {
                a.RootElement.GetRawXml().Should().Be("<a>1</a>");
            }

            reader.Read();
            XmlNode b = XmlNode.ParseValue(ref reader);
            b.ToString().Should().Be("<b>2</b>");
        }

        [TestMethod]
        public void Node_Parse_ReturnsTheRootElement()
        {
            XmlNode fromString = XmlNode.Parse("<?xml version=\"1.0\"?><!--c--><r a=\"1\">t</r>");
            fromString.ToString().Should().Be("<r a=\"1\">t</r>");
            XmlNode fromBytes = XmlNode.Parse("<r a=\"1\">t</r>"u8);
            fromBytes.ToString().Should().Be("<r a=\"1\">t</r>");
        }

        [TestMethod]
        public void Node_ToString_OfATreeParsedFromXml11_WritesItsCharactersAsReferences()
        {
            // A node has no version of its own. Its text may hold a character only XML 1.1 allows, and one
            // that XML 1.1 normalizes when it is written literally, so both are written as references.
            XmlNode.Parse("<?xml version=\"1.1\"?><a b=\"&#1;\">x&#1;y&#x85;z</a>").ToString().Should().Be("<a b=\"&#x1;\">x&#x1;y&#x85;z</a>");
            XmlNode.Parse("<a>plain &amp; simple</a>").ToString().Should().Be("<a>plain &amp; simple</a>");
        }

        [TestMethod]
        public void NameLookups_WithoutANamespace_MatchTheLocalNameInAnyNamespace()
        {
            // The lookups that are given no namespace find the first node with the local name, by design.
            var element = new XmlElementNode("e");
            element.SetAttribute("id", "namespaced", "urn:a");
            element.SetAttribute("id", "plain");
            element.AddElement("c", "urn:c");
            element.AddElement("c");
            element.Attributes["id"]!.Value.Should().Be("namespaced");
            element.Attributes["id", "urn:a"]!.Value.Should().Be("namespaced");
            element.Attributes["id", ""]!.Value.Should().Be("plain");
            element["c"]!.NamespaceUri.Should().Be("urn:c");
            element["c", ""]!.NamespaceUri.Should().Be("");
            element.Attributes["missing"].Should().BeNull();
            element.Attributes["id", "urn:other"].Should().BeNull();
            element["missing"].Should().BeNull();
        }

        [TestMethod]
        public void ProcessingInstructionNode_Target_IsCheckedWhenItIsSet()
        {
            var node = new XmlProcessingInstructionNode("target", "data");
            FluentActions.Invoking(() => node.Target = null!).Should().ThrowExactly<ArgumentNullException>();
            FluentActions.Invoking(() => node.Target = "").Should().ThrowExactly<ArgumentException>();
            FluentActions.Invoking(() => node.Target = "  ").Should().ThrowExactly<ArgumentException>();
            node.Target.Should().Be("target");
            node.Target = "other";
            node.ToString().Should().Be("<?other data?>");
        }

        [TestMethod]
        public void SerializeToElement_GivesAnElementThatHoldsNoPooledMemory()
        {
            // Nobody can dispose the document behind the element, so it must own nothing that needs returning.
            var element = XmlSerializer.SerializeToElement(new POCOs.Person { Name = "Ada", Age = 36 }, new XmlSerializerOptions());
            element.GetRawXml().Should().Be("<Person><Name>Ada</Name><Age>36</Age></Person>");
            element.Document!.IsPooled.Should().BeFalse();
            using var document = XmlSerializer.SerializeToDocument(new POCOs.Person { Name = "Ada", Age = 36 });
            document.IsPooled.Should().BeTrue();
            document.RootElement.GetRawXml().Should().Be("<Person><Name>Ada</Name><Age>36</Age></Person>");
        }

        [TestMethod]
        public void SetAttribute_WithNoNamespace_LeavesANamespacedAttributeOfTheSameNameAlone()
        {
            var element = new XmlElementNode("e");
            var namespaced = element.SetAttribute("id", "1", "urn:a");
            var plain = element.SetAttribute("id", "2");
            ReferenceEquals(plain, namespaced).Should().BeFalse();
            namespaced.Value.Should().Be("1");
            element.Attributes.Count.Should().Be(2);
            ReferenceEquals(element.SetAttribute("id", "3"), plain).Should().BeTrue();
            ReferenceEquals(element.SetAttribute("id", "4", "urn:a"), namespaced).Should().BeTrue();
            ReferenceEquals(element.SetAttribute("id", "5", ""), plain).Should().BeTrue();
            element.Attributes.Count.Should().Be(2);
            namespaced.Value.Should().Be("4");
            plain.Value.Should().Be("5");
        }

        #endregion

        #region Private Methods

        private static string Written(XmlDocument document, bool omit)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = omit }))
            {
                document.WriteTo(writer);
            }

            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        #endregion

    }

}
