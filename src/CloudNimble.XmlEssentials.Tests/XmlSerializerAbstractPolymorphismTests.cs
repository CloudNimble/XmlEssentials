using System;
using System.Collections.Generic;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// An abstract class or an interface that declares its derived types can be the declared type of a document
    /// element, a member, an item written as a repeated element, and an item in a wrapper, with the
    /// discriminator in each of its three places.
    /// </summary>
    [TestClass]
    public class XmlSerializerAbstractPolymorphismTests
    {

        #region Fields

        private const string Xsi = "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"";
        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true };

        #endregion

        #region Public Methods

        [TestMethod]
        public void AbstractClass_DiscriminatorAttribute_RoundTripsEverywhere()
        {
            RoundTrip<KindShape>(
                new KindCircle { Label = "a", Radius = 2 },
                new KindSquare { Label = "b", Side = 3 },
                "<KindShape kind=\"circle\"><Label>a</Label><Radius>2</Radius></KindShape>",
                "<PolyItemOfKindShape><Item kind=\"circle\"><Label>a</Label><Radius>2</Radius></Item></PolyItemOfKindShape>",
                "<PolyListOfKindShape><Items kind=\"circle\"><Label>a</Label><Radius>2</Radius></Items><Items kind=\"square\"><Label>b</Label><Side>3</Side></Items></PolyListOfKindShape>",
                "<PolyWrappedOfKindShape><Items><KindShape kind=\"circle\"><Label>a</Label><Radius>2</Radius></KindShape><KindShape kind=\"square\"><Label>b</Label><Side>3</Side></KindShape></Items></PolyWrappedOfKindShape>");
        }

        [TestMethod]
        public void AbstractClass_ElementName_RoundTripsEverywhere()
        {
            RoundTrip<NamedShape>(
                new NamedCircle { Label = "a", Radius = 2 },
                new NamedSquare { Label = "b", Side = 3 },
                "<NamedCircle><Label>a</Label><Radius>2</Radius></NamedCircle>",
                "<PolyItemOfNamedShape><NamedCircle><Label>a</Label><Radius>2</Radius></NamedCircle></PolyItemOfNamedShape>",
                "<PolyListOfNamedShape><NamedCircle><Label>a</Label><Radius>2</Radius></NamedCircle><NamedSquare><Label>b</Label><Side>3</Side></NamedSquare></PolyListOfNamedShape>",
                "<PolyWrappedOfNamedShape><Items><NamedCircle><Label>a</Label><Radius>2</Radius></NamedCircle><NamedSquare><Label>b</Label><Side>3</Side></NamedSquare></Items></PolyWrappedOfNamedShape>");
        }

        [TestMethod]
        public void AbstractClass_XsiType_RoundTripsEverywhere()
        {
            RoundTrip<TypedShape>(
                new TypedCircle { Label = "a", Radius = 2 },
                new TypedSquare { Label = "b", Side = 3 },
                "<TypedShape " + Xsi + " xsi:type=\"circle\"><Label>a</Label><Radius>2</Radius></TypedShape>",
                "<PolyItemOfTypedShape><Item " + Xsi + " xsi:type=\"circle\"><Label>a</Label><Radius>2</Radius></Item></PolyItemOfTypedShape>",
                "<PolyListOfTypedShape><Items " + Xsi + " xsi:type=\"circle\"><Label>a</Label><Radius>2</Radius></Items><Items " + Xsi + " xsi:type=\"square\"><Label>b</Label><Side>3</Side></Items></PolyListOfTypedShape>",
                "<PolyWrappedOfTypedShape><Items><TypedShape " + Xsi + " xsi:type=\"circle\"><Label>a</Label><Radius>2</Radius></TypedShape><TypedShape " + Xsi + " xsi:type=\"square\"><Label>b</Label><Side>3</Side></TypedShape></Items></PolyWrappedOfTypedShape>");
        }

        [TestMethod]
        public void AbstractType_ElementThatNamesNoDerivedType_IsRejected()
        {
            const string message = "The type 'CloudNimble.XmlEssentials.Tests.POCOs.NamedShape' is abstract or an interface, so the element must identify one of its derived types.";
            Message<XmlException>(() => XmlSerializer.Deserialize<NamedShape>("<NamedShape><Label>a</Label></NamedShape>")).Should().Be(message + " Path: /NamedShape | LineNumber: 1 | BytePositionInLine: 13.");
            Message<XmlException>(() => XmlSerializer.Deserialize<PolyItem<NamedShape>>("<PolyItemOfNamedShape><Item><Label>a</Label></Item></PolyItemOfNamedShape>")).Should().Be(message + " Path: /PolyItemOfNamedShape/Item | LineNumber: 1 | BytePositionInLine: 29.");
            Message<XmlException>(() => XmlSerializer.Deserialize<PolyList<NamedShape>>("<PolyListOfNamedShape><Items><Label>a</Label></Items></PolyListOfNamedShape>")).Should().Be(message + " Path: /PolyListOfNamedShape/Items[0] | LineNumber: 1 | BytePositionInLine: 30.");
            Message<XmlException>(() => XmlSerializer.Deserialize<PolyWrapped<NamedShape>>("<PolyWrappedOfNamedShape><Items><NamedShape><Label>a</Label></NamedShape></Items></PolyWrappedOfNamedShape>")).Should().Be(message + " Path: /PolyWrappedOfNamedShape/Items/NamedShape[0] | LineNumber: 1 | BytePositionInLine: 45.");
            Message<XmlException>(() => XmlSerializer.Deserialize<NamedShape>("<Other/>")).Should().Be("The root element does not match 'NamedShape'. Path: /NamedShape | LineNumber: 1 | BytePositionInLine: 9.");
        }

        [TestMethod]
        public void AbstractType_MissingDiscriminator_IsRejected()
        {
            Message<XmlException>(() => XmlSerializer.Deserialize<KindShape>("<KindShape><Label>a</Label></KindShape>"))
                .Should().Be("The type 'CloudNimble.XmlEssentials.Tests.POCOs.KindShape' is abstract or an interface, so the element must identify one of its derived types. Path: /KindShape | LineNumber: 1 | BytePositionInLine: 12.");
            Message<XmlException>(() => XmlSerializer.Deserialize<IKindShape>("<IKindShape><Label>a</Label></IKindShape>"))
                .Should().Be("The type 'CloudNimble.XmlEssentials.Tests.POCOs.IKindShape' is abstract or an interface, so the element must identify one of its derived types. Path: /IKindShape | LineNumber: 1 | BytePositionInLine: 13.");
            Message<XmlException>(() => XmlSerializer.Deserialize<PolyItem<TypedShape>>("<PolyItemOfTypedShape><Item><Label>a</Label></Item></PolyItemOfTypedShape>"))
                .Should().Be("The type 'CloudNimble.XmlEssentials.Tests.POCOs.TypedShape' is abstract or an interface, so the element must identify one of its derived types. Path: /PolyItemOfTypedShape/Item | LineNumber: 1 | BytePositionInLine: 29.");
            Message<XmlException>(() => XmlSerializer.Deserialize<PolyList<ITypedShape>>("<PolyListOfITypedShape><Items><Label>a</Label></Items></PolyListOfITypedShape>"))
                .Should().Be("The type 'CloudNimble.XmlEssentials.Tests.POCOs.ITypedShape' is abstract or an interface, so the element must identify one of its derived types. Path: /PolyListOfITypedShape/Items[0] | LineNumber: 1 | BytePositionInLine: 31.");
        }

        [TestMethod]
        public void AbstractType_NilMember_ReadsAsNull()
        {
            var xml = XmlSerializer.Serialize(new PolyItem<KindShape>(), Bare);
            xml.Should().Be("<PolyItemOfKindShape><Item " + Xsi + " xsi:nil=\"true\"/></PolyItemOfKindShape>");
            XmlSerializer.Deserialize<PolyItem<KindShape>>(xml)!.Item.Should().BeNull();
            XmlSerializer.Deserialize<PolyItem<IKindShape>>("<PolyItemOfIKindShape><Item " + Xsi + " xsi:nil=\"true\"/></PolyItemOfIKindShape>")!.Item.Should().BeNull();
        }

        [TestMethod]
        public void AbstractType_TypeInfo_DescribesItsOwnMembersAndCreatesNothing()
        {
            foreach (var type in new[] { typeof(NamedShape), typeof(INamedShape), typeof(KindShape), typeof(IKindShape), typeof(TypedShape), typeof(ITypedShape) })
            {
                var info = Bare.GetTypeInfo(type);
                info.Type.Should().Be(type);
                info.CreateObject.Should().BeNull("{0} cannot be created", type);
                info.Properties.Should().HaveCount(1);
                info.Properties[0].Name.Should().Be(new XmlName("Label"));
            }
        }

        [TestMethod]
        public void AbstractType_UnknownDiscriminator_IsRejected()
        {
            Message<XmlException>(() => XmlSerializer.Deserialize<KindShape>("<KindShape kind=\"hexagon\"/>")).Should().Be("Unknown type discriminator 'hexagon'. Path: /KindShape | LineNumber: 1 | BytePositionInLine: 28.");
            Message<XmlException>(() => XmlSerializer.Deserialize<ITypedShape>("<ITypedShape " + Xsi + " xsi:type=\"hexagon\"/>")).Should().Be("Unknown type discriminator 'hexagon'. Path: /ITypedShape | LineNumber: 1 | BytePositionInLine: 88.");
            Message<XmlException>(() => XmlSerializer.Deserialize<PolyItem<IKindShape>>("<PolyItemOfIKindShape><Item kind=\"hexagon\"/></PolyItemOfIKindShape>")).Should().Be("Unknown type discriminator 'hexagon'. Path: /PolyItemOfIKindShape/Item | LineNumber: 1 | BytePositionInLine: 45.");
        }

        [TestMethod]
        public void AbstractType_UndeclaredDerivedType_IsNotWritten()
        {
            var triangle = new NamedTriangle { Label = "t", Height = 4 };
            const string ofClass = "The type 'CloudNimble.XmlEssentials.Tests.POCOs.NamedTriangle' is not a declared derived type of 'CloudNimble.XmlEssentials.Tests.POCOs.NamedShape'.";
            const string ofInterface = "The type 'CloudNimble.XmlEssentials.Tests.POCOs.NamedTriangle' is not a declared derived type of 'CloudNimble.XmlEssentials.Tests.POCOs.INamedShape'.";
            Message<NotSupportedException>(() => XmlSerializer.Serialize<NamedShape>(triangle, Bare)).Should().Be(ofClass);
            Message<NotSupportedException>(() => XmlSerializer.Serialize(new PolyItem<NamedShape> { Item = triangle }, Bare)).Should().Be(ofClass);
            Message<NotSupportedException>(() => XmlSerializer.Serialize(new PolyList<NamedShape> { Items = [triangle] }, Bare)).Should().Be(ofClass);
            Message<NotSupportedException>(() => XmlSerializer.Serialize(new PolyWrapped<NamedShape> { Items = [triangle] }, Bare)).Should().Be(ofClass);
            Message<NotSupportedException>(() => XmlSerializer.Serialize<INamedShape>(triangle, Bare)).Should().Be(ofInterface);
            Message<NotSupportedException>(() => XmlSerializer.Serialize(new PolyItem<INamedShape> { Item = triangle }, Bare)).Should().Be(ofInterface);
        }

        [TestMethod]
        public void AbstractType_WithFallBackToBaseType_WritesItsOwnMembersAndCannotReadThemBack()
        {
            XmlSerializer.Serialize<LooseShape>(new LooseCircle { Label = "a", Radius = 2 }, Bare).Should().Be("<LooseCircle><Label>a</Label><Radius>2</Radius></LooseCircle>");
            var xml = XmlSerializer.Serialize<LooseShape>(new LooseTriangle { Label = "t", Height = 4 }, Bare);
            xml.Should().Be("<LooseShape><Label>t</Label></LooseShape>");
            Message<XmlException>(() => XmlSerializer.Deserialize<LooseShape>(xml))
                .Should().Be("The type 'CloudNimble.XmlEssentials.Tests.POCOs.LooseShape' is abstract or an interface, so the element must identify one of its derived types. Path: /LooseShape | LineNumber: 1 | BytePositionInLine: 13.");
        }

        [TestMethod]
        public void AbstractType_WithNoDerivedTypes_IsNotSupported()
        {
            const string ofClass = "The type 'CloudNimble.XmlEssentials.Tests.POCOs.LonelyShape' is not supported.";
            const string ofInterface = "The type 'CloudNimble.XmlEssentials.Tests.POCOs.ILonelyShape' is not supported.";
            Message<NotSupportedException>(() => Bare.GetTypeInfo<LonelyShape>()).Should().Be(ofClass);
            Message<NotSupportedException>(() => XmlSerializer.Serialize<LonelyShape?>(null, Bare)).Should().Be(ofClass);
            Message<NotSupportedException>(() => XmlSerializer.Deserialize<LonelyShape>("<LonelyShape/>")).Should().Be(ofClass);
            Message<NotSupportedException>(() => XmlSerializer.Serialize(new PolyItem<LonelyShape>(), Bare)).Should().Be(ofClass);
            Message<NotSupportedException>(() => Bare.GetTypeInfo<ILonelyShape>()).Should().Be(ofInterface);
            Message<NotSupportedException>(() => XmlSerializer.Serialize<ILonelyShape?>(null, Bare)).Should().Be(ofInterface);
            Message<NotSupportedException>(() => XmlSerializer.Deserialize<ILonelyShape>("<ILonelyShape/>")).Should().Be(ofInterface);
            Message<NotSupportedException>(() => XmlSerializer.Serialize(new PolyList<ILonelyShape>(), Bare)).Should().Be(ofInterface);
        }

        [TestMethod]
        public void Interface_DiscriminatorAttribute_RoundTripsEverywhere()
        {
            RoundTrip<IKindShape>(
                new KindCircle { Label = "a", Radius = 2 },
                new KindSquare { Label = "b", Side = 3 },
                "<IKindShape kind=\"circle\"><Label>a</Label><Radius>2</Radius></IKindShape>",
                "<PolyItemOfIKindShape><Item kind=\"circle\"><Label>a</Label><Radius>2</Radius></Item></PolyItemOfIKindShape>",
                "<PolyListOfIKindShape><Items kind=\"circle\"><Label>a</Label><Radius>2</Radius></Items><Items kind=\"square\"><Label>b</Label><Side>3</Side></Items></PolyListOfIKindShape>",
                "<PolyWrappedOfIKindShape><Items><IKindShape kind=\"circle\"><Label>a</Label><Radius>2</Radius></IKindShape><IKindShape kind=\"square\"><Label>b</Label><Side>3</Side></IKindShape></Items></PolyWrappedOfIKindShape>");
        }

        [TestMethod]
        public void Interface_ElementName_RoundTripsEverywhere()
        {
            RoundTrip<INamedShape>(
                new NamedCircle { Label = "a", Radius = 2 },
                new NamedSquare { Label = "b", Side = 3 },
                "<NamedCircle><Label>a</Label><Radius>2</Radius></NamedCircle>",
                "<PolyItemOfINamedShape><NamedCircle><Label>a</Label><Radius>2</Radius></NamedCircle></PolyItemOfINamedShape>",
                "<PolyListOfINamedShape><NamedCircle><Label>a</Label><Radius>2</Radius></NamedCircle><NamedSquare><Label>b</Label><Side>3</Side></NamedSquare></PolyListOfINamedShape>",
                "<PolyWrappedOfINamedShape><Items><NamedCircle><Label>a</Label><Radius>2</Radius></NamedCircle><NamedSquare><Label>b</Label><Side>3</Side></NamedSquare></Items></PolyWrappedOfINamedShape>");
        }

        [TestMethod]
        public void Interface_XsiType_RoundTripsEverywhere()
        {
            RoundTrip<ITypedShape>(
                new TypedCircle { Label = "a", Radius = 2 },
                new TypedSquare { Label = "b", Side = 3 },
                "<ITypedShape " + Xsi + " xsi:type=\"circle\"><Label>a</Label><Radius>2</Radius></ITypedShape>",
                "<PolyItemOfITypedShape><Item " + Xsi + " xsi:type=\"circle\"><Label>a</Label><Radius>2</Radius></Item></PolyItemOfITypedShape>",
                "<PolyListOfITypedShape><Items " + Xsi + " xsi:type=\"circle\"><Label>a</Label><Radius>2</Radius></Items><Items " + Xsi + " xsi:type=\"square\"><Label>b</Label><Side>3</Side></Items></PolyListOfITypedShape>",
                "<PolyWrappedOfITypedShape><Items><ITypedShape " + Xsi + " xsi:type=\"circle\"><Label>a</Label><Radius>2</Radius></ITypedShape><ITypedShape " + Xsi + " xsi:type=\"square\"><Label>b</Label><Side>3</Side></ITypedShape></Items></PolyWrappedOfITypedShape>");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns the message of the exception of exactly type <typeparamref name="TException"/> that <paramref name="action"/> throws.
        /// </summary>
        /// <typeparam name="TException">The exception type.</typeparam>
        /// <param name="action">The call.</param>
        /// <returns>The message.</returns>
        private static string Message<TException>(Action action)
            where TException : Exception
        {
            return FluentActions.Invoking(action).Should().ThrowExactly<TException>().Which.Message;
        }

        /// <summary>
        /// Writes a circle as the document element and as a member, and a circle and a square as repeated and
        /// as wrapped items, all declared as <typeparamref name="TBase"/>; then reads each document back and
        /// checks the run-time types and that writing them again gives the same document.
        /// </summary>
        /// <typeparam name="TBase">The declared type: an abstract class or an interface.</typeparam>
        /// <param name="circle">The first value.</param>
        /// <param name="square">The second value.</param>
        /// <param name="root">The expected document with the circle as its document element.</param>
        /// <param name="member">The expected document with the circle as a member.</param>
        /// <param name="list">The expected document with both as repeated elements.</param>
        /// <param name="wrapped">The expected document with both in a wrapper.</param>
        private static void RoundTrip<TBase>(TBase circle, TBase square, string root, string member, string list, string wrapped)
            where TBase : class
        {
            XmlSerializer.Serialize(circle, Bare).Should().Be(root);
            var readRoot = XmlSerializer.Deserialize<TBase>(root)!;
            readRoot.Should().BeOfType(circle.GetType());
            XmlSerializer.Serialize(readRoot, Bare).Should().Be(root);

            XmlSerializer.Serialize(new PolyItem<TBase> { Item = circle }, Bare).Should().Be(member);
            var readMember = XmlSerializer.Deserialize<PolyItem<TBase>>(member)!;
            readMember.Item.Should().BeOfType(circle.GetType());
            XmlSerializer.Serialize(readMember, Bare).Should().Be(member);

            XmlSerializer.Serialize(new PolyList<TBase> { Items = [circle, square] }, Bare).Should().Be(list);
            var readList = XmlSerializer.Deserialize<PolyList<TBase>>(list)!;
            TypesOf(readList.Items).Should().Equal(circle.GetType(), square.GetType());
            XmlSerializer.Serialize(readList, Bare).Should().Be(list);

            XmlSerializer.Serialize(new PolyWrapped<TBase> { Items = [circle, square] }, Bare).Should().Be(wrapped);
            var readWrapped = XmlSerializer.Deserialize<PolyWrapped<TBase>>(wrapped)!;
            TypesOf(readWrapped.Items).Should().Equal(circle.GetType(), square.GetType());
            XmlSerializer.Serialize(readWrapped, Bare).Should().Be(wrapped);
        }

        private static List<Type> TypesOf<TBase>(List<TBase> items)
            where TBase : class
        {
            var types = new List<Type>();
            foreach (var item in items)
            {
                types.Add(item.GetType());
            }

            return types;
        }

        #endregion

    }

}
