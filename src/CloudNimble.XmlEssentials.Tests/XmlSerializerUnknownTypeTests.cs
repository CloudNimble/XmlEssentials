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
        public void Root_DeclaredAsObject_FollowsTheHandling()
        {
            XmlSerializer.Deserialize<object>("<a><b>1</b></a>", AsElement).Should().BeOfType<XmlElement>().Which.GetRawXml().Should().Be("<a><b>1</b></a>");
            XmlSerializer.Deserialize<object>("<a><b>1</b></a>", AsNode).Should().BeOfType<XmlElementNode>().Which.ToString().Should().Be("<a><b>1</b></a>");
        }

        #endregion

    }

}
