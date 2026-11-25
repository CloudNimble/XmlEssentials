using System;
using System.Collections.Generic;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs.Bcl;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// <see cref="XmlSerializerOptions.Bcl"/>, which writes the documents
    /// <c>System.Xml.Serialization.XmlSerializer</c> writes, and each setting it is made of.
    /// </summary>
    /// <remarks>
    /// <see cref="Document"/> was written by that serializer on .NET 11, in a script outside this repository's
    /// runtime and tests, for a type with the same members as <see cref="BclOrder"/> holding the same values.
    /// </remarks>
    [TestClass]
    public class XmlSerializerBclDefaultsTests
    {

        #region Fields

        private const string Document =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<BclOrder xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\n" +
            "  <Field>f</Field>\n" +
            "  <Id>7</Id>\n" +
            "  <Name>a&amp;b</Name>\n" +
            "  <Tags>\n" +
            "    <string>x</string>\n" +
            "    <string>y</string>\n" +
            "  </Tags>\n" +
            "  <Lines>\n" +
            "    <BclLine>\n" +
            "      <Sku>s</Sku>\n" +
            "      <Qty>2</Qty>\n" +
            "    </BclLine>\n" +
            "  </Lines>\n" +
            "  <Empty />\n" +
            "  <Fixed>\n" +
            "    <int>9</int>\n" +
            "  </Fixed>\n" +
            "  <Grid>\n" +
            "    <ArrayOfInt>\n" +
            "      <int>1</int>\n" +
            "    </ArrayOfInt>\n" +
            "  </Grid>\n" +
            "  <Status>Shipped</Status>\n" +
            "  <Initial>97</Initial>\n" +
            "  <Data>AQI=</Data>\n" +
            "  <Count xsi:nil=\"true\" />\n" +
            "  <Limit>5</Limit>\n" +
            "  <Child>\n" +
            "    <Sku>c</Sku>\n" +
            "    <Qty>1</Qty>\n" +
            "  </Child>\n" +
            "  <When>2026-03-14T09:30:00Z</When>\n" +
            "  <Sent>2026-03-14T09:30:00Z</Sent>\n" +
            "  <Price>12.50</Price>\n" +
            "  <Ratio>0.1</Ratio>\n" +
            "  <Active>true</Active>\n" +
            "  <Key>00000000-0000-0000-0000-000000000000</Key>\n" +
            "</BclOrder>";

        private const string Nil = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>";

        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true };

        #endregion

        #region Public Methods

        [TestMethod]
        public void Bcl_WritesTheDocumentTheOldSerializerWrites_WithTheLineEndingOfThePlatform()
        {
            XmlSerializer.Serialize(Order(), XmlSerializerOptions.Bcl).Should().Be(Document.Replace("\n", Environment.NewLine));
        }

        [TestMethod]
        public void Bcl_ReadsTheDocumentTheOldSerializerWrites()
        {
            var read = XmlSerializer.Deserialize<BclOrder>(Document, XmlSerializerOptions.Bcl)!;

            XmlSerializer.Serialize(read, new XmlSerializerOptions(XmlSerializerDefaults.Bcl) { NewLine = "\n" }).Should().Be(Document);
            read.Note.Should().BeNull();
            read.Missing.Should().BeNull();
            read.Count.Should().BeNull();
            read.Fixed.Should().Equal(9);
            read.Empty.Should().BeEmpty();
            read.Initial.Should().Be('a');
            read.Sent.Should().Be(new DateTimeOffset(2026, 3, 14, 9, 30, 0, TimeSpan.Zero));
        }

        [TestMethod]
        public void Bcl_IsTheOptionsItsDocumentationLists_AndCannotBeChanged()
        {
            var options = XmlSerializerOptions.Bcl;

            options.CharHandling.Should().Be(XmlCharHandling.Number);
            options.DefaultCollectionMapping.Should().Be(XmlCollectionMapping.Wrapped);
            options.DefaultIgnoreCondition.Should().Be(XmlIgnoreCondition.WhenWritingNullReference);
            options.EmptyElementStyle.Should().Be(XmlEmptyElementStyle.SelfClosingWithSpace);
            options.IgnoreReadOnlyProperties.Should().BeTrue();
            options.IncludeFields.Should().BeTrue();
            options.MemberOrder.Should().Be(XmlMemberOrder.FieldsFirst);
            options.Namespaces.Should().Equal(
                new KeyValuePair<string, string>("xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                new KeyValuePair<string, string>("xsd", "http://www.w3.org/2001/XMLSchema"));
            options.PreferredObjectCreationHandling.Should().Be(XmlObjectCreationHandling.Populate);
            options.UtcOffsetHandling.Should().Be(XmlUtcOffsetHandling.WriteZ);
            options.WriteIndented.Should().BeTrue();
            options.IsReadOnly.Should().BeTrue();
            FluentActions.Invoking(() => options.WriteIndented = false)
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be("This XmlSerializerOptions instance is read-only.");
        }

        [TestMethod]
        public void Bcl_SettingsAreCopied_AndACopyCanBeChanged()
        {
            var copy = new XmlSerializerOptions(XmlSerializerOptions.Bcl) { WriteIndented = false, OmitXmlDeclaration = true, Namespaces = null };

            copy.CharHandling.Should().Be(XmlCharHandling.Number);
            copy.DefaultCollectionMapping.Should().Be(XmlCollectionMapping.Wrapped);
            copy.MemberOrder.Should().Be(XmlMemberOrder.FieldsFirst);
            copy.UtcOffsetHandling.Should().Be(XmlUtcOffsetHandling.WriteZ);
            XmlSerializer.Serialize(new MixedMembers(), copy)
                .Should().Be("<MixedMembers><Lead>7</Lead><BaseField>2</BaseField><BaseProp>1</BaseProp><F1>4</F1><F2>6</F2><P1>3</P1><P2>5</P2></MixedMembers>");
        }

        [TestMethod]
        public void DefaultCollectionMapping_Wrapped_WrapsACollectionWithNoAttribute_AndReadsItBack()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultCollectionMapping = XmlCollectionMapping.Wrapped, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull };
            var lists = Lists();

            var xml = XmlSerializer.Serialize(lists, options);

            xml.Should().Be(
                "<MappedLists><Counts><a>1</a></Counts><Data>AQI=</Data><w><int>1</int></w><n>2</n><n>3</n>" +
                "<Named><v>4</v></Named><Plain><int>5</int><int>6</int></Plain><t>7</t></MappedLists>");
            XmlSerializer.Serialize(XmlSerializer.Deserialize<MappedLists>(xml, options)!, options).Should().Be(xml);
        }

        [TestMethod]
        public void DefaultCollectionMapping_Unwrapped_RepeatsTheMemberElement_AndReadsItBack()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull };
            var lists = Lists();

            var xml = XmlSerializer.Serialize(lists, options);

            xml.Should().Be(
                "<MappedLists><Counts><a>1</a></Counts><Data>AQI=</Data><w><int>1</int></w><n>2</n><n>3</n>" +
                "<Named>4</Named><Plain>5</Plain><Plain>6</Plain><t>7</t></MappedLists>");
            XmlSerializer.Serialize(XmlSerializer.Deserialize<MappedLists>(xml, options)!, options).Should().Be(xml);
        }

        [TestMethod]
        public void DefaultCollectionMapping_Wrapped_WritesAnEmptyWrapper_AndNilForANullCollection()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultCollectionMapping = XmlCollectionMapping.Wrapped };

            XmlSerializer.Serialize(new MappedLists(), options)
                .Should().Be("<MappedLists><Counts/><Data></Data><w/><Missing" + Nil + "<Named/><Plain/></MappedLists>");
        }

        [TestMethod]
        public void WhenWritingNullReference_LeavesOutANullReference_AndWritesNilForANullableValue()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNullReference };
            var value = new NullableMembers { Items = { "a", null } };

            var xml = XmlSerializer.Serialize(value, options);

            xml.Should().Be(
                "<NullableMembers><Count xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>" +
                "<Items><string>a</string><string xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></Items>" +
                "<MarkedCount xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></NullableMembers>");
            var read = XmlSerializer.Deserialize<NullableMembers>(xml, options)!;
            read.Count.Should().BeNull();
            read.Items.Should().Equal("a", null);
        }

        [TestMethod]
        public void WhenWritingNullReference_OnAMember_AppliesToThatMemberOnly()
        {
            XmlSerializer.Serialize(new NullableMembers(), Bare).Should().Be(
                "<NullableMembers><Child" + Nil + "<Count" + Nil + "<Items/><MarkedCount" + Nil + "<Text" + Nil + "</NullableMembers>");
        }

        [TestMethod]
        public void WhenWritingNullReference_WritesAValueThatIsNotNull()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNullReference };
            var value = new NullableMembers { Child = new BclLine { Sku = "s", Qty = 1 }, Count = 0, Mark = 2, MarkedCount = 3, MarkedText = "m", Text = "" };

            XmlSerializer.Serialize(value, options).Should().Be(
                "<NullableMembers Mark=\"2\"><Child><Sku>s</Sku><Qty>1</Qty></Child><Count>0</Count><Items/><MarkedCount>3</MarkedCount><MarkedText>m</MarkedText><Text></Text></NullableMembers>");
        }

        [TestMethod]
        public void CharHandling_Number_WritesAndReadsTheCodeUnit_InAnElementAnAttributeAndAnItem()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, CharHandling = XmlCharHandling.Number, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull };
            var value = new TypedValues { Letter = 'a', Letters = { 'b', '\0', '￿' }, Mark = ' ', Maybe = 'é' };

            var xml = XmlSerializer.Serialize(value, options);

            xml.Should().Be(
                "<TypedValues Mark=\"32\" Stamp=\"0001-01-01T00:00:00+00:00\"><Away>0001-01-01T00:00:00+00:00</Away><Letter>97</Letter>" +
                "<Letters><char>98</char><char>0</char><char>65535</char></Letters><Maybe>233</Maybe><Sent>0001-01-01T00:00:00+00:00</Sent><Times/><When>0001-01-01T00:00:00</When></TypedValues>");
            var read = XmlSerializer.Deserialize<TypedValues>(xml, options)!;
            read.Letter.Should().Be('a');
            read.Letters.Should().Equal('b', '\0', '￿');
            read.Mark.Should().Be(' ');
            read.Maybe.Should().Be('é');
        }

        [TestMethod]
        [DataRow("a")]
        [DataRow("65536")]
        [DataRow("-1")]
        [DataRow("")]
        public void CharHandling_Number_RefusesAValueThatIsNotTheNumberOfACharacter(string text)
        {
            var options = new XmlSerializerOptions { CharHandling = XmlCharHandling.Number };

            FluentActions.Invoking(() => XmlSerializer.Deserialize<TypedValues>("<TypedValues><Letter>" + text + "</Letter></TypedValues>", options))
                .Should().ThrowExactly<XmlException>();
        }

        [TestMethod]
        public void CharHandling_Character_IsTheDefault_AndDoesNotReadANumberAsACodeUnit()
        {
            XmlSerializer.Deserialize<TypedValues>("<TypedValues><Letter>9</Letter></TypedValues>")!.Letter.Should().Be('9');
            FluentActions.Invoking(() => XmlSerializer.Deserialize<TypedValues>("<TypedValues><Letter>97</Letter></TypedValues>"))
                .Should().ThrowExactly<XmlException>();
        }

        [TestMethod]
        public void UtcOffsetHandling_WriteZ_WritesZForAZeroOffsetOnly_AndBothFormsAreRead()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, UtcOffsetHandling = XmlUtcOffsetHandling.WriteZ, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull };
            var zero = new DateTimeOffset(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);
            var away = new DateTimeOffset(2026, 3, 14, 9, 30, 0, TimeSpan.FromHours(2)).AddTicks(1234567);
            var value = new TypedValues
            {
                Away = away,
                Letter = 'a',
                Mark = 'b',
                Sent = zero.AddTicks(5000000),
                Stamp = zero,
                Times = { zero, away },
                When = new DateTime(2026, 3, 14, 9, 30, 0, DateTimeKind.Utc),
            };

            var xml = XmlSerializer.Serialize(value, options);

            xml.Should().Be(
                "<TypedValues Mark=\"b\" Stamp=\"2026-03-14T09:30:00Z\"><Away>2026-03-14T09:30:00.1234567+02:00</Away><Letter>a</Letter><Letters/>" +
                "<Sent>2026-03-14T09:30:00.5Z</Sent><Times><dateTimeOffset>2026-03-14T09:30:00Z</dateTimeOffset><dateTimeOffset>2026-03-14T09:30:00.1234567+02:00</dateTimeOffset></Times>" +
                "<When>2026-03-14T09:30:00Z</When></TypedValues>");
            foreach (var reader in new[] { options, Bare })
            {
                var read = XmlSerializer.Deserialize<TypedValues>(xml, reader)!;
                read.Stamp.Should().Be(zero);
                read.Stamp.Offset.Should().Be(TimeSpan.Zero);
                read.Sent.Should().Be(zero.AddTicks(5000000));
                read.Away.Offset.Should().Be(TimeSpan.FromHours(2));
                read.Times.Should().Equal(zero, away);
            }

            XmlSerializer.Deserialize<TypedValues>("<TypedValues Stamp=\"2026-03-14T09:30:00+00:00\"><Letter>a</Letter></TypedValues>", options)!.Stamp.Should().Be(zero);
        }

        [TestMethod]
        public void MemberOrder_FieldsFirst_PutsTheFieldsOfEachClassBeforeItsProperties()
        {
            var first = new XmlSerializerOptions { OmitXmlDeclaration = true, IncludeFields = true, MemberOrder = XmlMemberOrder.FieldsFirst };
            var last = new XmlSerializerOptions { OmitXmlDeclaration = true, IncludeFields = true };

            var xml = XmlSerializer.Serialize(new MixedMembers(), first);

            xml.Should().Be("<MixedMembers><Lead>7</Lead><BaseField>2</BaseField><BaseProp>1</BaseProp><F1>4</F1><F2>6</F2><P1>3</P1><P2>5</P2></MixedMembers>");
            XmlSerializer.Serialize(new MixedMembers(), last)
                .Should().Be("<MixedMembers><Lead>7</Lead><BaseProp>1</BaseProp><BaseField>2</BaseField><P1>3</P1><P2>5</P2><F1>4</F1><F2>6</F2></MixedMembers>");
            XmlSerializer.Serialize(XmlSerializer.Deserialize<MixedMembers>(xml, last)!, first).Should().Be(xml, "the order of the elements does not decide what is read");
        }

        [TestMethod]
        public void MemberOrder_FieldsFirst_ChangesNothingWhenFieldsAreNotIncluded()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, MemberOrder = XmlMemberOrder.FieldsFirst };

            XmlSerializer.Serialize(new MixedMembers(), options)
                .Should().Be("<MixedMembers><Lead>7</Lead><BaseProp>1</BaseProp><P1>3</P1><P2>5</P2></MixedMembers>");
        }

        #endregion

        #region Private Methods

        private static MappedLists Lists()
        {
            return new MappedLists
            {
                Counts = { ["a"] = 1 },
                Data = [1, 2],
                Explicit = { 1 },
                Flat = { 2, 3 },
                Named = { 4 },
                Plain = { 5, 6 },
                Typed = { 7 },
            };
        }

        private static BclOrder Order()
        {
            var order = new BclOrder
            {
                Active = true,
                Child = new BclLine { Sku = "c", Qty = 1 },
                Data = [1, 2],
                Field = "f",
                Id = 7,
                Initial = 'a',
                Key = Guid.Empty,
                Limit = 5,
                Lines = { new BclLine { Sku = "s", Qty = 2 } },
                Name = "a&b",
                Price = 12.50m,
                Ratio = 0.1,
                Sent = new DateTimeOffset(2026, 3, 14, 9, 30, 0, TimeSpan.Zero),
                Status = BclStatus.Shipped,
                Tags = { "x", "y" },
                When = new DateTime(2026, 3, 14, 9, 30, 0, DateTimeKind.Utc),
            };
            order.Fixed.Add(9);
            return order;
        }

        #endregion

    }

}
