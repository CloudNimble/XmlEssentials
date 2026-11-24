using System;
using System.Collections.Generic;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
#if NET11_0_OR_GREATER
using CloudNimble.XmlEssentials.Tests.POCOs.Closed;
#endif
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// <see cref="XmlSerializerOptions.InferClosedTypePolymorphism"/> and
    /// <see cref="XmlPolymorphicAttribute.InferClosedTypePolymorphism"/>: the derived types of a closed type are
    /// taken from the list the compiler makes, as System.Text.Json takes them.
    /// </summary>
    [TestClass]
    public class XmlSerializerClosedTypeTests
    {

        #region Fields

#if NET11_0_OR_GREATER
        private const string Closed = "CloudNimble.XmlEssentials.Tests.POCOs.Closed.";

#endif
        private const string Types = "CloudNimble.XmlEssentials.Tests.POCOs.";

        #endregion

        #region Public Methods

        [TestMethod]
        public void InferClosedTypePolymorphism_IsOffUntilItIsAskedFor()
        {
            new XmlSerializerOptions().InferClosedTypePolymorphism.Should().BeFalse();
            new XmlPolymorphicAttribute().InferClosedTypePolymorphism.Should().BeFalse();
            new XmlPolymorphicAttribute { InferClosedTypePolymorphism = true }.InferClosedTypePolymorphism.Should().BeTrue();
            new XmlSerializerOptions(new XmlSerializerOptions { InferClosedTypePolymorphism = true }).InferClosedTypePolymorphism.Should().BeTrue();
        }

        [TestMethod]
        public void Attribute_OnATypeThatIsNotClosed_IsRefused()
        {
            // The attribute asks for derived types that only a closed type has. Before .NET 11 no type is closed.
            FluentActions.Invoking(() => XmlSerializer.Serialize(new OpenInferred(), Inferring()))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(
                    "The type '" + Types + "OpenInferred' enables XmlPolymorphicAttribute.InferClosedTypePolymorphism but is not a closed type, " +
                    "so no derived types can be inferred. Declare the type 'closed' or register its derived types using XmlDerivedTypeAttribute.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<OpenInferredAbstract>("<OpenInferredAbstract/>", new XmlSerializerOptions()))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(
                    "The type '" + Types + "OpenInferredAbstract' enables XmlPolymorphicAttribute.InferClosedTypePolymorphism but is not a closed type, " +
                    "so no derived types can be inferred. Declare the type 'closed' or register its derived types using XmlDerivedTypeAttribute.");
        }

#if NET11_0_OR_GREATER
        [TestMethod]
        public void ClosedType_WithTheOption_WritesAndReadsItsDerivedTypes()
        {
            var options = Inferring();

            // A derived type is named after itself. One that is closed too cannot be created, so the types that
            // derive from it stand in its place.
            XmlSerializer.Serialize<ClosedShape>(new ClosedCircle { Radius = 1 }, options).Should().Be("<ClosedCircle><Radius>1</Radius></ClosedCircle>");
            XmlSerializer.Serialize<ClosedShape>(new ClosedSquare { Sides = 4 }, options).Should().Be("<ClosedSquare><Sides>4</Sides></ClosedSquare>");
            XmlSerializer.Serialize<ClosedShape>(new ClosedBlob(), options).Should().Be("<ClosedBlob/>");
            XmlSerializer.Serialize<ClosedPolygon>(new ClosedSquare { Sides = 4 }, options).Should().Be("<ClosedSquare><Sides>4</Sides></ClosedSquare>");

            XmlSerializer.Deserialize<ClosedShape>("<ClosedCircle><Radius>1</Radius></ClosedCircle>", options).Should().BeOfType<ClosedCircle>().Which.Radius.Should().Be(1);
            XmlSerializer.Deserialize<ClosedShape>("<ClosedSquare><Sides>4</Sides></ClosedSquare>", options).Should().BeOfType<ClosedSquare>().Which.Sides.Should().Be(4);
            XmlSerializer.Deserialize<ClosedShape>("<ClosedBlob/>", options).Should().BeOfType<ClosedBlob>();
            XmlSerializer.Deserialize<ClosedPolygon>("<ClosedSquare><Sides>4</Sides></ClosedSquare>", options).Should().BeOfType<ClosedSquare>();

            var held = XmlSerializer.Serialize(new ClosedShapeHolder { Shape = new ClosedCircle { Radius = 2 } }, options);
            held.Should().Be("<ClosedShapeHolder><ClosedCircle><Radius>2</Radius></ClosedCircle></ClosedShapeHolder>");
            XmlSerializer.Deserialize<ClosedShapeHolder>(held, options)!.Shape.Should().BeOfType<ClosedCircle>().Which.Radius.Should().Be(2);

            var list = XmlSerializer.Serialize(new ClosedShapeList { Shapes = [new ClosedCircle { Radius = 3 }, new ClosedSquare { Sides = 4 }, new ClosedBlob()] }, options);
            list.Should().Be("<ClosedShapeList><Shapes><ClosedCircle><Radius>3</Radius></ClosedCircle><ClosedSquare><Sides>4</Sides></ClosedSquare><ClosedBlob/></Shapes></ClosedShapeList>");
            var read = XmlSerializer.Deserialize<ClosedShapeList>(list, options)!;
            read.Shapes.Should().HaveCount(3);
            read.Shapes[0].Should().BeOfType<ClosedCircle>();
            read.Shapes[1].Should().BeOfType<ClosedSquare>();
            read.Shapes[2].Should().BeOfType<ClosedBlob>();

            // The name of a derived type goes through the naming policy, as a declared discriminator does.
            var camel = Inferring();
            camel.PropertyNamingPolicy = XmlNamingPolicy.CamelCase;
            XmlSerializer.Serialize<ClosedShape>(new ClosedCircle { Radius = 1 }, camel).Should().Be("<closedCircle><radius>1</radius></closedCircle>");
        }

        [TestMethod]
        public void ClosedType_WithoutTheOption_IsATypeWithNoDerivedTypes()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            FluentActions.Invoking(() => XmlSerializer.Serialize<ClosedShape>(new ClosedCircle(), options))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be("The type '" + Closed + "ClosedShape' is not supported.");
        }

        [TestMethod]
        public void Attribute_TakesPrecedenceOverTheOption()
        {
            var off = new XmlSerializerOptions { OmitXmlDeclaration = true };
            XmlSerializer.Serialize<ClosedAsked>(new ClosedAskedLeaf(), off).Should().Be("<ClosedAskedLeaf/>");
            XmlSerializer.Deserialize<ClosedAsked>("<ClosedAskedLeaf/>", off).Should().BeOfType<ClosedAskedLeaf>();

            FluentActions.Invoking(() => XmlSerializer.Serialize<ClosedRefused>(new ClosedRefusedLeaf(), Inferring()))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be("The type '" + Closed + "ClosedRefused' is not supported.");
        }

        [TestMethod]
        public void DeclaredDerivedTypes_AreTheWholeList()
        {
            // System.Text.Json has the same rule: a type that declares a derived type has nothing inferred.
            var options = Inferring();
            XmlSerializer.Serialize<ClosedDeclared>(new ClosedDeclaredFirst(), options).Should().Be("<first/>");
            FluentActions.Invoking(() => XmlSerializer.Serialize<ClosedDeclared>(new ClosedDeclaredSecond(), options))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be("The type '" + Closed + "ClosedDeclaredSecond' is not a declared derived type of '" + Closed + "ClosedDeclared'.");
        }

        [TestMethod]
        public void Discriminator_InAnAttribute_IsTheNameOfTheDerivedType()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            var xml = XmlSerializer.Serialize<ClosedKind>(new ClosedKindLeaf { Value = 5 }, options);
            xml.Should().Be("<ClosedKind kind=\"ClosedKindLeaf\"><Value>5</Value></ClosedKind>");
            XmlSerializer.Deserialize<ClosedKind>(xml, options).Should().BeOfType<ClosedKindLeaf>().Which.Value.Should().Be(5);
        }

        [TestMethod]
        public void GenericClosedType_TakesTheDerivedTypesThatFitItsArguments()
        {
            var options = Inferring();

            // The name of a generic type is written without its arity.
            XmlSerializer.Serialize<ClosedBox<int>>(new ClosedFull<int> { Value = 1 }, options).Should().Be("<ClosedFull><Value>1</Value></ClosedFull>");
            XmlSerializer.Serialize<ClosedBox<int>>(new ClosedIntBox(), options).Should().Be("<ClosedIntBox/>");
            XmlSerializer.Serialize<ClosedBox<int>>(new ClosedValueBox<int>(), options).Should().Be("<ClosedValueBox/>");
            XmlSerializer.Deserialize<ClosedBox<int>>("<ClosedFull><Value>1</Value></ClosedFull>", options).Should().BeOfType<ClosedFull<int>>().Which.Value.Should().Be(1);
            XmlSerializer.Deserialize<ClosedBox<int>>("<ClosedIntBox/>", options).Should().BeOfType<ClosedIntBox>();
            XmlSerializer.Deserialize<ClosedBox<int>>("<ClosedValueBox/>", options).Should().BeOfType<ClosedValueBox<int>>();

            // A box of pairs has a derived type that takes two arguments, which the one argument of the box does not give it.
            XmlSerializer.Serialize<ClosedBox<KeyValuePair<int, int>>>(new ClosedFull<KeyValuePair<int, int>>(), options).Should().Be("<ClosedFull><Value><Key>0</Key><Value>0</Value></Value></ClosedFull>");
            FluentActions.Invoking(() => XmlSerializer.Serialize<ClosedBox<KeyValuePair<int, int>>>(new ClosedPairBox<int, int>(), options))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "The type '" + Closed + "ClosedPairBox`2[System.Int32,System.Int32]' is not a declared derived type of " +
                    "'" + Closed + "ClosedBox`1[System.Collections.Generic.KeyValuePair`2[System.Int32,System.Int32]]'.");

            // A box of strings: the box of integers, and the box that takes only value types, are not its derived types.
            XmlSerializer.Serialize<ClosedBox<string>>(new ClosedFull<string> { Value = "v" }, options).Should().Be("<ClosedFull><Value>v</Value></ClosedFull>");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<ClosedBox<string>>("<ClosedIntBox/>", options))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The root element does not match 'ClosedBoxOfString'. Path: /ClosedBoxOfString | LineNumber: 1 | BytePositionInLine: 16.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<ClosedBox<string>>("<ClosedValueBox/>", options))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The root element does not match 'ClosedBoxOfString'. Path: /ClosedBoxOfString | LineNumber: 1 | BytePositionInLine: 18.");
        }

        [TestMethod]
        public void ClosedType_WithNoDerivedTypes_HasNothingToInfer()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<ClosedAlone>("<ClosedAlone/>", Inferring()))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The type '" + Closed + "ClosedAlone' is abstract or an interface, so the element must identify one of its derived types. Path: /ClosedAlone | LineNumber: 1 | BytePositionInLine: 15.");
        }
#endif

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns options that infer the derived types of closed types.
        /// </summary>
        /// <returns>The options.</returns>
        private static XmlSerializerOptions Inferring()
        {
            return new XmlSerializerOptions { InferClosedTypePolymorphism = true, OmitXmlDeclaration = true };
        }

        #endregion

    }

}
