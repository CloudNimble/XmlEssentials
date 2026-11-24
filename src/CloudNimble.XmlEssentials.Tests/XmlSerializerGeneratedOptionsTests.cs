using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A generated context follows the options it runs with, and gives the result the reflection resolver gives
    /// for them. Each test compiles a program with the generator and runs it.
    /// </summary>
    [TestClass]
    public class XmlSerializerGeneratedOptionsTests
    {

        #region Fields

        private const string BclNil = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>";

        private const string All =
            "<Parcel><BaseField>1</BaseField><BaseProp>2</BaseProp><Field>3</Field><Count" + BclNil + "<n>4</n><Letter>97</Letter>"
            + "<Named><v>5</v></Named><Plain><int>6</int></Plain><Sent>2026-03-14T09:30:00Z</Sent><Marked" + BclNil + "<t>7</t></Parcel>";

        private const string Note = "<Note" + BclNil;

        private const string Offset = "<Sent>2026-03-14T09:30:00+00:00</Sent><Marked" + BclNil + "<t>7</t></Parcel>";

        private const string Plain = "<Parcel><BaseProp>2</BaseProp><Count" + BclNil + "<n>4</n><Letter>a</Letter>";

        private const string Shown = "3,4,a,5,6,00:00:00,True";

        private const string Usings = """
            using System;
            using System.Collections.Generic;
            using CloudNimble.Text.Xml;
            using CloudNimble.Text.Xml.Serialization;

            """;

        /// <summary>
        /// Writes a value through the context and through the reflection resolver with options built the same
        /// way, and reads the context's document back through both. A failure is reported as its message.
        /// </summary>
        private const string Compare = """

            public static class Both
            {
                public static string Write<T>(T value, Func<XmlSerializerOptions, XmlTypeInfo<T>> info, Func<T, string> show, Action<XmlSerializerOptions>? configure = null)
                {
                    try
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
                    catch (Exception ex)
                    {
                        return ex.GetType().Name + ": " + ex.Message;
                    }
                }
            }
            """;

        private const string Item = """
            public sealed class Item
            {
                [XmlAttribute]
                public string? Code { get; set; }

                public string? Name { get; set; }

                public int Count { get; set; }

                public int? Limit { get; set; }

                public double Ratio { get; set; }

                [XmlIgnore(Condition = XmlIgnoreCondition.WhenWritingNull)]
                public string? Note { get; set; }

                [XmlIgnore(Condition = XmlIgnoreCondition.WhenWritingDefault)]
                public int Rank { get; set; }

                [XmlIgnore(Condition = XmlIgnoreCondition.Never)]
                public string? Always { get; set; }
            }

            [XmlSerializable<Item>]
            internal partial class AppXmlContext : XmlSerializerContext
            {
            }

            """;

        private const string Nil = "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"";

        #endregion

        #region Public Methods

        [TestMethod]
        public void SettingsOfTheBclDefaults_AreFollowedByAGeneratedContext_AsTheReflectionResolverFollowsThem()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public class ParcelBase
                {
                    public int BaseField = 1;

                    public int BaseProp { get; set; } = 2;
                }

                public sealed class Parcel : ParcelBase
                {
                    public int Field = 3;

                    public int? Count { get; set; }

                    [XmlElement("n")]
                    public List<int> Flat { get; set; } = new() { 4 };

                    public char Letter { get; set; } = 'a';

                    [XmlArrayItem("v")]
                    public List<int> Named { get; set; } = new() { 5 };

                    public string? Note { get; set; }

                    public List<int> Plain { get; set; } = new() { 6 };

                    public DateTimeOffset Sent { get; set; } = new DateTimeOffset(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

                    [XmlIgnore(Condition = XmlIgnoreCondition.WhenWritingNullReference)]
                    public int? Marked { get; set; }

                    [XmlIgnore(Condition = XmlIgnoreCondition.WhenWritingNullReference)]
                    public string? MarkedText { get; set; }

                    [XmlElement<int>("t")]
                    public List<int> Typed { get; set; } = new() { 7 };
                }

                [XmlSerializable<Parcel>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                // This seed leaves the write fast path in use, so the fast path is what writes the wrappers.
                [XmlSourceGenerationOptions(DefaultCollectionMapping = XmlCollectionMapping.Wrapped, OmitXmlDeclaration = true)]
                [XmlSerializable<Parcel>]
                internal partial class WrappedXmlContext : XmlSerializerContext
                {
                }

                [XmlSourceGenerationOptions(
                    CharHandling = XmlCharHandling.Number,
                    DefaultCollectionMapping = XmlCollectionMapping.Wrapped,
                    DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNullReference,
                    IncludeFields = true,
                    MemberOrder = XmlMemberOrder.FieldsFirst,
                    OmitXmlDeclaration = true,
                    UtcOffsetHandling = XmlUtcOffsetHandling.WriteZ)]
                [XmlSerializable<Parcel>]
                internal partial class SeededXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    private static string Show(Parcel parcel) => parcel.Field + "," + parcel.Flat[0] + "," + parcel.Letter + "," + parcel.Named[0] + "," + parcel.Plain[0] + "," + parcel.Sent.Offset + "," + (parcel.Count is null);

                    private static void All(XmlSerializerOptions options)
                    {
                        options.CharHandling = XmlCharHandling.Number;
                        options.DefaultCollectionMapping = XmlCollectionMapping.Wrapped;
                        options.DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNullReference;
                        options.IncludeFields = true;
                        options.MemberOrder = XmlMemberOrder.FieldsFirst;
                        options.UtcOffsetHandling = XmlUtcOffsetHandling.WriteZ;
                    }

                    public static string Run()
                    {
                        var seeded = SeededXmlContext.Default.Options;
                        var same = new XmlSerializerOptions { OmitXmlDeclaration = true };
                        All(same);
                        var fromSeed = XmlSerializer.Serialize(new Parcel(), SeededXmlContext.Default.Parcel);
                        var fromReflection = XmlSerializer.Serialize(new Parcel(), same);
                        var fast = XmlSerializer.Serialize(new Parcel(), WrappedXmlContext.Default.Parcel);
                        var slow = XmlSerializer.Serialize(new Parcel(), new XmlSerializerOptions { DefaultCollectionMapping = XmlCollectionMapping.Wrapped, OmitXmlDeclaration = true });

                        return string.Join("\n",
                            Both.Write(new Parcel(), o => new AppXmlContext(o).Parcel, Show),
                            Both.Write(new Parcel(), o => new AppXmlContext(o).Parcel, Show, o => o.DefaultCollectionMapping = XmlCollectionMapping.Wrapped),
                            Both.Write(new Parcel(), o => new AppXmlContext(o).Parcel, Show, All),
                            seeded.CharHandling + "," + seeded.DefaultCollectionMapping + "," + seeded.DefaultIgnoreCondition + "," + seeded.MemberOrder + "," + seeded.UtcOffsetHandling,
                            fromSeed == fromReflection ? fromSeed : fromSeed + " <> reflection: " + fromReflection,
                            fast == slow ? fast : fast + " <> reflection: " + slow);
                    }
                }
                """ + Compare).Should().Be(
                Plain + "<Named>5</Named>" + Note + "<Plain>6</Plain>" + Offset + "\nsame\n" + Shown + "\n" + Shown + "\n"
                + Plain + "<Named><v>5</v></Named>" + Note + "<Plain><int>6</int></Plain>" + Offset + "\nsame\n" + Shown + "\n" + Shown + "\n"
                + All + "\nsame\n" + Shown + "\n" + Shown + "\n"
                + "Number,Wrapped,WhenWritingNullReference,FieldsFirst,WriteZ\n"
                + All + "\n"
                + Plain + "<Named><v>5</v></Named>" + Note + "<Plain><int>6</int></Plain>" + Offset);
        }

        [TestMethod]
        public void Options_TheFastPathWasNotCompiledFor_AreAppliedFromTheMetadata()
        {
            const string Plain = "<Item><Name " + Nil + "/><Count>0</Count><Limit " + Nil + "/><Ratio>0</Ratio><Always " + Nil + "/></Item>";
            // The write fast path has the names and the choice of members compiled in. With other options it is not
            // used, and the document is written from the metadata, as the reflection resolver writes it.
            XmlSerializerGeneratorTests.RunGenerated(Usings + Item + """
                public static class Use
                {
                    public static string Run()
                    {
                        static string Show(Item item) => item.Name ?? "null";
                        return string.Join("\n",
                            Both.Write(new Item(), o => new AppXmlContext(o).Item, Show, o => o.PropertyNamingPolicy = XmlNamingPolicy.CamelCase),
                            Both.Write(new Item { Code = "c" }, o => new AppXmlContext(o).Item, Show, o => o.AttributeNamingPolicy = XmlNamingPolicy.KebabCaseUpper),
                            Both.Write(new Item(), o => new AppXmlContext(o).Item, Show, o => o.DefaultPropertyMapping = XmlPropertyMapping.Attribute),
                            Both.Write(new Item(), o => new AppXmlContext(o).Item, Show, o => o.IncludeFields = true),
                            Both.Write(new Item(), o => new AppXmlContext(o).Item, Show, o => o.IgnoreReadOnlyProperties = true),
                            Both.Write(new Item(), o => new AppXmlContext(o).Item, Show, o => o.IgnoreReadOnlyFields = true),
                            Both.Write(new Item { Name = "n" }, o => new AppXmlContext(o).Item, Show, o => o.Converters.Add(new Upper())),
                            Both.Write(new Item(), o => new AppXmlContext(o).Item, Show, o => o.Converters.Add(new Whole())));
                    }
                }

                public sealed class Upper : XmlConverter<string>
                {
                    public override string? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
                    {
                        string? value = null;
                        while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
                        {
                            value = reader.GetString();
                        }

                        return value;
                    }

                    public override void Write(Utf8XmlWriter writer, string value, XmlSerializerOptions options) => writer.WriteStringValue(value.ToUpperInvariant());
                }

                public sealed class Whole : XmlConverter<Item>
                {
                    public override Item? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
                    {
                        while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
                        {
                        }

                        return new Item { Name = "whole" };
                    }

                    public override void Write(Utf8XmlWriter writer, Item value, XmlSerializerOptions options) => writer.WriteStringValue("item");
                }
                """ + Compare).Should().Be(string.Join("\n",
                "<item><name " + Nil + "/><count>0</count><limit " + Nil + "/><ratio>0</ratio><always " + Nil + "/></item>", "same", "null", "null",
                "<Item CODE=\"c\"><Name " + Nil + "/><Count>0</Count><Limit " + Nil + "/><Ratio>0</Ratio><Always " + Nil + "/></Item>", "same", "null", "null",
                "<Item Count=\"0\" Ratio=\"0\"/>", "same", "null", "null",
                Plain, "same", "null", "null",
                Plain, "same", "null", "null",
                Plain, "same", "null", "null",
                "<Item><Name>N</Name><Count>0</Count><Limit " + Nil + "/><Ratio>0</Ratio><Always " + Nil + "/></Item>", "same", "N", "N",
                "<Item>item</Item>", "same", "whole", "whole"));
        }

        [TestMethod]
        public void CompiledSettings_ThatTheSeedSets_AreAccepted()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                public sealed class Item
                {
                    public string? FirstName { get; set; }

                    public int ItemCount;

                    public string? Note { get; set; }
                }

                [XmlSourceGenerationOptions(
                    OmitXmlDeclaration = true,
                    PropertyNamingPolicy = XmlKnownNamingPolicy.CamelCase,
                    AttributeNamingPolicy = XmlKnownNamingPolicy.KebabCaseLower,
                    DefaultPropertyMapping = XmlPropertyMapping.Attribute,
                    IncludeFields = true,
                    DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull)]
                [XmlSerializable<Item>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var seeded = XmlSerializer.Serialize(new Item { FirstName = "Ada", ItemCount = 2 }, AppXmlContext.Default.Item);
                        var same = new XmlSerializerOptions
                        {
                            OmitXmlDeclaration = true,
                            PropertyNamingPolicy = XmlNamingPolicy.CamelCase,
                            AttributeNamingPolicy = XmlNamingPolicy.KebabCaseLower,
                            DefaultPropertyMapping = XmlPropertyMapping.Attribute,
                            IncludeFields = true,
                            DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull,
                        };
                        var given = XmlSerializer.Serialize(new Item { FirstName = "Ada", ItemCount = 2 }, new AppXmlContext(same).Item);
                        var reflected = XmlSerializer.Serialize(new Item { FirstName = "Ada", ItemCount = 2 }, typeof(Item), new XmlSerializerOptions
                        {
                            OmitXmlDeclaration = true,
                            PropertyNamingPolicy = XmlNamingPolicy.CamelCase,
                            AttributeNamingPolicy = XmlNamingPolicy.KebabCaseLower,
                            DefaultPropertyMapping = XmlPropertyMapping.Attribute,
                            IncludeFields = true,
                            DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull,
                        });
                        return seeded + "\n" + given + "\n" + reflected + "\n" + AppXmlContext.Default.Options.DefaultIgnoreCondition + "," + AppXmlContext.Default.Options.DefaultPropertyMapping;
                    }
                }
                """).Should().Be(
                "<item first-name=\"Ada\" item-count=\"2\"/>\n<item first-name=\"Ada\" item-count=\"2\"/>\n<item first-name=\"Ada\" item-count=\"2\"/>\nWhenWritingNull,Attribute");
        }

        [TestMethod]
        public void IgnoreConditions_OnMembersAndInOptions_AreFollowed()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + Item + """
                public static class Use
                {
                    public static string Run()
                    {
                        static string Show(Item item) => item.Count + "," + (item.Limit?.ToString() ?? "null") + "," + item.Rank;
                        return string.Join("\n",
                            Both.Write(new Item { Ratio = 1 }, o => new AppXmlContext(o).Item, Show),
                            Both.Write(new Item { Ratio = 1 }, o => new AppXmlContext(o).Item, Show, o => o.DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull),
                            Both.Write(new Item { Ratio = 1 }, o => new AppXmlContext(o).Item, Show, o => o.DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingDefault),
                            Both.Write(new Item { Code = "c", Name = "n", Count = 1, Limit = 0, Ratio = 2, Note = "t", Rank = 3, Always = "a" }, o => new AppXmlContext(o).Item, Show, o => o.DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingDefault));
                    }
                }
                """ + Compare).Should().Be(
                "<Item><Name " + Nil + "/><Count>0</Count><Limit " + Nil + "/><Ratio>1</Ratio><Always " + Nil + "/></Item>\nsame\n0,null,0\n0,null,0\n" +
                "<Item><Count>0</Count><Ratio>1</Ratio><Always " + Nil + "/></Item>\nsame\n0,null,0\n0,null,0\n" +
                "<Item><Ratio>1</Ratio><Always " + Nil + "/></Item>\nsame\n0,null,0\n0,null,0\n" +
                "<Item Code=\"c\"><Name>n</Name><Count>1</Count><Limit>0</Limit><Ratio>2</Ratio><Note>t</Note><Rank>3</Rank><Always>a</Always></Item>\nsame\n1,0,3\n1,0,3");
        }

        [TestMethod]
        public void IgnoreConditions_ForOneDirection_AreFollowedAsTheReflectionResolverFollowsThem()
        {
            XmlSerializerGeneratorTests.RunGenerated("#nullable enable\n" + Usings + """
                public sealed class Directions
                {
                    [XmlIgnore(Condition = XmlIgnoreCondition.WhenReading)]
                    public int Count { get; set; } = 1;

                    [XmlElement("Item")]
                    [XmlIgnore(Condition = XmlIgnoreCondition.WhenReading)]
                    public List<int> Items { get; set; } = new() { 1 };

                    public string Normal { get; set; } = "n";

                    [XmlIgnore(Condition = XmlIgnoreCondition.WhenReading)]
                    public string ReadIgnored { get; set; } = "r";

                    [XmlAttribute]
                    [XmlIgnore(Condition = XmlIgnoreCondition.WhenReading)]
                    public string ReadIgnoredAttribute { get; set; } = "ra";

                    [XmlArray]
                    [XmlIgnore(Condition = XmlIgnoreCondition.WhenReading)]
                    public List<int> Tags { get; set; } = new() { 1 };

                    [XmlIgnore(Condition = XmlIgnoreCondition.WhenWriting)]
                    public string WriteIgnored { get; set; } = "w";

                    [XmlAttribute]
                    [XmlIgnore(Condition = XmlIgnoreCondition.WhenWriting)]
                    public string WriteIgnoredAttribute { get; set; } = "wa";

                    [XmlArray]
                    public List<int> GetOnly { get; } = new() { 1 };
                }

                public sealed class Plain
                {
                    [XmlAttribute]
                    public string Code { get; set; } = "c";

                    public string? Maybe { get; set; }

                    public string Name { get; set; } = "n";

                    [XmlArray]
                    public List<int> Tags { get; set; } = new() { 1 };
                }

                public sealed class Body
                {
                    [XmlText]
                    [XmlIgnore(Condition = XmlIgnoreCondition.WhenReading)]
                    public int Number { get; set; } = 1;
                }

                public sealed record Pair([property: XmlIgnore(Condition = XmlIgnoreCondition.WhenReading)] string Name, string Other);

                public sealed class Needs
                {
                    public required string Name { get; set; }
                }

                public sealed class Starts
                {
                    public string Name { get; init; } = "n";
                }

                [XmlSerializable<Directions>]
                [XmlSerializable<Plain>]
                [XmlSerializable<Body>]
                [XmlSerializable<Pair>]
                [XmlSerializable<Needs>]
                [XmlSerializable<Starts>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    private const string Nil = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>";

                    // Runs the same work through a generated context and through the reflection resolver, each with
                    // its own options, and reports one result when they agree.
                    private static string Same(Func<XmlSerializerOptions, string> generated, Func<XmlSerializerOptions, string> reflected, Action<XmlSerializerOptions>? configure = null)
                    {
                        string Run(Func<XmlSerializerOptions, string> work)
                        {
                            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
                            configure?.Invoke(options);
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

                    private static string Show(Directions d) => string.Join(",", d.Count, string.Join(";", d.Items), d.Normal, d.ReadIgnored, d.ReadIgnoredAttribute, string.Join(";", d.Tags), d.WriteIgnored, d.WriteIgnoredAttribute, string.Join(";", d.GetOnly));

                    private static string Show(Plain p) => string.Join(",", p.Code, p.Maybe ?? "null", p.Name, string.Join(";", p.Tags));

                    public static string Run()
                    {
                        const string directions =
                            "<Directions ReadIgnoredAttribute=\"x\" WriteIgnoredAttribute=\"x\"><Count>not a number</Count><Item>not a number</Item><Normal>x</Normal>" +
                            "<ReadIgnored>x</ReadIgnored><Tags><int>not a number</int></Tags><WriteIgnored>x</WriteIgnored><GetOnly><int>not a number</int></GetOnly></Directions>";
                        const string plain = "<Plain Code=\"x\"><Maybe>m</Maybe><Name>x</Name><Tags><int>9</int></Tags></Plain>";
                        Action<XmlSerializerOptions> whenReading = o => o.DefaultIgnoreCondition = XmlIgnoreCondition.WhenReading;
                        Action<XmlSerializerOptions> whenWriting = o => o.DefaultIgnoreCondition = XmlIgnoreCondition.WhenWriting;
                        return string.Join("\n",
                            Same(o => XmlSerializer.Serialize(new Directions(), new AppXmlContext(o).Directions), o => XmlSerializer.Serialize(new Directions(), typeof(Directions), o)),
                            Same(o => Show(XmlSerializer.Deserialize(directions, new AppXmlContext(o).Directions)!), o => Show((Directions)XmlSerializer.Deserialize(directions, typeof(Directions), o)!)),
                            Same(o => Show(XmlSerializer.Deserialize(directions, new AppXmlContext(o).Directions)!), o => Show((Directions)XmlSerializer.Deserialize(directions, typeof(Directions), o)!),
                                o => { o.UnmappedMemberHandling = XmlUnmappedMemberHandling.Disallow; o.RespectNullableAnnotations = true; }),
                            Same(o => XmlSerializer.Serialize(new Directions { WriteIgnored = null!, WriteIgnoredAttribute = null! }, new AppXmlContext(o).Directions),
                                o => XmlSerializer.Serialize(new Directions { WriteIgnored = null!, WriteIgnoredAttribute = null! }, typeof(Directions), o),
                                o => o.RespectNullableAnnotations = true),
                            Same(o => Show(XmlSerializer.Deserialize("<Directions><ReadIgnored" + Nil + "<Tags" + Nil + "</Directions>", new AppXmlContext(o).Directions)!),
                                o => Show((Directions)XmlSerializer.Deserialize("<Directions><ReadIgnored" + Nil + "<Tags" + Nil + "</Directions>", typeof(Directions), o)!),
                                o => o.RespectNullableAnnotations = true),
                            Same(o => XmlSerializer.Serialize(new Plain(), new AppXmlContext(o).Plain), o => XmlSerializer.Serialize(new Plain(), typeof(Plain), o), whenWriting),
                            Same(o => Show(XmlSerializer.Deserialize(plain, new AppXmlContext(o).Plain)!), o => Show((Plain)XmlSerializer.Deserialize(plain, typeof(Plain), o)!), whenWriting),
                            Same(o => XmlSerializer.Serialize(new Plain(), new AppXmlContext(o).Plain), o => XmlSerializer.Serialize(new Plain(), typeof(Plain), o), whenReading),
                            Same(o => Show(XmlSerializer.Deserialize(plain, new AppXmlContext(o).Plain)!), o => Show((Plain)XmlSerializer.Deserialize(plain, typeof(Plain), o)!), whenReading),
                            Same(o => XmlSerializer.Serialize(new Directions(), new AppXmlContext(o).Directions), o => XmlSerializer.Serialize(new Directions(), typeof(Directions), o), whenWriting),
                            Same(o => { var context = new AppXmlContext(o); return XmlSerializer.Serialize(new Body(), context.Body) + XmlSerializer.Deserialize("<Body>not a number</Body>", context.Body)!.Number; },
                                o => XmlSerializer.Serialize(new Body(), typeof(Body), o) + ((Body)XmlSerializer.Deserialize("<Body>not a number</Body>", typeof(Body), o)!).Number),
                            Same(o => XmlSerializer.Deserialize("<Pair><Name>x</Name><Other>y</Other></Pair>", new AppXmlContext(o).Pair)!.ToString(),
                                o => XmlSerializer.Deserialize("<Pair><Name>x</Name><Other>y</Other></Pair>", typeof(Pair), o)!.ToString()!),
                            Same(o => XmlSerializer.Deserialize("<Pair><Name>x</Name><Other>y</Other></Pair>", new AppXmlContext(o).Pair)!.ToString(),
                                o => XmlSerializer.Deserialize("<Pair><Name>x</Name><Other>y</Other></Pair>", typeof(Pair), o)!.ToString()!, whenReading),
                            Same(o => XmlSerializer.Deserialize("<Needs><Name>x</Name></Needs>", new AppXmlContext(o).Needs)!.Name,
                                o => ((Needs)XmlSerializer.Deserialize("<Needs><Name>x</Name></Needs>", typeof(Needs), o)!).Name, whenReading),
                            Same(o => XmlSerializer.Deserialize("<Starts><Name>x</Name></Starts>", new AppXmlContext(o).Starts)!.Name,
                                o => ((Starts)XmlSerializer.Deserialize("<Starts><Name>x</Name></Starts>", typeof(Starts), o)!).Name, whenReading));
                    }
                }
                """).Should().Be(string.Join("\n",
                "<Directions ReadIgnoredAttribute=\"ra\"><Count>1</Count><Item>1</Item><Normal>n</Normal><ReadIgnored>r</ReadIgnored><Tags><int>1</int></Tags><GetOnly><int>1</int></GetOnly></Directions>",
                "1,1,x,r,ra,1,x,x,1",
                "1,1,x,r,ra,1,x,x,1",
                "<Directions ReadIgnoredAttribute=\"ra\"><Count>1</Count><Item>1</Item><Normal>n</Normal><ReadIgnored>r</ReadIgnored><Tags><int>1</int></Tags><GetOnly><int>1</int></GetOnly></Directions>",
                "1,1,n,r,ra,1,w,wa,1",
                // As the default for every member, WhenWriting and WhenReading have no effect, as in System.Text.Json.
                "<Plain Code=\"c\"><Maybe " + Nil + "/><Name>n</Name><Tags><int>1</int></Tags></Plain>",
                "x,m,x,9",
                "<Plain Code=\"c\"><Maybe " + Nil + "/><Name>n</Name><Tags><int>1</int></Tags></Plain>",
                "x,m,x,9",
                "<Directions ReadIgnoredAttribute=\"ra\"><Count>1</Count><Item>1</Item><Normal>n</Normal><ReadIgnored>r</ReadIgnored><Tags><int>1</int></Tags><GetOnly><int>1</int></GetOnly></Directions>",
                "<Body>1</Body>1",
                "Pair { Name = x, Other = y }",
                "Pair { Name = x, Other = y }",
                "x",
                "x"));
        }

        [TestMethod]
        [DataRow("public sealed class Bad { [XmlIgnore(Condition = XmlIgnoreCondition.WhenReading)] public required string Name { get; set; } }", DisplayName = "the required modifier")]
        [DataRow("public sealed class Bad { [XmlRequired] [XmlIgnore(Condition = XmlIgnoreCondition.WhenReading)] public string Name { get; set; } = \"n\"; }", DisplayName = "the attribute")]
        public void IgnoreConditions_WhenReadingOnARequiredMember_IsReported(string types)
        {
            var source = "using CloudNimble.Text.Xml;\nusing CloudNimble.Text.Xml.Serialization;\n" + types + "\n[XmlSerializable<Bad>]\ninternal partial class AppXmlContext : XmlSerializerContext\n{\n}\n";
            var (_, diagnostics, _) = GeneratorTestHost.Run(source);
            diagnostics.Where(d => d.Id is "XMLXE017").Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
                .Should().Equal("'Bad.Name' is required, and its ignore condition is XmlIgnoreCondition.WhenReading, so it would never be read");
        }

        [TestMethod]
        public void UnknownTypeHandling_IsFollowed()
        {
            XmlSerializerGeneratorTests.RunGenerated("#nullable enable\n" + Usings + """
                public sealed class Holder
                {
                    public object? Any { get; set; }

                    [XmlArray]
                    public List<object?> Items { get; set; } = new();
                }

                [XmlSerializable<Holder>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                [XmlSourceGenerationOptions(UnknownTypeHandling = XmlUnknownTypeHandling.XmlNode, PreferredObjectCreationHandling = XmlObjectCreationHandling.Populate)]
                [XmlSerializable<Holder>]
                internal partial class SeededContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    private static string Show(Holder holder) => (holder.Any?.GetType().Name ?? "null") + "," + string.Join(";", System.Linq.Enumerable.Select(holder.Items, item => item?.GetType().Name ?? "null"));

                    private static string Read(XmlUnknownTypeHandling handling)
                    {
                        const string xml = "<Holder><Any><b>1</b></Any><Items><anyType>2</anyType><anyType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></Items></Holder>";
                        var generated = Show(XmlSerializer.Deserialize(xml, new AppXmlContext(new XmlSerializerOptions { UnknownTypeHandling = handling }).Holder)!);
                        var reflected = Show((Holder)XmlSerializer.Deserialize(xml, typeof(Holder), new XmlSerializerOptions { UnknownTypeHandling = handling })!);
                        return generated == reflected ? generated : generated + " <> reflection: " + reflected;
                    }

                    public static string Run()
                    {
                        return Read(XmlUnknownTypeHandling.XmlElement) + "\n" + Read(XmlUnknownTypeHandling.XmlNode) + "\n" + Write(XmlUnknownTypeHandling.XmlElement) + "\n" + Write(XmlUnknownTypeHandling.XmlNode);
                    }

                    private static string Write(XmlUnknownTypeHandling handling)
                    {
                        const string xml = "<Holder><Any id=\"7\"><b>1</b></Any><Items><anyType>2</anyType><anyType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></Items></Holder>";
                        var info = new AppXmlContext(new XmlSerializerOptions { UnknownTypeHandling = handling, OmitXmlDeclaration = true }).Holder;
                        return XmlSerializer.Serialize(XmlSerializer.Deserialize(xml, info)!, info);
                    }
                }
                """).Should().Be(
                    "XmlElement,XmlElement;null\nXmlElementNode,XmlElementNode;null\n" +
                    "<Holder><Any id=\"7\"><b>1</b></Any><Items><anyType>2</anyType><anyType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></Items></Holder>\n" +
                    "<Holder><Any id=\"7\"><b>1</b></Any><Items><anyType>2</anyType><anyType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></Items></Holder>");
        }

        [TestMethod]
        public void LayoutSettings_AreFollowedByAGeneratedContext()
        {
            XmlSerializerGeneratorTests.RunGenerated("#nullable enable\n" + Usings + """
                public sealed class Styled
                {
                    [XmlAttribute]
                    public string Code { get; set; } = "";

                    [XmlAttribute]
                    public int Id { get; set; }

                    [XmlArray]
                    public List<string> Lines { get; set; } = new();

                    public string? Note { get; set; }

                    public string Text { get; set; } = "";
                }

                [XmlSerializable<Styled>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        XmlSerializerOptions Options() => new XmlSerializerOptions
                        {
                            AttributeStyle = XmlAttributeStyle.PerLine,
                            EmptyElementStyle = XmlEmptyElementStyle.SelfClosingWithSpace,
                            NewLine = "\r\n",
                            OmitXmlDeclaration = true,
                        };
                        var value = new Styled { Code = "c", Id = 7, Text = "one\ntwo" };
                        var generated = XmlSerializer.Serialize(value, new AppXmlContext(Options()).Styled);
                        var reflected = XmlSerializer.Serialize(value, Options());
                        var read = XmlSerializer.Deserialize(generated, new AppXmlContext(Options()).Styled)!;
                        return (generated == reflected ? generated : generated + " <> reflection: " + reflected) + "|" + read.Text.Replace("\n", "<LF>");
                    }
                }
                """).Should().Be(

                    // Nothing is indented, so Note is on the line that starts with the eight blanks before Id,
                    // and its second line has those and six more for "<Note ".
                    "<Styled Code=\"c\"\r\n        Id=\"7\"><Lines /><Note xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"\r\n              xsi:nil=\"true\" /><Text>one\r\ntwo</Text></Styled>|one<LF>two");
        }

        [TestMethod]
        public void EverySeed_ReachesTheOptionsOfTheDefaultContext()
        {
            XmlSerializerGeneratorTests.RunGenerated("#nullable enable\n" + Usings + """
                public enum Level
                {
                    Low,
                    High,
                }

                public sealed class Item
                {
                    public Item? Child { get; set; }

                    public readonly int Fixed = 3;

                    public Level Level { get; set; }

                    public List<Level> Levels { get; set; } = new();

                    [XmlAttribute]
                    public Level Mark { get; set; }

                    public Level? Maybe { get; set; }

                    public string Name { get; set; } = "";

                    public string ReadOnly => "ro";
                }

                [XmlSourceGenerationOptions(
                    EmitByteOrderMark = true,
                    EnumHandling = XmlEnumHandling.Integer,
                    IgnoreReadOnlyFields = true,
                    IgnoreReadOnlyProperties = true,
                    IncludeFields = true,
                    MaxDepth = 3,
                    PropertyNameCaseInsensitive = true,
                    XmlVersion = XmlVersion.Version11)]
                [XmlSerializable<Item>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                // This seed leaves the write fast path in use, so the fast path is what writes the enumerations.
                [XmlSourceGenerationOptions(EnumHandling = XmlEnumHandling.Integer, OmitXmlDeclaration = true)]
                [XmlSerializable<Item>]
                internal partial class EnumXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    private static XmlSerializerOptions Same() => new XmlSerializerOptions
                    {
                        EmitByteOrderMark = true,
                        EnumHandling = XmlEnumHandling.Integer,
                        IgnoreReadOnlyFields = true,
                        IgnoreReadOnlyProperties = true,
                        IncludeFields = true,
                        MaxDepth = 3,
                        PropertyNameCaseInsensitive = true,
                        XmlVersion = XmlVersion.Version11,
                    };

                    private static string Text(byte[] bytes) => System.Text.Encoding.UTF8.GetString(bytes).Replace("﻿", "<BOM>");

                    private static string Thrown(Action action)
                    {
                        try
                        {
                            action();
                            return "nothing was thrown";
                        }
                        catch (Exception ex)
                        {
                            return ex.GetType().Name + ": " + ex.Message;
                        }
                    }

                    public static string Run()
                    {
                        var options = AppXmlContext.Default.Options;
                        var value = new Item { Level = Level.High, Levels = { Level.Low, Level.High }, Mark = Level.High, Maybe = Level.High, Name = "n" };
                        var deep = new Item { Child = new Item { Child = new Item { Child = new Item() } } };

                        var generated = Text(XmlSerializer.SerializeToUtf8Bytes(value, AppXmlContext.Default.Item));
                        var reflected = Text(XmlSerializer.SerializeToUtf8Bytes(value, Same()));
                        var generatedDepth = Thrown(() => XmlSerializer.Serialize(deep, AppXmlContext.Default.Item));
                        var reflectedDepth = Thrown(() => XmlSerializer.Serialize(deep, Same()));
                        var fast = XmlSerializer.Serialize(value, EnumXmlContext.Default.Item);
                        var slow = XmlSerializer.Serialize(value, new XmlSerializerOptions { EnumHandling = XmlEnumHandling.Integer, OmitXmlDeclaration = true });

                        return string.Join("\n",
                            options.EmitByteOrderMark + "," + options.EnumHandling + "," + options.IgnoreReadOnlyFields + "," + options.IgnoreReadOnlyProperties + "," + options.MaxDepth + "," + options.PropertyNameCaseInsensitive + "," + options.XmlVersion,
                            generated == reflected ? generated : generated + " <> reflection: " + reflected,
                            XmlSerializer.Deserialize("<Item MARK=\"1\"><NAME>x</NAME><level>1</level></Item>", AppXmlContext.Default.Item)!.Name,
                            generatedDepth == reflectedDepth ? generatedDepth : generatedDepth + " <> reflection: " + reflectedDepth,
                            fast == slow ? fast : fast + " <> reflection: " + slow);
                    }
                }
                """).Should().Be(
                    "True,Integer,True,True,3,True,Version11\n" +
                    "<BOM><?xml version=\"1.1\" encoding=\"utf-8\"?><Item Mark=\"1\"><Child xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><Level>1</Level><Levels>0</Levels><Levels>1</Levels><Maybe>1</Maybe><Name>n</Name></Item>\n" +
                    "x\n" +
                    "XmlException: MaxDepth exceeded (3). Path: /Item/Child/Child.\n" +
                    "<Item Mark=\"1\"><Child xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><Level>1</Level><Levels>0</Levels><Levels>1</Levels><Maybe>1</Maybe><Name>n</Name><ReadOnly>ro</ReadOnly></Item>");
        }

        [TestMethod]
        public void ElementNamespaces_AreFollowedByAGeneratedContext()
        {
            XmlSerializerGeneratorTests.RunGenerated("#nullable enable\n" + Usings + """
                [XmlDerivedType<Circle>("shape", Namespace = "urn:round")]
                [XmlDerivedType<Square>("shape", Namespace = "urn:square")]
                public abstract class Shape
                {
                }

                public sealed class Circle : Shape
                {
                    public int Radius { get; set; }
                }

                public sealed class Square : Shape
                {
                    public int Side { get; set; }
                }

                public sealed class Drawing
                {
                    [XmlArray(Namespace = "urn:w")]
                    [XmlArrayItem("i", Namespace = "urn:i")]
                    public List<int> Numbers { get; set; } = new();

                    [XmlArray]
                    public List<Shape> Shapes { get; set; } = new();
                }

                [XmlSourceGenerationOptions(OmitXmlDeclaration = true)]
                [XmlSerializable<Drawing>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        var value = new Drawing { Numbers = { 1 }, Shapes = { new Circle { Radius = 2 }, new Square { Side = 3 } } };
                        var generated = XmlSerializer.Serialize(value, AppXmlContext.Default.Drawing);
                        var reflected = XmlSerializer.Serialize(value, new XmlSerializerOptions { OmitXmlDeclaration = true });
                        var read = XmlSerializer.Deserialize(generated, AppXmlContext.Default.Drawing)!;
                        return (generated == reflected ? generated : generated + " <> reflection: " + reflected)
                            + "|" + read.Numbers[0] + "," + read.Shapes[0].GetType().Name + "," + read.Shapes[1].GetType().Name;
                    }
                }
                """).Should().Be(
                    "<Drawing><Numbers xmlns=\"urn:w\"><i xmlns=\"urn:i\">1</i></Numbers><Shapes><shape xmlns=\"urn:round\"><Radius>2</Radius></shape><shape xmlns=\"urn:square\"><Side>3</Side></shape></Shapes></Drawing>|1,Circle,Square");
        }

