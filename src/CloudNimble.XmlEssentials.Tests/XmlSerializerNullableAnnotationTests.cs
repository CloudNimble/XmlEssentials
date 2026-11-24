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
    /// <see cref="XmlSerializerOptions.RespectNullableAnnotations"/>: a null for a member or constructor parameter
    /// annotated non-nullable is refused when it is read and when it is written, as System.Text.Json refuses it.
    /// </summary>
    [TestClass]
    public class XmlSerializerNullableAnnotationTests
    {

        #region Fields

        private const string Advice = " Consider updating its nullability annotation.";
        private const string Nil = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"";
        private const string Types = "CloudNimble.XmlEssentials.Tests.POCOs.";
        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true };
        private static readonly XmlSerializerOptions Respect = new() { OmitXmlDeclaration = true, RespectNullableAnnotations = true };

        #endregion

        #region Public Methods

        [TestMethod]
        public void FlowAttributes_DecideWhatIsRead()
        {
            XmlSerializer.Deserialize<AnnotatedFlow>("<AnnotatedFlow><Allowed" + Nil + "/></AnnotatedFlow>", Respect)!.Allowed.Should().BeNull();
            XmlSerializer.Deserialize<AnnotatedFlow>("<AnnotatedFlow><Certain" + Nil + "/></AnnotatedFlow>", Respect)!.Certain.Should().BeNull();
            Reading<AnnotatedFlow>("<AnnotatedFlow><Disallowed" + Nil + "/></AnnotatedFlow>", Respect)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(Setting("Disallowed", "AnnotatedFlow") + " Path: /AnnotatedFlow/Disallowed | LineNumber: 1 | BytePositionInLine: 98.");
            Reading<AnnotatedFlow>("<AnnotatedFlow><Possibly" + Nil + "/></AnnotatedFlow>", Respect)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(Setting("Possibly", "AnnotatedFlow") + " Path: /AnnotatedFlow/Possibly | LineNumber: 1 | BytePositionInLine: 96.");
        }

        [TestMethod]
        public void FlowAttributes_DecideWhatIsWritten()
        {
            Writing(new AnnotatedFlow { Allowed = null }, Respect)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(Getting("Allowed", "AnnotatedFlow") + " Path: /AnnotatedFlow.");
            Writing(new AnnotatedFlow { Certain = null }, Respect)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(Getting("Certain", "AnnotatedFlow") + " Path: /AnnotatedFlow.");
            XmlSerializer.Serialize(new AnnotatedFlow { Disallowed = null!, Possibly = null! }, Respect).Should().Be(
                "<AnnotatedFlow><Allowed>a</Allowed><Certain>c</Certain><Disallowed" + Nil + "/><Possibly" + Nil + "/></AnnotatedFlow>");
        }

        [TestMethod]
        public void GenericMember_SaysNothingAboutNull()
        {
            var xml = XmlSerializer.Serialize(new AnnotatedBox<string> { Value = null! }, Respect);
            xml.Should().Be("<AnnotatedBoxOfString><Value" + Nil + "/></AnnotatedBoxOfString>");
            XmlSerializer.Deserialize<AnnotatedBox<string>>(xml, Respect)!.Value.Should().BeNull();
        }

        [TestMethod]
        public void NullRoot_IsNotAMember()
        {
            var xml = XmlSerializer.Serialize<Annotated?>(null, Respect);
            xml.Should().Be("<Annotated" + Nil + "/>");
            XmlSerializer.Deserialize<Annotated>(xml, Respect).Should().BeNull();
        }

        [TestMethod]
        public void ObliviousMember_SaysNothingAboutNull()
        {
            var xml = XmlSerializer.Serialize(new AnnotatedOblivious { Name = null }, Respect);
            xml.Should().Be("<AnnotatedOblivious><Name" + Nil + "/></AnnotatedOblivious>");
            XmlSerializer.Deserialize<AnnotatedOblivious>(xml, Respect)!.Name.Should().BeNull();
        }

        [TestMethod]
        public void Off_NullIsReadAndWrittenWhateverTheAnnotation()
        {
            Bare.RespectNullableAnnotations.Should().BeFalse();
            var xml = XmlSerializer.Serialize(new Annotated { Name = null! }, Bare);
            xml.Should().Be("<Annotated><Maybe" + Nil + "/><Name" + Nil + "/></Annotated>");
            XmlSerializer.Deserialize<Annotated>(xml, Bare)!.Name.Should().BeNull();
            XmlSerializer.Deserialize<AnnotatedRecord>("<AnnotatedRecord><Name" + Nil + "/><Maybe>m</Maybe></AnnotatedRecord>", Bare)!.Name.Should().BeNull();
        }

        [TestMethod]
        public void Read_NilForAConstructorParameter_IsRejectedWhenTheParameterIsNonNullable()
        {
            Reading<AnnotatedRecord>("<AnnotatedRecord><Name" + Nil + "/><Maybe>m</Maybe></AnnotatedRecord>", Respect)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(
                    "The constructor parameter 'Name' on type '" + Types + "AnnotatedRecord' doesn't allow null values." + Advice + " Path: /AnnotatedRecord/Name | LineNumber: 1 | BytePositionInLine: 94.");
            XmlSerializer.Deserialize<AnnotatedRecord>("<AnnotatedRecord><Name>n</Name><Maybe" + Nil + "/></AnnotatedRecord>", Respect)
                .Should().Be(new AnnotatedRecord("n", null));
        }

        [TestMethod]
        public void Read_NilForANonNullableMember_IsRejected()
        {
            Reading<Annotated>("<Annotated><Name" + Nil + "/></Annotated>", Respect)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(Setting("Name", "Annotated") + " Path: /Annotated/Name | LineNumber: 1 | BytePositionInLine: 88.");

            var fields = new XmlSerializerOptions { RespectNullableAnnotations = true, IncludeFields = true };
            Reading<AnnotatedField>("<AnnotatedField><Label" + Nil + "/></AnnotatedField>", fields)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(Setting("Label", "AnnotatedField") + " Path: /AnnotatedField/Label | LineNumber: 1 | BytePositionInLine: 94.");
        }

        [TestMethod]
        public void Read_NilForANullableMember_AndAMissingMember_AreAccepted()
        {
            var read = XmlSerializer.Deserialize<Annotated>("<Annotated><Maybe" + Nil + "/></Annotated>", Respect)!;
            read.Maybe.Should().BeNull();
            read.Name.Should().Be("n", "a member the document leaves out is not set to null");
        }

        [TestMethod]
        public void Read_NilInsideACollectionOrDictionary_IsAccepted()
        {
            var read = XmlSerializer.Deserialize<AnnotatedShapes>(
                "<AnnotatedShapes><Item" + Nil + "/><Map><a" + Nil + "/></Map><Tags><String" + Nil + "/></Tags></AnnotatedShapes>", Respect)!;
            read.Loose.Should().Equal([null!]);
            read.Map.Should().Equal(new Dictionary<string, string> { ["a"] = null! });
            read.Tags.Should().Equal([null!]);
        }

        [TestMethod]
        public void Read_NilWrapper_ForANonNullableCollection_IsRejected()
        {
            Reading<AnnotatedShapes>("<AnnotatedShapes><Tags" + Nil + "/></AnnotatedShapes>", Respect)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(Setting("Tags", "AnnotatedShapes") + " Path: /AnnotatedShapes/Tags | LineNumber: 1 | BytePositionInLine: 94.");
            Reading<AnnotatedShapes>("<AnnotatedShapes><Map" + Nil + "/></AnnotatedShapes>", Respect)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(Setting("Map", "AnnotatedShapes") + " Path: /AnnotatedShapes/Map | LineNumber: 1 | BytePositionInLine: 93.");
        }

        [TestMethod]
        public void Write_NullFromANonNullableMember_IsRejected()
        {
            Writing(new Annotated { Name = null! }, Respect)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(Getting("Name", "Annotated") + " Path: /Annotated.");
            Writing(new AnnotatedRecord(null!, "m"), Respect)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(Getting("Name", "AnnotatedRecord") + " Path: /AnnotatedRecord.");
            Writing(new AnnotatedShapes { Code = null! }, Respect)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(Getting("Code", "AnnotatedShapes") + " Path: /AnnotatedShapes.");
            Writing(new AnnotatedShapes { Loose = null! }, Respect)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(Getting("Loose", "AnnotatedShapes") + " Path: /AnnotatedShapes.");
            Writing(new AnnotatedShapes { Map = null! }, Respect)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(Getting("Map", "AnnotatedShapes") + " Path: /AnnotatedShapes.");
            Writing(new AnnotatedShapes { Tags = null! }, Respect)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(Getting("Tags", "AnnotatedShapes") + " Path: /AnnotatedShapes.");
            Writing(new AnnotatedText { Body = null! }, Respect)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(Getting("Body", "AnnotatedText") + " Path: /AnnotatedText.");

            var fields = new XmlSerializerOptions { RespectNullableAnnotations = true, IncludeFields = true };
            Writing(new AnnotatedField { Label = null! }, fields)
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(Getting("Label", "AnnotatedField") + " Path: /AnnotatedField.");
        }

        [TestMethod]
        public void Write_NullInsideACollectionOrDictionary_IsAccepted()
        {
            XmlSerializer.Serialize(new AnnotatedShapes { Loose = [null!], Map = { ["a"] = null! }, Tags = [null!] }, Respect).Should().Be(
                "<AnnotatedShapes Code=\"c\"><Item" + Nil + "/><Map><a" + Nil + "/></Map><Tags><String" + Nil + "/></Tags></AnnotatedShapes>");
        }

        [TestMethod]
        public void Write_NullThatTheIgnoreConditionLeavesOut_IsNotRejected()
        {
            foreach (var condition in new[] { XmlIgnoreCondition.WhenWritingNull, XmlIgnoreCondition.WhenWritingDefault })
            {
                var options = new XmlSerializerOptions { OmitXmlDeclaration = true, RespectNullableAnnotations = true, DefaultIgnoreCondition = condition };
                XmlSerializer.Serialize(new Annotated { Name = null! }, options).Should().Be("<Annotated/>");
                XmlSerializer.Serialize(new AnnotatedShapes { Code = null!, Loose = null!, Map = null!, Tags = null! }, options).Should().Be("<AnnotatedShapes/>");
                Writing(new AnnotatedIgnored { Kept = null! }, options)
                    .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(Getting("Kept", "AnnotatedIgnored") + " Path: /AnnotatedIgnored.", "the condition on the member wins over the option");
            }

            XmlSerializer.Serialize(new AnnotatedIgnored { Defaulted = null!, Skipped = null! }, Respect).Should().Be("<AnnotatedIgnored><Kept>k</Kept></AnnotatedIgnored>");
        }

        #endregion

        #region Private Methods

        private static string Getting(string member, string type)
        {
            return "The property or field '" + member + "' on type '" + Types + type + "' doesn't allow getting null values." + Advice;
        }

        private static Action Reading<T>(string xml, XmlSerializerOptions options)
        {
            return () => XmlSerializer.Deserialize<T>(xml, options);
        }

        private static string Setting(string member, string type)
        {
            return "The property or field '" + member + "' on type '" + Types + type + "' doesn't allow setting null values." + Advice;
        }

        private static Action Writing<T>(T value, XmlSerializerOptions options)
        {
            return () => XmlSerializer.Serialize(value, options);
        }

        #endregion

    }

}
