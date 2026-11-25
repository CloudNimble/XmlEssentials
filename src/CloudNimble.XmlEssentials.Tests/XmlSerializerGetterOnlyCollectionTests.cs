using System.Collections.Generic;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A collection property with no setter is not read, unless the value it holds is populated
    /// (<see cref="XmlObjectCreationHandling.Populate"/>), which adds the items to the collection it already holds.
    /// </summary>
    [TestClass]
    public class XmlSerializerGetterOnlyCollectionTests
    {

        #region Fields

        private const string Document =
            "<GetterOnlyCollections><Items>1</Items><Items>2</Items><Names><string>a</string><string>b</string></Names>" +
            "<Set>3</Set><Fixed>0</Fixed><Frozen>9</Frozen></GetterOnlyCollections>";

        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull };
        private static readonly XmlSerializerOptions Populate = new() { PreferredObjectCreationHandling = XmlObjectCreationHandling.Populate };
        private static readonly XmlSerializerOptions Replace = new();

        #endregion

        #region Public Methods

        [TestMethod]
        public void GetterOnlyCollection_InATypeWithConstructorParameters_IsNeverPopulated()
        {
            var xml = XmlSerializer.Serialize(new GetterOnlyCtor("n"), Bare);
            xml.Should().Be("<GetterOnlyCtor><Name>n</Name><Items>5</Items><Tags><string>t</string></Tags></GetterOnlyCtor>");
            foreach (var options in new[] { Replace, Populate })
            {
                var read = XmlSerializer.Deserialize<GetterOnlyCtor>(
                    "<GetterOnlyCtor><Items>1</Items><Name>n</Name><Items>2</Items><Tags><string>u</string></Tags></GetterOnlyCtor>", options)!;
                read.Name.Should().Be("n");
                read.Items.Should().Equal([5], "the list is as the constructor left it");
                read.Tags.Should().Equal("t");
            }
        }

        [TestMethod]
        public void GetterOnlyCollections_AreNotReadByDefault()
        {
            Replace.PreferredObjectCreationHandling.Should().Be(XmlObjectCreationHandling.Replace);
            var read = XmlSerializer.Deserialize<GetterOnlyCollections>(Document, Replace)!;
            read.Items.Should().BeEmpty();
            read.Names.Should().BeEmpty();
            read.Set.Should().BeEmpty();
            read.Fixed.Should().Equal(0);
            read.Frozen.Should().Equal(9);
        }

        [TestMethod]
        public void GetterOnlyCollections_ArePopulatedWhenTheOptionsSaySo()
        {
            var value = new GetterOnlyCollections();
            value.Items.AddRange([1, 2]);
            value.Names.AddRange(["a", "b"]);
            value.Set.Add(3);
            XmlSerializer.Serialize(value, Bare).Should().Be(Document);

            var read = XmlSerializer.Deserialize<GetterOnlyCollections>(
                "<GetterOnlyCollections><Items>1</Items><Items>2</Items><Names><string>a</string><string>b</string></Names><Set>3</Set></GetterOnlyCollections>", Populate)!;
            read.Items.Should().Equal(1, 2);
            read.Names.Should().Equal("a", "b");
            read.Set.Should().Equal(3);
        }

        [TestMethod]
        public void GetterOnlyCollections_ThatCannotBePopulated_AreNotRead()
        {
            var read = XmlSerializer.Deserialize<GetterOnlyCollections>(
                "<GetterOnlyCollections><Missing>1</Missing><Fixed>7</Fixed><Fixed>8</Fixed><Frozen>4</Frozen><Items>6</Items></GetterOnlyCollections>", Populate)!;
            read.Missing.Should().BeNull("there is no list to add to and no setter to give it one");
            read.Fixed.Should().Equal([0], "an array cannot take items");
            read.Frozen.Should().Equal([9], "a read-only collection cannot take items");
            read.Items.Should().Equal(6);
        }

        [TestMethod]
        public void GetterOnlyCollections_AreStillWrittenAndFilled_WhenReadOnlyPropertiesAreIgnored()
        {
            // System.Text.Json makes the same exception: a collection needs no setter.
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, IgnoreReadOnlyProperties = true, PreferredObjectCreationHandling = XmlObjectCreationHandling.Populate };
            XmlSerializer.Serialize(new GetterOnlyCollections(), options)
                .Should().Be("<GetterOnlyCollections><Names/><Fixed>0</Fixed><Frozen>9</Frozen></GetterOnlyCollections>");
            var read = XmlSerializer.Deserialize<GetterOnlyCollections>(Document, options)!;
            read.Items.Should().Equal(1, 2);
            read.Names.Should().Equal("a", "b");
            read.Set.Should().Equal(3);
        }

        [TestMethod]
        public void ReadOnlyDictionaryAndReadOnlyCollectionField_AreStillWrittenAndFilled_WhenReadOnlyMembersAreIgnored()
        {
            var options = new XmlSerializerOptions
            {
                OmitXmlDeclaration = true,
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
                IncludeFields = true,
                PreferredObjectCreationHandling = XmlObjectCreationHandling.Populate,
            };
            var value = new ReadOnlyHolders();
            value.Counts["a"] = 1;
            value.Numbers.Add(2);

            var xml = XmlSerializer.Serialize(value, options);

            xml.Should().Be("<ReadOnlyHolders><Counts><a>1</a></Counts><Numbers>2</Numbers></ReadOnlyHolders>");
            var read = XmlSerializer.Deserialize<ReadOnlyHolders>(xml, options)!;
            read.Counts.Should().Equal(new Dictionary<string, int> { ["a"] = 1 });
            read.Numbers.Should().Equal(2);
            XmlSerializer.Serialize(value, new XmlSerializerOptions { OmitXmlDeclaration = true, IncludeFields = true })
                .Should().Be("<ReadOnlyHolders><Computed>c</Computed><Counts><a>1</a></Counts><Fixed>1</Fixed><Numbers>2</Numbers></ReadOnlyHolders>");
        }

        #endregion

    }

}
