using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// What the XSD generator makes of each schema construct: the classes it declares, the documents those classes
    /// read and write, and what it reports about the constructs it does not map.
    /// </summary>
    [TestClass]
    public class XmlSerializerXsdMappingTests
    {

        #region Fields

        private const string S = XmlSerializerXsdGeneratorTests.S;

        /// <summary>
        /// The namespace of the generated types, as symbols print it.
        /// </summary>
        private const string G = "XmlEssentials.Generated.";

        /// <summary>
        /// The attribute on an element that is not nillable, so that a null is left out and not written as nil.
        /// </summary>
        private const string Ignore = S + ".XmlIgnoreAttribute(Condition = " + S + ".XmlIgnoreCondition.WhenWritingNull)";

        private const string Xsi = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"";

        private const string XsiType = S + ".XmlPolymorphicAttribute(DiscriminatorPlacement = CloudNimble.Text.Xml.XmlDiscriminatorPlacement.XsiType)";

        /// <summary>
        /// A schema with a base type that is abstract, two levels of extension, a named group, an attribute group,
        /// a global attribute, a named simple type that is not an enumeration, an enumeration whose value is not a
        /// C# identifier, a wrapper element, and members that are optional, nillable, or both.
        /// </summary>
        private const string ShapesSchema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:s="urn:shapes" targetNamespace="urn:shapes" elementFormDefault="qualified">
              <xs:simpleType name="Mode">
                <xs:restriction base="xs:string">
                  <xs:enumeration value="next-day"/>
                  <xs:enumeration value="Plain"/>
                </xs:restriction>
              </xs:simpleType>
              <xs:simpleType name="Percent">
                <xs:restriction base="xs:int">
                  <xs:minInclusive value="0"/>
                </xs:restriction>
              </xs:simpleType>
              <xs:attribute name="unit" type="xs:string"/>
              <xs:attributeGroup name="Audit">
                <xs:attribute name="by" type="xs:string"/>
                <xs:attribute name="rev" type="xs:int"/>
              </xs:attributeGroup>
              <xs:group name="Names">
                <xs:sequence>
                  <xs:element name="Title" type="xs:string"/>
                  <xs:element name="Alias" type="xs:string" minOccurs="0"/>
                </xs:sequence>
              </xs:group>
              <xs:complexType name="Shape" abstract="true">
                <xs:annotation>
                  <xs:documentation>Something that is drawn.</xs:documentation>
                </xs:annotation>
                <xs:sequence>
                  <xs:element name="Name" type="xs:string"/>
                </xs:sequence>
                <xs:attribute name="id" type="xs:int" use="required"/>
              </xs:complexType>
              <xs:complexType name="Circle">
                <xs:complexContent>
                  <xs:extension base="s:Shape">
                    <xs:sequence>
                      <xs:element name="Radius" type="xs:int"/>
                    </xs:sequence>
                    <xs:attribute name="fill" type="xs:string"/>
                  </xs:extension>
                </xs:complexContent>
              </xs:complexType>
              <xs:complexType name="Ring">
                <xs:complexContent>
                  <xs:extension base="s:Circle">
                    <xs:sequence>
                      <xs:element name="Inner" type="xs:int" minOccurs="0"/>
                    </xs:sequence>
                  </xs:extension>
                </xs:complexContent>
              </xs:complexType>
              <xs:element name="drawing" type="s:Drawing"/>
              <xs:complexType name="Drawing">
                <xs:sequence>
                  <xs:group ref="s:Names"/>
                  <xs:element name="Main" type="s:Shape"/>
                  <xs:element name="Extra" type="s:Circle" minOccurs="0" maxOccurs="unbounded"/>
                  <xs:element name="Opacity" type="s:Percent" minOccurs="0"/>
                  <xs:element name="Mode" type="s:Mode" minOccurs="0"/>
                  <xs:element name="Tags">
                    <xs:complexType>
                      <xs:sequence>
                        <xs:element name="Tag" type="xs:string" minOccurs="0" maxOccurs="unbounded"/>
                      </xs:sequence>
                    </xs:complexType>
                  </xs:element>
                  <xs:element name="Note" type="xs:string" nillable="true"/>
                  <xs:element name="Count" type="xs:int" nillable="true" minOccurs="0"/>
                  <xs:element name="Size">
                    <xs:simpleType>
                      <xs:restriction base="xs:decimal"/>
                    </xs:simpleType>
                  </xs:element>
                </xs:sequence>
                <xs:attribute ref="s:unit"/>
                <xs:attribute name="scale" type="xs:double"/>
                <xs:attributeGroup ref="s:Audit"/>
              </xs:complexType>
            </xs:schema>
            """;

        #endregion

        #region Public Methods

        [TestMethod]
        public void ShapesSchema_DeclaresClassesForTypes_WithInheritanceAndOptionalMembers()
        {
            var (declared, diagnostics) = Generate(ShapesSchema, "shapes.xsd");

            diagnostics.Should().BeEmpty();
            declared.Should().Equal(
                "enum " + G + "Mode",
                "  next_day [" + S + ".XmlPropertyNameAttribute(\"next-day\")]",
                "  Plain",
                "class " + G + "Drawing [" + S + ".XmlRootAttribute(\"drawing\", Namespace = \"urn:shapes\")]",
                "  string? unit [" + S + ".XmlAttributeAttribute(\"unit\")]",
                "  double? scale [" + S + ".XmlAttributeAttribute(\"scale\")]",
                "  string? by [" + S + ".XmlAttributeAttribute(\"by\")]",
                "  int? rev [" + S + ".XmlAttributeAttribute(\"rev\")]",
                "  string? Title [" + Ignore + "]",
                "  string? Alias [" + Ignore + "]",
                "  " + G + "Shape? Main [" + Ignore + "]",
                "  System.Collections.Generic.List<" + G + "Circle>? Extra",
                "  int? Opacity [" + Ignore + "]",
                "  " + G + "Mode? Mode [" + Ignore + "]",
                "  System.Collections.Generic.List<string>? Tags [" + S + ".XmlArrayAttribute(\"Tags\"), " + S + ".XmlArrayItemAttribute(\"Tag\"), " + Ignore + "]",
                "  string? Note",
                "  int? Count",
                "  decimal Size",
                "class " + G + "Shape [" + S + ".XmlRootAttribute(\"Shape\", Namespace = \"urn:shapes\"), " + XsiType + ", " + S + ".XmlDerivedTypeAttribute<" + G + "Circle>(\"Circle\"), " + S + ".XmlDerivedTypeAttribute<" + G + "Ring>(\"Ring\")]",
                "  int id [" + S + ".XmlAttributeAttribute(\"id\")]",
                "  string? Name [" + Ignore + "]",
                "class " + G + "Circle : Shape [" + S + ".XmlRootAttribute(\"Circle\", Namespace = \"urn:shapes\"), " + XsiType + ", " + S + ".XmlDerivedTypeAttribute<" + G + "Ring>(\"Ring\")]",
                "  string? fill [" + S + ".XmlAttributeAttribute(\"fill\")]",
                "  int Radius",
                "class " + G + "Ring : Circle [" + S + ".XmlRootAttribute(\"Ring\", Namespace = \"urn:shapes\")]",
                "  int? Inner [" + Ignore + "]",
                "class " + G + "ShapesXmlContext : XmlSerializerContext [" + string.Join(", ", new[] { "Mode", "Drawing", "Shape", "Circle", "Ring" }.Select(name => S + ".XmlSerializableAttribute<" + G + name + ">")) + "]",
                "  " + S + ".XmlTypeInfo<" + G + "Mode> Mode",
                "  " + S + ".XmlTypeInfo<" + G + "Drawing> Drawing",
                "  " + S + ".XmlTypeInfo<" + G + "Shape> Shape",
                "  " + S + ".XmlTypeInfo<" + G + "Circle> Circle",
                "  " + S + ".XmlTypeInfo<" + G + "Ring> Ring");
        }

        [TestMethod]
        public void ShapesSchema_ReadsASchemaValidDocument_AndWritesItBack_AsReflectionDoes()
        {
            Execute(ShapesSchema, "shapes.xsd", """
                using System;
                using System.Linq;
                using CloudNimble.Text.Xml;
                using XmlEssentials.Generated;

                public static class Use
                {
                    public static string Run()
                    {
                        // A document as another producer writes it: a prefix for the namespace, and the same prefix
                        // in the value of xsi:type.
                        const string document =
                            "<d:drawing xmlns:d=\"urn:shapes\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" unit=\"mm\" scale=\"1.5\" rev=\"2\">" +
                            "<d:Title>t</d:Title>" +
                            "<d:Main xsi:type=\"d:Ring\" id=\"1\"><d:Name>m</d:Name><d:Radius>3</d:Radius><d:Inner>1</d:Inner></d:Main>" +
                            "<d:Extra id=\"2\" fill=\"red\"><d:Name>c</d:Name><d:Radius>4</d:Radius></d:Extra>" +
                            "<d:Extra xsi:type=\"d:Ring\" id=\"3\"><d:Name>r</d:Name><d:Radius>5</d:Radius></d:Extra>" +
                            "<d:Mode>next-day</d:Mode>" +
                            "<d:Tags><d:Tag>a</d:Tag><d:Tag>b</d:Tag></d:Tags>" +
                            "<d:Note xsi:nil=\"true\"/>" +
                            "<d:Size>2.50</d:Size>" +
                            "</d:drawing>";
                        var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultNamespace = "urn:shapes" };
                        var context = new ShapesXmlContext(options);
                        var read = XmlSerializer.Deserialize(document, context.Drawing)!;
                        var main = (Ring)read.Main!;
                        var shown = string.Join("|",
                            read.unit, read.scale, read.by ?? "null", read.rev, read.Title, read.Alias ?? "null",
                            main.id + ":" + main.Name + ":" + main.Radius + ":" + main.Inner,
                            string.Join(",", read.Extra!.Select(e => e.GetType().Name + ":" + e.id + ":" + (e.fill ?? "null") + ":" + e.Radius + ":" + ((e as Ring)?.Inner.ToString() ?? "-"))),
                            read.Opacity?.ToString() ?? "null", read.Mode, string.Join(",", read.Tags!), read.Note ?? "null", read.Count?.ToString() ?? "null", read.Size);
                        var written = XmlSerializer.Serialize(read, context.Drawing);
                        var reflected = XmlSerializer.Serialize(read, new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultNamespace = "urn:shapes" });
                        var again = XmlSerializer.Serialize(XmlSerializer.Deserialize(written, context.Drawing)!, context.Drawing);
                        return shown + "\n" + written + "\n" + (written == reflected ? "same" : "reflection: " + reflected) + "\n" + (written == again ? "stable" : "again: " + again);
                    }
                }
                """).Should().Be(
                    "mm|1.5|null|2|t|null|1:m:3:1|Circle:2:red:4:-,Ring:3:null:5:|null|next_day|a,b|null|null|2.50\n" +
                    "<drawing xmlns=\"urn:shapes\" unit=\"mm\" scale=\"1.5\" rev=\"2\"><Title>t</Title>" +
                    "<Main" + Xsi + " xsi:type=\"Ring\" id=\"1\"><Name>m</Name><Radius>3</Radius><Inner>1</Inner></Main>" +
                    "<Extra" + Xsi + " xsi:type=\"Circle\" id=\"2\" fill=\"red\"><Name>c</Name><Radius>4</Radius></Extra>" +
                    "<Extra" + Xsi + " xsi:type=\"Ring\" id=\"3\"><Name>r</Name><Radius>5</Radius></Extra>" +
                    "<Mode>next-day</Mode><Tags><Tag>a</Tag><Tag>b</Tag></Tags>" +
                    "<Note" + Xsi + " xsi:nil=\"true\"/><Count" + Xsi + " xsi:nil=\"true\"/><Size>2.50</Size></drawing>\n" +
                    "same\n" +
                    "stable");
        }

        [TestMethod]
        public void LongSequence_KeepsTheOrderOfTheSchema()
        {
            var elements = string.Join("", Enumerable.Range(1, 24).Select(i => "<xs:element name=\"E" + i + "\" type=\"xs:int\"/>"));
            var (declared, diagnostics) = Generate(
                "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"><xs:element name=\"Row\"><xs:complexType><xs:sequence>" + elements
                + "</xs:sequence><xs:attribute name=\"a\" type=\"xs:string\"/></xs:complexType></xs:element></xs:schema>",
                "row.xsd");

            diagnostics.Should().BeEmpty();
            declared.Take(26).Should().Equal(
                new[] { "class XmlEssentials.Generated.Row [" + S + ".XmlRootAttribute(\"Row\")]", "  string? a [" + S + ".XmlAttributeAttribute(\"a\")]" }
                    .Concat(Enumerable.Range(1, 24).Select(i => "  int E" + i)));
        }

        [TestMethod]
        public void UnmappedConstructs_AreReported_AndTheRestIsGenerated()
        {
            const string schema = """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:t="urn:t" targetNamespace="urn:t">
                  <xs:element name="shape" type="t:Shape"/>
                  <xs:element name="figure" type="t:Shape"/>
                  <xs:element name="circle" type="t:Circle" substitutionGroup="t:shape"/>
                  <xs:complexType name="Shape">
                    <xs:sequence>
                      <xs:element name="Name" type="xs:string" default="none"/>
                    </xs:sequence>
                  </xs:complexType>
                  <xs:complexType name="Circle">
                    <xs:complexContent>
                      <xs:restriction base="t:Shape">
                        <xs:sequence>
                          <xs:element name="Name" type="xs:string"/>
                        </xs:sequence>
                      </xs:restriction>
                    </xs:complexContent>
                  </xs:complexType>
                  <xs:complexType name="Page" mixed="true">
                    <xs:sequence>
                      <xs:element ref="t:shape"/>
                      <xs:element ref="t:missing"/>
                      <xs:choice>
                        <xs:element name="Card" type="xs:string"/>
                        <xs:element name="Cash" type="xs:int"/>
                      </xs:choice>
                      <xs:sequence maxOccurs="unbounded">
                        <xs:element name="Key" type="xs:string"/>
                        <xs:element name="Value" type="xs:int"/>
                      </xs:sequence>
                      <xs:group ref="t:Gone"/>
                    </xs:sequence>
                    <xs:attribute name="version" type="xs:int" fixed="2"/>
                    <xs:attribute name="skip" type="xs:int" use="prohibited"/>
                    <xs:attribute ref="t:absent"/>
                    <xs:attribute ref="xml:lang"/>
                    <xs:attributeGroup ref="t:Lost"/>
                  </xs:complexType>
                  <xs:complexType name="Foreign">
                    <xs:complexContent>
                      <xs:extension base="t:Elsewhere">
                        <xs:sequence>
                          <xs:element name="Own" type="xs:string"/>
                        </xs:sequence>
                      </xs:extension>
                    </xs:complexContent>
                  </xs:complexType>
                </xs:schema>
                """;

            var (declared, diagnostics) = Generate(schema, "t.xsd");

            diagnostics.Should().Equal(
                "Warning XMLESS018: The elements 'shape', 'figure' share the type 'Shape'. The generated class has one root name, 'shape'.",
                "Info XMLESS011: The default value 'none' of 'Name' is not applied to the generated member.",
                "Warning XMLESS018: Element 'circle' is in the substitution group of 'shape'. Substitution groups are not mapped, so it is not read or written in place of 'shape'.",
                "Info XMLESS011: 'Circle' restricts 'Shape'. It is mapped as a class derived from it, and what it restricts is not enforced.",
                "Warning XMLESS018: The type of 'Page' has mixed content. The text between its elements is not read or written.",
                "Warning XMLESS018: The element 'shape' that 'Page' refers to heads a substitution group. Substitution groups are not mapped, so only 'shape' itself is read and written there.",
                "Warning XMLESS018: The element 'missing' that 'Page' refers to is not in the schema. It is mapped as a string.",
                "Info XMLESS011: xs:choice is mapped as optional members.",
                "Warning XMLESS018: A repeating xs:sequence in 'Page' is mapped as members that each repeat. The order between them is not kept.",
                "Warning XMLESS018: The group 'Gone' that 'Page' refers to is not in the schema, so its elements are not generated.",
                "Info XMLESS011: The fixed value '2' of 'version' is not applied to the generated member.",
                "Warning XMLESS018: The attribute 'absent' that a ref names is not in the schema. It is mapped as a string.",
                "Warning XMLESS018: The attribute group 'Lost' that 'Page' refers to is not in the schema, so its attributes are not generated.",
                "Warning XMLESS018: The base type 'Elsewhere' of 'Foreign' is not a complex type of the schema, so its members are not generated.",
                "Warning XMLESS018: Schema 't.xsd' has a target namespace and local elements that are in no namespace (elementFormDefault is not \"qualified\"). The generated types write them in the target namespace.");
            declared.Should().Equal(
                "class " + G + "Shape [" + S + ".XmlRootAttribute(\"shape\", Namespace = \"urn:t\"), " + XsiType + ", " + S + ".XmlDerivedTypeAttribute<" + G + "Circle>(\"Circle\")]",
                "  string? Name [" + Ignore + "]",
                "class " + G + "Circle : Shape [" + S + ".XmlRootAttribute(\"circle\", Namespace = \"urn:t\")]",
                "class " + G + "Page [" + S + ".XmlRootAttribute(\"Page\", Namespace = \"urn:t\")]",
                "  int? version [" + S + ".XmlAttributeAttribute(\"version\")]",
                "  string? absent [" + S + ".XmlAttributeAttribute(\"absent\")]",
                "  string? lang [" + S + ".XmlAttributeAttribute(\"lang\", Namespace = \"http://www.w3.org/XML/1998/namespace\")]",
                "  " + G + "Shape? shape [" + Ignore + "]",
                "  string? missing [" + Ignore + "]",
                "  string? Card [" + Ignore + "]",
                "  int? Cash [" + Ignore + "]",
                "  System.Collections.Generic.List<string>? Key",
                "  System.Collections.Generic.List<int>? Value",
                "class " + G + "Foreign [" + S + ".XmlRootAttribute(\"Foreign\", Namespace = \"urn:t\")]",
                "  string? Own [" + Ignore + "]",
                "class " + G + "TXmlContext : XmlSerializerContext [" + string.Join(", ", new[] { "Shape", "Circle", "Page", "Foreign" }.Select(name => S + ".XmlSerializableAttribute<" + G + name + ">")) + "]",
                "  " + S + ".XmlTypeInfo<" + G + "Shape> Shape",
                "  " + S + ".XmlTypeInfo<" + G + "Circle> Circle",
                "  " + S + ".XmlTypeInfo<" + G + "Page> Page",
                "  " + S + ".XmlTypeInfo<" + G + "Foreign> Foreign");
        }

        [TestMethod]
        public void NameCollisions_InsideAClassAndAnEnumeration_AreReported()
        {
            const string schema = """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
                  <xs:simpleType name="Kind">
                    <xs:restriction base="xs:string">
                      <xs:enumeration value="a-b"/>
                      <xs:enumeration value="a_b"/>
                    </xs:restriction>
                  </xs:simpleType>
                  <xs:element name="Item">
                    <xs:complexType>
                      <xs:sequence>
                        <xs:element name="id" type="xs:string"/>
                        <xs:element name="Item" type="xs:string"/>
                      </xs:sequence>
                      <xs:attribute name="id" type="xs:int"/>
                    </xs:complexType>
                  </xs:element>
                  <xs:complexType name="Item">
                    <xs:sequence>
                      <xs:element name="Other" type="xs:string"/>
                    </xs:sequence>
                  </xs:complexType>
                </xs:schema>
                """;

            var (declared, diagnostics) = Generate(schema, "c.xsd");

            diagnostics.Should().Equal(
                "Error XMLESS012: The generated C# name 'Kind.a_b' is used more than once",
                "Error XMLESS012: The generated C# name 'Item.Item' is used more than once",
                "Error XMLESS012: The generated C# name 'Item.id' is used more than once",
                "Error XMLESS012: The generated C# name 'Item' is used more than once");
            declared.Should().Equal(
                "enum " + G + "Kind",
                "  a_b [" + S + ".XmlPropertyNameAttribute(\"a-b\")]",
                "class " + G + "Item [" + S + ".XmlRootAttribute(\"Item\")]",
                "  string? id [" + Ignore + "]",
                "class " + G + "CXmlContext : XmlSerializerContext [" + S + ".XmlSerializableAttribute<" + G + "Kind>, " + S + ".XmlSerializableAttribute<" + G + "Item>]",
                "  " + S + ".XmlTypeInfo<" + G + "Kind> Kind",
                "  " + S + ".XmlTypeInfo<" + G + "Item> Item");
        }

        [TestMethod]
        public void TypesAndGroupsThatReferToThemselves_AreGeneratedWithoutFollowingTheCircle()
        {
            // No schema may say any of this, and a schema that is being edited may say it for a while. The build
            // must end, with classes that compile.
            const string schema = """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
                  <xs:simpleType name="Loop">
                    <xs:restriction base="Loop"/>
                  </xs:simpleType>
                  <xs:group name="G">
                    <xs:sequence>
                      <xs:element name="InGroup" type="Loop"/>
                      <xs:group ref="G"/>
                    </xs:sequence>
                  </xs:group>
                  <xs:attributeGroup name="AG">
                    <xs:attribute name="inGroup" type="xs:int"/>
                    <xs:attributeGroup ref="AG"/>
                  </xs:attributeGroup>
                  <xs:complexType name="A">
                    <xs:complexContent>
                      <xs:extension base="B">
                        <xs:sequence>
                          <xs:group ref="G"/>
                        </xs:sequence>
                        <xs:attributeGroup ref="AG"/>
                      </xs:extension>
                    </xs:complexContent>
                  </xs:complexType>
                  <xs:complexType name="B">
                    <xs:complexContent>
                      <xs:extension base="A">
                        <xs:sequence>
                          <xs:element name="Own" type="A" minOccurs="0"/>
                        </xs:sequence>
                      </xs:extension>
                    </xs:complexContent>
                  </xs:complexType>
                </xs:schema>
                """;

            var (declared, diagnostics) = Generate(schema, "loop.xsd");

            diagnostics.Should().BeEmpty();
            declared.Where(line => !line.StartsWith(' ')).Select(line => line.Split('[')[0].TrimEnd()).Should().Equal(
                "class " + G + "B",
                "class " + G + "A : B",
                "class " + G + "LoopXmlContext : XmlSerializerContext");
            declared.Where(line => line.StartsWith(' ') && !line.Contains("XmlTypeInfo")).Select(line => line.Split('[')[0].TrimEnd()).Should().Equal(
                "  " + G + "A? Own",
                "  int? inGroup",
                "  string? InGroup");
        }

        [TestMethod]
        public void UnqualifiedLocalElements_InATargetNamespace_AreReported()
        {
            const string schema = """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="urn:u">
                  <xs:element name="Order">
                    <xs:complexType>
                      <xs:sequence>
                        <xs:element name="Id" type="xs:string"/>
                      </xs:sequence>
                    </xs:complexType>
                  </xs:element>
                </xs:schema>
                """;

            Generate(schema, "u.xsd").Diagnostics.Should().Equal(
                "Warning XMLESS018: Schema 'u.xsd' has a target namespace and local elements that are in no namespace (elementFormDefault is not \"qualified\"). The generated types write them in the target namespace.");
        }

        [TestMethod]
        public void ReferenceToAGlobalElement_UsesTheClassOfThatElement()
        {
            const string schema = """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
                  <xs:element name="Line">
                    <xs:complexType>
                      <xs:sequence>
                        <xs:element name="Sku" type="xs:string"/>
                      </xs:sequence>
                    </xs:complexType>
                  </xs:element>
                  <xs:element name="Order">
                    <xs:complexType>
                      <xs:sequence>
                        <xs:element ref="Line" maxOccurs="unbounded"/>
                        <xs:element name="Lines">
                          <xs:complexType>
                            <xs:sequence>
                              <xs:element ref="Line" maxOccurs="unbounded"/>
                            </xs:sequence>
                          </xs:complexType>
                        </xs:element>
                      </xs:sequence>
                    </xs:complexType>
                  </xs:element>
                </xs:schema>
                """;

            var (declared, diagnostics) = Generate(schema, "r.xsd");

            diagnostics.Should().BeEmpty();
            declared.Should().Equal(
                "class XmlEssentials.Generated.Line [" + S + ".XmlRootAttribute(\"Line\")]",
                "  string? Sku [" + Ignore + "]",
                "class XmlEssentials.Generated.Order [" + S + ".XmlRootAttribute(\"Order\")]",
                "  System.Collections.Generic.List<XmlEssentials.Generated.Line>? Line",
                "  System.Collections.Generic.List<XmlEssentials.Generated.Line>? Lines [" + S + ".XmlArrayAttribute(\"Lines\"), " + S + ".XmlArrayItemAttribute(\"Line\"), " + Ignore + "]",
                "class XmlEssentials.Generated.RXmlContext : XmlSerializerContext [" + S + ".XmlSerializableAttribute<XmlEssentials.Generated.Line>, " + S + ".XmlSerializableAttribute<XmlEssentials.Generated.Order>]",
                "  " + S + ".XmlTypeInfo<XmlEssentials.Generated.Line> Line",
                "  " + S + ".XmlTypeInfo<XmlEssentials.Generated.Order> Order");
            Execute(schema, "r.xsd", """
                using CloudNimble.Text.Xml;
                using XmlEssentials.Generated;

                public static class Use
                {
                    public static string Run()
                    {
                        const string document = "<Order><Line><Sku>a</Sku></Line><Line><Sku>b</Sku></Line><Lines><Line><Sku>c</Sku></Line></Lines></Order>";
                        var context = new RXmlContext(new XmlSerializerOptions { OmitXmlDeclaration = true });
                        var read = XmlSerializer.Deserialize(document, context.Order)!;
                        return read.Line!.Count + "," + read.Lines![0].Sku + "," + (XmlSerializer.Serialize(read, context.Order) == document);
                    }
                }
                """).Should().Be("2,c,True");
        }

        #endregion

        #region Private Methods

        private static string Execute(string schema, string fileName, string source)
        {
            var (compilation, diagnostics, _) = GeneratorTestHost.Run(source, [new TestAdditionalText(fileName, schema)], null);
            diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error).Should().BeEmpty();
            var errors = compilation.GetDiagnostics().Where(d => d.Severity is DiagnosticSeverity.Error).ToArray();
            errors.Should().BeEmpty(string.Join("\n", errors.Select(d => d.ToString())));
            using var stream = new System.IO.MemoryStream();
            var emit = compilation.Emit(stream);
            emit.Success.Should().BeTrue(string.Join("\n", emit.Diagnostics.Select(d => d.ToString())));
            stream.Position = 0;
            var loadContext = new System.Runtime.Loader.AssemblyLoadContext("Xsd" + fileName, isCollectible: true);
            try
            {
                var assembly = loadContext.LoadFromStream(stream);
                return (string)assembly.GetType("Use")!.GetMethod("Run")!.Invoke(null, null)!;
            }
            finally
            {
                loadContext.Unload();
            }
        }

        private static (List<string> Declared, List<string> Diagnostics) Generate(string schema, string fileName)
        {
            var (compilation, diagnostics, generated) = GeneratorTestHost.Run("public static class Marker { }", [new TestAdditionalText(fileName, schema)], null);
            var errors = compilation.GetDiagnostics().Where(d => d.Severity is DiagnosticSeverity.Error).ToArray();
            errors.Should().BeEmpty(string.Join("\n", errors.Select(d => d.ToString())));
            return (
                XmlSerializerGeneratorTests.Declared(compilation, generated),
                diagnostics.Where(d => d.Id.StartsWith("XMLESS", System.StringComparison.Ordinal))
                    .Select(d => d.Severity + " " + d.Id + ": " + d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).ToList());
        }

        #endregion

    }

}