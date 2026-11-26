using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// What the XSD generator makes of the constructs that say which elements may appear and where: choices,
    /// groups that repeat, substitution groups, mixed content, element form, and default values.
    /// </summary>
    [TestClass]
    public class XmlSerializerXsdStructureTests
    {

        #region Fields

        private const string S = XmlSerializerXsdGeneratorTests.S;

        private const string G = "XmlEssentials.Generated.";

        private const string Ignore = S + ".XmlIgnoreAttribute(Condition = " + S + ".XmlIgnoreCondition.WhenWritingNull)";

        /// <summary>
        /// A schema with a substitution group, an abstract element with one substitute, a choice, a choice that
        /// repeats, a sequence that repeats, a type with mixed content, and default and fixed values.
        /// </summary>
        private const string PagesSchema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:p="urn:pages" targetNamespace="urn:pages" elementFormDefault="qualified">
              <xs:simpleType name="Tone">
                <xs:restriction base="xs:string">
                  <xs:enumeration value="warm"/>
                  <xs:enumeration value="cold"/>
                </xs:restriction>
              </xs:simpleType>
              <xs:complexType name="Shape">
                <xs:sequence>
                  <xs:element name="Name" type="xs:string"/>
                </xs:sequence>
              </xs:complexType>
              <xs:complexType name="Circle">
                <xs:complexContent>
                  <xs:extension base="p:Shape">
                    <xs:sequence>
                      <xs:element name="Radius" type="xs:int"/>
                    </xs:sequence>
                  </xs:extension>
                </xs:complexContent>
              </xs:complexType>
              <xs:complexType name="Square">
                <xs:complexContent>
                  <xs:extension base="p:Shape">
                    <xs:sequence>
                      <xs:element name="Side" type="xs:int"/>
                    </xs:sequence>
                  </xs:extension>
                </xs:complexContent>
              </xs:complexType>
              <xs:element name="shape" type="p:Shape"/>
              <xs:element name="circle" type="p:Circle" substitutionGroup="p:shape"/>
              <xs:element name="square" type="p:Square" substitutionGroup="p:shape"/>
              <xs:element name="token" type="xs:string" abstract="true"/>
              <xs:element name="word" type="xs:string" substitutionGroup="p:token"/>
              <xs:complexType name="Para" mixed="true">
                <xs:sequence>
                  <xs:element name="b" type="xs:string" minOccurs="0" maxOccurs="unbounded"/>
                </xs:sequence>
                <xs:attribute name="lang" type="xs:string"/>
              </xs:complexType>
              <xs:element name="page">
                <xs:complexType>
                  <xs:sequence>
                    <xs:element ref="p:shape" maxOccurs="unbounded"/>
                    <xs:element ref="p:token"/>
                    <xs:choice>
                      <xs:element name="Card" type="xs:string"/>
                      <xs:element name="Cash" type="xs:decimal"/>
                    </xs:choice>
                    <xs:choice maxOccurs="unbounded">
                      <xs:element name="Text" type="xs:string"/>
                      <xs:element name="Count" type="xs:int"/>
                      <xs:element name="Body" type="p:Para"/>
                    </xs:choice>
                    <xs:sequence maxOccurs="unbounded">
                      <xs:element name="Key" type="xs:string"/>
                      <xs:element name="Value" type="xs:int"/>
                    </xs:sequence>
                    <xs:element name="Tone" type="p:Tone" default="cold"/>
                    <xs:element name="Lead" type="p:Shape" minOccurs="0"/>
                  </xs:sequence>
                  <xs:attribute name="rev" type="xs:int" default="3"/>
                  <xs:attribute name="title" type="xs:string" fixed="x"/>
                  <xs:attribute name="ratio" type="xs:double" default="INF"/>
                  <xs:attribute name="when" type="xs:date" default="2020-01-01"/>
                  <xs:attribute name="big" type="xs:unsignedLong" default="7"/>
                  <xs:attribute name="on" type="xs:boolean" default="1"/>
                  <xs:attribute name="price" type="xs:decimal" default="1.50"/>
                  <xs:attribute name="bad" type="xs:int" default="x"/>
                  <xs:attribute name="small" type="xs:float" default="0.5"/>
                  <xs:attribute name="wide" type="xs:long" default="-9"/>
                </xs:complexType>
              </xs:element>
            </xs:schema>
            """;

        /// <summary>
        /// A schema where elements that may stand in one place have one type: a choice of two text elements, a
        /// sequence that repeats with two text elements and a group inside it, a choice of two elements of one
        /// complex type, and a substitution group of text elements. It also has what stays as members of their
        /// own: a choice with an element that repeats, and a group that repeats around a wildcard.
        /// </summary>
        private const string BoxSchema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:element name="item" type="xs:string"/>
              <xs:element name="thing" type="xs:string" substitutionGroup="item"/>
              <xs:complexType name="Address">
                <xs:sequence>
                  <xs:element name="City" type="xs:string"/>
                </xs:sequence>
              </xs:complexType>
              <xs:group name="Counted">
                <xs:sequence>
                  <xs:element name="Count" type="xs:int"/>
                </xs:sequence>
              </xs:group>
              <xs:element name="Box">
                <xs:complexType>
                  <xs:sequence>
                    <xs:choice>
                      <xs:element name="Home" type="xs:string"/>
                      <xs:element name="Work" type="xs:string"/>
                    </xs:choice>
                    <xs:sequence maxOccurs="unbounded">
                      <xs:element name="Key" type="xs:string"/>
                      <xs:element name="Value" type="xs:string"/>
                      <xs:group ref="Counted"/>
                    </xs:sequence>
                    <xs:choice>
                      <xs:element name="BillTo" type="Address"/>
                      <xs:element name="ShipTo" type="Address"/>
                    </xs:choice>
                    <xs:choice>
                      <xs:element name="Many" type="xs:int" maxOccurs="unbounded"/>
                      <xs:element name="One" type="xs:string"/>
                    </xs:choice>
                    <xs:choice>
                      <xs:element name="Only" type="xs:int"/>
                    </xs:choice>
                    <xs:element ref="item"/>
                    <xs:sequence minOccurs="0" maxOccurs="unbounded">
                      <xs:element name="Left" type="xs:string"/>
                      <xs:any/>
                    </xs:sequence>
                  </xs:sequence>
                </xs:complexType>
              </xs:element>
            </xs:schema>
            """;

        /// <summary>
        /// A schema with a target namespace whose local elements are in no namespace, as they are when the schema
        /// does not say <c>elementFormDefault="qualified"</c>.
        /// </summary>
        private const string FormsSchema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:f="urn:f" targetNamespace="urn:f">
              <xs:element name="Note" type="xs:string"/>
              <xs:attribute name="code" type="xs:string"/>
              <xs:element name="Order">
                <xs:complexType>
                  <xs:sequence>
                    <xs:element name="Id" type="xs:int"/>
                    <xs:element name="Line" maxOccurs="unbounded">
                      <xs:complexType>
                        <xs:sequence>
                          <xs:element name="Sku" type="xs:string"/>
                        </xs:sequence>
                      </xs:complexType>
                    </xs:element>
                    <xs:element name="Tags">
                      <xs:complexType>
                        <xs:sequence>
                          <xs:element name="Tag" type="xs:string" maxOccurs="unbounded"/>
                        </xs:sequence>
                      </xs:complexType>
                    </xs:element>
                    <xs:element ref="f:Note"/>
                    <xs:element name="Q" type="xs:string" form="qualified"/>
                    <xs:choice>
                      <xs:element name="A" type="xs:string"/>
                      <xs:element name="B" type="xs:int" form="qualified"/>
                    </xs:choice>
                  </xs:sequence>
                  <xs:attribute ref="f:code"/>
                  <xs:attribute name="mark" type="xs:string" form="qualified"/>
                  <xs:attribute name="plain" type="xs:string"/>
                </xs:complexType>
              </xs:element>
            </xs:schema>
            """;

        #endregion

        #region Public Methods

        [TestMethod]
        public void PagesSchema_DeclaresOneMemberForAChoiceARepeatingGroupAndASubstitutionGroup()
        {
            var (declared, diagnostics) = XmlSerializerXsdMappingTests.Generate(PagesSchema, "pages.xsd");

            diagnostics.Should().Equal(
                "Info XMLESS011: The default value '2020-01-01' of 'when' is not applied to the generated member.",
                "Info XMLESS011: The default value 'x' of 'bad' is not applied to the generated member.");
            declared.Should().Equal(
                "enum " + G + "Tone",
                "  warm",
                "  cold",
                "class " + G + "Shape [" + S + ".XmlRootAttribute(\"shape\", Namespace = \"urn:pages\"), " + S + ".XmlPolymorphicAttribute(DiscriminatorPlacement = CloudNimble.Text.Xml.XmlDiscriminatorPlacement.XsiType), " + S + ".XmlDerivedTypeAttribute<" + G + "Circle>(\"Circle\"), " + S + ".XmlDerivedTypeAttribute<" + G + "Square>(\"Square\")]",
                "  string? Name [" + Ignore + "]",
                "class " + G + "Circle : Shape [" + S + ".XmlRootAttribute(\"circle\", Namespace = \"urn:pages\")]",
                "  int Radius",
                "class " + G + "Square : Shape [" + S + ".XmlRootAttribute(\"square\", Namespace = \"urn:pages\")]",
                "  int Side",
                "class " + G + "page [" + S + ".XmlRootAttribute(\"page\", Namespace = \"urn:pages\")]",
                "  int rev [" + S + ".XmlAttributeAttribute(\"rev\")]",
                "  string? title [" + S + ".XmlAttributeAttribute(\"title\")]",
                "  double ratio [" + S + ".XmlAttributeAttribute(\"ratio\")]",
                "  System.DateOnly? when [" + S + ".XmlAttributeAttribute(\"when\")]",
                "  ulong big [" + S + ".XmlAttributeAttribute(\"big\")]",
                "  bool on [" + S + ".XmlAttributeAttribute(\"on\")]",
                "  decimal price [" + S + ".XmlAttributeAttribute(\"price\")]",
                "  int? bad [" + S + ".XmlAttributeAttribute(\"bad\")]",
                "  float small [" + S + ".XmlAttributeAttribute(\"small\")]",
                "  long wide [" + S + ".XmlAttributeAttribute(\"wide\")]",
                "  System.Collections.Generic.List<" + G + "Shape>? shape [" + S + ".XmlElementAttribute<" + G + "Shape>(\"shape\"), " + S + ".XmlElementAttribute<" + G + "Circle>(\"circle\"), " + S + ".XmlElementAttribute<" + G + "Square>(\"square\")]",
                "  string? token [" + S + ".XmlElementAttribute<System.String>(\"word\"), " + Ignore + "]",
                "  object? Item [" + S + ".XmlElementAttribute<System.String>(\"Card\"), " + S + ".XmlElementAttribute<System.Decimal>(\"Cash\"), " + Ignore + "]",
                "  System.Collections.Generic.List<object>? Items [" + S + ".XmlElementAttribute<System.String>(\"Text\"), " + S + ".XmlElementAttribute<System.Int32>(\"Count\"), " + S + ".XmlElementAttribute<" + G + "Para>(\"Body\")]",
                "  System.Collections.Generic.List<object>? Items2 [" + S + ".XmlElementAttribute<System.String>(\"Key\"), " + S + ".XmlElementAttribute<System.Int32>(\"Value\")]",
                "  " + G + "Tone Tone",
                "  " + G + "Shape? Lead [" + Ignore + "]",
                "class " + G + "Para [" + S + ".XmlRootAttribute(\"Para\", Namespace = \"urn:pages\")]",
                "  string? lang [" + S + ".XmlAttributeAttribute(\"lang\")]",
                "  System.Collections.Generic.List<" + S + ".XmlMixedContentItem>? Content [" + S + ".XmlMixedAttribute]",
                "class " + G + "PagesXmlContext : XmlSerializerContext [" + S + ".XmlSerializableAttribute<" + G + "Tone>, " + S + ".XmlSerializableAttribute<" + G + "Shape>, " + S + ".XmlSerializableAttribute<" + G + "Circle>, " + S + ".XmlSerializableAttribute<" + G + "Square>, " + S + ".XmlSerializableAttribute<" + G + "page>, " + S + ".XmlSerializableAttribute<" + G + "Para>, " + S + ".XmlSerializableAttribute<" + S + ".XmlMixedText>]",
                "  " + S + ".XmlTypeInfo<" + G + "Tone> Tone",
                "  " + S + ".XmlTypeInfo<" + G + "Shape> Shape",
                "  " + S + ".XmlTypeInfo<" + G + "Circle> Circle",
                "  " + S + ".XmlTypeInfo<" + G + "Square> Square",
                "  " + S + ".XmlTypeInfo<" + G + "page> page",
                "  " + S + ".XmlTypeInfo<" + G + "Para> Para",
                "  " + S + ".XmlTypeInfo<" + S + ".XmlMixedText> XmlMixedText");
        }

        [TestMethod]
        public void PagesSchema_ReadsASchemaValidDocument_KeepsItsOrder_AndWritesItBack_AsReflectionDoes()
        {
            XmlSerializerXsdMappingTests.Execute(PagesSchema, "pages.xsd", """
                using System;
                using System.Globalization;
                using System.Linq;
                using CloudNimble.Text.Xml;
                using CloudNimble.Text.Xml.Serialization;
                using XmlEssentials.Generated;

                public static class Use
                {
                    // Numbers are shown in the invariant culture, so the result is the same on every machine.
                    private static string Join(params object?[] values)
                    {
                        return string.Join("|", values.Select(static value => Convert.ToString(value, CultureInfo.InvariantCulture)));
                    }

                    private static string Show(object value)
                    {
                        return value switch
                        {
                            Circle circle => "circle(" + circle.Name + "," + circle.Radius + ")",
                            Square square => "square(" + square.Name + "," + square.Side + ")",
                            Shape shape => "shape(" + shape.Name + ")",
                            Para para => "para[" + para.lang + ":" + string.Join("", para.Content!.Select(item => item is XmlMixedText text ? text.Text : "<" + ((XmlMixedElement)item).Element.LocalName + ">")) + "]",
                            _ => value.GetType().Name + ":" + Convert.ToString(value, CultureInfo.InvariantCulture),
                        };
                    }

                    public static string Run()
                    {
                        const string document =
                            "<p:page xmlns:p=\"urn:pages\" rev=\"4\">" +
                            "<p:circle><p:Name>c</p:Name><p:Radius>1</p:Radius></p:circle><p:shape><p:Name>s</p:Name></p:shape><p:square><p:Name>q</p:Name><p:Side>2</p:Side></p:square>" +
                            "<p:word>w</p:word>" +
                            "<p:Cash>2.5</p:Cash>" +
                            "<p:Text>a</p:Text><p:Body lang=\"en\">one <p:b>two</p:b> three</p:Body><p:Count>5</p:Count><p:Text>b</p:Text>" +
                            "<p:Key>k1</p:Key><p:Value>1</p:Value><p:Key>k2</p:Key><p:Value>2</p:Value>" +
                            "<p:Tone>warm</p:Tone>" +
                            "</p:page>";
                        var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultNamespace = "urn:pages" };
                        var context = new PagesXmlContext(options);
                        var read = XmlSerializer.Deserialize(document, context.page)!;
                        var shown = Join(
                            read.rev, read.title, read.ratio, read.when?.ToString(CultureInfo.InvariantCulture) ?? "null", read.big, read.on, read.price, read.bad?.ToString(CultureInfo.InvariantCulture) ?? "null", read.small, read.wide,
                            string.Join(",", read.shape!.Select(Show)), read.token, Show(read.Item!),
                            string.Join(",", read.Items!.Select(Show)), string.Join(",", read.Items2!.Select(Show)), read.Tone, read.Lead is null);
                        var written = XmlSerializer.Serialize(read, context.page);
                        var reflected = XmlSerializer.Serialize(read, new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultNamespace = "urn:pages" });
                        var again = XmlSerializer.Serialize(XmlSerializer.Deserialize(written, context.page)!, context.page);
                        var fresh = new page();
                        return shown + "\n" + written + "\n" + (written == reflected ? "same" : "reflection: " + reflected) + "\n" + (written == again ? "stable" : "again: " + again)
                            + "\n" + Join(fresh.rev, fresh.title, fresh.ratio, fresh.big, fresh.on, fresh.price, fresh.small, fresh.wide, fresh.Tone);
                    }
                }
                """).Should().Be(
                    "4|x|Infinity|null|7|True|1.50|null|0.5|-9|circle(c,1),shape(s),square(q,2)|w|Decimal:2.5|String:a,para[en:one <b> three],Int32:5,String:b|String:k1,Int32:1,String:k2,Int32:2|warm|True\n" +
                    "<page xmlns=\"urn:pages\" rev=\"4\" title=\"x\" ratio=\"INF\" big=\"7\" on=\"true\" price=\"1.50\" small=\"0.5\" wide=\"-9\"><circle><Name>c</Name><Radius>1</Radius></circle><shape><Name>s</Name></shape><square><Name>q</Name><Side>2</Side></square><word>w</word><Cash>2.5</Cash><Text>a</Text><Body lang=\"en\">one <p:b xmlns:p=\"urn:pages\">two</p:b> three</Body><Count>5</Count><Text>b</Text><Key>k1</Key><Value>1</Value><Key>k2</Key><Value>2</Value><Tone>warm</Tone></page>\n" +
                    "same\n" +
                    "stable\n" +
                    "3|x|Infinity|7|True|1.50|0.5|-9|cold");
        }

        [TestMethod]
        public void ElementsOfOneType_ShareAMemberThatHoldsNamedValues()
        {
            var (declared, diagnostics) = XmlSerializerXsdMappingTests.Generate(BoxSchema, "box.xsd");

            diagnostics.Should().Equal(
                "Info XMLESS011: xs:choice in 'Box' is mapped as optional members, because its alternatives are not single elements.",
                "Warning XMLESS018: A repeating xs:sequence in 'Box' is mapped as members that each repeat, because it holds a wildcard or one element name with two types. The order between them is not kept.");
            declared.Should().Equal(
                "class " + G + "Box [" + S + ".XmlRootAttribute(\"Box\")]",
                "  " + S + ".XmlNamedValue? Item [" + S + ".XmlElementAttribute<System.String>(\"Home\"), " + S + ".XmlElementAttribute<System.String>(\"Work\"), " + Ignore + "]",
                "  System.Collections.Generic.List<" + S + ".XmlNamedValue>? Items [" + S + ".XmlElementAttribute<System.String>(\"Key\"), " + S + ".XmlElementAttribute<System.String>(\"Value\"), " + S + ".XmlElementAttribute<System.Int32>(\"Count\")]",
                "  " + S + ".XmlNamedValue? Item2 [" + S + ".XmlElementAttribute<" + G + "Address>(\"BillTo\"), " + S + ".XmlElementAttribute<" + G + "Address>(\"ShipTo\"), " + Ignore + "]",
                "  System.Collections.Generic.List<int>? Many",
                "  string? One [" + Ignore + "]",
                "  int Only",
                "  " + S + ".XmlNamedValue? item [" + S + ".XmlElementAttribute<System.String>(\"item\"), " + S + ".XmlElementAttribute<System.String>(\"thing\"), " + Ignore + "]",
                "  System.Collections.Generic.List<string>? Left",
                "  " + S + ".XmlExtensionData? Extra [" + S + ".XmlExtensionDataAttribute]",
                "class " + G + "Address [" + S + ".XmlRootAttribute(\"Address\")]",
                "  string? City [" + Ignore + "]",
                "class " + G + "BoxXmlContext : XmlSerializerContext [" + S + ".XmlSerializableAttribute<" + G + "Box>, " + S + ".XmlSerializableAttribute<" + G + "Address>, " + S + ".XmlSerializableAttribute<" + S + ".XmlNamedValue>]",
                "  " + S + ".XmlTypeInfo<" + G + "Box> Box",
                "  " + S + ".XmlTypeInfo<" + G + "Address> Address",
                "  " + S + ".XmlTypeInfo<" + S + ".XmlNamedValue> XmlNamedValue");
        }

        [TestMethod]
        public void ElementsOfOneType_AreReadAndWrittenByName_InTheOrderOfTheDocument_AsReflectionDoes()
        {
            XmlSerializerXsdMappingTests.Execute(BoxSchema, "box.xsd", """
                using System;
                using System.Linq;
                using CloudNimble.Text.Xml;
                using CloudNimble.Text.Xml.Serialization;
                using XmlEssentials.Generated;

                public static class Use
                {
                    private static string Show(XmlNamedValue named)
                    {
                        return named.Name + "=" + (named.Value is Address address ? "address(" + address.City + ")" : named.Value);
                    }

                    public static string Run()
                    {
                        // Work and Home are both text, and so are Key and Value. The element name is what tells them apart.
                        const string document =
                            "<Box><Work>w</Work>" +
                            "<Key>k1</Key><Value>v1</Value><Count>1</Count><Key>k2</Key><Value>v2</Value><Count>2</Count>" +
                            "<ShipTo><City>Paris</City></ShipTo><Many>7</Many><Only>3</Only><thing>t</thing></Box>";
                        var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
                        var context = new BoxXmlContext(options);
                        var read = XmlSerializer.Deserialize(document, context.Box)!;
                        var shown = string.Join("|", Show(read.Item!), string.Join(",", read.Items!.Select(Show)), Show(read.Item2!), read.Many![0], read.Only, Show(read.item!));
                        var written = XmlSerializer.Serialize(read, context.Box);
                        var reflected = XmlSerializer.Serialize(read, new XmlSerializerOptions { OmitXmlDeclaration = true });

                        // A value made in code names its element the same way.
                        var made = new Box { Item = new XmlNamedValue("Home", "h"), Items = [new XmlNamedValue("Value", "v"), new XmlNamedValue("Key", "k")], Only = 1 };
                        return shown + "\n" + (written == document ? "identical" : written) + "\n" + (written == reflected ? "same" : "reflection: " + reflected)
                            + "\n" + XmlSerializer.Serialize(made, context.Box);
                    }
                }
                """).Should().Be(
                    "Work=w|Key=k1,Value=v1,Count=1,Key=k2,Value=v2,Count=2|ShipTo=address(Paris)|7|3|thing=t\n" +
                    "identical\n" +
                    "same\n" +
                    "<Box><Home>h</Home><Value>v</Value><Key>k</Key><Only>1</Only></Box>");
        }

        [TestMethod]
        public void FormsSchema_PutsLocalElementsInNoNamespace_AndGlobalOnesInTheTargetNamespace()
        {
            var (declared, diagnostics) = XmlSerializerXsdMappingTests.Generate(FormsSchema, "forms.xsd");

            diagnostics.Should().BeEmpty();
            declared.Should().Equal(
                "class " + G + "Order_Line [" + S + ".XmlRootAttribute(\"Line\", Namespace = \"urn:f\")]",
                "  string? Sku [" + S + ".XmlElementAttribute(\"Sku\", Namespace = \"\"), " + Ignore + "]",
                "class " + G + "Order [" + S + ".XmlRootAttribute(\"Order\", Namespace = \"urn:f\")]",
                "  string? code [" + S + ".XmlAttributeAttribute(\"code\", Namespace = \"urn:f\")]",
                "  string? mark [" + S + ".XmlAttributeAttribute(\"mark\", Namespace = \"urn:f\")]",
                "  string? plain [" + S + ".XmlAttributeAttribute(\"plain\")]",
                "  int Id [" + S + ".XmlElementAttribute(\"Id\", Namespace = \"\")]",
                "  System.Collections.Generic.List<" + G + "Order_Line>? Line [" + S + ".XmlElementAttribute(\"Line\", Namespace = \"\")]",
                "  System.Collections.Generic.List<string>? Tags [" + S + ".XmlArrayAttribute(\"Tags\", Namespace = \"\"), " + S + ".XmlArrayItemAttribute(\"Tag\", Namespace = \"\"), " + Ignore + "]",
                "  string? Note [" + S + ".XmlElementAttribute(\"Note\", Namespace = \"urn:f\"), " + Ignore + "]",
                "  string? Q [" + S + ".XmlElementAttribute(\"Q\", Namespace = \"urn:f\"), " + Ignore + "]",
                "  object? Item [" + S + ".XmlElementAttribute<System.String>(\"A\", Namespace = \"\"), " + S + ".XmlElementAttribute<System.Int32>(\"B\", Namespace = \"urn:f\"), " + Ignore + "]",
                "class " + G + "FormsXmlContext : XmlSerializerContext [" + S + ".XmlSerializableAttribute<" + G + "Order_Line>, " + S + ".XmlSerializableAttribute<" + G + "Order>]",
                "  " + S + ".XmlTypeInfo<" + G + "Order_Line> Order_Line",
                "  " + S + ".XmlTypeInfo<" + G + "Order> Order");
        }

        [TestMethod]
        public void FormsSchema_ReadsASchemaValidDocument_AndWritesItBack_AsReflectionDoes()
        {
            XmlSerializerXsdMappingTests.Execute(FormsSchema, "forms.xsd", """
                using System;
                using CloudNimble.Text.Xml;
                using XmlEssentials.Generated;

                public static class Use
                {
                    public static string Run()
                    {
                        // The local elements are in no namespace. The document element, the element that is
                        // referred to, and what says form="qualified" are in the target namespace.
                        const string document =
                            "<f:Order xmlns:f=\"urn:f\" f:code=\"c\" f:mark=\"m\" plain=\"p\">" +
                            "<Id>1</Id><Line><Sku>a</Sku></Line><Line><Sku>b</Sku></Line><Tags><Tag>t</Tag></Tags>" +
                            "<f:Note>n</f:Note><f:Q>q</f:Q><f:B>7</f:B></f:Order>";
                        var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultNamespace = "urn:f" };
                        var context = new FormsXmlContext(options);
                        var read = XmlSerializer.Deserialize(document, context.Order)!;
                        var shown = string.Join("|", read.code, read.mark, read.plain, read.Id, read.Line!.Count, read.Line[1].Sku, string.Join(",", read.Tags!), read.Note, read.Q, read.Item);
                        var written = XmlSerializer.Serialize(read, context.Order);
                        var reflected = XmlSerializer.Serialize(read, new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultNamespace = "urn:f" });
                        var again = XmlSerializer.Serialize(XmlSerializer.Deserialize(written, context.Order)!, context.Order);

                        // The same elements in the target namespace are other elements, which this type does not have.
                        var qualified = XmlSerializer.Deserialize("<Order xmlns=\"urn:f\"><Id>1</Id><Note>n</Note></Order>", context.Order)!;
                        return shown + "\n" + written + "\n" + (written == reflected ? "same" : "reflection: " + reflected) + "\n" + (written == again ? "stable" : "again: " + again)
                            + "\n" + qualified.Id + "|" + qualified.Note;
                    }
                }
                """).Should().Be(
                    "c|m|p|1|2|b|t|n|q|7\n" +
                    "<Order xmlns=\"urn:f\" xmlns:ns1=\"urn:f\" ns1:code=\"c\" ns1:mark=\"m\" plain=\"p\"><Id xmlns=\"\">1</Id><Line xmlns=\"\"><Sku>a</Sku></Line><Line xmlns=\"\"><Sku>b</Sku></Line><Tags xmlns=\"\"><Tag>t</Tag></Tags><Note>n</Note><Q>q</Q><B>7</B></Order>\n" +
                    "same\n" +
                    "stable\n" +
                    "0|n");
        }

        [TestMethod]
        public void SubstitutionGroup_AValueOfATypeDerivedFromASubstitute_IsWrittenUnderItsElementWithXsiType()
        {
            const string schema = """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:w="urn:w" targetNamespace="urn:w" elementFormDefault="qualified">
                  <xs:complexType name="Shape">
                    <xs:sequence>
                      <xs:element name="Name" type="xs:string"/>
                    </xs:sequence>
                  </xs:complexType>
                  <xs:complexType name="Circle">
                    <xs:complexContent>
                      <xs:extension base="w:Shape">
                        <xs:sequence>
                          <xs:element name="Radius" type="xs:int"/>
                        </xs:sequence>
                      </xs:extension>
                    </xs:complexContent>
                  </xs:complexType>
                  <xs:complexType name="Wheel">
                    <xs:complexContent>
                      <xs:extension base="w:Circle">
                        <xs:sequence>
                          <xs:element name="Spokes" type="xs:int"/>
                        </xs:sequence>
                      </xs:extension>
                    </xs:complexContent>
                  </xs:complexType>
                  <xs:element name="shape" type="w:Shape"/>
                  <xs:element name="circle" type="w:Circle" substitutionGroup="w:shape"/>
                  <xs:element name="page">
                    <xs:complexType>
                      <xs:sequence>
                        <xs:element ref="w:shape" maxOccurs="unbounded"/>
                        <xs:element name="Shapes">
                          <xs:complexType>
                            <xs:sequence>
                              <xs:element ref="w:shape" maxOccurs="unbounded"/>
                            </xs:sequence>
                          </xs:complexType>
                        </xs:element>
                      </xs:sequence>
                    </xs:complexType>
                  </xs:element>
                </xs:schema>
                """;

            XmlSerializerXsdMappingTests.Execute(schema, "wheels.xsd", """
                using System.Linq;
                using CloudNimble.Text.Xml;
                using XmlEssentials.Generated;

                public static class Use
                {
                    public static string Run()
                    {
                        // A wheel is a circle, so it stands where a circle may, and says what it is with xsi:type. It
                        // is written under the nearest element that allows it, which is circle.
                        const string document =
                            "<w:page xmlns:w=\"urn:w\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                            "<w:circle xsi:type=\"w:Wheel\"><w:Name>a</w:Name><w:Radius>1</w:Radius><w:Spokes>9</w:Spokes></w:circle>" +
                            "<w:shape><w:Name>b</w:Name></w:shape>" +
                            "<w:Shapes><w:circle><w:Name>c</w:Name><w:Radius>2</w:Radius></w:circle><w:shape xsi:type=\"w:Wheel\"><w:Name>d</w:Name><w:Radius>3</w:Radius><w:Spokes>4</w:Spokes></w:shape></w:Shapes>" +
                            "</w:page>";
                        var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultNamespace = "urn:w" };
                        var context = new WheelsXmlContext(options);
                        var read = XmlSerializer.Deserialize(document, context.page)!;
                        var written = XmlSerializer.Serialize(read, context.page);
                        var reflected = XmlSerializer.Serialize(read, new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultNamespace = "urn:w" });
                        return string.Join(",", read.shape!.Select(s => s.GetType().Name)) + "|" + string.Join(",", read.Shapes!.shape!.Select(s => s.GetType().Name))
                            + "\n" + written + "\n" + (written == reflected ? "same" : "reflection: " + reflected);
                    }
                }
                """).Should().Be(
                    "Wheel,Shape|Circle,Wheel\n" +
                    "<page xmlns=\"urn:w\"><circle xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"Wheel\"><Name>a</Name><Radius>1</Radius><Spokes>9</Spokes></circle>" +
                    "<shape><Name>b</Name></shape>" +
                    "<Shapes><circle><Name>c</Name><Radius>2</Radius></circle><circle xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"Wheel\"><Name>d</Name><Radius>3</Radius><Spokes>4</Spokes></circle></Shapes></page>\n" +
                    "same");
        }

        [TestMethod]
        public void NamesTheGeneratorMakesUp_GiveWayToTheNamesOfTheSchema()
        {
            // A choice has no name in a schema, so its member is called Item. The schema may use that name itself,
            // in the type or in the type it extends.
            const string schema = """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
                  <xs:complexType name="Base">
                    <xs:sequence>
                      <xs:choice>
                        <xs:element name="A" type="xs:string"/>
                        <xs:element name="B" type="xs:int"/>
                      </xs:choice>
                    </xs:sequence>
                  </xs:complexType>
                  <xs:complexType name="Derived">
                    <xs:complexContent>
                      <xs:extension base="Base">
                        <xs:sequence>
                          <xs:choice>
                            <xs:element name="C" type="xs:string"/>
                            <xs:element name="D" type="xs:int"/>
                          </xs:choice>
                          <xs:element name="Item2" type="xs:string"/>
                          <xs:choice maxOccurs="unbounded">
                            <xs:element name="E" type="xs:string"/>
                            <xs:element name="F" type="xs:int"/>
                          </xs:choice>
                          <xs:element name="Items" type="xs:string"/>
                        </xs:sequence>
                      </xs:extension>
                    </xs:complexContent>
                  </xs:complexType>
                </xs:schema>
                """;

            var (declared, diagnostics) = XmlSerializerXsdMappingTests.Generate(schema, "names.xsd");

            diagnostics.Should().BeEmpty();
            declared.Where(line => line.StartsWith(' ') && !line.Contains("XmlTypeInfo")).Select(line => line.Split('[')[0].TrimEnd()).Should().Equal(
                "  object? Item",
                "  object? Item3",
                "  string? Item2",
                "  System.Collections.Generic.List<object>? Items2",
                "  string? Items");
        }

        [TestMethod]
        public void GroupThatRepeatsAroundOneElement_IsAListOfThatElement_AndIsNotReported()
        {
            const string schema = """
                <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
                  <xs:element name="Bag">
                    <xs:complexType>
                      <xs:sequence>
                        <xs:choice minOccurs="0" maxOccurs="unbounded">
                          <xs:element name="Entry" type="xs:string"/>
                        </xs:choice>
                        <xs:sequence maxOccurs="unbounded">
                          <xs:element name="Row" type="xs:int"/>
                        </xs:sequence>
                        <xs:element name="Tags">
                          <xs:complexType>
                            <xs:sequence minOccurs="0" maxOccurs="unbounded">
                              <xs:element name="Tag" type="xs:string"/>
                            </xs:sequence>
                          </xs:complexType>
                        </xs:element>
                        <xs:element name="Marks">
                          <xs:complexType>
                            <xs:choice maxOccurs="unbounded">
                              <xs:element name="Mark" type="xs:int"/>
                            </xs:choice>
                          </xs:complexType>
                        </xs:element>
                      </xs:sequence>
                    </xs:complexType>
                  </xs:element>
                </xs:schema>
                """;

            var (declared, diagnostics) = XmlSerializerXsdMappingTests.Generate(schema, "bag.xsd");

            diagnostics.Should().BeEmpty();
            declared.Should().Equal(
                "class " + G + "Bag [" + S + ".XmlRootAttribute(\"Bag\")]",
                "  System.Collections.Generic.List<string>? Entry",
                "  System.Collections.Generic.List<int>? Row",
                "  System.Collections.Generic.List<string>? Tags [" + S + ".XmlArrayAttribute(\"Tags\"), " + S + ".XmlArrayItemAttribute(\"Tag\"), " + Ignore + "]",
                "  System.Collections.Generic.List<int>? Marks [" + S + ".XmlArrayAttribute(\"Marks\"), " + S + ".XmlArrayItemAttribute(\"Mark\"), " + Ignore + "]",
                "class " + G + "BagXmlContext : XmlSerializerContext [" + S + ".XmlSerializableAttribute<" + G + "Bag>]",
                "  " + S + ".XmlTypeInfo<" + G + "Bag> Bag");
        }

        #endregion

    }

}
