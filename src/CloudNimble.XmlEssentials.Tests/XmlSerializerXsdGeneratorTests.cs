using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// XSD AdditionalFiles generator: POCOs, context, shared emit, XMLXE010–013.
    /// </summary>
    [TestClass]
    public class XmlSerializerXsdGeneratorTests
    {

        #region Fields

        /// <summary>
        /// The namespace of the serialization attributes and types, as symbols print it.
        /// </summary>
        internal const string S = "CloudNimble.Text.Xml.Serialization";

        internal const string OrderSchema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="urn:order" elementFormDefault="qualified">
              <xs:annotation>
                <xs:documentation>A purchase order.</xs:documentation>
              </xs:annotation>
              <xs:simpleType name="Status">
                <xs:restriction base="xs:string">
                  <xs:enumeration value="Open"/>
                  <xs:enumeration value="Closed"/>
                </xs:restriction>
              </xs:simpleType>
              <xs:element name="Order">
                <xs:complexType>
                  <xs:sequence>
                    <xs:element name="Id" type="xs:string"/>
                    <xs:element name="Status" type="Status"/>
                    <xs:element name="Qty" type="xs:int" nillable="true"/>
                    <xs:element name="Line" maxOccurs="unbounded">
                      <xs:complexType>
                        <xs:sequence>
                          <xs:element name="Sku" type="xs:string"/>
                          <xs:element name="Qty" type="xs:int"/>
                        </xs:sequence>
                      </xs:complexType>
                    </xs:element>
                    <xs:any minOccurs="0"/>
                  </xs:sequence>
                  <xs:attribute name="currency" type="xs:string"/>
                  <xs:anyAttribute/>
                </xs:complexType>
              </xs:element>
            </xs:schema>
            """;

        #endregion

        #region Public Methods

        [TestMethod]
        public void OrderSchema_EmitsPocosContextAndConverters()
        {
            var (compilation, diagnostics, generated) = GeneratorTestHost.Run(
                "public static class Marker { }",
                [new TestAdditionalText("order.xsd", OrderSchema)],
                null);
            diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error).Should().BeEmpty();
            XmlSerializerGeneratorTests.Declared(compilation, generated).Should().Equal(
                "enum XmlEssentials.Generated.Status",
                "  Open",
                "  Closed",
                "class XmlEssentials.Generated.Order_Line [" + S + ".XmlRootAttribute(\"Line\", Namespace = \"urn:order\")]",
                "  string? Sku",
                "  int Qty",
                "class XmlEssentials.Generated.Order [" + S + ".XmlRootAttribute(\"Order\", Namespace = \"urn:order\")]",
                "  string? currency [" + S + ".XmlAttributeAttribute(\"currency\")]",
                "  string? Id",
                "  XmlEssentials.Generated.Status Status",
                "  int? Qty",
                "  System.Collections.Generic.List<XmlEssentials.Generated.Order_Line>? Line",
                "  " + S + ".XmlExtensionData? Extra [" + S + ".XmlExtensionDataAttribute]",
                "class XmlEssentials.Generated.OrderXmlContext : XmlSerializerContext [" + S + ".XmlSerializableAttribute(typeof(XmlEssentials.Generated.Status)), " + S + ".XmlSerializableAttribute(typeof(XmlEssentials.Generated.Order_Line)), " + S + ".XmlSerializableAttribute(typeof(XmlEssentials.Generated.Order))]",
                "  " + S + ".XmlTypeInfo<XmlEssentials.Generated.Status> Status",
                "  " + S + ".XmlTypeInfo<XmlEssentials.Generated.Order_Line> Order_Line",
                "  " + S + ".XmlTypeInfo<XmlEssentials.Generated.Order> Order");
            compilation.GetDiagnostics().Where(d => d.Severity is DiagnosticSeverity.Error).Should().BeEmpty();
        }

        [TestMethod]
        public void OrderSchema_RoundTripsThroughGeneratedContext()
        {
            const string source = """
                using CloudNimble.Text.Xml;
                using XmlEssentials.Generated;

                public static class Use
                {
                    public static string Run()
                    {
                        var order = new Order
                        {
                            currency = "USD",
                            Id = "A-1",
                            Status = Status.Open,
                            Qty = 2,
                            Line = [new Order_Line { Sku = "sku", Qty = 1 }],
                        };
                        var xml = XmlSerializer.Serialize(order, OrderXmlContext.Default.Order);
                        var read = XmlSerializer.Deserialize(xml, OrderXmlContext.Default.Order);
                        return read!.Id + ":" + read.currency + ":" + read.Qty + ":" + read.Line![0].Sku + ":" + xml;
                    }
                }
                """;
            var (compilation, diagnostics, _) = GeneratorTestHost.Run(
                source,
                [new TestAdditionalText("order.xsd", OrderSchema)],
                null);
            diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error).Should().BeEmpty();
            var errors = compilation.GetDiagnostics().Where(d => d.Severity is DiagnosticSeverity.Error).ToArray();
            errors.Should().BeEmpty(string.Join("\n", errors.Select(d => d.ToString())));
            using var stream = new System.IO.MemoryStream();
            var emit = compilation.Emit(stream);
            emit.Success.Should().BeTrue(string.Join("\n", emit.Diagnostics.Select(d => d.ToString())));
            stream.Position = 0;
            var loadContext = new System.Runtime.Loader.AssemblyLoadContext("XsdOrder", isCollectible: true);
            var assembly = loadContext.LoadFromStream(stream);
            var result = (string)assembly.GetType("Use")!.GetMethod("Run")!.Invoke(null, null)!;
            result.Should().Be("A-1:USD:2:sku:<?xml version=\"1.0\" encoding=\"utf-8\"?><Order xmlns=\"urn:order\" currency=\"USD\"><Id>A-1</Id><Status>Open</Status><Qty>2</Qty><Line><Sku>sku</Sku><Qty>1</Qty></Line></Order>");
            loadContext.Unload();
        }

        [TestMethod]
        public void IncludeMergesTypesFromAnotherAdditionalFile()
        {
            const string types = """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="urn:order">
                  <xs:complexType name="Address">
                    <xs:sequence>
                      <xs:element name="City" type="xs:string"/>
                    </xs:sequence>
                  </xs:complexType>
                </xs:schema>
                """;
            const string order = """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="urn:order">
                  <xs:include schemaLocation="types.xsd"/>
                  <xs:element name="Customer">
                    <xs:complexType>
                      <xs:sequence>
                        <xs:element name="Home" type="Address"/>
                      </xs:sequence>
                    </xs:complexType>
                  </xs:element>
                </xs:schema>
                """;
            var (compilation, diagnostics, generated) = GeneratorTestHost.Run(
                "public static class Marker { }",
                [new TestAdditionalText("order.xsd", order), new TestAdditionalText("types.xsd", types)],
                null);
            diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error).Should().BeEmpty();
            XmlSerializerGeneratorTests.Declared(compilation, generated).Should().Equal(
                "class XmlEssentials.Generated.Customer [" + S + ".XmlRootAttribute(\"Customer\", Namespace = \"urn:order\")]",
                "  XmlEssentials.Generated.Address? Home",
                "class XmlEssentials.Generated.Address [" + S + ".XmlRootAttribute(\"Address\", Namespace = \"urn:order\")]",
                "  string? City",
                "class XmlEssentials.Generated.OrderXmlContext : XmlSerializerContext [" + S + ".XmlSerializableAttribute(typeof(XmlEssentials.Generated.Customer)), " + S + ".XmlSerializableAttribute(typeof(XmlEssentials.Generated.Address))]",
                "  " + S + ".XmlTypeInfo<XmlEssentials.Generated.Customer> Customer",
                "  " + S + ".XmlTypeInfo<XmlEssentials.Generated.Address> Address");
        }

        [TestMethod]
        public void MissingImport_ReportsXMLXE010()
        {
            const string schema = """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
                  <xs:import namespace="urn:other" schemaLocation="missing.xsd"/>
                  <xs:element name="Root">
                    <xs:complexType>
                      <xs:sequence>
                        <xs:element name="Id" type="xs:string"/>
                      </xs:sequence>
                    </xs:complexType>
                  </xs:element>
                </xs:schema>
                """;
            var (_, diagnostics, _) = GeneratorTestHost.Run(
                "public static class Marker { }",
                [new TestAdditionalText("root.xsd", schema)],
                null);
            diagnostics.Where(d => d.Id.StartsWith("XMLXE", System.StringComparison.Ordinal)).Select(d => d.Id).Should().Equal("XMLXE010");
        }

        [TestMethod]
        public void AbsoluteUriImport_ReportsXMLXE010()
        {
            const string schema = """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
                  <xs:include schemaLocation="https://example.com/other.xsd"/>
                  <xs:element name="Root">
                    <xs:complexType>
                      <xs:sequence>
                        <xs:element name="Id" type="xs:string"/>
                      </xs:sequence>
                    </xs:complexType>
                  </xs:element>
                </xs:schema>
                """;
            var (_, diagnostics, _) = GeneratorTestHost.Run(
                "public static class Marker { }",
                [new TestAdditionalText("root.xsd", schema)],
                null);
            diagnostics.Where(d => d.Id.StartsWith("XMLXE", System.StringComparison.Ordinal)).Select(d => d.Id).Should().Equal("XMLXE010");
        }

        [TestMethod]
        public void DtdInSchema_ReportsXMLXE010()
        {
            const string schema = """
                <?xml version="1.0"?>
                <!DOCTYPE schema [
                  <!ENTITY x "y">
                ]>
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
                  <xs:element name="Root">
                    <xs:complexType>
                      <xs:sequence>
                        <xs:element name="Id" type="xs:string"/>
                      </xs:sequence>
                    </xs:complexType>
                  </xs:element>
                </xs:schema>
                """;
            var (_, diagnostics, _) = GeneratorTestHost.Run(
                "public static class Marker { }",
                [new TestAdditionalText("root.xsd", schema)],
                null);
            diagnostics.Where(d => d.Id.StartsWith("XMLXE", System.StringComparison.Ordinal)).Select(d => d.Id).Should().Equal("XMLXE010");
        }

        [TestMethod]
        public void Redefine_ReportsXMLXE011()
        {
            const string schema = """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
                  <xs:redefine schemaLocation="other.xsd"/>
                  <xs:element name="Root">
                    <xs:complexType>
                      <xs:sequence>
                        <xs:element name="Id" type="xs:string"/>
                      </xs:sequence>
                    </xs:complexType>
                  </xs:element>
                </xs:schema>
                """;
            var (_, diagnostics, _) = GeneratorTestHost.Run(
                "public static class Marker { }",
                [new TestAdditionalText("root.xsd", schema)],
                null);
            diagnostics.Where(d => d.Id.StartsWith("XMLXE", System.StringComparison.Ordinal)).Select(d => d.Id).Should().Equal("XMLXE011");
        }

        [TestMethod]
        public void NameCollision_ReportsXMLXE012()
        {
            const string schema = """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
                  <xs:element name="A-B">
                    <xs:complexType>
                      <xs:sequence>
                        <xs:element name="Id" type="xs:string"/>
                      </xs:sequence>
                    </xs:complexType>
                  </xs:element>
                  <xs:element name="A_B">
                    <xs:complexType>
                      <xs:sequence>
                        <xs:element name="Id" type="xs:string"/>
                      </xs:sequence>
                    </xs:complexType>
                  </xs:element>
                </xs:schema>
                """;
            var (_, diagnostics, _) = GeneratorTestHost.Run(
                "public static class Marker { }",
                [new TestAdditionalText("root.xsd", schema)],
                null);
            diagnostics.Where(d => d.Id.StartsWith("XMLXE", System.StringComparison.Ordinal)).Select(d => d.Id).Should().Equal("XMLXE012");
        }

        [TestMethod]
        public void InvalidElementName_ReportsXMLXE013()
        {
            const string schema = """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
                  <xs:element name="1bad">
                    <xs:complexType>
                      <xs:sequence>
                        <xs:element name="Id" type="xs:string"/>
                      </xs:sequence>
                    </xs:complexType>
                  </xs:element>
                </xs:schema>
                """;
            var (_, diagnostics, _) = GeneratorTestHost.Run(
                "public static class Marker { }",
                [new TestAdditionalText("root.xsd", schema)],
                null);
            diagnostics.Where(d => d.Id.StartsWith("XMLXE", System.StringComparison.Ordinal)).Select(d => d.Id).Should().Equal("XMLXE013");
        }

        [TestMethod]
        public void MetadataOverridesNamespaceAndContextName()
        {
            var options = new TestAnalyzerConfigOptionsProvider(
                TestAnalyzerConfigOptions.Empty,
                new Dictionary<string, AnalyzerConfigOptions>
                {
                    ["order.xsd"] = new TestAnalyzerConfigOptions(new Dictionary<string, string>
                    {
                        ["build_metadata.AdditionalFiles.XmlEssentialsNamespace"] = "MyApp.Xml.Order",
                        ["build_metadata.AdditionalFiles.XmlEssentialsContext"] = "OrderXmlContext",
                    }),
                });
            var (compilation, diagnostics, generated) = GeneratorTestHost.Run(
                "public static class Marker { }",
                [new TestAdditionalText("order.xsd", OrderSchema)],
                options);
            diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error).Should().BeEmpty();
            XmlSerializerGeneratorTests.Declared(compilation, generated).Where(line => !line.StartsWith(' ')).Select(line => line.Split(' ')[0] + " " + line.Split(' ')[1]).Should().Equal(
                "enum MyApp.Xml.Order.Status",
                "class MyApp.Xml.Order.Order_Line",
                "class MyApp.Xml.Order.Order",
                "class MyApp.Xml.Order.OrderXmlContext");
        }

        [TestMethod]
        public void RootNamespace_SeedsGeneratedNamespace()
        {
            var options = new TestAnalyzerConfigOptionsProvider(
                new TestAnalyzerConfigOptions(new Dictionary<string, string>
                {
                    ["build_property.RootNamespace"] = "Contoso",
                }),
                new Dictionary<string, AnalyzerConfigOptions>());
            var (compilation, diagnostics, generated) = GeneratorTestHost.Run(
                "public static class Marker { }",
                [new TestAdditionalText("order.xsd", OrderSchema)],
                options);
            diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error).Should().BeEmpty();
            XmlSerializerGeneratorTests.Declared(compilation, generated).Where(line => !line.StartsWith(' ')).Select(line => line.Split(' ')[0] + " " + line.Split(' ')[1]).Should().Equal(
                "enum Contoso.Generated.Status",
                "class Contoso.Generated.Order_Line",
                "class Contoso.Generated.Order",
                "class Contoso.Generated.OrderXmlContext");
        }

        [TestMethod]
        public void WsdlAdditionalFile_IsIgnored()
        {
            const string wsdl = """
                <definitions xmlns="http://schemas.xmlsoap.org/wsdl/">
                  <message name="Hello"/>
                </definitions>
                """;
            var (compilation, diagnostics, generated) = GeneratorTestHost.Run(
                "public static class Marker { }",
                [new TestAdditionalText("service.wsdl", wsdl)],
                null);
            diagnostics.Where(d => d.Id.StartsWith("XMLXE")).Should().BeEmpty();
            generated.Should().BeEmpty();
        }

        #endregion

    }

}
