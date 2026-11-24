using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A value whose declared type is itself a derived type. A derived type inherits the list its base declares,
    /// but a value of the declared type is written as itself, with no discriminator and under the name of its
    /// member, as <c>System.Xml.Serialization</c> writes it. Only a type below the declared type is identified.
    /// </summary>
    [TestClass]
    public class XmlSerializerDeclaredDerivedTypeTests
    {

        #region Fields

        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true };
        private const string Xsi = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"";

        #endregion

        #region Public Methods

        [TestMethod]
        public void DeclaredType_AsTheDocumentElement_HasNoDiscriminator()
        {
            XmlSerializer.Serialize(new Book { Title = "t", Author = "a" }, Bare)
                .Should().Be("<Book><Title>t</Title><Author>a</Author></Book>");
            XmlSerializer.Serialize(new KindBook { Title = "t", Author = "a" }, Bare)
                .Should().Be("<KindBook><Title>t</Title><Author>a</Author></KindBook>");
            XmlSerializer.Serialize(new XsiBook { Title = "t", Author = "a" }, Bare)
                .Should().Be("<XsiBook><Title>t</Title><Author>a</Author></XsiBook>");
        }

        [TestMethod]
        public void DeclaredType_OfAMemberAndOfItems_IsWrittenUnderTheirNames_AndReadBack()
        {
            // Two members of one derived type, one of them a collection with no wrapper, are two element names.
            var books = new Declared<Book>
            {
                Flat = [new Book { Title = "f", Author = "a" }],
                One = new Book { Title = "o", Author = "a" },
                Wrapped = [new Book { Title = "w", Author = "a" }],
            };
            var xml = XmlSerializer.Serialize(books, Bare);
            xml.Should().Be(
                "<DeclaredOfBook><Flat><Title>f</Title><Author>a</Author></Flat><One><Title>o</Title><Author>a</Author></One>" +
                "<Wrapped><Book><Title>w</Title><Author>a</Author></Book></Wrapped></DeclaredOfBook>");
            XmlSerializer.Serialize(XmlSerializer.Deserialize<Declared<Book>>(xml, Bare), Bare).Should().Be(xml);

            var kinds = new Declared<KindBook>
            {
                Flat = [new KindBook { Title = "f", Author = "a" }],
                One = new KindBook { Title = "o", Author = "a" },
                Wrapped = [new KindBook { Title = "w", Author = "a" }],
            };
            xml = XmlSerializer.Serialize(kinds, Bare);
            xml.Should().Be(
                "<DeclaredOfKindBook><Flat><Title>f</Title><Author>a</Author></Flat><One><Title>o</Title><Author>a</Author></One>" +
                "<Wrapped><KindBook><Title>w</Title><Author>a</Author></KindBook></Wrapped></DeclaredOfKindBook>");
            XmlSerializer.Serialize(XmlSerializer.Deserialize<Declared<KindBook>>(xml, Bare), Bare).Should().Be(xml);

            var typed = new Declared<XsiBook>
            {
                Flat = [new XsiBook { Title = "f", Author = "a" }],
                One = new XsiBook { Title = "o", Author = "a" },
                Wrapped = [new XsiBook { Title = "w", Author = "a" }],
            };
            xml = XmlSerializer.Serialize(typed, Bare);
            xml.Should().Be(
                "<DeclaredOfXsiBook><Flat><Title>f</Title><Author>a</Author></Flat><One><Title>o</Title><Author>a</Author></One>" +
                "<Wrapped><XsiBook><Title>w</Title><Author>a</Author></XsiBook></Wrapped></DeclaredOfXsiBook>");
            XmlSerializer.Serialize(XmlSerializer.Deserialize<Declared<XsiBook>>(xml, Bare), Bare).Should().Be(xml);
        }

        [TestMethod]
        public void Document_ThatIdentifiesTheDeclaredType_IsReadAsIt()
        {
            // A document written through the base type names the derived type. It is still that type.
            XmlSerializer.Deserialize<XsiBook>("<XsiBook" + Xsi + " xsi:type=\"Book\"><Title>t</Title></XsiBook>", Bare)!.Title.Should().Be("t");
            XmlSerializer.Deserialize<Declared<KindBook>>("<DeclaredOfKindBook><One kind=\"book\"><Title>t</Title></One></DeclaredOfKindBook>", Bare)!.One!.Title.Should().Be("t");
            XmlSerializer.Deserialize<Declared<XsiBook>>("<DeclaredOfXsiBook><One" + Xsi + " xsi:type=\"Book\"><Title>t</Title></One></DeclaredOfXsiBook>", Bare)!.One!.Title.Should().Be("t");
            XmlSerializer.Deserialize<Declared<Book>>("<DeclaredOfBook><Wrapped><Book><Title>t</Title></Book></Wrapped></DeclaredOfBook>", Bare)!.Wrapped![0].Title.Should().Be("t");
        }

        [TestMethod]
        public void Document_ThatIdentifiesATypeBesideTheDeclaredType_IsRefused()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Declared<KindBook>>("<DeclaredOfKindBook><One kind=\"dvd\"><Title>t</Title></One></DeclaredOfKindBook>", Bare))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(
                    "The element identifies the type 'CloudNimble.XmlEssentials.Tests.POCOs.KindDvd', which is not a 'CloudNimble.XmlEssentials.Tests.POCOs.KindBook'. Path: /DeclaredOfKindBook/One | LineNumber: 1 | BytePositionInLine: 37.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Declared<Book>>("<DeclaredOfBook><Wrapped><Dvd><Title>t</Title></Dvd></Wrapped></DeclaredOfBook>", Bare))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(
                    "The element identifies the type 'CloudNimble.XmlEssentials.Tests.POCOs.Dvd', which is not a 'CloudNimble.XmlEssentials.Tests.POCOs.Book'. Path: /DeclaredOfBook/Wrapped/Book[0] | LineNumber: 1 | BytePositionInLine: 31.");
        }

        [TestMethod]
        public void ElementNamedForATypeBesideTheDeclaredType_IsNotAnElementOfTheMember()
        {
            // A member declared as Book claims no element named Dvd, so the element is unknown to the type.
            var strict = new XmlSerializerOptions { UnmappedMemberHandling = XmlUnmappedMemberHandling.Disallow };
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Declared<Book>>("<DeclaredOfBook><Dvd><Title>t</Title></Dvd></DeclaredOfBook>", strict))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(
                    "Unknown element 'Dvd'. Path: /DeclaredOfBook | LineNumber: 1 | BytePositionInLine: 22.");
            var read = XmlSerializer.Deserialize<Declared<Book>>("<DeclaredOfBook><Dvd><Title>t</Title></Dvd></DeclaredOfBook>", Bare)!;
            read.One.Should().BeNull();
            read.Flat.Should().BeNull();
        }

        [TestMethod]
        public void TypeBelowTheDeclaredType_IsWrittenWithItsDiscriminator_AndReadBack()
        {
            // The list is inherited, so a type listed on the top type only is still identified.
            var audio = new TierAudioBook { Title = "t", Author = "a", Minutes = 5 };
            var xml = XmlSerializer.Serialize<TierBook>(audio, Bare);
            xml.Should().Be("<TierBook kind=\"audio\"><Title>t</Title><Author>a</Author><Minutes>5</Minutes></TierBook>");
            XmlSerializer.Deserialize<TierBook>(xml, Bare).Should().BeOfType<TierAudioBook>().Which.Minutes.Should().Be(5);
            XmlSerializer.Serialize(new TierBook { Title = "t", Author = "a" }, Bare)
                .Should().Be("<TierBook><Title>t</Title><Author>a</Author></TierBook>");

            var held = new Declared<TierBook> { Flat = [audio], One = audio, Wrapped = [audio, new TierBook { Title = "b", Author = "a" }] };
            xml = XmlSerializer.Serialize(held, Bare);
            xml.Should().Be(
                "<DeclaredOfTierBook><Flat kind=\"audio\"><Title>t</Title><Author>a</Author><Minutes>5</Minutes></Flat>" +
                "<One kind=\"audio\"><Title>t</Title><Author>a</Author><Minutes>5</Minutes></One>" +
                "<Wrapped><TierBook kind=\"audio\"><Title>t</Title><Author>a</Author><Minutes>5</Minutes></TierBook>" +
                "<TierBook><Title>b</Title><Author>a</Author></TierBook></Wrapped></DeclaredOfTierBook>");
            var back = XmlSerializer.Deserialize<Declared<TierBook>>(xml, Bare)!;
            back.One.Should().BeOfType<TierAudioBook>();
            back.Flat![0].Should().BeOfType<TierAudioBook>();
            back.Wrapped![0].Should().BeOfType<TierAudioBook>();
            back.Wrapped[1].Should().BeOfType<TierBook>();

            FluentActions.Invoking(() => XmlSerializer.Deserialize<TierBook>("<TierBook kind=\"dvd\"><Title>t</Title></TierBook>", Bare))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(
                    "The element identifies the type 'CloudNimble.XmlEssentials.Tests.POCOs.TierDvd', which is not a 'CloudNimble.XmlEssentials.Tests.POCOs.TierBook'. Path: /TierBook | LineNumber: 1 | BytePositionInLine: 22.");
        }

        [TestMethod]
        public void TypeBelowTheDeclaredType_IsWrittenUnderItsElementName_AndReadBack()
        {
            var audio = new NameTierAudioBook { Title = "t", Author = "a", Minutes = 5 };
            var xml = XmlSerializer.Serialize<NameTierBook>(audio, Bare);
            xml.Should().Be("<NameTierAudioBook><Title>t</Title><Author>a</Author><Minutes>5</Minutes></NameTierAudioBook>");
            XmlSerializer.Deserialize<NameTierBook>(xml, Bare).Should().BeOfType<NameTierAudioBook>();
            XmlSerializer.Serialize(new NameTierBook { Title = "t", Author = "a" }, Bare)
                .Should().Be("<NameTierBook><Title>t</Title><Author>a</Author></NameTierBook>");

            // A member declared as the middle type keeps its own name for a value of that type, and gives it up
            // to the element name of the type below for a value of that type.
            const string audioXml = "<NameTierAudioBook><Title>t</Title><Author>a</Author><Minutes>5</Minutes></NameTierAudioBook>";
            const string wrappedXml = "<Wrapped>" + audioXml + "<NameTierBook><Title>b</Title><Author>a</Author></NameTierBook></Wrapped>";
            var book = new NameTierBook { Title = "b", Author = "a" };
            xml = XmlSerializer.Serialize(new Held<NameTierBook> { One = audio, Wrapped = [audio, book] }, Bare);
            xml.Should().Be("<HeldOfNameTierBook>" + audioXml + wrappedXml + "</HeldOfNameTierBook>");
            var back = XmlSerializer.Deserialize<Held<NameTierBook>>(xml, Bare)!;
            back.One.Should().BeOfType<NameTierAudioBook>();
            back.Wrapped![0].Should().BeOfType<NameTierAudioBook>();
            back.Wrapped[1].Should().BeOfType<NameTierBook>();

            xml = XmlSerializer.Serialize(new Held<NameTierBook> { One = book, Wrapped = [audio, book] }, Bare);
            xml.Should().Be("<HeldOfNameTierBook><One><Title>b</Title><Author>a</Author></One>" + wrappedXml + "</HeldOfNameTierBook>");
            XmlSerializer.Deserialize<Held<NameTierBook>>(xml, Bare)!.One.Should().BeOfType<NameTierBook>();

            // Two members that both take the element named for the type below are still one name too many.
            FluentActions.Invoking(() => Bare.GetTypeInfo<Declared<NameTierBook>>())
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(
                    "The type maps more than one member to the element name 'NameTierAudioBook'.");
        }

        #endregion

    }

}
