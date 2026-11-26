using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    /// POCO element mapping: public properties, naming policy, nested objects, records, init, required, and constructors.
    /// </summary>
    [TestClass]
    public class XmlSerializerObjectTests
    {

        #region Fields

        private static readonly XmlSerializerOptions Bare = new() { TypeInfoResolver = DefaultXmlTypeInfoResolver.Instance, OmitXmlDeclaration = true };
        private static readonly XmlSerializerOptions AttributeBare = new()
        {
            DefaultPropertyMapping = XmlPropertyMapping.Attribute,
            OmitXmlDeclaration = true,
            TypeInfoResolver = DefaultXmlTypeInfoResolver.Instance,
        };

        #endregion

        #region Public Methods

        [TestMethod]
        public void ReadAhead_DecodedParentAttribute_SurvivesAChildSkip()
        {
            var value = new ReadAheadItem { A = "x&y", C = new ReadAheadChild { B = "1&2" } };
            var xml = XmlSerializer.Serialize(value, Bare);
            xml.Should().Be("<ReadAheadItem A=\"x&amp;y\"><C B=\"1&amp;2\"/></ReadAheadItem>");
            var read = XmlSerializer.Deserialize<ReadAheadItem>(xml, Bare)!;
            read.A.Should().Be("x&y");
            read.C!.B.Should().Be("1&2");
            XmlSerializer.Serialize(read, Bare).Should().Be(xml);
        }

        [TestMethod]
        public void ReadAhead_NineDecodedAttributesOnParentAndChild_KeepTheirValues()
        {
            var value = new ReadAheadItem
            {
                A = "x&y",
                A1 = "1",
                A2 = "2",
                A3 = "3",
                A4 = "4",
                A5 = "5",
                A6 = "6",
                A7 = "7",
                A8 = "8",
                C = new ReadAheadChild
                {
                    B = "1&2",
                    B1 = "1",
                    B2 = "2",
                    B3 = "3",
                    B4 = "4",
                    B5 = "5",
                    B6 = "6",
                    B7 = "7",
                    B8 = "8",
                },
            };
            var xml = XmlSerializer.Serialize(value, Bare);
            xml.Should().Be("<ReadAheadItem A=\"x&amp;y\" A1=\"1\" A2=\"2\" A3=\"3\" A4=\"4\" A5=\"5\" A6=\"6\" A7=\"7\" A8=\"8\"><C B=\"1&amp;2\" B1=\"1\" B2=\"2\" B3=\"3\" B4=\"4\" B5=\"5\" B6=\"6\" B7=\"7\" B8=\"8\"/></ReadAheadItem>");
            var read = XmlSerializer.Deserialize<ReadAheadItem>(xml, Bare)!;
            read.A.Should().Be("x&y");
            read.C!.B.Should().Be("1&2");
            XmlSerializer.Serialize(read, Bare).Should().Be(xml);
        }

        [TestMethod]
        public void Person_RoundTripsAsChildElements()
        {
            var person = new Person { Name = "Ada", Age = 36 };
            XmlSerializer.Serialize(person, Bare).Should().Be("<Person><Name>Ada</Name><Age>36</Age></Person>");
            var read = XmlSerializer.Deserialize<Person>("<Person><Name>Ada</Name><Age>36</Age></Person>")!;
            read.Name.Should().Be("Ada");
            read.Age.Should().Be(36);
        }

        [TestMethod]
        public void Person_WebCamelCase()
        {
            var person = new Person { Name = "Ada", Age = 36 };
            XmlSerializer.Serialize(person, XmlSerializerOptions.Web).Should().Be("<person><name>Ada</name><age>36</age></person>");
            var read = XmlSerializer.Deserialize<Person>("<person><name>Ada</name><age>36</age></person>", XmlSerializerOptions.Web)!;
            read.Name.Should().Be("Ada");
            read.Age.Should().Be(36);
        }

        [TestMethod]
        public void Person_NullName_WritesXsiNil()
        {
            XmlSerializer.Serialize(new Person { Age = 1 }, Bare).Should().Be(
                "<Person><Name xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><Age>1</Age></Person>");
        }

        [TestMethod]
        public void Person_WhenWritingNull_OmitsNullName()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull };
            XmlSerializer.Serialize(new Person { Age = 1 }, options).Should().Be("<Person><Age>1</Age></Person>");
        }

        [TestMethod]
        public void NestedAddress_RoundTrips()
        {
            var employee = new Employee
            {
                Name = "Ada",
                Home = new Address { Street = "1 Main", City = "Paris" },
            };
            XmlSerializer.Serialize(employee, Bare).Should().Be(
                "<Employee><Name>Ada</Name><Home><Street>1 Main</Street><City>Paris</City></Home></Employee>");
            var read = XmlSerializer.Deserialize<Employee>(
                "<Employee><Name>Ada</Name><Home><Street>1 Main</Street><City>Paris</City></Home></Employee>")!;
            read.Name.Should().Be("Ada");
            read.Home!.Street.Should().Be("1 Main");
            read.Home.City.Should().Be("Paris");
        }

        [TestMethod]
        public void NestedAddress_Null_WritesXsiNil()
        {
            XmlSerializer.Serialize(new Employee { Name = "Ada" }, Bare).Should().Be(
                "<Employee><Name>Ada</Name><Home xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></Employee>");
        }

        [TestMethod]
        public void PersonRecord_UsesPrimaryConstructor()
        {
            var record = new PersonRecord("Ada", 36);
            XmlSerializer.Serialize(record, Bare).Should().Be("<PersonRecord><Name>Ada</Name><Age>36</Age></PersonRecord>");
            var read = XmlSerializer.Deserialize<PersonRecord>("<PersonRecord><Name>Ada</Name><Age>36</Age></PersonRecord>")!;
            read.Should().Be(new PersonRecord("Ada", 36));
        }

        [TestMethod]
        public void InitWidget_SetsInitProperties()
        {
            XmlSerializer.Serialize(new InitWidget { Id = 7, Label = "x" }, Bare).Should().Be("<InitWidget><Id>7</Id><Label>x</Label></InitWidget>");
            var read = XmlSerializer.Deserialize<InitWidget>("<InitWidget><Id>7</Id><Label>x</Label></InitWidget>")!;
            read.Id.Should().Be(7);
            read.Label.Should().Be("x");
        }

        [TestMethod]
        public void RequiredCatalog_MissingTitle_Throws()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<RequiredCatalog>("<RequiredCatalog/>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The required member 'Title' is missing. Path: /RequiredCatalog | LineNumber: 1 | BytePositionInLine: 19.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<RequiredCatalog>(
                    "<RequiredCatalog><Title xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></RequiredCatalog>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The required member 'Title' cannot be nil. Path: /RequiredCatalog/Title | LineNumber: 1 | BytePositionInLine: 95.");
        }

        [TestMethod]
        public void NamedPerson_UsesPropertyName()
        {
            XmlSerializer.Serialize(new NamedPerson { Name = "Ada" }, Bare).Should().Be("<NamedPerson><FullName>Ada</FullName></NamedPerson>");
            XmlSerializer.Deserialize<NamedPerson>("<NamedPerson><FullName>Ada</FullName></NamedPerson>")!.Name.Should().Be("Ada");
        }

        [TestMethod]
        public void IgnoredPerson_OmitsSecret()
        {
            XmlSerializer.Serialize(new IgnoredPerson { Name = "Ada", Secret = "x" }, Bare).Should().Be("<IgnoredPerson><Name>Ada</Name></IgnoredPerson>");
            var read = XmlSerializer.Deserialize<IgnoredPerson>("<IgnoredPerson><Name>Ada</Name><Secret>x</Secret></IgnoredPerson>")!;
            read.Name.Should().Be("Ada");
            read.Secret.Should().BeNull();
        }

        [TestMethod]
        public void RootedPerson_UsesXmlRoot()
        {
            XmlSerializer.Serialize(new RootedPerson { Name = "Ada" }, Bare).Should().Be(
                "<Human xmlns=\"urn:people\"><Name>Ada</Name></Human>");
            XmlSerializer.Deserialize<RootedPerson>("<Human xmlns=\"urn:people\"><Name>Ada</Name></Human>")!.Name.Should().Be("Ada");
        }

        [TestMethod]
        public void CtorBox_UsesMarkedConstructor()
        {
            XmlSerializer.Serialize(new CtorBox("hi"), Bare).Should().Be("<CtorBox><Value>hi</Value></CtorBox>");
            XmlSerializer.Deserialize<CtorBox>("<CtorBox><Value>hi</Value></CtorBox>")!.Value.Should().Be("hi");
        }

        [TestMethod]
        public void MappedDerived_WritesBaseThenDerived()
        {
            XmlSerializer.Serialize(new MappedDerived { A = "a", B = "b" }, Bare).Should().Be(
                "<MappedDerived><A>a</A><B>b</B></MappedDerived>");
        }

        [TestMethod]
        public void OrderedDerived_InterleavesBaseAndDerived()
        {
            XmlSerializer.Serialize(new OrderedDerived { A = "a", B = "b", C = "c" }, Bare).Should().Be(
                "<OrderedDerived><A>a</A><B>b</B><C>c</C></OrderedDerived>");
        }

        [TestMethod]
        public void FieldBox_IncludeFields()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, IncludeFields = true };
            XmlSerializer.Serialize(new FieldBox { Value = "x" }, options).Should().Be("<FieldBox><Value>x</Value></FieldBox>");
            XmlSerializer.Deserialize<FieldBox>("<FieldBox><Value>x</Value></FieldBox>", options)!.Value.Should().Be("x");
        }

        [TestMethod]
        public void PrivateHolder_IncludesNonPublicMember()
        {
            XmlSerializer.Serialize(new PrivateHolder { Name = "Ada", Hidden = "h" }, Bare).Should().Be(
                "<PrivateHolder><Name>Ada</Name><Hidden>h</Hidden></PrivateHolder>");
            var read = XmlSerializer.Deserialize<PrivateHolder>("<PrivateHolder><Name>Ada</Name><Hidden>h</Hidden></PrivateHolder>")!;
            read.Name.Should().Be("Ada");
            read.Hidden.Should().Be("h");
        }

        [TestMethod]
        public void UnmappedMember_SkipByDefault_DisallowThrows()
        {
            XmlSerializer.Deserialize<Person>("<Person><Name>Ada</Name><Extra>x</Extra><Age>1</Age></Person>")!.Name.Should().Be("Ada");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Person>("<Person><Name>Ada</Name><Extra>x</Extra></Person>", XmlSerializerOptions.Strict))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Unknown element 'Extra'. Path: /Person | LineNumber: 1 | BytePositionInLine: 32.");
        }

        [TestMethod]
        public void EmptyPerson_DeserializesDefaults()
        {
            var read = XmlSerializer.Deserialize<Person>("<Person/>")!;
            read.Name.Should().BeNull();
            read.Age.Should().Be(0);
        }

        [TestMethod]
        public void AttributeMapped_WritesAnAttributeOnTheDefaultElementMapping()
        {
            XmlSerializer.Serialize(new AttributeMapped { Id = 1 }, Bare).Should().Be("<AttributeMapped Id=\"1\"/>");
            XmlSerializer.Deserialize<AttributeMapped>("<AttributeMapped Id=\"1\"/>")!.Id.Should().Be(1);
        }

        [TestMethod]
        public void TextMapped_WritesSimpleContent()
        {
            XmlSerializer.Serialize(new TextMapped { Value = "x" }, Bare).Should().Be("<TextMapped>x</TextMapped>");
            XmlSerializer.Deserialize<TextMapped>("<TextMapped>x</TextMapped>")!.Value.Should().Be("x");
            XmlSerializer.Deserialize<TextMapped>("<TextMapped/>")!.Value.Should().BeNull();
        }

        [TestMethod]
        public void Price_WritesAttributeThenSimpleContent()
        {
            XmlSerializer.Serialize(new Price { Currency = "USD", Amount = 19.99m }, Bare).Should().Be("<Price Currency=\"USD\">19.99</Price>");
            var read = XmlSerializer.Deserialize<Price>("<Price Currency=\"USD\">19.99</Price>")!;
            read.Currency.Should().Be("USD");
            read.Amount.Should().Be(19.99m);
        }

        [TestMethod]
        public void CDataMapped_WritesACdataSection()
        {
            XmlSerializer.Serialize(new CDataMapped { Value = "a<b" }, Bare).Should().Be("<CDataMapped><![CDATA[a<b]]></CDataMapped>");
            XmlSerializer.Deserialize<CDataMapped>("<CDataMapped><![CDATA[a<b]]></CDataMapped>")!.Value.Should().Be("a<b");
            XmlSerializer.Deserialize<CDataMapped>("<CDataMapped>plain</CDataMapped>")!.Value.Should().Be("plain");
        }

        [TestMethod]
        public void TwoText_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<TwoText>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "The type 'CloudNimble.XmlEssentials.Tests.POCOs.TwoText' has more than one [XmlTextAttribute] or [XmlCDataAttribute].");
        }

        [TestMethod]
        public void Paragraph_WritesMixedText()
        {
            var paragraph = new Paragraph { Content = [new XmlMixedText { Text = "hello" }] };
            XmlSerializer.Serialize(paragraph, Bare).Should().Be("<Paragraph>hello</Paragraph>");
            var read = XmlSerializer.Deserialize<Paragraph>("<Paragraph>hello</Paragraph>")!;
            read.Content.Should().HaveCount(1);
            read.Content[0].Should().BeOfType<XmlMixedText>().Which.Text.Should().Be("hello");
        }

        [TestMethod]
        public void Paragraph_RoundTripsMixedElements()
        {
            var paragraph = new Paragraph
            {
                Content =
                [
                    new XmlMixedText("Hello "),
                    new XmlMixedElement { Element = new XmlElementNode("em") { InnerText = "world" } },
                    new XmlMixedText("!"),
                ],
            };
            var xml = XmlSerializer.Serialize(paragraph, Bare);
            xml.Should().Be("<Paragraph>Hello <em>world</em>!</Paragraph>");
            var read = XmlSerializer.Deserialize<Paragraph>(xml)!;
            read.Content.Should().HaveCount(3);
            read.Content[0].Should().BeOfType<XmlMixedText>().Which.Text.Should().Be("Hello ");
            read.Content[1].Should().BeOfType<XmlMixedElement>().Which.Element!.LocalName.Should().Be("em");
            ((XmlMixedElement)read.Content[1]).Element!.InnerText.Should().Be("world");
            read.Content[2].Should().BeOfType<XmlMixedText>().Which.Text.Should().Be("!");
        }

        [TestMethod]
        public void MixedPlusChild_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<MixedPlusChild>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "The type 'CloudNimble.XmlEssentials.Tests.POCOs.MixedPlusChild' maps [XmlMixedAttribute] with other element or text members.");
        }

        [TestMethod]
        public void Node_IgnoreCycles_WritesNilForTheBackReference()
        {
            var node = new Node { Name = "root" };
            node.Child = node;
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, ReferenceHandler = XmlReferenceHandler.IgnoreCycles };
            XmlSerializer.Serialize(node, options).Should().Be(
                "<Node><Child xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><Name>root</Name></Node>");
        }

        [TestMethod]
        public void Shelf_WritesDerivedElementNames()
        {
            var shelf = new Shelf { Item = new Book { Title = "Clean Code", Author = "Robert C. Martin" } };
            XmlSerializer.Serialize(shelf, Bare).Should().Be("<Shelf><Book><Title>Clean Code</Title><Author>Robert C. Martin</Author></Book></Shelf>");
            var read = XmlSerializer.Deserialize<Shelf>("<Shelf><Book><Title>Clean Code</Title><Author>Robert C. Martin</Author></Book></Shelf>")!;
            read.Item.Should().BeOfType<Book>();
            ((Book)read.Item!).Author.Should().Be("Robert C. Martin");
        }

        [TestMethod]
        public void Catalog_WritesUnwrappedItems()
        {
            var catalog = new Catalog { Title = "Ada", Items = ["a", "b"] };
            XmlSerializer.Serialize(catalog, Bare).Should().Be("<Catalog><Items>a</Items><Items>b</Items><Title>Ada</Title></Catalog>");
            var read = XmlSerializer.Deserialize<Catalog>("<Catalog><Title>Ada</Title><Items>a</Items><Items>b</Items></Catalog>")!;
            read.Title.Should().Be("Ada");
            read.Items.Should().Equal("a", "b");
            XmlSerializer.Serialize(new Catalog { Title = "Ada", Items = [] }, Bare).Should().Be("<Catalog><Title>Ada</Title></Catalog>");
        }

        [TestMethod]
        public void WrappedCatalog_WritesAWrapper()
        {
            var catalog = new WrappedCatalog { Title = "Ada", Items = ["a", "b"] };
            XmlSerializer.Serialize(catalog, Bare).Should().Be("<WrappedCatalog><Title>Ada</Title><Items><Item>a</Item><Item>b</Item></Items></WrappedCatalog>");
            var read = XmlSerializer.Deserialize<WrappedCatalog>("<WrappedCatalog><Title>Ada</Title><Items><Item>a</Item><Item>b</Item></Items></WrappedCatalog>")!;
            read.Items.Should().Equal("a", "b");
            XmlSerializer.Serialize(new WrappedCatalog { Title = "Ada", Items = [] }, Bare).Should().Be("<WrappedCatalog><Title>Ada</Title><Items/></WrappedCatalog>");
        }

        [TestMethod]
        public void ListOfInt32_WritesItemTypeNames()
        {
            XmlSerializer.Serialize(new List<int> { 1, 2 }, Bare).Should().Be("<ArrayOfInt><int>1</int><int>2</int></ArrayOfInt>");
            XmlSerializer.Deserialize<List<int>>("<ArrayOfInt><int>1</int><int>2</int></ArrayOfInt>").Should().Equal(1, 2);
            XmlSerializer.Deserialize<int[]>("<ArrayOfInt><int>3</int></ArrayOfInt>").Should().Equal(3);
        }

        [TestMethod]
        public void Scorecard_WritesDictionaryKeysAsElementNames()
        {
            var card = new Scorecard { Scores = new() { ["alice"] = 10, ["bob"] = 20 } };
            var xml = XmlSerializer.Serialize(card, Bare);
            xml.Should().Be("<Scorecard><Scores><alice>10</alice><bob>20</bob></Scores></Scorecard>");
            var read = XmlSerializer.Deserialize<Scorecard>(xml)!;
            read.Scores.Should().ContainKey("alice").WhoseValue.Should().Be(10);
            read.Scores.Should().ContainKey("bob").WhoseValue.Should().Be(20);
        }

        [TestMethod]
        public void Scorecard_WritesKeyValuePairsWhenTheKeyIsNotAName()
        {
            var card = new Scorecard { Scores = new() { ["not a name"] = 10 } };
            var xml = XmlSerializer.Serialize(card, Bare);
            xml.Should().Be("<Scorecard><Scores><Key>not a name</Key><Value>10</Value></Scores></Scorecard>");
            var round = XmlSerializer.Deserialize<Scorecard>(xml)!;
            round.Scores.Should().ContainKey("not a name").WhoseValue.Should().Be(10);
        }

        [TestMethod]
        public void BareArray_UsesTheMemberNameAsTheWrapper()
        {
            XmlSerializer.Serialize(new BareArray { Values = [1, 2] }, Bare).Should().Be("<BareArray><Values><int>1</int><int>2</int></Values></BareArray>");
            XmlSerializer.Deserialize<BareArray>("<BareArray><Values><int>1</int><int>2</int></Values></BareArray>")!.Values.Should().Equal(1, 2);
        }

        [TestMethod]
        public void ExtraBag_RoundTripsTheKnownMember()
        {
            XmlSerializer.Serialize(new ExtraBag { Name = "Ada" }, Bare).Should().Be("<ExtraBag><Name>Ada</Name></ExtraBag>");
            XmlSerializer.Deserialize<ExtraBag>("<ExtraBag><Name>Ada</Name></ExtraBag>")!.Name.Should().Be("Ada");
        }

        [TestMethod]
        public void ExtraBag_CapturesUnmappedMembers()
        {
            var bag = new ExtraBag { Name = "Ada" };
            bag.Extra.Attributes[new XmlName("note")] = "x";
            bag.Extra.Elements.Add(new XmlElementNode("Nick") { InnerText = "Ada" });
            var xml = XmlSerializer.Serialize(bag, Bare);
            xml.Should().Be("<ExtraBag note=\"x\"><Name>Ada</Name><Nick>Ada</Nick></ExtraBag>");
            var read = XmlSerializer.Deserialize<ExtraBag>(xml)!;
            read.Name.Should().Be("Ada");
            read.Extra.Attributes.Should().HaveCount(1);
            read.Extra.Attributes[new XmlName("note")].Should().Be("x");
            read.Extra.Elements.Should().HaveCount(1);
            read.Extra.Elements[0].LocalName.Should().Be("Nick");
            read.Extra.Elements[0].InnerText.Should().Be("Ada");
        }

        [TestMethod]
        public void Person_ObjectOverload_RoundTrips()
        {
            var xml = XmlSerializer.Serialize((object)new Person { Name = "Ada", Age = 1 }, typeof(Person), Bare);
            xml.Should().Be("<Person><Name>Ada</Name><Age>1</Age></Person>");
            var read = (Person)XmlSerializer.Deserialize(xml, typeof(Person))!;
            read.Name.Should().Be("Ada");
        }

        [TestMethod]
        public void Node_Preserve_WritesIdAndRef()
        {
            var node = new Node { Name = "root" };
            node.Child = node;
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, ReferenceHandler = XmlReferenceHandler.Preserve };
            XmlSerializer.Serialize(node, options).Should().Be(
                "<Node xmlns:ns1=\"urn:CloudNimble.Text.Xml:refs\" ns1:id=\"1\"><Child ns1:ref=\"1\"/><Name>root</Name></Node>");
        }

        [TestMethod]
        public void Node_Preserve_RoundTripsTheCycle()
        {
            var node = new Node { Name = "root" };
            node.Child = node;
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, ReferenceHandler = XmlReferenceHandler.Preserve };
            var xml = XmlSerializer.Serialize(node, options);
            var read = XmlSerializer.Deserialize<Node>(xml, options)!;
            read.Name.Should().Be("root");
            object.ReferenceEquals(read, read.Child).Should().BeTrue();
        }

        [TestMethod]
        public void Product_WritesDerivedRootName()
        {
            var xml = XmlSerializer.Serialize<Product>(new Book { Title = "Clean Code", Author = "Robert C. Martin" }, Bare);
            xml.Should().Be("<Book><Title>Clean Code</Title><Author>Robert C. Martin</Author></Book>");
            var read = XmlSerializer.Deserialize<Product>(xml)!;
            read.Should().BeOfType<Book>();
            ((Book)read).Author.Should().Be("Robert C. Martin");
        }

        [TestMethod]
        public void ProductCatalog_WritesUnwrappedDerivedItems()
        {
            var catalog = new ProductCatalog
            {
                Items =
                [
                    new Book { Title = "Clean Code", Author = "Robert C. Martin" },
                    new Dvd { Title = "The Matrix", Runtime = 136 },
                ],
            };
            var xml = XmlSerializer.Serialize(catalog, Bare);
            xml.Should().Be("<ProductCatalog><Book><Title>Clean Code</Title><Author>Robert C. Martin</Author></Book><Dvd><Title>The Matrix</Title><Runtime>136</Runtime></Dvd></ProductCatalog>");
            var read = XmlSerializer.Deserialize<ProductCatalog>(xml)!;
            read.Items.Should().HaveCount(2);
            read.Items[0].Should().BeOfType<Book>().Which.Author.Should().Be("Robert C. Martin");
            read.Items[1].Should().BeOfType<Dvd>().Which.Runtime.Should().Be(136);
        }

        [TestMethod]
        public void KindShelf_WritesTheKindAttribute()
        {
            var shelf = new KindShelf { Item = new KindBook { Title = "Clean Code", Author = "Robert C. Martin" } };
            var xml = XmlSerializer.Serialize(shelf, Bare);
            xml.Should().Be("<KindShelf><Item kind=\"book\"><Title>Clean Code</Title><Author>Robert C. Martin</Author></Item></KindShelf>");
            var read = XmlSerializer.Deserialize<KindShelf>(xml)!;
            read.Item.Should().BeOfType<KindBook>();
            ((KindBook)read.Item!).Author.Should().Be("Robert C. Martin");
        }

        [TestMethod]
        public void KindTape_ThrowsOnWrite()
        {
            FluentActions.Invoking(() => XmlSerializer.Serialize(new KindShelf { Item = new KindTape { Title = "x" } }, Bare))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "The type 'CloudNimble.XmlEssentials.Tests.POCOs.KindTape' is not a declared derived type of 'CloudNimble.XmlEssentials.Tests.POCOs.KindProduct'.");
        }

        [TestMethod]
        public void XsiShelf_WritesXsiType()
        {
            var shelf = new XsiShelf { Item = new XsiBook { Title = "Clean Code", Author = "Robert C. Martin" } };
            var xml = XmlSerializer.Serialize(shelf, Bare);
            xml.Should().Be("<XsiShelf><Item xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"Book\"><Title>Clean Code</Title><Author>Robert C. Martin</Author></Item></XsiShelf>");
            var read = XmlSerializer.Deserialize<XsiShelf>(xml)!;
            read.Item.Should().BeOfType<XsiBook>();
            ((XsiBook)read.Item!).Author.Should().Be("Robert C. Martin");
        }

        [TestMethod]
        public void KindProduct_WritesTheKindAttributeOnTheRoot()
        {
            var xml = XmlSerializer.Serialize<KindProduct>(new KindBook { Title = "Clean Code", Author = "Robert C. Martin" }, Bare);
            xml.Should().Be("<KindProduct kind=\"book\"><Title>Clean Code</Title><Author>Robert C. Martin</Author></KindProduct>");
            var read = XmlSerializer.Deserialize<KindProduct>(xml)!;
            read.Should().BeOfType<KindBook>();
            ((KindBook)read).Author.Should().Be("Robert C. Martin");
        }

        [TestMethod]
        public void XsiProduct_WritesXsiTypeOnTheRoot()
        {
            var xml = XmlSerializer.Serialize<XsiProduct>(new XsiBook { Title = "Clean Code", Author = "Robert C. Martin" }, Bare);
            xml.Should().Be("<XsiProduct xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"Book\"><Title>Clean Code</Title><Author>Robert C. Martin</Author></XsiProduct>");
            var read = XmlSerializer.Deserialize<XsiProduct>(xml)!;
            read.Should().BeOfType<XsiBook>();
            ((XsiBook)read).Author.Should().Be("Robert C. Martin");
        }

        [TestMethod]
        public void KindCatalog_WritesKindOnEachItem()
        {
            var catalog = new KindCatalog
            {
                Items =
                [
                    new KindBook { Title = "Clean Code", Author = "Robert C. Martin" },
                    new KindDvd { Title = "The Matrix", Runtime = 136 },
                ],
            };
            var xml = XmlSerializer.Serialize(catalog, Bare);
            xml.Should().Be("<KindCatalog><Items kind=\"book\"><Title>Clean Code</Title><Author>Robert C. Martin</Author></Items><Items kind=\"dvd\"><Title>The Matrix</Title><Runtime>136</Runtime></Items></KindCatalog>");
            var read = XmlSerializer.Deserialize<KindCatalog>(xml)!;
            read.Items.Should().HaveCount(2);
            read.Items[0].Should().BeOfType<KindBook>().Which.Author.Should().Be("Robert C. Martin");
            read.Items[1].Should().BeOfType<KindDvd>().Which.Runtime.Should().Be(136);
        }

        [TestMethod]
        public void KindShelf_UnknownDiscriminator_Throws()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<KindShelf>("<KindShelf><Item kind=\"tape\"><Title>x</Title></Item></KindShelf>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Unknown type discriminator 'tape'. Path: /KindShelf/Item | LineNumber: 1 | BytePositionInLine: 30.");
        }

        [TestMethod]
        public void KindShelf_DerivedElementName_IsUnmapped()
        {
            XmlSerializer.Deserialize<KindShelf>("<KindShelf><Book><Title>x</Title></Book></KindShelf>")!.Item.Should().BeNull();
        }

        [TestMethod]
        public void KindTape_AsRoot_ThrowsOnWrite()
        {
            FluentActions.Invoking(() => XmlSerializer.Serialize<KindProduct>(new KindTape { Title = "x" }, Bare))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "The type 'CloudNimble.XmlEssentials.Tests.POCOs.KindTape' is not a declared derived type of 'CloudNimble.XmlEssentials.Tests.POCOs.KindProduct'.");
        }

        [TestMethod]
        public void FallbackShelf_WritesUnknownDerivedAsTheBaseType()
        {
            var xml = XmlSerializer.Serialize(new FallbackShelf { Item = new FallbackTape { Title = "x" } }, Bare);
            xml.Should().Be("<FallbackShelf><Item><Title>x</Title></Item></FallbackShelf>");
        }

        [TestMethod]
        public void NearShelf_WritesTheNearestDeclaredAncestor()
        {
            var xml = XmlSerializer.Serialize(new NearShelf { Item = new NearSpecialBook { Title = "x", Author = "Ada" } }, Bare);
            xml.Should().Be("<NearShelf><NearBook><Title>x</Title><Author>Ada</Author></NearBook></NearShelf>");
            var read = XmlSerializer.Deserialize<NearShelf>(xml)!;
            read.Item.Should().BeOfType<NearBook>();
            ((NearBook)read.Item!).Author.Should().Be("Ada");
        }

        [TestMethod]
        public void BadKindProduct_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<BadKindProduct>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "[XmlPolymorphicAttribute] on 'CloudNimble.XmlEssentials.Tests.POCOs.BadKindProduct' uses Attribute and requires TypeDiscriminatorPropertyName.");
        }

        [TestMethod]
        public void WrongExtra_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<WrongExtra>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Member 'CloudNimble.XmlEssentials.Tests.POCOs.WrongExtra.Extra' is marked [XmlExtensionDataAttribute] but is not XmlExtensionData.");
        }

        [TestMethod]
        public void TwoExtra_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<TwoExtra>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "The type 'CloudNimble.XmlEssentials.Tests.POCOs.TwoExtra' has more than one [XmlExtensionDataAttribute].");
        }

        [TestMethod]
        public void Mixed_OnAMemberThatIsNotACollectionOfMixedContentItems_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<MixedOfObjects>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Member 'CloudNimble.XmlEssentials.Tests.POCOs.MixedOfObjects.Content' is marked [XmlMixedAttribute] but is not a collection of XmlMixedContentItem.");
            FluentActions.Invoking(() => Bare.GetTypeInfo<MixedNotACollection>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Member 'CloudNimble.XmlEssentials.Tests.POCOs.MixedNotACollection.Content' is marked [XmlMixedAttribute] but is not a collection of XmlMixedContentItem.");
            FluentActions.Invoking(() => XmlSerializer.Serialize(new MixedOfObjects { Content = { "text" } }))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Member 'CloudNimble.XmlEssentials.Tests.POCOs.MixedOfObjects.Content' is marked [XmlMixedAttribute] but is not a collection of XmlMixedContentItem.");
        }

        [TestMethod]
        public void TwoMixed_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<TwoMixed>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "The type 'CloudNimble.XmlEssentials.Tests.POCOs.TwoMixed' has more than one [XmlMixedAttribute].");
        }

        [TestMethod]
        public void Node_Preserve_UnknownRef_Throws()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, ReferenceHandler = XmlReferenceHandler.Preserve };
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Node>(
                "<Node xmlns:ns1=\"urn:CloudNimble.Text.Xml:refs\" ns1:id=\"1\"><Child ns1:ref=\"99\"/><Name>root</Name></Node>", options))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Unknown reference '99'. Path: /Node/Child | LineNumber: 1 | BytePositionInLine: 81.");
        }

        [TestMethod]
        public void Node_Preserve_SkipsContentOnARefElement()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, ReferenceHandler = XmlReferenceHandler.Preserve };
            var read = XmlSerializer.Deserialize<Node>(
                "<Node xmlns:ns1=\"urn:CloudNimble.Text.Xml:refs\" ns1:id=\"1\"><Name>root</Name><Child ns1:ref=\"1\"><Name>ignored</Name></Child></Node>", options)!;
            object.ReferenceEquals(read, read.Child).Should().BeTrue();
            read.Name.Should().Be("root");
        }

        [TestMethod]
        public void ExtraBag_OmitsXmlnsFromExtensionAttributes()
        {
            var bag = new ExtraBag { Name = "Ada" };
            bag.Extra.Attributes[new XmlName("xmlns")] = "urn:x";
            XmlSerializer.Serialize(bag, Bare).Should().Be("<ExtraBag><Name>Ada</Name></ExtraBag>");
        }

        [TestMethod]
        public void TypedArrayItems_UsesTheGenericItemName()
        {
            XmlSerializer.Serialize(new TypedArrayItems { Values = [1, 2] }, Bare).Should().Be("<TypedArrayItems><Values><N>1</N><N>2</N></Values></TypedArrayItems>");
            XmlSerializer.Deserialize<TypedArrayItems>("<TypedArrayItems><Values><N>3</N></Values></TypedArrayItems>")!.Values.Should().Equal(3);
        }

        [TestMethod]
        public void NamespacedChoice_WritesTheTypedElementInItsNamespace()
        {
            var xml = XmlSerializer.Serialize(new NamespacedChoice { Item = new Person { Name = "Ada", Age = 1 } }, Bare);
            xml.Should().Be("<NamespacedChoice><P xmlns=\"urn:p\"><Name>Ada</Name><Age>1</Age></P></NamespacedChoice>");
            var read = XmlSerializer.Deserialize<NamespacedChoice>("<NamespacedChoice><P xmlns=\"urn:p\"><Name>Ada</Name><Age>1</Age></P></NamespacedChoice>")!;
            read.Item!.Name.Should().Be("Ada");
        }

        [TestMethod]
        public void WrappedCatalog_NullItems_WritesNil()
        {
            XmlSerializer.Serialize(new WrappedCatalog { Title = "Ada", Items = null }, Bare).Should().Be(
                "<WrappedCatalog><Title>Ada</Title><Items xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></WrappedCatalog>");
        }

        [TestMethod]
        public void XsiShelf_IgnoresSiblingAttributesWhenReadingTheType()
        {
            var read = XmlSerializer.Deserialize<XsiShelf>(
                "<XsiShelf><Item xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" extra=\"1\" xsi:type=\"Book\"><Title>Clean Code</Title><Author>Ada</Author></Item></XsiShelf>")!;
            read.Item.Should().BeOfType<XsiBook>();
            ((XsiBook)read.Item!).Author.Should().Be("Ada");
        }

        [TestMethod]
        public void KindProduct_WithoutKind_ReadsAsTheBaseType()
        {
            var read = XmlSerializer.Deserialize<KindProduct>("<KindProduct><Title>x</Title></KindProduct>")!;
            read.Should().BeOfType<KindProduct>();
            read.Title.Should().Be("x");
        }

        [TestMethod]
        public void KindProduct_UnknownRootDiscriminator_Throws()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<KindProduct>("<KindProduct kind=\"tape\"><Title>x</Title></KindProduct>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Unknown type discriminator 'tape'. Path: /KindProduct | LineNumber: 1 | BytePositionInLine: 26.");
        }

        [TestMethod]
        public void NearProduct_WritesTheNearestAncestorAsTheRoot()
        {
            var xml = XmlSerializer.Serialize<NearProduct>(new NearSpecialBook { Title = "x", Author = "Ada" }, Bare);
            xml.Should().Be("<NearBook><Title>x</Title><Author>Ada</Author></NearBook>");
        }

        [TestMethod]
        public void NearTape_WritesAsTheDeclaredBaseMember()
        {
            var xml = XmlSerializer.Serialize(new NearShelf { Item = new NearTape { Title = "x" } }, Bare);
            xml.Should().Be("<NearShelf><Item><Title>x</Title></Item></NearShelf>");
        }

        [TestMethod]
        public void BadKindCatalog_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<BadKindCatalog>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "[XmlPolymorphicAttribute] on 'CloudNimble.XmlEssentials.Tests.POCOs.BadKindProduct' uses Attribute and requires TypeDiscriminatorPropertyName.");
        }

        [TestMethod]
        public void Node_Preserve_WritesIdOnANestedFirstSeenObject()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, ReferenceHandler = XmlReferenceHandler.Preserve };
            var xml = XmlSerializer.Serialize(new Node { Name = "root", Child = new Node { Name = "child" } }, options);
            xml.Should().Be("<Node xmlns:ns1=\"urn:CloudNimble.Text.Xml:refs\" ns1:id=\"1\"><Child ns1:id=\"2\"><Child xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><Name>child</Name></Child><Name>root</Name></Node>");
            var read = XmlSerializer.Deserialize<Node>(xml, options)!;
            read.Name.Should().Be("root");
            read.Child!.Name.Should().Be("child");
        }

        [TestMethod]
        public void Catalog_NullItem_WritesNil()
        {
            XmlSerializer.Serialize(new Catalog { Title = "Ada", Items = [null!, "a"] }, Bare).Should().Be(
                "<Catalog><Items xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><Items>a</Items><Title>Ada</Title></Catalog>");
        }

        [TestMethod]
        public void TextMapped_NullValue_OmitsContent()
        {
            XmlSerializer.Serialize(new TextMapped { Value = null }, Bare).Should().Be("<TextMapped/>");
        }

        [TestMethod]
        public void CDataNumber_WritesTheNumberInCData()
        {
            XmlSerializer.Serialize(new CDataNumber { Value = 7 }, Bare).Should().Be("<CDataNumber><![CDATA[7]]></CDataNumber>");
            XmlSerializer.Deserialize<CDataNumber>("<CDataNumber><![CDATA[8]]></CDataNumber>")!.Value.Should().Be(8);
        }

        [TestMethod]
        public void NullableText_RoundTrips()
        {
            XmlSerializer.Serialize(new NullableText { Value = 4 }, Bare).Should().Be("<NullableText>4</NullableText>");
            XmlSerializer.Deserialize<NullableText>("<NullableText>5</NullableText>")!.Value.Should().Be(5);
        }

        [TestMethod]
        public void TextMapped_JoinsSplitText()
        {
            XmlSerializer.Deserialize<TextMapped>("<TextMapped>hel<!--x-->lo</TextMapped>")!.Value.Should().Be("hello");
        }

        [TestMethod]
        public void TwoArrayItems_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<TwoArrayItems>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Member 'CloudNimble.XmlEssentials.Tests.POCOs.TwoArrayItems.Values' has more than one [XmlArrayItemAttribute] or a typed item choice. Polymorphic collection items are not enabled for this type.");
        }

        [TestMethod]
        public void ComplexText_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<ComplexText>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Member 'CloudNimble.XmlEssentials.Tests.POCOs.ComplexText.Value' is mapped as XML text. Complex types cannot be simple content.");
        }

        [TestMethod]
        public void CtorCatalog_BindsUnwrappedItems()
        {
            var read = XmlSerializer.Deserialize<CtorCatalog>("<CtorCatalog><Items>a</Items><Items>b</Items><Title>Ada</Title></CtorCatalog>")!;
            read.Title.Should().Be("Ada");
            read.Items.Should().Equal("a", "b");
        }

        [TestMethod]
        public void CollectionOfInt32_RoundTrips()
        {
            var xml = XmlSerializer.Serialize(new Collection<int> { 1, 2 }, Bare);
            xml.Should().Be("<ArrayOfInt><int>1</int><int>2</int></ArrayOfInt>");
            XmlSerializer.Deserialize<Collection<int>>(xml).Should().Equal(1, 2);
        }

        [TestMethod]
        public void Scorecard_EmptyKey_WritesKeyValuePairs()
        {
            var xml = XmlSerializer.Serialize(new Scorecard { Scores = new() { [""] = 10 } }, Bare);
            xml.Should().Be("<Scorecard><Scores><Key></Key><Value>10</Value></Scores></Scorecard>");
        }

        [TestMethod]
        public void MultidimensionalArray_IsNotACollection()
        {
            FluentActions.Invoking(() => Bare.GetConverter(typeof(int[,])))
                .Should().ThrowExactly<NotSupportedException>();
        }

        [TestMethod]
        public void ObjectConverter_ReadLexical_Throws()
        {
            var converter = (XmlConverter<Person>)Bare.GetConverter(typeof(Person));
            FluentActions.Invoking(() => converter.ReadLexical("Ada"u8, Bare))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(
                    "Converter 'CloudNimble.Text.Xml.Serialization.XmlObjectConverter`1[CloudNimble.XmlEssentials.Tests.POCOs.Person]' does not support simple content.");
        }

        [TestMethod]
        public void FallbackShelf_UnknownDiscriminator_ReadsAsTheBaseType()
        {
            var read = XmlSerializer.Deserialize<FallbackShelf>("<FallbackShelf><Item kind=\"tape\"><Title>x</Title></Item></FallbackShelf>")!;
            read.Item.Should().BeOfType<FallbackProduct>();
            read.Item!.Title.Should().Be("x");
        }

        [TestMethod]
        public void TypedAndNamedElement_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<TypedAndNamedElement>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Member 'CloudNimble.XmlEssentials.Tests.POCOs.TypedAndNamedElement.Item' has more than one [XmlElementAttribute] or a typed element choice. Polymorphic element choice is not enabled for this type.");
        }

        [TestMethod]
        public void TextAndAttribute_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<TextAndAttribute>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Member 'CloudNimble.XmlEssentials.Tests.POCOs.TextAndAttribute.Value' is mapped as both simple content and an attribute or element.");
        }

        [TestMethod]
        public void HashSetOfInt32_RoundTrips()
        {
            var xml = XmlSerializer.Serialize(new HashSet<int> { 7 }, Bare);
            xml.Should().Be("<ArrayOfInt><int>7</int></ArrayOfInt>");
            XmlSerializer.Deserialize<HashSet<int>>(xml).Should().Equal(7);
        }

        [TestMethod]
        public void CollectionFactory_ReturnsNullForANonCollection()
        {
            XmlCollectionConverterFactory.Instance.CreateConverter(typeof(int), Bare).Should().BeNull();
        }

        [TestMethod]
        public void GetOnlyExtra_CapturesIntoTheExistingBag()
        {
            var read = XmlSerializer.Deserialize<GetOnlyExtra>("<GetOnlyExtra note=\"x\"><Name>Ada</Name><Nick>Ada</Nick></GetOnlyExtra>")!;
            read.Name.Should().Be("Ada");
            read.Extra.Attributes[new XmlName("note")].Should().Be("x");
            read.Extra.Elements.Should().HaveCount(1);
            read.Extra.Elements[0].LocalName.Should().Be("Nick");
        }

        [TestMethod]
        public void NullExtra_AssignsANewBag()
        {
            var read = XmlSerializer.Deserialize<NullExtra>("<NullExtra note=\"x\"><Name>Ada</Name></NullExtra>")!;
            read.Name.Should().Be("Ada");
            read.Extra.Should().NotBeNull();
            read.Extra!.Attributes[new XmlName("note")].Should().Be("x");
        }

        [TestMethod]
        public void FallbackProduct_UnknownRootDiscriminator_ReadsAsTheBaseType()
        {
            var read = XmlSerializer.Deserialize<FallbackProduct>("<FallbackProduct kind=\"tape\"><Title>x</Title></FallbackProduct>")!;
            read.Should().BeOfType<FallbackProduct>();
            read.Title.Should().Be("x");
        }

        [TestMethod]
        public void FallbackTape_AsRoot_WritesTheBaseElement()
        {
            XmlSerializer.Serialize<FallbackProduct>(new FallbackTape { Title = "x" }, Bare).Should().Be("<FallbackProduct><Title>x</Title></FallbackProduct>");
        }

        [TestMethod]
        public void TextPlusElement_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<TextPlusElement>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "The type 'CloudNimble.XmlEssentials.Tests.POCOs.TextPlusElement' maps simple content and child elements. Use [XmlMixedAttribute] for mixed content.");
        }

        [TestMethod]
        public void AmbiguousBox_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<AmbiguousBox>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "The type 'CloudNimble.XmlEssentials.Tests.POCOs.AmbiguousBox' has more than one constructor that can bind its members. Mark one with [XmlConstructorAttribute].");
        }

        [TestMethod]
        public void DuplicateProperty_ThrowsWhenDisallowed()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Person>("<Person><Age>1</Age><Age>2</Age></Person>", XmlSerializerOptions.Strict))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Duplicate property 'Age'. Path: /Person | LineNumber: 1 | BytePositionInLine: 26.");
        }

        [TestMethod]
        public void PacketOfInt32_UsesGenericRootName()
        {
            XmlSerializer.Serialize(new Packet<int> { Value = 4 }, Bare).Should().Be("<PacketOfInt32><Value>4</Value></PacketOfInt32>");
            XmlSerializer.Deserialize<Packet<int>>("<PacketOfInt32><Value>4</Value></PacketOfInt32>")!.Value.Should().Be(4);
        }

        [TestMethod]
        public void ScalarTypeInfo_HasNoObjectMembers()
        {
            var info = XmlSerializerOptions.Default.GetTypeInfo<int>();
            info.Properties.Should().BeEmpty();
            info.CreateObject.Should().BeNull();
        }

        [TestMethod]
        public void ElementNamed_AndNamespacedChild()
        {
            XmlSerializer.Serialize(new ElementNamed { Name = "Ada" }, Bare).Should().Be("<ElementNamed><Nick>Ada</Nick></ElementNamed>");
            XmlSerializer.Serialize(new NamespacedChild { N = 1 }, Bare).Should().Be("<NamespacedChild><N xmlns=\"urn:n\">1</N></NamespacedChild>");
        }

        [TestMethod]
        public void BareRoot_UsesTypeName()
        {
            XmlSerializer.Serialize(new BareRoot { N = 3 }, Bare).Should().Be("<BareRoot><N>3</N></BareRoot>");
        }

        [TestMethod]
        public void TwinNames_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<TwinNames>())
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(
                    "The type maps more than one member to the element name 'X'.");
            FluentActions.Invoking(() => AttributeBare.GetTypeInfo<TwinNames>())
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(
                    "The type maps more than one member to the attribute name 'X'.");
        }

        [TestMethod]
        public void TwoMarkedCtors_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<TwoMarkedCtors>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "The type 'CloudNimble.XmlEssentials.Tests.POCOs.TwoMarkedCtors' has more than one constructor marked [XmlConstructorAttribute].");
        }

        [TestMethod]
        public void AttributeNamed_WritesANamespacedAttribute()
        {
            XmlSerializer.Serialize(new AttributeNamed { Id = 1 }, Bare).Should().Be("<AttributeNamed xmlns:ns1=\"urn:x\" ns1:id=\"1\"/>");
            XmlSerializer.Deserialize<AttributeNamed>("<AttributeNamed p:id=\"1\" xmlns:p=\"urn:x\"/>")!.Id.Should().Be(1);
        }

        [TestMethod]
        public void AttributeAndElement_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<AttributeAndElement>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Member 'CloudNimble.XmlEssentials.Tests.POCOs.AttributeAndElement.N' is mapped as both an XML attribute and an element.");
        }

        [TestMethod]
        public void AttributeOnComplex_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<AttributeOnComplex>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Member 'CloudNimble.XmlEssentials.Tests.POCOs.AttributeOnComplex.Home' is mapped as an XML attribute. Complex types cannot be attributes.");
        }

        [TestMethod]
        public void ChoiceElement_WritesTheTypedElementName()
        {
            var choice = new ChoiceElement { Item = new Person { Name = "Ada", Age = 36 } };
            XmlSerializer.Serialize(choice, Bare).Should().Be("<ChoiceElement><P><Name>Ada</Name><Age>36</Age></P></ChoiceElement>");
            var read = XmlSerializer.Deserialize<ChoiceElement>("<ChoiceElement><P><Name>Ada</Name><Age>36</Age></P></ChoiceElement>")!;
            read.Item.Should().BeOfType<Person>();
            ((Person)read.Item!).Name.Should().Be("Ada");
        }

        [TestMethod]
        public void TypeOnlyElement_UsesTheMemberName()
        {
            var choice = new TypeOnlyElement { Item = new Person { Name = "Ada", Age = 1 } };
            XmlSerializer.Serialize(choice, Bare).Should().Be("<TypeOnlyElement><Item><Name>Ada</Name><Age>1</Age></Item></TypeOnlyElement>");
        }

        [TestMethod]
        public void NullMember_IsNil_UnlessTheIgnoreConditionLeavesItOut()
        {
            XmlSerializer.Serialize(new Person { Age = 1 }, new XmlSerializerOptions { OmitXmlDeclaration = true }).Should().Be(
                "<Person><Name xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><Age>1</Age></Person>");
            var whenNull = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull };
            XmlSerializer.Serialize(new Person { Age = 1 }, whenNull).Should().Be("<Person><Age>1</Age></Person>");
            var whenDefault = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingDefault };
            XmlSerializer.Serialize(new Person { Age = 1 }, whenDefault).Should().Be("<Person><Age>1</Age></Person>");
            XmlSerializer.Serialize(new Person(), whenDefault).Should().Be("<Person/>");
        }

        [TestMethod]
        public void WhenWritingDefault_OmitsZeroAge()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingDefault };
            XmlSerializer.Serialize(new Person { Name = "Ada" }, options).Should().Be("<Person><Name>Ada</Name></Person>");
        }

        [TestMethod]
        public void IgnoreReadOnlyProperties_OmitsGetOnly()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, IgnoreReadOnlyProperties = true };
            XmlSerializer.Serialize(new ReadOnlyHolder { Name = "Ada" }, options).Should().Be("<ReadOnlyHolder><Name>Ada</Name></ReadOnlyHolder>");
        }

        [TestMethod]
        public void PropertyNameCaseInsensitive_ReadsFoldedNames()
        {
            var options = new XmlSerializerOptions { PropertyNameCaseInsensitive = true };
            var read = XmlSerializer.Deserialize<Person>("<Person><name>Ada</name><AGE>9</AGE></Person>", options)!;
            read.Name.Should().Be("Ada");
            read.Age.Should().Be(9);
        }

        [TestMethod]
        public void CtorBox_MissingValue_Throws()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<CtorBox>("<CtorBox/>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The required constructor parameter 'Value' is missing. Path: /CtorBox | LineNumber: 1 | BytePositionInLine: 11.");
        }

        [TestMethod]
        public void DefaultPropertyMapping_Attribute_WritesScalarsAsAttributes()
        {
            var person = new Person { Name = "Ada", Age = 36 };
            XmlSerializer.Serialize(person, AttributeBare).Should().Be("<Person Name=\"Ada\" Age=\"36\"/>");
            var read = XmlSerializer.Deserialize<Person>("<Person Name=\"Ada\" Age=\"36\"/>", AttributeBare)!;
            read.Name.Should().Be("Ada");
            read.Age.Should().Be(36);
        }

        [TestMethod]
        public void DefaultPropertyMapping_Attribute_NestedObjectStaysAnElement_WithAttributeMembers()
        {
            var employee = new Employee
            {
                Name = "Ada",
                Home = new Address { Street = "1 Main", City = "Paris" },
            };
            const string xml = "<Employee Name=\"Ada\"><Home Street=\"1 Main\" City=\"Paris\"/></Employee>";
            XmlSerializer.Serialize(employee, AttributeBare).Should().Be(xml);
            var read = XmlSerializer.Deserialize<Employee>(xml, AttributeBare)!;
            read.Name.Should().Be("Ada");
            read.Home.Should().NotBeNull();
            read.Home!.Street.Should().Be("1 Main");
            read.Home.City.Should().Be("Paris");
        }

        [TestMethod]
        public void DefaultPropertyMapping_Attribute_NullNestedObject_IsANilElement()
        {
            XmlSerializer.Serialize(new Employee { Name = "Ada" }, AttributeBare).Should().Be(
                "<Employee Name=\"Ada\"><Home xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></Employee>");
            var read = XmlSerializer.Deserialize<Employee>(
                "<Employee Name=\"Ada\"><Home xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></Employee>", AttributeBare)!;
            read.Name.Should().Be("Ada");
            read.Home.Should().BeNull();
        }

        [TestMethod]
        public void DefaultPropertyMapping_Attribute_OmitsNullScalars()
        {
            XmlSerializer.Serialize(new Person { Age = 1 }, AttributeBare).Should().Be("<Person Age=\"1\"/>");
            var read = XmlSerializer.Deserialize<Person>("<Person Age=\"1\"/>", AttributeBare)!;
            read.Name.Should().BeNull();
            read.Age.Should().Be(1);
        }

        [TestMethod]
        public void DefaultPropertyMapping_Attribute_PointStruct_WritesAttributes()
        {
            XmlSerializer.Serialize(new Point { X = 3, Y = 4 }, AttributeBare).Should().Be("<Point X=\"3\" Y=\"4\"/>");
            var read = XmlSerializer.Deserialize<Point>("<Point X=\"3\" Y=\"4\"/>", AttributeBare);
            read.X.Should().Be(3);
            read.Y.Should().Be(4);
        }

        [TestMethod]
        public void DefaultPropertyMapping_Attribute_XmlElement_StillWritesAnElement()
        {
            XmlSerializer.Serialize(new ElementNamed { Name = "Ada" }, AttributeBare).Should().Be("<ElementNamed><Nick>Ada</Nick></ElementNamed>");
        }

        [TestMethod]
        public void DefaultPropertyMapping_Attribute_WebCamelCase()
        {
            var options = new XmlSerializerOptions(XmlSerializerDefaults.Web)
            {
                DefaultPropertyMapping = XmlPropertyMapping.Attribute,
            };
            XmlSerializer.Serialize(new Person { Name = "Ada", Age = 36 }, options).Should().Be("<person name=\"Ada\" age=\"36\"/>");
            var read = XmlSerializer.Deserialize<Person>("<person name=\"Ada\" age=\"36\"/>", options)!;
            read.Name.Should().Be("Ada");
            read.Age.Should().Be(36);
        }

        [TestMethod]
        public void DefaultPropertyMapping_Attribute_NodeRecursesThroughNestedObjects()
        {
            var node = new Node { Name = "root", Child = new Node { Name = "leaf" } };
            const string xml = "<Node Name=\"root\"><Child Name=\"leaf\"><Child xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></Child></Node>";
            XmlSerializer.Serialize(node, AttributeBare).Should().Be(xml);
            var read = XmlSerializer.Deserialize<Node>(xml, AttributeBare)!;
            read.Name.Should().Be("root");
            read.Child.Should().NotBeNull();
            read.Child!.Name.Should().Be("leaf");
            read.Child.Child.Should().BeNull();
        }

        [TestMethod]
        public void DefaultPropertyMapping_Attribute_NamespacedName_DoesNotBindAnUnprefixedMember()
        {
            var read = XmlSerializer.Deserialize<Person>("<Person p:Name=\"Ada\" Age=\"36\" xmlns:p=\"urn:other\"/>", AttributeBare)!;
            read.Name.Should().BeNull();
            read.Age.Should().Be(36);
        }

        [TestMethod]
        public void DefaultPropertyMapping_Attribute_UnknownAttribute_IsRejectedWhenDisallowed()
        {
            var options = new XmlSerializerOptions
            {
                DefaultPropertyMapping = XmlPropertyMapping.Attribute,
                UnmappedMemberHandling = XmlUnmappedMemberHandling.Disallow,
            };
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Person>("<Person Name=\"Ada\" extra=\"1\"/>", options))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Unknown attribute 'extra'. Path: /Person | LineNumber: 1 | BytePositionInLine: 31.");
        }

        [TestMethod]
        public void ObjectFactory_RejectsScalars()
        {
            XmlObjectConverterFactory.Instance.CanConvert(typeof(int)).Should().BeFalse();
            XmlObjectConverterFactory.Instance.CreateConverter(typeof(int), Bare).Should().BeNull();
        }

        [TestMethod]
        public void TypeInfo_ExposesProperties()
        {
            var info = Bare.GetTypeInfo<Person>();
            info.Properties.Count.Should().Be(2);
            info.Properties[0].Name.LocalName.Should().Be("Name");
            info.Properties[1].Name.LocalName.Should().Be("Age");
            info.CreateObject.Should().NotBeNull();
            info.CreateObject!().Should().BeOfType<Person>();
        }

        [TestMethod]
        public void TypeInfo_Properties_IsAListThatCannotBeChanged()
        {
            // The type info is read-only, so its list of members is too. Each change is refused as System.Text.Json
            // refuses a change to a read-only JsonTypeInfo, and an argument is checked first.
            const string readOnly = "This XmlTypeInfo instance is marked read-only or has already been used in serialization or deserialization.";
            var properties = Bare.GetTypeInfo<Person>().Properties;
            var name = properties[0];
            var age = properties[1];

            properties.IsReadOnly.Should().BeTrue();
            properties.Contains(age).Should().BeTrue();
            properties.IndexOf(age).Should().Be(1);
            properties.Contains(Bare.GetTypeInfo<MixedOrder>().Properties[0]).Should().BeFalse();
            properties.IndexOf(Bare.GetTypeInfo<MixedOrder>().Properties[0]).Should().Be(-1);
            var copy = new XmlPropertyInfo?[3];
            properties.CopyTo(copy!, 1);
            copy.Should().Equal(null, name, age);
            properties.Select(property => property.Name.LocalName).Should().Equal("Name", "Age");
            var untyped = new List<object?>();
            foreach (var property in (System.Collections.IEnumerable)properties)
            {
                untyped.Add(property);
            }

            untyped.Should().Equal(name, age);

            FluentActions.Invoking(() => properties.Add(age)).Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(readOnly);
            FluentActions.Invoking(() => properties.Insert(0, age)).Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(readOnly);
            FluentActions.Invoking(() => properties[0] = age).Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(readOnly);
            FluentActions.Invoking(() => properties.Remove(age)).Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(readOnly);
            FluentActions.Invoking(() => properties.RemoveAt(0)).Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(readOnly);
            FluentActions.Invoking(properties.Clear).Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(readOnly);
            FluentActions.Invoking(() => properties.Add(null!)).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("item");
            FluentActions.Invoking(() => properties.Insert(0, null!)).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("item");
            FluentActions.Invoking(() => properties[0] = null!).Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("value");

            // Nothing changed, and a type with no members to describe has the same kind of list.
            properties.Should().Equal(name, age);
            var none = XmlSerializerOptions.Default.GetTypeInfo<int>().Properties;
            none.Count.Should().Be(0);
            none.IsReadOnly.Should().BeTrue();
            FluentActions.Invoking(() => none.Add(age)).Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(readOnly);
        }

        [TestMethod]
        public void Point_RoundTripsAsAStruct()
        {
            XmlSerializer.Serialize(new Point { X = 3, Y = 4 }, Bare).Should().Be("<Point><X>3</X><Y>4</Y></Point>");
            var read = XmlSerializer.Deserialize<Point>("<Point><X>3</X><Y>4</Y></Point>");
            read.X.Should().Be(3);
            read.Y.Should().Be(4);
        }

        [TestMethod]
        public void MixedOrder_WritesAPositiveOrderAfterTheMembersThatSetNone()
        {
            // As System.Text.Json places it: a member that sets no order is placed as one that sets zero.
            XmlSerializer.Serialize(new MixedOrder { A = 1, Z = 2 }, Bare).Should().Be("<MixedOrder><Z>2</Z><A>1</A></MixedOrder>");
            var read = XmlSerializer.Deserialize<MixedOrder>("<MixedOrder><Z>2</Z><A>1</A></MixedOrder>")!;
            read.Z.Should().Be(2);
            read.A.Should().Be(1);
        }

        [TestMethod]
        public void ElementOrdered_WritesANegativeXmlElementOrderBeforeTheMembersThatSetNone()
        {
            XmlSerializer.Serialize(new ElementOrdered { A = 1, Z = 2 }, Bare).Should().Be("<ElementOrdered><Z>2</Z><A>1</A></ElementOrdered>");
        }

        [TestMethod]
        public void Order_IsNullWhereAMemberSetsNone_AndZeroIsAnOrderAMemberSet()
        {
            // The contract tells a member that set zero from one that set nothing. Both are placed at zero, so
            // they keep the order they are declared in.
            static string Orders<T>() => string.Join(",", Bare.GetTypeInfo<T>().Properties.Select(property => property.Name.LocalName + "=" + (property.Order?.ToString() ?? "null")));

            Orders<ZeroOrdered>().Should().Be("A=null,B=0,C=null");
            XmlSerializer.Serialize(new ZeroOrdered { A = 1, B = 2, C = 3 }, Bare).Should().Be("<ZeroOrdered><A>1</A><B>2</B><C>3</C></ZeroOrdered>");
            Orders<MixedOrder>().Should().Be("Z=null,A=5");
            Orders<ElementOrdered>().Should().Be("Z=-1,A=null");
            Orders<WrappedCatalog>().Should().Be("Title=null,Items=1");
            new XmlElementAttribute().Order.Should().Be(0);
            new XmlArrayAttribute().Order.Should().Be(0);
        }

        [TestMethod]
        public void OptionalName_UsesConstructorDefault()
        {
            XmlSerializer.Deserialize<OptionalName>("<OptionalName/>")!.Name.Should().Be("anon");
            XmlSerializer.Deserialize<OptionalName>("<OptionalName><Name>Ada</Name></OptionalName>")!.Name.Should().Be("Ada");
        }

        [TestMethod]
        public void ExtraCtor_IgnoresConstructorParametersThatAreNotMembers()
        {
            XmlSerializer.Serialize(new ExtraCtor { Name = "Ada" }, Bare).Should().Be("<ExtraCtor><Name>Ada</Name></ExtraCtor>");
            XmlSerializer.Deserialize<ExtraCtor>("<ExtraCtor><Name>Ada</Name></ExtraCtor>")!.Name.Should().Be("Ada");
        }

        [TestMethod]
        public void UnmappedCtorParam_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<UnmappedCtorParam>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "The constructor parameter 'unused' is not a serialized member.");
        }

        [TestMethod]
        public void IndexedHolder_AndWriteOnlyHolder_SkipMembersWithoutGetters()
        {
            XmlSerializer.Serialize(new IndexedHolder { Name = "Ada" }, Bare).Should().Be("<IndexedHolder><Name>Ada</Name></IndexedHolder>");
            XmlSerializer.Serialize(new WriteOnlyHolder { Name = "Ada" }, Bare).Should().Be("<WriteOnlyHolder><Name>Ada</Name></WriteOnlyHolder>");
            XmlSerializer.Deserialize<IndexedHolder>("<IndexedHolder><Name>Ada</Name></IndexedHolder>")!.Name.Should().Be("Ada");
            XmlSerializer.Deserialize<WriteOnlyHolder>("<WriteOnlyHolder><Name>Ada</Name></WriteOnlyHolder>")!.Name.Should().Be("Ada");
        }

        [TestMethod]
        public void ReadOnlyFieldBox_IsOmittedWhenIgnoreReadOnlyFields()
        {
            var include = new XmlSerializerOptions { OmitXmlDeclaration = true, IncludeFields = true };
            XmlSerializer.Serialize(new ReadOnlyFieldBox(), include).Should().Be("<ReadOnlyFieldBox><Value>x</Value></ReadOnlyFieldBox>");
            XmlSerializer.Deserialize<ReadOnlyFieldBox>("<ReadOnlyFieldBox><Value>changed</Value></ReadOnlyFieldBox>", include)!.Value.Should().Be("x");

            var skip = new XmlSerializerOptions { OmitXmlDeclaration = true, IncludeFields = true, IgnoreReadOnlyFields = true };
            XmlSerializer.Serialize(new ReadOnlyFieldBox(), skip).Should().Be("<ReadOnlyFieldBox/>");
        }

        [TestMethod]
        public void FieldBox_WithoutIncludeFields_HasNoMembers()
        {
            XmlSerializer.Serialize(new FieldBox { Value = "x" }, Bare).Should().Be("<FieldBox/>");
        }

        [TestMethod]
        public void ReadOnlyHolder_WritesGetOnlyMembers()
        {
            XmlSerializer.Serialize(new ReadOnlyHolder { Name = "Ada" }, Bare).Should().Be(
                "<ReadOnlyHolder><N>7</N><Name>Ada</Name></ReadOnlyHolder>");
            var read = XmlSerializer.Deserialize<ReadOnlyHolder>("<ReadOnlyHolder><N>9</N><Name>Ada</Name></ReadOnlyHolder>")!;
            read.N.Should().Be(7);
            read.Name.Should().Be("Ada");
        }

        [TestMethod]
        public void RepeatedElement_ThrowsAtTypeInfo()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<RepeatedElement>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Member 'CloudNimble.XmlEssentials.Tests.POCOs.RepeatedElement.Value' has more than one [XmlElementAttribute] or a typed element choice. Polymorphic element choice is not enabled for this type.");
        }

        [TestMethod]
        public void NamespacedChild_RoundTrips_AndIgnoresAChildInTheWrongNamespace()
        {
            const string xml = "<NamespacedChild><N xmlns=\"urn:n\">1</N></NamespacedChild>";
            XmlSerializer.Deserialize<NamespacedChild>(xml)!.N.Should().Be(1);
            XmlSerializer.Deserialize<NamespacedChild>("<NamespacedChild><N>1</N></NamespacedChild>")!.N.Should().Be(0);
            FluentActions.Invoking(() => XmlSerializer.Deserialize<NamespacedChild>("<NamespacedChild><N>1</N></NamespacedChild>", XmlSerializerOptions.Strict))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Unknown element 'N'. Path: /NamespacedChild | LineNumber: 1 | BytePositionInLine: 21.");
        }

        [TestMethod]
        public void ElementNamed_ReadsTheRenamedChild()
        {
            XmlSerializer.Deserialize<ElementNamed>("<ElementNamed><Nick>Ada</Nick></ElementNamed>")!.Name.Should().Be("Ada");
        }

        [TestMethod]
        public void RequiredCatalog_RoundTripsWhenTitleIsPresent()
        {
            XmlSerializer.Serialize(new RequiredCatalog { Title = "Ada" }, Bare).Should().Be("<RequiredCatalog><Title>Ada</Title></RequiredCatalog>");
            XmlSerializer.Deserialize<RequiredCatalog>("<RequiredCatalog><Title>Ada</Title></RequiredCatalog>")!.Title.Should().Be("Ada");
        }

        [TestMethod]
        public void RequiredTitle_MissingTitle_Throws()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<RequiredTitle>("<RequiredTitle/>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The required member 'Title' is missing. Path: /RequiredTitle | LineNumber: 1 | BytePositionInLine: 17.");
            XmlSerializer.Deserialize<RequiredTitle>("<RequiredTitle><Title>Ada</Title></RequiredTitle>")!.Title.Should().Be("Ada");
        }

        [TestMethod]
        public void ConditionallyIgnored_OmitsNullAndDefaultMembers()
        {
            XmlSerializer.Serialize(new ConditionallyIgnored(), Bare).Should().Be("<ConditionallyIgnored/>");
            XmlSerializer.Serialize(new ConditionallyIgnored { Name = "Ada" }, Bare).Should().Be("<ConditionallyIgnored><Name>Ada</Name></ConditionallyIgnored>");
            XmlSerializer.Serialize(new ConditionallyIgnored { Age = 1 }, Bare).Should().Be("<ConditionallyIgnored><Age>1</Age></ConditionallyIgnored>");
        }

        [TestMethod]
        public void HiddenCtor_UsesTheMarkedPrivateConstructor()
        {
            XmlSerializer.Serialize(HiddenCtor.Create("hi"), Bare).Should().Be("<HiddenCtor><Value>hi</Value></HiddenCtor>");
            XmlSerializer.Deserialize<HiddenCtor>("<HiddenCtor><Value>hi</Value></HiddenCtor>")!.Value.Should().Be("hi");
        }

        [TestMethod]
        public void PrivateEmptyCtor_IsInvokedThroughReflection()
        {
            XmlSerializer.Serialize(PrivateEmptyCtor.Create("Ada"), Bare).Should().Be("<PrivateEmptyCtor><Name>Ada</Name></PrivateEmptyCtor>");
            XmlSerializer.Deserialize<PrivateEmptyCtor>("<PrivateEmptyCtor><Name>Ada</Name></PrivateEmptyCtor>")!.Name.Should().Be("Ada");
        }

        [TestMethod]
        public void Node_RoundTripsASelfReferentialType()
        {
            var node = new Node { Name = "root", Child = new Node { Name = "leaf" } };
            const string xml = "<Node><Child><Child xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><Name>leaf</Name></Child><Name>root</Name></Node>";
            XmlSerializer.Serialize(node, Bare).Should().Be(xml);
            var read = XmlSerializer.Deserialize<Node>(xml)!;
            read.Name.Should().Be("root");
            read.Child.Should().NotBeNull();
            read.Child!.Name.Should().Be("leaf");
            read.Child.Child.Should().BeNull();
        }

        [TestMethod]
        public void Person_NullRoot_WritesXsiNil()
        {
            XmlSerializer.Serialize<Person?>(null, Bare).Should().Be(
                "<Person xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>");
            XmlSerializer.Deserialize<Person>("<Person xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>").Should().BeNull();
        }

        [TestMethod]
        public void NestedAddress_DeserializesNilAndEmptyName()
        {
            var nil = XmlSerializer.Deserialize<Employee>(
                "<Employee><Name>Ada</Name><Home xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></Employee>")!;
            nil.Name.Should().Be("Ada");
            nil.Home.Should().BeNull();

            var empty = XmlSerializer.Deserialize<Person>("<Person><Name/><Age>1</Age></Person>")!;
            empty.Name.Should().Be("");
            empty.Age.Should().Be(1);

            var namedNil = XmlSerializer.Deserialize<Person>(
                "<Person><Name xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><Age>1</Age></Person>")!;
            namedNil.Name.Should().BeNull();
            namedNil.Age.Should().Be(1);
        }

        [TestMethod]
        public void DuplicateProperty_LastValueWinsWhenAllowed()
        {
            var read = XmlSerializer.Deserialize<Person>("<Person><Age>1</Age><Age>2</Age></Person>")!;
            read.Age.Should().Be(2);
        }

        [TestMethod]
        public void CommentsAndProcessingInstructions_InsideAnObject_AreIgnored()
        {
            var options = new XmlSerializerOptions { ReadCommentHandling = XmlCommentHandling.Allow };
            var read = XmlSerializer.Deserialize<Person>(
                "<Person><!--c--><?pi d?><Name>Ada</Name><?pi e?><Age>36</Age><!--f--></Person>", options)!;
            read.Name.Should().Be("Ada");
            read.Age.Should().Be(36);

            var reader = new Utf8XmlReader(
                "<Person><!--c--><?pi d?><Name>Ada</Name><Age>36</Age></Person>"u8,
                new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow });
            var fromReader = XmlSerializer.Deserialize<Person>(ref reader)!;
            fromReader.Name.Should().Be("Ada");
            fromReader.Age.Should().Be(36);
        }

        [TestMethod]
        public void CharacterContent_BetweenObjectChildren_IsIgnored()
        {
            var read = XmlSerializer.Deserialize<Person>("<Person>hello<Name>Ada</Name>world<Age>36</Age></Person>")!;
            read.Name.Should().Be("Ada");
            read.Age.Should().Be(36);
        }

        [TestMethod]
        public void CData_InsideAnObject_IsRejected()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Person>("<Person><![CDATA[x]]><Name>Ada</Name><Age>1</Age></Person>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Unexpected CData in object content. Path: /Person | LineNumber: 1 | BytePositionInLine: 22.");
        }

        [TestMethod]
        public void Person_WrongRoot_IsRejected()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Person>("<Human><Name>Ada</Name></Human>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The root element does not match 'Person'. Path: /Person | LineNumber: 1 | BytePositionInLine: 8.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Person>("<Person><Name>Ada</Name></Human>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("End tag does not match start tag. Path: /Person | LineNumber: 1 | BytePositionInLine: 33.");
        }

        [TestMethod]
        public void Person_InvalidChild_IsRejected()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Person>("<Person><Name>Ada</Name><Age>nope</Age></Person>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not a valid integer. Path: /Person/Age | LineNumber: 1 | BytePositionInLine: 40.");
        }

        [TestMethod]
        public void Person_TruncatedDocument_IsRejected()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Person>("<Person><Name>Ada</Name>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Unexpected end of XML. Path: /Person | LineNumber: 1 | BytePositionInLine: 25.");
        }

        [TestMethod]
        public void Person_IncompleteReader_ReportsTheElementWasNotClosed()
        {
            FluentActions.Invoking(() =>
            {
                var reader = new Utf8XmlReader("<Person><Name>Ada</Name>"u8, isFinalBlock: false, new XmlReaderState());
                XmlSerializer.Deserialize<Person>(ref reader);
            }).Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The element was not closed. Path: /Person | LineNumber: 1 | BytePositionInLine: 25.");
        }

        [TestMethod]
        public void Person_StreamRoundTrip()
        {
            var utf8 = Encoding.UTF8.GetBytes("<Person><Name>Ada</Name><Age>36</Age></Person>");
            var read = XmlSerializer.Deserialize<Person>(new TrickleStream(utf8, 5))!;
            read.Name.Should().Be("Ada");
            read.Age.Should().Be(36);
        }

        [TestMethod]
        public void CtorBox_MissingValue_IsNullWhenRequiredConstructorParametersAreIgnored()
        {
            var options = new XmlSerializerOptions { RespectRequiredConstructorParameters = false };
            XmlSerializer.Deserialize<CtorBox>("<CtorBox/>", options)!.Value.Should().BeNull();
        }

        [TestMethod]
        public void PersonRecord_MissingValueTypeParameter_IsZeroWhenRequiredConstructorParametersAreIgnored()
        {
            var options = new XmlSerializerOptions { RespectRequiredConstructorParameters = false };
            XmlSerializer.Deserialize<PersonRecord>("<PersonRecord><Name>Ada</Name><Age>36</Age></PersonRecord>", options).Should().Be(new PersonRecord("Ada", 36));
            XmlSerializer.Deserialize<PersonRecord>("<PersonRecord><Name>Bo</Name></PersonRecord>", options).Should().Be(new PersonRecord("Bo", 0));
            XmlSerializer.Deserialize<PersonRecord>("<PersonRecord><Age>7</Age></PersonRecord>", options).Should().Be(new PersonRecord(null!, 7));
        }

        [TestMethod]
        public void ObjectConverter_WriteNull_Throws()
        {
            var converter = Bare.GetTypeInfo<Person>().Converter;
            FluentActions.Invoking(() => XmlWriterHarness.ToXml(writer => converter.Write(writer, null!, Bare)))
                .Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("value");
        }

        [TestMethod]
        public void ObjectFactory_AcceptsObjectsAndStructs_AndRejectsOtherKinds()
        {
            var factory = XmlObjectConverterFactory.Instance;
            factory.CanConvert(typeof(Person)).Should().BeTrue();
            factory.CanConvert(typeof(Point)).Should().BeTrue();
            factory.CanConvert(typeof(string)).Should().BeFalse();
            factory.CanConvert(typeof(int[])).Should().BeFalse();
            factory.CanConvert(typeof(IDisposable)).Should().BeFalse();
            factory.CanConvert(typeof(object)).Should().BeFalse();
            factory.CanConvert(typeof(Action)).Should().BeFalse();
            factory.CanConvert(typeof(int?)).Should().BeFalse();
            factory.CreateConverter(typeof(string), Bare).Should().BeNull();
        }

        #endregion

    }

}
