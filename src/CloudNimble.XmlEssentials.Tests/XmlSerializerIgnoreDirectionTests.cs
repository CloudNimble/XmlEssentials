using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// <see cref="XmlIgnoreCondition.WhenWriting"/> and <see cref="XmlIgnoreCondition.WhenReading"/>: a member that
    /// is ignored in one direction only, as System.Text.Json ignores it.
    /// </summary>
    [TestClass]
    public class XmlSerializerIgnoreDirectionTests
    {

        #region Fields

        private const string Nil = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>";
        private const string Read =
            "<IgnoreDirections ReadIgnoredAttribute=\"x\" WriteIgnoredAttribute=\"x\"><Count>not a number</Count><Item>9</Item><Normal>x</Normal>" +
            "<ReadIgnored>x</ReadIgnored><Tags><Int32>9</Int32></Tags><WriteIgnored>x</WriteIgnored></IgnoreDirections>";

        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true };

        #endregion

        #region Public Methods

        [TestMethod]
        public void ConditionValues_AreThoseOfSystemTextJson()
        {
            ((int)XmlIgnoreCondition.WhenWriting).Should().Be(4);
            ((int)XmlIgnoreCondition.WhenReading).Should().Be(5);
        }

        [TestMethod]
        [DataRow(XmlIgnoreCondition.WhenWriting, DisplayName = "when writing")]
        [DataRow(XmlIgnoreCondition.WhenReading, DisplayName = "when reading")]
        public void Options_WhenWritingAndWhenReading_HaveNoEffect_AsInSystemTextJson(XmlIgnoreCondition condition)
        {
            // System.Text.Json accepts both as DefaultIgnoreCondition and applies neither: a member with no mark of
            // its own is written and read as under Never. They take effect only on a member.
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultIgnoreCondition = condition };
            options.DefaultIgnoreCondition.Should().Be(condition);
            XmlSerializer.Serialize(new Annotated { Name = "x" }, options).Should().Be("<Annotated><Maybe" + Nil + "<Name>x</Name></Annotated>");
            var read = XmlSerializer.Deserialize<Annotated>("<Annotated><Maybe>m</Maybe><Name>x</Name></Annotated>", options)!;
            read.Maybe.Should().Be("m");
            read.Name.Should().Be("x");
            XmlSerializer.Serialize(new IgnoreDirections(), options).Should().Be(
                "<IgnoreDirections ReadIgnoredAttribute=\"ra\"><Count>1</Count><Item>1</Item><Normal>n</Normal><ReadIgnored>r</ReadIgnored><Tags><Int32>1</Int32></Tags></IgnoreDirections>",
                "the condition on a member still applies");
        }

        [TestMethod]
        public void WhenReading_AMemberThatFillsAConstructorParameter_IsStillRead()
        {
            XmlSerializer.Deserialize<IgnoreDirectionsRecord>("<IgnoreDirectionsRecord><Name>x</Name><Other>y</Other></IgnoreDirectionsRecord>")
                .Should().Be(new IgnoreDirectionsRecord("x", "y"));
            XmlSerializer.Serialize(new IgnoreDirectionsRecord("x", "y"), Bare).Should().Be("<IgnoreDirectionsRecord><Name>x</Name><Other>y</Other></IgnoreDirectionsRecord>");
        }

        [TestMethod]
        public void WhenReading_IsWrittenAndNotRead()
        {
            XmlSerializer.Serialize(new IgnoreDirections(), Bare).Should().Be(
                "<IgnoreDirections ReadIgnoredAttribute=\"ra\"><Count>1</Count><Item>1</Item><Normal>n</Normal><ReadIgnored>r</ReadIgnored><Tags><Int32>1</Int32></Tags></IgnoreDirections>");

            foreach (var handling in new[] { XmlObjectCreationHandling.Replace, XmlObjectCreationHandling.Populate })
            {
                // The document's element is accepted even when unmapped members are refused: the member is known, and only not read.
                var options = new XmlSerializerOptions { UnmappedMemberHandling = XmlUnmappedMemberHandling.Disallow, PreferredObjectCreationHandling = handling };
                var read = XmlSerializer.Deserialize<IgnoreDirections>(Read, options)!;
                read.Count.Should().Be(1, "the element is passed over, so what it holds is not read as a number");
                read.Items.Should().Equal(1);
                read.Normal.Should().Be("x");
                read.ReadIgnored.Should().Be("r");
                read.ReadIgnoredAttribute.Should().Be("ra");
                read.Tags.Should().Equal(1);
                read.WriteIgnored.Should().Be("x");
                read.WriteIgnoredAttribute.Should().Be("x");
            }

            XmlSerializer.Deserialize<IgnoreDirectionsText>("<IgnoreDirectionsText>x</IgnoreDirectionsText>")!.Body.Should().Be("b");
            XmlSerializer.Serialize(new IgnoreDirectionsText(), Bare).Should().Be("<IgnoreDirectionsText>b</IgnoreDirectionsText>");
        }

        [TestMethod]
        public void WhenReading_OnARequiredMember_IsRejected()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<IgnoreDirectionsRequired>("<IgnoreDirectionsRequired><Name>x</Name></IgnoreDirectionsRequired>"))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(
                    "The member 'Name' on type 'CloudNimble.XmlEssentials.Tests.POCOs.IgnoreDirectionsRequired' is required, and its ignore condition is XmlIgnoreCondition.WhenReading, so it would never be read.");
        }

        [TestMethod]
        public void WhenReadingAndWhenWriting_AreNotCheckedAgainstNullableAnnotations()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, RespectNullableAnnotations = true };
            XmlSerializer.Serialize(new IgnoreDirections { WriteIgnored = null!, WriteIgnoredAttribute = null! }, options).Should().Be(
                "<IgnoreDirections ReadIgnoredAttribute=\"ra\"><Count>1</Count><Item>1</Item><Normal>n</Normal><ReadIgnored>r</ReadIgnored><Tags><Int32>1</Int32></Tags></IgnoreDirections>",
                "a member that is not written is not refused for holding null");
            XmlSerializer.Deserialize<IgnoreDirections>("<IgnoreDirections><ReadIgnored" + Nil + "<Tags" + Nil + "</IgnoreDirections>", options)!.ReadIgnored
                .Should().Be("r", "a member that is not read is not refused for being nil");
        }

        [TestMethod]
        public void WithNoSetter_AMemberIsPassedOverWithoutBeingRead()
        {
            XmlSerializer.Deserialize<PopulateTarget>("<PopulateTarget><GetOnlyPoint><Y>not a number</Y></GetOnlyPoint><GetOnly><Int32>not a number</Int32></GetOnly><Loose>not a number</Loose></PopulateTarget>")!
                .GetOnly.Should().Equal(1);
        }

        #endregion

    }

}
