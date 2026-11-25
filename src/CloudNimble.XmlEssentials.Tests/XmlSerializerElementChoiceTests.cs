using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using CloudNimble.XmlEssentials.Tests.POCOs.Choices;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A member that holds a choice of elements, each named for a type, and a member whose element is in no
    /// namespace.
    /// </summary>
    [TestClass]
    public class XmlSerializerElementChoiceTests
    {

        #region Fields

        private const string Types = "CloudNimble.XmlEssentials.Tests.POCOs.Choices.";

        private static readonly XmlSerializerOptions Bare = new() { TypeInfoResolver = DefaultXmlTypeInfoResolver.Instance, OmitXmlDeclaration = true, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull };

        #endregion

        #region Public Methods

        [TestMethod]
        public void Choice_WritesEachValueUnderTheElementOfItsType_AndReadsItBackInOrder()
        {
            var value = new ChoiceHolder
            {
                Any = 5,
                Flat = { new ChoiceShape { Name = "f" } },
                Many = { "a", 2, new ChoiceSquare { S = 3 }, "b" },
                One = new ChoiceSquare { S = 1 },
                Spaced = new ChoiceSquare { S = 7 },
                Unnamed = new ChoiceSquare { S = 8 },
                Wrapped = { new ChoiceCircle { R = 4 }, new ChoiceSquare { S = 5 }, new ChoiceShape { Name = "plain" } },
            };

            var xml = XmlSerializer.Serialize(value, Bare);

            xml.Should().Be(
                "<ChoiceHolder><square><S>1</S></square><number>5</number><t>a</t><n>2</n><sq><S>3</S></sq><t>b</t>" +
                "<wrapped><c><R>4</R></c><s><S>5</S></s><ChoiceShape><Name>plain</Name></ChoiceShape></wrapped>" +
                "<shape xmlns=\"urn:square\"><S>7</S></shape><Flat><Name>f</Name></Flat><ChoiceSquare><S>8</S></ChoiceSquare></ChoiceHolder>");
            var read = XmlSerializer.Deserialize<ChoiceHolder>(xml, Bare)!;
            read.One.Should().BeOfType<ChoiceSquare>().Which.S.Should().Be(1);
            read.Any.Should().Be(5);
            read.Many.Should().HaveCount(4);
            read.Many[0].Should().Be("a");
            read.Many[1].Should().Be(2);
            read.Many[2].Should().BeOfType<ChoiceSquare>().Which.S.Should().Be(3);
            read.Many[3].Should().Be("b");
            read.Wrapped.Should().HaveCount(3);
            read.Wrapped[0].Should().BeOfType<ChoiceCircle>();
            read.Wrapped[1].Should().BeOfType<ChoiceSquare>();
            read.Wrapped[2].Should().BeOfType<ChoiceShape>().Which.Name.Should().Be("plain");
            read.Spaced.Should().BeOfType<ChoiceSquare>().Which.S.Should().Be(7);
            read.Unnamed.Should().BeOfType<ChoiceSquare>().Which.S.Should().Be(8);
            XmlSerializer.Serialize(read, Bare).Should().Be(xml);
        }

        [TestMethod]
        public void Choice_AValueOfTheMembersOwnType_IsWrittenUnderTheMembersName()
        {
            var xml = XmlSerializer.Serialize(new ChoiceHolder { Any = "s", One = new ChoiceShape { Name = "p" }, Spaced = new ChoiceCircle { R = 2 }, Unnamed = new ChoiceCircle { R = 9 } }, Bare);

            xml.Should().Be("<ChoiceHolder><One><Name>p</Name></One><text>s</text><wrapped/><shape xmlns=\"urn:circle\"><R>2</R></shape><ChoiceCircle><R>9</R></ChoiceCircle></ChoiceHolder>");
            var read = XmlSerializer.Deserialize<ChoiceHolder>(xml, Bare)!;
            read.One.Should().BeOfType<ChoiceShape>().Which.Name.Should().Be("p");
            read.Any.Should().Be("s");
            read.Spaced.Should().BeOfType<ChoiceCircle>().Which.R.Should().Be(2);
            read.Unnamed.Should().BeOfType<ChoiceCircle>().Which.R.Should().Be(9);
        }

        [TestMethod]
        public void Choice_AValueOfATypeDerivedFromAnAlternative_IsWrittenUnderItsElement_WithItsDiscriminator()
        {
            const string xsi = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"";
            var value = new ChoiceOfBases
            {
                Items = { new TypedCircle { Radius = 1 }, new KindSquare { Side = 2 }, new ChoiceShape { Name = "p" }, new TypedSquare { Label = "s" } },
                One = new TypedCircle { Radius = 3 },
            };

            var xml = XmlSerializer.Serialize(value, Bare);

            xml.Should().Be(
                "<ChoiceOfBases><typed" + xsi + " xsi:type=\"circle\"><Radius>1</Radius></typed><kind kind=\"square\"><Side>2</Side></kind>" +
                "<plain><Name>p</Name></plain><typed" + xsi + " xsi:type=\"square\"><Label>s</Label><Side>0</Side></typed>" +
                "<one-typed" + xsi + " xsi:type=\"circle\"><Radius>3</Radius></one-typed></ChoiceOfBases>");
            var read = XmlSerializer.Deserialize<ChoiceOfBases>(xml, Bare)!;
            read.Items.Should().HaveCount(4);
            read.Items[0].Should().BeOfType<TypedCircle>().Which.Radius.Should().Be(1);
            read.Items[1].Should().BeOfType<KindSquare>().Which.Side.Should().Be(2);
            read.Items[2].Should().BeOfType<ChoiceShape>().Which.Name.Should().Be("p");
            read.Items[3].Should().BeOfType<TypedSquare>().Which.Label.Should().Be("s");
            read.One.Should().BeOfType<TypedCircle>().Which.Radius.Should().Be(3);
            XmlSerializer.Serialize(read, Bare).Should().Be(xml);

            // A type that derives from an alternative that declares no derived types has no way to be told apart.
            FluentActions.Invoking(() => XmlSerializer.Serialize(new ChoiceOfBases { Items = { new ChoiceCircle() } }, Bare))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be("The type '" + Types + "ChoiceCircle' is not a declared derived type of 'System.Object'.");
        }

        [TestMethod]
        public void Choice_AValueOfATypeThatHasNoElement_IsRefused()
        {
            FluentActions.Invoking(() => XmlSerializer.Serialize(new ChoiceHolder { Any = 1.5 }, Bare))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be("The type 'System.Double' is not a declared derived type of 'System.Object'.");
        }

        [TestMethod]
        public void Choice_ThatCannotBeToldApart_IsRefusedWhenTheTypeIsBound()
        {
            FluentActions.Invoking(() => Bare.GetTypeInfo<ChoiceOfAnotherType>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Member '" + Types + "ChoiceOfAnotherType.One' has an element for the type 'System.String', which is not a '" + Types + "ChoiceShape'.");
            FluentActions.Invoking(() => Bare.GetTypeInfo<ChoiceOfOneTypeTwice>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Member '" + Types + "ChoiceOfOneTypeTwice.One' has more than one element for the type '" + Types + "ChoiceCircle'. A value is written under the element of its type, so a type can have one. Declare the member as XmlNamedValue to let each value name its element.");
            FluentActions.Invoking(() => Bare.GetTypeInfo<ChoiceOfOneNameTwice>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Member '" + Types + "ChoiceOfOneNameTwice.One' has more than one element named 'a'. An element is read as the type of its name, so a name can have one.");
            FluentActions.Invoking(() => Bare.GetTypeInfo<ChoiceOfItemsHalfTyped>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Member '" + Types + "ChoiceOfItemsHalfTyped.Values' has more than one [XmlArrayItemAttribute] or a typed item choice. Polymorphic collection items are not enabled for this type.");
        }

        [TestMethod]
        public void NamedValue_IsWrittenUnderTheElementItNames_SoTwoElementsCanHoldOneType()
        {
            const string xsi = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"";
            var value = new NamedHolder
            {
                Items =
                {
                    new XmlNamedValue("v", "1"),
                    new XmlNamedValue("k", "2"),
                    new XmlNamedValue("n", 3),
                    new XmlNamedValue("shape", new TypedCircle { Radius = 1 }),
                    new XmlNamedValue("k", null),
                    new XmlNamedValue("plain", new ChoiceShape { Name = "p" }),
                },
                Phone = new XmlNamedValue { Name = "Work", Value = "w" },
                Spaced = new XmlNamedValue("id", "x") { Namespace = "urn:b" },
                Wrapped = { new XmlNamedValue("b", "1"), new XmlNamedValue("a", "2") },
            };

            var xml = XmlSerializer.Serialize(value, Bare);

            xml.Should().Be(
                "<NamedHolder><Work>w</Work><v>1</v><k>2</k><n>3</n><shape" + xsi + " xsi:type=\"circle\"><Radius>1</Radius></shape>" +
                "<k" + xsi + " xsi:nil=\"true\"/><plain><Name>p</Name></plain><id xmlns=\"urn:b\">x</id><wrapped><b>1</b><a>2</a></wrapped></NamedHolder>");
            var read = XmlSerializer.Deserialize<NamedHolder>(xml, Bare)!;
            read.Phone!.Name.Should().Be("Work");
            read.Phone.Value.Should().Be("w");
            read.Phone.Namespace.Should().BeNull();
            read.Items.ConvertAll(item => item.Name).Should().Equal("v", "k", "n", "shape", "k", "plain");
            read.Items[0].Value.Should().Be("1");
            read.Items[2].Value.Should().Be(3);
            read.Items[3].Value.Should().BeOfType<TypedCircle>().Which.Radius.Should().Be(1);
            read.Items[4].Value.Should().BeNull();
            read.Items[5].Value.Should().BeOfType<ChoiceShape>().Which.Name.Should().Be("p");
            read.Spaced!.Namespace.Should().Be("urn:b");
            read.Spaced.Value.Should().Be("x");
            read.Wrapped.ConvertAll(item => item.Name + "=" + item.Value).Should().Equal("b=1", "a=2");
            XmlSerializer.Serialize(read, Bare).Should().Be(xml);

            // With no namespace given, the first element of that name is the one.
            XmlSerializer.Serialize(new NamedHolder { Spaced = new XmlNamedValue("id", "y") }, Bare)
                .Should().Be("<NamedHolder><id xmlns=\"urn:a\">y</id><wrapped/></NamedHolder>");
        }

        [TestMethod]
        public void NamedValue_ThatNamesNoElementOfTheMember_OrHoldsAnotherType_IsRefused()
        {
            FluentActions.Invoking(() => XmlSerializer.Serialize(new NamedHolder { Phone = new XmlNamedValue("Mobile", "m") }, Bare))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be("The member has no element named 'Mobile' for a XmlNamedValue.");
            FluentActions.Invoking(() => XmlSerializer.Serialize(new NamedHolder { Spaced = new XmlNamedValue("id", "x") { Namespace = "urn:c" } }, Bare))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be("The member has no element named 'id' for a XmlNamedValue.");
            FluentActions.Invoking(() => XmlSerializer.Serialize(new NamedHolder { Phone = new XmlNamedValue("Home", 5) }, Bare))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be("The element 'Home' holds a value of type 'System.String', and the value given for it is of type 'System.Int32'.");

            // A type that derives from the type of the element is another type, unless that type declares it.
            FluentActions.Invoking(() => XmlSerializer.Serialize(new NamedHolder { Items = { new XmlNamedValue("plain", new ChoiceCircle()) } }, Bare))
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "The element 'plain' holds a value of type '" + Types + "ChoiceShape', and the value given for it is of type '" + Types + "ChoiceCircle'.");
            FluentActions.Invoking(() => Bare.GetTypeInfo<NamedOfOneNameTwice>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "Member '" + Types + "NamedOfOneNameTwice.One' has more than one element named 'a'. An element is read as the type of its name, so a name can have one.");
            FluentActions.Invoking(() => new XmlNamedValue(" ", 1)).Should().ThrowExactly<ArgumentException>().Which.ParamName.Should().Be("name");
        }

        [TestMethod]
        public void MemberInNoNamespace_UndeclaresTheDefaultNamespace_AndIsReadOnlyFromNoNamespace()
        {
            var value = new UnqualifiedMembers
            {
                Child = new UnqualifiedMembers { Local = "x", Qualified = "y" },
                Items = { 1, 2 },
                Local = "l",
                Qualified = "q",
                Typed = 3,
                Wrapped = { 4 },
            };

            var xml = XmlSerializer.Serialize(value, Bare);

            xml.Should().Be(
                "<root xmlns=\"urn:r\"><child xmlns=\"\"><local>x</local><Qualified>y</Qualified><wrap/></child>" +
                "<item xmlns=\"\">1</item><item xmlns=\"\">2</item><local xmlns=\"\">l</local><Qualified>q</Qualified><typed xmlns=\"\">3</typed>" +
                "<wrap xmlns=\"\"><int>4</int></wrap></root>");
            var read = XmlSerializer.Deserialize<UnqualifiedMembers>(xml, Bare)!;
            XmlSerializer.Serialize(read, Bare).Should().Be(xml);

            // A document with a prefix for the namespace says the same thing.
            var prefixed = XmlSerializer.Deserialize<UnqualifiedMembers>(
                "<r:root xmlns:r=\"urn:r\"><child><local>x</local></child><item>1</item><local>l</local><r:Qualified>q</r:Qualified><typed>3</typed><wrap><int>4</int></wrap></r:root>", Bare)!;
            prefixed.Child!.Local.Should().Be("x");
            prefixed.Items.Should().Equal(1);
            prefixed.Local.Should().Be("l");
            prefixed.Qualified.Should().Be("q");
            prefixed.Typed.Should().Be(3);
            prefixed.Wrapped.Should().Equal(4);

            // An element of the same name in the namespace is another element, and is not this member.
            var qualified = XmlSerializer.Deserialize<UnqualifiedMembers>("<root xmlns=\"urn:r\"><local>l</local><item>1</item><Qualified>q</Qualified></root>", Bare)!;
            qualified.Local.Should().BeNull();
            qualified.Items.Should().BeEmpty();
            qualified.Qualified.Should().Be("q");
        }

        #endregion

    }

}