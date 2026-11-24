using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Tests.POCOs;
using CloudNimble.XmlEssentials.Tests.POCOs.Prefixes;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// The namespace declarations the serializer writes on a document element:
    /// <see cref="XmlSerializerOptions.Namespaces"/>, <c>[XmlNamespaceDeclaration]</c> on the type, and
    /// <see cref="KnownNamespaces"/>.
    /// </summary>
    [TestClass]
    public class XmlSerializerNamespaceDeclarationTests
    {

        #region Fields

        private const string FeedXml = "<feed xmlns:a=\"urn:a\" xmlns:b=\"urn:b\" b:id=\"7\"><a:x>1</a:x><y>2</y><b:entry><a:z>3</a:z></b:entry></feed>";

        #endregion

        #region Public Methods

        [TestMethod]
        public void AttributeOnTheType_DeclaresItsPrefixesOnTheDocumentElement()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            XmlSerializer.Serialize(Feed(), options).Should().Be(FeedXml);

            var read = XmlSerializer.Deserialize<PrefixedFeed>(FeedXml)!;
            read.Id.Should().Be(7);
            read.X.Should().Be("1");
            read.Y.Should().Be("2");
            read.Entry!.Z.Should().Be("3");
        }

        [TestMethod]
        public void AttributeOnTheType_DoesNothing_WhereTheTypeIsNotTheDocumentElement()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            XmlSerializer.Serialize(new FeedHolder { Feed = Feed() }, options).Should().Be(
                "<FeedHolder><Feed xmlns:ns1=\"urn:b\" ns1:id=\"7\"><x xmlns=\"urn:a\">1</x><y>2</y><ns1:entry><z xmlns=\"urn:a\">3</z></ns1:entry></Feed></FeedHolder>");
        }

        [TestMethod]
        public void AttributeOnTheType_ThatCannotBeWritten_IsRefusedWhenTheTypeIsFirstUsed()
        {
            FluentActions.Invoking(() => XmlSerializer.Serialize(new NullPrefixDeclared()))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(
                    "[XmlNamespaceDeclarationAttribute] on 'CloudNimble.XmlEssentials.Tests.POCOs.Prefixes.NullPrefixDeclared' cannot be written. A prefix is null. Use an empty string for the default namespace.");
            FluentActions.Invoking(() => XmlSerializer.Serialize(new BadPrefixDeclared()))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(
                    "[XmlNamespaceDeclarationAttribute] on 'CloudNimble.XmlEssentials.Tests.POCOs.Prefixes.BadPrefixDeclared' cannot be written. The prefix 'xmlns' cannot be bound to 'urn:x': The xmlns prefix cannot be declared.");
        }

        [TestMethod]
        public void KnownNamespaces_AreTheCustomaryPrefixesOfEachFormat()
        {
            Pairs(KnownNamespaces.Edmx).Should().Equal("edmx=http://docs.oasis-open.org/odata/ns/edmx");
            Pairs(KnownNamespaces.Podcast).Should().Equal(
                "atom=http://www.w3.org/2005/Atom",
                "content=http://purl.org/rss/1.0/modules/content/",
                "dc=http://purl.org/dc/elements/1.1/",
                "googleplay=http://www.google.com/schemas/play-podcasts/1.0",
                "itunes=http://www.itunes.com/dtds/podcast-1.0.dtd",
                "media=http://search.yahoo.com/mrss/",
                "podcast=https://podcastindex.org/namespace/1.0",
                "psc=http://podlove.org/simple-chapters",
                "sy=http://purl.org/rss/1.0/modules/syndication/");
            Pairs(KnownNamespaces.Rss).Should().Equal(
                "atom=http://www.w3.org/2005/Atom",
                "content=http://purl.org/rss/1.0/modules/content/",
                "dc=http://purl.org/dc/elements/1.1/",
                "georss=http://www.georss.org/georss",
                "media=http://search.yahoo.com/mrss/",
                "slash=http://purl.org/rss/1.0/modules/slash/",
                "sy=http://purl.org/rss/1.0/modules/syndication/",
                "wfw=http://wellformedweb.org/CommentAPI/");
            Pairs(KnownNamespaces.Sitemap).Should().Equal(
                "image=http://www.google.com/schemas/sitemap-image/1.1",
                "news=http://www.google.com/schemas/sitemap-news/0.9",
                "video=http://www.google.com/schemas/sitemap-video/1.1",
                "xhtml=http://www.w3.org/1999/xhtml");
            Pairs(KnownNamespaces.Soap11).Should().Equal(
                "soap=http://schemas.xmlsoap.org/soap/envelope/",
                "xsd=http://www.w3.org/2001/XMLSchema",
                "xsi=http://www.w3.org/2001/XMLSchema-instance");
            Pairs(KnownNamespaces.Soap12).Should().Equal(
                "soap=http://www.w3.org/2003/05/soap-envelope",
                "xsd=http://www.w3.org/2001/XMLSchema",
                "xsi=http://www.w3.org/2001/XMLSchema-instance");
            Pairs(KnownNamespaces.Svg).Should().Equal("svg=http://www.w3.org/2000/svg", "xlink=http://www.w3.org/1999/xlink");
            Pairs(KnownNamespaces.XmlSchema).Should().Equal("xsd=http://www.w3.org/2001/XMLSchema", "xsi=http://www.w3.org/2001/XMLSchema-instance");
        }

        [TestMethod]
        public void KnownNamespaces_CanBeAssignedToTheOptions_AndCannotBeChanged()
        {
            var options = new XmlSerializerOptions { Namespaces = KnownNamespaces.Svg, OmitXmlDeclaration = true };

            XmlSerializer.Serialize(new LinkPair { Plain = "p", Qualified = "q" }, options).Should().Be(
                "<LinkPair xmlns:svg=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\"><x>p</x><x xmlns=\"urn:a\">q</x></LinkPair>");
            FluentActions.Invoking(() => ((IDictionary<string, string>)KnownNamespaces.Svg).Add("x", "urn:x"))
                .Should().ThrowExactly<NotSupportedException>();
        }

        [TestMethod]
        public void Namespaces_AreCopiedWhenSet_AndCopiedToAnotherInstance()
        {
            var source = new Dictionary<string, string> { ["a"] = "urn:a" };
            var options = new XmlSerializerOptions { Namespaces = source, OmitXmlDeclaration = true };
            source["b"] = "urn:b";

            Pairs(options.Namespaces!).Should().Equal("a=urn:a");
            Pairs(new XmlSerializerOptions(options).Namespaces!).Should().Equal("a=urn:a");

            options.Namespaces = null;
            options.Namespaces.Should().BeNull();
            XmlSerializer.Serialize(new LinkPair { Plain = "p", Qualified = "q" }, options).Should().Be("<LinkPair><x>p</x><x xmlns=\"urn:a\">q</x></LinkPair>");
            FluentActions.Invoking(() => options.Namespaces = source)
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be("This XmlSerializerOptions instance is read-only.");
        }

        [TestMethod]
        public async Task Namespaces_AreDeclaredOnTheElementThatHoldsASequence_AndOnEachTopLevelValue()
        {
            var options = new XmlSerializerOptions { Namespaces = new Dictionary<string, string> { ["a"] = "urn:a" }, OmitXmlDeclaration = true };
            var values = new[] { new LinkPair { Plain = "p", Qualified = "q" }, new LinkPair { Plain = "s", Qualified = "r" } };

            using var wrapped = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(wrapped, Values(values), options);
            Encoding.UTF8.GetString(wrapped.ToArray()).Should().Be(
                "<ArrayOfLinkPair xmlns:a=\"urn:a\"><LinkPair><x>p</x><a:x>q</a:x></LinkPair><LinkPair><x>s</x><a:x>r</a:x></LinkPair></ArrayOfLinkPair>");

            using var topLevel = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(topLevel, Values(values), topLevelValues: true, options);
            Encoding.UTF8.GetString(topLevel.ToArray()).Should().Be(
                "<LinkPair xmlns:a=\"urn:a\"><x>p</x><a:x>q</a:x></LinkPair><LinkPair xmlns:a=\"urn:a\"><x>s</x><a:x>r</a:x></LinkPair>");
        }

        [TestMethod]
        public void Namespaces_AreDeclaredOnTheDocumentElement_InTheirOrder_UsedOrNot()
        {
            var options = new XmlSerializerOptions
            {
                Namespaces = new Dictionary<string, string> { ["unused"] = "urn:u", ["a"] = "urn:a" },
                OmitXmlDeclaration = true,
            };
            const string xml = "<LinkPair xmlns:unused=\"urn:u\" xmlns:a=\"urn:a\"><x>p</x><a:x>q</a:x></LinkPair>";

            XmlSerializer.Serialize(new LinkPair { Plain = "p", Qualified = "q" }, options).Should().Be(xml);

            var read = XmlSerializer.Deserialize<LinkPair>(xml, options)!;
            read.Plain.Should().Be("p");
            read.Qualified.Should().Be("q");
        }

        [TestMethod]
        public void Namespaces_AreDeclaredOnTheDocumentElementOfANullValue()
        {
            var options = new XmlSerializerOptions { Namespaces = new Dictionary<string, string> { ["a"] = "urn:a" }, OmitXmlDeclaration = true };

            XmlSerializer.Serialize<LinkPair?>(null, options).Should().Be(
                "<LinkPair xmlns:a=\"urn:a\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>");
        }

        [TestMethod]
        public void Namespaces_ComeBeforeThoseOfTheType_WhichGiveWayByPrefixAndByNamespace()
        {
            // The options name urn:a under another prefix, so the type's declaration of it is left out.
            var renamed = new XmlSerializerOptions { Namespaces = new Dictionary<string, string> { ["q"] = "urn:a" }, OmitXmlDeclaration = true };
            XmlSerializer.Serialize(Feed(), renamed).Should().Be(
                "<feed xmlns:q=\"urn:a\" xmlns:b=\"urn:b\" b:id=\"7\"><q:x>1</q:x><y>2</y><b:entry><q:z>3</q:z></b:entry></feed>");

            // The options bind the type's prefix to something else, so urn:a is declared where it is used.
            var taken = new XmlSerializerOptions { Namespaces = new Dictionary<string, string> { ["a"] = "urn:other" }, OmitXmlDeclaration = true };
            XmlSerializer.Serialize(Feed(), taken).Should().Be(
                "<feed xmlns:a=\"urn:other\" xmlns:b=\"urn:b\" b:id=\"7\"><x xmlns=\"urn:a\">1</x><y>2</y><b:entry><z xmlns=\"urn:a\">3</z></b:entry></feed>");
        }

        [TestMethod]
        [DataRow("xmlns", "urn:x", "The prefix 'xmlns' cannot be bound to 'urn:x': The xmlns prefix cannot be declared. (Parameter 'namespaces')")]
        [DataRow("xml", "urn:x", "The prefix 'xml' cannot be bound to 'urn:x': The xml prefix cannot be rebound. (Parameter 'namespaces')")]
        [DataRow("p", "http://www.w3.org/XML/1998/namespace", "The prefix 'p' cannot be bound to 'http://www.w3.org/XML/1998/namespace': The xml and xmlns namespaces cannot be bound to another prefix. (Parameter 'namespaces')")]
        [DataRow("", "http://www.w3.org/2000/xmlns/", "The prefix '' cannot be bound to 'http://www.w3.org/2000/xmlns/': The xml and xmlns namespaces cannot be declared as the default namespace. (Parameter 'namespaces')")]
        [DataRow("a b", "urn:x", "The prefix 'a b' cannot be bound to 'urn:x': Invalid XML name. (Parameter 'namespaces')")]
        [DataRow("a:b", "urn:x", "The prefix 'a:b' cannot be bound to 'urn:x': Invalid XML name. (Parameter 'namespaces')")]
        [DataRow("p", "", "The namespace URI of the prefix 'p' is null or empty. (Parameter 'namespaces')")]
        [DataRow("p", null, "The namespace URI of the prefix 'p' is null or empty. (Parameter 'namespaces')")]
        public void Namespaces_ThatCannotBeWritten_AreRefusedWhenSet(string prefix, string? namespaceUri, string message)
        {
            var options = new XmlSerializerOptions();

            FluentActions.Invoking(() => options.Namespaces = new Dictionary<string, string> { [prefix] = namespaceUri! })
                .Should().ThrowExactly<ArgumentException>().Which.Message.Should().Be(message);
            options.Namespaces.Should().BeNull();
        }

        [TestMethod]
        public void TheDefaultNamespace_CanBeDeclared_AndAnElementWithNoNamespaceOfItsOwnIsThenInIt()
        {
            var options = new XmlSerializerOptions { Namespaces = new Dictionary<string, string> { [""] = "urn:d" }, OmitXmlDeclaration = true };
            const string xml = "<LinkPair xmlns=\"urn:d\"><x>p</x><x xmlns=\"urn:a\">q</x></LinkPair>";

            XmlSerializer.Serialize(new LinkPair { Plain = "p", Qualified = "q" }, options).Should().Be(xml);

            var read = XmlSerializer.Deserialize<LinkPair>(xml, options)!;
            read.Plain.Should().Be("p");
            read.Qualified.Should().Be("q");
        }

        [TestMethod]
        public void TheDocumentElementsOwnNamespace_IsDeclaredOnce_AndDecidesHowTheElementIsWritten()
        {
            var value = new QualifiedRoot { V = "1" };
            static XmlSerializerOptions With(params (string Prefix, string Uri)[] namespaces) => new()
            {
                Namespaces = new OrderedDictionary<string, string>(namespaces.Select(pair => KeyValuePair.Create(pair.Prefix, pair.Uri))),
                OmitXmlDeclaration = true,
            };

            // Declared as the default namespace: no prefix, and the member with no namespace of its own is in it.
            XmlSerializer.Serialize(value, With(("", "urn:r"))).Should().Be("<root xmlns=\"urn:r\"><v>1</v></root>");
            XmlSerializer.Serialize(value, With(("r", "urn:r"), ("", "urn:r"))).Should().Be("<root xmlns=\"urn:r\" xmlns:r=\"urn:r\"><v>1</v></root>");

            // Declared with a prefix: the element has the prefix, and an element with no prefix has no namespace.
            XmlSerializer.Serialize(value, With(("r", "urn:r"))).Should().Be("<r:root xmlns:r=\"urn:r\"><v>1</v></r:root>");
            XmlSerializer.Serialize(value, With(("r", "urn:r"), ("", "urn:other"))).Should().Be("<r:root xmlns:r=\"urn:r\" xmlns=\"urn:other\"><v>1</v></r:root>");

            // Two prefixes for one namespace: the one declared last is used, here and inside.
            XmlSerializer.Serialize(value, With(("r", "urn:r"), ("s", "urn:r"))).Should().Be("<s:root xmlns:s=\"urn:r\" xmlns:r=\"urn:r\"><v>1</v></s:root>");

            // Every one of them reads back.
            XmlSerializer.Deserialize<QualifiedRoot>("<r:root xmlns:r=\"urn:r\"><v>1</v></r:root>")!.V.Should().Be("1");
        }

        [TestMethod]
        public void Namespaces_AreDeclaredOnEveryKindOfDocumentElement()
        {
            var options = new XmlSerializerOptions { Namespaces = new Dictionary<string, string> { ["a"] = "urn:a" }, OmitXmlDeclaration = true };

            // A single value, a collection, and a value of a derived type, which is written under that type's name.
            XmlSerializer.Serialize(5, options).Should().Be("<int xmlns:a=\"urn:a\">5</int>");
            XmlSerializer.Serialize(new List<int> { 1, 2 }, options).Should().Be("<ArrayOfInt xmlns:a=\"urn:a\"><int>1</int><int>2</int></ArrayOfInt>");
            XmlSerializer.Serialize<NamedShape>(new NamedCircle { Label = "c", Radius = 2 }, options).Should().Be(
                "<NamedCircle xmlns:a=\"urn:a\"><Label>c</Label><Radius>2</Radius></NamedCircle>");
        }

        [TestMethod]
        public void TheDocumentElementsOwnNamespace_WithNoPrefix_CannotShareTheElementWithAnotherDefaultNamespace()
        {
            var options = new XmlSerializerOptions { Namespaces = new Dictionary<string, string> { [""] = "urn:other" }, OmitXmlDeclaration = true };

            FluentActions.Invoking(() => XmlSerializer.Serialize(new QualifiedRoot { V = "1" }, options))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(
                    "The document element of 'CloudNimble.XmlEssentials.Tests.POCOs.Prefixes.QualifiedRoot' is in the namespace 'urn:r', and no prefix is declared for that namespace, so the default namespace cannot be declared as 'urn:other'.");
        }

        #endregion

        #region Private Methods

        private static PrefixedFeed Feed()
        {
            return new PrefixedFeed { Id = 7, X = "1", Y = "2", Entry = new PrefixedEntry { Z = "3" } };
        }

        private static string[] Pairs(IReadOnlyDictionary<string, string> namespaces)
        {
            return [.. namespaces.Select(pair => pair.Key + "=" + pair.Value)];
        }

        private static async IAsyncEnumerable<T> Values<T>(IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                yield return value;
            }

            await Task.CompletedTask;
        }

        #endregion

    }

}
