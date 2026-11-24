using System;
using System.Collections.Generic;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.Aot
{

    /// <summary>
    /// Checks, in a Native AOT program, the features the runtime library provides from generated metadata:
    /// a constructor with parameters, a struct member, collections and a dictionary, an init-only member, a
    /// converter on a member, populated members, naming policies, derived types, references, and extension data.
    /// </summary>
    public static class EngineFeatures
    {

        #region Internal Methods

        /// <summary>
        /// Runs every check, and throws on the first result that differs from the expected one.
        /// </summary>
        internal static void Check()
        {
            var context = new FeatureXmlContext(new XmlSerializerOptions { OmitXmlDeclaration = true });

            var order = new FeatureOrder { Id = 7, Name = "n", Origin = new FeaturePoint { X = 3, Y = 4 }, Limit = 255, Counts = { ["a"] = 1 }, Numbers = { 1, 2 } };
            order.Lines.Add(new FeatureLine(2, "s") { Price = 1.5m });
            const string orderXml =
                "<FeatureOrder Id=\"7\"><Counts><a>1</a></Counts><Limit>ff</Limit><Lines><FeatureLine><Qty>2</Qty><Sku>s</Sku><Price>1.5</Price></FeatureLine></Lines>" +
                "<Name>n</Name><N>1</N><N>2</N><Origin><X>3</X><Y>4</Y></Origin></FeatureOrder>";
            Expect(XmlSerializer.Serialize(order, context.FeatureOrder), orderXml, "feature order document");
            var read = XmlSerializer.Deserialize(orderXml, context.FeatureOrder)!;
            Expect(
                read.Id + "|" + read.Name + "|" + read.Origin.X + "," + read.Origin.Y + "|" + read.Limit + "|" + read.Counts["a"] + "|" + string.Join(",", read.Numbers) + "|" + read.Lines.Count,
                "7|n|3,4|255|1|1,2|0",
                "feature order values");

            // Populate adds to the list that has no setter.
            var populate = new FeatureXmlContext(new XmlSerializerOptions { PreferredObjectCreationHandling = XmlObjectCreationHandling.Populate });
            var populated = XmlSerializer.Deserialize(orderXml, populate.FeatureOrder)!;
            Expect(populated.Lines.Count + "|" + populated.Lines[0].Qty + "|" + populated.Lines[0].Sku + "|" + populated.Lines[0].Price, "1|2|s|1.5", "feature order populated");

            // The naming policy is applied when the metadata is bound, not when the program is compiled.
            var camel = new FeatureXmlContext(new XmlSerializerOptions { OmitXmlDeclaration = true, PropertyNamingPolicy = XmlNamingPolicy.CamelCase, DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingDefault });
            Expect(XmlSerializer.Serialize(new FeatureOrder { Id = 7 }, camel.FeatureOrder), "<featureOrder id=\"7\"><counts/><lines/></featureOrder>", "feature order camel case");

            // A derived type, with the discriminator as an attribute.
            const string shapeXml = "<FeatureShape kind=\"circle\"><Name>c</Name><Radius>2</Radius></FeatureShape>";
            Expect(XmlSerializer.Serialize(new FeatureCircle { Name = "c", Radius = 2 }, context.FeatureShape), shapeXml, "feature derived type document");
            Expect(XmlSerializer.Deserialize(shapeXml, context.FeatureShape) is FeatureCircle { Radius: 2, Name: "c" } ? "circle" : "other", "circle", "feature derived type value");

            // References are preserved, and extension data is kept.
            var preserve = new FeatureXmlContext(new XmlSerializerOptions { OmitXmlDeclaration = true, ReferenceHandler = XmlReferenceHandler.Preserve });
            var node = new FeatureNode { Name = "root" };
            node.Child = node;
            const string nodeXml = "<FeatureNode xmlns:ns1=\"urn:CloudNimble.Text.Xml:refs\" ns1:id=\"1\"><Child ns1:ref=\"1\"/><Name>root</Name></FeatureNode>";
            Expect(XmlSerializer.Serialize(node, preserve.FeatureNode), nodeXml, "feature references document");
            var readNode = XmlSerializer.Deserialize(nodeXml, preserve.FeatureNode)!;
            Expect(ReferenceEquals(readNode, readNode.Child) ? "same" : "copy", "same", "feature references value");
            const string bagXml = "<FeatureNode other=\"1\"><Name>n</Name><More>m</More></FeatureNode>";
            Expect(
                XmlSerializer.Serialize(XmlSerializer.Deserialize(bagXml, context.FeatureNode)!, context.FeatureNode),
                "<FeatureNode other=\"1\"><Child xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><Name>n</Name><More>m</More></FeatureNode>",
                "feature extension data");

            // The prefixes a type declares are read from the type, and those of the options come first.
            var prefixed = new FeatureXmlContext(new XmlSerializerOptions { Namespaces = KnownNamespaces.Edmx, OmitXmlDeclaration = true });
            const string feedXml = "<feed xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\" xmlns:a=\"urn:a\"><a:Title>t</a:Title></feed>";
            Expect(XmlSerializer.Serialize(new FeatureFeed { Title = "t" }, prefixed.FeatureFeed), feedXml, "feature namespace declarations document");
            Expect(XmlSerializer.Deserialize(feedXml, prefixed.FeatureFeed)!.Title!, "t", "feature namespace declarations value");
            Expect(string.Join(",", XmlDocument.DiscoverNamespaces(feedXml).Keys), "edmx,a", "feature discovered namespaces");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Throws when <paramref name="actual"/> is not exactly <paramref name="expected"/>.
        /// </summary>
        /// <param name="actual">The result.</param>
        /// <param name="expected">The whole expected result.</param>
        /// <param name="what">The name of the check, for the message.</param>
        private static void Expect(string actual, string expected, string what)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(what + "\nexpected: " + expected + "\nactual:   " + actual);
            }
        }

        #endregion

    }

    /// <summary>
    /// A value type held as a member.
    /// </summary>
    public struct FeaturePoint
    {

        /// <summary>
        /// Gets or sets the horizontal position.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Gets or sets the vertical position.
        /// </summary>
        public int Y { get; set; }

    }

    /// <summary>
    /// A record created through a constructor with a value-type parameter.
    /// </summary>
    /// <param name="Qty">The quantity.</param>
    /// <param name="Sku">The stock keeping unit.</param>
    public sealed record FeatureLine(int Qty, string Sku)
    {

        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        public decimal Price { get; set; }

    }

    /// <summary>
    /// A converter that reads and writes an integer in hexadecimal.
    /// </summary>
    public sealed class FeatureHexConverter : XmlConverter<int>
    {

        /// <inheritdoc />
        public override int Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            var value = 0;
            while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
            {
                value = Convert.ToInt32(reader.GetString(), 16);
            }

            return value;
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, int value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("x"));
        }

    }

    /// <summary>
    /// An order with a member of each shape the runtime library reads and writes from metadata.
    /// </summary>
    public sealed class FeatureOrder
    {

        /// <summary>
        /// Gets or sets a dictionary of value types.
        /// </summary>
        public Dictionary<string, int> Counts { get; set; } = [];

        /// <summary>
        /// Gets or sets the identifier, written as an attribute.
        /// </summary>
        [XmlAttribute]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets a nullable value with a converter of its own.
        /// </summary>
        [XmlConverter<FeatureHexConverter>]
        public int? Limit { get; set; }

        /// <summary>
        /// Gets a wrapped list with no setter, which only populating can fill.
        /// </summary>
        [XmlArray]
        public List<FeatureLine> Lines { get; } = [];

        /// <summary>
        /// Gets the name, which is set only as the object is created.
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// Gets or sets a list of value types under an element name of its own.
        /// </summary>
        [XmlElement("N")]
        public List<int> Numbers { get; set; } = [];

        /// <summary>
        /// Gets or sets a value-type member.
        /// </summary>
        public FeaturePoint Origin { get; set; }

    }

    /// <summary>
    /// A base type that names a derived type, with the discriminator as an attribute.
    /// </summary>
    [XmlPolymorphic(TypeDiscriminatorPropertyName = "kind", DiscriminatorPlacement = XmlDiscriminatorPlacement.Attribute)]
    [XmlDerivedType<FeatureCircle>("circle")]
    public class FeatureShape
    {

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }

    }

    /// <summary>
    /// The derived type of <see cref="FeatureShape"/>.
    /// </summary>
    public sealed class FeatureCircle : FeatureShape
    {

        /// <summary>
        /// Gets or sets the radius.
        /// </summary>
        public int Radius { get; set; }

    }

    /// <summary>
    /// A type that refers to itself and keeps what it does not map.
    /// </summary>
    public sealed class FeatureNode
    {

        /// <summary>
        /// Gets or sets the next node, which may be this one.
        /// </summary>
        public FeatureNode? Child { get; set; }

        /// <summary>
        /// Gets or sets the attributes and elements no member maps.
        /// </summary>
        [XmlExtensionData]
        public XmlExtensionData Extra { get; set; } = new();

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }

    }

    /// <summary>
    /// A document type that declares the prefix of its namespace.
    /// </summary>
    [XmlRoot("feed")]
    [XmlNamespaceDeclaration("a", "urn:a")]
    public sealed class FeatureFeed
    {

        /// <summary>
        /// Gets or sets the title, an element in <c>urn:a</c>.
        /// </summary>
        [XmlElement("Title", "urn:a")]
        public string? Title { get; set; }

    }

    /// <summary>
    /// The context for the types <see cref="EngineFeatures"/> checks. It has no options of its own, so each check
    /// supplies them.
    /// </summary>
    [XmlSerializable<FeatureOrder>]
    [XmlSerializable<FeatureShape>]
    [XmlSerializable<FeatureNode>]
    [XmlSerializable<FeatureFeed>]
    internal partial class FeatureXmlContext : XmlSerializerContext
    {
    }

}
