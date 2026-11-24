using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Interceptors: a call that passes a value or an input and no options is redirected to the generated
    /// context, with the signature of the overload it stands in for. A call that passes options runs as it was
    /// written. Each test compiles a program with the generator and runs it.
    /// </summary>
    [TestClass]
    public class XmlSerializerInterceptorTests
    {

        #region Fields

        /// <summary>
        /// A type and a context whose seeded options make the context's output unlike the reflection
        /// resolver's: no declaration, and camelCase names. Which one handled a call shows in the XML.
        /// </summary>
        private const string Model = """
            using System;
            using System.Buffers;
            using System.IO;
            using System.Text;
            using CloudNimble.Text.Xml;
            using CloudNimble.Text.Xml.Nodes;
            using CloudNimble.Text.Xml.Serialization;

            public sealed class Person
            {
                public string? Name { get; set; }
            }

            public struct Point
            {
                public int X { get; set; }
            }

            [XmlSourceGenerationOptions(OmitXmlDeclaration = true, PropertyNamingPolicy = XmlKnownNamingPolicy.CamelCase)]
            [XmlSerializable<Person>]
            [XmlSerializable<Point>]
            internal partial class AppXmlContext : XmlSerializerContext
            {
            }

            """;

        #endregion

        #region Public Methods

        [TestMethod]
        public void CallWithConverterInItsOptions_UsesTheConverter()
        {
            XmlSerializerGeneratorTests.RunGenerated(Model + """
                public sealed class ShoutingConverter : XmlConverter<Person>
                {
                    public override Person? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
                    {
                        var person = new Person();
                        while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
                        {
                            if (reader.TokenType is XmlTokenType.Text)
                            {
                                person.Name = reader.GetString().ToLowerInvariant();
                            }
                        }

                        return person;
                    }

                    public override void Write(Utf8XmlWriter writer, Person value, XmlSerializerOptions options)
                    {
                        writer.WriteStringValue(value.Name!.ToUpperInvariant());
                    }
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
                        options.Converters.Add(new ShoutingConverter());
                        var xml = XmlSerializer.Serialize(new Person { Name = "Ada" }, options);
                        var read = XmlSerializer.Deserialize<Person>(xml, options);
                        return xml + "|" + read!.Name;
                    }
                }
                """).Should().Be("<Person>ADA</Person>|ada");
        }

        [TestMethod]
        public void CallWithOptions_IsNotIntercepted()
        {
            var (compilation, diagnostics, generated) = GeneratorTestHost.Run(Model + """
                public static class Use
                {
                    public static string Run(Person person, XmlSerializerOptions options, Stream stream)
                    {
                        XmlSerializer.Deserialize<Person>("<Person/>", options);
                        XmlSerializer.Deserialize<Person>(stream, options);
                        XmlSerializer.Deserialize<Person>("<Person/>", (XmlSerializerOptions?)null);
                        XmlSerializer.Deserialize<Person>(options: options, xml: "<Person/>");
                        XmlSerializer.Serialize(person, (XmlSerializerOptions?)null);
                        XmlSerializer.Serialize(person, options: options);
                        return XmlSerializer.Serialize(person, options);
                    }
                }
                """);
            diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error).Select(d => d.ToString()).Should().BeEmpty();
            EmitErrors(compilation).Should().BeEmpty();
            InterceptorCount(generated).Should().Be(0);
        }

        [TestMethod]
        public void CallWithOptions_LeavesTheOptionsAsTheyWere()
        {
            XmlSerializerGeneratorTests.RunGenerated(Model + """
                public static class Use
                {
                    public static string Run()
                    {
                        var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
                        var resolverBefore = options.TypeInfoResolver;
                        var xml = XmlSerializer.Serialize(new Person { Name = "Ada" }, options);
                        var read = XmlSerializer.Deserialize<Person>(xml, options);
                        return xml + "|" + read!.Name + "|" + ReferenceEquals(resolverBefore, options.TypeInfoResolver) + "|" + options.TypeInfoResolver!.GetType().Name;
                    }
                }
                """).Should().Be("<Person><Name>Ada</Name></Person>|Ada|True|DefaultXmlTypeInfoResolver");
        }

        [TestMethod]
        public void CallWithOptions_WritesWhatTheSameCallWritesWithoutAContext()
        {
            // The non-generic overloads are never candidates for interception, so they give what the
            // reflection resolver writes for the same options.
            XmlSerializerGeneratorTests.RunGenerated(Model + """
                public static class Use
                {
                    public static string Run()
                    {
                        var person = new Person { Name = "Ada" };
                        var result = "";
                        foreach (var options in new[] { new XmlSerializerOptions(), new XmlSerializerOptions { WriteIndented = true, DefaultNamespace = "urn:p" }, XmlSerializerOptions.Web, XmlSerializerOptions.Strict })
                        {
                            var expected = XmlSerializer.Serialize(person, typeof(Person), options);
                            var actual = XmlSerializer.Serialize(person, options);
                            var read = XmlSerializer.Deserialize<Person>(actual, options);
                            result += (actual == expected) + ":" + read!.Name + ";";
                        }

                        return result;
                    }
                }
                """).Should().Be("True:Ada;True:Ada;True:Ada;True:Ada;");
        }

        [TestMethod]
        public void CallWithReadOnlyOptions_DoesNotThrow()
        {
            XmlSerializerGeneratorTests.RunGenerated(Model + """
                public static class Use
                {
                    public static string Run()
                    {
                        var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
                        options.MakeReadOnly();
                        var xml = XmlSerializer.Serialize(new Person { Name = "Ada" }, options);
                        var read = XmlSerializer.Deserialize<Person>(xml, options);
                        var web = XmlSerializer.Serialize(new Person { Name = "Ada" }, XmlSerializerOptions.Web);
                        return xml + "|" + read!.Name + "|" + options.IsReadOnly + "|" + XmlSerializer.Deserialize<Person>(web, XmlSerializerOptions.Web)!.Name;
                    }
                }
                """).Should().Be("<Person><Name>Ada</Name></Person>|Ada|True|Ada");
        }

        [TestMethod]
        public void CallWithoutOptions_ForATypeNoContextLists_IsNotIntercepted()
        {
            XmlSerializerGeneratorTests.RunGenerated(Model + """
                public sealed class Unlisted
                {
                    public int N { get; set; }
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var xml = XmlSerializer.Serialize(new Unlisted { N = 4 });
                        return xml + "|" + XmlSerializer.Deserialize<Unlisted>(xml)!.N;
                    }
                }
                """).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?><Unlisted><N>4</N></Unlisted>|4");
        }

        [TestMethod]
        public void DeserializeWithoutOptions_FromEveryKindOfInput_UsesTheContext()
        {
            // Each call reads a document only the context can read: its names are camelCase. Before the
            // interceptor took its signature from the call, every input but a string failed to compile (CS9144).
            XmlSerializerGeneratorTests.RunGenerated(Model + """
                public static class Use
                {
                    public static string Run()
                    {
                        const string xml = "<person><name>Ada</name></person>";
                        var bytes = Encoding.UTF8.GetBytes(xml);
                        using var stream = new MemoryStream(bytes);
                        using var document = XmlDocument.Parse(xml);
                        var reader = new Utf8XmlReader(bytes);
                        var names = new[]
                        {
                            XmlSerializer.Deserialize<Person>(xml)!.Name,
                            XmlSerializer.Deserialize<Person>(xml.AsSpan())!.Name,
                            XmlSerializer.Deserialize<Person>(bytes)!.Name,
                            XmlSerializer.Deserialize<Person>(new ReadOnlySpan<byte>(bytes))!.Name,
                            XmlSerializer.Deserialize<Person>(new ReadOnlySequence<byte>(bytes))!.Name,
                            XmlSerializer.Deserialize<Person>(stream)!.Name,
                            XmlSerializer.Deserialize<Person>(ref reader)!.Name,
                            XmlSerializer.Deserialize<Person>(document)!.Name,
                            XmlSerializer.Deserialize<Person>(document.RootElement)!.Name,
                            XmlSerializer.Deserialize<Person>(XmlNode.Parse(xml)!)!.Name,
                            CloudNimble.Text.Xml.XmlSerializer.Deserialize<Person>(xml: xml)!.Name,
                        };
                        return string.Join(",", names);
                    }
                }
                """).Should().Be("Ada,Ada,Ada,Ada,Ada,Ada,Ada,Ada,Ada,Ada,Ada");
        }

        [TestMethod]
        public void DeserializeWithoutOptions_OfAStruct_ReturnsTheStruct()
        {
            XmlSerializerGeneratorTests.RunGenerated(Model + """
                public static class Use
                {
                    public static string Run()
                    {
                        Point point = XmlSerializer.Deserialize<Point>("<point><x>7</x></point>");
                        Point fromBytes = XmlSerializer.Deserialize<Point>("<point><x>8</x></point>"u8);
                        return point.X + "," + fromBytes.X + "," + XmlSerializer.Serialize(new Point { X = 9 });
                    }
                }
                """).Should().Be("7,8,<point><x>9</x></point>");
        }

        [TestMethod]
        public void EachCallWithoutOptions_GetsOneInterceptor()
        {
            var (compilation, diagnostics, generated) = GeneratorTestHost.Run(Model + """
                public static class Use
                {
                    public static string Run(Person person, Stream stream)
                    {
                        XmlSerializer.Deserialize<Person>("<person/>");
                        XmlSerializer.Deserialize<Person>(stream);
                        XmlSerializer.Deserialize<Point>("<point/>"u8);
                        XmlSerializer.Serialize(new Point());
                        XmlSerializer.Serialize(value: person);
                        return XmlSerializer.Serialize(person);
                    }
                }
                """);
            diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error).Select(d => d.ToString()).Should().BeEmpty();
            EmitErrors(compilation).Should().BeEmpty();
            InterceptorCount(generated).Should().Be(6);
        }

        [TestMethod]
        public void SerializeWithoutOptions_UsesTheContext()
        {
            XmlSerializerGeneratorTests.RunGenerated(Model + """
                public static class Use
                {
                    public static string Run()
                    {
                        var person = new Person { Name = "Ada" };
                        return XmlSerializer.Serialize(person) + "|" + XmlSerializer.Serialize<Person>(person) + "|" + XmlSerializer.Serialize(value: person);
                    }
                }
                """).Should().Be("<person><name>Ada</name></person>|<person><name>Ada</name></person>|<person><name>Ada</name></person>");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Emits <paramref name="compilation"/> and returns the errors. An interceptor whose signature does not
        /// match the call it intercepts (CS9144) is reported only when the compilation is emitted.
        /// </summary>
        /// <param name="compilation">The compilation, with the generated trees.</param>
        /// <returns>The errors, as text.</returns>
        private static string[] EmitErrors(Compilation compilation)
        {
            using var stream = new System.IO.MemoryStream();
            return [.. compilation.Emit(stream).Diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error).Select(d => d.ToString())];
        }

        /// <summary>
        /// Counts the <c>[InterceptsLocation]</c> attributes in generated source.
        /// </summary>
        /// <param name="generated">The generated trees.</param>
        /// <returns>The number of intercepted call sites.</returns>
        private static int InterceptorCount(System.Collections.Immutable.ImmutableArray<SyntaxTree> generated)
        {
            return generated
                .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>())
                .Count(attribute => attribute.Name.ToString() is "InterceptsLocation");
        }

        #endregion

    }

}
