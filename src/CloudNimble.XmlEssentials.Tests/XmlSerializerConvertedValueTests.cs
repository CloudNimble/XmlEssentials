using System;
using System.Collections.Generic;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A type with a converter the caller supplied can be an attribute or the text of an element, and a
    /// member can name the converter that reads and writes it.
    /// </summary>
    [TestClass]
    public class XmlSerializerConvertedValueTests
    {

        #region Fields

        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true };

        #endregion

        #region Public Methods

        [TestMethod]
        public void ConvertedType_AsAttributesAndText_RoundTrips()
        {
            var value = new Priced { Price = new Money(12.5m, "USD"), Sale = new Money(9m, "USD"), Total = new Money(21.5m, "USD") };
            var xml = XmlSerializer.Serialize(value, Bare);
            xml.Should().Be("<Priced Price=\"12.5 USD\" Sale=\"9 USD\">21.5 USD</Priced>");
            XmlSerializer.Deserialize<Priced>(xml).Should().BeEquivalentTo(value);

            var withoutSale = XmlSerializer.Serialize(new Priced { Price = new Money(1m, "EUR"), Total = new Money(2m, "EUR") }, Bare);
            withoutSale.Should().Be("<Priced Price=\"1 EUR\">2 EUR</Priced>");
            XmlSerializer.Deserialize<Priced>(withoutSale)!.Sale.Should().BeNull();
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Priced>("<Priced Price=\"cheap\"/>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not an amount and a currency.");
        }

        [TestMethod]
        public void ConvertedType_RegisteredInOptions_CanBeAnAttributeAndText()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            options.Converters.Add(new WeightConverter());
            var xml = XmlSerializer.Serialize(new Parcel { Tare = new Weight(5), Gross = new Weight(120) }, options);
            xml.Should().Be("<Parcel Tare=\"5g\">120g</Parcel>");
            XmlSerializer.Deserialize<Parcel>(xml, options).Should().BeEquivalentTo(new Parcel { Tare = new Weight(5), Gross = new Weight(120) });

            // Without the converter the type is an object with members, which cannot be one value.
            FluentActions.Invoking(() => XmlSerializer.Serialize(new Parcel(), Bare))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Member 'CloudNimble.XmlEssentials.Tests.POCOs.Parcel.Tare' is mapped as an XML attribute. Complex types cannot be attributes.");
        }

        [TestMethod]
        public void ConverterThatWritesElementsOnly_AsAnAttribute_SaysSo()
        {
            FluentActions.Invoking(() => XmlSerializer.Serialize(new EnvelopeAttribute { Letter = new Envelope { Content = 1 } }, Bare))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(
                    "Converter 'CloudNimble.XmlEssentials.Tests.EnvelopeConverter' does not support attributes.");
        }

        [TestMethod]
        public void MemberConverter_ReadsAndWritesThatMemberOnly()
        {
            var value = new MemberConverted { Flags = 255, Plain = 255, Hex = 255, Maybe = 16, Joined = [1, 2, 3] };
            var xml = XmlSerializer.Serialize(value, new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull });
            xml.Should().Be("<MemberConverted Flags=\"ff\"><Plain>255</Plain><Hex>ff</Hex><Maybe>10</Maybe><Joined>1,2,3</Joined></MemberConverted>");
            var read = XmlSerializer.Deserialize<MemberConverted>(xml)!;
            read.Flags.Should().Be(255);
            read.Plain.Should().Be(255);
            read.Hex.Should().Be(255);
            read.Maybe.Should().Be(16);
            read.Joined.Should().Equal(1, 2, 3);

            var nil = XmlSerializer.Serialize(new MemberConverted(), Bare);
            nil.Should().Be(
                "<MemberConverted Flags=\"0\"><Plain>0</Plain><Hex>0</Hex><Maybe xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>" +
                "<Joined xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></MemberConverted>");
            var empty = XmlSerializer.Deserialize<MemberConverted>(nil)!;
            empty.Maybe.Should().BeNull();
            empty.Joined.Should().BeNull();
        }

        [TestMethod]
        public void ConverterAttribute_NamesItsConverterAsATypeArgument()
        {
            XmlConverterAttribute onType = new XmlConverterAttribute<MoneyConverter>();
            onType.ConverterType.Should().Be(typeof(MoneyConverter));
            var declared = (XmlConverterAttribute)Attribute.GetCustomAttribute(typeof(Money), typeof(XmlConverterAttribute))!;
            declared.Should().BeOfType<XmlConverterAttribute<MoneyConverter>>();
            declared.ConverterType.Should().Be(typeof(MoneyConverter));
        }

        [TestMethod]
        public void MemberConverter_ThatDoesNotFitTheMember_IsRejected()
        {
            FluentActions.Invoking(() => XmlSerializer.Serialize(new WrongMemberConverter { Name = "n" }, Bare))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(
                    "The converter 'CloudNimble.XmlEssentials.Tests.HexInt32Converter' is not compatible with the type 'System.String'.");
        }

        #endregion

    }

}
