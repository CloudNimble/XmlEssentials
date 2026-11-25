using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// The namespaces of elements that are not named for a member: the items of a collection in a wrapper
    /// (<see cref="XmlArrayItemAttribute.Namespace"/>) and the element named for a derived type
    /// (<see cref="XmlDerivedTypeAttribute.Namespace"/>).
    /// </summary>
    [TestClass]
    public class XmlSerializerElementNamespaceTests
    {

        #region Fields

        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true };

        #endregion

        #region Public Methods

        [TestMethod]
        public void ArrayItemNamespace_IsTheNamespaceOfTheItemsInAWrapper()
        {
            // As System.Xml.Serialization writes them: an item with no namespace of its own is in the namespace of
            // its wrapper, an item with one declares it, and an empty namespace takes the item out of every namespace.
            var value = new NamespacedItems { Bare = [1], Inherit = [2], None = [3], Own = [4], Plain = ["a", null] };
            var xml = XmlSerializer.Serialize(value, Bare);
            xml.Should().Be(
                "<NamespacedItems>" +
                "<Bare>1</Bare>" +
                "<Inherit xmlns=\"urn:w\"><i>2</i></Inherit>" +
                "<None xmlns=\"urn:w\"><i xmlns=\"\">3</i></None>" +
                "<Own xmlns=\"urn:w\"><i xmlns=\"urn:i\">4</i></Own>" +
                "<Plain><i xmlns=\"urn:i\">a</i><i xmlns=\"urn:i\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></Plain>" +
                "</NamespacedItems>");

            var read = XmlSerializer.Deserialize<NamespacedItems>(xml, Bare)!;
            read.Bare.Should().Equal(1);
            read.Inherit.Should().Equal(2);
            read.None.Should().Equal(3);
            read.Own.Should().Equal(4);
            read.Plain.Should().Equal("a", null);

            // With no wrapper the item is the element of the member, so the attribute names nothing: the item has the
            // name and the namespace of the member.
            XmlSerializer.Serialize(new NamespacedItems { Bare = [1, 2] }, Bare).Should().Be("<NamespacedItems><Bare>1</Bare><Bare>2</Bare><Inherit xmlns=\"urn:w\"/><None xmlns=\"urn:w\"/><Own xmlns=\"urn:w\"/><Plain/></NamespacedItems>");

            // Every element in a wrapper is an item when it is read, whatever its name and namespace.
            XmlSerializer.Deserialize<NamespacedItems>("<NamespacedItems><Own xmlns=\"urn:w\"><x xmlns=\"urn:other\">9</x></Own></NamespacedItems>", Bare)!.Own.Should().Equal(9);
        }

        [TestMethod]
        public void DerivedTypeNamespace_IsTheNamespaceOfTheElementNamedForTheType()
        {
            var held = XmlSerializer.Serialize(new NamespacedShapeHolder { Shape = new NamespacedSquare { Side = 2 } }, Bare);
            held.Should().Be("<NamespacedShapeHolder xmlns=\"urn:holder\"><shape xmlns=\"urn:square\"><Side>2</Side></shape></NamespacedShapeHolder>");
            XmlSerializer.Deserialize<NamespacedShapeHolder>(held, Bare)!.Shape.Should().BeOfType<NamespacedSquare>().Which.Side.Should().Be(2);
            XmlSerializer.Deserialize<NamespacedShapeHolder>(held.Replace("urn:square", "urn:round").Replace("Side", "Radius"), Bare)!.Shape
                .Should().BeOfType<NamespacedCircle>().Which.Radius.Should().Be(2);

            var value = new NamespacedShapes
            {
                Shapes = [new NamespacedCircle { Radius = 1 }, new NamespacedSquare { Side = 3 }, new NamespacedBlob(), new NamespacedDot()],
            };
            var xml = XmlSerializer.Serialize(value, Bare);
            xml.Should().Be(
                "<NamespacedShapes xmlns=\"urn:holder\">" +
                "<Shapes>" +
                "<shape xmlns=\"urn:round\"><Radius>1</Radius></shape>" +
                "<shape xmlns=\"urn:square\"><Side>3</Side></shape>" +
                "<blob/>" +
                "<dot xmlns=\"\"/>" +
                "</Shapes>" +
                "</NamespacedShapes>");

            // Two derived types have the local name "shape". The namespace says which one an element is.
            var read = XmlSerializer.Deserialize<NamespacedShapes>(xml, Bare)!;
            read.Shapes.Should().HaveCount(4);
            read.Shapes[0].Should().BeOfType<NamespacedCircle>().Which.Radius.Should().Be(1);
            read.Shapes[1].Should().BeOfType<NamespacedSquare>().Which.Side.Should().Be(3);
            read.Shapes[2].Should().BeOfType<NamespacedBlob>();
            read.Shapes[3].Should().BeOfType<NamespacedDot>();

            // As the document element.
            var root = XmlSerializer.Serialize<NamespacedShape>(new NamespacedCircle { Radius = 5 }, Bare);
            root.Should().Be("<shape xmlns=\"urn:round\"><Radius>5</Radius></shape>");
            XmlSerializer.Deserialize<NamespacedShape>(root, Bare).Should().BeOfType<NamespacedCircle>().Which.Radius.Should().Be(5);
            XmlSerializer.Deserialize<NamespacedShape>("<shape xmlns=\"urn:square\"><Side>6</Side></shape>", Bare).Should().BeOfType<NamespacedSquare>().Which.Side.Should().Be(6);
        }

        [TestMethod]
        public void DerivedTypeNamespace_WhereNoElementIsNamedForTheType_IsRefused()
        {
            FluentActions.Invoking(() => XmlSerializer.Serialize(new NamespacedBadBase(), Bare))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "[XmlDerivedTypeAttribute] for 'CloudNimble.XmlEssentials.Tests.POCOs.NamespacedBadLeaf' on 'CloudNimble.XmlEssentials.Tests.POCOs.NamespacedBadBase' sets Namespace, " +
                    "which is the namespace of the element named for the derived type. The discriminator of 'CloudNimble.XmlEssentials.Tests.POCOs.NamespacedBadBase' is not an element name.");
        }

        #endregion

    }

}
