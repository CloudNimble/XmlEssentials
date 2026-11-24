using System;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Which constructor creates an object that is read. The rule is the one <c>System.Text.Json</c> has: the
    /// marked constructor, then the public parameterless one, then the one public constructor with parameters.
    /// </summary>
    [TestClass]
    public class XmlSerializerConstructorChoiceTests
    {

        #region Fields

        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true };

        #endregion

        #region Public Methods

        [TestMethod]
        public void GetOnlyMembers_OfATypeWithAParameterlessConstructor_AreReadOnlyThroughAMarkedConstructor()
        {
            // The parameterless constructor is used, and a member with no setter is not read.
            var unmarked = XmlSerializer.Deserialize<GetOnlyWithBothCtors>("<GetOnlyWithBothCtors><Name>Ada</Name><Value>7</Value></GetOnlyWithBothCtors>", Bare)!;
            unmarked.Name.Should().BeNull();
            unmarked.Value.Should().Be(0);

            var marked = XmlSerializer.Deserialize<MarkedWithBothCtors>("<MarkedWithBothCtors><Name>Ada</Name><Value>7</Value></MarkedWithBothCtors>", Bare)!;
            marked.Name.Should().Be("Ada");
            marked.Value.Should().Be(7);
        }

        [TestMethod]
        public void KeyValuePair_IsCreatedThroughItsConstructor()
        {
            // It is a struct with no setters, and no attribute can be put on its constructor.
            var pair = new System.Collections.Generic.KeyValuePair<string, int>("a", 1);
            var xml = XmlSerializer.Serialize(pair, Bare);
            xml.Should().Be("<KeyValuePairOfStringInt32><Key>a</Key><Value>1</Value></KeyValuePairOfStringInt32>");
            XmlSerializer.Deserialize<System.Collections.Generic.KeyValuePair<string, int>>(xml, Bare).Should().Be(pair);
        }

        [TestMethod]
        public void MemberTheDocumentLeavesOut_OfATypeWithAParameterlessConstructor_IsNotRequired()
        {
            // The constructor with parameters is not used, so none of its parameters is required.
            var read = XmlSerializer.Deserialize<CamelOrParameterless>("<CamelOrParameterless/>", Bare)!;
            read.Name.Should().BeNull();
            read.CreatedBy.Should().Be("parameterless");
        }

        [TestMethod]
        public void Struct_IsCreatedAsItsDefault_UnlessItsConstructorIsMarked()
        {
            var unmarked = XmlSerializer.Deserialize<CtorStruct>("<CtorStruct><X>1</X><Y>2</Y></CtorStruct>", Bare);
            unmarked.X.Should().Be(1);
            unmarked.Y.Should().Be(2);
            unmarked.ThroughConstructor.Should().BeFalse();

            var marked = XmlSerializer.Deserialize<MarkedCtorStruct>("<MarkedCtorStruct><X>1</X><Y>2</Y></MarkedCtorStruct>", Bare);
            marked.X.Should().Be(1);
            marked.Y.Should().Be(2);
        }

        [TestMethod]
        public void TwoConstructorsWithParameters_AndNoParameterlessOne_AreRefused()
        {
            // The one with more parameters is not preferred: one of them has to be marked.
            FluentActions.Invoking(() => Bare.GetTypeInfo<TwoSizedCtors>())
                .Should().ThrowExactly<NotSupportedException>().Which.Message.Should().Be(
                    "The type 'CloudNimble.XmlEssentials.Tests.POCOs.TwoSizedCtors' has more than one constructor that can bind its members. Mark one with [XmlConstructorAttribute].");
        }

        #endregion

    }

}
