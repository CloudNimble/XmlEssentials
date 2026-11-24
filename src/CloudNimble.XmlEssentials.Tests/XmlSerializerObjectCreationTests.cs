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
    /// <see cref="XmlSerializerOptions.PreferredObjectCreationHandling"/> and
    /// <see cref="XmlObjectCreationHandlingAttribute"/>: whether a member that already holds a value is given a new
    /// one or has its value populated, with the rules System.Text.Json has for <c>JsonObjectCreationHandling</c>.
    /// </summary>
    [TestClass]
    public class XmlSerializerObjectCreationTests
    {

        #region Fields

        private const string Nil = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>";
        private const string Types = "CloudNimble.XmlEssentials.Tests.POCOs.";
        private static readonly XmlSerializerOptions Populate = new() { PreferredObjectCreationHandling = XmlObjectCreationHandling.Populate };
        private static readonly XmlSerializerOptions Replace = new();

        #endregion

        #region Public Methods

        [TestMethod]
        public void Attribute_OnAMember_WinsOverTheOption()
        {
            const string xml = "<PopulateMarked><Items><Int32>9</Int32></Items><Other><Int32>9</Int32></Other><Replaced><Int32>9</Int32></Replaced></PopulateMarked>";
            var replaced = XmlSerializer.Deserialize<PopulateMarked>(xml, Replace)!;
            replaced.Items.Should().Equal(1, 9);
            replaced.Other.Should().Equal(1);
            replaced.Replaced.Should().Equal(9);

            var populated = XmlSerializer.Deserialize<PopulateMarked>(xml, Populate)!;
            populated.Items.Should().Equal(1, 9);
            populated.Other.Should().Equal(1, 9);
            populated.Replaced.Should().Equal(9);
        }

        [TestMethod]
        public void Attribute_OnAMember_DoesNotReachTheMembersOfTheObjectItHolds()
        {
            const string inner = "<Inner><Child><B>2</B></Child><List><Int32>9</Int32></List><Size>4</Size></Inner>";
            var marked = XmlSerializer.Deserialize<PopulateNestedMarked>("<PopulateNestedMarked>" + inner + "</PopulateNestedMarked>", Replace)!;
            marked.Inner.Size.Should().Be(4, "the object the marked member holds is populated");
            marked.Inner.Child.B.Should().Be(0, "its own members follow the options, which replace");
            marked.Inner.List.Should().Equal(1);

            var nested = XmlSerializer.Deserialize<PopulateNested>("<PopulateNested>" + inner + "</PopulateNested>", Populate)!;
            nested.Inner.Size.Should().Be(4);
            nested.Inner.Child.A.Should().Be(7);
            nested.Inner.Child.B.Should().Be(2);
            nested.Inner.List.Should().Equal(1, 9);

            XmlSerializer.Deserialize<PopulateNested>("<PopulateNested>" + inner + "</PopulateNested>", Replace)!.Inner.Size.Should().Be(3, "a member with no setter is not read");
        }

        [TestMethod]
        public void Attribute_OnAMemberThatCannotBePopulated_IsRejected()
        {
            const string cannot = " is marked with XmlObjectCreationHandling.Populate but it doesn't support populating. This can be either because the property type is immutable or it could use a custom converter.";
            Reading<PopulateBadArray>("<PopulateBadArray/>").Should().ThrowExactly<InvalidOperationException>().Which.Message
                .Should().Be("Property 'Items' on type '" + Types + "PopulateBadArray'" + cannot);
            Reading<PopulateBadMixed>("<PopulateBadMixed/>").Should().ThrowExactly<InvalidOperationException>().Which.Message
                .Should().Be("Property 'Content' on type '" + Types + "PopulateBadMixed'" + cannot);
            Reading<PopulateBadText>("<PopulateBadText/>").Should().ThrowExactly<InvalidOperationException>().Which.Message
                .Should().Be("Property 'Name' on type '" + Types + "PopulateBadText'" + cannot);
            Reading<PopulateBadConverted>("<PopulateBadConverted/>").Should().ThrowExactly<InvalidOperationException>().Which.Message
                .Should().Be("Property 'Price' on type '" + Types + "PopulateBadConverted'" + cannot);
            Reading<PopulateBadStruct>("<PopulateBadStruct/>").Should().ThrowExactly<InvalidOperationException>().Which.Message
                .Should().Be("Property 'Point' on type '" + Types + "PopulateBadStruct' is marked with XmlObjectCreationHandling.Populate but is a value type that doesn't have a setter.");
            Reading<PopulateBadPolymorphic>("<PopulateBadPolymorphic/>").Should().ThrowExactly<InvalidOperationException>().Which.Message
                .Should().Be("Property 'Product' on type '" + Types + "PopulateBadPolymorphic' is marked with XmlObjectCreationHandling.Populate but its type allows polymorphic deserialization.");
            FluentActions.Invoking(() => XmlSerializer.Serialize(new PopulateBadArray())).Should().ThrowExactly<InvalidOperationException>().Which.Message
                .Should().Be("Property 'Items' on type '" + Types + "PopulateBadArray'" + cannot, "the type is refused however it is used");
        }

        [TestMethod]
        public void Attribute_OnAType_AppliesToTheMembersThatCanBePopulated()
        {
            var read = XmlSerializer.Deserialize<PopulateMarkedType>(
                "<PopulateMarkedType><Array><Int32>9</Int32></Array><Items><Int32>9</Int32></Items><Name>x</Name></PopulateMarkedType>", Replace)!;
            read.Array.Should().Equal(9);
            read.Items.Should().Equal(1, 9);
            read.Name.Should().Be("x");
        }

        [TestMethod]
        public void ConstructorWithParameters_MeansNoMemberIsPopulated()
        {
            const string xml = "<PopulateWithConstructor><Name>n</Name><Items><Int32>9</Int32></Items></PopulateWithConstructor>";
            XmlSerializer.Deserialize<PopulateWithConstructor>(xml, Populate)!.Items.Should().Equal(1);
            XmlSerializer.Deserialize<PopulateWithConstructor>(xml, Replace)!.Items.Should().Equal(1);
            Reading<PopulateBadConstructor>("<PopulateBadConstructor><Name>n</Name></PopulateBadConstructor>").Should().ThrowExactly<NotSupportedException>().Which.Message
                .Should().Be("XmlObjectCreationHandling.Populate is currently not supported in types with parameterized constructors.");
        }

        [TestMethod]
        public void ConstructorParameter_OfAnObjectThatIsPopulated_IsLeftAsItIs()
        {
            // The object exists, so it is not constructed again, and nothing else can set the name.
            var read = XmlSerializer.Deserialize<PopulateConstructed>(
                "<PopulateConstructed><Child><Name>new</Name><Items>9</Items></Child></PopulateConstructed>", Populate)!;
            read.Child.Name.Should().Be("kept");
            read.Child.Items.Should().Equal([5], "a type created through a constructor with parameters populates none of its members");
        }

        [TestMethod]
        public void Default_IsReplace()
        {
            new XmlSerializerOptions().PreferredObjectCreationHandling.Should().Be(XmlObjectCreationHandling.Replace);
            new XmlSerializerOptions(Populate).PreferredObjectCreationHandling.Should().Be(XmlObjectCreationHandling.Populate);
            new XmlObjectCreationHandlingAttribute(XmlObjectCreationHandling.Populate).Handling.Should().Be(XmlObjectCreationHandling.Populate);
        }

        [TestMethod]
        public void InterfaceCollections_ArePopulatedWhenTheyCanTakeItems()
        {
            var read = XmlSerializer.Deserialize<PopulateInterfaces>(
                "<PopulateInterfaces><Collection><Int32>9</Int32></Collection><Entries><b>2</b></Entries><List><Int32>9</Int32></List><ReadOnly><Int32>9</Int32></ReadOnly><Sequence><Int32>9</Int32></Sequence></PopulateInterfaces>",
                Populate)!;
            read.Collection.Should().Equal(1, 9);
            read.Entries.Should().Equal(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });
            read.List.Should().Equal(1, 9);
            read.ReadOnly.Should().Equal([9], "a read-only view cannot be populated, so it is replaced");
            read.Sequence.Should().Equal([1], "a sequence cannot be populated and has no setter, so it is not read");

            Reading<PopulateInterfaces>("<PopulateInterfaces><Fixed><Int32>9</Int32></Fixed></PopulateInterfaces>", Populate)
                .Should().ThrowExactly<NotSupportedException>().Which.Message
                .Should().Be("The collection of type 'System.Int32[]' held by the member 'Fixed' is read-only and cannot be populated.");
            XmlSerializer.Deserialize<PopulateInterfaces>("<PopulateInterfaces><Fixed><Int32>9</Int32></Fixed></PopulateInterfaces>", Replace)!.Fixed.Should().Equal(9);

            Reading<PopulateInterfaces>("<PopulateInterfaces><FixedEntries><b>2</b></FixedEntries></PopulateInterfaces>", Populate)
                .Should().ThrowExactly<NotSupportedException>().Which.Message
                .Should().Be("The dictionary of type 'System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.Int32]' is read-only and cannot be populated.");
            XmlSerializer.Deserialize<PopulateInterfaces>("<PopulateInterfaces><FixedEntries><b>2</b></FixedEntries></PopulateInterfaces>", Replace)!.FixedEntries
                .Should().Equal(new Dictionary<string, int> { ["b"] = 2 });
        }

        [TestMethod]
        public void Populate_AddsToCollectionsAndDictionaries()
        {
            var read = XmlSerializer.Deserialize<PopulateTarget>(
                "<PopulateTarget><GetOnly><Int32>9</Int32></GetOnly><GetOnlyMap><a>5</a><b>2</b></GetOnlyMap><Loose>8</Loose><Loose>9</Loose><Map><b>2</b></Map>" +
                "<NullGetOnly><Int32>9</Int32></NullGetOnly><NullSettable><Int32>9</Int32></NullSettable><Queue><Int32>9</Int32></Queue><Set><Int32>9</Int32></Set>" +
                "<Settable><Int32>9</Int32></Settable><Stack><Int32>9</Int32></Stack></PopulateTarget>", Populate)!;
            read.GetOnly.Should().Equal(1, 9);
            read.GetOnlyMap.Should().Equal(new Dictionary<string, int> { ["a"] = 5, ["b"] = 2 }, "an entry the document gives again takes the document's value");
            read.Loose.Should().Equal(1, 8, 9);
            read.Map.Should().Equal(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });
            read.NullGetOnly.Should().BeNull("there is nothing to populate and no setter to give it a new list");
            read.NullSettable.Should().Equal(9);
            read.Queue.Should().Equal(1, 9);
            read.Set.Should().Equal(1, 9);
            read.Settable.Should().Equal(1, 9);
            read.Stack.Should().Equal([9, 1], "the first item of the document is on top, as when a stack is read whole");
        }

        [TestMethod]
        public void Populate_AMemberGivenTwice_IsPopulatedTwice()
        {
            var read = XmlSerializer.Deserialize<PopulateTarget>(
                "<PopulateTarget><GetOnly><Int32>2</Int32></GetOnly><GetOnlyChild><B>2</B></GetOnlyChild><GetOnly><Int32>3</Int32></GetOnly><GetOnlyChild><A>4</A></GetOnlyChild></PopulateTarget>", Populate)!;
            read.GetOnly.Should().Equal(1, 2, 3);
            read.GetOnlyChild.A.Should().Be(4);
            read.GetOnlyChild.B.Should().Be(2);
        }

        [TestMethod]
        public void Populate_DoesNotApplyToWhatCannotBePopulated()
        {
            var read = XmlSerializer.Deserialize<PopulateTarget>(
                "<PopulateTarget><Array><Int32>9</Int32></Array><GetOnlyPoint><Y>2</Y></GetOnlyPoint><Immutable><Int32>9</Int32></Immutable>" +
                "<ImmutableMap><b>2</b></ImmutableMap><StructMap><b>2</b></StructMap></PopulateTarget>", Populate)!;
            read.Array.Should().Equal(9);
            read.Immutable.Should().Equal(9);
            read.GetOnlyPoint.Should().Be(new PopulatePoint { X = 7 }, "a value type with no setter cannot be populated, and is not read");
            read.ImmutableMap.Should().Equal(new Dictionary<string, int> { ["b"] = 2 });
            read.StructMap.Should().Equal(new Dictionary<string, int> { ["b"] = 2 }, "a dictionary that is a value type is replaced");

            var mixed = XmlSerializer.Deserialize<PopulateMixed>("<PopulateMixed>y</PopulateMixed>", Populate)!;
            mixed.Content.Should().ContainSingle().Which.Should().BeOfType<XmlMixedText>().Which.Text.Should().Be("y", "mixed content is replaced");
        }

        [TestMethod]
        public void Populate_NilForAMember_SetsNullOrIsRejected()
        {
            var read = XmlSerializer.Deserialize<PopulateTarget>("<PopulateTarget><Child" + Nil + "<Map" + Nil + "<Settable" + Nil + "<MaybePoint" + Nil + "</PopulateTarget>", Populate)!;
            read.Child.Should().BeNull();
            read.Map.Should().BeNull();
            read.Settable.Should().BeNull();
            read.MaybePoint.Should().BeNull();

            Reading<PopulateTarget>("<PopulateTarget><GetOnly" + Nil + "</PopulateTarget>", Populate).Should().ThrowExactly<InvalidOperationException>().Which.Message
                .Should().Be("Unable to assign 'null' to the property or field of type 'System.Collections.Generic.List`1[System.Int32]'.");
            Reading<PopulateTarget>("<PopulateTarget><GetOnlyChild" + Nil + "</PopulateTarget>", Populate).Should().ThrowExactly<InvalidOperationException>().Which.Message
                .Should().Be("Unable to assign 'null' to the property or field of type '" + Types + "PopulateChild'.");
            Reading<PopulateTarget>("<PopulateTarget><GetOnlyMap" + Nil + "</PopulateTarget>", Populate).Should().ThrowExactly<InvalidOperationException>().Which.Message
                .Should().Be("Unable to assign 'null' to the property or field of type 'System.Collections.Generic.Dictionary`2[System.String,System.Int32]'.");

            var replaced = XmlSerializer.Deserialize<PopulateTarget>("<PopulateTarget><GetOnly" + Nil + "<GetOnlyChild" + Nil + "<GetOnlyMap" + Nil + "</PopulateTarget>", Replace)!;
            replaced.GetOnly.Should().Equal([1], "a member with no setter is not read");
            replaced.GetOnlyChild.A.Should().Be(7);
            replaced.GetOnlyMap.Keys.Should().Equal("a");
        }

        [TestMethod]
        public void Populate_SetsTheMembersOfObjectsAndValueTypes()
        {
            var read = XmlSerializer.Deserialize<PopulateTarget>(
                "<PopulateTarget><Child><B>2</B></Child><GetOnlyChild><B>2</B></GetOnlyChild><MaybePoint><Y>2</Y></MaybePoint><Point><Y>2</Y></Point></PopulateTarget>", Populate)!;
            read.Child.A.Should().Be(7, "the object the member holds is kept");
            read.Child.B.Should().Be(2);
            read.GetOnlyChild.A.Should().Be(7);
            read.GetOnlyChild.B.Should().Be(2);
            read.MaybePoint.Should().Be(new PopulatePoint { X = 7, Y = 2 });
            read.Point.Should().Be(new PopulatePoint { X = 7, Y = 2 });
        }

        [TestMethod]
        [DataRow(false, DisplayName = "preserve references")]
        [DataRow(true, DisplayName = "ignore cycles")]
        public void Populate_WithReferenceHandling_IsRejectedForATypeWithAMemberToPopulate(bool ignoreCycles)
        {
            var handler = ignoreCycles ? XmlReferenceHandler.IgnoreCycles : XmlReferenceHandler.Preserve;
            var options = new XmlSerializerOptions { PreferredObjectCreationHandling = XmlObjectCreationHandling.Populate, ReferenceHandler = handler };
            Reading<PopulateTarget>("<PopulateTarget/>", options).Should().ThrowExactly<InvalidOperationException>().Which.Message
                .Should().Be("XmlObjectCreationHandling.Populate is incompatible with reference handling.");
            Reading<PopulateMarked>("<PopulateMarked/>", new XmlSerializerOptions { ReferenceHandler = handler }).Should().ThrowExactly<InvalidOperationException>().Which.Message
                .Should().Be("XmlObjectCreationHandling.Populate is incompatible with reference handling.");
            XmlSerializer.Deserialize<PopulateChild>("<PopulateChild><B>2</B></PopulateChild>", options)!.B.Should().Be(2, "a type with nothing to populate is not affected");
        }

        [TestMethod]
        public void Replace_GivesSettableMembersNewValues_AndDoesNotReadMembersWithNoSetter()
        {
            var read = XmlSerializer.Deserialize<PopulateTarget>(
                "<PopulateTarget><Array><Int32>9</Int32></Array><Child><B>2</B></Child><GetOnly><Int32>9</Int32></GetOnly><GetOnlyChild><B>2</B></GetOnlyChild>" +
                "<GetOnlyMap><b>2</b></GetOnlyMap><GetOnlyPoint><Y>2</Y></GetOnlyPoint><Immutable><Int32>9</Int32></Immutable><Loose>9</Loose><Map><b>2</b></Map>" +
                "<MaybePoint><Y>2</Y></MaybePoint><NullGetOnly><Int32>9</Int32></NullGetOnly><NullSettable><Int32>9</Int32></NullSettable><Point><Y>2</Y></Point>" +
                "<Queue><Int32>9</Int32></Queue><Set><Int32>9</Int32></Set><Settable><Int32>9</Int32></Settable><Stack><Int32>9</Int32></Stack></PopulateTarget>", Replace)!;
            read.Array.Should().Equal(9);
            read.Child.A.Should().Be(1, "a new object is created");
            read.Child.B.Should().Be(2);
            read.GetOnly.Should().Equal(1);
            read.GetOnlyChild.A.Should().Be(7);
            read.GetOnlyChild.B.Should().Be(0);
            read.GetOnlyMap.Should().Equal(new Dictionary<string, int> { ["a"] = 1 });
            read.GetOnlyPoint.Should().Be(new PopulatePoint { X = 7 });
            read.Immutable.Should().Equal(9);
            read.Loose.Should().Equal(1);
            read.Map.Should().Equal(new Dictionary<string, int> { ["b"] = 2 });
            read.MaybePoint.Should().Be(new PopulatePoint { Y = 2 });
            read.NullGetOnly.Should().BeNull();
            read.NullSettable.Should().Equal(9);
            read.Point.Should().Be(new PopulatePoint { Y = 2 });
            read.Queue.Should().Equal(1);
            read.Set.Should().Equal(1);
            read.Settable.Should().Equal(9);
            read.Stack.Should().Equal(1);
        }

        #endregion

        #region Private Methods

        private static Action Reading<T>(string xml, XmlSerializerOptions? options = null)
        {
            return () => XmlSerializer.Deserialize<T>(xml, options);
        }

        #endregion

    }

}
