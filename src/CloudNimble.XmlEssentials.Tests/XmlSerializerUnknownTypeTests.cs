using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// <see cref="XmlSerializerOptions.UnknownTypeHandling"/>: what a value declared as <see cref="object"/> is read
    /// as, the counterpart of <c>JsonSerializerOptions.UnknownTypeHandling</c>.
    /// </summary>
    [TestClass]
    public class XmlSerializerUnknownTypeTests
    {

        #region Fields

        private const string Document =
            "<UnknownHolder><Any><b>1</b></Any><Items><Object>2</Object><Object xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></Items>" +
            "<Map><k><c/></k></Map></UnknownHolder>";

        private static readonly XmlSerializerOptions AsElement = new();
        private static readonly XmlSerializerOptions AsNode = new() { UnknownTypeHandling = XmlUnknownTypeHandling.XmlNode };

        #endregion

        #region Public Methods

        [TestMethod]
        public void Default_IsXmlElement()
        {
            AsElement.UnknownTypeHandling.Should().Be(XmlUnknownTypeHandling.XmlElement);
            new XmlSerializerOptions(AsNode).UnknownTypeHandling.Should().Be(XmlUnknownTypeHandling.XmlNode);
            ((int)XmlUnknownTypeHandling.XmlElement).Should().Be(0);
            ((int)XmlUnknownTypeHandling.XmlNode).Should().Be(1);
        }

        [TestMethod]
        public void Members_Items_AndDictionaryValues_AreReadAsElements()
        {
            var read = XmlSerializer.Deserialize<UnknownHolder>(Document, AsElement)!;
            read.Any.Should().BeOfType<XmlElement>().Which.GetRawXml().Should().Be("<Any><b>1</b></Any>");
            read.Items.Should().HaveCount(2);
            read.Items[0].Should().BeOfType<XmlElement>().Which.GetRawXml().Should().Be("<Object>2</Object>");
            read.Items[1].Should().BeNull();
            read.Map.Should().ContainSingle().Which.Value.Should().BeOfType<XmlElement>().Which.GetRawXml().Should().Be("<k><c/></k>");
        }

        [TestMethod]
        public void Members_Items_AndDictionaryValues_AreReadAsNodes()
        {
            var read = XmlSerializer.Deserialize<UnknownHolder>(Document, AsNode)!;
            read.Any.Should().BeOfType<XmlElementNode>().Which.ToString().Should().Be("<Any><b>1</b></Any>");
            read.Items.Should().HaveCount(2);
            read.Items[0].Should().BeOfType<XmlElementNode>().Which.ToString().Should().Be("<Object>2</Object>");
            read.Items[1].Should().BeNull("a nil element is null whatever the handling");
            read.Map.Should().ContainSingle().Which.Value.Should().BeOfType<XmlElementNode>().Which.ToString().Should().Be("<k><c/></k>");
        }

        [TestMethod]
        public void Node_CanBeChanged()
        {
            var read = XmlSerializer.Deserialize<UnknownHolder>("<UnknownHolder><Any><b>1</b></Any></UnknownHolder>", AsNode)!;
            var node = (XmlElementNode)read.Any!;
            node.Add(new XmlElementNode("c"));
            node.ToString().Should().Be("<Any><b>1</b><c/></Any>");
        }

        [TestMethod]
        public void ReadValues_AreWrittenBackAsTheyWereRead()
        {
            const string rich =
                "<UnknownHolder><Any xmlns:p=\"urn:p\" id=\"7\" p:kind=\"x\"><p:b>1</p:b>tail</Any><Items><Object>2</Object><Object/><Object><d e=\"f\"/></Object></Items>" +
                "<Map><k>text</k><l xmlns=\"\"><m xmlns=\"urn:m\"/></l></Map></UnknownHolder>";
            foreach (var handling in new[] { XmlUnknownTypeHandling.XmlElement, XmlUnknownTypeHandling.XmlNode })
            {
                var options = new XmlSerializerOptions { UnknownTypeHandling = handling, OmitXmlDeclaration = true };
                XmlSerializer.Serialize(XmlSerializer.Deserialize<UnknownHolder>(Document, options), options).Should().Be(Document);
                XmlSerializer.Serialize(XmlSerializer.Deserialize<UnknownHolder>(rich, options), options).Should().Be(
                    "<UnknownHolder><Any xmlns:p=\"urn:p\" id=\"7\" p:kind=\"x\"><p:b>1</p:b>tail</Any><Items><Object>2</Object><Object/><Object><d e=\"f\"/></Object></Items>" +
                    "<Map><k>text</k><l><m xmlns=\"urn:m\"/></l></Map></UnknownHolder>");
                XmlSerializer.Serialize(XmlSerializer.Deserialize<object>("<a><b>1</b></a>", options), options).Should().Be("<a><b>1</b></a>");

                // An element that was read with an end tag and nothing in it is written with its end tag, and
                // indentation puts nothing between the two tags.
                const string empty = "<UnknownHolder><Any></Any><Items><Object></Object><Object a=\"1\"></Object><Object/></Items><Map><k></k></Map></UnknownHolder>";
                XmlSerializer.Serialize(XmlSerializer.Deserialize<UnknownHolder>(empty, options), options).Should().Be(empty);
                var indented = new XmlSerializerOptions(options) { NewLine = "\n", WriteIndented = true };
                XmlSerializer.Serialize(XmlSerializer.Deserialize<UnknownHolder>(empty, options), indented).Should().Be(
                    "<UnknownHolder>\n  <Any></Any>\n  <Items>\n    <Object></Object>\n    <Object a=\"1\"></Object>\n    <Object/>\n  </Items>\n  <Map>\n    <k></k>\n  </Map>\n</UnknownHolder>");
            }
        }

        [TestMethod]
        public void Element_InAnObjectMember_TakesTheNameOfTheMember()
        {
            // The member names the element. An element built under another name gives its attributes and its
            // content, as any other value gives its content and not the name of its type.
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            using var prefixed = XmlDocument.Parse("<p:x xmlns:p=\"urn:p\" id=\"1\"><p:b/>t</p:x>");
            XmlSerializer.Serialize(new UnknownHolder { Any = prefixed.RootElement }, options)
                .Should().Be("<UnknownHolder><Any xmlns:p=\"urn:p\" id=\"1\"><p:b/>t</Any><Items/><Map/></UnknownHolder>");
            XmlSerializer.Serialize(new UnknownHolder { Any = XmlNode.Parse("<p:x xmlns:p=\"urn:p\" id=\"1\"><p:b/>t</p:x>") }, options)
                .Should().Be("<UnknownHolder><Any xmlns:p=\"urn:p\" id=\"1\"><p:b/>t</Any><Items/><Map/></UnknownHolder>");

            // A default namespace the element declares is the element's own. The member is not moved into it.
            using var inDefault = XmlDocument.Parse("<x xmlns=\"urn:d\"><b/></x>");
            XmlSerializer.Serialize(new UnknownHolder { Any = inDefault.RootElement }, options)
                .Should().Be("<UnknownHolder><Any><b xmlns=\"urn:d\"/></Any><Items/><Map/></UnknownHolder>");
            XmlSerializer.Serialize(new UnknownHolder { Any = XmlNode.Parse("<x xmlns=\"urn:d\"><b/></x>") }, options)
                .Should().Be("<UnknownHolder><Any><b xmlns=\"urn:d\"/></Any><Items/><Map/></UnknownHolder>");

            using var nil = XmlDocument.Parse("<x xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>");
            XmlSerializer.Serialize(new UnknownHolder { Any = nil.RootElement }, options)
                .Should().Be("<UnknownHolder><Any xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><Items/><Map/></UnknownHolder>");

            XmlElement text = default;
            foreach (var child in prefixed.RootElement.EnumerateChildren())
            {
                text = child;
            }

            XmlSerializer.Serialize(new UnknownHolder { Any = text }, options).Should().Be("<UnknownHolder><Any>t</Any><Items/><Map/></UnknownHolder>");
            XmlSerializer.Serialize(new UnknownHolder { Any = default(XmlElement) }, options).Should().Be("<UnknownHolder><Any/><Items/><Map/></UnknownHolder>");
        }

        [TestMethod]
        public void Root_DeclaredAsObject_FollowsTheHandling()
        {
            XmlSerializer.Deserialize<object>("<a><b>1</b></a>", AsElement).Should().BeOfType<XmlElement>().Which.GetRawXml().Should().Be("<a><b>1</b></a>");
            XmlSerializer.Deserialize<object>("<a><b>1</b></a>", AsNode).Should().BeOfType<XmlElementNode>().Which.ToString().Should().Be("<a><b>1</b></a>");
        }

        #endregion

    }

}
