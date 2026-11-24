using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using CloudNimble.XmlEssentials.Tests.POCOs.Wrappers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Covers how members are matched, held, and written: member order, collections of every shape,
    /// dictionaries, derived types, references, constructors, and simple content that arrives in pieces.
    /// </summary>
    [TestClass]
    public class XmlSerializerObjectMappingTests
    {

        #region Fields

        private const string RefsNamespace = "urn:CloudNimble.Text.Xml:refs";
        private const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true };
        private static readonly XmlSerializerOptions Preserve = new() { OmitXmlDeclaration = true, ReferenceHandler = XmlReferenceHandler.Preserve };

        [TestMethod]
        public void WrappedCatalog_NullItems_AreLeftOutWhenNullsAreIgnored()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull };
            XmlSerializer.Serialize(new WrappedCatalog { Title = "Ada", Items = null }, options).Should().Be("<WrappedCatalog><Title>Ada</Title></WrappedCatalog>");
        }

        #endregion

        #region Public Methods

        [TestMethod]
        public void ArrayHolder_RoundTripsAnArrayMember()
        {
            var xml = XmlSerializer.Serialize(new ArrayHolder { Values = [1, 2, 3] }, Bare);
            xml.Should().Be("<ArrayHolder><Values>1</Values><Values>2</Values><Values>3</Values></ArrayHolder>");
            XmlSerializer.Deserialize<ArrayHolder>(xml)!.Values.Should().Equal(1, 2, 3);
        }

        [TestMethod]
        public void Int32Array_RoundTripsAsTheRoot()
        {
            var xml = XmlSerializer.Serialize(new[] { 1, 2 }, Bare);
            xml.Should().Be("<ArrayOfInt><int>1</int><int>2</int></ArrayOfInt>");
            XmlSerializer.Deserialize<int[]>(xml).Should().Equal(1, 2);
        }

        [TestMethod]
        [DataRow(typeof(bool), "boolean")]
        [DataRow(typeof(byte), "unsignedByte")]
        [DataRow(typeof(sbyte), "byte")]
        [DataRow(typeof(short), "short")]
        [DataRow(typeof(ushort), "unsignedShort")]
        [DataRow(typeof(int), "int")]
        [DataRow(typeof(uint), "unsignedInt")]
        [DataRow(typeof(long), "long")]
        [DataRow(typeof(ulong), "unsignedLong")]
        [DataRow(typeof(float), "float")]
        [DataRow(typeof(double), "double")]
        [DataRow(typeof(decimal), "decimal")]
        [DataRow(typeof(char), "char")]
        [DataRow(typeof(string), "string")]
        [DataRow(typeof(DateTime), "dateTime")]
        [DataRow(typeof(DateTimeOffset), "dateTimeOffset")]
        [DataRow(typeof(DateOnly), "dateOnly")]
        [DataRow(typeof(TimeOnly), "timeOnly")]
        [DataRow(typeof(Guid), "guid")]
        [DataRow(typeof(byte[]), "base64Binary")]
        [DataRow(typeof(TimeSpan), "TimeSpan")]
        [DataRow(typeof(DayOfWeek), "DayOfWeek")]
        [DataRow(typeof(int?), "int")]
        [DataRow(typeof(Person), "Person")]
        [DataRow(typeof(int[]), "ArrayOfInt")]
        [DataRow(typeof(List<int>), "ArrayOfInt")]
        [DataRow(typeof(HashSet<int>), "ArrayOfInt")]
        [DataRow(typeof(IEnumerable<int>), "ArrayOfInt")]
        [DataRow(typeof(int?[]), "ArrayOfInt")]
        [DataRow(typeof(string[]), "ArrayOfString")]
        [DataRow(typeof(uint[]), "ArrayOfUnsignedInt")]
        [DataRow(typeof(DateTime[]), "ArrayOfDateTime")]
        [DataRow(typeof(byte[][]), "ArrayOfBase64Binary")]
        [DataRow(typeof(int[][]), "ArrayOfArrayOfInt")]
        [DataRow(typeof(List<List<int>>), "ArrayOfArrayOfInt")]
        [DataRow(typeof(Person[]), "ArrayOfPerson")]
        [DataRow(typeof(List<Person>), "ArrayOfPerson")]
        [DataRow(typeof(NamedLine), "line-item")]
        [DataRow(typeof(List<NamedLine>), "ArrayOfLineitem")]
        [DataRow(typeof(KeyValuePair<int, string>), "KeyValuePairOfInt32String")]
        [DataRow(typeof(OuterBox<int>.Part), "PartOfInt32")]
        [DataRow(typeof(Dictionary<string, int>), "DictionaryOfStringInt32")]
        [DataRow(typeof(Dictionary<string, int?>), "DictionaryOfStringNullableOfInt32")]
        [DataRow(typeof(Dictionary<string, List<int>>), "DictionaryOfStringListOfInt32")]
        [DataRow(typeof(Dictionary<string, byte[]>), "DictionaryOfStringArrayOfByte")]
        public void DocumentElement_IsNamedAsXmlSerializerNamesTheType(Type type, string name)
        {
            // Probed against System.Xml.Serialization on .NET 11: an XML Schema name for a built-in type,
            // ArrayOf and the item name for every kind of collection, and the CLR names of the arguments in the
            // name of any other generic type. The last six have no counterpart there and follow the same rule.
            Bare.GetTypeInfo(type).RootName.Should().Be(new XmlName(name));
        }

        [TestMethod]
        public void Collections_OfOneItemType_WriteTheSameDocument_AndReadEachOthers()
        {
            const string xml = "<ArrayOfInt><int>1</int><int>2</int></ArrayOfInt>";
            XmlSerializer.Serialize(new[] { 1, 2 }, Bare).Should().Be(xml);
            XmlSerializer.Serialize(new List<int> { 1, 2 }, Bare).Should().Be(xml);
            XmlSerializer.Serialize(new Queue<int>([1, 2]), Bare).Should().Be(xml);
            XmlSerializer.Deserialize<int[]>(xml).Should().Equal(1, 2);
            XmlSerializer.Deserialize<List<int>>(xml).Should().Equal(1, 2);
            XmlSerializer.Deserialize<HashSet<int>>(xml).Should().Equal(1, 2);
            XmlSerializer.Deserialize<IReadOnlyList<int>>(xml).Should().Equal(1, 2);
        }

        [TestMethod]
        public void TypeNamedByXmlRoot_HasThatName_AsAnItemOfACollection()
        {
            // [XmlRoot] names the type wherever the type names an element: at the root, as an item of a
            // collection at the root, and as an item of a collection in a wrapper.
            var lines = new List<NamedLine> { new() { N = 1 }, new() { N = 2 } };
            const string list = "<ArrayOfLineitem><line-item><N>1</N></line-item><line-item><N>2</N></line-item></ArrayOfLineitem>";
            XmlSerializer.Serialize(lines, Bare).Should().Be(list);
            Array.ConvertAll(XmlSerializer.Deserialize<NamedLine[]>(list)!, line => line.N).Should().Equal(1, 2);

            var holder = new NamedLineHolder { Lines = lines, Numbers = [3], Words = [["a", "b"]] };
            const string held = "<NamedLineHolder><Lines><line-item><N>1</N></line-item><line-item><N>2</N></line-item></Lines>"
                + "<Numbers><int>3</int></Numbers><Words><ArrayOfString><string>a</string><string>b</string></ArrayOfString></Words></NamedLineHolder>";
            XmlSerializer.Serialize(holder, Bare).Should().Be(held);
            XmlSerializer.Serialize(XmlSerializer.Deserialize<NamedLineHolder>(held), Bare).Should().Be(held);
        }

        [TestMethod]
        public void AttributeMapped_CaseInsensitive_MatchesTheAttributeName()
        {
            var options = new XmlSerializerOptions { PropertyNameCaseInsensitive = true };
            XmlSerializer.Deserialize<AttributeMapped>("<AttributeMapped id=\"5\"/>", options)!.Id.Should().Be(5);
        }

        [TestMethod]
        public void AttributeMapped_CaseInsensitive_DoesNotMatchAnAttributeInANamespace()
        {
            var options = new XmlSerializerOptions { PropertyNameCaseInsensitive = true };
            var read = XmlSerializer.Deserialize<AttributeMapped>("<AttributeMapped xmlns:p=\"urn:p\" p:id=\"7\" ID=\"5\"/>", options)!;
            read.Id.Should().Be(5);
        }

        [TestMethod]
        public void AttributeMapped_CaseSensitive_IgnoresAnAttributeThatDiffersInCase()
        {
            XmlSerializer.Deserialize<AttributeMapped>("<AttributeMapped id=\"5\"/>")!.Id.Should().Be(0);
        }

        [TestMethod]
        public void AttributeMapped_ReadsAttributesInAnyOrder()
        {
            var options = new XmlSerializerOptions { DefaultPropertyMapping = XmlPropertyMapping.Attribute };
            var read = XmlSerializer.Deserialize<Person>("<Person Age=\"36\" Name=\"Ada\"/>", options)!;
            read.Name.Should().Be("Ada");
            read.Age.Should().Be(36);
        }

        [TestMethod]
        public void BuiltInConverter_WriteAsAttribute_DeclaresANamespaceThatHasNoPrefix()
        {
            var converter = Bare.GetTypeInfo<int>().Converter;
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("e");
                converter.WriteAsAttribute(writer, new XmlName("plain"), 1, Bare);
                converter.WriteAsAttribute(writer, new XmlName("count", "urn:x"), 5, Bare);
                converter.WriteAsAttribute(writer, new XmlName("size", "urn:y", "y"), 7, Bare);
                writer.WriteEndElement();
            }).Should().Be("<e plain=\"1\" xmlns:ns1=\"urn:x\" ns1:count=\"5\" xmlns:y=\"urn:y\" y:size=\"7\"/>");
        }

        [TestMethod]
        public void Catalog_NullItems_WritesNoElements()
        {
            XmlSerializer.Serialize(new Catalog { Title = "Ada", Items = null }, Bare).Should().Be("<Catalog><Title>Ada</Title></Catalog>");
        }

        [TestMethod]
        public void CtorExtra_SetsMembersTheConstructorDoesNotTake()
        {
            var xml = XmlSerializer.Serialize(new CtorExtra("abc") { Note = "n" }, Bare);
            xml.Should().Be("<CtorExtra><Length>3</Length><Note>n</Note><Value>abc</Value></CtorExtra>");
            var read = XmlSerializer.Deserialize<CtorExtra>(xml)!;
            read.Value.Should().Be("abc");
            read.Note.Should().Be("n");
            read.Length.Should().Be(3);
        }

        [TestMethod]
        public void CtorExtra_NilMember_IsSetToNull()
        {
            var read = XmlSerializer.Deserialize<CtorExtra>($"<CtorExtra xmlns:xsi=\"{XsiNamespace}\"><Value>abc</Value><Note xsi:nil=\"true\"/></CtorExtra>")!;
            read.Value.Should().Be("abc");
            read.Note.Should().BeNull();
        }

        [TestMethod]
        public void CtorExtra_MissingMember_IsLeftAlone()
        {
            var read = XmlSerializer.Deserialize<CtorExtra>("<CtorExtra><Value>abc</Value></CtorExtra>")!;
            read.Value.Should().Be("abc");
            read.Note.Should().BeNull();
        }

        [TestMethod]
        public void CustomConverter_ForAValueType_AlsoServesItsNullableForm_AsElementAttributeAndText()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            options.Converters.Add(new HexInt32Converter());
            XmlSerializer.Serialize(new AttributeMapped { Id = 255 }, options).Should().Be("<AttributeMapped Id=\"ff\"/>");
            XmlSerializer.Deserialize<AttributeMapped>("<AttributeMapped Id=\"ff\"/>", options)!.Id.Should().Be(255);
            XmlSerializer.Serialize(new OptionalCount { Count = 255 }, options).Should().Be("<OptionalCount Count=\"ff\"/>");
            XmlSerializer.Serialize(new OptionalCount(), options).Should().Be("<OptionalCount/>");
            XmlSerializer.Deserialize<OptionalCount>("<OptionalCount Count=\"ff\"/>", options)!.Count.Should().Be(255);
            const string namespaced = "<NamespacedOptionalCount xmlns:ns1=\"urn:x\" ns1:count=\"ff\"/>";
            XmlSerializer.Serialize(new NamespacedOptionalCount { Count = 255 }, options).Should().Be(namespaced);
            XmlSerializer.Deserialize<NamespacedOptionalCount>(namespaced, options)!.Count.Should().Be(255);
            XmlSerializer.Serialize(new NullableText { Value = 255 }, options).Should().Be("<NullableText>ff</NullableText>");
            XmlSerializer.Deserialize<NullableText>("<NullableText>ff</NullableText>", options)!.Value.Should().Be(255);
            XmlSerializer.Serialize<int?>(255, options).Should().Be("<int>ff</int>");
            XmlSerializer.Deserialize<int?>("<int>ff</int>", options).Should().Be(255);
            XmlSerializer.Serialize(new Person { Name = "Ada", Age = 255 }, options).Should().Be("<Person><Name>Ada</Name><Age>ff</Age></Person>");
        }

        [TestMethod]
        public void NullableScalars_RoundTripAsMembersItemsAndRoots()
        {
            XmlSerializer.Serialize(new List<int?> { 1, null, 3 }, Bare)
                .Should().Be($"<ArrayOfInt><int>1</int><int xmlns:xsi=\"{XsiNamespace}\" xsi:nil=\"true\"/><int>3</int></ArrayOfInt>");
            XmlSerializer.Deserialize<List<int?>>($"<ArrayOfInt><int>1</int><int xmlns:xsi=\"{XsiNamespace}\" xsi:nil=\"true\"/><int>3</int></ArrayOfInt>")
                .Should().Equal(1, null, 3);
            XmlSerializer.Serialize<DayOfWeek?>(DayOfWeek.Friday, Bare).Should().Be("<DayOfWeek>Friday</DayOfWeek>");
            XmlSerializer.Deserialize<DayOfWeek?>("<DayOfWeek>Friday</DayOfWeek>").Should().Be(DayOfWeek.Friday);
            XmlSerializer.Serialize<TimeSpan?>(TimeSpan.FromSeconds(1.5), Bare).Should().Be("<TimeSpan>PT1.5S</TimeSpan>");
            XmlSerializer.Deserialize<TimeSpan?>("<TimeSpan>PT1.5S</TimeSpan>").Should().Be(TimeSpan.FromSeconds(1.5));
        }

        [TestMethod]
        public void DefaultedCtor_ParameterThatIsNotAMember_TakesItsDefault()
        {
            var read = XmlSerializer.Deserialize<DefaultedCtor>("<DefaultedCtor><Name>Ada</Name><Total>9</Total></DefaultedCtor>")!;
            read.Name.Should().Be("Ada");
            read.Total.Should().Be(5);
        }

        [TestMethod]
        public void LongNames_LongerThanTheNameLimit_AreRejected()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, MaxNameByteCount = 4 };
            FluentActions.Invoking(() => XmlSerializer.Serialize(new LongNames { LongAttribute = "x" }, options))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("MaxNameByteCount exceeded (4). Path: /r.");
            FluentActions.Invoking(() => XmlSerializer.Serialize(new LongNames { LongElement = "x" }, options))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("MaxNameByteCount exceeded (4). Path: /r.");
            XmlSerializer.Serialize(new LongNames(), options).Should().Be("<r/>");
        }

        [TestMethod]
        public void Person_DeeperThanTheDepthLimit_IsRejected()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, MaxDepth = 1 };
            FluentActions.Invoking(() => XmlSerializer.Serialize(new Person { Name = "Ada", Age = 36 }, options))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("MaxDepth exceeded (1). Path: /Person.");
        }

        [TestMethod]
        [DataRow("<Person><Name>Ada</Name><Age>36</Age></Person>", "Ada", DisplayName = "short text")]
        [DataRow("<Person><Name> Ada </Name><Age> 36 </Age></Person>", " Ada ", DisplayName = "blanks around the value are kept in a string and ignored in a number")]
        [DataRow("<Person><Name>a\tb</Name><Age>36</Age></Person>", "a\tb", DisplayName = "a tab inside the value")]
        [DataRow("<Person><Name>Ada Lovelace, Countess of Lovelace</Name><Age>36</Age></Person>", "Ada Lovelace, Countess of Lovelace", DisplayName = "text longer than the short scan")]
        [DataRow("<Person><Name>Ada Lovelace &amp; Charles Babbage</Name><Age>36</Age></Person>", "Ada Lovelace & Charles Babbage", DisplayName = "a reference after the short scan")]
        [DataRow("<Person><Name>Ada</Name ><Age>36</Age\n></Person>", "Ada", DisplayName = "white space inside the end tags")]
        [DataRow("<Person><Name>Ada<!--c--></Name><Age>3<?p x?>6</Age></Person>", "Ada", DisplayName = "a comment or processing instruction after the text")]
        [DataRow("<Person><Name>Ada<![CDATA[]]></Name><Age><![CDATA[36]]></Age></Person>", "Ada", DisplayName = "CDATA beside the text")]
        [DataRow("<p:Person xmlns:p=\"urn:p\"><p:Name>Ada</p:Name><p:Age>36</p:Age></p:Person>", "Ada", DisplayName = "prefixed names")]
        [DataRow("<Person><Name>Ada\nLovelace</Name><Age>36</Age></Person>", "Ada\nLovelace", DisplayName = "a line end inside the value")]
        [DataRow("<Person><Name>Ada &gt; Bo</Name><Age>36</Age></Person>", "Ada > Bo", DisplayName = "a reference inside the short scan")]
        public void Person_ReadsAScalarElement_HoweverItsTextAndEndTagAreWritten(string xml, string name)
        {
            var read = XmlSerializer.Deserialize<Person>(xml)!;
            read.Name.Should().Be(name);
            read.Age.Should().Be(36);
        }

        [TestMethod]
        public void Person_ScalarElement_WithTheWrongEndTag_Throws()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Person>("<Person><Name>Ada</Nome><Age>36</Age></Person>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("End tag does not match start tag. Path: /Person/Name | LineNumber: 1 | BytePositionInLine: 25.");
        }

        [TestMethod]
        public void Person_ScalarElement_LongerThanTheTextLimit_Throws()
        {
            var options = new XmlSerializerOptions { MaxTextByteCount = 4 };
            XmlSerializer.Deserialize<Person>("<Person><Name>Adam</Name><Age>36</Age></Person>", options)!.Name.Should().Be("Adam");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Person>("<Person><Name>Adaline</Name><Age>36</Age></Person>", options))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("MaxTextByteCount exceeded (4). Path: /Person/Name | LineNumber: 1 | BytePositionInLine: 22.");
        }

        [TestMethod]
        public void Person_WrittenAfterAnOpenAttribute_ClosesTheAttributeFirst()
        {
            var converter = Bare.GetTypeInfo<Person>().Converter;
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("r");
                writer.WriteStartAttribute("a");
                writer.WriteStringValue("x");
                converter.Write(writer, new Person { Name = "Ada", Age = 36 }, Bare);
                writer.WriteEndElement();
            }).Should().Be("<r a=\"x\"><Name>Ada</Name><Age>36</Age></r>");
        }

        [TestMethod]
        public void Person_IndentedMembers_EachGetTheirOwnLine()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, WriteIndented = true, NewLine = "\n" };
            XmlSerializer.Serialize(new Employee { Name = "Ada", Home = new Address { Street = "1 Main", City = "Paris" } }, options)
                .Should().Be("<Employee>\n  <Name>Ada</Name>\n  <Home>\n    <Street>1 Main</Street>\n    <City>Paris</City>\n  </Home>\n</Employee>");
        }

        [TestMethod]
        public void Team_RoundTripsAWrappedListOfObjects()
        {
            var xml = XmlSerializer.Serialize(new Team { Members = [new Person { Name = "Ada", Age = 36 }] }, Bare);
            xml.Should().Be("<Team><Members><Person><Name>Ada</Name><Age>36</Age></Person></Members></Team>");
            var read = XmlSerializer.Deserialize<Team>(xml)!;
            read.Members.Should().ContainSingle().Which.Name.Should().Be("Ada");
        }

        [TestMethod]
        public void Dictionary_KeyNamedKey_IsWrittenAsAPair()
        {
            var xml = XmlSerializer.Serialize(new Scorecard { Scores = new() { ["Key"] = 1, ["Value"] = 2 } }, Bare);
            xml.Should().Be("<Scorecard><Scores><Key>Key</Key><Value>1</Value><Value>2</Value></Scores></Scorecard>");
            var read = XmlSerializer.Deserialize<Scorecard>(xml)!;
            read.Scores.Should().Equal(new Dictionary<string, int> { ["Key"] = 1, ["Value"] = 2 });
        }

        [TestMethod]
        public void Dictionary_KeyPolicy_RenamesStringKeys()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DictionaryKeyPolicy = XmlNamingPolicy.CamelCase };
            XmlSerializer.Serialize(new Dictionary<string, int> { ["FirstName"] = 1 }, options)
                .Should().Be("<DictionaryOfStringInt32><firstName>1</firstName></DictionaryOfStringInt32>");
        }

        [TestMethod]
        public void Dictionary_ObjectValues_AreWrittenUnderTheirKeys()
        {
            var xml = XmlSerializer.Serialize(new Dictionary<string, Person> { ["lead"] = new Person { Name = "Ada", Age = 36 } }, Bare);
            xml.Should().Be("<DictionaryOfStringPerson><lead><Name>Ada</Name><Age>36</Age></lead></DictionaryOfStringPerson>");
            var read = XmlSerializer.Deserialize<Dictionary<string, Person>>(xml)!;
            read.Keys.Should().Equal("lead");
            read["lead"].Name.Should().Be("Ada");
            read["lead"].Age.Should().Be(36);
        }

        [TestMethod]
        public void Dictionary_NullValue_IsWrittenAsANilElement()
        {
            var xml = XmlSerializer.Serialize(new Dictionary<string, string?> { ["a"] = null, ["b"] = "x" }, Bare);
            xml.Should().Be("<DictionaryOfStringString><a xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><b>x</b></DictionaryOfStringString>");
        }

        [TestMethod]
        public void Dictionary_EmptyValues_AreWrittenAsAStartAndAnEndTag()
        {
            XmlSerializer.Serialize(new Dictionary<string, string> { ["a"] = string.Empty, ["b"] = "x" }, Bare)
                .Should().Be("<DictionaryOfStringString><a></a><b>x</b></DictionaryOfStringString>");
            XmlSerializer.Serialize(new Dictionary<string, byte[]> { ["a"] = [], ["b"] = [1] }, Bare)
                .Should().Be("<DictionaryOfStringArrayOfByte><a></a><b>AQ==</b></DictionaryOfStringArrayOfByte>");
        }

        [TestMethod]
        public void Dictionary_KeysOfAnyLength_RoundTrip()
        {
            var shorter = new string('a', 32);
            var longer = new string('b', 33);
            var map = new Dictionary<string, int> { [shorter] = 1, [longer] = 2 };
            var xml = XmlSerializer.Serialize(map, Bare);
            xml.Should().Be($"<DictionaryOfStringInt32><{shorter}>1</{shorter}><{longer}>2</{longer}></DictionaryOfStringInt32>");
            XmlSerializer.Deserialize<Dictionary<string, int>>(xml).Should().Equal(map);
        }

        [TestMethod]
        public void Dictionary_Indented_PutsEachEntryOnItsOwnLine()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, WriteIndented = true, NewLine = "\n" };
            XmlSerializer.Serialize(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 }, options)
                .Should().Be("<DictionaryOfStringInt32>\n  <a>1</a>\n  <b>2</b>\n</DictionaryOfStringInt32>");
        }

        [TestMethod]
        public void Dictionary_ConverterCalledAtDocumentLevel_WritesTheEntryAsTheDocumentElement()
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true }))
            {
                Bare.GetTypeInfo<Dictionary<string, int>>().Converter.Write(writer, new Dictionary<string, int> { ["a"] = 1 }, Bare);
            }

            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<a>1</a>");
        }

        [TestMethod]
        public void Dictionary_NonStringKeys_RoundTripAsPairs()
        {
            var xml = XmlSerializer.Serialize(new Dictionary<int, string> { [1] = "one", [2] = "two" }, Bare);
            xml.Should().Be("<DictionaryOfInt32String><Key>1</Key><Value>one</Value><Key>2</Key><Value>two</Value></DictionaryOfInt32String>");
            XmlSerializer.Deserialize<Dictionary<int, string>>(xml).Should().Equal(new Dictionary<int, string> { [1] = "one", [2] = "two" });
        }

        [TestMethod]
        public void Dictionary_NonStringKeys_RejectAnElementNamedForAKey()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Dictionary<int, string>>("<DictionaryOfInt32String><one>1</one></DictionaryOfInt32String>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The element 'one' is not an entry of a dictionary keyed by 'System.Int32'. Path: /DictionaryOfInt32String | LineNumber: 1 | BytePositionInLine: 31.");
        }

        [TestMethod]
        public void Dictionary_Sorted_WritesInKeyOrder()
        {
            var sorted = new SortedDictionary<string, int> { ["b"] = 2, ["a"] = 1 };
            var xml = XmlSerializer.Serialize(sorted, Bare);
            xml.Should().Be("<SortedDictionaryOfStringInt32><a>1</a><b>2</b></SortedDictionaryOfStringInt32>");
            XmlSerializer.Deserialize<SortedDictionary<string, int>>(xml).Should().Equal(sorted);
        }

        [TestMethod]
        public void DerivedTypeName_BesideAWrappedCollection_IsNotAnItemOfTheCollection()
        {
            const string xml = "<PolyWrappedOfNamedShape><NamedCircle><Label>a</Label></NamedCircle><Items><NamedSquare><Label>b</Label></NamedSquare></Items></PolyWrappedOfNamedShape>";
            var read = XmlSerializer.Deserialize<PolyWrapped<NamedShape>>(xml)!;
            read.Items.Should().ContainSingle().Which.Should().BeOfType<NamedSquare>().Which.Label.Should().Be("b");

            var strict = new XmlSerializerOptions { UnmappedMemberHandling = XmlUnmappedMemberHandling.Disallow };
            FluentActions.Invoking(() => XmlSerializer.Deserialize<PolyWrapped<NamedShape>>(xml, strict))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Unknown element 'NamedCircle'. Path: /PolyWrappedOfNamedShape | LineNumber: 1 | BytePositionInLine: 39.");
        }

        [TestMethod]
        public void DerivedTypeNames_OfAMemberAndOfAWrappedCollection_DoNotCompete()
        {
            var album = new ShapeAlbum
            {
                Cover = new NamedCircle { Label = "c", Radius = 2 },
                Pages = [new NamedSquare { Label = "s", Side = 4 }, new NamedCircle { Label = "p", Radius = 3 }],
            };
            const string xml = "<ShapeAlbum><NamedCircle><Label>c</Label><Radius>2</Radius></NamedCircle><Pages><NamedSquare><Label>s</Label><Side>4</Side></NamedSquare><NamedCircle><Label>p</Label><Radius>3</Radius></NamedCircle></Pages></ShapeAlbum>";
            XmlSerializer.Serialize(album, Bare).Should().Be(xml);

            var read = XmlSerializer.Deserialize<ShapeAlbum>(xml)!;
            read.Cover.Should().BeOfType<NamedCircle>().Which.Radius.Should().Be(2);
            read.Pages.Should().HaveCount(2);
            read.Pages[0].Should().BeOfType<NamedSquare>().Which.Side.Should().Be(4);
            read.Pages[1].Should().BeOfType<NamedCircle>().Which.Radius.Should().Be(3);
            XmlSerializer.Serialize(read, Bare).Should().Be(xml);
        }

        [TestMethod]
        public void DerivedTypeNames_OfAMemberAndOfACollectionWithNoWrapper_AreRefused()
        {
            FluentActions.Invoking(() => XmlSerializer.Serialize(new ShapeStrip(), Bare))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be("The type maps more than one member to the element name 'NamedCircle'.");
        }

        [TestMethod]
        public void ElementNameClaimedByTwoMembers_IsRejectedWhenTheTypeIsBound()
        {
            FluentActions.Invoking(() => XmlSerializer.Serialize(new TwinShelf(), Bare))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be("The type maps more than one member to the element name 'Book'.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<BookNamedShelf>("<BookNamedShelf/>"))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be("The type maps more than one member to the element name 'Book'.");
        }

        [TestMethod]
        public void ElementNamesInOneNamespace_AreOneName_AndAreRefused()
        {
            FluentActions.Invoking(() => XmlSerializer.Serialize(new QualifiedTwins(), Bare))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be("The type maps more than one member to the element name 'x'.");
        }

        [TestMethod]
        [DataRow("<LinkPair xmlns:a=\"urn:a\"><a:x>q</a:x><x>p</x></LinkPair>")]
        [DataRow("<LinkPair xmlns:a=\"urn:a\"><x>p</x><a:x>q</a:x></LinkPair>")]
        [DataRow("<LinkPair><x>p</x><x xmlns=\"urn:a\">q</x></LinkPair>")]
        public void ElementNamesInTwoNamespaces_AreTwoNames_AndEachElementGoesToTheMemberOfItsNamespace(string xml)
        {
            var read = XmlSerializer.Deserialize<LinkPair>(xml)!;

            read.Plain.Should().Be("p");
            read.Qualified.Should().Be("q");
            XmlSerializer.Serialize(read, Bare).Should().Be("<LinkPair><x>p</x><x xmlns=\"urn:a\">q</x></LinkPair>");
        }

        [TestMethod]
        public void ElementNamesInTwoNamespaces_LeaveAnElementOfAThirdNamespace_ToTheMemberWithNoNamespace()
        {
            var read = XmlSerializer.Deserialize<LinkPair>("<LinkPair xmlns:b=\"urn:b\"><b:x>o</b:x></LinkPair>")!;

            read.Plain.Should().Be("o");
            read.Qualified.Should().BeNull();
        }

        [TestMethod]
        public void ElementNamesInTwoNamespaces_ThatDifferInCase_AreToldApartByNamespaceWhenCaseIsIgnored()
        {
            var ignoreCase = new XmlSerializerOptions { PropertyNameCaseInsensitive = true };

            var read = XmlSerializer.Deserialize<LinkPairAnyCase>("<LinkPairAnyCase xmlns:a=\"urn:a\"><a:x>q</a:x><X>p</X></LinkPairAnyCase>", ignoreCase)!;

            read.Plain.Should().Be("p");
            read.Qualified.Should().Be("q");
        }

        [TestMethod]
        public void ElementNamesThatDifferOnlyInCase_AreAmbiguousOnlyWhenCaseIsIgnored()
        {
            var shelf = new LowerBookShelf { Item = new Book { Title = "t", Author = "a" }, Label = "l" };
            const string xml = "<LowerBookShelf><Book><Title>t</Title><Author>a</Author></Book><book>l</book></LowerBookShelf>";
            XmlSerializer.Serialize(shelf, Bare).Should().Be(xml);
            var read = XmlSerializer.Deserialize<LowerBookShelf>(xml)!;
            read.Item.Should().BeOfType<Book>().Which.Author.Should().Be("a");
            read.Label.Should().Be("l");
            var ignoreCase = new XmlSerializerOptions { PropertyNameCaseInsensitive = true };
            FluentActions.Invoking(() => XmlSerializer.Deserialize<LowerBookShelf>(xml, ignoreCase))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be("The type maps more than one member to the element name 'book'.");
        }

        [TestMethod]
        public void EmptyValues_AreWrittenAsAStartAndEndTag_WhereverTheyAre()
        {
            XmlSerializer.Serialize(new Person { Name = "", Age = 0 }, Bare).Should().Be("<Person><Name></Name><Age>0</Age></Person>");
            XmlSerializer.Serialize(new Catalog { Title = "", Items = [""] }, Bare).Should().Be("<Catalog><Items></Items><Title></Title></Catalog>");
            XmlSerializer.Serialize(new WrappedCatalog { Title = "t", Items = [""] }, Bare).Should().Be("<WrappedCatalog><Items><Item></Item></Items><Title>t</Title></WrappedCatalog>");
            XmlSerializer.Serialize(new List<string> { "" }, Bare).Should().Be("<ArrayOfString><string></string></ArrayOfString>");
            XmlSerializer.Serialize(new List<byte[]> { Array.Empty<byte>() }, Bare).Should().Be("<ArrayOfBase64Binary><base64Binary></base64Binary></ArrayOfBase64Binary>");
            XmlSerializer.Serialize("", Bare).Should().Be("<string></string>");
            XmlSerializer.Deserialize<Person>("<Person><Name></Name><Age>0</Age></Person>")!.Name.Should().Be("");
        }

        [TestMethod]
        [DataRow(XmlVersion.Version10, "1.0", "a\u0085b\u2028c", DisplayName = "XML 1.0")]
        [DataRow(XmlVersion.Version11, "1.1", "a&#x85;b&#x2028;c", DisplayName = "XML 1.1 escapes NEL and LS so they are not read back as line ends")]
        public void Members_RoundTripInEitherXmlVersion(XmlVersion version, string number, string written)
        {
            var options = new XmlSerializerOptions { XmlVersion = version };
            var xml = XmlSerializer.Serialize(new Person { Name = "a\u0085b\u2028c", Age = 36 }, options);
            xml.Should().Be($"<?xml version=\"{number}\" encoding=\"utf-8\"?><Person><Name>{written}</Name><Age>36</Age></Person>");
            var read = XmlSerializer.Deserialize<Person>(xml, options)!;
            read.Name.Should().Be("a\u0085b\u2028c");
            read.Age.Should().Be(36);
            var attributes = XmlSerializer.Serialize(new LongNames { LongAttribute = "a\u0085b\u2028c" }, options);
            attributes.Should().Be($"<?xml version=\"{number}\" encoding=\"utf-8\"?><r LongAttribute=\"{written}\"/>");
            XmlSerializer.Deserialize<LongNames>(attributes, options)!.LongAttribute.Should().Be("a\u0085b\u2028c");
        }

        [TestMethod]
        public void ExtraBag_KeepsAnUnmappedElementWhenThereAreNoUnmappedAttributes()
        {
            const string xml = "<ExtraBag><Name>Ada</Name><Nick>A</Nick></ExtraBag>";
            var read = XmlSerializer.Deserialize<ExtraBag>(xml)!;
            read.Extra.Attributes.Should().BeEmpty();
            read.Extra.Elements.Should().ContainSingle().Which.ToString().Should().Be("<Nick>A</Nick>");
            XmlSerializer.Serialize(read, Bare).Should().Be(xml);
        }

        [TestMethod]
        public void ExtraBag_KeepsNamespacedAttributesAndElements()
        {
            const string xml = "<ExtraBag xmlns:p=\"urn:p\" p:flag=\"1\" plain=\"2\"><Name>Ada</Name><Nick>A</Nick></ExtraBag>";
            var read = XmlSerializer.Deserialize<ExtraBag>(xml)!;
            read.Name.Should().Be("Ada");
            read.Extra.Attributes.Should().Equal(new Dictionary<XmlName, string>
            {
                [new XmlName("flag", "urn:p", "p")] = "1",
                [new XmlName("plain")] = "2",
            });
            read.Extra.Elements.Should().ContainSingle().Which.ToString().Should().Be("<Nick>A</Nick>");
            XmlSerializer.Serialize(read, Bare).Should().Be(xml);
        }

        [TestMethod]
        public void FieldExtra_KeepsUnmappedMembersInAField()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, IncludeFields = true };
            const string xml = "<FieldExtra note=\"x\"><Name>Ada</Name><Nick>A</Nick></FieldExtra>";
            var read = XmlSerializer.Deserialize<FieldExtra>(xml, options)!;
            read.Name.Should().Be("Ada");
            read.Extra.Attributes.Should().Equal(new Dictionary<XmlName, string> { [new XmlName("note")] = "x" });
            XmlSerializer.Serialize(read, options).Should().Be(xml);
        }

        [TestMethod]
        public void Fragile_ConstructorException_IsThrownAsItIs()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Fragile>("<Fragile><Name>x</Name></Fragile>"))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be("Fragile cannot be created.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<HiddenFragile>("<HiddenFragile><Name>x</Name></HiddenFragile>"))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be("HiddenFragile cannot be created.");
        }

        [TestMethod]
        public void GuardedName_ConstructorException_IsThrownAsItIs()
        {
            XmlSerializer.Deserialize<GuardedName>("<GuardedName><Name>Ada</Name></GuardedName>")!.Name.Should().Be("Ada");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<GuardedName>("<GuardedName><Name></Name></GuardedName>"))
                .Should().ThrowExactly<ArgumentException>().Which.Message.Should().Be("A name is required. (Parameter 'Name')");
        }

        [TestMethod]
        public void HashSetOfInt32_RoundTripsAsTheRoot()
        {
            var xml = XmlSerializer.Serialize(new HashSet<int> { 7 }, Bare);
            xml.Should().Be("<ArrayOfInt><int>7</int></ArrayOfInt>");
            XmlSerializer.Deserialize<HashSet<int>>(xml).Should().Equal(7);
        }

        [TestMethod]
        public void LazyExtra_WithNowhereToKeepThem_DropsUnmappedMembers()
        {
            var read = XmlSerializer.Deserialize<LazyExtra>("<LazyExtra note=\"x\"><Name>Ada</Name><Nick>A</Nick></LazyExtra>")!;
            read.Name.Should().Be("Ada");
            read.Extra.Should().BeNull();
        }

        [TestMethod]
        public void Lookup_RoundTripsADictionaryDeclaredAsAnInterface()
        {
            var xml = XmlSerializer.Serialize(new Lookup { Entries = new Dictionary<string, int> { ["a"] = 1 } }, Bare);
            xml.Should().Be("<Lookup><Entries><a>1</a></Entries></Lookup>");
            var read = XmlSerializer.Deserialize<Lookup>(xml)!;
            read.Entries.Should().BeOfType<Dictionary<string, int>>().Which.Should().Equal(new Dictionary<string, int> { ["a"] = 1 });
        }

        [TestMethod]
        public void MixedPair_ReferenceToAnotherType_Throws()
        {
            var xml = $"<MixedPair xmlns:r=\"{RefsNamespace}\"><First r:id=\"1\"><Name>n</Name></First><Second r:ref=\"1\"/></MixedPair>";
            FluentActions.Invoking(() => XmlSerializer.Deserialize<MixedPair>(xml, Preserve))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Unknown reference '1'. Path: /MixedPair/Second | LineNumber: 1 | BytePositionInLine: 109.");
        }

        [TestMethod]
        public void NamespacedOptionalCount_WritesTheAttributeInItsNamespace()
        {
            var xml = XmlSerializer.Serialize(new NamespacedOptionalCount { Count = 5 }, Bare);
            xml.Should().Be("<NamespacedOptionalCount xmlns:ns1=\"urn:x\" ns1:count=\"5\"/>");
            XmlSerializer.Deserialize<NamespacedOptionalCount>(xml)!.Count.Should().Be(5);
        }

        [TestMethod]
        public void NearShelf_WritesTheNearestDeclaredAncestorTwoLevelsUp()
        {
            var xml = XmlSerializer.Serialize(new NearShelf { Item = new NearAbridgedAudioBook { Title = "x", Author = "Ada" } }, Bare);
            xml.Should().Be("<NearShelf><NearBook><Title>x</Title><Author>Ada</Author></NearBook></NearShelf>");
        }

        [TestMethod]
        public void Node_ObjectOverloads_PreserveReferences()
        {
            var node = new Node { Name = "root" };
            node.Child = node;
            var xml = XmlSerializer.Serialize(node, typeof(Node), Preserve);
            xml.Should().Be($"<Node xmlns:ns1=\"{RefsNamespace}\" ns1:id=\"1\"><Child ns1:ref=\"1\"/><Name>root</Name></Node>");
            var read = (Node)XmlSerializer.Deserialize(xml, typeof(Node), Preserve)!;
            read.Name.Should().Be("root");
            read.Child.Should().BeSameAs(read);
        }

        [TestMethod]
        public void Node_ReferenceId_WithoutAHandler_IsIgnored()
        {
            var read = XmlSerializer.Deserialize<Node>($"<Node xmlns:r=\"{RefsNamespace}\" r:id=\"1\"><Name>root</Name></Node>")!;
            read.Name.Should().Be("root");
            read.Child.Should().BeNull();
        }

        [TestMethod]
        public void Node_ReferenceRef_WithoutAHandler_Throws()
        {
            var xml = $"<Node xmlns:r=\"{RefsNamespace}\"><Child r:ref=\"1\"/><Name>root</Name></Node>";
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Node>(xml))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Unknown reference '1'. Path: /Node/Child | LineNumber: 1 | BytePositionInLine: 65.");
        }

        [TestMethod]
        public void OptionalCount_WritesTheAttributeOnlyWhenItHasAValue()
        {
            XmlSerializer.Serialize(new OptionalCount { Count = 5 }, Bare).Should().Be("<OptionalCount Count=\"5\"/>");
            XmlSerializer.Serialize(new OptionalCount(), Bare).Should().Be("<OptionalCount/>");
            XmlSerializer.Deserialize<OptionalCount>("<OptionalCount Count=\"5\"/>")!.Count.Should().Be(5);
            XmlSerializer.Deserialize<OptionalCount>("<OptionalCount/>")!.Count.Should().BeNull();
        }

        [TestMethod]
        public void Order_ReadsElementsInAnyOrder_AndSkipsUnknownOnes()
        {
            var read = XmlSerializer.Deserialize<Employee>("<Employee><Other>1</Other><Home><City>Paris</City><Zip>75001</Zip><Street>Rue</Street></Home><Name>Ada</Name></Employee>")!;
            read.Name.Should().Be("Ada");
            read.Home!.Street.Should().Be("Rue");
            read.Home.City.Should().Be("Paris");
        }

        [TestMethod]
        public void Paragraph_KeepsAChildNamedLikeTheMixedMember()
        {
            const string xml = "<Paragraph>a<Content>b</Content>c</Paragraph>";
            var read = XmlSerializer.Deserialize<Paragraph>(xml)!;
            read.Content.Should().HaveCount(3);
            read.Content[0].Should().BeOfType<XmlMixedText>().Which.Text.Should().Be("a");
            read.Content[1].Should().BeOfType<XmlMixedElement>().Which.Element!.ToString().Should().Be("<Content>b</Content>");
            read.Content[2].Should().BeOfType<XmlMixedText>().Which.Text.Should().Be("c");
            XmlSerializer.Serialize(read, Bare).Should().Be(xml);
        }

        [TestMethod]
        public void PersonRecord_ReferenceId_WithoutAHandler_IsIgnored()
        {
            var read = XmlSerializer.Deserialize<PersonRecord>($"<PersonRecord xmlns:r=\"{RefsNamespace}\" r:id=\"1\"><Name>Ada</Name><Age>36</Age></PersonRecord>")!;
            read.Should().Be(new PersonRecord("Ada", 36));
        }

        [TestMethod]
        public void Price_SkipsAnUnmappedChildOfSimpleContent()
        {
            var read = XmlSerializer.Deserialize<Price>("<Price Currency=\"USD\">19.99<Note>x</Note></Price>")!;
            read.Amount.Should().Be(19.99m);
            read.Currency.Should().Be("USD");
        }

        [TestMethod]
        public void Price_DisallowUnmapped_RejectsAChildOfSimpleContent()
        {
            var options = new XmlSerializerOptions { UnmappedMemberHandling = XmlUnmappedMemberHandling.Disallow };
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Price>("<Price Currency=\"USD\">19.99<Note>x</Note></Price>", options))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Unknown element 'Note'. Path: /Price | LineNumber: 1 | BytePositionInLine: 34.");
        }

        [TestMethod]
        public void Product_ReadsEveryDeclaredRootName()
        {
            var dvd = XmlSerializer.Deserialize<Product>("<Dvd><Title>The Matrix</Title><Runtime>136</Runtime></Dvd>")!;
            dvd.Should().BeOfType<Dvd>().Which.Runtime.Should().Be(136);
            dvd.Title.Should().Be("The Matrix");
            var plain = XmlSerializer.Deserialize<Product>("<Product><Title>x</Title></Product>")!;
            plain.GetType().Should().Be(typeof(Product));
            plain.Title.Should().Be("x");
        }

        [TestMethod]
        public void ProductCatalog_WritesBaseTypeItemsUnderTheMemberName()
        {
            var catalog = new ProductCatalog
            {
                Items =
                [
                    new Product { Title = "plain" },
                    new Book { Title = "Clean Code", Author = "Robert C. Martin" },
                ],
            };
            var xml = XmlSerializer.Serialize(catalog, Bare);
            xml.Should().Be("<ProductCatalog><Items><Title>plain</Title></Items><Book><Title>Clean Code</Title><Author>Robert C. Martin</Author></Book></ProductCatalog>");
            var read = XmlSerializer.Deserialize<ProductCatalog>(xml)!;
            read.Items.Should().HaveCount(2);
            read.Items[0].GetType().Should().Be(typeof(Product));
            read.Items[0].Title.Should().Be("plain");
            read.Items[1].Should().BeOfType<Book>().Which.Author.Should().Be("Robert C. Martin");
        }

        [TestMethod]
        public void RecordPair_Preserve_RoundTripsOneInstanceCreatedThroughAConstructor()
        {
            var person = new PersonRecord("Ada", 36);
            var xml = XmlSerializer.Serialize(new RecordPair { First = person, Second = person }, Preserve);
            xml.Should().Be($"<RecordPair xmlns:ns1=\"{RefsNamespace}\" ns1:id=\"1\"><First ns1:id=\"2\"><Name>Ada</Name><Age>36</Age></First><Second ns1:ref=\"2\"/></RecordPair>");
            var read = XmlSerializer.Deserialize<RecordPair>(xml, Preserve)!;
            read.First.Should().Be(person);
            read.Second.Should().BeSameAs(read.First);
        }

        [TestMethod]
        public void Shelf_BaseTypeItem_IsWrittenUnderTheMemberName()
        {
            var xml = XmlSerializer.Serialize(new Shelf { Item = new Product { Title = "x" } }, Bare);
            xml.Should().Be("<Shelf><Item><Title>x</Title></Item></Shelf>");
            var read = XmlSerializer.Deserialize<Shelf>(xml)!;
            read.Item!.GetType().Should().Be(typeof(Product));
            read.Item.Title.Should().Be("x");
        }

        [TestMethod]
        public void Shelf_SkipsAnElementThatNamesNoMemberAndNoDerivedType()
        {
            var read = XmlSerializer.Deserialize<Shelf>("<Shelf><Tape><Title>t</Title></Tape><Dvd><Title>The Matrix</Title><Runtime>136</Runtime></Dvd></Shelf>")!;
            read.Item.Should().BeOfType<Dvd>().Which.Runtime.Should().Be(136);
        }

        [TestMethod]
        public void FallbackShelf_UnknownDiscriminator_ReadsTheBaseType()
        {
            var read = XmlSerializer.Deserialize<FallbackShelf>("<FallbackShelf><Item kind=\"tape\"><Title>x</Title></Item></FallbackShelf>")!;
            read.Item!.GetType().Should().Be(typeof(FallbackProduct));
            read.Item.Title.Should().Be("x");
        }

        [TestMethod]
        public void TagSet_RoundTripsASetMember()
        {
            var xml = XmlSerializer.Serialize(new TagSet { Tags = ["a"] }, Bare);
            xml.Should().Be("<TagSet><Tags>a</Tags></TagSet>");
            XmlSerializer.Deserialize<TagSet>("<TagSet><Tags>a</Tags><Tags>b</Tags><Tags>a</Tags></TagSet>")!.Tags.Should().BeEquivalentTo(["a", "b"]);
        }

        [TestMethod]
        [DataRow("<TextMapped>a&amp;b</TextMapped>", "a&b", DisplayName = "one run with a reference")]
        [DataRow("<TextMapped>ab<!--c-->cd</TextMapped>", "abcd", DisplayName = "two runs split by a comment")]
        [DataRow("<TextMapped>ab<?pi x?>cd<![CDATA[<e>]]></TextMapped>", "abcd<e>", DisplayName = "three runs split by a processing instruction and CDATA")]
        [DataRow("<TextMapped>a&lt;b<![CDATA[c]]>d&gt;e</TextMapped>", "a<bcd>e", DisplayName = "runs with references on both sides of CDATA")]
        [DataRow("<TextMapped><![CDATA[]]></TextMapped>", "", DisplayName = "an empty CDATA section")]
        [DataRow("<TextMapped><![CDATA[]]>x</TextMapped>", "x", DisplayName = "an empty CDATA section, then text")]
        public void TextMapped_JoinsSimpleContentThatArrivesInPieces(string xml, string expected)
        {
            var options = new XmlSerializerOptions { ReadCommentHandling = XmlCommentHandling.Allow };
            XmlSerializer.Deserialize<TextMapped>(xml)!.Value.Should().Be(expected);
            XmlSerializer.Deserialize<TextMapped>(xml, options)!.Value.Should().Be(expected);
        }

        [TestMethod]
        public void TextMapped_LongContentInPieces_IsJoinedWhole()
        {
            var first = new string('a', 300);
            var second = new string('b', 900);
            var xml = $"<TextMapped>{first}&amp;<![CDATA[{second}]]>&amp;{first}</TextMapped>";
            XmlSerializer.Deserialize<TextMapped>(xml)!.Value.Should().Be($"{first}&{second}&{first}");
        }

        [TestMethod]
        public void TextMapped_IncompleteReader_ReportsTheElementWasNotClosed()
        {
            FluentActions.Invoking(() =>
            {
                var reader = new Utf8XmlReader("<TextMapped>abc<?pi x?>"u8, isFinalBlock: false, new XmlReaderState());
                XmlSerializer.Deserialize<TextMapped>(ref reader);
            }).Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The element was not closed. Path: /TextMapped | LineNumber: 1 | BytePositionInLine: 24.");
        }

        [TestMethod]
        public void Vehicle_DerivedTypeDeclaredWithoutADiscriminator_UsesTheTypeName()
        {
            var xml = XmlSerializer.Serialize<Vehicle>(new Car { Wheels = 4, Doors = 2 }, Bare);
            xml.Should().Be("<Car><Wheels>4</Wheels><Doors>2</Doors></Car>");
            XmlSerializer.Deserialize<Vehicle>(xml).Should().BeOfType<Car>().Which.Doors.Should().Be(2);
        }

        [TestMethod]
        public void SampleRecord_ReadOneAfterAnother_EachKeepsItsOwnValues()
        {
            var first = XmlSerializer.Deserialize<SampleRecord>("<SampleRecord><Count>1</Count><Total>10000000000</Total><Ratio>0.5</Ratio><Flagged>true</Flagged><Day>Friday</Day><Id>0f8fad5b-d9cb-469f-a165-70867728950e</Id></SampleRecord>");
            var second = XmlSerializer.Deserialize<SampleRecord>("<SampleRecord><Count>2</Count><Total>-7</Total><Ratio>-2.25</Ratio><Flagged>false</Flagged><Day>Monday</Day><Id>7c9e6679-7425-40de-944b-e07fc1f90ae7</Id></SampleRecord>");
            first.Should().Be(new SampleRecord(1, 10000000000L, 0.5, true, DayOfWeek.Friday, Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e")));
            second.Should().Be(new SampleRecord(2, -7L, -2.25, false, DayOfWeek.Monday, Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7")));
        }

        [TestMethod]
        public void SampleRecord_InAList_EveryItemKeepsItsOwnValues()
        {
            var items = new List<SampleRecord>();
            for (var i = 0; i < 40; i++)
            {
                items.Add(new SampleRecord(i, i * 3_000_000_000L, i / 8.0, i % 3 == 0, (DayOfWeek)(i % 7), new Guid(i, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)));
            }

            var read = XmlSerializer.Deserialize<List<SampleRecord>>(XmlSerializer.SerializeToUtf8Bytes(items))!;
            read.Should().Equal(items);
        }

        [TestMethod]
        public void DefaultedRecord_MembersLeftOut_TakeTheirDefaultsEveryTime()
        {
            XmlSerializer.Deserialize<DefaultedRecord>("<DefaultedRecord><Name>a</Name></DefaultedRecord>").Should().Be(new DefaultedRecord("a", 5, 1.5));
            XmlSerializer.Deserialize<DefaultedRecord>("<DefaultedRecord><Name>b</Name><Count>9</Count><Scale>2</Scale></DefaultedRecord>").Should().Be(new DefaultedRecord("b", 9, 2));
            XmlSerializer.Deserialize<DefaultedRecord>("<DefaultedRecord><Name>c</Name></DefaultedRecord>").Should().Be(new DefaultedRecord("c", 5, 1.5));
            XmlSerializer.Deserialize<DefaultedRecord>("<DefaultedRecord><Scale>3</Scale><Name>d</Name></DefaultedRecord>").Should().Be(new DefaultedRecord("d", 5, 3));
        }

        [TestMethod]
        [DataRow("<LinkedRecord><Id>1</Id><Next><Id>2</Id><Next><Id>3</Id></Next></Next></LinkedRecord>", DisplayName = "each number before the link it holds")]
        [DataRow("<LinkedRecord><Next><Next><Id>3</Id></Next><Id>2</Id></Next><Id>1</Id></LinkedRecord>", DisplayName = "each number after the link it holds")]
        public void LinkedRecord_NestedInItself_ReadsEveryLevel(string xml)
        {
            XmlSerializer.Deserialize<LinkedRecord>(xml).Should().Be(new LinkedRecord(1, new LinkedRecord(2, new LinkedRecord(3, null))));
        }

        [TestMethod]
        public void KeptArgument_ConstructorKeepsWhatItWasGiven_TheNextReadDoesNotChangeIt()
        {
            var first = XmlSerializer.Deserialize<KeptArgument>("<KeptArgument><Value>1</Value></KeptArgument>")!;
            var second = XmlSerializer.Deserialize<KeptArgument>("<KeptArgument><Value>2</Value></KeptArgument>")!;
            first.Kept.Should().Be(1);
            first.Value.Should().Be(1);
            second.Kept.Should().Be(2);
            second.Value.Should().Be(2);
        }

        [TestMethod]
        public void GuardedCount_AfterAConstructorThrows_TheNextReadsAreUnaffected()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<GuardedCount>("<GuardedCount><Count>-1</Count><Weight>2.5</Weight></GuardedCount>"))
                .Should().ThrowExactly<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("Count");
            var read = XmlSerializer.Deserialize<GuardedCount>("<GuardedCount><Weight>2.5</Weight><Count>3</Count></GuardedCount>")!;
            read.Count.Should().Be(3);
            read.Weight.Should().Be(2.5);
            var next = XmlSerializer.Deserialize<GuardedCount>("<GuardedCount><Count>4</Count></GuardedCount>")!;
            next.Count.Should().Be(4);
            next.Weight.Should().Be(0);
        }

        [TestMethod]
        public void SampleRecord_ReadOnManyThreadsAtOnce_EveryReadGetsItsOwnValues()
        {
            var wrong = new System.Collections.Concurrent.ConcurrentBag<string>();
            System.Threading.Tasks.Parallel.For(0, 8, thread =>
            {
                for (var i = 0; i < 4000; i++)
                {
                    var expected = new SampleRecord((thread * 100_000) + i, -i, thread + 0.5, i % 2 == 0, (DayOfWeek)(thread % 7), new Guid(thread, 0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)i));
                    var xml = $"<SampleRecord><Count>{expected.Count}</Count><Total>{expected.Total}</Total><Ratio>{expected.Ratio.ToString(System.Globalization.CultureInfo.InvariantCulture)}</Ratio><Flagged>{(expected.Flagged ? "true" : "false")}</Flagged><Day>{expected.Day}</Day><Id>{expected.Id}</Id></SampleRecord>";
                    var read = XmlSerializer.Deserialize<SampleRecord>(xml);
                    if (read != expected)
                    {
                        wrong.Add($"{expected} was read as {read}");
                    }
                }
            });
            wrong.Should().BeEmpty();
        }

        #endregion

    }

}
