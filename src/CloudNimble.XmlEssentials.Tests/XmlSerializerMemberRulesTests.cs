using System;
using System.Collections.Generic;
using System.Linq;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Rules for single members and for the document element: ignored constructor parameters, null wrapped
    /// collections, CDATA that holds its own terminator, sibling derived types, and option values that are rejected.
    /// </summary>
    [TestClass]
    public class XmlSerializerMemberRulesTests
    {

        #region Fields

        private const string Nil = "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"";
        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true };

        #endregion

        #region Public Methods

        [TestMethod]
        public void SetterThatIsNotPublic_IsNotUsed_UnlessTheMemberIsIncluded()
        {
            // System.Text.Json has the same rule, with [JsonInclude].
            const string document =
                "<SetterAccess><Included>a</Included><Init>b</Init><Internal>c</Internal><Private>d</Private><PrivateInit>e</PrivateInit><Public>f</Public></SetterAccess>";

            var read = XmlSerializer.Deserialize<SetterAccess>(document)!;

            read.Included.Should().Be("a");
            read.Init.Should().Be("b");
            read.Internal.Should().Be("-");
            read.Private.Should().Be("-");
            read.PrivateInit.Should().Be("-");
            read.Public.Should().Be("f");

            // Every one of them is written: a property is written from its getter.
            XmlSerializer.Serialize(new SetterAccess(), new XmlSerializerOptions { OmitXmlDeclaration = true }).Should().Be(
                "<SetterAccess><Included>-</Included><Init>-</Init><Internal>-</Internal><Private>-</Private><PrivateInit>-</PrivateInit><Public>-</Public></SetterAccess>");

            // A property whose setter is not used is read-only, and is left out with the other read-only properties.
            XmlSerializer.Serialize(new SetterAccess(), new XmlSerializerOptions { OmitXmlDeclaration = true, IgnoreReadOnlyProperties = true }).Should().Be(
                "<SetterAccess><Included>-</Included><Init>-</Init><Public>-</Public></SetterAccess>");
        }

        [TestMethod]
        [DataRow("a]]>b", "<CDataMapped><![CDATA[a]]]]><![CDATA[>b]]></CDataMapped>", DisplayName = "in the middle")]
        [DataRow("]]>", "<CDataMapped><![CDATA[]]]]><![CDATA[>]]></CDataMapped>", DisplayName = "alone")]
        [DataRow("]]>]]>", "<CDataMapped><![CDATA[]]]]><![CDATA[>]]]]><![CDATA[>]]></CDataMapped>", DisplayName = "twice")]
        [DataRow("a]]", "<CDataMapped><![CDATA[a]]]]></CDataMapped>", DisplayName = "brackets only")]
        [DataRow("a]>b", "<CDataMapped><![CDATA[a]>b]]></CDataMapped>", DisplayName = "one bracket")]
        [DataRow("", "<CDataMapped><![CDATA[]]></CDataMapped>", DisplayName = "empty")]
        public void CData_ThatHoldsItsTerminator_IsSplitIntoSections(string value, string expected)
        {
            var xml = XmlSerializer.Serialize(new CDataMapped { Value = value }, Bare);
            xml.Should().Be(expected);
            XmlSerializer.Deserialize<CDataMapped>(xml)!.Value.Should().Be(value);
        }

        [TestMethod]
        public void ConstructorParameter_WhosePropertyIsIgnored_TakesItsDefault()
        {
            var xml = XmlSerializer.Serialize(new IgnoredCtorRecord("Ada", 7, "hidden"), Bare);
            xml.Should().Be("<IgnoredCtorRecord><Name>Ada</Name></IgnoredCtorRecord>");
            XmlSerializer.Deserialize<IgnoredCtorRecord>(xml).Should().Be(new IgnoredCtorRecord("Ada", 0, null));
            XmlSerializer.Deserialize<IgnoredCtorRecord>("<IgnoredCtorRecord><Name>Ada</Name><Secret>9</Secret><Note>n</Note></IgnoredCtorRecord>")
                .Should().Be(new IgnoredCtorRecord("Ada", 0, null), "an ignored member is not read either");
        }

        [TestMethod]
        public void DefaultIgnoreCondition_Always_IsRejected()
        {
            var options = new XmlSerializerOptions();
            FluentActions.Invoking(() => options.DefaultIgnoreCondition = XmlIgnoreCondition.Always)
                .Should().ThrowExactly<ArgumentException>().Which.Message.Should().Be(
                    "DefaultIgnoreCondition cannot be Always, which would ignore every member. Use [XmlIgnore] on the members to leave out. (Parameter 'value')");
            options.DefaultIgnoreCondition.Should().Be(XmlIgnoreCondition.Never);
            options.DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull;
            options.DefaultIgnoreCondition.Should().Be(XmlIgnoreCondition.WhenWritingNull);
            options.DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingDefault;
            options.DefaultIgnoreCondition.Should().Be(XmlIgnoreCondition.WhenWritingDefault);
        }

        [TestMethod]
        public void DictionaryKeyPolicy_AppliesWhenWritingOnly()
        {
            // A policy cannot be undone, so a key is read as it is written in the document, as in System.Text.Json.
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DictionaryKeyPolicy = XmlNamingPolicy.CamelCase };
            var xml = XmlSerializer.Serialize(new Dictionary<string, int> { ["MyKey"] = 1 }, options);
            xml.Should().Be("<DictionaryOfStringInt32><myKey>1</myKey></DictionaryOfStringInt32>");
            XmlSerializer.Deserialize<Dictionary<string, int>>(xml, options).Should().Equal(new Dictionary<string, int> { ["myKey"] = 1 });
        }

        [TestMethod]
        public void DocumentElement_IsMatchedWithoutCaseWhenPropertyNamesAre()
        {
            var loose = new XmlSerializerOptions { PropertyNameCaseInsensitive = true };
            XmlSerializer.Deserialize<Person>("<person><name>Ada</name><AGE>3</AGE></person>", loose).Should().BeEquivalentTo(new Person { Name = "Ada", Age = 3 });
            XmlSerializer.Deserialize<Person>("<PERSON/>", loose).Should().BeEquivalentTo(new Person());
            var product = XmlSerializer.Deserialize<Product>("<BOOK><Title>x</Title><Author>Ada</Author></BOOK>", loose);
            product.Should().BeOfType<Book>().Which.Author.Should().Be("Ada");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Person>("<person/>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The root element does not match 'Person'. Path: /Person | LineNumber: 1 | BytePositionInLine: 10.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Person>("<People/>", loose))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The root element does not match 'Person'. Path: /Person | LineNumber: 1 | BytePositionInLine: 10.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Product>("<book><Title>x</Title></book>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The root element does not match 'Product'. Path: /Product | LineNumber: 1 | BytePositionInLine: 7.");
        }

        [TestMethod]
        public void NamesOutsideAscii_MatchExactlyWhenCaseIsIgnored()
        {
            // Ignoring case is an ASCII rule. A name that holds other characters still matches itself.
            var loose = new XmlSerializerOptions { OmitXmlDeclaration = true, PropertyNameCaseInsensitive = true };
            var xml = XmlSerializer.Serialize(new CafeOrder { Cream = "x", Sugar = "y" }, loose);
            xml.Should().Be("<Café><Crème>x</Crème><Sugar>y</Sugar></Café>");
            XmlSerializer.Deserialize<CafeOrder>(xml, loose).Should().BeEquivalentTo(new CafeOrder { Cream = "x", Sugar = "y" });
            XmlSerializer.Deserialize<CafeOrder>(xml).Should().BeEquivalentTo(new CafeOrder { Cream = "x", Sugar = "y" });
            XmlSerializer.Deserialize<CafeOrder>("<Café><crème>x</crème><SUGAR>y</SUGAR></Café>", loose).Should().BeEquivalentTo(new CafeOrder { Sugar = "y" });
            FluentActions.Invoking(() => XmlSerializer.Deserialize<CafeOrder>("<café/>", loose))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The root element does not match 'Café'. Path: /Café | LineNumber: 1 | BytePositionInLine: 9.");
        }

        [TestMethod]
        public void MetadataAttributes_AreAcceptedInAnyPosition()
        {
            // XML 1.0 section 3.1: the order of attribute specifications in a start-tag is not significant. A
            // discriminator, a reference id and a reference are attributes, so none of them has to come first.
            foreach (var xml in new[]
            {
                "<FallbackProduct kind=\"book\" other=\"1\"><Title>t</Title><Author>a</Author></FallbackProduct>",
                "<FallbackProduct other=\"1\" kind=\"book\"><Title>t</Title><Author>a</Author></FallbackProduct>",
            })
            {
                XmlSerializer.Deserialize<FallbackProduct>(xml).Should().BeOfType<FallbackBook>().Which.Author.Should().Be("a");
            }

            var preserve = new XmlSerializerOptions { ReferenceHandler = XmlReferenceHandler.Preserve, DefaultPropertyMapping = XmlPropertyMapping.Attribute };
            foreach (var xml in new[]
            {
                "<Node xmlns:r=\"urn:CloudNimble.Text.Xml:refs\" r:id=\"1\" Name=\"root\"><Child r:ref=\"1\" Name=\"ignored\"/></Node>",
                "<Node xmlns:r=\"urn:CloudNimble.Text.Xml:refs\" Name=\"root\" r:id=\"1\"><Child Name=\"ignored\" r:ref=\"1\"/></Node>",
            })
            {
                var read = XmlSerializer.Deserialize<Node>(xml, preserve)!;
                read.Name.Should().Be("root");
                read.Child.Should().BeSameAs(read);
            }
        }

        [TestMethod]
        public void NullWrappedCollection_IsNil_UnlessTheIgnoreConditionLeavesItOut()
        {
            XmlSerializer.Serialize(new Team(), Bare).Should().Be("<Team><Members " + Nil + "/></Team>");
            XmlSerializer.Deserialize<Team>("<Team><Members " + Nil + "/></Team>")!.Members.Should().BeNull();

            var whenNull = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull };
            XmlSerializer.Serialize(new Team(), whenNull).Should().Be("<Team/>");
            XmlSerializer.Deserialize<Team>("<Team/>", whenNull)!.Members.Should().BeNull();
            XmlSerializer.Serialize(new Team { Members = [] }, whenNull).Should().Be("<Team><Members/></Team>", "an empty collection is not a null one");
            var whenDefault = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingDefault };
            XmlSerializer.Serialize(new Team(), whenDefault).Should().Be("<Team/>");

            XmlSerializer.Serialize(new TeamIgnores(), Bare).Should().Be("<TeamIgnores><Always " + Nil + "/></TeamIgnores>");
            XmlSerializer.Serialize(new TeamIgnores(), whenDefault).Should().Be("<TeamIgnores><Always " + Nil + "/></TeamIgnores>", "the attribute on a member wins over the option");
        }

        [TestMethod]
        public void SiblingType_InTheDocument_IsAnXmlError()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Book>("<Dvd><Title>x</Title></Dvd>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(
                    "The element identifies the type 'CloudNimble.XmlEssentials.Tests.POCOs.Dvd', which is not a 'CloudNimble.XmlEssentials.Tests.POCOs.Book'. Path: /Book | LineNumber: 1 | BytePositionInLine: 6.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<KindBook>("<KindBook kind=\"dvd\"><Title>x</Title></KindBook>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(
                    "The element identifies the type 'CloudNimble.XmlEssentials.Tests.POCOs.KindDvd', which is not a 'CloudNimble.XmlEssentials.Tests.POCOs.KindBook'. Path: /KindBook | LineNumber: 1 | BytePositionInLine: 22.");
            XmlSerializer.Deserialize<Book>("<Book><Title>x</Title><Author>Ada</Author></Book>")!.Author.Should().Be("Ada");
            XmlSerializer.Deserialize<KindBook>("<KindBook kind=\"book\"><Title>x</Title></KindBook>")!.Title.Should().Be("x");
            XmlSerializer.Deserialize<KindBook>("<KindBook><Title>y</Title></KindBook>")!.Title.Should().Be("y");
        }

        [TestMethod]
        public void TypedElementAttribute_SetsOrderAndNillability()
        {
            var info = Bare.GetTypeInfo<TypedElementFacets>();
            info.Properties.Select(property => property.Name.LocalName + ":" + property.Order + ":" + property.IsNillable)
                .Should().Equal("First:0:False", "Count:1:True", "Last:2:True");
            var xml = XmlSerializer.Serialize(new TypedElementFacets { First = 1, Count = 2, Last = new Person { Name = "Ada", Age = 3 } }, Bare);
            xml.Should().Be("<TypedElementFacets><First>1</First><Count>2</Count><Last><Name>Ada</Name><Age>3</Age></Last></TypedElementFacets>");
        }

        [TestMethod]
        public void UnknownDiscriminator_OnRead_FallsBackToTheDeclaredTypeOnly()
        {
            // A discriminator names one type. When it is unknown there is no ancestor to find, so both
            // fall-back settings read the declared type, and the default setting fails.
            var near = XmlSerializer.Deserialize<NearProduct>("<NearProduct><Title>x</Title></NearProduct>")!;
            near.Should().BeOfType<NearProduct>().Which.Title.Should().Be("x");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<NearProduct>("<NearSpecialBook><Title>x</Title></NearSpecialBook>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The root element does not match 'NearProduct'. Path: /NearProduct | LineNumber: 1 | BytePositionInLine: 18.");
            XmlSerializer.Deserialize<FallbackProduct>("<FallbackProduct kind=\"tape\"><Title>x</Title></FallbackProduct>")
                .Should().BeOfType<FallbackProduct>();
            FluentActions.Invoking(() => XmlSerializer.Deserialize<KindProduct>("<KindProduct kind=\"tape\"><Title>x</Title></KindProduct>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Unknown type discriminator 'tape'. Path: /KindProduct | LineNumber: 1 | BytePositionInLine: 26.");
        }

        #endregion

    }

}
