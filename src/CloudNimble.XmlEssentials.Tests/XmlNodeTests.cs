using System;
using System.Collections;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Mutable tree tests for <see cref="XmlNode"/>.
    /// </summary>
    [TestClass]
    public class XmlNodeTests
    {

        #region Public Methods

        [TestMethod]
        public void Element_AddAndRemove()
        {
            var root = new XmlElementNode("root");
            var child = root.AddElement("a");
            child.InnerText = "1";
            root.Children.Count.Should().Be(1);
            root["a"]!.InnerText.Should().Be("1");
            root.Children.Remove(child).Should().BeTrue();
            child.Parent.Should().BeNull();
        }

        [TestMethod]
        public void Element_SetAttributeReplaces()
        {
            var root = new XmlElementNode("e");
            root.SetAttribute("a", "1");
            root.SetAttribute("a", "2");
            root.Attributes.Count.Should().Be(1);
            root.Attributes["a"]!.Value.Should().Be("2");
        }

        [TestMethod]
        public void NodeList_RejectsNodeWithParent()
        {
            var a = new XmlElementNode("a");
            var b = new XmlElementNode("b");
            a.Add(b);
            var threw = false;
            try
            {
                new XmlElementNode("c").Add(b);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void CommentCDataPi_Write()
        {
            var root = new XmlElementNode("e");
            root.Add(new XmlCommentNode("c"));
            root.Add(new XmlCDataNode("x"));
            root.Add(new XmlProcessingInstructionNode("pi", "d"));
            root.ToString().Should().Be("<e><!--c--><![CDATA[x]]><?pi d?></e>");
        }

        [TestMethod]
        public void Attribute_NullAssignments_BecomeEmpty()
        {
            var attribute = new XmlAttributeNode("a", "1", "urn:x", "p")
            {
                NamespaceUri = null,
                Prefix = null!,
                Value = null!,
            };
            attribute.NamespaceUri.Should().BeEmpty();
            attribute.Prefix.Should().BeEmpty();
            attribute.Value.Should().BeEmpty();
            attribute.NamespaceUri = "urn:y";
            attribute.Prefix = "q";
            attribute.Value = "2";
            attribute.NamespaceUri.Should().Be("urn:y");
            attribute.Prefix.Should().Be("q");
            attribute.Value.Should().Be("2");
        }

        [TestMethod]
        public void Element_SetAttributeNull_ClearsExistingValue()
        {
            var root = new XmlElementNode("e");
            root.SetAttribute("a", "1");
            root.SetAttribute("a", null).Value.Should().BeEmpty();
            root.Attributes.Count.Should().Be(1);
        }

        [TestMethod]
        public void DeclarationNode_StandaloneVariants()
        {
            new XmlDeclarationNode().ToString().Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            new XmlDeclarationNode { Standalone = true }.ToString().Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>");
            new XmlDeclarationNode { Standalone = false }.ToString().Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?>");
        }

        [TestMethod]
        public void DeclarationNode_Writes()
        {
            var node = new XmlDeclarationNode { Version = "1.1", Standalone = false };
            node.ToString().Should().Be("<?xml version=\"1.1\" encoding=\"utf-8\" standalone=\"no\"?>");
        }

        [TestMethod]
        public void NodeList_IndexAndCopy()
        {
            var root = new XmlElementNode("e");
            var a = new XmlTextNode("a");
            var b = new XmlTextNode("b");
            root.Children.Add(a);
            root.Children.Insert(0, b);
            root.Children.IndexOf(a).Should().Be(1);
            root.Children.Contains(b).Should().BeTrue();
            var array = new XmlNode[2];
            root.Children.CopyTo(array, 0);
            array[0].Should().BeSameAs(b);
            array[1].Should().BeSameAs(a);
            root.Children[1] = new XmlTextNode("c");
            root.Children.RemoveAt(0);
            root.ToString().Should().Be("<e>c</e>");
            root.Children.Clear();
            root.Children.Count.Should().Be(0);
            root.ToString().Should().Be("<e/>");
        }

        [TestMethod]
        public void NodeLists_OfAnElementWithNothingInIt_AreEmptyAndRefuseAPosition()
        {
            // An element holds no array for its children or its attributes until it has one.
            var root = new XmlElementNode("e");
            var text = new XmlTextNode("a");
            var attribute = new XmlAttributeNode("a", "1");
            root.Children.Count.Should().Be(0);
            root.Children.IndexOf(text).Should().Be(-1);
            root.Children.Contains(text).Should().BeFalse();
            root.Children.Remove(text).Should().BeFalse();
            root.Children.Should().BeEmpty();
            var nodes = new XmlNode[] { text };
            root.Children.CopyTo(nodes, 1);
            nodes[0].Should().BeSameAs(text);
            root.Attributes.Count.Should().Be(0);
            root.Attributes.IndexOf(attribute).Should().Be(-1);
            root.Attributes.Remove(attribute).Should().BeFalse();
            root.Attributes["a"].Should().BeNull();
            var attributes = new[] { attribute };
            root.Attributes.CopyTo(attributes, 1);
            attributes[0].Should().BeSameAs(attribute);
            root.ToString().Should().Be("<e/>");

            FluentActions.Invoking(() => root.Children[0]).Should().ThrowExactly<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => root.Children.RemoveAt(0)).Should().ThrowExactly<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => root.Children.Insert(1, text)).Should().ThrowExactly<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => root.Children.CopyTo(null!, 0)).Should().ThrowExactly<ArgumentNullException>();
            FluentActions.Invoking(() => root.Attributes[0]).Should().ThrowExactly<ArgumentOutOfRangeException>();

            // A node that a position refused has no parent, so it can still be added.
            text.Parent.Should().BeNull();
        }

        [TestMethod]
        public void NodeList_InsertAndRemove_KeepTheOrderOfTheOthers()
        {
            var root = new XmlElementNode("e");
            foreach (var name in new[] { "a", "b", "c", "d", "e" })
            {
                root.AddElement(name);
            }

            root.Children.Insert(2, new XmlElementNode("x"));
            root.Children.Insert(0, new XmlElementNode("y"));
            root.Children.Insert(root.Children.Count, new XmlElementNode("z"));
            root.ToString().Should().Be("<e><y/><a/><b/><x/><c/><d/><e/><z/></e>");
            root.Children.RemoveAt(3);
            var first = root.Children[0];
            root.Children.Remove(first).Should().BeTrue();
            first.Parent.Should().BeNull();
            root.Children.RemoveAt(root.Children.Count - 1);
            root.ToString().Should().Be("<e><a/><b/><c/><d/><e/></e>");
        }

        [TestMethod]
        public void AttributeList_Indexers()
        {
            var root = new XmlElementNode("e");
            root.SetAttribute("a", "1", "urn:x");
            root.Attributes["a", "urn:x"]!.Value.Should().Be("1");
            root.Attributes.Contains(root.Attributes[0]).Should().BeTrue();
            root.Attributes.IndexOf(root.Attributes[0]).Should().Be(0);
            var copy = new XmlAttributeNode[1];
            root.Attributes.CopyTo(copy, 0);
            copy[0].Should().BeSameAs(root.Attributes[0]);
            root.Attributes.RemoveAt(0);
            root.Attributes.Count.Should().Be(0);
        }

        [TestMethod]
        public void ParseValue_FromReader()
        {
            var reader = new Utf8XmlReader("<e>z</e>"u8);
            var node = XmlNode.ParseValue(ref reader);
            node!.ToString().Should().Be("<e>z</e>");
        }

        [TestMethod]
        public void Attribute_SettersAndPrefixedWrite()
        {
            var attribute = new XmlAttributeNode("a", "1", "urn:x", "p");
            attribute.LocalName = "b";
            attribute.Prefix = "q";
            attribute.NamespaceUri = "urn:y";
            attribute.Value = "2";
            var clone = (XmlAttributeNode)attribute.DeepClone();
            (clone.Prefix + ":" + clone.LocalName + "{" + clone.NamespaceUri + "}=" + clone.Value).Should().Be("q:b{urn:y}=2");
            clone.Should().NotBeSameAs(attribute);
            var holder = new XmlElementNode("e");
            holder.Attributes.Add(new XmlAttributeNode("a", "1", "urn:x", "p"));
            holder.ToString().Should().Be("<e xmlns:p=\"urn:x\" p:a=\"1\"/>");
        }

        [TestMethod]
        public void Element_PrefixedWriteAndIndexer()
        {
            var root = new XmlElementNode("e", "urn:x", "p");
            root.Prefix = "p";
            root.NamespaceUri = "urn:x";
            root.LocalName = "e";
            var child = root.AddElement("c", "urn:x");
            root["c", "urn:x"].Should().BeSameAs(child);
            root["missing"].Should().BeNull();
            root["c", "urn:no"].Should().BeNull();
            root.InnerText = "";
            root.InnerText.Should().BeEmpty();
            root.SetAttribute("a", "1", "urn:x");
            root.SetAttribute("a", "2", "urn:x");
            root.ToString().Should().Be("<p:e xmlns:p=\"urn:x\" p:a=\"2\"/>");
        }

        [TestMethod]
        public void LeafNodes_DeepClone()
        {
            ((XmlTextNode)new XmlTextNode("t").DeepClone()).Value.Should().Be("t");
            ((XmlCDataNode)new XmlCDataNode("c").DeepClone()).Value.Should().Be("c");
            ((XmlCommentNode)new XmlCommentNode("h").DeepClone()).Value.Should().Be("h");
            ((XmlProcessingInstructionNode)new XmlProcessingInstructionNode("pi", "d").DeepClone()).Target.Should().Be("pi");
            new XmlDeclarationNode { Standalone = true }.DeepClone().ToString().Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>");
        }

        [TestMethod]
        public void AttributeList_InsertReplaceRemove()
        {
            var root = new XmlElementNode("e");
            var a = new XmlAttributeNode("a", "1");
            var b = new XmlAttributeNode("b", "2");
            root.Attributes.Add(a);
            root.Attributes.Insert(0, b);
            root.Attributes[1] = new XmlAttributeNode("c", "3");
            root.Attributes.Remove(b).Should().BeTrue();
            root.ToString().Should().Be("<e c=\"3\"/>");
            root.Attributes.IsReadOnly.Should().BeFalse();
            root.Attributes.Clear();
            root.ToString().Should().Be("<e/>");
        }

        [TestMethod]
        public void Node_NamespaceUriDefaultSet()
        {
            var text = new XmlTextNode("x");
            text.NamespaceUri = "urn:x";
            text.NamespaceUri.Should().BeNull();
        }

        [TestMethod]
        public void Leaf_NullCtors_WriteInsideElement()
        {
            var root = new XmlElementNode("e");
            root.Add(new XmlTextNode());
            root.Add(new XmlCDataNode());
            root.Add(new XmlCommentNode());
            root.Add(new XmlProcessingInstructionNode("pi"));
            root.ToString().Should().Be("<e><![CDATA[]]><!----><?pi?></e>");
            new XmlTextNode().ValueKind.Should().Be(XmlValueKind.Text);
            new XmlCDataNode().ValueKind.Should().Be(XmlValueKind.CData);
            new XmlCommentNode().ValueKind.Should().Be(XmlValueKind.Comment);
            new XmlProcessingInstructionNode("pi").ValueKind.Should().Be(XmlValueKind.ProcessingInstruction);
            new XmlDeclarationNode().ValueKind.Should().Be(XmlValueKind.XmlDeclaration);
            new XmlAttributeNode("a").ValueKind.Should().Be(XmlValueKind.Attribute);
            new XmlElementNode("e").ValueKind.Should().Be(XmlValueKind.Element);
            root.Children.Remove(new XmlTextNode("missing")).Should().BeFalse();
            root.Attributes.Remove(new XmlAttributeNode("z")).Should().BeFalse();
            var enumerated = new System.Collections.Generic.List<XmlValueKind>();
            foreach (XmlNode child in root)
            {
                enumerated.Add(child.ValueKind);
            }

            enumerated.Should().Equal(XmlValueKind.Text, XmlValueKind.CData, XmlValueKind.Comment, XmlValueKind.ProcessingInstruction);
        }

        [TestMethod]
        public void Parse_Utf8()
        {
            var node = XmlNode.Parse("<e a=\"1\">x</e>"u8);
            node.Should().BeOfType<XmlElementNode>();
            ((XmlElementNode)node!).Attributes["a"]!.Value.Should().Be("1");
        }

        [TestMethod]
        public void NodeList_GetItemAndNonGenericEnumerator()
        {
            var root = new XmlElementNode("e");
            var text = new XmlTextNode("a");
            root.Children.Add(text);
            root.Children[0].Should().BeSameAs(text);
            root.Children.IsReadOnly.Should().BeFalse();
            var n = 0;
            foreach (var _ in (IEnumerable)root.Children)
            {
                n++;
            }

            n.Should().Be(1);
        }

        [TestMethod]
        public void AttributeList_ParentAndNonGenericEnumerator()
        {
            var root = new XmlElementNode("e");
            var attribute = new XmlAttributeNode("a", "1");
            root.Attributes.Add(attribute);
            var n = 0;
            foreach (var _ in (IEnumerable)root.Attributes)
            {
                n++;
            }

            n.Should().Be(1);
            var threw = false;
            try
            {
                new XmlElementNode("f").Attributes.Add(attribute);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Element_DefaultNamespaceWrite_AndDeepCloneAttributes()
        {
            var root = new XmlElementNode("e", "urn:x");
            root.Prefix.Should().BeEmpty();
            root.NamespaceUri = null;
            root.NamespaceUri.Should().BeEmpty();
            root.Prefix = null!;
            root.Prefix.Should().BeEmpty();
            root.NamespaceUri = "urn:x";
            root.SetAttribute("a", "1");
            root.Attributes.Add(new XmlAttributeNode("xmlns", "urn:x"));
            ((XmlElementNode)root.DeepClone()).Attributes["a"]!.Value.Should().Be("1");
            root.ToString().Should().Be("<e a=\"1\" xmlns=\"urn:x\"/>");
        }

        [TestMethod]
        public void Element_InnerTextFromCData_AndNonGenericEnumerator()
        {
            var root = new XmlElementNode("e");
            root.Add(new XmlCDataNode("z"));
            root.InnerText.Should().Be("z");
            var n = 0;
            foreach (var _ in (IEnumerable)root)
            {
                n++;
            }

            n.Should().Be(1);
        }

        #endregion

    }

}
