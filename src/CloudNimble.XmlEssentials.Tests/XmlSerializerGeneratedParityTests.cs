using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A generated context writes and reads what the reflection resolver does. Each test compiles a program
    /// with the generator and runs it; the program writes a value through the context and through the
    /// reflection resolver, reads the context's document back through both, and reports all four results.
    /// </summary>
    [TestClass]
    public class XmlSerializerGeneratedParityTests
    {

        #region Fields

        private const string Usings = """
            using System;
            using System.Collections.Generic;
            using System.Collections.Immutable;
            using System.Collections.ObjectModel;
            using System.Linq;
            using CloudNimble.Text.Xml;
            using CloudNimble.Text.Xml.Nodes;
            using CloudNimble.Text.Xml.Serialization;

            """;

        /// <summary>
        /// Writes a value through the context and through the reflection resolver with options built the same
        /// way, and reads the context's document back through both.
        /// </summary>
        private const string Compare = """

            public static class Both
            {
                public static string Write<T>(T value, Func<XmlSerializerOptions, XmlTypeInfo<T>> info, Func<T, string> show, Action<XmlSerializerOptions>? configure = null)
                {
                    var generatedOptions = new XmlSerializerOptions { OmitXmlDeclaration = true };
                    configure?.Invoke(generatedOptions);
                    var typeInfo = info(generatedOptions);
                    var reflectionOptions = new XmlSerializerOptions { OmitXmlDeclaration = true };
                    configure?.Invoke(reflectionOptions);
                    var generated = XmlSerializer.Serialize(value, typeInfo);
                    var reflected = XmlSerializer.Serialize(value, typeof(T), reflectionOptions);
                    var readByContext = show(XmlSerializer.Deserialize(generated, typeInfo)!);
                    var readByReflection = show((T)XmlSerializer.Deserialize(generated, typeof(T), reflectionOptions)!);
                    return generated + "\n" + (generated == reflected ? "same" : "reflection: " + reflected) + "\n" + readByContext + "\n" + readByReflection;
                }
            }
            """;

        #endregion

        #region Public Methods

        [TestMethod]
        public void CData_ThatHoldsItsTerminator_IsSplitIntoSections()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed class Note
                {
                    [XmlCData]
                    public string? Body { get; set; }
                }

                [XmlSerializable<Note>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        return Both.Write(new Note { Body = "a]]>b" }, o => new AppXmlContext(o).Note, n => n.Body!);
                    }
                }
                """ + Compare).Should().Be("<Note><![CDATA[a]]]]><![CDATA[>b]]></Note>\nsame\na]]>b\na]]>b");
        }

        [TestMethod]
        public void CollectionKinds_AreReadAsTheReflectionResolverReadsThem()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed class Kinds
                {
                    public Queue<int>? Queue { get; set; }
                    public Stack<int>? Stack { get; set; }
                    public ISet<int>? Set { get; set; }
                    public ImmutableArray<int> Array { get; set; }
                    [XmlArray]
                    public ImmutableList<string>? List { get; set; }
                    public IReadOnlyDictionary<string, int>? View { get; set; }
                    public ImmutableDictionary<string, int>? Map { get; set; }
                    public Dictionary<string, string?>? Nulls { get; set; }
                    public int[]? Numbers { get; set; }
                    public IEnumerable<int>? Sequence { get; set; }
                    public byte[]? Data { get; set; }
                }

                [XmlSerializable<Kinds>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var stack = new Stack<int>();
                        stack.Push(1);
                        stack.Push(2);
                        var value = new Kinds
                        {
                            Queue = new Queue<int>(new[] { 1, 2 }),
                            Stack = stack,
                            Set = new HashSet<int> { 3 },
                            Array = ImmutableArray.Create(4, 5),
                            List = ImmutableList.Create("a"),
                            View = new Dictionary<string, int> { ["v"] = 6 },
                            Map = ImmutableDictionary<string, int>.Empty.Add("m", 7),
                            Nulls = new Dictionary<string, string?> { ["n"] = null },
                            Numbers = new[] { 8, 9 },
                            Sequence = new[] { 10 },
                            Data = new byte[] { 1, 2 },
                        };
                        return Both.Write(value, o => new AppXmlContext(o).Kinds, k =>
                            string.Join(",", k.Queue!) + "|" + string.Join(",", k.Stack!) + "|" + k.Set!.GetType().Name + ":" + string.Join(",", k.Set!) + "|" + string.Join(",", k.Array)
                            + "|" + string.Join(",", k.List!) + "|" + k.View!["v"] + "|" + k.Map!["m"] + "|" + (k.Nulls!["n"] is null)
                            + "|" + k.Numbers!.GetType().Name + ":" + string.Join(",", k.Numbers!) + "|" + k.Sequence!.GetType().Name + ":" + string.Join(",", k.Sequence!) + "|" + string.Join(",", k.Data!));
                    }
                }
                """ + Compare).Should().Be(
                "<Kinds><Queue>1</Queue><Queue>2</Queue><Stack>2</Stack><Stack>1</Stack><Set>3</Set><Array>4</Array><Array>5</Array><List><String>a</String></List>" +
                "<View><v>6</v></View><Map><m>7</m></Map><Nulls><n xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></Nulls>" +
                "<Numbers>8</Numbers><Numbers>9</Numbers><Sequence>10</Sequence><Data>AQI=</Data></Kinds>" +
                "\nsame\n1,2|2,1|HashSet`1:3|4,5|a|6|7|True|Int32[]:8,9|List`1:10|1,2\n1,2|2,1|HashSet`1:3|4,5|a|6|7|True|Int32[]:8,9|List`1:10|1,2");
        }

        [TestMethod]
        public void ConstructorParameter_WhosePropertyIsIgnored_TakesItsDefault()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed record Login(string Name, [property: XmlIgnore] int Secret, [property: XmlIgnore] string? Note);

                [XmlSerializable<Login>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        return Both.Write(new Login("Ada", 7, "hidden"), o => new AppXmlContext(o).Login, l => l.ToString());
                    }
                }
                """ + Compare).Should().Be(
                "<Login><Name>Ada</Name></Login>\nsame\nLogin { Name = Ada, Secret = 0, Note =  }\nLogin { Name = Ada, Secret = 0, Note =  }");
        }

        [TestMethod]
        public void Context_AnswersOnlyForTheTypesItListsAndBuiltInOnes()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed class First
                {
                    public int A { get; set; }
                }

                public sealed class Second
                {
                    public int B { get; set; }
                }

                public sealed class Third
                {
                    public int C { get; set; }
                }

                public enum Color
                {
                    Red,
                }

                [XmlSerializable<First>]
                internal partial class FirstContext : XmlSerializerContext
                {
                }

                [XmlSerializable<Second>]
                [XmlSerializable<Second[]>]
                internal partial class SecondContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var first = new FirstContext();
                        var answers = string.Join(",", new[] { typeof(First), typeof(Second), typeof(int), typeof(int?), typeof(string), typeof(Color), typeof(List<int>), typeof(Dictionary<string, int>), typeof(object), typeof(XmlElement) }
                            .Select(type => first.GetTypeInfo(type) is null ? "no" : "yes"));

                        var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
                        options.TypeInfoResolverChain.Add(new FirstContext());
                        options.TypeInfoResolverChain.Add(new SecondContext());
                        var chained = XmlSerializer.Serialize(new Second { B = 2 }, options) + XmlSerializer.Serialize(new First { A = 1 }, options)
                            + ":" + options.GetTypeInfo<Second>().Converter.GetType().Name + ":" + XmlSerializer.Deserialize<Second>("<Second><B>5</B></Second>", options)!.B
                            + ":" + XmlSerializer.Serialize(new[] { new Second { B = 7 } }, options)
                            + ":" + ReferenceEquals(options.GetTypeInfo<Second>().Options, options) + ":" + ReferenceEquals(options.GetTypeInfo<First>().Options, options);

                        string missing;
                        try
                        {
                            missing = XmlSerializer.Serialize(new Third { C = 3 }, options);
                        }
                        catch (NotSupportedException ex)
                        {
                            missing = ex.Message;
                        }

                        string direct;
                        try
                        {
                            direct = XmlSerializer.Serialize(new Second { B = 2 }, typeof(Second), first);
                        }
                        catch (NotSupportedException ex)
                        {
                            direct = ex.Message;
                        }

                        return answers + "\n" + chained + "\n" + missing + "\n" + direct;
                    }
                }
                """).Should().Be(
                "yes,no,yes,yes,yes,no,no,no,yes,yes\n" +
                "<Second><B>2</B></Second><First><A>1</A></First>:SecondXmlConverter:5:<SecondArray><Second><B>7</B></Second></SecondArray>:True:True\n" +
                "The type 'Third' is not supported.\n" +
                "The type 'Second' is not supported by 'FirstContext'.");
        }

        [TestMethod]
        public void Enumerations_FollowTheConvertersTheCallerSupplies()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public enum Color
                {
                    Red,
                    Green,
                }

                [XmlConverter<SizeConverter>]
                public enum Size
                {
                    Small,
                    Large,
                }

                public sealed class SizeConverter : XmlConverter<Size>
                {
                    public override Size Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
                    {
                        var value = Size.Small;
                        while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
                        {
                            value = reader.GetString() == "L" ? Size.Large : Size.Small;
                        }

                        return value;
                    }

                    public override void Write(Utf8XmlWriter writer, Size value, XmlSerializerOptions options) => writer.WriteStringValue(value is Size.Large ? "L" : "S");
                }

                public sealed class Shouting : XmlConverter<Color>
                {
                    public override Color Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
                    {
                        var value = Color.Red;
                        while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
                        {
                            value = Enum.Parse<Color>(reader.GetString()!, ignoreCase: true);
                        }

                        return value;
                    }

                    public override void Write(Utf8XmlWriter writer, Color value, XmlSerializerOptions options) => writer.WriteStringValue(value.ToString().ToUpperInvariant());
                }

                public sealed class Shirt
                {
                    public Color Color { get; set; }

                    public Color? Trim { get; set; }

                    public Size Size { get; set; }

                    public List<Color> Stripes { get; set; } = new();
                }

                [XmlSerializable<Shirt>]
                [XmlSerializable<Color>]
                [XmlSerializable<Size>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        static string Show(Shirt shirt) => shirt.Color + "," + shirt.Trim + "," + shirt.Size + "," + string.Join("+", shirt.Stripes);
                        var shirt = new Shirt { Color = Color.Green, Trim = Color.Red, Size = Size.Large, Stripes = { Color.Red, Color.Green } };
                        return string.Join("\n",
                            Both.Write(shirt, o => new AppXmlContext(o).Shirt, Show),
                            Both.Write(shirt, o => new AppXmlContext(o).Shirt, Show, o => o.Converters.Add(new Shouting())),
                            Both.Write(Color.Green, o => new AppXmlContext(o).Color, c => c.ToString(), o => o.Converters.Add(new Shouting())),
                            Both.Write(Size.Large, o => new AppXmlContext(o).Size, s => s.ToString()));
                    }
                }
                """ + Compare).Should().Be(
                "<Shirt><Color>Green</Color><Trim>Red</Trim><Size>L</Size><Stripes>Red</Stripes><Stripes>Green</Stripes></Shirt>\nsame\nGreen,Red,Large,Red+Green\nGreen,Red,Large,Red+Green\n" +
                "<Shirt><Color>GREEN</Color><Trim>RED</Trim><Size>L</Size><Stripes>RED</Stripes><Stripes>GREEN</Stripes></Shirt>\nsame\nGreen,Red,Large,Red+Green\nGreen,Red,Large,Red+Green\n" +
                "<Color>GREEN</Color>\nsame\nGreen\nGreen\n" +
                "<Size>L</Size>\nsame\nLarge\nLarge");
        }

        [TestMethod]
        public void ChainedContexts_WriteWithTheOptionsThatHoldTheChain()
        {
            // Each context has options of its own, with other settings. The options that hold the chain decide.
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed class Part
                {
                    public string? Name { get; set; }

                    public int? Count { get; set; }
                }

                public sealed class Box
                {
                    public Part? Inner { get; set; }

                    public List<Part> Parts { get; set; } = new();
                }

                [XmlSourceGenerationOptions(WriteIndented = true)]
                [XmlSerializable<Part>]
                internal partial class PartContext : XmlSerializerContext
                {
                }

                [XmlSerializable<Box>]
                internal partial class BoxContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull };
                        options.TypeInfoResolverChain.Add(new BoxContext());
                        options.TypeInfoResolverChain.Add(PartContext.Default);
                        var box = new Box { Inner = new Part { Name = "a" }, Parts = { new Part { Count = 2 } } };
                        var chained = XmlSerializer.Serialize(box, options);
                        var read = XmlSerializer.Deserialize<Box>(chained, options)!;
                        var own = XmlSerializer.Serialize(new Part { Name = "a" }, PartContext.Default.Part);

                        string mismatch;
                        try
                        {
                            var camel = new XmlSerializerOptions { PropertyNamingPolicy = XmlNamingPolicy.CamelCase };
                            camel.TypeInfoResolverChain.Add(new BoxContext());
                            mismatch = XmlSerializer.Serialize(box, camel);
                        }
                        catch (NotSupportedException ex)
                        {
                            mismatch = ex.Message;
                        }

                        return chained + "\n" + read.Inner!.Name + "," + read.Parts[0].Count + "\n" + own.Replace("\r\n", "\n") + "\n" + mismatch;
                    }
                }
                """).Should().Be(
                "<Box><Inner><Name>a</Name></Inner><Parts><Count>2</Count></Parts></Box>\n" +
                "a,2\n" +
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Part>\n  <Name>a</Name>\n  <Count xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>\n</Part>\n" +
                "The generated converters were compiled with ElementNamingPolicy = none, and the options have XmlCamelCaseNamingPolicy. Set it with [XmlSourceGenerationOptions] on the context, or use the reflection resolver for these options.");
        }

        [TestMethod]
        public void GenericTypesAndSameNamedTypes_EachGetTheirOwnConverter()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                namespace Shop
                {
                    public sealed class Item
                    {
                        public string? Sku { get; set; }
                    }
                }

                namespace Depot
                {
                    public sealed class Item
                    {
                        public int Bin { get; set; }
                    }
                }

                public sealed class Pair<T>
                {
                    public T? First { get; set; }

                    public T? Second { get; set; }
                }

                public sealed class Order
                {
                    public Pair<int>? Numbers { get; set; }

                    public Pair<string>? Names { get; set; }

                    public Shop.Item? Sold { get; set; }

                    public Depot.Item? Stored { get; set; }
                }

                [XmlSerializable<Order>]
                [XmlSerializable<Pair<string>>(TypeInfoPropertyName = "Words")]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var order = new Order
                        {
                            Numbers = new Pair<int> { First = 1, Second = 2 },
                            Names = new Pair<string> { First = "a", Second = "b" },
                            Sold = new Shop.Item { Sku = "s" },
                            Stored = new Depot.Item { Bin = 4 },
                        };
                        return Both.Write(order, o => new AppXmlContext(o).Order, o => o.Numbers!.Second + "," + o.Names!.Second + "," + o.Sold!.Sku + "," + o.Stored!.Bin)
                            + "\n" + Both.Write(new Pair<string> { First = "x" }, o => new AppXmlContext(o).Words, p => p.First!, o => o.DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull)
                            + "\n" + string.Join(",", new[] { typeof(Pair<int>), typeof(Pair<string>), typeof(Shop.Item), typeof(Depot.Item) }
                                .Select(type => AppXmlContext.Default.GetTypeInfo(type)!.Converter.GetType().Name));
                    }
                }
                """ + Compare).Should().Be(
                "<Order><Numbers><First>1</First><Second>2</Second></Numbers><Names><First>a</First><Second>b</Second></Names><Sold><Sku>s</Sku></Sold><Stored><Bin>4</Bin></Stored></Order>\nsame\n2,b,s,4\n2,b,s,4\n" +
                "<PairOfString><First>x</First></PairOfString>\nsame\nx\nx\n" +
                "PairInt32XmlConverter,WordsXmlConverter,ItemXmlConverter,Item2XmlConverter");
        }

        [TestMethod]
        public void UnmappedMembers_AreSkippedOrRefusedAsTheOptionsSay()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed class Plain
                {
                    [XmlAttribute]
                    public string? Code { get; set; }

                    public int Count { get; set; }
                }

                public sealed class Keeper
                {
                    public int Count { get; set; }

                    [XmlExtensionData]
                    public XmlExtensionData? Extra { get; set; }
                }

                [XmlSerializable<Plain>]
                [XmlSerializable<Keeper>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        static string Read(string xml, bool disallow, bool generated)
                        {
                            var options = new XmlSerializerOptions { UnmappedMemberHandling = disallow ? XmlUnmappedMemberHandling.Disallow : XmlUnmappedMemberHandling.Skip };
                            try
                            {
                                var plain = generated ? XmlSerializer.Deserialize(xml, new AppXmlContext(options).Plain)! : XmlSerializer.Deserialize<Plain>(xml, options)!;
                                return plain.Code + ":" + plain.Count;
                            }
                            catch (XmlException ex)
                            {
                                return ex.Message;
                            }
                        }

                        const string Known = "<Plain xmlns:x=\"urn:x\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" Code=\"c\"><Count>1</Count></Plain>";
                        const string Element = "<Plain Code=\"c\"><Count>1</Count><Other>2</Other></Plain>";
                        const string Attribute = "<Plain Code=\"c\" other=\"2\"><Count>1</Count></Plain>";
                        const string Serializer = "<Plain xmlns=\"\" xmlns:r=\"urn:CloudNimble.Text.Xml:refs\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" r:id=\"1\" xsi:nil=\"false\" xsi:type=\"Plain\" Code=\"c\"><Count>1</Count></Plain>";
                        const string Lookalike = "<Plain Code=\"c\" nil=\"false\"><Count>1</Count></Plain>";
                        var lines = new List<string>();
                        foreach (var xml in new[] { Known, Element, Attribute, Serializer, Lookalike })
                        {
                            foreach (var disallow in new[] { false, true })
                            {
                                var generated = Read(xml, disallow, generated: true);
                                var reflected = Read(xml, disallow, generated: false);
                                lines.Add(generated + (generated == reflected ? "" : " != " + reflected));
                            }
                        }

                        var strict = new XmlSerializerOptions { UnmappedMemberHandling = XmlUnmappedMemberHandling.Disallow };
                        lines.Add(XmlSerializer.Deserialize("<Keeper other=\"2\"><Count>3</Count><Other/></Keeper>", new AppXmlContext(strict).Keeper)!.Count.ToString());
                        return string.Join("\n", lines);
                    }
                }
                """).Should().Be("c:1\nc:1\nc:1\nUnknown element 'Other'.\nc:1\nUnknown attribute 'other'.\nc:1\nc:1\nc:1\nUnknown attribute 'nil'.\n3");
        }

        [TestMethod]
        [DataRow("public sealed class Note { [XmlMixed] public List<XmlMixedContentItem> Body { get; set; } = new(); }", "XMLXE014", "'Note.Body' is marked [XmlMixed]; a generated converter would not read or write mixed content, so use the reflection resolver for the declaring type", DisplayName = "mixed content")]
        [DataRow("public sealed class Inner { public int A { get; set; } } public sealed class Note { [XmlText] public Inner? Body { get; set; } }", "XMLXE015", "Member 'Note.Body' is mapped as XML text, and its type is not a single value", DisplayName = "text that is an object")]
        [DataRow("public sealed class Note { [XmlText] public List<string> Body { get; set; } = new(); }", "XMLXE015", "Member 'Note.Body' is mapped as XML text, and its type is not a single value", DisplayName = "text that is a collection")]
        public void MembersAGeneratedConverterCannotHandle_AreReportedRatherThanDropped(string types, string id, string expectedMessage)
        {
            var source = Usings + types + "\n[XmlSerializable<Note>]\ninternal partial class AppXmlContext : XmlSerializerContext\n{\n}\n";
            var (_, diagnostics, _) = GeneratorTestHost.Run(source);
            diagnostics.Where(d => d.Id == id).Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).Should().Equal(expectedMessage);
        }

        [TestMethod]
        public void Numbers_ThatAreEmpty_AreRejectedAsTheReflectionResolverRejectsThem()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed class Tally
                {
                    public int Count { get; set; }

                    public decimal Price { get; set; }

                    public int? Limit { get; set; }
                }

                [XmlSerializable<Tally>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        static string Read(string xml, bool generated)
                        {
                            try
                            {
                                var tally = generated ? XmlSerializer.Deserialize(xml, new AppXmlContext(new XmlSerializerOptions()).Tally)! : XmlSerializer.Deserialize<Tally>(xml)!;
                                return tally.Count + "," + tally.Price.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + (tally.Limit?.ToString() ?? "null");
                            }
                            catch (XmlException ex)
                            {
                                return ex.Message;
                            }
                        }

                        var lines = new List<string>();
                        foreach (var xml in new[] { "<Tally><Count>1</Count><Price>2.50</Price><Limit>3</Limit></Tally>", "<Tally><Count/></Tally>", "<Tally><Price></Price></Tally>", "<Tally><Limit/></Tally>", "<Tally><Count> 7 </Count></Tally>" })
                        {
                            var generated = Read(xml, generated: true);
                            var reflected = Read(xml, generated: false);
                            lines.Add(generated + (generated == reflected ? "" : " != " + reflected));
                        }

                        return string.Join("\n", lines);
                    }
                }
                """).Should().Be("1,2.50,3\nThe value is not a valid integer.\nThe value is not a valid decimal.\nThe value is not a valid integer.\n7,0,null");
        }

        [TestMethod]
        public void NestedCollections_AreCreatedWithoutClosingATypeAtRunTime()
        {
            // A collection inside a collection, or as a dictionary value, is read and written by the runtime
            // library. The context creates its type info with every type argument named.
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public enum Color
                {
                    Red,
                    Green,
                }

                public sealed class NullableColorConverter : XmlConverter<Color?>
                {
                    public override Color? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
                    {
                        Color? value = null;
                        while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
                        {
                            value = reader.GetString() == "g" ? Color.Green : Color.Red;
                        }

                        return value;
                    }

                    public override void Write(Utf8XmlWriter writer, Color? value, XmlSerializerOptions options) => writer.WriteStringValue(value is Color.Green ? "g" : "r");
                }

                public sealed class Grid
                {
                    public List<List<int>> Rows { get; set; } = new();

                    public Dictionary<string, int[]> Named { get; set; } = new();

                    public Dictionary<string, Queue<int>> Queues { get; set; } = new();

                    public Color? Tint { get; set; }
                }

                [XmlSerializable<Grid>]
                [XmlSerializable<List<int[]>>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        static string Show(Grid grid) => string.Join("/", grid.Rows.Select(row => string.Join(",", row)))
                            + "|" + string.Join(",", grid.Named["a"]) + "|" + string.Join(",", grid.Queues["q"]) + "|" + grid.Tint;
                        var grid = new Grid
                        {
                            Rows = { new List<int> { 1, 2 }, new List<int> { 3 } },
                            Named = { ["a"] = new[] { 4, 5 } },
                            Queues = { ["q"] = new Queue<int>(new[] { 6 }) },
                            Tint = Color.Green,
                        };
                        return string.Join("\n",
                            Both.Write(grid, o => new AppXmlContext(o).Grid, Show),
                            Both.Write(grid, o => new AppXmlContext(o).Grid, Show, o => o.Converters.Add(new NullableColorConverter())),
                            Both.Write(new List<int[]> { new[] { 7 }, new[] { 8, 9 } }, o => (XmlTypeInfo<List<int[]>>)new AppXmlContext(o).GetTypeInfo(typeof(List<int[]>))!, l => string.Join("/", l.Select(a => string.Join(",", a)))));
                    }
                }
                """ + Compare).Should().Be(
                "<Grid><Rows><Int32>1</Int32><Int32>2</Int32></Rows><Rows><Int32>3</Int32></Rows><Named><a><Int32>4</Int32><Int32>5</Int32></a></Named>" +
                "<Queues><q><Int32>6</Int32></q></Queues><Tint>Green</Tint></Grid>\nsame\n1,2/3|4,5|6|Green\n1,2/3|4,5|6|Green\n" +
                "<Grid><Rows><Int32>1</Int32><Int32>2</Int32></Rows><Rows><Int32>3</Int32></Rows><Named><a><Int32>4</Int32><Int32>5</Int32></a></Named>" +
                "<Queues><q><Int32>6</Int32></q></Queues><Tint>g</Tint></Grid>\nsame\n1,2/3|4,5|6|Green\n1,2/3|4,5|6|Green\n" +
                "<ListOfInt32Array><Int32Array><Int32>7</Int32></Int32Array><Int32Array><Int32>8</Int32><Int32>9</Int32></Int32Array></ListOfInt32Array>\nsame\n7/8,9\n7/8,9");
        }

        [TestMethod]
        public void InheritedMembers_BringTheirTypesIntoTheContext()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed class Address
                {
                    public string? City { get; set; }
                }

                public class Party
                {
                    public Address? Home { get; set; }

                    public List<Address> Others { get; set; } = new();
                }

                public sealed class Customer : Party
                {
                    public string? Name { get; set; }
                }

                [XmlSerializable<Customer>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var customer = new Customer { Name = "Ada", Home = new Address { City = "Paris" }, Others = { new Address { City = "Rome" } } };
                        return Both.Write(customer, o => new AppXmlContext(o).Customer, c => c.Name + "," + c.Home!.City + "," + c.Others[0].City)
                            + "\n" + (AppXmlContext.Default.GetTypeInfo(typeof(Address)) is not null);
                    }
                }
                """ + Compare).Should().Be(
                "<Customer><Home><City>Paris</City></Home><Others><City>Rome</City></Others><Name>Ada</Name></Customer>\nsame\nAda,Paris,Rome\nAda,Paris,Rome\nTrue");
        }

        [TestMethod]
        public void ElementAttribute_OrderAndIsNullable_GiveTheSameDocument()
        {
            // IsNullable describes the member for a schema. It changes nothing that is written or read, by
            // either resolver, and Order is followed by both.
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed class Facets
                {
                    [XmlElement(Order = 2)]
                    public string? Last { get; set; }

                    [XmlElement(Order = 1, IsNullable = true)]
                    public int Count { get; set; }

                    [XmlElement(Order = 0, IsNullable = true)]
                    public string? First { get; set; }
                }

                [XmlSerializable<Facets>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        return Both.Write(new Facets { Last = "z", Count = 2 }, o => new AppXmlContext(o).Facets, f => (f.First ?? "null") + "," + f.Count + "," + f.Last);
                    }
                }
                """ + Compare).Should().Be(
                "<Facets><First xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><Count>2</Count><Last>z</Last></Facets>\nsame\nnull,2,z\nnull,2,z");
        }

        [TestMethod]
        public void ObjectCreationHandling_IsFollowedAsTheReflectionResolverFollowsIt()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed class Target
                {
                    [XmlArray]
                    public int[] Array { get; set; } = { 1 };

                    [XmlArray]
                    public IList<int> Fixed { get; set; } = new[] { 1 };

                    public IDictionary<string, int> FixedMap { get; set; } = new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });

                    [XmlArray]
                    public List<int> GetOnly { get; } = new() { 1 };

                    public Dictionary<string, int> GetOnlyMap { get; } = new() { ["a"] = 1 };

                    [XmlElement("Loose")]
                    public List<int> Loose { get; } = new() { 1 };

                    public Dictionary<string, int> Map { get; set; } = new() { ["a"] = 1 };

                    [XmlArray]
                    [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)]
                    public List<int> Marked { get; } = new() { 1 };

                    [XmlArray]
                    public List<int>? NullGetOnly { get; }

                    [XmlArray]
                    public List<int>? NullSettable { get; set; }

                    [XmlArray]
                    public Queue<int> Queue { get; } = new(new[] { 1 });

                    [XmlArray]
                    [XmlObjectCreationHandling(XmlObjectCreationHandling.Replace)]
                    public List<int> Replaced { get; set; } = new() { 1 };

                    [XmlArray]
                    public IEnumerable<int> Sequence { get; } = new List<int> { 1 };

                    [XmlArray]
                    public HashSet<int> Set { get; } = new() { 1 };

                    [XmlArray]
                    public List<int> Settable { get; set; } = new() { 1 };

                    [XmlArray]
                    public Stack<int> Stack { get; } = new(new[] { 1 });
                }

                [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)]
                public sealed class MarkedType
                {
                    [XmlArray]
                    public int[] Array { get; set; } = { 1 };

                    [XmlArray]
                    public List<int> Items { get; } = new() { 1 };

                    public string Name { get; set; } = "n";
                }

                public sealed record WithConstructor(string Name)
                {
                    [XmlArray]
                    public List<int> Items { get; } = new() { 1 };
                }

                public sealed class Child
                {
                    public int A { get; set; } = 1;

                    public int B { get; set; }
                }

                public sealed class Holder
                {
                    public Child Child { get; } = new() { A = 7 };
                }

                public sealed class ReplacingHolder
                {
                    [XmlObjectCreationHandling(XmlObjectCreationHandling.Replace)]
                    public Child Child { get; set; } = new() { A = 7 };
                }

                [XmlSerializable<Target>]
                [XmlSerializable<MarkedType>]
                [XmlSerializable<WithConstructor>]
                [XmlSerializable<Holder>]
                [XmlSerializable<ReplacingHolder>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    private const string Nil = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>";

                    private static string Join<T>(IEnumerable<T>? items) => items is null ? "null" : "[" + string.Join(",", items) + "]";

                    private static string Join(Dictionary<string, int>? map) => map is null ? "null" : "[" + string.Join(",", System.Linq.Enumerable.Select(map, pair => pair.Key + "=" + pair.Value)) + "]";

                    // Runs the same work through a generated context and through the reflection resolver, each with
                    // its own options, and reports one result when they agree.
                    private static string Same(bool populate, Func<XmlSerializerOptions, string> generated, Func<XmlSerializerOptions, string> reflected)
                    {
                        string Run(Func<XmlSerializerOptions, string> work)
                        {
                            var options = new XmlSerializerOptions();
                            if (populate)
                            {
                                options.PreferredObjectCreationHandling = XmlObjectCreationHandling.Populate;
                            }

                            try
                            {
                                return work(options);
                            }
                            catch (Exception ex)
                            {
                                return ex.GetType().Name + ": " + ex.Message;
                            }
                        }

                        var fromContext = Run(generated);
                        var fromReflection = Run(reflected);
                        return fromContext == fromReflection ? fromContext : fromContext + " <> reflection: " + fromReflection;
                    }

                    private static string Show(Target t) => string.Join(" ",
                        "Array" + Join(t.Array), "Fixed" + Join(t.Fixed), "GetOnly" + Join(t.GetOnly), "GetOnlyMap" + Join(t.GetOnlyMap), "Loose" + Join(t.Loose),
                        "Map" + Join(t.Map), "Marked" + Join(t.Marked), "NullGetOnly" + Join(t.NullGetOnly), "NullSettable" + Join(t.NullSettable), "Queue" + Join(t.Queue),
                        "Replaced" + Join(t.Replaced), "Sequence" + Join(t.Sequence), "Set" + Join(t.Set), "Settable" + Join(t.Settable), "Stack" + Join(t.Stack));

                    private static string ReadTarget(bool populate, string content)
                    {
                        var xml = "<Target>" + content + "</Target>";
                        return Same(populate, o => Show(XmlSerializer.Deserialize(xml, new AppXmlContext(o).Target)!), o => Show((Target)XmlSerializer.Deserialize(xml, typeof(Target), o)!));
                    }

                    public static string Run()
                    {
                        const string all =
                            "<Array><Int32>9</Int32></Array><GetOnly><Int32>9</Int32></GetOnly><GetOnlyMap><a>5</a><b>2</b></GetOnlyMap><Loose>8</Loose><Loose>9</Loose>" +
                            "<Map><b>2</b></Map><Marked><Int32>9</Int32></Marked><NullGetOnly><Int32>9</Int32></NullGetOnly><NullSettable><Int32>9</Int32></NullSettable>" +
                            "<Queue><Int32>9</Int32></Queue><Replaced><Int32>9</Int32></Replaced><Sequence><Int32>9</Int32></Sequence><Set><Int32>9</Int32></Set>" +
                            "<Settable><Int32>9</Int32></Settable><Stack><Int32>8</Int32><Int32>9</Int32></Stack>";
                        const string marked = "<MarkedType><Array><Int32>9</Int32></Array><Items><Int32>9</Int32></Items><Name>x</Name></MarkedType>";
                        const string constructed = "<WithConstructor><Name>n</Name><Items><Int32>9</Int32></Items></WithConstructor>";
                        const string held = "<Holder><Child><B>2</B></Child></Holder>";
                        const string replacing = "<ReplacingHolder><Child><B>2</B></Child></ReplacingHolder>";
                        return string.Join("\n",
                            ReadTarget(false, all),
                            ReadTarget(true, all),
                            ReadTarget(true, "<Fixed><Int32>9</Int32></Fixed>"),
                            ReadTarget(true, "<FixedMap><b>2</b></FixedMap>"),
                            ReadTarget(false, "<Fixed><Int32>9</Int32></Fixed>"),
                            ReadTarget(true, "<Map" + Nil + "<Settable" + Nil),
                            ReadTarget(true, "<GetOnly" + Nil),
                            ReadTarget(true, "<GetOnlyMap" + Nil),
                            ReadTarget(true, "<Marked" + Nil),
                            ReadTarget(false, "<GetOnly" + Nil + "<GetOnlyMap" + Nil),
                            Same(false,
                                o => { var m = XmlSerializer.Deserialize(marked, new AppXmlContext(o).MarkedType)!; return Join(m.Array) + Join(m.Items) + m.Name; },
                                o => { var m = (MarkedType)XmlSerializer.Deserialize(marked, typeof(MarkedType), o)!; return Join(m.Array) + Join(m.Items) + m.Name; }),
                            Same(true,
                                o => Join(XmlSerializer.Deserialize(constructed, new AppXmlContext(o).WithConstructor)!.Items),
                                o => Join(((WithConstructor)XmlSerializer.Deserialize(constructed, typeof(WithConstructor), o)!).Items)),
                            Same(false,
                                o => { var h = XmlSerializer.Deserialize(held, new AppXmlContext(o).Holder)!; return h.Child.A + "," + h.Child.B; },
                                o => { var h = (Holder)XmlSerializer.Deserialize(held, typeof(Holder), o)!; return h.Child.A + "," + h.Child.B; }),
                            Same(true,
                                o => { var h = XmlSerializer.Deserialize(held, new AppXmlContext(o).Holder)!; return h.Child.A + "," + h.Child.B; },
                                o => { var h = (Holder)XmlSerializer.Deserialize(held, typeof(Holder), o)!; return h.Child.A + "," + h.Child.B; }),
                            Same(true,
                                o => { var h = XmlSerializer.Deserialize(replacing, new AppXmlContext(o).ReplacingHolder)!; return h.Child.A + "," + h.Child.B; },
                                o => { var h = (ReplacingHolder)XmlSerializer.Deserialize(replacing, typeof(ReplacingHolder), o)!; return h.Child.A + "," + h.Child.B; }));
                    }
                }
                """).Should().Be(string.Join("\n",
                "Array[9] Fixed[1] GetOnly[1] GetOnlyMap[a=1] Loose[1] Map[b=2] Marked[1,9] NullGetOnlynull NullSettable[9] Queue[1] Replaced[9] Sequence[1] Set[1] Settable[9] Stack[1]",
                "Array[9] Fixed[1] GetOnly[1,9] GetOnlyMap[a=5,b=2] Loose[1,8,9] Map[a=1,b=2] Marked[1,9] NullGetOnlynull NullSettable[9] Queue[1,9] Replaced[9] Sequence[1] Set[1,9] Settable[1,9] Stack[8,9,1]",
                "NotSupportedException: The collection of type 'System.Int32[]' held by the member 'Fixed' is read-only and cannot be populated.",
                "NotSupportedException: The dictionary of type 'System.Collections.ObjectModel.ReadOnlyDictionary`2[System.String,System.Int32]' is read-only and cannot be populated.",
                "Array[1] Fixed[9] GetOnly[1] GetOnlyMap[a=1] Loose[1] Map[a=1] Marked[1] NullGetOnlynull NullSettablenull Queue[1] Replaced[1] Sequence[1] Set[1] Settable[1] Stack[1]",
                "Array[1] Fixed[1] GetOnly[1] GetOnlyMap[a=1] Loose[1] Mapnull Marked[1] NullGetOnlynull NullSettablenull Queue[1] Replaced[1] Sequence[1] Set[1] Settablenull Stack[1]",
                "InvalidOperationException: Unable to assign 'null' to the property or field of type 'System.Collections.Generic.List`1[System.Int32]'.",
                "InvalidOperationException: Unable to assign 'null' to the property or field of type 'System.Collections.Generic.Dictionary`2[System.String,System.Int32]'.",
                "InvalidOperationException: Unable to assign 'null' to the property or field of type 'System.Collections.Generic.List`1[System.Int32]'.",
                "Array[1] Fixed[1] GetOnly[1] GetOnlyMap[a=1] Loose[1] Map[a=1] Marked[1] NullGetOnlynull NullSettablenull Queue[1] Replaced[1] Sequence[1] Set[1] Settable[1] Stack[1]",
                "[9][1,9]x",
                "[1]",
                "7,0",
                // The reflection resolver sets the members of the object the member holds. A generated converter
                // cannot, and says so instead of creating a new object.
                "NotSupportedException: The generated converter for 'global::Holder' cannot populate the member 'Child'. Mark the member [XmlObjectCreationHandling(XmlObjectCreationHandling.Replace)], or read this type through the reflection resolver. <> reflection: 7,2",
                "1,2"));
        }

        [TestMethod]
        [DataRow("public sealed class Bad { [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)] public int[] Items { get; set; } = { 1 }; }", "'Bad.Items' is marked with XmlObjectCreationHandling.Populate, but its type cannot be populated", DisplayName = "an array")]
        [DataRow("public sealed class Bad { [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)] public string Name { get; set; } = \"n\"; }", "'Bad.Name' is marked with XmlObjectCreationHandling.Populate, but its type cannot be populated", DisplayName = "a string")]
        [DataRow("public sealed class Child { public int A { get; set; } } public sealed class Bad { [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)] public Child Child { get; } = new(); }", "'Bad.Child' is marked with XmlObjectCreationHandling.Populate, which a generated converter cannot do for this member; read the type through the reflection resolver", DisplayName = "an object")]
        [DataRow("public sealed class Child { public int A { get; set; } } [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)] public sealed class Bad { public Child Child { get; } = new(); }", "'Bad.Child' is marked with XmlObjectCreationHandling.Populate, which a generated converter cannot do for this member; read the type through the reflection resolver", DisplayName = "an object, marked on the type")]
        [DataRow("public sealed class Bad { [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)] public System.Collections.Generic.List<int> Items { get; init; } = new(); }", "'Bad.Items' is marked with XmlObjectCreationHandling.Populate, which a generated converter cannot do for this member; read the type through the reflection resolver", DisplayName = "a member with an init accessor")]
        [DataRow("public sealed record Bad(string Name) { [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)] public System.Collections.Generic.List<int> Items { get; } = new(); }", "'Bad.Items' is marked with XmlObjectCreationHandling.Populate, which is not supported in a type created through a constructor with parameters", DisplayName = "a constructor with parameters")]
        public void ObjectCreationHandling_ThatAGeneratedConverterCannotFollow_IsReported(string types, string expectedMessage)
        {
            var source = "using CloudNimble.Text.Xml;\nusing CloudNimble.Text.Xml.Serialization;\n" + types + "\n[XmlSerializable<Bad>]\ninternal partial class AppXmlContext : XmlSerializerContext\n{\n}\n";
            var (_, diagnostics, _) = GeneratorTestHost.Run(source);
            diagnostics.Where(d => d.Id is "XMLXE016").Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).Should().Equal(expectedMessage);
        }

        [TestMethod]
        public void NullWrappedCollection_IsNil_UnlessTheIgnoreConditionLeavesItOut()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed class Team
                {
                    [XmlArray]
                    public List<string>? Members { get; set; }
                }

                [XmlSerializable<Team>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        static string Show(Team team) => team.Members is null ? "null" : "count " + team.Members.Count;
                        return Both.Write(new Team(), o => new AppXmlContext(o).Team, Show)
                            + "\n" + Both.Write(new Team(), o => new AppXmlContext(o).Team, Show, o => o.DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull)
                            + "\n" + Both.Write(new Team(), o => new AppXmlContext(o).Team, Show, o => o.DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingDefault);
                    }
                }
                """ + Compare).Should().Be(
                "<Team><Members xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></Team>\nsame\nnull\nnull\n" +
                "<Team/>\nsame\nnull\nnull\n" +
                "<Team/>\nsame\nnull\nnull");
        }

        [TestMethod]
        public void XmlValuesAndObjects_AreMembers()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed class Envelope
                {
                    public XmlElement Element { get; set; }
                    public XmlNode? Node { get; set; }
                    public object? Any { get; set; }
                }

                [XmlSerializable<Envelope>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        using var document = XmlDocument.Parse("<foo a=\"1\"/>");
                        var value = new Envelope { Element = document.RootElement, Node = XmlNode.Parse("<n>1</n>"), Any = 5 };
                        return Both.Write(value, o => new AppXmlContext(o).Envelope, e => e.Element.ValueKind + "|" + e.Node + "|" + ((XmlElement)e.Any!).GetInt32());
                    }
                }
                """ + Compare).Should().Be(
                "<Envelope><Element><foo a=\"1\"/></Element><Node><n>1</n></Node><Any>5</Any></Envelope>\nsame\nEmptyElement|<n>1</n>|5\nEmptyElement|<n>1</n>|5");
        }

        #endregion

    }

}