#if NET11_0_OR_GREATER
        [TestMethod]
        public void ClosedTypes_AreInferredByAGeneratedContext()
        {
            // The compiler this test compiles with is older than the closed modifier, so the source carries the
            // attribute the modifier makes the compiler write.
            XmlSerializerGeneratorTests.RunGenerated("#nullable enable\n" + Usings + """
                [System.Runtime.CompilerServices.IsClosedType(DerivedTypes = new[] { typeof(Circle), typeof(Polygon), typeof(Full<>) })]
                public abstract class Shape
                {
                }

                public sealed class Circle : Shape
                {
                    public int Radius { get; set; }
                }

                [System.Runtime.CompilerServices.IsClosedType(DerivedTypes = new[] { typeof(Square) })]
                public abstract class Polygon : Shape
                {
                    public int Sides { get; set; }
                }

                public sealed class Square : Polygon
                {
                }

                // Listed without arguments, and not a derived type of Shape, which takes none.
                public sealed class Full<T> : Shape
                {
                }

                public sealed class Drawing
                {
                    [XmlArray]
                    public List<Shape> Shapes { get; set; } = new();
                }

                [XmlSerializable<Drawing>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    private static XmlSerializerOptions Options(bool infer) => new XmlSerializerOptions { InferClosedTypePolymorphism = infer, OmitXmlDeclaration = true };

                    private static string Thrown(Func<string> action)
                    {
                        try
                        {
                            return action();
                        }
                        catch (Exception ex)
                        {
                            return ex.GetType().Name + ": " + ex.Message;
                        }
                    }

                    public static string Run()
                    {
                        var value = new Drawing { Shapes = { new Circle { Radius = 1 }, new Square { Sides = 4 } } };
                        var generated = XmlSerializer.Serialize(value, new AppXmlContext(Options(true)).Drawing);
                        var reflected = XmlSerializer.Serialize(value, Options(true));
                        var read = XmlSerializer.Deserialize(generated, new AppXmlContext(Options(true)).Drawing)!;
                        var generatedOff = Thrown(() => XmlSerializer.Serialize(value, new AppXmlContext(Options(false)).Drawing));
                        var reflectedOff = Thrown(() => XmlSerializer.Serialize(value, Options(false)));
                        return string.Join("\n",
                            generated == reflected ? generated : generated + " <> reflection: " + reflected,
                            read.Shapes[0].GetType().Name + "," + read.Shapes[1].GetType().Name,
                            generatedOff == reflectedOff ? generatedOff : generatedOff + " <> reflection: " + reflectedOff);
                    }
                }
                """).Should().Be(
                    "<Drawing><Shapes><Circle><Radius>1</Radius></Circle><Square><Sides>4</Sides></Square></Shapes></Drawing>\n" +
                    "Circle,Square\n" +
                    "NotSupportedException: The type 'Shape' is not supported.");
        }
