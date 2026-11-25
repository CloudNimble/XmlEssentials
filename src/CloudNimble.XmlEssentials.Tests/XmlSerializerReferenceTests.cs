using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// <see cref="XmlReferenceHandler"/> on a root, a member, an item written as a repeated element, and an item
    /// inside a wrapper, for an object that repeats without a cycle, a cycle, and an object that holds itself.
    /// </summary>
    [TestClass]
    public class XmlSerializerReferenceTests
    {

        #region Fields

        private const string Refs = "xmlns:ns1=\"urn:CloudNimble.Text.Xml:refs\"";
        private const string Nil = "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"";
        private static readonly XmlSerializerOptions IgnoreCycles = new() { OmitXmlDeclaration = true, ReferenceHandler = XmlReferenceHandler.IgnoreCycles };
        private static readonly XmlSerializerOptions Keyed = new() { OmitXmlDeclaration = true, ReferenceHandler = new KeyedReferenceHandler() };
        private static readonly XmlSerializerOptions Preserve = new() { OmitXmlDeclaration = true, ReferenceHandler = XmlReferenceHandler.Preserve };

        #endregion

        #region Public Methods

        [TestMethod]
        public void CustomHandler_Item_WritesItsOwnIdsAndRefs()
        {
            var a = new RefLeaf { Name = "a" };
            var xml = XmlSerializer.Serialize(new RefList { Items = [a, a] }, Keyed);
            xml.Should().Be("<RefList " + Refs + " ns1:id=\"k1\"><Items ns1:id=\"k2\"><Name>a</Name></Items><Items ns1:ref=\"k2\"/></RefList>");
            var read = XmlSerializer.Deserialize<RefList>(xml, Keyed)!;
            read.Items.Should().HaveCount(2);
            read.Items[0]!.Name.Should().Be("a");
            ReferenceEquals(read.Items[0], read.Items[1]).Should().BeTrue();
        }

        [TestMethod]
        public void CustomHandler_Member_WritesItsOwnIdsAndRefs()
        {
            var a = new RefLeaf { Name = "a" };
            var xml = XmlSerializer.Serialize(new RefPair { First = a, Second = a }, Keyed);
            xml.Should().Be("<RefPair " + Refs + " ns1:id=\"k1\"><First ns1:id=\"k2\"><Name>a</Name></First><Second ns1:ref=\"k2\"/></RefPair>");
            var read = XmlSerializer.Deserialize<RefPair>(xml, Keyed)!;
            read.First!.Name.Should().Be("a");
            ReferenceEquals(read.First, read.Second).Should().BeTrue();
        }

        [TestMethod]
        public void CustomHandler_WithEmptyIdentifiers_WritesNoIdAndNilForAnObjectSeenBefore()
        {
            // An empty identifier cannot be referred to, so nothing is written for it, and an object the
            // resolver has already seen is replaced by nil: as a member, as an item, and when it is a cycle.
            var nameless = new XmlSerializerOptions { OmitXmlDeclaration = true, ReferenceHandler = new NamelessReferenceHandler() };
            var a = new RefLeaf { Name = "a" };
            XmlSerializer.Serialize(new RefPair { First = a, Second = a }, nameless).Should().Be(
                "<RefPair><First><Name>a</Name></First><Second " + Nil + "/></RefPair>");
            XmlSerializer.Serialize(new RefList { Items = [a, a] }, nameless).Should().Be(
                "<RefList><Items><Name>a</Name></Items><Items " + Nil + "/></RefList>");
            var self = new RefLeaf { Name = "s" };
            self.Children.Add(self);
            XmlSerializer.Serialize(self, nameless).Should().Be("<RefLeaf><Children " + Nil + "/><Name>s</Name></RefLeaf>");
        }

        [TestMethod]
        public void IgnoreCycles_AncestorAmongItems_IsWrittenAsNil()
        {
            var root = new RefLeaf { Name = "r" };
            var a = new RefLeaf { Name = "a" };
            root.Children.Add(a);
            a.Children.Add(root);
            var xml = XmlSerializer.Serialize(root, IgnoreCycles);
            xml.Should().Be("<RefLeaf><Children><Children " + Nil + "/><Name>a</Name></Children><Name>r</Name></RefLeaf>");
            var read = XmlSerializer.Deserialize<RefLeaf>(xml, IgnoreCycles)!;
            read.Name.Should().Be("r");
            read.Children.Should().HaveCount(1);
            read.Children[0]!.Name.Should().Be("a");
            read.Children[0]!.Children.Should().Equal([null]);
        }

        [TestMethod]
        public void IgnoreCycles_AncestorAsMember_IsWrittenAsNil()
        {
            var a = new Node { Name = "a" };
            var b = new Node { Name = "b", Child = a };
            a.Child = b;
            XmlSerializer.Serialize(a, IgnoreCycles).Should().Be("<Node><Child><Child " + Nil + "/><Name>b</Name></Child><Name>a</Name></Node>");
        }

        [TestMethod]
        public void IgnoreCycles_AncestorInAWrapper_IsWrittenAsNil()
        {
            // The wrapped list belongs to the root, and one of its items holds an item that is itself.
            var a = new RefLeaf { Name = "a" };
            a.Children.Add(a);
            XmlSerializer.Serialize(new RefWrapped { Items = [a] }, IgnoreCycles).Should().Be(
                "<RefWrapped><Items><RefLeaf><Children " + Nil + "/><Name>a</Name></RefLeaf></Items></RefWrapped>");
        }

        [TestMethod]
        public void IgnoreCycles_ObjectSharedByTwoBranches_IsWrittenInBoth()
        {
            var shared = new RefLeaf { Name = "c" };
            var a = new RefLeaf { Name = "a", Children = [shared] };
            var b = new RefLeaf { Name = "b", Children = [shared] };
            XmlSerializer.Serialize(new RefPair { First = a, Second = b }, IgnoreCycles).Should().Be(
                "<RefPair><First><Children><Name>c</Name></Children><Name>a</Name></First><Second><Children><Name>c</Name></Children><Name>b</Name></Second></RefPair>");
        }

        [TestMethod]
        public void IgnoreCycles_RepeatedDerivedItem_IsWrittenEachTime()
        {
            var circle = new KindCircle { Label = "a", Radius = 2 };
            XmlSerializer.Serialize(new PolyList<KindShape> { Items = [circle, circle] }, IgnoreCycles).Should().Be(
                "<PolyListOfKindShape><Items kind=\"circle\"><Label>a</Label><Radius>2</Radius></Items><Items kind=\"circle\"><Label>a</Label><Radius>2</Radius></Items></PolyListOfKindShape>");
            XmlSerializer.Serialize(new PolyList<NamedShape> { Items = [new NamedCircle { Label = "a", Radius = 2 }] }, IgnoreCycles).Should().Be(
                "<PolyListOfNamedShape><NamedCircle><Label>a</Label><Radius>2</Radius></NamedCircle></PolyListOfNamedShape>");
        }

        [TestMethod]
        public void IgnoreCycles_RepeatedItem_IsWrittenEachTime()
        {
            var a = new RefLeaf { Name = "a" };
            var xml = XmlSerializer.Serialize(new RefList { Items = [a, a] }, IgnoreCycles);
            xml.Should().Be("<RefList><Items><Name>a</Name></Items><Items><Name>a</Name></Items></RefList>");
            var read = XmlSerializer.Deserialize<RefList>(xml, IgnoreCycles)!;
            read.Items.Should().HaveCount(2);
            read.Items[0]!.Name.Should().Be("a");
            read.Items[1]!.Name.Should().Be("a");
        }

        [TestMethod]
        public void IgnoreCycles_RepeatedMember_IsWrittenEachTime()
        {
            var a = new RefLeaf { Name = "a" };
            var xml = XmlSerializer.Serialize(new RefPair { First = a, Second = a }, IgnoreCycles);
            xml.Should().Be("<RefPair><First><Name>a</Name></First><Second><Name>a</Name></Second></RefPair>");
            var read = XmlSerializer.Deserialize<RefPair>(xml, IgnoreCycles)!;
            read.First!.Name.Should().Be("a");
            read.Second!.Name.Should().Be("a");
        }

        [TestMethod]
        public void IgnoreCycles_RepeatedWrappedItem_IsWrittenEachTime()
        {
            var a = new RefLeaf { Name = "a" };
            XmlSerializer.Serialize(new RefWrapped { Items = [a, a] }, IgnoreCycles).Should().Be(
                "<RefWrapped><Items><RefLeaf><Name>a</Name></RefLeaf><RefLeaf><Name>a</Name></RefLeaf></Items></RefWrapped>");
        }

        [TestMethod]
        public void IgnoreCycles_SelfAmongItems_IsWrittenAsNil()
        {
            var a = new RefLeaf { Name = "a" };
            a.Children.Add(a);
            var xml = XmlSerializer.Serialize(a, IgnoreCycles);
            xml.Should().Be("<RefLeaf><Children " + Nil + "/><Name>a</Name></RefLeaf>");
            var read = XmlSerializer.Deserialize<RefLeaf>(xml, IgnoreCycles)!;
            read.Name.Should().Be("a");
            read.Children.Should().Equal([null]);
        }

        [TestMethod]
        public void IgnoreCycles_SelfAsMember_IsWrittenAsNil()
        {
            var node = new Node { Name = "root" };
            node.Child = node;
            XmlSerializer.Serialize(node, IgnoreCycles).Should().Be("<Node><Child " + Nil + "/><Name>root</Name></Node>");
        }

        [TestMethod]
        public void IgnoreCycles_SameWriterOptionsTwice_StartsEachDocumentFresh()
        {
            var a = new RefLeaf { Name = "a" };
            var pair = new RefPair { First = a, Second = a };
            const string expected = "<RefPair><First><Name>a</Name></First><Second><Name>a</Name></Second></RefPair>";
            XmlSerializer.Serialize(pair, IgnoreCycles).Should().Be(expected);
            XmlSerializer.Serialize(pair, IgnoreCycles).Should().Be(expected);
        }

        [TestMethod]
        public void Preserve_AncestorAmongItems_IsWrittenAsARef()
        {
            var root = new RefLeaf { Name = "r" };
            var a = new RefLeaf { Name = "a" };
            root.Children.Add(a);
            a.Children.Add(root);
            var xml = XmlSerializer.Serialize(root, Preserve);
            xml.Should().Be("<RefLeaf " + Refs + " ns1:id=\"1\"><Children ns1:id=\"2\"><Children ns1:ref=\"1\"/><Name>a</Name></Children><Name>r</Name></RefLeaf>");
            var read = XmlSerializer.Deserialize<RefLeaf>(xml, Preserve)!;
            read.Name.Should().Be("r");
            read.Children.Should().HaveCount(1);
            read.Children[0]!.Name.Should().Be("a");
            read.Children[0]!.Children.Should().HaveCount(1);
            ReferenceEquals(read.Children[0]!.Children[0], read).Should().BeTrue();
        }

        [TestMethod]
        public void Preserve_ItemThatIsAlsoAMemberOfAnotherItem_IsWrittenOnce()
        {
            var a = new RefLeaf { Name = "a" };
            var b = new RefLeaf { Name = "b", Children = [a] };
            var xml = XmlSerializer.Serialize(new RefList { Items = [a, b] }, Preserve);
            xml.Should().Be("<RefList " + Refs + " ns1:id=\"1\"><Items ns1:id=\"2\"><Name>a</Name></Items><Items ns1:id=\"3\"><Children ns1:ref=\"2\"/><Name>b</Name></Items></RefList>");
            var read = XmlSerializer.Deserialize<RefList>(xml, Preserve)!;
            read.Items.Should().HaveCount(2);
            read.Items[1]!.Name.Should().Be("b");
            ReferenceEquals(read.Items[0], read.Items[1]!.Children[0]).Should().BeTrue();
        }

        [TestMethod]
        public void Preserve_RepeatedDerivedValue_IsWrittenOnceWhereverTheDiscriminatorIs()
        {
            // A value of a derived type is written by the derived type's converter, with the discriminator on
            // the element. It gets an id and is referred to like any other object.
            var named = new NamedCircle { Label = "a", Radius = 2 };
            var namedXml = XmlSerializer.Serialize(new PolyList<NamedShape> { Items = [named, named] }, Preserve);
            namedXml.Should().Be("<PolyListOfNamedShape " + Refs + " ns1:id=\"1\"><NamedCircle ns1:id=\"2\"><Label>a</Label><Radius>2</Radius></NamedCircle><NamedCircle ns1:ref=\"2\"/></PolyListOfNamedShape>");
            var namedRead = XmlSerializer.Deserialize<PolyList<NamedShape>>(namedXml, Preserve)!;
            namedRead.Items.Should().HaveCount(2);
            namedRead.Items[0].Should().BeOfType<NamedCircle>();
            ReferenceEquals(namedRead.Items[0], namedRead.Items[1]).Should().BeTrue();

            var kind = new KindCircle { Label = "a", Radius = 2 };
            var kindXml = XmlSerializer.Serialize(new PolyList<IKindShape> { Items = [kind, kind] }, Preserve);
            kindXml.Should().Be("<PolyListOfIKindShape " + Refs + " ns1:id=\"1\"><Items kind=\"circle\" ns1:id=\"2\"><Label>a</Label><Radius>2</Radius></Items><Items kind=\"circle\" ns1:ref=\"2\"/></PolyListOfIKindShape>");
            var kindRead = XmlSerializer.Deserialize<PolyList<IKindShape>>(kindXml, Preserve)!;
            kindRead.Items.Should().HaveCount(2);
            kindRead.Items[0].Should().BeOfType<KindCircle>();
            ReferenceEquals(kindRead.Items[0], kindRead.Items[1]).Should().BeTrue();

            var typedXml = XmlSerializer.Serialize(new PolyItem<TypedShape> { Item = new TypedSquare { Label = "b", Side = 3 } }, Preserve);
            typedXml.Should().Be("<PolyItemOfTypedShape " + Refs + " ns1:id=\"1\"><Item xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"square\" ns1:id=\"2\"><Label>b</Label><Side>3</Side></Item></PolyItemOfTypedShape>");
            XmlSerializer.Deserialize<PolyItem<TypedShape>>(typedXml, Preserve)!.Item.Should().BeOfType<TypedSquare>();
        }

        [TestMethod]
        public void Preserve_RepeatedItem_IsWrittenOnceAndReadAsOneObject()
        {
            var a = new RefLeaf { Name = "a" };
            var xml = XmlSerializer.Serialize(new RefList { Items = [a, a, a] }, Preserve);
            xml.Should().Be("<RefList " + Refs + " ns1:id=\"1\"><Items ns1:id=\"2\"><Name>a</Name></Items><Items ns1:ref=\"2\"/><Items ns1:ref=\"2\"/></RefList>");
            var read = XmlSerializer.Deserialize<RefList>(xml, Preserve)!;
            read.Items.Should().HaveCount(3);
            read.Items[0]!.Name.Should().Be("a");
            ReferenceEquals(read.Items[0], read.Items[1]).Should().BeTrue();
            ReferenceEquals(read.Items[0], read.Items[2]).Should().BeTrue();
        }

        [TestMethod]
        public void Preserve_RepeatedMember_IsWrittenOnceAndReadAsOneObject()
        {
            var a = new RefLeaf { Name = "a" };
            var xml = XmlSerializer.Serialize(new RefPair { First = a, Second = a }, Preserve);
            xml.Should().Be("<RefPair " + Refs + " ns1:id=\"1\"><First ns1:id=\"2\"><Name>a</Name></First><Second ns1:ref=\"2\"/></RefPair>");
            var read = XmlSerializer.Deserialize<RefPair>(xml, Preserve)!;
            read.First!.Name.Should().Be("a");
            ReferenceEquals(read.First, read.Second).Should().BeTrue();
        }

        [TestMethod]
        public void Preserve_RepeatedWrappedItem_IsWrittenOnceAndReadAsOneObject()
        {
            var a = new RefLeaf { Name = "a" };
            var xml = XmlSerializer.Serialize(new RefWrapped { Items = [a, a] }, Preserve);
            xml.Should().Be("<RefWrapped " + Refs + " ns1:id=\"1\"><Items><RefLeaf ns1:id=\"2\"><Name>a</Name></RefLeaf><RefLeaf ns1:ref=\"2\"/></Items></RefWrapped>");
            var read = XmlSerializer.Deserialize<RefWrapped>(xml, Preserve)!;
            read.Items.Should().HaveCount(2);
            read.Items[0]!.Name.Should().Be("a");
            ReferenceEquals(read.Items[0], read.Items[1]).Should().BeTrue();
        }

        [TestMethod]
        public void Preserve_SelfAmongItems_IsWrittenAsARef()
        {
            var a = new RefLeaf { Name = "a" };
            a.Children.Add(a);
            var xml = XmlSerializer.Serialize(a, Preserve);
            xml.Should().Be("<RefLeaf " + Refs + " ns1:id=\"1\"><Children ns1:ref=\"1\"/><Name>a</Name></RefLeaf>");
            var read = XmlSerializer.Deserialize<RefLeaf>(xml, Preserve)!;
            read.Name.Should().Be("a");
            read.Children.Should().HaveCount(1);
            ReferenceEquals(read.Children[0], read).Should().BeTrue();
        }

        [TestMethod]
        public void Preserve_TwoEqualObjects_AreWrittenTwice()
        {
            // Sharing is by identity, not by value.
            var xml = XmlSerializer.Serialize(new RefPair { First = new RefLeaf { Name = "a" }, Second = new RefLeaf { Name = "a" } }, Preserve);
            xml.Should().Be("<RefPair " + Refs + " ns1:id=\"1\"><First ns1:id=\"2\"><Name>a</Name></First><Second ns1:id=\"3\"><Name>a</Name></Second></RefPair>");
            var read = XmlSerializer.Deserialize<RefPair>(xml, Preserve)!;
            ReferenceEquals(read.First, read.Second).Should().BeFalse();
        }

        #endregion

    }

}
