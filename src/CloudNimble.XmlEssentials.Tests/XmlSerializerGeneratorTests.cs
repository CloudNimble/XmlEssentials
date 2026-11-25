using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Source generator: context emit, diagnostics, and interceptors.
    /// </summary>
    [TestClass]
    public class XmlSerializerGeneratorTests
    {

        #region Public Methods

        [TestMethod]
        public void PartialContext_EmitsDefaultAndPersonTypeInfo()
        {
            const string source = """
                using CloudNimble.Text.Xml.Serialization;

                public sealed class Person
                {
                    public string? Name { get; set; }
                    public int Age { get; set; }
                }

                [XmlSerializable<Person>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }
                """;
            var (compilation, diagnostics, generated) = GeneratorTestHost.Run(source);
            diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error).Should().BeEmpty();
            var text = string.Join("\n", generated.Select(tree => tree.ToString()));
            Declared(compilation, generated).Should().Equal(
                "class AppXmlContext : XmlSerializerContext [CloudNimble.Text.Xml.Serialization.XmlSerializableAttribute<Person>]",
                "  CloudNimble.Text.Xml.Serialization.XmlTypeInfo<Person> Person");
            RunGenerated(source + """

                public static class Use
                {
                    public static string Run()
                    {
                        return ReferenceEquals(AppXmlContext.Default, AppXmlContext.Default) + "|" + AppXmlContext.Default.Person.Type + "|" + CloudNimble.Text.Xml.XmlSerializer.Serialize(new Person { Name = "Ada", Age = 36 }, AppXmlContext.Default.Person);
                    }
                }
                """).Should().Be("True|Person|<?xml version=\"1.0\" encoding=\"utf-8\"?><Person><Name>Ada</Name><Age>36</Age></Person>");
            compilation.GetDiagnostics().Where(d => d.Severity is DiagnosticSeverity.Error).Should().BeEmpty();
        }

        [TestMethod]
        public void NonPartialContext_ReportsXMLESS001()
        {
            const string source = """
                using CloudNimble.Text.Xml.Serialization;

                public sealed class Person
                {
                    public string? Name { get; set; }
                }

                [XmlSerializable<Person>]
                internal class AppXmlContext : XmlSerializerContext
                {
                    public AppXmlContext() : base(null) { }
                    public override XmlTypeInfo? GetTypeInfo(System.Type type) => null;
                }
                """;
            var (_, diagnostics, _) = GeneratorTestHost.Run(source);
            diagnostics.Where(d => d.Id.StartsWith("XMLESS", System.StringComparison.Ordinal)).Select(d => d.Id).Should().Equal("XMLESS001");
        }

        [TestMethod]
        public void ComplexAttribute_ReportsXMLESS003()
        {
            const string source = """
                using CloudNimble.Text.Xml.Serialization;

                public sealed class Address
                {
                    public string? Street { get; set; }
                }

                public sealed class Person
                {
                    [XmlAttribute]
                    public Address? Home { get; set; }
                }

                [XmlSerializable<Person>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }
                """;
            var (_, diagnostics, _) = GeneratorTestHost.Run(source);
            diagnostics.Where(d => d.Id.StartsWith("XMLESS", System.StringComparison.Ordinal)).Select(d => d.Id).Should().Equal("XMLESS003");
        }

        [TestMethod]
        public void TwoTextMembers_ReportsXMLESS004()
        {
            const string source = """
                using CloudNimble.Text.Xml.Serialization;

                public sealed class Payload
                {
                    [XmlText]
                    public string? A { get; set; }
                    [XmlCData]
                    public string? B { get; set; }
                }

                [XmlSerializable<Payload>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }
                """;
            var (_, diagnostics, _) = GeneratorTestHost.Run(source);
            diagnostics.Where(d => d.Id.StartsWith("XMLESS", System.StringComparison.Ordinal)).Select(d => d.Id).Should().Equal("XMLESS004");
        }

        [TestMethod]
        public void CamelCaseOptions_RenamesElements()
        {
            const string source = """
                using CloudNimble.Text.Xml;
                using CloudNimble.Text.Xml.Serialization;

                public sealed class Person
                {
                    public string? Name { get; set; }
                }

                [XmlSourceGenerationOptions(PropertyNamingPolicy = XmlKnownNamingPolicy.CamelCase)]
                [XmlSerializable<Person>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }
                """;
            var (_, diagnostics, generated) = GeneratorTestHost.Run(source);
            diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error).Should().BeEmpty();
            RunGenerated(source + """

                public static class Use
                {
                    public static string Run()
                    {
                        var read = XmlSerializer.Deserialize("<person><name>Ada</name><Name>ignored</Name></person>", AppXmlContext.Default.Person)!;
                        return read.Name + "|" + XmlSerializer.Serialize(read, AppXmlContext.Default.Person);
                    }
                }
                """).Should().Be("Ada|<?xml version=\"1.0\" encoding=\"utf-8\"?><person><name>Ada</name></person>");
        }

        [TestMethod]
        public void GeneratedContext_RoundTripsAPerson()
        {
            const string source = """
                using CloudNimble.Text.Xml;
                using CloudNimble.Text.Xml.Serialization;

                public sealed class Person
                {
                    public string? Name { get; set; }
                    public int Age { get; set; }
                }

                [XmlSourceGenerationOptions(OmitXmlDeclaration = true)]
                [XmlSerializable<Person>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var xml = XmlSerializer.Serialize(new Person { Name = "Ada", Age = 36 }, AppXmlContext.Default.Person);
                        var read = XmlSerializer.Deserialize(xml, AppXmlContext.Default.Person);
                        return read!.Name + ":" + read.Age + ":" + xml;
                    }
                }
                """;
            RunGenerated(source).Should().Be("Ada:36:<Person><Name>Ada</Name><Age>36</Age></Person>");
        }

        [TestMethod]
        public void SerializeCall_EmitsAnInterceptor()
        {
            const string source = """
                using CloudNimble.Text.Xml;
                using CloudNimble.Text.Xml.Serialization;

                public sealed class Person
                {
                    public string? Name { get; set; }
                }

                [XmlSerializable<Person>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run(Person person) => XmlSerializer.Serialize(person);
                }
                """;
            var (_, diagnostics, generated) = GeneratorTestHost.Run(source);
            diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error).Should().BeEmpty();
            generated.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>())
                .Select(attribute => attribute.Name.ToString())
                .Where(name => name is "InterceptsLocation" or "global::System.Runtime.CompilerServices.InterceptsLocation" or "System.Runtime.CompilerServices.InterceptsLocation" or "InterceptsLocationAttribute" or "global::System.Runtime.CompilerServices.InterceptsLocationAttribute")
                .Should().HaveCount(1, "the one call to XmlSerializer.Serialize in the source is the one call to intercept");
        }

        [TestMethod]
        public void IntPtrMember_ReportsXMLESS002()
        {
            const string source = """
                using CloudNimble.Text.Xml.Serialization;

                public sealed class Holder
                {
                    public System.IntPtr Handle { get; set; }
                }

                [XmlSerializable<Holder>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }
                """;
            var (_, diagnostics, _) = GeneratorTestHost.Run(source);
            diagnostics.Where(d => d.Id.StartsWith("XMLESS", System.StringComparison.Ordinal)).Select(d => d.Id).Should().Equal("XMLESS002");
        }

        [TestMethod]
        public void UnboundGeneric_IsRejectedByTheCompiler_AndReportsXMLESS002()
        {
            const string source = """
                using CloudNimble.Text.Xml.Serialization;

                [XmlSerializable<System.Collections.Generic.List<>>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }
                """;
            // The type is a type argument of the attribute, and the language does not allow an unbound one there.
            // The generator still sees the attribute, reports the type, and generates nothing from it.
            var (compilation, diagnostics, _) = GeneratorTestHost.Run(source);
            diagnostics.Where(d => d.Id.StartsWith("XMLESS", System.StringComparison.Ordinal)).Select(d => d.Id).Should().Equal("XMLESS002");
            compilation.GetDiagnostics().Where(d => d.Severity is DiagnosticSeverity.Error).Select(d => d.Id + ": " + d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).Should().Equal("CS7003: Unexpected use of an unbound generic name");
        }

        [TestMethod]
        public void AmbiguousConstructors_ReportsXMLESS005()
        {
            const string source = """
                using CloudNimble.Text.Xml.Serialization;

                public sealed class Point
                {
                    public int X { get; set; }
                    public int Y { get; set; }
                    public Point(int X, int Y) { }
                    public Point(int Y, int X) { }
                }

                [XmlSerializable<Point>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }
                """;
            var (_, diagnostics, _) = GeneratorTestHost.Run(source);
            diagnostics.Where(d => d.Id.StartsWith("XMLESS", System.StringComparison.Ordinal)).Select(d => d.Id).Should().Equal("XMLESS005");
        }

        [TestMethod]
        public void InvalidXmlName_ReportsXMLESS006()
        {
            const string source = """
                using CloudNimble.Text.Xml.Serialization;

                public sealed class Person
                {
                    [XmlElement("1name")]
                    public string? Name { get; set; }
                }

                [XmlSerializable<Person>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }
                """;
            var (_, diagnostics, _) = GeneratorTestHost.Run(source);
            diagnostics.Where(d => d.Id.StartsWith("XMLESS", System.StringComparison.Ordinal)).Select(d => d.Id).Should().Equal("XMLESS006");
        }

        [TestMethod]
        public void BclXmlInclude_ReportsXMLESS007()
        {
            const string source = """
                using CloudNimble.Text.Xml.Serialization;

                [System.Xml.Serialization.XmlInclude(typeof(Person))]
                public sealed class Person
                {
                    public string? Name { get; set; }
                }

                [XmlSerializable<Person>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }
                """;
            var (_, diagnostics, _) = GeneratorTestHost.Run(source);
            diagnostics.Where(d => d.Id.StartsWith("XMLESS", System.StringComparison.Ordinal)).Select(d => d.Id).Should().Equal("XMLESS007");
        }

        [TestMethod]
        public void TwoXmlConstructors_ReportsXMLESS005()
        {
            const string source = """
                using CloudNimble.Text.Xml.Serialization;

                public sealed class Person
                {
                    public string? Name { get; set; }
                    [XmlConstructor]
                    public Person() { }
                    [XmlConstructor]
                    public Person(string Name) { this.Name = Name; }
                }

                [XmlSerializable<Person>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }
                """;
            var (_, diagnostics, _) = GeneratorTestHost.Run(source);
            diagnostics.Where(d => d.Id.StartsWith("XMLESS", System.StringComparison.Ordinal)).Select(d => d.Id).Should().Equal("XMLESS005");
        }

        [TestMethod]
        public void XmlMetadataServices_RoundTripsAnInt()
        {
            var converter = (XmlConverter<int>)XmlSerializerOptions.Default.GetConverter(typeof(int));
            var info = XmlMetadataServices.CreateValueInfo(new XmlSerializerOptions { OmitXmlDeclaration = true }, converter);
            var xml = XmlSerializer.Serialize(7, info);
            xml.Should().Be("<int>7</int>");
            XmlSerializer.Deserialize(xml, info).Should().Be(7);
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("n");
            XmlMetadataServices.Write(writer, 9, info);
            writer.WriteEndElement();
            writer.Dispose();
            System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<n>9</n>");
            var attrBuffer = new ArrayBufferWriter<byte>();
            var attrWriter = new Utf8XmlWriter(attrBuffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            attrWriter.WriteStartElement("e");
            XmlMetadataServices.WriteAttribute(attrWriter, new XmlName("n"), 3, info);
            attrWriter.WriteEndElement();
            attrWriter.Dispose();
            System.Text.Encoding.UTF8.GetString(attrBuffer.WrittenSpan).Should().Be("<e n=\"3\"/>");
        }

        [TestMethod]
        public void XmlSerializableAttribute_StoresTheType()
        {
            var attribute = new XmlSerializableAttribute<int> { TypeInfoPropertyName = "N", GenerationMode = XmlSourceGenerationMode.Metadata };
            attribute.Type.Should().Be(typeof(int));
            attribute.TypeInfoPropertyName.Should().Be("N");
            attribute.GenerationMode.Should().Be(XmlSourceGenerationMode.Metadata);
        }

        [TestMethod]
        public void XmlSourceGenerationOptionsAttribute_KeepsEveryValueItIsGivenAndStartsUnset()
        {
            var attribute = new XmlSourceGenerationOptionsAttribute
            {
                WriteIndented = true,
                OmitXmlDeclaration = true,
                EmitByteOrderMark = true,
                IncludeFields = true,
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
                PropertyNameCaseInsensitive = true,
                PreferredObjectCreationHandling = XmlObjectCreationHandling.Populate,
                RespectNullableAnnotations = true,
                UnknownTypeHandling = XmlUnknownTypeHandling.XmlNode,
                MaxDepth = 8,
                DefaultNamespace = "urn:x",
                PropertyNamingPolicy = XmlKnownNamingPolicy.CamelCase,
                ElementNamingPolicy = XmlKnownNamingPolicy.SnakeCaseLower,
                AttributeNamingPolicy = XmlKnownNamingPolicy.KebabCaseLower,
                DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull,
                DefaultPropertyMapping = XmlPropertyMapping.Attribute,
                EnumHandling = XmlEnumHandling.Integer,
                XmlVersion = XmlVersion.Version11,
                GenerationMode = XmlSourceGenerationMode.Serialization,
            };
            attribute.Should().BeEquivalentTo(new
            {
                WriteIndented = true,
                OmitXmlDeclaration = true,
                EmitByteOrderMark = true,
                IncludeFields = true,
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
                PropertyNameCaseInsensitive = true,
                PreferredObjectCreationHandling = XmlObjectCreationHandling.Populate,
                RespectNullableAnnotations = true,
                UnknownTypeHandling = XmlUnknownTypeHandling.XmlNode,
                MaxDepth = 8,
                DefaultNamespace = "urn:x",
                PropertyNamingPolicy = XmlKnownNamingPolicy.CamelCase,
                ElementNamingPolicy = XmlKnownNamingPolicy.SnakeCaseLower,
                AttributeNamingPolicy = XmlKnownNamingPolicy.KebabCaseLower,
                DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull,
                DefaultPropertyMapping = XmlPropertyMapping.Attribute,
                EnumHandling = XmlEnumHandling.Integer,
                XmlVersion = XmlVersion.Version11,
                GenerationMode = XmlSourceGenerationMode.Serialization,
            });
            new XmlSourceGenerationOptionsAttribute().Should().BeEquivalentTo(new
            {
                WriteIndented = false,
                OmitXmlDeclaration = false,
                EmitByteOrderMark = false,
                IncludeFields = false,
                IgnoreReadOnlyFields = false,
                IgnoreReadOnlyProperties = false,
                PropertyNameCaseInsensitive = false,
                PreferredObjectCreationHandling = XmlObjectCreationHandling.Replace,
                RespectNullableAnnotations = false,
                UnknownTypeHandling = XmlUnknownTypeHandling.XmlElement,
                MaxDepth = 0,
                DefaultNamespace = (string?)null,
            });
        }

        [TestMethod]
        public void GeneratedContext_PositionalRecord_IsCreatedThroughItsConstructor()
        {
            RunGenerated("""
                using CloudNimble.Text.Xml;
                using CloudNimble.Text.Xml.Serialization;

                public sealed record Person(string Name, int Age);

                [XmlSourceGenerationOptions(OmitXmlDeclaration = true)]
                [XmlSerializable<Person>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var xml = XmlSerializer.Serialize(new Person("Ada", 36), AppXmlContext.Default.Person);
                        var read = XmlSerializer.Deserialize("<Person><Age>36</Age><Name>Ada</Name></Person>", AppXmlContext.Default.Person);
                        return read + "|" + (read == new Person("Ada", 36)) + "|" + xml;
                    }
                }
                """).Should().Be("Person { Name = Ada, Age = 36 }|True|<Person><Name>Ada</Name><Age>36</Age></Person>");
        }

        [TestMethod]
        public void GeneratedContext_ConstructorParametersWithDefaults_TakeThemWhenTheDocumentLeavesThemOut()
        {
            RunGenerated("""
                using System;
                using CloudNimble.Text.Xml;
                using CloudNimble.Text.Xml.Serialization;

                public sealed record Item(string Name, int Count = 5, double Scale = 1.5, DayOfWeek Day = DayOfWeek.Friday, string Note = "n/a \"quoted\"", long? Limit = null, char Mark = 'x', bool On = true, decimal Price = 2.50m);

                [XmlSerializable<Item>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var first = XmlSerializer.Deserialize("<Item><Name>a</Name></Item>", AppXmlContext.Default.Item);
                        var second = XmlSerializer.Deserialize("<Item><Name>b</Name><Count>9</Count><Scale>2</Scale><Day>Monday</Day><Note>n</Note><Limit>7</Limit><On>false</On><Price>3</Price></Item>", AppXmlContext.Default.Item);
                        var third = XmlSerializer.Deserialize("<Item><Count>0</Count><Name>c</Name></Item>", AppXmlContext.Default.Item);
                        return (first == new Item("a")) + "|" + (second == new Item("b", 9, 2, DayOfWeek.Monday, "n", 7, 'x', false, 3m)) + "|" + (third == new Item("c", 0));
                    }
                }
                """).Should().Be("True|True|True");
        }

        [TestMethod]
        public void GeneratedContext_ConstructorParameterWithNoDefault_IsRequiredUnlessTheOptionsSayOtherwise()
        {
            RunGenerated("""
                using CloudNimble.Text.Xml;
                using CloudNimble.Text.Xml.Serialization;

                public sealed record Person(string Name, int Age);

                [XmlSerializable<Person>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        string message;
                        try
                        {
                            XmlSerializer.Deserialize("<Person><Name>Ada</Name></Person>", AppXmlContext.Default.Person);
                            message = "no exception";
                        }
                        catch (XmlException ex)
                        {
                            message = ex.Message;
                        }

                        var lenient = new AppXmlContext(new XmlSerializerOptions { RespectRequiredConstructorParameters = false });
                        var read = XmlSerializer.Deserialize("<Person><Name>Ada</Name></Person>", lenient.Person);
                        return message + "|" + read;
                    }
                }
                """).Should().Be("The required constructor parameter 'Age' is missing. Path: /Person | LineNumber: 1 | BytePositionInLine: 34.|Person { Name = Ada, Age = 0 }");
        }

        [TestMethod]
        public void GeneratedContext_ConstructorParameters_FillMembersWhateverTheirCase()
        {
            // The same shapes as XmlSerializerConstructorCaseTests, through a generated context.
            RunGenerated("""
                using CloudNimble.Text.Xml;
                using CloudNimble.Text.Xml.Serialization;

                public sealed class Camel
                {
                    public string Name { get; }
                    public double Value { get; }
                    public Camel(string name, double value) { Name = name; Value = value; }
                }

                public sealed class Marked
                {
                    public int First { get; }
                    public int Second { get; }
                    [XmlIgnore] public string CreatedBy { get; }
                    [XmlConstructor] public Marked(int first) { First = first; Second = first + 1; CreatedBy = "marked"; }
                    public Marked(int first, int second) { First = first; Second = second; CreatedBy = "both"; }
                }

                public sealed class Either
                {
                    [XmlIgnore] public string CreatedBy { get; }
                    public string? Name { get; set; }
                    public Either() { CreatedBy = "parameterless"; }
                    public Either(string? name) { Name = name; CreatedBy = "parameterized"; }
                }

                public sealed class Twins
                {
                    public string Name { get; }
                    public string name { get; }
                    public Twins(string name, string Name) { this.name = name; this.Name = Name; }
                }

                public sealed class Titled
                {
                    [XmlElement("title")] public string Heading { get; }
                    public Titled(string TITLE) { Heading = TITLE; }
                }

                [XmlSerializable<Camel>]
                [XmlSerializable<Marked>]
                [XmlSerializable<Either>]
                [XmlSerializable<Twins>]
                [XmlSerializable<Titled>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var context = new AppXmlContext(new XmlSerializerOptions { OmitXmlDeclaration = true });
                        var camel = XmlSerializer.Deserialize("<Camel><Name>Voltage</Name><Value>230</Value></Camel>", context.Camel)!;
                        var marked = XmlSerializer.Deserialize("<Marked><First>4</First><Second>9</Second></Marked>", context.Marked)!;
                        var either = XmlSerializer.Deserialize("<Either><Name>Ada</Name></Either>", context.Either)!;
                        var twins = XmlSerializer.Deserialize("<Twins><Name>upper</Name><name>lower</name></Twins>", context.Twins)!;
                        var titled = XmlSerializer.Deserialize("<Titled><title>Dune</title></Titled>", context.Titled)!;
                        return camel.Name + "=" + camel.Value
                            + "|" + marked.CreatedBy + ":" + marked.First + "," + marked.Second
                            + "|" + either.CreatedBy + ":" + either.Name
                            + "|" + twins.Name + "," + twins.name
                            + "|" + titled.Heading
                            + "|" + XmlSerializer.Serialize(new Camel("Voltage", 230), context.Camel)
                            + XmlSerializer.Serialize(new Twins("lower", "upper"), context.Twins)
                            + XmlSerializer.Serialize(new Titled("Dune"), context.Titled);
                    }
                }
                """).Should().Be(
                "Voltage=230|marked:4,5|parameterless:Ada|upper,lower|Dune|"
                + "<Camel><Name>Voltage</Name><Value>230</Value></Camel>"
                + "<Twins><Name>upper</Name><name>lower</name></Twins>"
                + "<Titled><title>Dune</title></Titled>");
        }

        [TestMethod]
        public void GeneratedContext_MarkedConstructor_IsUsedAndAParameterNoMemberFillsTakesItsDefault()
        {
            RunGenerated("""
                using CloudNimble.Text.Xml;
                using CloudNimble.Text.Xml.Serialization;

                public sealed class Scored
                {
                    public Scored()
                    {
                        Name = "parameterless";
                    }

                    [XmlConstructor]
                    internal Scored(string Name, int bonus = 5)
                    {
                        this.Name = Name;
                        Total = bonus;
                    }

                    public string Name { get; }

                    [XmlIgnore]
                    public int Total { get; }

                    public string? Note { get; set; }
                }

                [XmlSourceGenerationOptions(OmitXmlDeclaration = true)]
                [XmlSerializable<Scored>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var read = XmlSerializer.Deserialize("<Scored><Note>later</Note><Name>Ada</Name></Scored>", AppXmlContext.Default.Scored)!;
                        return read.Name + ":" + read.Total + ":" + read.Note + ":" + XmlSerializer.Serialize(read, AppXmlContext.Default.Scored);
                    }
                }
                """).Should().Be("Ada:5:later:<Scored><Name>Ada</Name><Note>later</Note></Scored>");
        }

        [TestMethod]
        public void GeneratedContext_InitOnlyAndRequiredMembers_AreSetAndMembersLeftOutKeepWhatTheTypeGaveThem()
        {
            RunGenerated("""
                using System.Collections.Generic;
                using CloudNimble.Text.Xml;
                using CloudNimble.Text.Xml.Serialization;

                public sealed class Entry
                {
                    public string? Id { get; init; }
                    public required string Name { get; set; }
                    public int Count { get; set; } = 7;
                    public List<string> Tags { get; set; } = ["kept"];
                }

                [XmlSerializable<Entry>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var sparse = XmlSerializer.Deserialize("<Entry><Name>n</Name><Id>i</Id></Entry>", AppXmlContext.Default.Entry)!;
                        var full = XmlSerializer.Deserialize("<Entry><Id>j</Id><Name>m</Name><Count>2</Count><Tags>a</Tags><Tags>b</Tags></Entry>", AppXmlContext.Default.Entry)!;
                        string message;
                        try
                        {
                            XmlSerializer.Deserialize("<Entry><Id>i</Id></Entry>", AppXmlContext.Default.Entry);
                            message = "no exception";
                        }
                        catch (XmlException ex)
                        {
                            message = ex.Message;
                        }

                        return sparse.Id + ":" + sparse.Name + ":" + sparse.Count + ":" + string.Join(",", sparse.Tags)
                            + "|" + full.Id + ":" + full.Name + ":" + full.Count + ":" + string.Join(",", full.Tags)
                            + "|" + message;
                    }
                }
                """).Should().Be("i:n:7:kept|j:m:2:a,b|The required member 'Name' is missing. Path: /Entry | LineNumber: 1 | BytePositionInLine: 26.");
        }

        [TestMethod]
        public void GeneratedContext_RecordOfValueTypes_IsReadAllocatingOnlyTheRecord()
        {
            RunGenerated("""
                using System;
                using CloudNimble.Text.Xml;
                using CloudNimble.Text.Xml.Serialization;

                public sealed record Sample(int Count, long Total, double Ratio, bool Flagged);

                [XmlSerializable<Sample>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var xml = "<Sample><Count>1</Count><Total>10000000000</Total><Ratio>0.5</Ratio><Flagged>true</Flagged></Sample>"u8.ToArray();
                        var info = AppXmlContext.Default.Sample;
                        for (var i = 0; i < 100; i++)
                        {
                            XmlSerializer.Deserialize(xml, info);
                        }

                        var before = GC.GetAllocatedBytesForCurrentThread();
                        var read = XmlSerializer.Deserialize(xml, info);
                        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                        before = GC.GetAllocatedBytesForCurrentThread();
                        var same = new Sample(1, 10000000000L, 0.5, true);
                        var expected = GC.GetAllocatedBytesForCurrentThread() - before;
                        return (read == same) + "|" + (allocated - expected);
                    }
                }
                """).Should().Be("True|0");
        }

        [TestMethod]
        [DataRow("public sealed class Odd { public Odd(string Name, int extra) { this.Name = Name; } public string Name { get; } }", "XMLESS002", DisplayName = "no constructor can be called: one parameter is not a member and has no default")]
        [DataRow("public sealed class Odd { [XmlConstructor] public Odd(string Name) { this.Name = Name; } [XmlConstructor] public Odd(string Name, int Age) { this.Name = Name; this.Age = Age; } public string Name { get; } public int Age { get; } }", "XMLESS005", DisplayName = "two marked constructors")]
        [DataRow("public sealed class Odd { public Odd(string Name) { this.Name = Name; } public Odd(int Age) { this.Age = Age; } public string? Name { get; } public int Age { get; } }", "XMLESS005", DisplayName = "two constructors that bind as many members")]
        [DataRow("public sealed class Odd { public Odd(string Name) { this.Name = Name; } public Odd(string Name, int Age) { this.Name = Name; this.Age = Age; } public string? Name { get; } public int Age { get; } }", "XMLESS005", DisplayName = "two constructors with parameters of different sizes and no parameterless one")]
        public void GeneratedContext_ConstructorThatCannotBeChosenOrCalled_IsReported(string type, string expectedId)
        {
            var source = "using CloudNimble.Text.Xml;\nusing CloudNimble.Text.Xml.Serialization;\n" + type + "\n[XmlSerializable<Odd>]\ninternal partial class AppXmlContext : XmlSerializerContext\n{\n}\n";
            var (_, diagnostics, _) = GeneratorTestHost.Run(source);
            diagnostics.Where(d => d.Id.StartsWith("XMLESS", System.StringComparison.Ordinal)).Select(d => d.Id).Should().Equal(expectedId);
        }

        [TestMethod]
        public void GeneratedContext_NilElements_AreNullWhereTheTypeAllowsItAndAnErrorWhereItDoesNot()
        {
            RunGenerated(""""
                using System;
                using System.Collections.Generic;
                using CloudNimble.Text.Xml;
                using CloudNimble.Text.Xml.Serialization;

                public sealed class Holder
                {
                    public string? Name { get; set; } = "kept";
                    public int? Age { get; set; } = 3;
                    public decimal? Price { get; set; } = 1m;
                    public int Count { get; set; } = 4;
                    public Holder? Child { get; set; }
                    public List<string?> Tags { get; set; } = new();
                }

                public sealed class Strict
                {
                    public required string Name { get; set; }
                }

                [XmlSerializable<Holder>]
                [XmlSerializable<Strict>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    private const string Nil = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"";

                    private static string Try(Func<string> read)
                    {
                        try
                        {
                            return read();
                        }
                        catch (XmlException ex)
                        {
                            return ex.Message;
                        }
                    }

                    private static string Show(Holder holder)
                    {
                        return (holder.Name ?? "null") + "," + (holder.Age?.ToString() ?? "null") + "," + (holder.Price?.ToString() ?? "null") + "," + holder.Count + "," + (holder.Child is null ? "null" : "child") + ",[" + string.Join(";", holder.Tags.ConvertAll(tag => tag ?? "null")) + "]";
                    }

                    public static string Run()
                    {
                        return string.Join("|",
                            Try(() => Show(XmlSerializer.Deserialize($"<Holder><Name{Nil}/><Age{Nil}/><Price{Nil}/><Child{Nil}/><Tags>a</Tags><Tags{Nil}/><Tags/></Holder>", AppXmlContext.Default.Holder)!)),
                            Try(() => Show(XmlSerializer.Deserialize($"<Holder><Name{Nil}></Name><Age{Nil}></Age><Price{Nil}></Price><Count>9</Count></Holder>", AppXmlContext.Default.Holder)!)),
                            Try(() => Show(XmlSerializer.Deserialize("<Holder><Name/><Tags/></Holder>", AppXmlContext.Default.Holder)!)),
                            Try(() => Show(XmlSerializer.Deserialize($"<Holder><Count{Nil}/></Holder>", AppXmlContext.Default.Holder)!)),
                            Try(() => XmlSerializer.Deserialize($"<Strict><Name{Nil}/></Strict>", AppXmlContext.Default.Strict)!.Name));
                    }
                }
                """").Should().Be("null,null,null,4,null,[a;null;]|null,null,null,9,null,[]|,3,1,4,null,[]|The element is nil but type 'System.Int32' is not nillable. Path: /Holder/Count | LineNumber: 1 | BytePositionInLine: 86.|The required member 'Name' cannot be nil. Path: /Strict/Name | LineNumber: 1 | BytePositionInLine: 85.");
        }

        [TestMethod]
        public void GeneratedContext_NestedRecordsAndAnInternalConstructor_AreReadAndWritten()
        {
            RunGenerated("""
                using System.Collections.Generic;
                using CloudNimble.Text.Xml;
                using CloudNimble.Text.Xml.Serialization;

                public sealed record Outer(string Name, Inner Inner, List<Inner> More);

                public sealed record Inner(int Value);

                public sealed class OnlyInternal
                {
                    internal OnlyInternal()
                    {
                    }

                    public string? Name { get; set; }
                }

                public sealed class Renamed
                {
                    public Renamed(string title)
                    {
                        Heading = title;
                    }

                    [XmlElement("title")]
                    public string Heading { get; }
                }

                [XmlSourceGenerationOptions(OmitXmlDeclaration = true)]
                [XmlSerializable<Outer>]
                [XmlSerializable<OnlyInternal>]
                [XmlSerializable<Renamed>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var outer = new Outer("o", new Inner(1), new List<Inner> { new Inner(2), new Inner(3) });
                        var xml = XmlSerializer.Serialize(outer, AppXmlContext.Default.Outer);
                        var read = XmlSerializer.Deserialize(xml, AppXmlContext.Default.Outer)!;
                        var same = read.Name == "o" && read.Inner == new Inner(1) && read.More.Count == 2 && read.More[0] == new Inner(2) && read.More[1] == new Inner(3);
                        return xml + "|" + same
                            + "|" + XmlSerializer.Deserialize("<OnlyInternal><Name>x</Name></OnlyInternal>", AppXmlContext.Default.OnlyInternal)!.Name
                            + "|" + XmlSerializer.Deserialize("<Renamed><title>t</title></Renamed>", AppXmlContext.Default.Renamed)!.Heading;
                    }
                }
                """).Should().Be("<Outer><Name>o</Name><Inner><Value>1</Value></Inner><More><Value>2</Value></More><More><Value>3</Value></More></Outer>|True|x|t");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Describes what the generator declared, from the compiled symbols and not from the source text.
        /// </summary>
        /// <param name="compilation">The compilation after the generator ran.</param>
        /// <param name="generated">The generated trees.</param>
        /// <returns>
        /// One line per type declared outside any other type (kind, full name, base type, attributes), each
        /// followed by one indented line per public property, field, or enum member with its type and attributes.
        /// </returns>
        internal static List<string> Declared(Compilation compilation, IEnumerable<SyntaxTree> generated)
        {
            var lines = new List<string>();
            var seen = new HashSet<string>();
            foreach (var tree in generated)
            {
                var model = compilation.GetSemanticModel(tree);
                foreach (var declaration in tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.BaseTypeDeclarationSyntax>())
                {
                    if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol type || type.ContainingType is not null || !seen.Add(type.ToDisplayString()))
                    {
                        continue;
                    }

                    var baseType = type.BaseType is { SpecialType: SpecialType.None } parent && type.TypeKind is TypeKind.Class ? " : " + parent.Name : "";
                    lines.Add(type.TypeKind.ToString().ToLowerInvariant() + " " + type.ToDisplayString() + baseType + Attributes(type));
                    foreach (var member in type.GetMembers().Where(m => m.DeclaredAccessibility is Accessibility.Public && !m.IsImplicitlyDeclared && !m.IsStatic || m is IFieldSymbol { IsConst: true }))
                    {
                        if (member is IPropertySymbol property)
                        {
                            lines.Add("  " + property.Type.ToDisplayString() + " " + property.Name + Attributes(property));
                        }
                        else if (member is IFieldSymbol field)
                        {
                            lines.Add("  " + (type.TypeKind is TypeKind.Enum ? "" : field.Type.ToDisplayString() + " ") + field.Name + Attributes(field));
                        }
                    }
                }
            }

            return lines;

            static string Attributes(ISymbol symbol)
            {
                var attributes = symbol.GetAttributes().Select(a => a.ToString()).ToList();
                return attributes.Count == 0 ? "" : " [" + string.Join(", ", attributes) + "]";
            }
        }

        /// <summary>
        /// Compiles <paramref name="source"/> with the generator, loads the result, and calls its <c>Use.Run()</c>.
        /// </summary>
        /// <param name="source">A program with a context and a static <c>Use.Run</c> method that returns a string.</param>
        /// <returns>What <c>Use.Run()</c> returned.</returns>
        internal static string RunGenerated(string source)
        {
            var (compilation, diagnostics, generated) = GeneratorTestHost.Run(source);
            diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error).Select(d => d.ToString()).Should().BeEmpty();
            compilation.GetDiagnostics().Where(d => d.Severity is DiagnosticSeverity.Error).Select(d => d.ToString()).Should().BeEmpty();
            using var stream = new System.IO.MemoryStream();
            var emit = compilation.Emit(stream);
            emit.Diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error).Select(d => d.ToString()).Should().BeEmpty();
            stream.Position = 0;
            var loadContext = new System.Runtime.Loader.AssemblyLoadContext("Generated", isCollectible: true);
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

        #endregion

    }

}
