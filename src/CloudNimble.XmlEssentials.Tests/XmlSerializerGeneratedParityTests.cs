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
                "<Kinds><Queue>1</Queue><Queue>2</Queue><Stack>2</Stack><Stack>1</Stack><Set>3</Set><Array>4</Array><Array>5</Array><List><string>a</string></List>" +
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
                            + ":" + options.GetTypeInfo<Second>().Properties.Count + ":" + XmlSerializer.Deserialize<Second>("<Second><B>5</B></Second>", options)!.B
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
                "<Second><B>2</B></Second><First><A>1</A></First>:1:5:<ArrayOfSecond><Second><B>7</B></Second></ArrayOfSecond>:True:True\n" +
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
                // A naming policy the context was not compiled with is applied from the metadata, not refused.
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><box><inner><name>a</name><count xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></inner><parts><name xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><count>2</count></parts></box>");
        }

        [TestMethod]
        public void GenericTypesAndSameNamedTypes_EachGetTheirOwnTypeInfo()
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
                                .Select(type => AppXmlContext.Default.GetTypeInfo(type)!.Type.ToString()));
                    }
                }
                """ + Compare).Should().Be(
                "<Order><Numbers><First>1</First><Second>2</Second></Numbers><Names><First>a</First><Second>b</Second></Names><Sold><Sku>s</Sku></Sold><Stored><Bin>4</Bin></Stored></Order>\nsame\n2,b,s,4\n2,b,s,4\n" +
                "<PairOfString><First>x</First></PairOfString>\nsame\nx\nx\n" +
                "Pair`1[System.Int32],Pair`1[System.String],Shop.Item,Depot.Item");
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
                """).Should().Be("c:1\nc:1\nc:1\nUnknown element 'Other'. Path: /Plain | LineNumber: 1 | BytePositionInLine: 40.\nc:1\nUnknown attribute 'other'. Path: /Plain | LineNumber: 1 | BytePositionInLine: 27.\nc:1\nc:1\nc:1\nUnknown attribute 'nil'. Path: /Plain | LineNumber: 1 | BytePositionInLine: 29.\n3");
        }

        [TestMethod]
        [DataRow("public sealed class Inner { public int A { get; set; } } public sealed class Note { [XmlText] public Inner? Body { get; set; } }", "XMLESS015", "Member 'Note.Body' is mapped as XML text, and its type is not a single value", DisplayName = "text that is an object")]
        [DataRow("public sealed class Note { [XmlText] public List<string> Body { get; set; } = new(); }", "XMLESS015", "Member 'Note.Body' is mapped as XML text, and its type is not a single value", DisplayName = "text that is a collection")]
        public void TextMembersThatAreNotASingleValue_AreReportedRatherThanDropped(string types, string id, string expectedMessage)
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
                """).Should().Be(
                    "1,2.50,3\n" +
                    "The value is not a valid integer. Path: /Tally/Count | LineNumber: 1 | BytePositionInLine: 16.\n" +
                    "The value is not a valid decimal. Path: /Tally/Price | LineNumber: 1 | BytePositionInLine: 23.\n" +
                    "The value is not a valid integer. Path: /Tally/Limit | LineNumber: 1 | BytePositionInLine: 16.\n" +
                    "7,0,null");
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
                "<Grid><Rows><int>1</int><int>2</int></Rows><Rows><int>3</int></Rows><Named><a><int>4</int><int>5</int></a></Named>" +
                "<Queues><q><int>6</int></q></Queues><Tint>Green</Tint></Grid>\nsame\n1,2/3|4,5|6|Green\n1,2/3|4,5|6|Green\n" +
                "<Grid><Rows><int>1</int><int>2</int></Rows><Rows><int>3</int></Rows><Named><a><int>4</int><int>5</int></a></Named>" +
                "<Queues><q><int>6</int></q></Queues><Tint>g</Tint></Grid>\nsame\n1,2/3|4,5|6|Green\n1,2/3|4,5|6|Green\n" +
                "<ArrayOfArrayOfInt><ArrayOfInt><int>7</int></ArrayOfInt><ArrayOfInt><int>8</int><int>9</int></ArrayOfInt></ArrayOfArrayOfInt>\nsame\n7/8,9\n7/8,9");
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
        public void GenerationMode_DecidesWhatAContextCanDo()
        {
            // Default and Metadata | Serialization give both the metadata and the write fast path. Metadata gives the
            // metadata alone. Serialization gives the fast path alone: it writes with the options it was compiled
            // for and does nothing else, as System.Text.Json has it. The mode on [XmlSerializable] wins for its type.
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed class Part
                {
                    public string? Name { get; set; }
                }

                public sealed class Crate
                {
                    public Part? Inner { get; set; }

                    public List<Part> Many { get; set; } = new();
                }

                [XmlDerivedType<Bolt>("bolt")]
                public class Fastener
                {
                    public int Size { get; set; }
                }

                public sealed class Bolt : Fastener
                {
                }

                public sealed class Tool
                {
                    public string? Name { get; set; }
                }

                [XmlSerializable<Part>]
                internal partial class DefaultContext : XmlSerializerContext
                {
                }

                [XmlSourceGenerationOptions(GenerationMode = XmlSourceGenerationMode.Metadata)]
                [XmlSerializable<Part>]
                internal partial class MetadataContext : XmlSerializerContext
                {
                }

                [XmlSourceGenerationOptions(GenerationMode = XmlSourceGenerationMode.Serialization)]
                [XmlSerializable<Part>]
                [XmlSerializable<Crate>]
                [XmlSerializable<Fastener>]
                internal partial class SerializationContext : XmlSerializerContext
                {
                }

                [XmlSourceGenerationOptions(GenerationMode = XmlSourceGenerationMode.Metadata | XmlSourceGenerationMode.Serialization)]
                [XmlSerializable<Part>]
                internal partial class BothContext : XmlSerializerContext
                {
                }

                [XmlSourceGenerationOptions(GenerationMode = XmlSourceGenerationMode.Metadata)]
                [XmlSerializable<Part>(GenerationMode = XmlSourceGenerationMode.Serialization)]
                [XmlSerializable<Tool>]
                internal partial class MixedContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    private static string Try(Func<string> work)
                    {
                        try
                        {
                            return work();
                        }
                        catch (Exception ex)
                        {
                            return ex.GetType().Name;
                        }
                    }

                    private static string Show(Func<XmlSerializerOptions, XmlTypeInfo<Part>> info)
                    {
                        var plain = info(new XmlSerializerOptions { OmitXmlDeclaration = true });
                        var camel = info(new XmlSerializerOptions { OmitXmlDeclaration = true, PropertyNamingPolicy = XmlNamingPolicy.CamelCase });
                        return plain.Properties.Count
                            + " " + Try(() => XmlSerializer.Serialize(new Part { Name = "a" }, plain))
                            + " " + Try(() => XmlSerializer.Deserialize("<Part><Name>b</Name></Part>", plain)!.Name!)
                            + " " + Try(() => XmlSerializer.Serialize(new Part { Name = "a" }, camel))
                            + " " + Try(() => XmlSerializer.Deserialize("<part><name>b</name></part>", camel)!.Name!);
                    }

                    public static string Run()
                    {
                        var mixed = new MixedContext(new XmlSerializerOptions { OmitXmlDeclaration = true });
                        var serialization = new SerializationContext(new XmlSerializerOptions { OmitXmlDeclaration = true });
                        return string.Join("\n",
                            Show(o => new DefaultContext(o).Part),
                            Show(o => new MetadataContext(o).Part),
                            Show(o => new SerializationContext(o).Part),
                            Show(o => new BothContext(o).Part),
                            Show(o => new MixedContext(o).Part),
                            mixed.Tool.Properties.Count + " " + XmlSerializer.Serialize(new Tool { Name = "t" }, mixed.Tool) + " " + XmlSerializer.Deserialize("<Tool><Name>u</Name></Tool>", mixed.Tool)!.Name,
                            Try(() => XmlSerializer.Serialize(new Crate { Inner = new Part { Name = "i" }, Many = { new Part { Name = "m" } } }, serialization.Crate)),
                            Try(() => XmlSerializer.Serialize(new Fastener { Size = 1 }, serialization.Fastener)));
                    }
                }
                """).Should().Be(string.Join("\n",
                "1 <Part><Name>a</Name></Part> b <part><name>a</name></part> b",
                "1 <Part><Name>a</Name></Part> b <part><name>a</name></part> b",
                "0 <Part><Name>a</Name></Part> InvalidOperationException InvalidOperationException InvalidOperationException",
                "1 <Part><Name>a</Name></Part> b <part><name>a</name></part> b",
                "0 <Part><Name>a</Name></Part> InvalidOperationException InvalidOperationException InvalidOperationException",
                "1 <Tool><Name>t</Name></Tool> u",
                // A serialization-only context writes a graph of types that each have a fast path. A type with derived
                // types has none, and with no metadata either it cannot be written, as in System.Text.Json.
                "<Crate><Inner><Name>i</Name></Inner><Many><Name>m</Name></Many></Crate>",
                "InvalidOperationException"));
        }

        [TestMethod]
        public void MembersOfTheSameName_FollowTheRulesOfSystemTextJson()
        {
            // Each expected line is what System.Text.Json gives for the same types with [JsonIgnore] and
            // [JsonPropertyName]: an override or a member declared with new takes the place of the base member, an
            // ignored override hides the property it overrides, an ignored new member does not hide the one it
            // hides, attributes are not inherited by an override, and unrelated members with one name are an error.
            XmlSerializerGeneratorTests.RunGenerated("#nullable enable\n" + Usings + """
                public class A1 { public virtual int X { get; set; } }
                public class A2 : A1 { public override int X { get; set; } }
                public class B1 { public int X { get; set; } = 9; }
                public class B2 : B1 { public new int X { get; set; } }
                public class C1 { public virtual int X { get; set; } }
                public class C2 : C1 { [XmlIgnore] public override int X { get; set; } }
                public class D1 { [XmlIgnore] public virtual int X { get; set; } }
                public class D2 : D1 { public override int X { get; set; } }
                public class E1 { public int X { get; set; } [XmlElement("X")] public int Y { get; set; } }
                public class F1 { public int X { get; set; } = 9; }
                public class F2 : F1 { public new string? X { get; set; } }
                public class G1 { [XmlElement("x")] public int A { get; set; } }
                public class G2 : G1 { [XmlElement("x")] public int B { get; set; } }
                public class H1 { public int X { get; set; } = 9; }
                public class H2 : H1 { [XmlIgnore] public new int X { get; set; } }
                public class I1 { [XmlElement("X")] public int A { get; set; } }
                public class I2 : I1 { public int X { get; set; } }
                public class J1 { [XmlElement("n", Order = 5)] public virtual int X { get; set; } public int Y { get; set; } }
                public class J2 : J1 { public override int X { get; set; } }
                public class K1 { public int X { get; set; } = 9; }
                public class K2 : K1 { [XmlIgnore] public new int X { get; set; } [XmlElement("X")] public int Y { get; set; } }
                public class L1 { public virtual int X { get; set; } = 9; }
                public class L2 : L1 { [XmlIgnore] public new virtual string? X { get; set; } }
                public class M1 { public virtual int X { get; set; } = 9; }
                public class M2 : M1 { [XmlIgnore] public new int X { get; set; } }
                public class N1 { public virtual int X { get; set; } }
                public class N2 : N1 { public override int X { get; set; } }
                public class N3 : N2 { [XmlIgnore] public override int X { get; set; } }
                public interface IX { int X { get; set; } }
                public class O1 : IX { public int X { get; set; } = 9; }
                public class O2 : O1, IX { [XmlIgnore] public new int X { get; set; } }
                public class P1 { public int X; }
                public class P2 : P1 { public new int X; }

                [XmlSerializable<A2>]
                [XmlSerializable<B2>]
                [XmlSerializable<C2>]
                [XmlSerializable<D2>]
                [XmlSerializable<E1>]
                [XmlSerializable<F2>]
                [XmlSerializable<G2>]
                [XmlSerializable<H2>]
                [XmlSerializable<I2>]
                [XmlSerializable<J2>]
                [XmlSerializable<K2>]
                [XmlSerializable<L2>]
                [XmlSerializable<M2>]
                [XmlSerializable<N3>]
                [XmlSerializable<O2>]
                [XmlSerializable<P2>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    // Writes the value through a generated context and through the reflection resolver, and reports
                    // one result when they agree.
                    private static string Same<T>(T value, Func<AppXmlContext, XmlTypeInfo<T>> info, bool fields = false)
                    {
                        string Run(bool generated)
                        {
                            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, IncludeFields = fields };
                            try
                            {
                                return generated ? XmlSerializer.Serialize(value, info(new AppXmlContext(options))) : XmlSerializer.Serialize(value, typeof(T), options);
                            }
                            catch (Exception ex)
                            {
                                return ex.GetType().Name + ": " + ex.Message;
                            }
                        }

                        var fromContext = Run(true);
                        var fromReflection = Run(false);
                        return fromContext == fromReflection ? fromContext : fromContext + " <> reflection: " + fromReflection;
                    }

                    public static string Run()
                    {
                        var read = XmlSerializer.Deserialize("<A2><X>5</X></A2>", new AppXmlContext(new XmlSerializerOptions()).A2)!;
                        return string.Join("\n",
                            Same(new A2 { X = 1 }, c => c.A2),
                            read.X.ToString(),
                            Same(new B2 { X = 1 }, c => c.B2),
                            Same(new C2 { X = 1 }, c => c.C2),
                            Same(new D2 { X = 1 }, c => c.D2),
                            Same(new E1 { X = 1, Y = 2 }, c => c.E1),
                            Same(new F2 { X = "s" }, c => c.F2),
                            Same(new G2 { A = 1, B = 2 }, c => c.G2),
                            Same(new H2 { X = 1 }, c => c.H2),
                            Same(new I2 { A = 1, X = 2 }, c => c.I2),
                            Same(new J2 { X = 1, Y = 2 }, c => c.J2),
                            Same(new K2 { X = 1, Y = 2 }, c => c.K2),
                            Same(new L2 { X = "s" }, c => c.L2),
                            Same(new M2 { X = 1 }, c => c.M2),
                            Same(new N3 { X = 1 }, c => c.N3),
                            Same(new O2 { X = 1 }, c => c.O2),
                            Same(new P2 { X = 1 }, c => c.P2, fields: true),
                            Same(new P2 { X = 1 }, c => c.P2));
                    }
                }
                """).Should().Be(string.Join("\n",
                "<A2><X>1</X></A2>",
                "5",
                "<B2><X>1</X></B2>",
                "<C2/>",
                "<D2><X>1</X></D2>",
                "InvalidOperationException: The type maps more than one member to the element name 'X'.",
                "<F2><X>s</X></F2>",
                "InvalidOperationException: The type maps more than one member to the element name 'x'.",
                "<H2><X>9</X></H2>",
                "InvalidOperationException: The type maps more than one member to the element name 'X'.",
                // The override does not take the name or the order of the property it overrides, so both are written.
                "<J2><n>1</n><Y>2</Y><X>1</X></J2>",
                "<K2><X>2</X></K2>",
                "<L2><X>9</X></L2>",
                "<M2><X>9</X></M2>",
                "<N3/>",
                "<O2/>",
                "<P2><X>1</X></P2>",
                "<P2/>"));
        }

        [TestMethod]
        public void EveryFeatureOfTheEngine_WorksThroughAGeneratedContext()
        {
            // A generated context hands the runtime library metadata, so it has what the reflection resolver has:
            // derived types, member converters, mixed content, extension data, references, populated objects,
            // init-only and required members, and members and constructors that are not public.
            XmlSerializerGeneratorTests.RunGenerated("#nullable enable\n" + Usings + """
                [XmlPolymorphic(TypeDiscriminatorPropertyName = "kind", DiscriminatorPlacement = XmlDiscriminatorPlacement.Attribute)]
                [XmlDerivedType<Circle>("circle")]
                public class Shape
                {
                    public int Id { get; set; }
                }

                public sealed class Circle : Shape
                {
                    public int Radius { get; set; }
                }

                public sealed class Ring
                {
                    public List<Circle> Items { get; set; } = new();
                }

                public sealed class Drawing
                {
                    public Shape? Main { get; set; }

                    public List<Shape> All { get; set; } = new();
                }

                public sealed class Hex : XmlConverter<int>
                {
                    public override int Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
                    {
                        var value = 0;
                        while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
                        {
                            value = Convert.ToInt32(reader.GetString(), 16);
                        }

                        return value;
                    }

                    public override void Write(Utf8XmlWriter writer, int value, XmlSerializerOptions options) => writer.WriteStringValue(value.ToString("x"));
                }

                public sealed class Tagged
                {
                    [XmlConverter<Hex>]
                    public int Flags { get; set; }

                    [XmlConverter<Hex>]
                    public int? Maybe { get; set; }
                }

                public sealed class Note
                {
                    [XmlMixed]
                    public List<XmlMixedContentItem> Body { get; set; } = new();
                }

                public sealed class Bag
                {
                    [XmlExtensionData]
                    public XmlExtensionData Extra { get; set; } = new();

                    public string? Name { get; set; }
                }

                public sealed class Node
                {
                    public Node? Next { get; set; }

                    public string? Name { get; set; }
                }

                public sealed class Child
                {
                    public int A { get; set; } = 7;

                    public int B { get; set; }
                }

                public sealed class Holder
                {
                    public Child Child { get; } = new();

                    public string? Label { get; init; }

                    public List<int> Items { get; } = new() { 1 };
                }

                public sealed class Hidden
                {
                    [XmlInclude]
                    internal int Secret { get; set; } = 5;

                    [XmlInclude]
                    private int Hush { get; set; } = 6;

                    public int Seen { get; private set; } = 1;

                    public int Peek() => Hush;
                }

                public sealed class Odd
                {
                    [XmlConstructor]
                    private Odd(string name)
                    {
                        Name = name + "!";
                    }

                    public string Name { get; }
                }

                public sealed class Needs
                {
                    public required string Name { get; set; }

                    public required int Count { get; init; }
                }

                [XmlSerializable<Shape>]
                [XmlSerializable<Drawing>]
                [XmlSerializable<Ring>]
                [XmlSerializable<Tagged>]
                [XmlSerializable<Note>]
                [XmlSerializable<Bag>]
                [XmlSerializable<Node>]
                [XmlSerializable<Holder>]
                [XmlSerializable<Hidden>]
                [XmlSerializable<Needs>]
                [XmlSerializable<Odd>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    // Runs the same work through a generated context and through the reflection resolver, each with
                    // its own options, and reports one result when they agree.
                    private static string Same(Func<XmlSerializerOptions, AppXmlContext?, string> work, Action<XmlSerializerOptions>? configure = null)
                    {
                        string Run(bool generated)
                        {
                            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
                            configure?.Invoke(options);
                            try
                            {
                                return work(options, generated ? new AppXmlContext(options) : null);
                            }
                            catch (Exception ex)
                            {
                                return ex.GetType().Name + ": " + ex.Message;
                            }
                        }

                        var fromContext = Run(true);
                        var fromReflection = Run(false);
                        return fromContext == fromReflection ? fromContext : fromContext + " <> reflection: " + fromReflection;
                    }

                    private static string Write<T>(T value, XmlSerializerOptions options, XmlTypeInfo<T>? info) => info is null ? XmlSerializer.Serialize(value, typeof(T), options) : XmlSerializer.Serialize(value, info);

                    private static T Read<T>(string xml, XmlSerializerOptions options, XmlTypeInfo<T>? info) => info is null ? (T)XmlSerializer.Deserialize(xml, typeof(T), options)! : XmlSerializer.Deserialize(xml, info)!;

                    public static string Run()
                    {
                        const string shape = "<Shape kind=\"circle\"><Id>1</Id><Radius>2</Radius></Shape>";
                        const string tagged = "<Tagged><Flags>ff</Flags><Maybe>10</Maybe></Tagged>";
                        const string note = "<Note>a<b>1</b>c</Note>";
                        const string bag = "<Bag other=\"1\"><Name>n</Name><More>m</More></Bag>";
                        const string holder = "<Holder><Child><B>2</B></Child><Label>l</Label><Items>9</Items></Holder>";
                        var node = new Node { Name = "root" };
                        node.Next = node;
                        var drawing = new Drawing { Main = new Circle { Id = 1, Radius = 2 }, All = { new Shape { Id = 3 }, new Circle { Id = 4, Radius = 5 } } };
                        string ShowHolder(Holder h) => h.Child.A + "," + h.Child.B + "|" + h.Label + "|" + string.Join(",", h.Items);
                        return string.Join("\n",
                            Same((o, c) => Write<Shape>(new Circle { Id = 1, Radius = 2 }, o, c?.Shape)),
                            Same((o, c) => Read(shape, o, c?.Shape).GetType().Name),
                            Same((o, c) => Write(drawing, o, c?.Drawing)),
                            Same((o, c) => Write(new Circle { Id = 1, Radius = 2 }, o, c?.Circle)),
                            Same((o, c) => Write(new Ring { Items = { new Circle { Id = 1, Radius = 2 } } }, o, c?.Ring)),
                            Same((o, c) => { var d = Read(Write(drawing, o, c?.Drawing), o, c?.Drawing); return d.Main!.GetType().Name + "," + d.All[0].GetType().Name + "," + d.All[1].GetType().Name; }),
                            Same((o, c) => Write(new Tagged { Flags = 255, Maybe = 16 }, o, c?.Tagged)),
                            Same((o, c) => { var t = Read(tagged, o, c?.Tagged); return t.Flags + "," + t.Maybe; }),
                            Same((o, c) => Write(new Tagged { Flags = 255 }, o, c?.Tagged), o => o.PropertyNamingPolicy = XmlNamingPolicy.CamelCase),
                            Same((o, c) => Write(Read(note, o, c?.Note), o, c?.Note) + " " + Read(note, o, c?.Note).Body.Count),
                            Same((o, c) => Write(Read(bag, o, c?.Bag), o, c?.Bag)),
                            Same((o, c) => Write(node, o, c?.Node), o => o.ReferenceHandler = XmlReferenceHandler.Preserve),
                            Same((o, c) => { var n = Read("<Node xmlns:r=\"urn:CloudNimble.Text.Xml:refs\" r:id=\"1\"><Next r:ref=\"1\"/><Name>root</Name></Node>", o, c?.Node); return n.Name + "," + ReferenceEquals(n, n.Next); }, o => o.ReferenceHandler = XmlReferenceHandler.Preserve),
                            Same((o, c) => ShowHolder(Read(holder, o, c?.Holder))),
                            Same((o, c) => ShowHolder(Read(holder, o, c?.Holder)), o => o.PreferredObjectCreationHandling = XmlObjectCreationHandling.Populate),
                            Same((o, c) => Write(new Hidden(), o, c?.Hidden)),
                            Same((o, c) => { var h = Read("<Hidden><Secret>8</Secret><Hush>9</Hush><Seen>2</Seen></Hidden>", o, c?.Hidden); return h.Secret + "," + h.Peek() + "," + h.Seen; }),
                            Same((o, c) => { var n = Read("<Needs><Name>n</Name><Count>3</Count></Needs>", o, c?.Needs); return n.Name + "," + n.Count; }),
                            Same((o, c) => Read("<Needs><Name>n</Name></Needs>", o, c?.Needs).Name),
                            Same((o, c) => Read("<Odd><Name>n</Name></Odd>", o, c?.Odd).Name));
                    }
                }
                """).Should().Be(string.Join("\n",
                "<Shape kind=\"circle\"><Id>1</Id><Radius>2</Radius></Shape>",
                "Circle",
                "<Drawing><Main kind=\"circle\"><Id>1</Id><Radius>2</Radius></Main><All><Id>3</Id></All><All kind=\"circle\"><Id>4</Id><Radius>5</Radius></All></Drawing>",
                "<Circle kind=\"circle\"><Id>1</Id><Radius>2</Radius></Circle>",
                "<Ring><Items kind=\"circle\"><Id>1</Id><Radius>2</Radius></Items></Ring>",
                "Circle,Shape,Circle",
                "<Tagged><Flags>ff</Flags><Maybe>10</Maybe></Tagged>",
                "255,16",
                "<tagged><flags>ff</flags><maybe xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></tagged>",
                "<Note>a<b>1</b>c</Note> 3",
                "<Bag other=\"1\"><Name>n</Name><More>m</More></Bag>",
                "<Node xmlns:ns1=\"urn:CloudNimble.Text.Xml:refs\" ns1:id=\"1\"><Next ns1:ref=\"1\"/><Name>root</Name></Node>",
                "root,True",
                "7,0|l|1",
                "7,2|l|1,9",
                "<Hidden><Secret>5</Secret><Hush>6</Hush><Seen>1</Seen></Hidden>",
                "8,9,2",
                "n,3",
                "XmlException: The required member 'Count' is missing. Path: /Needs | LineNumber: 1 | BytePositionInLine: 30.",
                "n!"));
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
                            "<Array><int>9</int></Array><GetOnly><int>9</int></GetOnly><GetOnlyMap><a>5</a><b>2</b></GetOnlyMap><Loose>8</Loose><Loose>9</Loose>" +
                            "<Map><b>2</b></Map><Marked><int>9</int></Marked><NullGetOnly><int>9</int></NullGetOnly><NullSettable><int>9</int></NullSettable>" +
                            "<Queue><int>9</int></Queue><Replaced><int>9</int></Replaced><Sequence><int>9</int></Sequence><Set><int>9</int></Set>" +
                            "<Settable><int>9</int></Settable><Stack><int>8</int><int>9</int></Stack>";
                        const string marked = "<MarkedType><Array><int>9</int></Array><Items><int>9</int></Items><Name>x</Name></MarkedType>";
                        const string constructed = "<WithConstructor><Name>n</Name><Items><int>9</int></Items></WithConstructor>";
                        const string held = "<Holder><Child><B>2</B></Child></Holder>";
                        const string replacing = "<ReplacingHolder><Child><B>2</B></Child></ReplacingHolder>";
                        return string.Join("\n",
                            ReadTarget(false, all),
                            ReadTarget(true, all),
                            ReadTarget(true, "<Fixed><int>9</int></Fixed>"),
                            ReadTarget(true, "<FixedMap><b>2</b></FixedMap>"),
                            ReadTarget(false, "<Fixed><int>9</int></Fixed>"),
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
                // The object the member holds is populated, as the reflection resolver populates it.
                "7,2",
                "1,2"));
        }

        [TestMethod]
        public void DerivedTypeNames_OfAMemberAndOfAWrappedCollection_DoNotCompete()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                [XmlDerivedType<Circle>]
                [XmlDerivedType<Square>]
                public abstract class Shape
                {
                    public string? Label { get; set; }
                }

                public sealed class Circle : Shape
                {
                    public double Radius { get; set; }
                }

                public sealed class Square : Shape
                {
                    public double Side { get; set; }
                }

                public sealed class Album
                {
                    public Shape? Cover { get; set; }

                    [XmlArray]
                    public List<Shape> Pages { get; set; } = new();
                }

                [XmlSerializable<Album>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        static string Show(Album album) => album.Cover!.GetType().Name + ":" + string.Join(",", album.Pages.Select(page => page.GetType().Name));
                        var album = new Album { Cover = new Circle { Label = "c", Radius = 2 } };
                        album.Pages.Add(new Square { Label = "s", Side = 4 });
                        album.Pages.Add(new Circle { Label = "p", Radius = 3 });
                        return Both.Write(album, o => new AppXmlContext(o).Album, Show);
                    }
                }
                """ + Compare).Should().Be(
                "<Album><Circle><Label>c</Label><Radius>2</Radius></Circle><Pages><Square><Label>s</Label><Side>4</Side></Square><Circle><Label>p</Label><Radius>3</Radius></Circle></Pages></Album>\nsame\nCircle:Square,Circle\nCircle:Square,Circle");
        }

        [TestMethod]
        public void ElementNamesInTwoNamespaces_AreTwoNames_AndEachElementGoesToTheMemberOfItsNamespace()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed class Pair
                {
                    [XmlAttribute("m", "urn:a")]
                    public string? Mark { get; set; }

                    [XmlElement("x")]
                    public string? Plain { get; set; }

                    [XmlElement("x", "urn:a")]
                    public string? Qualified { get; set; }
                }

                [XmlSerializable<Pair>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        static string Show(Pair pair) => pair.Mark + "," + pair.Plain + "," + pair.Qualified;
                        // The element of the namespace comes first, where the member with no namespace is tried first.
                        var typeInfo = new AppXmlContext(new XmlSerializerOptions()).Pair;
                        var read = XmlSerializer.Deserialize("<Pair xmlns:a=\"urn:a\" a:m=\"t\"><a:x>q</a:x><x>p</x></Pair>", typeInfo)!;
                        return Both.Write(new Pair { Mark = "t", Plain = "p", Qualified = "q" }, o => new AppXmlContext(o).Pair, Show) + "\n" + Show(read);
                    }
                }
                """ + Compare).Should().Be("<Pair xmlns:ns1=\"urn:a\" ns1:m=\"t\"><x>p</x><ns1:x>q</ns1:x></Pair>\nsame\nt,p,q\nt,p,q\nt,p,q");
        }

        [TestMethod]
        public void NamespaceDeclarations_OfTheOptionsAndOfTheType_AreWrittenOnTheDocumentElement()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                [XmlRoot("feed")]
                [XmlNamespaceDeclaration("a", "urn:a")]
                public sealed class Feed
                {
                    [XmlAttribute("id", "urn:b")]
                    public int Id { get; set; }

                    [XmlElement("x", "urn:a")]
                    public string? X { get; set; }
                }

                [XmlSerializable<Feed>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        return Both.Write(
                            new Feed { Id = 7, X = "1" },
                            o => new AppXmlContext(o).Feed,
                            feed => feed.Id + "," + feed.X,
                            o => o.Namespaces = new Dictionary<string, string> { ["b"] = "urn:b" });
                    }
                }
                """ + Compare).Should().Be("<feed xmlns:b=\"urn:b\" xmlns:a=\"urn:a\" b:id=\"7\"><a:x>1</a:x></feed>\nsame\n7,1\n7,1");
        }

        [TestMethod]
        public void ItemsOfAWrappedCollection_AreNamedForTheirType_AsTheReflectionResolverNamesThem()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                [XmlRoot("line-item")]
                public sealed class Line
                {
                    public int N { get; set; }
                }

                public sealed class Holder
                {
                    [XmlArray]
                    public List<Line> Lines { get; set; } = new();

                    [XmlArray]
                    public List<int> Numbers { get; set; } = new();

                    [XmlArray]
                    public List<uint?> Counts { get; set; } = new();

                    [XmlArray]
                    public List<string[]> Words { get; set; } = new();

                    [XmlArray]
                    public List<byte[]> Blobs { get; set; } = new();

                    [XmlArray]
                    public List<Guid> Ids { get; set; } = new();

                    [XmlArray]
                    public List<Outer<int>.Part> Parts { get; set; } = new();
                }

                public sealed class Outer<T>
                {
                    public sealed class Part
                    {
                        public T? Value { get; set; }
                    }
                }

                [XmlSerializable<Holder>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var holder = new Holder();
                        holder.Lines.Add(new Line { N = 1 });
                        holder.Numbers.Add(3);
                        holder.Counts.Add(4);
                        holder.Words.Add(new[] { "a" });
                        holder.Blobs.Add(new byte[] { 1 });
                        holder.Ids.Add(Guid.Empty);
                        holder.Parts.Add(new Outer<int>.Part { Value = 5 });
                        return Both.Write(holder, o => new AppXmlContext(o).Holder, h => h.Lines.Count + "," + h.Numbers[0] + "," + h.Counts[0] + "," + h.Words[0][0] + "," + h.Blobs[0][0] + "," + h.Ids[0] + "," + h.Parts[0].Value);
                    }
                }
                """ + Compare).Should().Be(
                "<Holder><Lines><line-item><N>1</N></line-item></Lines><Numbers><int>3</int></Numbers><Counts><unsignedInt>4</unsignedInt></Counts>"
                + "<Words><ArrayOfString><string>a</string></ArrayOfString></Words><Blobs><base64Binary>AQ==</base64Binary></Blobs>"
                + "<Ids><guid>00000000-0000-0000-0000-000000000000</guid></Ids><Parts><PartOfInt32><Value>5</Value></PartOfInt32></Parts></Holder>"
                + "\nsame\n1,3,4,a,1,00000000-0000-0000-0000-000000000000,5\n1,3,4,a,1,00000000-0000-0000-0000-000000000000,5");
        }

        [TestMethod]
        public void MixedMember_ThatIsNotACollectionOfMixedContentItems_IsRefused_AsTheReflectionResolverRefusesIt()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed class Page
                {
                    [XmlMixed]
                    public List<object> Content { get; set; } = new();
                }

                [XmlSerializable<Page>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    private static string Try(Func<string> work)
                    {
                        try
                        {
                            return work();
                        }
                        catch (Exception ex)
                        {
                            return ex.GetType().Name + ": " + ex.Message;
                        }
                    }

                    public static string Run()
                    {
                        var generated = Try(() => XmlSerializer.Serialize(new Page(), AppXmlContext.Default.Page));
                        var reflected = Try(() => XmlSerializer.Serialize(new Page(), new XmlSerializerOptions()));
                        return generated + "\n" + (generated == reflected ? "same" : "reflection: " + reflected);
                    }
                }
                """).Should().Be("NotSupportedException: Member 'Page.Content' is marked [XmlMixedAttribute] but is not a collection of XmlMixedContentItem.\nsame");
        }

        [TestMethod]
        public void ElementChoices_AndMembersInNoNamespace_AreWrittenAndReadAsTheReflectionResolverDoes()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public class Shape
                {
                    public string? Name { get; set; }
                }

                public sealed class Circle : Shape
                {
                    public int R { get; set; }
                }

                public sealed class Square : Shape
                {
                    public int S { get; set; }
                }

                [XmlRoot("page", Namespace = "urn:p")]
                public sealed class Page
                {
                    [XmlElement<Circle>("circle")]
                    [XmlElement<Square>("square")]
                    public Shape? One { get; set; }

                    [XmlElement<string>("t")]
                    [XmlElement<int>("n")]
                    [XmlElement<Square>("sq")]
                    public List<object> Many { get; set; } = new();

                    [XmlArray("wrapped")]
                    [XmlArrayItem<Circle>("c")]
                    [XmlArrayItem<Square>("s")]
                    public List<Shape> Wrapped { get; set; } = new();

                    [XmlElement("local", Namespace = "")]
                    public string? Local { get; set; }

                    [XmlArray("wrap", Namespace = "")]
                    public List<int> Numbers { get; set; } = new();

                    [XmlElement<string>("key")]
                    [XmlElement<string>("value")]
                    public List<XmlNamedValue> Pairs { get; set; } = new();
                }

                [XmlSerializable<Page>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var page = new Page { One = new Square { S = 1 }, Local = "l" };
                        page.Many.Add("a");
                        page.Many.Add(2);
                        page.Many.Add(new Square { S = 3 });
                        page.Wrapped.Add(new Circle { R = 4 });
                        page.Numbers.Add(5);
                        page.Pairs.Add(new XmlNamedValue("value", "v"));
                        page.Pairs.Add(new XmlNamedValue("key", "k"));
                        return Both.Write(page, o => new AppXmlContext(o).Page, p => p.One!.GetType().Name + "," + p.Many.Count + "," + p.Many[2].GetType().Name + "," + p.Wrapped[0].GetType().Name + "," + p.Local + "," + p.Numbers[0] + "," + p.Pairs[0].Name + "=" + p.Pairs[0].Value + "," + p.Pairs[1].Name + "=" + p.Pairs[1].Value);
                    }
                }
                """ + Compare).Should().Be(
                "<page xmlns=\"urn:p\"><square><Name xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><S>1</S></square>"
                + "<t>a</t><n>2</n><sq><Name xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><S>3</S></sq>"
                + "<wrapped><c><Name xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><R>4</R></c></wrapped>"
                + "<local xmlns=\"\">l</local><wrap xmlns=\"\"><int>5</int></wrap><value>v</value><key>k</key></page>"
                + "\nsame\nSquare,3,Square,Circle,l,5,value=v,key=k\nSquare,3,Square,Circle,l,5,value=v,key=k");
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
