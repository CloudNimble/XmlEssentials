using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A constructor parameter fills the member with the same name, whatever the case of either, as it does in
    /// <c>System.Text.Json</c>. <see cref="XmlSerializerOptions.PropertyNameCaseInsensitive"/> is about names
    /// in the document and has no part in it.
    /// </summary>
    [TestClass]
    public class XmlSerializerConstructorCaseTests
    {

        #region Fields

        private static readonly XmlSerializerOptions Insensitive = new() { OmitXmlDeclaration = true, PropertyNameCaseInsensitive = true };
        private static readonly XmlSerializerOptions Sensitive = new() { OmitXmlDeclaration = true };

        #endregion

        #region Public Methods

        [TestMethod]
        public void CamelCaseParameters_FillPascalCaseMembers()
        {
            const string xml = "<CamelCtor><Name>Voltage</Name><Value>230</Value></CamelCtor>";
            foreach (var options in new[] { Sensitive, Insensitive })
            {
                XmlSerializer.Serialize(new CamelCtor("Voltage", 230), options).Should().Be(xml);
                var read = XmlSerializer.Deserialize<CamelCtor>(xml, options)!;
                read.Name.Should().Be("Voltage");
                read.Value.Should().Be(230);
            }
        }

        [TestMethod]
        public void CamelCaseParameters_OnTheMarkedConstructor_FillPascalCaseMembers()
        {
            const string xml = "<CamelMarkedCtor><First>4</First><Second>9</Second></CamelMarkedCtor>";
            foreach (var options in new[] { Sensitive, Insensitive })
            {
                XmlSerializer.Serialize(new CamelMarkedCtor(4, 9), options).Should().Be(xml);
                var read = XmlSerializer.Deserialize<CamelMarkedCtor>(xml, options)!;
                read.CreatedBy.Should().Be("marked");
                read.First.Should().Be(4);
                read.Second.Should().Be(5, "Second has no setter, so only the constructor could have set it");
            }
        }

        [TestMethod]
        public void CamelCaseParameter_NextToAParameterlessConstructor_IsNotTheConstructorUsed()
        {
            // A public parameterless constructor is used when a type has one, however well the parameters of
            // another constructor name the members. The member is set through its setter.
            foreach (var options in new[] { Sensitive, Insensitive })
            {
                var read = XmlSerializer.Deserialize<CamelOrParameterless>("<CamelOrParameterless><Name>Ada</Name></CamelOrParameterless>", options)!;
                read.Name.Should().Be("Ada");
                read.CreatedBy.Should().Be("parameterless");
            }
        }

        [TestMethod]
        public void CamelCaseParameter_WhenTheDocumentLeavesTheMemberOut_IsRequired()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<CamelCtor>("<CamelCtor><Name>Voltage</Name></CamelCtor>", Sensitive))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The required constructor parameter 'Value' is missing. Path: /CamelCtor | LineNumber: 1 | BytePositionInLine: 44.");
        }

        [TestMethod]
        public void MembersThatDifferOnlyByCase_EachTakeTheParameterSpelledLikeThem()
        {
            const string xml = "<CaseTwinsCtor><Name>upper</Name><name>lower</name></CaseTwinsCtor>";
            XmlSerializer.Serialize(new CaseTwinsCtor("lower", "upper"), Sensitive).Should().Be(xml);
            var read = XmlSerializer.Deserialize<CaseTwinsCtor>(xml, Sensitive)!;
            read.Name.Should().Be("upper");
            read.name.Should().Be("lower");
        }

        [TestMethod]
        public void ParameterNamedLikeTheXmlName_InAnotherCase_FillsTheMember()
        {
            const string xml = "<XmlNamedCtor><title>Dune</title></XmlNamedCtor>";
            foreach (var options in new[] { Sensitive, Insensitive })
            {
                XmlSerializer.Serialize(new XmlNamedCtor("Dune"), options).Should().Be(xml);
                XmlSerializer.Deserialize<XmlNamedCtor>(xml, options)!.Heading.Should().Be("Dune");
            }
        }

        [TestMethod]
        public void PropertyNameCaseInsensitive_StillDecidesHowDocumentNamesMatch()
        {
            const string lower = "<CamelCtor><name>Voltage</name><value>230</value></CamelCtor>";
            var read = XmlSerializer.Deserialize<CamelCtor>(lower, Insensitive)!;
            read.Name.Should().Be("Voltage");
            read.Value.Should().Be(230);
            FluentActions.Invoking(() => XmlSerializer.Deserialize<CamelCtor>(lower, Sensitive))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The required constructor parameter 'Name' is missing. Path: /CamelCtor | LineNumber: 1 | BytePositionInLine: 62.");
        }

        #endregion

    }

}
