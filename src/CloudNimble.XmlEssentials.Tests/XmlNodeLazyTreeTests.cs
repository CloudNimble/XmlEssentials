using System;
using System.Buffers;
using System.Linq;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A tree made by <see cref="XmlNode.Parse(string)"/> keeps the parsed document, and an element makes its
    /// attributes and children when they are first asked for. Nothing a caller can see depends on which
    /// elements have been looked at: the tree reads, changes, copies and writes the same either way.
    /// </summary>
    [TestClass]
    public class XmlNodeLazyTreeTests
    {

        #region Fields

        private const string Catalog =
            "<c:catalog xmlns:c=\"urn:catalog\" xmlns=\"urn:items\" region=\"north &amp; east\">"
            + "<item id=\"1\"><name>Widget</name><qty>3</qty><note/></item>"
            + "<!-- between --><item id=\"2\" c:flag=\"y\"><name>Gadget &lt;2></name><qty>5</qty><note></note></item>"
            + "<?pi data?><c:tail><![CDATA[a<b]]>text</c:tail></c:catalog>";

        #endregion

        #region Public Methods

        [TestMethod]
        public void Tree_ThatNobodyLooksInto_IsWrittenAsItWasParsed()
        {
            XmlNode.Parse(Catalog).ToString().Should().Be(Catalog);
            XmlNode.Parse(Encoding.UTF8.GetBytes(Catalog)).ToString().Should().Be(Catalog);
        }

        [TestMethod]
        public void Tree_IsWrittenTheSame_HoweverMuchOfItHasBeenLookedAt()
        {
            // Looking does not change a tree. Each walk below makes a different part of it.
            var none = (XmlElementNode)XmlNode.Parse(Catalog);
            var top = (XmlElementNode)XmlNode.Parse(Catalog);
            top.Children.Count.Should().Be(5);
            top.Attributes.Count.Should().Be(3);
            var one = (XmlElementNode)XmlNode.Parse(Catalog);
            ((XmlElementNode)one.Children[2]).Attributes.Select(attribute => attribute.Prefix + ":" + attribute.LocalName).Should().Equal(":id", "c:flag");
            var all = (XmlElementNode)XmlNode.Parse(Catalog);
            all.InnerText.Should().Be("Widget3Gadget <2>5a<btext");

            foreach (var tree in new[] { none, top, one, all })
            {
                tree.ToString().Should().Be(Catalog);
                tree.DeepClone().ToString().Should().Be(Catalog);
            }
        }

        [TestMethod]
        public void Change_UnderAnElementThatWasNotLookedAt_IsWritten_AndTheRestIsKept()
        {
            var root = (XmlElementNode)XmlNode.Parse(Catalog);
            var second = (XmlElementNode)root.Children[2];
            second.SetAttribute("checked", "true");
            second["qty"]!.InnerText = "6";
            second.AddElement("stock", "urn:items").Add(new XmlTextNode("9"));
            root.ToString().Should().Be(
                "<c:catalog xmlns:c=\"urn:catalog\" xmlns=\"urn:items\" region=\"north &amp; east\">"
                + "<item id=\"1\"><name>Widget</name><qty>3</qty><note/></item>"
                + "<!-- between --><item id=\"2\" c:flag=\"y\" checked=\"true\"><name>Gadget &lt;2></name><qty>6</qty><note></note><stock>9</stock></item>"
                + "<?pi data?><c:tail><![CDATA[a<b]]>text</c:tail></c:catalog>");
        }

        [TestMethod]
        public void Rename_OfAnElementThatWasNotLookedAt_KeepsWhatIsUnderIt()
        {
            var root = (XmlElementNode)XmlNode.Parse("<a><b x=\"1\"><c>t</c></b><d></d></a>");
            var b = (XmlElementNode)root.Children[0];
            b.LocalName = "renamed";
            var d = (XmlElementNode)root.Children[1];
            d.IsEmptyElement = true;
            root.ToString().Should().Be("<a><renamed x=\"1\"><c>t</c></renamed><d/></a>");

            var prefixed = (XmlElementNode)XmlNode.Parse("<p:a xmlns:p=\"urn:p\"><p:b>t</p:b></p:a>");
            var inner = (XmlElementNode)prefixed.Children[0];
            inner.Prefix = "";
            inner.NamespaceUri = "";
            prefixed.ToString().Should().Be("<p:a xmlns:p=\"urn:p\"><b>t</b></p:a>");
        }

        [TestMethod]
        public void Element_MovedAwayFromItsDeclarations_IsWrittenTheSame_WhetherOrNotItWasLookedAt()
        {
            // An element that was not looked at is written from what was parsed, where its prefixes were declared
            // by an ancestor it no longer has. It must declare them, as the same element does once it is made.
            static string[] Moved(bool look)
            {
                var root = (XmlElementNode)XmlNode.Parse(Catalog);
                var item = (XmlElementNode)root.Children[2];
                var tail = (XmlElementNode)root.Children[4];
                if (look)
                {
                    item.InnerText.Should().Be("Gadget <2>5");
                    item.Attributes.Count.Should().Be(2);
                    tail.InnerText.Should().Be("a<btext");
                    tail.Attributes.Count.Should().Be(0);
                }

                var copy = tail.DeepClone();
                root.Children.Remove(item).Should().BeTrue();
                root.Children.Remove(tail).Should().BeTrue();
                var home = new XmlElementNode("home");
                home.Children.Add(item);
                home.Children.Add(tail);
                var other = new XmlElementNode("other");
                other.Children.Add(copy);
                return [home.ToString(), other.ToString(), root.ToString()];
            }

            string[] expected =
            [
                "<home><item xmlns=\"urn:items\" id=\"2\" xmlns:c=\"urn:catalog\" c:flag=\"y\"><name>Gadget &lt;2></name><qty>5</qty><note></note></item>"
                    + "<c:tail xmlns:c=\"urn:catalog\"><![CDATA[a<b]]>text</c:tail></home>",
                "<other><c:tail xmlns:c=\"urn:catalog\"><![CDATA[a<b]]>text</c:tail></other>",
                "<c:catalog xmlns:c=\"urn:catalog\" xmlns=\"urn:items\" region=\"north &amp; east\">"
                    + "<item id=\"1\"><name>Widget</name><qty>3</qty><note/></item><!-- between --><?pi data?></c:catalog>",
            ];
            Moved(look: true).Should().Equal(expected);
            Moved(look: false).Should().Equal(expected);
        }

        [TestMethod]
        public void Copy_OfAnElementThatWasNotLookedAt_IsATreeOfItsOwn()
        {
            var root = (XmlElementNode)XmlNode.Parse("<a><b><c>t</c></b></a>");
            var copy = (XmlElementNode)root.DeepClone();
            ((XmlElementNode)copy.Children[0])["c"]!.InnerText = "changed";
            ((XmlElementNode)root.Children[0]).SetAttribute("x", "1");
            root.ToString().Should().Be("<a><b x=\"1\"><c>t</c></b></a>");
            copy.ToString().Should().Be("<a><b><c>changed</c></b></a>");
            copy.Parent.Should().BeNull();
            copy.Children[0].Parent.Should().BeSameAs(copy);
        }

        [TestMethod]
        public void ParseValue_FromAReader_GivesATreeThatOutlivesTheReader()
        {
            var bytes = Encoding.UTF8.GetBytes("<r><e a=\"1\"><f>x</f></e><after/></r>");
            var reader = new Utf8XmlReader(bytes);
            reader.Read();
            reader.Read();
            var element = (XmlElementNode)XmlNode.ParseValue(ref reader);
            reader.Read().Should().BeTrue();
            reader.LocalNameEquals("after").Should().BeTrue();
            bytes.AsSpan().Clear();
            element.ToString().Should().Be("<e a=\"1\"><f>x</f></e>");
            element["f"]!.InnerText.Should().Be("x");
        }

        [TestMethod]
        public void Tree_WrittenInsideAnotherDocument_DeclaresTheNamespacesItNeeds()
        {
            // An element that was not looked at is written from the parsed document, into whatever is open.
            var root = (XmlElementNode)XmlNode.Parse("<p:a xmlns:p=\"urn:p\"><p:b><p:c>t</p:c></p:b></p:a>");
            var b = root.Children[0];
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true }))
            {
                writer.WriteStartElement("host");
                b.WriteTo(writer);
                writer.WriteEndElement();
            }

            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<host><p:b xmlns:p=\"urn:p\"><p:c>t</p:c></p:b></host>");
        }

        #endregion

    }

}
