using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Values that are XML already (<see cref="XmlElement"/>, <see cref="XmlDocument"/>, <see cref="XmlNode"/>),
    /// values declared as <see cref="object"/>, and anonymous types.
    /// </summary>
    [TestClass]
    public class XmlSerializerXmlValueTests
    {

        #region Fields

        private const string Foo = "<foo a=\"1\"><bar/>text</foo>";
        private const string Nil = "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"";
        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull };

        #endregion

        #region Public Methods

        [TestMethod]
        public void AnonymousType_IsWrittenUnderAFixedName()
        {
            XmlSerializer.Serialize(new { Name = "Ada", Age = 3 }, Bare).Should().Be("<AnonymousType><Name>Ada</Name><Age>3</Age></AnonymousType>");
            XmlSerializer.Serialize(new { Inner = new { A = 1 } }, Bare).Should().Be("<AnonymousType><Inner><A>1</A></Inner></AnonymousType>");
            XmlSerializer.Serialize(new[] { new { A = 1 }, new { A = 2 } }, Bare)
                .Should().Be("<ArrayOfAnonymousType><AnonymousType><A>1</A></AnonymousType><AnonymousType><A>2</A></AnonymousType></ArrayOfAnonymousType>");
            XmlSerializer.Serialize(new List<object> { new { A = 1 } }, Bare).Should().Be("<ArrayOfAnyType><AnonymousType><A>1</A></AnonymousType></ArrayOfAnyType>");
        }

        [TestMethod]
        public void AnonymousType_IsReadThroughItsConstructor()
        {
            var prototype = new { Name = "", Age = 0 };
            ReadAs(prototype, "<AnonymousType><Name>Ada</Name><Age>3</Age></AnonymousType>").Should().Be(new { Name = "Ada", Age = 3 });
            ReadAs(prototype, "<anonymousType><age>4</age><NAME>Bo</NAME></anonymousType>", new XmlSerializerOptions { PropertyNameCaseInsensitive = true })
                .Should().Be(new { Name = "Bo", Age = 4 });
            FluentActions.Invoking(() => ReadAs(prototype, "<AnonymousType><Age>4</Age></AnonymousType>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The required constructor parameter 'Name' is missing. Path: /AnonymousType | LineNumber: 1 | BytePositionInLine: 44.");

            static T ReadAs<T>(T prototype, string xml, XmlSerializerOptions? options = null)
            {
                return XmlSerializer.Deserialize<T>(xml, options)!;
            }
        }

        [TestMethod]
        public void Object_AsTheRoot_IsWrittenAsTheTypeTheValueHas()
        {
            XmlSerializer.Serialize<object>(new Person { Name = "Ada", Age = 3 }, Bare).Should().Be("<Person><Name>Ada</Name><Age>3</Age></Person>");
            XmlSerializer.Serialize<object>(5, Bare).Should().Be("<int>5</int>");
            XmlSerializer.Serialize<object>("s", Bare).Should().Be("<string>s</string>");
            XmlSerializer.Serialize<object>(new { A = 1 }, Bare).Should().Be("<AnonymousType><A>1</A></AnonymousType>");
            XmlSerializer.Serialize<object>(new List<int> { 1 }, Bare).Should().Be("<ArrayOfInt><int>1</int></ArrayOfInt>");
            XmlSerializer.Serialize<object>(new Book { Title = "t", Author = "a" }, Bare).Should().Be("<Book><Title>t</Title><Author>a</Author></Book>");
            XmlSerializer.Serialize(new Person { Name = "Ada", Age = 3 }, typeof(object), Bare).Should().Be("<Person><Name>Ada</Name><Age>3</Age></Person>");
            XmlSerializer.Serialize<object>(new object(), Bare).Should().Be("<anyType/>");
            XmlSerializer.Serialize<object?>(null, new XmlSerializerOptions { OmitXmlDeclaration = true }).Should().Be("<anyType " + Nil + "/>");
            Encoding.UTF8.GetString(XmlSerializer.SerializeToUtf8Bytes<object>(5, Bare)).Should().Be("<int>5</int>");
        }

        [TestMethod]
        public void Object_AsAMember_IsWrittenAsTheTypeTheValueHas()
        {
            var value = new ObjectHolder
            {
                Any = new Person { Name = "Ada", Age = 3 },
                Items = [1, "a", new object()],
                Map = new Dictionary<string, object?> { ["n"] = 1.5, ["p"] = new Person { Name = "Bo", Age = 4 } },
            };
            XmlSerializer.Serialize(value, Bare).Should().Be(
                "<ObjectHolder><Any><Name>Ada</Name><Age>3</Age></Any><Items>1</Items><Items>a</Items><Items/>" +
                "<Map><n>1.5</n><p><Name>Bo</Name><Age>4</Age></p></Map></ObjectHolder>");
            XmlSerializer.Serialize(new ObjectHolder { Any = 5 }, Bare).Should().Be("<ObjectHolder><Any>5</Any></ObjectHolder>");
            XmlSerializer.Serialize(new ObjectHolder { Any = new object() }, Bare).Should().Be("<ObjectHolder><Any/></ObjectHolder>");
            XmlSerializer.Serialize(new ObjectHolder(), Bare).Should().Be("<ObjectHolder/>");
        }

        [TestMethod]
        public void Object_IsReadAsTheElementItWasWrittenAs()
        {
            // The document does not say which type wrote an element, so a value declared as object reads
            // back as the element: the XmlElement that System.Text.Json's JsonElement corresponds to.
            var root = XmlSerializer.Deserialize<object>("<Person><Name>Ada</Name><Age>3</Age></Person>");
            var element = root.Should().BeOfType<XmlElement>().Subject;
            Write(element).Should().Be("<Person><Name>Ada</Name><Age>3</Age></Person>");
            element.Document!.IsPooled.Should().BeFalse();
            XmlSerializer.Deserialize<Person>(element).Should().BeEquivalentTo(new Person { Name = "Ada", Age = 3 });

            XmlSerializer.Deserialize<object>("<anyType " + Nil + "/>").Should().BeNull();
            XmlSerializer.Deserialize("<int>5</int>", typeof(object)).Should().BeOfType<XmlElement>().Which.GetInt32().Should().Be(5);

            var holder = XmlSerializer.Deserialize<ObjectHolder>(
                "<ObjectHolder><Any>5</Any><Items>1</Items><Items><Name>Ada</Name></Items><Map><n>1.5</n></Map></ObjectHolder>")!;
            Write(holder.Any.Should().BeOfType<XmlElement>().Subject).Should().Be("<Any>5</Any>");
            holder.Items.Should().HaveCount(2);
            Write((XmlElement)holder.Items![0]).Should().Be("<Items>1</Items>");
            Write((XmlElement)holder.Items[1]).Should().Be("<Items><Name>Ada</Name></Items>");
            Write((XmlElement)holder.Map!["n"]!).Should().Be("<n>1.5</n>");
        }

        [TestMethod]
        public void XmlDocument_IsWrittenAsItsDocumentElement()
        {
            using var document = XmlDocument.Parse("<?xml version=\"1.0\" encoding=\"utf-8\"?><!--before-->" + Foo + "<!--after-->");
            XmlSerializer.Serialize(document, Bare).Should().Be(Foo);
            XmlSerializer.Serialize(document, new XmlSerializerOptions()).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Foo);
            XmlSerializer.Serialize(new XmlValueHolder { Document = document }, Bare).Should().Be("<XmlValueHolder><Element/><Document>" + Foo + "</Document></XmlValueHolder>");

            var read = XmlSerializer.Deserialize<XmlDocument>(Foo)!;
            read.IsPooled.Should().BeFalse("a document the serializer hands out holds no rented memory, so it is safe to drop");
            Write(read.RootElement).Should().Be(Foo);
            read.Dispose();
        }

        [TestMethod]
        public void XmlElement_AsTheRoot_IsTheDocument()
        {
            using var document = XmlDocument.Parse(Foo);
            XmlSerializer.Serialize(document.RootElement, Bare).Should().Be(Foo);
            var read = XmlSerializer.Deserialize<XmlElement>(Foo);
            Write(read).Should().Be(Foo);
            read.Document!.IsPooled.Should().BeFalse();
            FluentActions.Invoking(() => XmlSerializer.Serialize(default(XmlElement), Bare))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be("The XML element is empty.");
        }

        [TestMethod]
        public void XmlElement_BelowTheRoot_KeepsTheNamespacesItUses()
        {
            using var document = XmlDocument.Parse("<r xmlns:p=\"urn:p\" xmlns=\"urn:d\"><p:a><b p:c=\"1\"/></p:a></r>");
            XmlElement inner = default;
            foreach (var child in document.RootElement.EnumerateChildren())
            {
                inner = child;
            }

            var xml = XmlSerializer.Serialize(inner, Bare);
            xml.Should().Be("<p:a xmlns:p=\"urn:p\"><b xmlns=\"urn:d\" p:c=\"1\"/></p:a>");

            var holder = XmlSerializer.Deserialize<XmlValueHolder>(
                "<XmlValueHolder xmlns:p=\"urn:p\"><Element><p:a><p:b/></p:a></Element></XmlValueHolder>")!;
            XmlSerializer.Serialize(holder.Element, Bare).Should().Be("<p:a xmlns:p=\"urn:p\"><p:b/></p:a>");
        }

        [TestMethod]
        public void XmlNode_AsTheRoot_IsTheDocument()
        {
            var node = XmlNode.Parse(Foo);
            XmlSerializer.Serialize(node, Bare).Should().Be(Foo);
            XmlSerializer.Serialize((XmlElementNode)node, Bare).Should().Be(Foo);
            XmlSerializer.Deserialize<XmlNode>(Foo).Should().BeOfType<XmlElementNode>().Which.ToString().Should().Be(Foo);
            XmlSerializer.Deserialize<XmlElementNode>(Foo)!.ToString().Should().Be(Foo);

            // A null value is a document element marked nil, and that reads back as null. An XmlElement is a
            // struct, so the same document is read as the element it is.
            var nil = XmlSerializer.Serialize<XmlNode?>(null, new XmlSerializerOptions { OmitXmlDeclaration = true });
            nil.Should().Be("<XmlNode " + Nil + "/>");
            XmlSerializer.Deserialize<XmlNode>(nil).Should().BeNull();
            XmlSerializer.Deserialize<XmlDocument>(nil).Should().BeNull();
            Write(XmlSerializer.Deserialize<XmlElement>(nil)).Should().Be("<XmlNode " + Nil + "/>");
            XmlSerializer.Deserialize<XmlNode>("<a b=\"1\"/>")!.ToString().Should().Be("<a b=\"1\"/>");

            FluentActions.Invoking(() => XmlSerializer.Serialize<XmlNode>(new XmlTextNode("t"), Bare))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Only an element can be serialized as an XML value. The node is a 'CloudNimble.Text.Xml.Nodes.XmlTextNode'.");
        }

        [TestMethod]
        public void XmlValues_AsMembers_AreWrittenInsideTheMemberElement()
        {
            using var document = XmlDocument.Parse(Foo);
            var value = new XmlValueHolder
            {
                Element = document.RootElement,
                Document = document,
                Node = XmlNode.Parse("<n/>"),
                ElementNode = (XmlElementNode)XmlNode.Parse("<e>1</e>"),
            };
            var xml = XmlSerializer.Serialize(value, Bare);
            xml.Should().Be(
                "<XmlValueHolder><Element>" + Foo + "</Element><Document>" + Foo + "</Document><Node><n/></Node><ElementNode><e>1</e></ElementNode></XmlValueHolder>");

            var read = XmlSerializer.Deserialize<XmlValueHolder>(xml)!;
            Write(read.Element).Should().Be(Foo);
            Write(read.Document!.RootElement).Should().Be(Foo);
            read.Document.IsPooled.Should().BeFalse();
            read.Node!.ToString().Should().Be("<n/>");
            read.ElementNode!.ToString().Should().Be("<e>1</e>");
            XmlSerializer.Serialize(read, Bare).Should().Be(xml);
        }

        [TestMethod]
        public void XmlValues_AsMembers_ThatAreAbsentOrEmpty()
        {
            XmlSerializer.Serialize(new XmlValueHolder(), Bare).Should().Be("<XmlValueHolder><Element/></XmlValueHolder>");
            var read = XmlSerializer.Deserialize<XmlValueHolder>("<XmlValueHolder><Element/><Document></Document><Node> </Node></XmlValueHolder>")!;
            read.Element.ValueKind.Should().Be(XmlValueKind.Undefined);
            read.Document.Should().BeNull();
            read.Node.Should().BeNull();
            read.ElementNode.Should().BeNull();
            FluentActions.Invoking(() => XmlSerializer.Deserialize<XmlValueHolder>("<XmlValueHolder><Node><a/><b/></Node></XmlValueHolder>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The element holds more than one element where one XML value is expected. Path: /XmlValueHolder/Node | LineNumber: 1 | BytePositionInLine: 31.");
            FluentActions.Invoking(() => XmlSerializer.Serialize(new XmlValueHolder { Node = new XmlTextNode("t") }, Bare))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Only an element can be serialized as an XML value. The node is a 'CloudNimble.Text.Xml.Nodes.XmlTextNode'.");
        }

        #endregion

        #region Private Methods

        private static string Write(XmlElement element)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true }))
            {
                element.WriteTo(writer);
            }

            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        #endregion

    }

}
