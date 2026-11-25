using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Built-in <see cref="XmlNamingPolicy"/> conversions.
    /// </summary>
    [TestClass]
    public class XmlNamingPolicyTests
    {

        #region Public Methods

        [TestMethod]
        public void CamelCase_LowercasesFirstCharacter()
        {
            XmlNamingPolicy.CamelCase.ConvertName("Int32").Should().Be("int32");
            XmlNamingPolicy.CamelCase.ConvertName("already").Should().Be("already");
            XmlNamingPolicy.CamelCase.ConvertName("").Should().Be("");
        }

        [TestMethod]
        public void PascalCase_UppercasesFirstCharacter()
        {
            XmlNamingPolicy.PascalCase.ConvertName("int32").Should().Be("Int32");
            XmlNamingPolicy.PascalCase.ConvertName("Already").Should().Be("Already");
        }

        [TestMethod]
        public void SnakeAndKebab_InsertSeparatorsAtWordBoundaries()
        {
            XmlNamingPolicy.SnakeCaseLower.ConvertName("PropertyName").Should().Be("property_name");
            XmlNamingPolicy.SnakeCaseUpper.ConvertName("PropertyName").Should().Be("PROPERTY_NAME");
            XmlNamingPolicy.KebabCaseLower.ConvertName("PropertyName").Should().Be("property-name");
            XmlNamingPolicy.KebabCaseUpper.ConvertName("PropertyName").Should().Be("PROPERTY-NAME");
            XmlNamingPolicy.SnakeCaseLower.ConvertName("XMLHttp").Should().Be("xml_http");
            XmlNamingPolicy.SnakeCaseLower.ConvertName("Int32").Should().Be("int32");
            XmlNamingPolicy.KebabCaseLower.ConvertName("XMLHttp").Should().Be("xml-http");
            XmlNamingPolicy.PascalCase.ConvertName("").Should().Be("");
            XmlNamingPolicy.SnakeCaseLower.ConvertName("").Should().Be("");
            XmlNamingPolicy.KebabCaseUpper.ConvertName("").Should().Be("");

            var longName = new string('a', 40) + "B" + new string('c', 40);
            XmlNamingPolicy.SnakeCaseLower.ConvertName(longName).Should().Be(new string('a', 40) + "_b" + new string('c', 40));
            XmlNamingPolicy.KebabCaseUpper.ConvertName(longName).Should().Be(new string('A', 40) + "-B" + new string('C', 40));
        }

        #endregion

    }

}