#endif

        [TestMethod]
        public void ExceptionPaths_AreTheSameFromAGeneratedContext()
        {
            XmlSerializerGeneratorTests.RunGenerated("#nullable enable\n" + Usings + """
                public sealed class Line
                {
                    public string? Label { get; set; }

                    public int Qty { get; set; }

                    [XmlAttribute]
                    public int Row { get; set; }
                }

                public sealed class Order
                {
                    public Dictionary<string, int> Counts { get; set; } = new();

                    public List<Line> Extras { get; set; } = new();

                    [XmlAttribute]
                    public int Id { get; set; }

                    [XmlArray]
                    public List<Line> Lines { get; set; } = new();
                }

                [XmlSourceGenerationOptions(OmitXmlDeclaration = true)]
                [XmlSerializable<Order>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                // In this mode there is no write fast path, so the engine writes from the metadata.
                [XmlSourceGenerationOptions(OmitXmlDeclaration = true, GenerationMode = XmlSourceGenerationMode.Metadata)]
                [XmlSerializable<Order>]
                internal partial class MetadataXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    private static string Describe(Action action)
                    {
                        try
                        {
                            action();
                        }
                        catch (XmlException ex)
                        {
                            return ex.Path + "|" + ex.LineNumber + ":" + ex.LinePosition + "|" + ex.Message;
                        }

                        return "nothing was thrown";
                    }

                    private static string Read(string xml)
                    {
                        var generated = Describe(() => XmlSerializer.Deserialize(xml, AppXmlContext.Default.Order));
                        var reflected = Describe(() => XmlSerializer.Deserialize<Order>(xml, new XmlSerializerOptions { OmitXmlDeclaration = true }));
                        return generated == reflected ? generated : generated + " <> reflection: " + reflected;
                    }

                    private static string Write(Order order)
                    {
                        var fast = Describe(() => XmlSerializer.Serialize(order, AppXmlContext.Default.Order));
                        var metadata = Describe(() => XmlSerializer.Serialize(order, MetadataXmlContext.Default.Order));
                        var reflected = Describe(() => XmlSerializer.Serialize(order, new XmlSerializerOptions { OmitXmlDeclaration = true }));
                        return fast == reflected && metadata == reflected ? fast : fast + " <> metadata: " + metadata + " <> reflection: " + reflected;
                    }

                    public static string Run()
                    {
                        return string.Join("\n",
                            Read("<Order Id=\"x\"/>"),
                            Read("<Order><Lines><Line><Qty>1</Qty></Line><Line Row=\"x\"/></Lines></Order>"),
                            Read("<Order><Extras><Qty>1</Qty></Extras><Extras><Qty>x</Qty></Extras></Order>"),
                            Read("<Order><Counts><a>1</a><b>x</b></Counts></Order>"),
                            Write(new Order { Lines = { new Line { Label = "ok" }, new Line { Label = "a\0b" } } }));
                    }
                }
                """).Should().Be(
                    "/Order/@Id|1:16|The value is not a valid integer. Path: /Order/@Id | LineNumber: 1 | BytePositionInLine: 16.\n" +
                    "/Order/Lines/Line[1]/@Row|1:55|The value is not a valid integer. Path: /Order/Lines/Line[1]/@Row | LineNumber: 1 | BytePositionInLine: 55.\n" +
                    "/Order/Extras[1]/Qty|1:57|The value is not a valid integer. Path: /Order/Extras[1]/Qty | LineNumber: 1 | BytePositionInLine: 57.\n" +
                    "/Order/Counts/b|1:32|The value is not a valid integer. Path: /Order/Counts/b | LineNumber: 1 | BytePositionInLine: 32.\n" +
                    "/Order/Lines/Line|:|Illegal character in XML value. Path: /Order/Lines/Line.");
        }

        [TestMethod]
        public void NamesAreMatchedWithoutCase_WhenTheOptionsAskForIt()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + Item + """
                public static class Use
                {
                    public static string Run()
                    {
                        const string Document = "<ITEM code=\"c\"><name>n</name><COUNT>4</COUNT></ITEM>";
                        var loose = new XmlSerializerOptions { PropertyNameCaseInsensitive = true };
                        var read = XmlSerializer.Deserialize(Document, new AppXmlContext(loose).Item)!;
                        var reflected = XmlSerializer.Deserialize<Item>(Document, new XmlSerializerOptions { PropertyNameCaseInsensitive = true })!;
                        var exact = XmlSerializer.Deserialize("<Item code=\"c\"><name>n</name><Count>4</Count></Item>", new AppXmlContext(new XmlSerializerOptions()).Item)!;
                        return read.Code + "," + read.Name + "," + read.Count + "|" + reflected.Code + "," + reflected.Name + "," + reflected.Count + "|" + (exact.Code ?? "null") + "," + (exact.Name ?? "null") + "," + exact.Count;
                    }
                }
                """).Should().Be("c,n,4|c,n,4|null,null,4");
        }

        [TestMethod]
        public void NumbersThatAreNotFinite_AreRefusedWhenNumberHandlingIsStrict()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + Item + """
                public static class Use
                {
                    public static string Run()
                    {
                        static string Show(Item item) => item.Ratio.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        return string.Join("\n",
                            Both.Write(new Item { Name = "n", Limit = 1, Always = "a", Ratio = double.NaN }, o => new AppXmlContext(o).Item, Show),
                            Both.Write(new Item { Name = "n", Limit = 1, Always = "a", Ratio = double.NaN }, o => new AppXmlContext(o).Item, Show, o => o.NumberHandling = XmlNumberHandling.Strict),
                            Both.Write(new Item { Name = "n", Limit = 1, Always = "a", Ratio = 2 }, o => new AppXmlContext(o).Item, Show, o => o.NumberHandling = XmlNumberHandling.Strict));
                    }
                }
                """ + Compare).Should().Be(
                "<Item><Name>n</Name><Count>0</Count><Limit>1</Limit><Ratio>NaN</Ratio><Always>a</Always></Item>\nsame\nNaN\nNaN\n" +
                "XmlException: Named floating-point literals are not allowed when NumberHandling is Strict. Path: /Item.\n" +
                "<Item><Name>n</Name><Count>0</Count><Limit>1</Limit><Ratio>2</Ratio><Always>a</Always></Item>\nsame\n2\n2");
        }

        [TestMethod]
        public void NullableAnnotations_AreRespectedAsTheReflectionResolverRespectsThem()
        {
            XmlSerializerGeneratorTests.RunGenerated("#nullable enable\n" + Usings + """
                using System.Diagnostics.CodeAnalysis;

                public class Base<T> where T : class
                {
                    public T Kept { get; set; } = default!;
                }

                public sealed class Note : Base<string>
                {
                    public Note()
                    {
                        Kept = "k";
                    }

                    [XmlAttribute]
                    public string Code { get; set; } = "c";

                    public string Name { get; set; } = "n";

                    public string? Maybe { get; set; } = "m";

                    [XmlArray]
                    public List<string> Tags { get; set; } = new();

                    [AllowNull]
                    public string Allowed { get; set; } = "a";

                    [DisallowNull]
                    public string? Disallowed { get; set; } = "d";

                    [MaybeNull]
                    public string Possibly { get; set; } = "p";

                    [NotNull]
                    public string? Certain { get; set; } = "c";

                    [DisallowNull]
                    public int? Limit { get; set; } = 1;

                    [XmlIgnore(Condition = XmlIgnoreCondition.WhenWritingNull)]
                    public string Skipped { get; set; } = "s";
                }

                public sealed record Pair(string Name, string? Maybe);

                public sealed class Box<T>
                {
                    public T Value { get; set; } = default!;
                }

                public sealed class Hard<T> where T : notnull
                {
                    public T Value { get; set; } = default!;
                }

                public sealed class Bound<T> where T : IComparable
                {
                    public T Value { get; set; } = default!;
                }

                [XmlSerializable<Note>]
                [XmlSerializable<Pair>]
                [XmlSerializable<Box<string>>(TypeInfoPropertyName = "Box")]
                [XmlSerializable<Hard<string>>(TypeInfoPropertyName = "Hard")]
                [XmlSerializable<Bound<string>>(TypeInfoPropertyName = "Bound")]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                [XmlSourceGenerationOptions(RespectNullableAnnotations = true)]
                [XmlSerializable<Pair>]
                internal partial class SeededContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    private const string Nil = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>";

                    // Runs the same work through a generated context and through the reflection resolver, each with
                    // its own options, and reports one result when they agree.
                    private static string Same(Func<XmlSerializerOptions, string> generated, Func<XmlSerializerOptions, string> reflected, Action<XmlSerializerOptions>? configure = null)
                    {
                        static string Run(Func<XmlSerializerOptions, string> work, Action<XmlSerializerOptions>? configure)
                        {
                            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, RespectNullableAnnotations = true };
                            configure?.Invoke(options);
                            try
                            {
                                return work(options);
                            }
                            catch (Exception ex)
                            {
                                return ex.GetType().Name + ": " + ex.Message;
                            }
                        }

                        var fromContext = Run(generated, configure);
                        var fromReflection = Run(reflected, configure);
                        return fromContext == fromReflection ? fromContext : fromContext + " <> reflection: " + fromReflection;
                    }

                    private static string ReadNote(string member, Action<XmlSerializerOptions>? configure = null)
                    {
                        var xml = "<Note><" + member + Nil + "</Note>";
                        return Same(
                            o => XmlSerializer.Deserialize(xml, new AppXmlContext(o).Note) is null ? "null" : "read",
                            o => XmlSerializer.Deserialize(xml, typeof(Note), o) is null ? "null" : "read",
                            configure);
                    }

                    private static string WriteNote(Note note, Action<XmlSerializerOptions>? configure = null)
                    {
                        return Same(o => XmlSerializer.Serialize(note, new AppXmlContext(o).Note), o => XmlSerializer.Serialize(note, typeof(Note), o), configure);
                    }

                    public static string Run()
                    {
                        return string.Join("\n",
                            ReadNote("Name"),
                            ReadNote("Kept"),
                            ReadNote("Maybe"),
                            ReadNote("Tags"),
                            ReadNote("Allowed"),
                            ReadNote("Disallowed"),
                            ReadNote("Possibly"),
                            ReadNote("Certain"),
                            ReadNote("Limit"),
                            ReadNote("Name", o => o.RespectNullableAnnotations = false),
                            Same(
                                o => XmlSerializer.Deserialize("<Pair><Name" + Nil + "<Maybe" + Nil + "</Pair>", new AppXmlContext(o).Pair)!.ToString(),
                                o => XmlSerializer.Deserialize("<Pair><Name" + Nil + "<Maybe" + Nil + "</Pair>", typeof(Pair), o)!.ToString()!),
                            Same(
                                o => XmlSerializer.Deserialize("<Pair><Name>n</Name><Maybe" + Nil + "</Pair>", new AppXmlContext(o).Pair)!.ToString(),
                                o => XmlSerializer.Deserialize("<Pair><Name>n</Name><Maybe" + Nil + "</Pair>", typeof(Pair), o)!.ToString()!),
                            Same(
                                o => XmlSerializer.Deserialize("<BoxOfString><Value" + Nil + "</BoxOfString>", new AppXmlContext(o).Box)!.Value ?? "null",
                                o => ((Box<string>)XmlSerializer.Deserialize("<BoxOfString><Value" + Nil + "</BoxOfString>", typeof(Box<string>), o)!).Value ?? "null"),
                            Same(
                                o => XmlSerializer.Deserialize("<HardOfString><Value" + Nil + "</HardOfString>", new AppXmlContext(o).Hard)!.Value ?? "null",
                                o => ((Hard<string>)XmlSerializer.Deserialize("<HardOfString><Value" + Nil + "</HardOfString>", typeof(Hard<string>), o)!).Value ?? "null"),
                            Same(
                                o => XmlSerializer.Deserialize("<BoundOfString><Value" + Nil + "</BoundOfString>", new AppXmlContext(o).Bound)!.Value ?? "null",
                                o => ((Bound<string>)XmlSerializer.Deserialize("<BoundOfString><Value" + Nil + "</BoundOfString>", typeof(Bound<string>), o)!).Value ?? "null"),
                            WriteNote(new Note { Name = null! }),
                            WriteNote(new Note { Kept = null! }),
                            WriteNote(new Note { Code = null! }),
                            WriteNote(new Note { Tags = null! }),
                            WriteNote(new Note { Allowed = null }),
                            WriteNote(new Note { Certain = null }),
                            WriteNote(new Note { Maybe = null, Possibly = null!, Disallowed = null!, Limit = null, Skipped = null! }),
                            WriteNote(new Note { Name = null!, Kept = null!, Code = null!, Tags = null!, Allowed = null, Certain = null }, o => o.DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull),
                            WriteNote(new Note { Name = null! }, o => o.RespectNullableAnnotations = false),
                            SeededContext.Default.Options.RespectNullableAnnotations.ToString(),
                            Same(
                                o => XmlSerializer.Deserialize("<Pair><Name" + Nil + "<Maybe>m</Maybe></Pair>", SeededContext.Default.Pair)!.ToString(),
                                o => XmlSerializer.Deserialize("<Pair><Name" + Nil + "<Maybe>m</Maybe></Pair>", typeof(Pair), o)!.ToString()!));
                    }
                }
                """).Should().Be(string.Join("\n",
                Setting("Name", "Note") + " Path: /Note/Name | LineNumber: 1 | BytePositionInLine: 83.",
                Setting("Kept", "Base`1[System.String]") + " Path: /Note/Kept | LineNumber: 1 | BytePositionInLine: 83.",
                "read",
                Setting("Tags", "Note") + " Path: /Note/Tags | LineNumber: 1 | BytePositionInLine: 83.",
                "read",
                Setting("Disallowed", "Note") + " Path: /Note/Disallowed | LineNumber: 1 | BytePositionInLine: 89.",
                Setting("Possibly", "Note") + " Path: /Note/Possibly | LineNumber: 1 | BytePositionInLine: 87.",
                "read",
                Setting("Limit", "Note") + " Path: /Note/Limit | LineNumber: 1 | BytePositionInLine: 84.",
                "read",
                "XmlException: The constructor parameter 'Name' on type 'Pair' doesn't allow null values. Consider updating its nullability annotation. Path: /Pair/Name | LineNumber: 1 | BytePositionInLine: 83.",
                "Pair { Name = n, Maybe =  }",
                "null",
                Setting("Value", "Hard`1[System.String]") + " Path: /HardOfString/Value | LineNumber: 1 | BytePositionInLine: 92.",
                "null",
                Getting("Name", "Note") + " Path: /Note.",
                Getting("Kept", "Base`1[System.String]") + " Path: /Note.",
                Getting("Code", "Note") + " Path: /Note.",
                Getting("Tags", "Note") + " Path: /Note.",
                Getting("Allowed", "Note") + " Path: /Note.",
                Getting("Certain", "Note") + " Path: /Note.",
                "<Note Code=\"c\"><Kept>k</Kept><Name>n</Name><Maybe " + Nil + "/><Tags/><Allowed>a</Allowed><Disallowed " + Nil + "/><Possibly " + Nil + "/><Certain>c</Certain><Limit " + Nil + "/></Note>",
                "<Note><Maybe>m</Maybe><Disallowed>d</Disallowed><Possibly>p</Possibly><Limit>1</Limit><Skipped>s</Skipped></Note>",
                "<Note Code=\"c\"><Kept>k</Kept><Name " + Nil + "/><Maybe>m</Maybe><Tags/><Allowed>a</Allowed><Disallowed>d</Disallowed><Possibly>p</Possibly><Certain>c</Certain><Limit>1</Limit><Skipped>s</Skipped></Note>",
                "True",
                "XmlException: The constructor parameter 'Name' on type 'Pair' doesn't allow null values. Consider updating its nullability annotation. Path: /Pair/Name | LineNumber: 1 | BytePositionInLine: 83."));

            static string Getting(string member, string type) => "XmlException: The property or field '" + member + "' on type '" + type + "' doesn't allow getting null values. Consider updating its nullability annotation.";
            static string Setting(string member, string type) => "XmlException: The property or field '" + member + "' on type '" + type + "' doesn't allow setting null values. Consider updating its nullability annotation.";
        }

        [TestMethod]
        public void TypeWithItsOwnConverter_IsReadAndWrittenByIt()
        {
            XmlSerializerGeneratorTests.RunGenerated(Usings + """
                [XmlConverter<TemperatureConverter>]
                public readonly record struct Temperature(int Degrees);

                public sealed class TemperatureConverter : XmlConverter<Temperature>
                {
                    public override Temperature Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
                    {
                        var value = default(Temperature);
                        while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
                        {
                            value = ReadLexical(reader.ValueSpan, options);
                        }

                        return value;
                    }

                    public override Temperature ReadAsAttribute(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options) => ReadLexical(reader.ValueSpan, options);

                    public override Temperature ReadLexical(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
                    {
                        return new Temperature(int.Parse(System.Text.Encoding.UTF8.GetString(utf8).TrimEnd('C')));
                    }

                    public override void Write(Utf8XmlWriter writer, Temperature value, XmlSerializerOptions options) => writer.WriteStringValue(value.Degrees + "C");

                    public override void WriteAsAttribute(Utf8XmlWriter writer, XmlName name, Temperature value, XmlSerializerOptions options)
                    {
                        writer.WriteStartAttribute(name.LocalName);
                        Write(writer, value, options);
                        writer.WriteEndAttribute();
                    }
                }

                public sealed class Reading
                {
                    [XmlAttribute]
                    public Temperature Low { get; set; }

                    [XmlAttribute]
                    public Temperature? High { get; set; }

                    [XmlText]
                    public Temperature Now { get; set; }
                }

                public sealed class Log
                {
                    public Temperature Peak { get; set; }

                    public List<Temperature> All { get; set; } = new();
                }

                [XmlSerializable<Reading>]
                [XmlSerializable<Log>]
                [XmlSerializable<Temperature>]
                internal partial class AppXmlContext : XmlSerializerContext
                {
                }

                public static class Use
                {
                    public static string Run()
                    {
                        return string.Join("\n",
                            Both.Write(new Reading { Low = new Temperature(1), High = new Temperature(9), Now = new Temperature(5) }, o => new AppXmlContext(o).Reading, r => r.Low + "," + r.High + "," + r.Now),
                            Both.Write(new Log { Peak = new Temperature(7), All = { new Temperature(2), new Temperature(3) } }, o => new AppXmlContext(o).Log, l => l.Peak + "," + l.All.Count),
                            Both.Write(new Temperature(4), o => new AppXmlContext(o).Temperature, t => t.ToString()));
                    }
                }
                """ + Compare).Should().Be(
                "<Reading Low=\"1C\" High=\"9C\">5C</Reading>\nsame\nTemperature { Degrees = 1 },Temperature { Degrees = 9 },Temperature { Degrees = 5 }\nTemperature { Degrees = 1 },Temperature { Degrees = 9 },Temperature { Degrees = 5 }\n" +
                "<Log><Peak>7C</Peak><All>2C</All><All>3C</All></Log>\nsame\nTemperature { Degrees = 7 },2\nTemperature { Degrees = 7 },2\n" +
                "<Temperature>4C</Temperature>\nsame\nTemperature { Degrees = 4 }\nTemperature { Degrees = 4 }");
        }

        #endregion

    }

}
