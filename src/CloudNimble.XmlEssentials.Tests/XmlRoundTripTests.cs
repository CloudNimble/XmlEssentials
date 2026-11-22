using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Round-trip properties: what the writer emits, the reader returns; what the DOM parses, it writes back.
    /// </summary>
    [TestClass]
    public class XmlRoundTripTests
    {

        #region Fields

        private static readonly string[] Fragments =
        [
            "a", "Z", "0", " ", "  ", "\t", "\n", "\r", "\r\n", "&", "<", ">", "\"", "'", "]", "]]", "]]>", "&amp;", "&#10;",
            "\u00E9", "\u4E2D", "\uD83D\uDE00", "\u0085", "\u2028", "\u00A0", "\uFFFD", "=", "/", "?>", "-->", "<!--", "<![CDATA[",
        ];

        private static readonly string[] Xml11Fragments = ["\u0001", "\u0008", "\u000B", "\u001F", "\u007F", "\u0080", "\u0084", "\u0086", "\u009F"];

        #endregion

        #region Public Methods

        [TestMethod]
        [DataRow(XmlVersion.Version10)]
        [DataRow(XmlVersion.Version11)]
        public void Writer_ThenReader_ReturnsEveryValueExactly(XmlVersion version)
        {
            var random = new Random(20261122);
            var fragments = new List<string>(Fragments);
            if (version is XmlVersion.Version11)
            {
                fragments.AddRange(Xml11Fragments);
            }

            for (var iteration = 0; iteration < 3000; iteration++)
            {
                var attributeValue = Compose(random, fragments);
                var textValue = Compose(random, fragments);
                var buffer = new ArrayBufferWriter<byte>();
                using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { Version = version }))
                {
                    writer.WriteStartElement("e");
                    writer.WriteAttributeString("a", attributeValue);
                    writer.WriteAttributeString("b"u8, Encoding.UTF8.GetBytes(attributeValue));
                    writer.WriteStartElement("chars");
                    writer.WriteStringValue(textValue);
                    writer.WriteEndElement();
                    writer.WriteElementString("bytes"u8, Encoding.UTF8.GetBytes(textValue));
                    writer.WriteEndElement();
                    writer.Flush();
                }

                var reader = new Utf8XmlReader(buffer.WrittenSpan, new XmlReaderOptions { WhitespaceHandling = XmlWhitespaceHandling.Significant });
                reader.Read().Should().BeTrue();
                reader.TokenType.Should().Be(XmlTokenType.XmlDeclaration);
                reader.Read().Should().BeTrue();
                reader.MoveToNextAttribute().Should().BeTrue();
                reader.GetString().Should().Be(attributeValue, "attribute written from chars, iteration {0}", iteration);
                reader.MoveToNextAttribute().Should().BeTrue();
                reader.GetString().Should().Be(attributeValue, "attribute written from UTF-8, iteration {0}", iteration);
                ReadElementText(ref reader).Should().Be(textValue, "text written from chars, iteration {0}", iteration);
                ReadElementText(ref reader).Should().Be(textValue, "text written from UTF-8, iteration {0}", iteration);

                using var document = XmlDocument.Parse(buffer.WrittenSpan, new XmlDocumentOptions { WhitespaceHandling = XmlWhitespaceHandling.Significant });
                document.RootElement.GetAttribute("a").GetString().Should().Be(attributeValue);
                document.RootElement.GetChild("chars").GetString().Should().Be(textValue);
                var rewritten = new ArrayBufferWriter<byte>();
                using (var rewriter = new Utf8XmlWriter(rewritten, new XmlWriterOptions { Version = version }))
                {
                    document.WriteTo(rewriter);
                    rewriter.Flush();
                }

                rewritten.WrittenSpan.SequenceEqual(buffer.WrittenSpan).Should().BeTrue(
                    "the DOM must write back the writer's own output byte for byte, iteration {0}: {1}",
                    iteration,
                    Encoding.UTF8.GetString(buffer.WrittenSpan));
            }
        }

        [TestMethod]
        [DataRow("<r xmlns:p=\"urn:p\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"p:T\"/>", DisplayName = "prefix used only in an attribute value")]
        [DataRow("<r xmlns:unused=\"urn:u\"><c/></r>", DisplayName = "unused declaration")]
        [DataRow("<r xmlns=\"urn:d\"><c xmlns=\"\"><g/></c><d/></r>", DisplayName = "default namespace undeclared")]
        [DataRow("<r xmlns=\"urn:d\" xmlns:p=\"urn:d\"><c/><p:c/></r>", DisplayName = "default and prefix on one namespace")]
        [DataRow("<e p:a=\"1\" xmlns:p=\"urn:p\" b=\"2\" xmlns=\"urn:d\"/>", DisplayName = "attribute before its declaration")]
        [DataRow("<r><a/><b></b><c> </c></r>", DisplayName = "empty tag, start-end pair, whitespace text")]
        [DataRow("<e a=\"x&#xA;y&#x9;z&#xD;w\" b=\"&lt;&quot;'&amp;\">a&#xD;b\nc\td</e>", DisplayName = "character references that must stay references")]
        [DataRow("<p>hello <b>world</b> !</p>", DisplayName = "mixed content")]
        [DataRow("<!--a--><?pi d?><r><!--b--><?q?><![CDATA[x<y]]></r><!--c--><?z z?>", DisplayName = "prolog and epilog nodes")]
        [DataRow("<r xml:lang=\"en\" xml:space=\"preserve\"><e xml:lang=\"\"/></r>", DisplayName = "xml prefix")]
        [DataRow("<a:r xmlns:a=\"urn:1\"><a:e xmlns:a=\"urn:2\"><a:f/></a:e><a:g/></a:r>", DisplayName = "prefix rebound in a nested scope")]
        [DataRow("<r>]]&gt;<e a=\"]]>\"/></r>", DisplayName = "CDATA end marker in text and attribute")]
        public void Document_WriteTo_ReproducesItsInput(string xml)
        {
            Rewrite(xml).Should().Be(xml);
        }

        [TestMethod]
        [DataRow("<e a='1'/>", "<e a=\"1\"/>", DisplayName = "single quotes become double quotes")]
        [DataRow("<e  a = \"1\"  ></e >", "<e a=\"1\"></e>", DisplayName = "whitespace inside tags is dropped")]
        [DataRow("<e a=\"l1\nl2\tt\">x&#65;&gt;</e>", "<e a=\"l1 l2 t\">xA></e>", DisplayName = "attribute whitespace normalizes, references resolve")]
        [DataRow("<e>a\r\nb\rc</e>", "<e>a\nb\nc</e>", DisplayName = "line ends normalize")]
        [DataRow("<?xml version='1.0' encoding='UTF-8' standalone='no'?><e/>", "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?><e/>", DisplayName = "declaration is canonical")]
        [DataRow("<?xml version=\"1.0\"?>\n<!--c-->\n<e/>\n", "<?xml version=\"1.0\" encoding=\"utf-8\"?><!--c--><e/>", DisplayName = "whitespace outside the root is dropped")]
        public void Document_WriteTo_NormalizesOnlyWhatTheInfosetIgnores(string xml, string expected)
        {
            Rewrite(xml).Should().Be(expected);
            Rewrite(expected).Should().Be(expected);
        }

        [TestMethod]
        [DataRow("<p>a <b>b</b> <i>c</i></p>", "a b c", DisplayName = "whitespace between inline elements in mixed content")]
        [DataRow("<p> a </p>", " a ", DisplayName = "whitespace around text in a leaf")]
        [DataRow("<r><c> </c></r>", " ", DisplayName = "whitespace-only leaf")]
        [DataRow("<r>\n  <c>x</c>\n</r>", "x", DisplayName = "indentation between elements")]
        public void Document_DefaultOptions_WhitespaceHandling(string xml, string text)
        {
            using var document = XmlDocument.Parse(xml);
            document.RootElement.GetString().Should().Be(text);
        }

        [TestMethod]
        public void Reader_DefaultOptions_ReportsOnlySignificantWhitespace()
        {
            var options = new XmlReaderOptions();
            XmlReaderHarness.Tokens("<r>\n  <c> </c>\n  <p>a <b>b</b> <i>c</i> </p>\n  <d><e/> </d>\n</r>"u8, options).Should().Equal(
                "StartElement :r{} open",
                "StartElement :c{} open",
                "Text :{} [ ]",
                "EndElement :c{}",
                "StartElement :p{} open",
                "Text :{} [a ]",
                "StartElement :b{} open",
                "Text :{} [b]",
                "EndElement :b{}",
                "Text :{} [ ]",
                "StartElement :i{} open",
                "Text :{} [c]",
                "EndElement :i{}",
                "Text :{} [ ]",
                "EndElement :p{}",
                "StartElement :d{} open",
                "StartElement :e{} empty",
                "EndElement :d{}",
                "EndElement :r{}");

            XmlReaderHarness.Tokens("<r xml:space=\"preserve\"> <a xml:space=\"default\"> <b/> </a><c xml:space=\"other\"> <d/></c></r>"u8, options).Should().Equal(
                "StartElement :r{} open @xml:space{http://www.w3.org/XML/1998/namespace}=[preserve]",
                "Text :{} [ ]",
                "StartElement :a{} open @xml:space{http://www.w3.org/XML/1998/namespace}=[default]",
                "StartElement :b{} empty",
                "EndElement :a{}",
                "StartElement :c{} open @xml:space{http://www.w3.org/XML/1998/namespace}=[other]",
                "Text :{} [ ]",
                "StartElement :d{} empty",
                "EndElement :c{}",
                "EndElement :r{}");

            XmlReaderHarness.Tokens("<r> <![CDATA[x]]> <a/> </r>"u8, options).Should().Equal(
                "StartElement :r{} open",
                "CData :{} [x]",
                "Text :{} [ ]",
                "StartElement :a{} empty",
                "Text :{} [ ]",
                "EndElement :r{}");

            XmlReaderHarness.MustThrow("<r> <a/></r>", new XmlReaderOptions { WhitespaceHandling = XmlWhitespaceHandling.None });
            XmlReaderHarness.MustRead("<r> </r>", new XmlReaderOptions { WhitespaceHandling = XmlWhitespaceHandling.None });
        }

        [TestMethod]
        public void Document_DefaultOptions_DropOnlyWhitespaceBetweenElements()
        {
            using var mixed = XmlDocument.Parse("<p>a <b>b</b> <i>c</i></p>");
            mixed.RootElement.GetRawXml().Should().Be("<p>a <b>b</b> <i>c</i></p>");
            using var leaf = XmlDocument.Parse("<r><c> </c></r>");
            leaf.RootElement.GetRawXml().Should().Be("<r><c> </c></r>");
            using var indented = XmlDocument.Parse("<r>\n  <c>x</c>\n</r>");
            indented.RootElement.GetRawXml().Should().Be("<r><c>x</c></r>");
        }

        [TestMethod]
        public void Document_WriteTo_Xml11_KeepsLineEndReferences()
        {
            const string xml = "<?xml version=\"1.1\" encoding=\"utf-8\"?><r xmlns:p=\"urn:p\"><e xmlns:p=\"\" a=\"&#x85;&#x2028;\">&#x85;&#x2028;&#x1;</e></r>";
            Rewrite(xml, XmlVersion.Version11).Should().Be(xml);
            using var document = XmlDocument.Parse(xml);
            document.RootElement.GetChild("e").GetString().Should().Be("\u0085\u2028\u0001");
            document.Declaration!.Value.Version.Should().Be("1.1");
        }

        [TestMethod]
        public void Document_SubtreeWriteTo_DeclaresWhatItsAncestorsDeclared()
        {
            using var document = XmlDocument.Parse("<r xmlns=\"urn:d\" xmlns:p=\"urn:p\"><c p:a=\"1\"><p:g/></c><n xmlns=\"\"><m/></n></r>");
            document.RootElement.GetChild("c", "urn:d").GetRawXml().Should().Be("<c xmlns=\"urn:d\" xmlns:p=\"urn:p\" p:a=\"1\"><p:g/></c>");
            document.RootElement.GetChild("n").GetRawXml().Should().Be("<n xmlns=\"\"><m/></n>");
            document.RootElement.GetChild("n").GetChild("m").GetRawXml().Should().Be("<m/>");
        }

        [TestMethod]
        public void NodeTree_WriteTo_KeepsNamespacesAndDeclarations()
        {
            const string xml = "<r xmlns=\"urn:d\" xmlns:p=\"urn:p\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"p:T\"><c xmlns=\"\" a=\"x&#xA;y\"/><p:e/></r>";
            var root = (XmlElementNode)XmlNode.Parse(xml)!;
            root.ToString().Should().Be(xml);
            root.DeepClone().ToString().Should().Be(xml);

            var built = new XmlElementNode("r", "urn:d");
            built.AddElement("same", "urn:d");
            built.AddElement("none");
            built.Add(new XmlElementNode("e", "urn:p", "p"));
            built.SetAttribute("a", "1", "urn:q");
            built.ToString().Should().Be("<r xmlns=\"urn:d\" xmlns:ns1=\"urn:q\" ns1:a=\"1\"><same/><none xmlns=\"\"/><p:e xmlns:p=\"urn:p\"/></r>");

            var renamed = (XmlElementNode)XmlNode.Parse("<p:e xmlns:p=\"urn:old\"/>")!;
            renamed.NamespaceUri = "urn:new";
            renamed.ToString().Should().Be("<p:e xmlns:p=\"urn:new\"/>");
        }

        [TestMethod]
        public void NodeTree_PrefixWithoutNamespace()
        {
            var orphan = new XmlElementNode("e", string.Empty, "p");
            var threw = false;
            try
            {
                orphan.ToString();
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
            XmlWriterHarness.ToXml(orphan.WriteTo, new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true }).Should().Be("<p:e/>");
        }

        [TestMethod]
        [DataRow("ISO-8859-1", "iso-8859-1")]
        [DataRow("x.y_z-1", "x.y_z-1")]
        [DataRow("UTF-16", "utf-16")]
        [DataRow("A9", "a9")]
        public void Reader_Declaration_UnsupportedEncoding_IsNamed(string declared, string reported)
        {
            string? message = null;
            try
            {
                XmlReaderHarness.Drain("<?xml version=\"1.0\" encoding=\"" + declared + "\"?><e/>");
            }
            catch (XmlException ex)
            {
                message = ex.Message;
            }

            message.Should().Be("XmlEssentials is UTF-8 only. Declared encoding '" + reported + "' is not supported.");
            XmlReaderHarness.MustThrow("<?xml version=\"1.0\" encoding=\"utf+8\"?><e/>");
            XmlReaderHarness.MustThrow("<?xml version=\"1.0\" encoding=\"utf/8\"?><e/>");
            XmlReaderHarness.MustThrow("<?xml version=\"1.0\" encoding=\"utf:8\"?><e/>");
            XmlReaderHarness.MustThrow("<?xml version=\"1.0\" encoding=\"utf~8\"?><e/>");
            XmlReaderHarness.MustThrow("<?xml version=\"1.0\" encoding=\"[utf8\"?><e/>");
            XmlReaderHarness.MustThrow("<?xml version=\"1.0\" encoding=\"{utf8\"?><e/>");
        }

        [TestMethod]
        public void Writer_Indent_NeverTouchesCharacterData()
        {
            var options = new XmlWriterOptions { OmitXmlDeclaration = true, Indented = true, NewLine = "\n", IndentSize = 2 };
            XmlWriterHarness.ToXml(
                w =>
                {
                    w.WriteStartElement("r");
                    w.WriteStartElement("only-elements");
                    w.WriteEmptyElement("a");
                    w.WriteEmptyElement("b");
                    w.WriteEndElement();
                    w.WriteStartElement("mixed");
                    w.WriteStringValue("hello ");
                    w.WriteStartElement("b");
                    w.WriteStringValue("world");
                    w.WriteEndElement();
                    w.WriteStringValue("!");
                    w.WriteEndElement();
                    w.WriteElementString("leaf", "v");
                    w.WriteElementString("utf8"u8, "v"u8);
                    w.WriteStartElement("cdata");
                    w.WriteCData("x");
                    w.WriteEndElement();
                    w.WriteStartElement("empty");
                    w.WriteFullEndElement();
                    w.WriteEndElement();
                },
                options).Should().Be(
                    "<r>\n  <only-elements>\n    <a/>\n    <b/>\n  </only-elements>\n  <mixed>hello <b>world</b>!</mixed>\n"
                    + "  <leaf>v</leaf>\n  <utf8>v</utf8>\n  <cdata><![CDATA[x]]></cdata>\n  <empty></empty>\n</r>");

            using var document = XmlDocument.Parse("<r><p>hello <b>world</b> !</p><q><x/></q></r>");
            XmlWriterHarness.ToXml(document.WriteTo, options).Should().Be("<r>\n  <p>hello <b>world</b> !</p>\n  <q>\n    <x/>\n  </q>\n</r>");
        }

        [TestMethod]
        public void Writer_IllegalCharacters()
        {
            var version11 = new XmlWriterOptions { OmitXmlDeclaration = true, Version = XmlVersion.Version11 };
            var unchecked11 = new XmlWriterOptions { OmitXmlDeclaration = true, Version = XmlVersion.Version11, SkipValidation = true };
            var control = ((char)1).ToString();
            var nel = ((char)0x85).ToString();
            byte[] truncated = [(byte)'a', 0xC3];

            XmlWriterHarness.MustThrow(w => w.WriteElementString("e", control));
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("a", control);
            });
            XmlWriterHarness.ToXml(w => w.WriteElementString("e", control), version11).Should().Be("<e>&#x1;</e>");
            XmlWriterHarness.ToXml(
                w =>
                {
                    w.WriteStartElement("e");
                    w.WriteAttributeString("a", control + nel);
                    w.WriteEndElement();
                },
                version11).Should().Be("<e a=\"&#x1;&#x85;\"/>");

            foreach (var payload in new[] { control, ((char)0xFFFF).ToString() })
            {
                XmlWriterHarness.MustThrow(w => w.WriteComment(payload));
                XmlWriterHarness.MustThrow(w => w.WriteProcessingInstruction("pi", payload));
                XmlWriterHarness.MustThrow(w =>
                {
                    w.WriteStartElement("e");
                    w.WriteCData(payload);
                });
            }

            XmlWriterHarness.MustThrow(w => w.WriteComment(control), version11);
            XmlWriterHarness.MustThrow(w => w.WriteComment(((char)0).ToString()), version11);
            XmlWriterHarness.MustThrow(w => w.WriteComment(truncated));
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteComment("caf" + (char)0xE9 + " " + nel);
                w.WriteStartElement("e");
                w.WriteEndElement();
            }, version11).Should().Be("<!--caf" + (char)0xE9 + " " + nel + "--><e/>");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteComment(control);
                w.WriteStartElement("e");
                w.WriteEndElement();
            }, unchecked11).Should().Be("<!--" + control + "--><e/>");
        }

        [TestMethod]
        public void Writer_ReservedNamespaces_AreRejected()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteNamespaceDeclaration("p", "http://www.w3.org/XML/1998/namespace");
            });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteNamespaceDeclaration("p", "http://www.w3.org/2000/xmlns/");
            });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteDefaultNamespace("http://www.w3.org/XML/1998/namespace");
            });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteDefaultNamespace("http://www.w3.org/2000/xmlns/");
            });
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteNamespaceDeclaration("xml", "http://www.w3.org/XML/1998/namespace");
                w.WriteEndElement();
            }).Should().Be("<e xmlns:xml=\"http://www.w3.org/XML/1998/namespace\"/>");
            XmlWriterHarness.ToXml(
                w =>
                {
                    w.WriteStartElement("e");
                    w.WriteNamespaceDeclaration("p", "http://www.w3.org/2000/xmlns/");
                    w.WriteDefaultNamespace("http://www.w3.org/XML/1998/namespace");
                    w.WriteEndElement();
                },
                new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true })
                .Should().Be("<e xmlns:p=\"http://www.w3.org/2000/xmlns/\" xmlns=\"http://www.w3.org/XML/1998/namespace\"/>");
        }

        [TestMethod]
        [DataRow("<:e/>", DisplayName = "element name starts with a colon")]
        [DataRow("<e :a=\"1\"/>", DisplayName = "attribute name starts with a colon")]
        [DataRow("<e></:e>", DisplayName = "end tag name starts with a colon")]
        [DataRow("<?a:b d?><e/>", DisplayName = "colon in a processing instruction target")]
        [DataRow("<?:a d?><e/>", DisplayName = "processing instruction target starts with a colon")]
        [DataRow("<?pi!d?><e/>", DisplayName = "no whitespace after a processing instruction target")]
        [DataRow("<?pi?d?><e/>", DisplayName = "question mark glued to a processing instruction target")]
        [DataRow("<?XML version=\"1.0\"?><e/>", DisplayName = "upper-case xml target")]
        [DataRow("<e/><?Xml d?>", DisplayName = "mixed-case xml target after the root")]
        [DataRow("<xmlns:e xmlns:p=\"urn:p\"/>", DisplayName = "element uses the xmlns prefix")]
        [DataRow("<e xmlns:p=\"http://www.w3.org/XML/1998/namespace\"/>", DisplayName = "prefix bound to the xml namespace")]
        [DataRow("<e xmlns:p=\"http://www.w3.org/2000/xmlns/\"/>", DisplayName = "prefix bound to the xmlns namespace")]
        [DataRow("<e xmlns=\"http://www.w3.org/XML/1998/namespace\"/>", DisplayName = "xml namespace as the default")]
        [DataRow("<e xmlns=\"http://www.w3.org/2000/xmlns/\"/>", DisplayName = "xmlns namespace as the default")]
        [DataRow("<e xmlns:xml=\"http://www.w3.org/2000/xmlns/\"/>", DisplayName = "xml prefix bound to the xmlns namespace")]
        [DataRow("<?xml?><e/>", DisplayName = "declaration without a version")]
        [DataRow("<?xml encoding=\"utf-8\"?><e/>", DisplayName = "declaration starts with encoding")]
        [DataRow("<?xml version=\"1.0\" standalone=\"yes\" encoding=\"utf-8\"?><e/>", DisplayName = "declaration out of order")]
        [DataRow("<?xml version=\"1.0\"encoding=\"utf-8\"?><e/>", DisplayName = "declaration without a separating space")]
        [DataRow("<?xml version=\"1.0\" standalone=\"maybe\"?><e/>", DisplayName = "standalone is not yes or no")]
        [DataRow("<?xml version=\"1.0\" standalone=\"YES\"?><e/>", DisplayName = "standalone in upper case")]
        [DataRow("<?xml version=\"2.0\"?><e/>", DisplayName = "version 2.0")]
        [DataRow("<?xml version=\"1\"?><e/>", DisplayName = "version without a minor number")]
        [DataRow("<?xml version=\"1.x\"?><e/>", DisplayName = "version with a non-digit")]
        [DataRow("<?xml version=1.0?><e/>", DisplayName = "unquoted version")]
        [DataRow("<?xml version=\"1.0?><e/>", DisplayName = "unterminated version")]
        [DataRow("<?xml version=\"1.0\" encoding=\"\"?><e/>", DisplayName = "empty encoding")]
        [DataRow("<?xml version=\"1.0\" encoding=\"8utf\"?><e/>", DisplayName = "encoding starts with a digit")]
        [DataRow("<?xml version=\"1.0\" encoding=\"utf 8\"?><e/>", DisplayName = "encoding contains a space")]
        [DataRow("<?xml version=\"1.0\" foo=\"bar\"?><e/>", DisplayName = "unknown pseudo-attribute")]
        [DataRow("<?xml version=\"1.0\" version=\"1.0\"?><e/>", DisplayName = "repeated version")]
        [DataRow("<?xml version=\"1.0\" junk?><e/>", DisplayName = "trailing junk")]
        [DataRow("<?xml version=\"1.1\"\u0085?><e/>", DisplayName = "NEL inside the declaration")]
        [DataRow("<?xml version=\"1.1\"\u2028?><e/>", DisplayName = "LS inside the declaration")]
        public void Reader_Rejects(string xml)
        {
            XmlReaderHarness.MustThrow(xml, new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow });
            XmlDocument.TryParse(Encoding.UTF8.GetBytes(xml), default, out _).Should().BeFalse();
        }

        [TestMethod]
        [DataRow("<?xml version=\"1.0\"?><e/>", "1.0", null)]
        [DataRow("<?xml version='1.0'?><e/>", "1.0", null)]
        [DataRow("<?xml  version = \"1.0\"  ?><e/>", "1.0", null)]
        [DataRow("<?xml version=\"1.1\"?><e/>", "1.1", null)]
        [DataRow("<?xml version=\"1.7\"?><e/>", "1.0", null)]
        [DataRow("<?xml version=\"1.0\" encoding=\"UTF-8\"?><e/>", "1.0", null)]
        [DataRow("<?xml version=\"1.0\" encoding='utf8'?><e/>", "1.0", null)]
        [DataRow("<?xml version=\"1.0\" standalone=\"yes\"?><e/>", "1.0", true)]
        [DataRow("<?xml version=\"1.0\" encoding=\"Utf-8\" standalone='no'?><e/>", "1.0", false)]
        [DataRow("<?xml\tversion=\"1.1\"\r\nencoding=\"utf-8\"\nstandalone=\"yes\"\n?><e/>", "1.1", true)]
        public void Reader_Declaration_IsParsedByTheProduction(string xml, string version, bool? standalone)
        {
            using var document = XmlDocument.Parse(xml);
            document.Declaration!.Value.Version.Should().Be(version);
            document.Declaration.Value.Encoding.Should().Be("utf-8");
            document.Declaration.Value.Standalone.Should().Be(standalone);
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml));
            reader.Read();
            reader.Version.Should().Be(version == "1.1" ? XmlVersion.Version11 : XmlVersion.Version10);
        }

        [TestMethod]
        public void Reader_NameAndValueComparisons()
        {
            var longText = new string('x', 400);
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes("<r xmlns:p=\"urn:p\"><e a=\"v\">" + longText + "</e><p:f/></r>"));
            reader.Read();
            reader.Read();
            reader.LocalNameEquals("e").Should().BeTrue();
            reader.NamespaceUriEquals((string?)null).Should().BeTrue();
            reader.NamespaceUriEquals(string.Empty).Should().BeTrue();
            reader.NamespaceUriEquals("urn:p").Should().BeFalse();
            reader.Read();
            reader.ValueTextEquals(longText).Should().BeTrue();
            reader.ValueTextEquals(longText.AsSpan()).Should().BeTrue();
            reader.ValueTextEquals(longText + "y").Should().BeFalse();
            reader.ValueTextEquals("x").Should().BeFalse();
            reader.ValueTextEquals((string?)null).Should().BeFalse();
            reader.Read();
            reader.Read();
            reader.LocalNameEquals("f").Should().BeTrue();
            reader.NamespaceUriEquals("urn:p").Should().BeTrue();
            reader.NamespaceUriEquals((string?)null).Should().BeFalse();
        }

        [TestMethod]
        public void Reader_EndTag_WithAnotherPrefixOnAWellKnownNamespace()
        {
            XmlReaderHarness.Tokens("<a:e xmlns:a=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:b=\"http://www.w3.org/2001/XMLSchema-instance\"></b:e>"u8, new XmlReaderOptions()).Should().Equal(
                "StartElement a:e{http://www.w3.org/2001/XMLSchema-instance} open @xmlns:a{http://www.w3.org/2000/xmlns/}=[http://www.w3.org/2001/XMLSchema-instance] @xmlns:b{http://www.w3.org/2000/xmlns/}=[http://www.w3.org/2001/XMLSchema-instance]",
                "EndElement b:e{http://www.w3.org/2001/XMLSchema-instance}");
        }

        [TestMethod]
        public void Reader_MultiByteText_AndPlainAttributeValues()
        {
            var delete = ((char)0x7F).ToString();
            var replacement = ((char)0xFFFD).ToString();
            var xml = "<r a=\"é☃\" b='x\"y' c=\"it's\">aé☃𝄞z" + delete + " é" + replacement + "b" + replacement + "</r>";
            XmlReaderHarness.Tokens(Encoding.UTF8.GetBytes(xml), new XmlReaderOptions()).Should().Equal(
                "StartElement :r{} open @:a{}=[é☃] @:b{}=[x\"y] @:c{}=[it's]",
                "Text :{} [aé☃𝄞z" + delete + " é" + replacement + "b" + replacement + "]",
                "EndElement :r{}");
        }

        [TestMethod]
        public void Reader_PrefixedNames_ResolveThroughRepeatedAndShadowedPrefixes()
        {
            const string xmlns = "http://www.w3.org/2000/xmlns/";
            const string xml = "http://www.w3.org/XML/1998/namespace";
            XmlReaderHarness.Tokens("<p:r xmlns:p=\"urn:1\" xmlns:q=\"urn:q\"><p:a p:x=\"1\" q:x=\"2\"><p:b xmlns:p=\"urn:2\" p:x=\"3\"/><p:c p:x=\"4\"/></p:a><xml:d xml:lang=\"en\"/></p:r>"u8, new XmlReaderOptions()).Should().Equal(
                "StartElement p:r{urn:1} open @xmlns:p{" + xmlns + "}=[urn:1] @xmlns:q{" + xmlns + "}=[urn:q]",
                "StartElement p:a{urn:1} open @p:x{urn:1}=[1] @q:x{urn:q}=[2]",
                "StartElement p:b{urn:2} empty @xmlns:p{" + xmlns + "}=[urn:2] @p:x{urn:2}=[3]",
                "StartElement p:c{urn:1} empty @p:x{urn:1}=[4]",
                "EndElement p:a{urn:1}",
                "StartElement xml:d{" + xml + "} empty @xml:lang{" + xml + "}=[en]",
                "EndElement p:r{urn:1}");
        }

        [TestMethod]
        public void Reader_Rejects_IllegalCharactersAmongMultiByteText()
        {
            byte[] invalidUtf8 = [.. "<a>é"u8, 0xFF, .. "</a>"u8];
            byte[] notACharacterFffe = [.. "<a>é"u8, 0xEF, 0xBF, 0xBE, .. "</a>"u8];
            byte[] notACharacterFfff = [.. "<a>é"u8, 0xEF, 0xBF, 0xBF, .. "</a>"u8];
            byte[] controlAfterMultiByte = [.. "<a>é"u8, 0x01, .. "</a>"u8];
            byte[] notACharacterInAttribute = [.. "<a b=\"é"u8, 0xEF, 0xBF, 0xBF, .. "\"/>"u8];
            byte[] deleteAfterRoot = [.. "<a/>"u8, 0x7F];
            byte[] controlAtEndOfInput = [.. "<a/>é"u8, 0x01];
            byte[] truncatedAtEndOfInput = [.. "<a/>"u8, 0xC3];
            byte[] unclosedAfterControl = [.. "<a>é"u8, 0x01];
            byte[] unclosedAfterDelete = [.. "<a>x"u8, 0x7F, (byte)' ', (byte)'y'];
            XmlReaderHarness.MustThrow(invalidUtf8);
            XmlReaderHarness.MustThrow(notACharacterFffe);
            XmlReaderHarness.MustThrow(notACharacterFfff);
            XmlReaderHarness.MustThrow(controlAfterMultiByte);
            XmlReaderHarness.MustThrow(notACharacterInAttribute);
            XmlReaderHarness.MustThrow(deleteAfterRoot);
            XmlReaderHarness.MustThrow(controlAtEndOfInput);
            XmlReaderHarness.MustThrow(truncatedAtEndOfInput);
            XmlReaderHarness.MustThrow(unclosedAfterControl);
            XmlReaderHarness.MustThrow(unclosedAfterDelete);
        }

        [TestMethod]
        public void Reader_Version11_RestrictedCharactersAmongMultiByteText()
        {
            byte[] declaration = [.. "<?xml version=\"1.1\"?>"u8];
            byte[] controlInAttribute = [.. declaration, .. "<e a=\"é"u8, 0xC2, 0x80, .. "\"/>"u8];
            byte[] controlAloneInAttribute = [.. declaration, .. "<e a=\""u8, 0xC2, 0x9F, .. "\"/>"u8];
            byte[] controlInText = [.. declaration, .. "<e>é"u8, 0xC2, 0x80, .. "</e>"u8];
            byte[] notACharacterInAttribute = [.. declaration, .. "<e a=\"é"u8, 0xEF, 0xBF, 0xBE, .. "\"/>"u8];
            XmlReaderHarness.MustThrow(controlInAttribute);
            XmlReaderHarness.MustThrow(controlAloneInAttribute);
            XmlReaderHarness.MustThrow(controlInText);
            XmlReaderHarness.MustThrow(notACharacterInAttribute);

            // NEL and LS are line ends in XML 1.1: a space in an attribute value, a line feed in text,
            // and each one starts a new line for error positions.
            byte[] lineEnds = [.. declaration, .. "<e a=\"é"u8, 0xC2, 0x85, .. "☃"u8, 0xE2, 0x80, 0xA8, .. "ü\">é"u8, 0xC2, 0x85, .. "☃"u8, 0xE2, 0x80, 0xA8, .. "ü</e>"u8];
            XmlReaderHarness.Tokens(lineEnds, new XmlReaderOptions()).Should().Equal(
                "XmlDeclaration :xml{} [version=\"1.1\"]",
                "StartElement :e{} open @:a{}=[é ☃ ü]",
                "Text :{} [é\n☃\nü]",
                "EndElement :e{}");

            byte[] errorOnLineThree = [.. declaration, .. "<e a=\"é"u8, 0xC2, 0x85, .. "☃"u8, 0xE2, 0x80, 0xA8, .. "ü\"><"u8];
            var reader = new Utf8XmlReader(errorOnLineThree);
            XmlException? thrown = null;
            try
            {
                while (reader.Read())
                {
                }
            }
            catch (XmlException exception)
            {
                thrown = exception;
            }

            thrown.Should().NotBeNull();
            thrown!.LineNumber.Should().Be(3);
        }

        [TestMethod]
        public void Reader_Rejects_AttributesThatCollideOrUseUndeclaredPrefixes()
        {
            XmlReaderHarness.MustThrow("<r xmlns:a=\"urn:x\" xmlns:b=\"urn:x\"><e a:x=\"1\" b:x=\"2\"/></r>");
            XmlReaderHarness.MustThrow("<r xmlns:a=\"urn:x\"><e a:x=\"1\" a:x=\"2\"/></r>");
            XmlReaderHarness.MustThrow("<e p:x=\"1\"/>");
            XmlReaderHarness.MustThrow("<r xmlns:p=\"urn:p\"><e p:x=\"1\"/><f q:x=\"1\"/></r>");
            XmlReaderHarness.MustThrow("<?xml version=\"1.1\"?><r xmlns:p=\"urn:p\"><e xmlns:p=\"\" p:x=\"1\"/></r>");
            XmlReaderHarness.MustThrow("<?xml version=\"1.1\"?><r xmlns:p=\"urn:p\"><e xmlns:p=\"\"><p:f/></e></r>");
        }

        [TestMethod]
        public void Reader_ProcessingInstructions_AndNames()
        {
            var options = new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow };
            XmlReaderHarness.Tokens("<?pi?><?q d?><?r  two  words ?><?xml-stylesheet href=\"a\"?><e/>"u8, options).Should().Equal(
                "ProcessingInstruction :pi{} []",
                "ProcessingInstruction :q{} [d]",
                "ProcessingInstruction :r{} [two  words ]",
                "ProcessingInstruction :xml-stylesheet{} [href=\"a\"]",
                "StartElement :e{} empty");
            XmlReaderHarness.Tokens("<e>t<!--c--><![CDATA[d]]></e>"u8, options).Should().Equal(
                "StartElement :e{} open",
                "Text :{} [t]",
                "Comment :{} [c]",
                "CData :{} [d]",
                "EndElement :e{}");
        }

        [TestMethod]
        public void Reader_EscapedAttributes_ShareOneBuffer()
        {
            var item = "<e a=\"x&amp;y\" b=\"p&lt;q\" c=\"1\t2\">t</e>";
            var xml = Encoding.UTF8.GetBytes("<r>" + string.Concat(System.Linq.Enumerable.Repeat(item, 200)) + "</r>");
            DrainAttributes(xml).Should().Be(600);
            var before = GC.GetAllocatedBytesForCurrentThread();
            var count = DrainAttributes(xml);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            count.Should().Be(600);
            allocated.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(512, "400 escaped attribute values must reuse one buffer per reader");

            var wide = "<e a=\"" + new string('&', 0).PadRight(300, 'x') + "&amp;\" b=\"" + new string('y', 400) + "&lt;\" c=\"z&gt;\"/>";
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(wide));
            reader.Read();
            reader.MoveToNextAttribute();
            reader.GetString().Should().Be(new string('x', 300) + "&");
            reader.MoveToNextAttribute();
            reader.GetString().Should().Be(new string('y', 400) + "<");
            reader.MoveToNextAttribute();
            reader.GetString().Should().Be("z>");
            reader.MoveToElement();
            reader.MoveToNextAttribute();
            reader.GetString().Should().Be(new string('x', 300) + "&");
        }

        [TestMethod]
        public void Reader_MalformedInput_OnlyEverThrowsXmlException()
        {
            string[] seeds =
            [
                "<?xml version=\"1.0\"?><p:r xmlns:p=\"urn:p\" a=\"1&amp;2\" p:b='x'><c>t&#65;</c><![CDATA[x]]><?pi d?><!--c--><d xmlns=\"\"/></p:r>",
                "<?xml version=\"1.1\"?><r xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><e xsi:nil=\"true\"/>caf\u00E9<a><b><c/></b></a></r>",
            ];
            var alphabet = Encoding.UTF8.GetBytes("<>/=\"'&;:!?-[] \r\n\tax0#\u00E9\0");
            var random = new Random(12345);
            for (var iteration = 0; iteration < 20_000; iteration++)
            {
                var bytes = new List<byte>(Encoding.UTF8.GetBytes(seeds[iteration % seeds.Length]));
                for (var edit = random.Next(1, 4); edit > 0; edit--)
                {
                    var position = random.Next(bytes.Count);
                    switch (random.Next(4))
                    {
                        case 0:
                            bytes[position] = alphabet[random.Next(alphabet.Length)];
                            break;
                        case 1:
                            bytes.RemoveAt(position);
                            break;
                        case 2:
                            bytes.Insert(position, alphabet[random.Next(alphabet.Length)]);
                            break;
                        default:
                            bytes[position] = (byte)random.Next(256);
                            break;
                    }
                }

                var mutated = bytes.ToArray();
                var split = random.Next(1, mutated.Length);
                var options = new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow, WhitespaceHandling = XmlWhitespaceHandling.Significant };
                List<string>? singlePass = null;
                try
                {
                    singlePass = XmlReaderHarness.Tokens(mutated, options);
                }
                catch (XmlException)
                {
                }

                List<string>? resumed = null;
                try
                {
                    resumed = XmlReaderHarness.TokensSplitAt(mutated, split, options);
                }
                catch (XmlException)
                {
                }

                if (singlePass is null)
                {
                    resumed.Should().BeNull("a document rejected in one pass must be rejected when split at byte {0}: {1}", split, Encoding.UTF8.GetString(mutated));
                    XmlDocument.TryParse(mutated, default, out _).Should().BeFalse();
                    continue;
                }

                resumed.Should().Equal(singlePass, "splitting at byte {0} must not change the tokens of {1}", split, Encoding.UTF8.GetString(mutated));
                var rewritten = XmlFixtureDocumentTests.WriteThroughDocument(mutated, mutated.AsSpan().StartsWith("<?xml version=\"1.1\""u8) ? XmlVersion.Version11 : XmlVersion.Version10);
                XmlFixtureDocumentTests.WriteThroughDocument(rewritten, rewritten.AsSpan().StartsWith("<?xml version=\"1.1\""u8) ? XmlVersion.Version11 : XmlVersion.Version10)
                    .AsSpan().SequenceEqual(rewritten).Should().BeTrue("a rewritten document must be stable: {0}", Encoding.UTF8.GetString(mutated));
            }
        }

        #endregion

        #region Private Methods

        private static string Compose(Random random, List<string> fragments)
        {
            var builder = new StringBuilder();
            for (var count = random.Next(0, 9); count > 0; count--)
            {
                builder.Append(fragments[random.Next(fragments.Count)]);
            }

            return builder.ToString();
        }

        private static int DrainAttributes(byte[] xml)
        {
            var reader = new Utf8XmlReader(xml);
            var count = 0;
            while (reader.Read())
            {
                while (reader.MoveToNextAttribute())
                {
                    count += reader.ValueSpan.Length > 0 ? 1 : 0;
                }
            }

            return count;
        }

        private static string ReadElementText(ref Utf8XmlReader reader)
        {
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.StartElement);
            if (reader.IsEmptyElement)
            {
                return string.Empty;
            }

            reader.Read().Should().BeTrue();
            if (reader.TokenType is XmlTokenType.EndElement)
            {
                return string.Empty;
            }

            var text = reader.GetString()!;
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            return text;
        }

        private static string Rewrite(string xml, XmlVersion version = XmlVersion.Version10)
        {
            return Encoding.UTF8.GetString(XmlFixtureDocumentTests.WriteThroughDocument(Encoding.UTF8.GetBytes(xml), version));
        }

        #endregion

    }

}
