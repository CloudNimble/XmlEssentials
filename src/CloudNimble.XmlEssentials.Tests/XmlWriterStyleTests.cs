using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// The settings that say how output is laid out: <see cref="XmlWriterOptions.EmptyElementStyle"/>,
    /// <see cref="XmlWriterOptions.AttributeStyle"/> and <see cref="XmlWriterOptions.NewLine"/>.
    /// </summary>
    /// <remarks>
    /// Each one is a rule the writer follows for every element, whichever code asked for the element: a caller
    /// of the writer, the serializer, a document or a node. So what is read and written again changes only in
    /// the ways the settings name, and always in the same way.
    /// </remarks>
    [TestClass]
    public class XmlWriterStyleTests
    {

        #region Fields

        private const string Nil = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"";

        #endregion

        #region Public Methods

        [TestMethod]
        [DataRow(XmlEmptyElementStyle.SelfClosing, "/>", DisplayName = "one tag")]
        [DataRow(XmlEmptyElementStyle.SelfClosingWithSpace, " />", DisplayName = "one tag with a space before the slash")]
        public void EmptyElementStyle_OneTag_IsWrittenByEveryRoute(XmlEmptyElementStyle style, string close)
        {
            var options = new XmlWriterOptions { EmptyElementStyle = style, OmitXmlDeclaration = true };
            var expected =
                "<r a=\"1\"><e1" + close + "<e2 xmlns=\"urn:n\"" + close + "<p:e3 xmlns:p=\"urn:p\"" + close + "<e4" + close + "<e5 b=\"2\"" + close +
                "<e6" + Nil + close + "<e7" + close + "<e8></e8><e9>t</e9></r>";
            XmlWriterHarness.ToXml(WriteEveryEmptyElement, options).Should().Be(expected);

            // A document and a node write an element that had an end tag with its end tag, and every other one
            // in the form that was asked for, whatever form it was read in.
            const string source = "<r a=\"1\"><e1 /><e2 xmlns=\"urn:n\"/><p:e3 xmlns:p=\"urn:p\"/><e4/><e5 b=\"2\"   /><e6" + Nil + "/><e7/><e8></e8><e9>t</e9></r>";
            using var document = XmlDocument.Parse(source);
            XmlWriterHarness.ToXml(document.WriteTo, options).Should().Be(expected);
            XmlWriterHarness.ToXml(XmlNode.Parse(source).WriteTo, options).Should().Be(expected);
        }

        [TestMethod]
        public void EmptyElementStyle_StartAndEndTag_IsWrittenByEveryRoute()
        {
            var options = new XmlWriterOptions { EmptyElementStyle = XmlEmptyElementStyle.StartAndEndTag, OmitXmlDeclaration = true };
            const string expected =
                "<r a=\"1\"><e1></e1><e2 xmlns=\"urn:n\"></e2><p:e3 xmlns:p=\"urn:p\"></p:e3><e4></e4><e5 b=\"2\"></e5>" +
                "<e6" + Nil + "></e6><e7></e7><e8></e8><e9>t</e9></r>";
            XmlWriterHarness.ToXml(WriteEveryEmptyElement, options).Should().Be(expected);

            const string source = "<r a=\"1\"><e1/><e2 xmlns=\"urn:n\"/><p:e3 xmlns:p=\"urn:p\"/><e4/><e5 b=\"2\"/><e6" + Nil + "/><e7/><e8></e8><e9>t</e9></r>";
            using var document = XmlDocument.Parse(source);
            XmlWriterHarness.ToXml(document.WriteTo, options).Should().Be(expected);
            XmlWriterHarness.ToXml(XmlNode.Parse(source).WriteTo, options).Should().Be(expected);

            // Indentation puts nothing between the two tags: white space there would be content.
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("a");
                writer.WriteStartElement("b");
                writer.WriteEndElement();
                writer.WriteEmptyElement("c");
                writer.WriteEndElement();
            }, options with { Indented = true, NewLine = "\n" }).Should().Be("<a>\n  <b></b>\n  <c></c>\n</a>");
        }

        [TestMethod]
        [DataRow(XmlEmptyElementStyle.SelfClosing, DisplayName = "one tag")]
        [DataRow(XmlEmptyElementStyle.SelfClosingWithSpace, DisplayName = "one tag with a space before the slash")]
        [DataRow(XmlEmptyElementStyle.StartAndEndTag, DisplayName = "a start tag and an end tag")]
        public void EmptyElementStyle_DoesNotApplyToAnEmptyString(XmlEmptyElementStyle style)
        {
            // An empty string is content: a value of no characters. It is written with an end tag under every
            // style, which keeps it apart from an element that was given nothing and from one that is nil.
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("r");
                writer.WriteElementString("a", "");
                writer.WriteStartElement("b");
                writer.WriteStringValue("");
                writer.WriteEndElement();
                writer.WriteStartElement("c");
                writer.WriteStringValue(""u8);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }, new XmlWriterOptions { EmptyElementStyle = style, OmitXmlDeclaration = true }).Should().Be("<r><a></a><b></b><c></c></r>");

            var options = new XmlSerializerOptions { EmptyElementStyle = style, OmitXmlDeclaration = true };
            var xml = XmlSerializer.Serialize(new Dictionary<string, string> { ["k"] = "" }, options);
            xml.Should().Be("<DictionaryOfStringString><k></k></DictionaryOfStringString>");
            XmlSerializer.Deserialize<Dictionary<string, string>>(xml, options)!["k"].Should().Be("");
        }

        [TestMethod]
        public void EmptyElementStyle_DefaultsToOneTag()
        {
            default(XmlWriterOptions).EmptyElementStyle.Should().Be(XmlEmptyElementStyle.SelfClosing);
            new XmlWriterOptions().EmptyElementStyle.Should().Be(XmlEmptyElementStyle.SelfClosing);
            new XmlSerializerOptions().EmptyElementStyle.Should().Be(XmlEmptyElementStyle.SelfClosing);
            ((int)XmlEmptyElementStyle.SelfClosing).Should().Be(0);
            ((int)XmlEmptyElementStyle.SelfClosingWithSpace).Should().Be(1);
            ((int)XmlEmptyElementStyle.StartAndEndTag).Should().Be(2);
        }

        [TestMethod]
        [DataRow(XmlEmptyElementStyle.SelfClosing, "<StyledOrder Code=\"c\" Id=\"7\" Region=\"r\"><Lines/><Note" + Nil + "/></StyledOrder>", DisplayName = "one tag")]
        [DataRow(XmlEmptyElementStyle.SelfClosingWithSpace, "<StyledOrder Code=\"c\" Id=\"7\" Region=\"r\"><Lines /><Note" + Nil + " /></StyledOrder>", DisplayName = "one tag with a space before the slash")]
        [DataRow(XmlEmptyElementStyle.StartAndEndTag, "<StyledOrder Code=\"c\" Id=\"7\" Region=\"r\"><Lines></Lines><Note" + Nil + "></Note></StyledOrder>", DisplayName = "a start tag and an end tag")]
        public void EmptyElementStyle_IsFollowedByTheSerializer(XmlEmptyElementStyle style, string expected)
        {
            var options = new XmlSerializerOptions { EmptyElementStyle = style, OmitXmlDeclaration = true };
            var xml = XmlSerializer.Serialize(new StyledOrder { Code = "c", Id = 7, Region = "r" }, options);
            xml.Should().Be(expected);

            // The three forms are one document to a reader.
            var read = XmlSerializer.Deserialize<StyledOrder>(xml, options)!;
            read.Code.Should().Be("c");
            read.Id.Should().Be(7);
            read.Region.Should().Be("r");
            read.Note.Should().BeNull();
            read.Lines.Should().BeEmpty();
            new XmlSerializerOptions(options).EmptyElementStyle.Should().Be(style);
        }

        [TestMethod]
        public void AttributeStyle_PerLine_PutsEachAttributeUnderTheFirst()
        {
            var options = new XmlWriterOptions { AttributeStyle = XmlAttributeStyle.PerLine, Indented = true, NewLine = "\n", OmitXmlDeclaration = true };
            XmlWriterHarness.ToXml(WriteOrder, options).Should().Be(
                "<order a=\"1\"\n" +
                "       b=\"2\"\n" +
                "       c=\"3\">\n" +
                "  <line x=\"1\"\n" +
                "        y=\"2\"/>\n" +
                "  <p:note xmlns:p=\"urn:p\"\n" +
                "          lang=\"en\">n</p:note>\n" +
                "  <nil" + " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"\n" +
                "       xsi:nil=\"true\"/>\n" +
                "  <one z=\"1\"/>\n" +
                "</order>");

            // The white space that starts the line is repeated as it is, so a tab stays a tab.
            XmlWriterHarness.ToXml(WriteOrder, options with { IndentCharacter = '\t', IndentSize = 1 }).Should().Be(
                "<order a=\"1\"\n" +
                "       b=\"2\"\n" +
                "       c=\"3\">\n" +
                "\t<line x=\"1\"\n" +
                "\t      y=\"2\"/>\n" +
                "\t<p:note xmlns:p=\"urn:p\"\n" +
                "\t        lang=\"en\">n</p:note>\n" +
                "\t<nil" + " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"\n" +
                "\t     xsi:nil=\"true\"/>\n" +
                "\t<one z=\"1\"/>\n" +
                "</order>");
        }

        [TestMethod]
        public void AttributeStyle_PerLine_WritesTheNumberOfAttributesItIsGiven()
        {
            var options = new XmlWriterOptions { AttributeStyle = XmlAttributeStyle.PerLine, AttributesPerLine = 2, Indented = true, NewLine = "\r\n", OmitXmlDeclaration = true };
            XmlWriterHarness.ToXml(WriteOrder, options).Should().Be(
                "<order a=\"1\" b=\"2\"\r\n" +
                "       c=\"3\">\r\n" +
                "  <line x=\"1\" y=\"2\"/>\r\n" +
                "  <p:note xmlns:p=\"urn:p\" lang=\"en\">n</p:note>\r\n" +
                "  <nil" + Nil + "/>\r\n" +
                "  <one z=\"1\"/>\r\n" +
                "</order>");

            // Zero is what default(XmlWriterOptions) holds, and it means one to a line.
            var one = default(XmlWriterOptions) with { AttributeStyle = XmlAttributeStyle.PerLine, NewLine = "\n", OmitXmlDeclaration = true };
            one.AttributesPerLine.Should().Be(0);
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("e");
                writer.WriteAttributeString("a", "1");
                writer.WriteAttributeNumber("b", 2);
                writer.WriteAttributeString("c"u8, "3"u8);
                writer.WriteEndElement();
            }, one).Should().Be("<e a=\"1\"\n   b=\"2\"\n   c=\"3\"/>");
        }

        [TestMethod]
        public void AttributeStyle_Inline_IsTheDefault_AndDoesNotReadTheNumber()
        {
            default(XmlWriterOptions).AttributeStyle.Should().Be(XmlAttributeStyle.Inline);
            new XmlWriterOptions().AttributeStyle.Should().Be(XmlAttributeStyle.Inline);
            new XmlSerializerOptions().AttributeStyle.Should().Be(XmlAttributeStyle.Inline);
            new XmlSerializerOptions().AttributesPerLine.Should().Be(1);
            ((int)XmlAttributeStyle.Inline).Should().Be(0);
            ((int)XmlAttributeStyle.PerLine).Should().Be(1);

            var options = new XmlWriterOptions { AttributesPerLine = 1, Indented = true, NewLine = "\n", OmitXmlDeclaration = true };
            XmlWriterHarness.ToXml(WriteOrder, options).Should().Be(
                "<order a=\"1\" b=\"2\" c=\"3\">\n" +
                "  <line x=\"1\" y=\"2\"/>\n" +
                "  <p:note xmlns:p=\"urn:p\" lang=\"en\">n</p:note>\n" +
                "  <nil" + Nil + "/>\n" +
                "  <one z=\"1\"/>\n" +
                "</order>");
        }

        [TestMethod]
        public void AttributeStyle_PerLine_WithoutIndentation_UsesTheLineTheTagIsOn()
        {
            // A line after the first starts with the white space that starts the line of the tag, and then a
            // space for each character of the tag up to its first attribute. An element in the middle of a line
            // has no column of its own to keep, and the rule is the same for it.
            var options = new XmlWriterOptions { AttributeStyle = XmlAttributeStyle.PerLine, NewLine = "\n", OmitXmlDeclaration = true };
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("r");
                writer.WriteStringValue("\n\t  ");
                writer.WriteStartElement("e");
                writer.WriteAttributeString("a", "1");
                writer.WriteAttributeString("b", "2");
                writer.WriteStartElement("été");
                writer.WriteAttributeString("c", "3");
                writer.WriteAttributeString("d", "4");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
            }, options).Should().Be(
                "<r>\n" +
                "\t  <e a=\"1\"\n" +
                "\t     b=\"2\"><été c=\"3\"\n" +
                "\t          d=\"4\"/></e></r>");

            // Three tags on a line that starts with none of them and with no white space.
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("r");
                writer.WriteStartElement("e");
                writer.WriteAttributeString("a", "1");
                writer.WriteStartElement("f");
                writer.WriteAttributeString("c", "3");
                writer.WriteAttributeString("d", "4");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
            }, options).Should().Be("<r><e a=\"1\"><f c=\"3\"\n   d=\"4\"/></e></r>");
        }

        [TestMethod]
        public void AttributeStyle_PerLine_KeepsTheLineAcrossAFlush()
        {
            var options = new XmlWriterOptions { AttributeStyle = XmlAttributeStyle.PerLine, NewLine = "\n", OmitXmlDeclaration = true };

            // More white space than the buffer holds, so the line starts in output the writer has handed on.
            var wide = new string(' ', 9000);
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("r");
                writer.WriteStringValue("\n" + wide);
                writer.WriteStartElement("e");
                writer.WriteAttributeString("a", "1");
                writer.WriteAttributeString("b", "2");
                writer.WriteEndElement();
                writer.WriteEndElement();
            }, options).Should().Be("<r>\n" + wide + "<e a=\"1\"\n" + wide + "   b=\"2\"/></r>");

            // More text than the buffer holds, on a line that starts with two blanks.
            var text = new string('x', 9000);
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("r");
                writer.WriteStringValue("\n  " + text);
                writer.WriteStartElement("e");
                writer.WriteAttributeString("a", "1");
                writer.WriteAttributeString("b", "2");
                writer.WriteEndElement();
                writer.WriteEndElement();
            }, options).Should().Be("<r>\n  " + text + "<e a=\"1\"\n     b=\"2\"/></r>");
        }

        [TestMethod]
        public async Task AttributeStyle_PerLine_KeepsTheLineAcrossAFlushTheCallerAsksFor()
        {
            var options = new XmlWriterOptions { AttributeStyle = XmlAttributeStyle.PerLine, NewLine = "\n", OmitXmlDeclaration = true };
            using var stream = new MemoryStream();
            await using (var writer = new Utf8XmlWriter(stream, options))
            {
                writer.WriteStartElement("r");
                writer.WriteStringValue("\n    ");
                await writer.FlushAsync();
                writer.WriteStartElement("e");
                writer.WriteAttributeString("a", "1");
                writer.Flush();
                writer.WriteAttributeString("b", "2");
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            Encoding.UTF8.GetString(stream.ToArray()).Should().Be("<r>\n    <e a=\"1\"\n       b=\"2\"/></r>");

            // A writer that is used again starts a new document, and a new first line.
            var buffer = new ArrayBufferWriter<byte>();
            using var reused = new Utf8XmlWriter(buffer, options);
            reused.WriteStartElement("r");
            reused.WriteStringValue("\n      ");
            reused.Flush();
            buffer.ResetWrittenCount();
            reused.Reset();
            reused.WriteStartElement("e");
            reused.WriteAttributeString("a", "1");
            reused.WriteAttributeString("b", "2");
            reused.WriteEndElement();
            reused.Flush();
            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<e a=\"1\"\n   b=\"2\"/>");
        }

        [TestMethod]
        public void AttributeStyle_IsFollowedByTheSerializer_ADocumentAndANode()
        {
            const string expected =
                "<StyledOrder Code=\"c\"\n" +
                "             Id=\"7\"\n" +
                "             Region=\"r\">\n" +
                "  <Lines>\n" +
                "    <string>l</string>\n" +
                "  </Lines>\n" +
                "  <Note>n</Note>\n" +
                "</StyledOrder>";
            var options = new XmlSerializerOptions { AttributeStyle = XmlAttributeStyle.PerLine, NewLine = "\n", OmitXmlDeclaration = true, WriteIndented = true };
            var xml = XmlSerializer.Serialize(new StyledOrder { Code = "c", Id = 7, Region = "r", Note = "n", Lines = ["l"] }, options);
            xml.Should().Be(expected);
            XmlSerializer.Deserialize<StyledOrder>(xml, options)!.Region.Should().Be("r");

            var copy = new XmlSerializerOptions(options) { AttributesPerLine = 2 };
            copy.AttributeStyle.Should().Be(XmlAttributeStyle.PerLine);
            XmlSerializer.Serialize(new StyledOrder { Code = "c", Id = 7, Region = "r" }, copy).Should().Be(
                "<StyledOrder Code=\"c\" Id=\"7\"\n" +
                "             Region=\"r\">\n" +
                "  <Lines/>\n" +
                "  <Note" + " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>\n" +
                "</StyledOrder>");
            new XmlSerializerOptions(copy).AttributesPerLine.Should().Be(2);

            // A document and a node keep the white space between elements that they read, so the writer is
            // not asked to indent: the layout of the attributes is the only thing that is set.
            var layout = new XmlWriterOptions { AttributeStyle = XmlAttributeStyle.PerLine, NewLine = "\n", OmitXmlDeclaration = true };
            const string inline = "<StyledOrder Code=\"c\" Id=\"7\" Region=\"r\">\n  <Lines>\n    <string>l</string>\n  </Lines>\n  <Note>n</Note>\n</StyledOrder>";
            using var document = XmlDocument.Parse(inline);
            XmlWriterHarness.ToXml(document.WriteTo, layout).Should().Be(expected);
            XmlWriterHarness.ToXml(XmlNode.Parse(inline).WriteTo, layout).Should().Be(expected);
        }

        [TestMethod]
        public void AttributesPerLine_ThatIsNotACount_IsRejected()
        {
            FluentActions.Invoking(() => new XmlWriterOptions { AttributesPerLine = -1 })
                .Should().ThrowExactly<ArgumentOutOfRangeException>().Which.Message.Should().Be("value ('-1') must be a non-negative value. (Parameter 'value')\r\nActual value was -1.".Replace("\r\n", Environment.NewLine));
            FluentActions.Invoking(() => new XmlSerializerOptions { AttributesPerLine = 0 })
                .Should().ThrowExactly<ArgumentOutOfRangeException>().Which.Message.Should().Be("value ('0') must be a non-negative and non-zero value. (Parameter 'value')\r\nActual value was 0.".Replace("\r\n", Environment.NewLine));
        }

        [TestMethod]
        [DataRow(XmlVersion.Version10, DisplayName = "XML 1.0")]
        [DataRow(XmlVersion.Version11, DisplayName = "XML 1.1")]
        public void NewLine_IsWrittenForEveryLineBreak(XmlVersion version)
        {
            var crlf = new XmlWriterOptions { NewLine = "\r\n", OmitXmlDeclaration = true, Version = version };
            var written = XmlWriterHarness.ToXml(WriteLineBreaks, crlf);
            written.Should().Be(
                "<r a=\"x&#xA;y\">one\r\ntwo<!--c1\r\nc2--><?pi d1\r\nd2?><![CDATA[e1\r\ne2]]>" +
                "<s>a &amp; b\r\nc</s><t>u8\r\nv</t><u>w\r\n]]&gt;x</u><v>cr&#xD;\r\nlf</v></r>");

            var lf = crlf with { NewLine = "\n" };
            XmlWriterHarness.ToXml(WriteLineBreaks, lf).Should().Be(
                "<r a=\"x&#xA;y\">one\ntwo<!--c1\nc2--><?pi d1\nd2?><![CDATA[e1\ne2]]>" +
                "<s>a &amp; b\nc</s><t>u8\nv</t><u>w\n]]&gt;x</u><v>cr&#xD;\nlf</v></r>");

            // Not one value is different. A reader gives a carriage return and a line feed back as a line feed.
            using var document = XmlDocument.Parse(written);
            XmlWriterHarness.ToXml(document.WriteTo, lf).Should().Be(XmlWriterHarness.ToXml(WriteLineBreaks, lf));
            XmlWriterHarness.ToXml(document.WriteTo, crlf).Should().Be(written);
            XmlWriterHarness.ToXml(XmlNode.Parse(written).WriteTo, crlf).Should().Be(written);
            document.RootElement.GetChild("v").GetString().Should().Be("cr\r\nlf");
            document.RootElement.GetAttribute("a").GetString().Should().Be("x\ny");
        }

        [TestMethod]
        public void NewLine_ACallerWroteAlready_IsNotWrittenTwice()
        {
            // A comment, a CDATA section and a processing instruction are written as they are given, so a
            // carriage return and a line feed in one is the line ending already.
            var crlf = new XmlWriterOptions { NewLine = "\r\n", OmitXmlDeclaration = true };
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("r");
                writer.WriteComment("a\r\nb\nc");
                writer.WriteEndElement();
            }, crlf).Should().Be("<r><!--a\r\nb\r\nc--></r>");
        }

        [TestMethod]
        public void NewLine_IsFollowedByTheSerializer()
        {
            var value = new StyledOrder { Code = "c", Id = 7, Region = "r", Note = "one\ntwo", Lines = ["a\nb"] };
            var map = new Dictionary<string, string> { ["k"] = "x\ny" };
            foreach (var newLine in new[] { "\n", "\r\n" })
            {
                var options = new XmlSerializerOptions { NewLine = newLine, OmitXmlDeclaration = true };
                var xml = XmlSerializer.Serialize(value, options);
                xml.Should().Be("<StyledOrder Code=\"c\" Id=\"7\" Region=\"r\"><Lines><string>a" + newLine + "b</string></Lines><Note>one" + newLine + "two</Note></StyledOrder>");
                var read = XmlSerializer.Deserialize<StyledOrder>(xml, options)!;
                read.Note.Should().Be("one\ntwo");
                read.Lines.Should().Equal("a\nb");

                var entries = XmlSerializer.Serialize(map, options);
                entries.Should().Be("<DictionaryOfStringString><k>x" + newLine + "y</k></DictionaryOfStringString>");
                XmlSerializer.Deserialize<Dictionary<string, string>>(entries, options)!["k"].Should().Be("x\ny");
            }
        }

        [TestMethod]
        public void NewLine_OfTheEnvironment_IsTheDefault()
        {
            new XmlWriterOptions().NewLine.Should().Be(Environment.NewLine);
            default(XmlWriterOptions).NewLine.Should().Be(Environment.NewLine);
            new XmlSerializerOptions().NewLine.Should().Be(Environment.NewLine);
            foreach (var options in new[] { default, new XmlWriterOptions() })
            {
                XmlWriterHarness.ToXml(writer =>
                {
                    writer.WriteStartElement("r");
                    writer.WriteStringValue("a\nb");
                    writer.WriteEndElement();
                }, options with { OmitXmlDeclaration = true }).Should().Be("<r>a" + Environment.NewLine + "b</r>");
            }

            // A method that returns a string has no options to take a line ending from, so it gives the line
            // feed the document holds, on every platform.
            using var document = XmlDocument.Parse("<r>a\r\nb</r>");
            document.RootElement.GetRawXml().Should().Be("<r>a\nb</r>");
            XmlNode.Parse("<r>a\r\nb</r>").ToString().Should().Be("<r>a\nb</r>");
        }

        [TestMethod]
        public void IsEmptyElement_OfANode_IsTheFormItsSourceHad()
        {
            var root = (XmlElementNode)XmlNode.Parse("<r><a/><b></b><c>t</c></r>");
            var a = root["a"]!;
            var b = root["b"]!;
            var c = root["c"]!;
            a.IsEmptyElement.Should().BeTrue();
            b.IsEmptyElement.Should().BeFalse();
            c.IsEmptyElement.Should().BeFalse();
            root.ToString().Should().Be("<r><a/><b></b><c>t</c></r>");
            ((XmlElementNode)root.DeepClone()).ToString().Should().Be("<r><a/><b></b><c>t</c></r>");

            // The value is not read while the element has children, and it is there when they are gone.
            c.InnerText = "";
            a.Add(new XmlTextNode("x"));
            b.IsEmptyElement = true;
            root.ToString().Should().Be("<r><a>x</a><b/><c></c></r>");

            var made = new XmlElementNode("m");
            made.IsEmptyElement.Should().BeTrue();
            made.ToString().Should().Be("<m/>");
            made.IsEmptyElement = false;
            made.ToString().Should().Be("<m></m>");
            made.AddElement("n").IsEmptyElement.Should().BeTrue();
            made.ToString().Should().Be("<m><n/></m>");
        }

        [TestMethod]
        public void ProjectFile_ChangedInOnePlace_IsWrittenBackWithOnlyThatChange()
        {
            const string project =
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
                "\n" +
                "\t<PropertyGroup>\n" +
                "\t\t<TargetFrameworks>net10.0;net11.0</TargetFrameworks>\n" +
                "\t\t<Description></Description>\n" +
                "\t</PropertyGroup>\n" +
                "\n" +
                "\t<!-- What the project depends on. -->\n" +
                "\t<ItemGroup>\n" +
                "\t\t<PackageReference Include=\"FluentAssertions\" Version=\"8.0.0\" />\n" +
                "\t\t<ProjectReference Include=\"..\\Library\\Library.csproj\" />\n" +
                "\t</ItemGroup>\n" +
                "\n" +
                "</Project>";
            var options = new XmlWriterOptions { EmptyElementStyle = XmlEmptyElementStyle.SelfClosingWithSpace, NewLine = "\n", OmitXmlDeclaration = true };

            var root = (XmlElementNode)XmlNode.Parse(project);
            XmlWriterHarness.ToXml(root.WriteTo, options).Should().Be(project);

            var package = root["ItemGroup"]!["PackageReference"]!;
            package.SetAttribute("Version", "8.1.0");
            XmlWriterHarness.ToXml(root.WriteTo, options).Should().Be(project.Replace("Version=\"8.0.0\"", "Version=\"8.1.0\""));

            // The same file with the line endings of another platform and attributes one to a line. Every
            // change is one the options name.
            XmlWriterHarness.ToXml(root.WriteTo, options with { AttributeStyle = XmlAttributeStyle.PerLine, NewLine = "\r\n" }).Should().Be(
                project.Replace("Version=\"8.0.0\"", "Version=\"8.1.0\"")
                    .Replace("<PackageReference Include=\"FluentAssertions\" Version", "<PackageReference Include=\"FluentAssertions\"\n\t\t                  Version")
                    .Replace("\n", "\r\n"));

            using var document = XmlDocument.Parse(project);
            XmlWriterHarness.ToXml(document.WriteTo, options).Should().Be(project);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Writes an element with no content in every way the writer has, and two elements that are not such
        /// an element: one written with <see cref="Utf8XmlWriter.WriteFullEndElement"/>, and one with text.
        /// </summary>
        /// <param name="writer">The writer.</param>
        private static void WriteEveryEmptyElement(Utf8XmlWriter writer)
        {
            writer.WriteStartElement("r");
            writer.WriteAttributeString("a", "1");
            writer.WriteEmptyElement("e1");
            writer.WriteEmptyElement("e2", "urn:n");
            writer.WriteEmptyElement("p", "e3", "urn:p");
            writer.WriteEmptyElement("e4"u8);
            writer.WriteStartElement("e5");
            writer.WriteAttributeString("b", "2");
            writer.WriteEndElement();
            writer.WriteStartElement("e6");
            writer.WriteNilValue();
            writer.WriteEndElement();
            writer.WriteElementString("e7", (string?)null);
            writer.WriteStartElement("e8");
            writer.WriteFullEndElement();
            writer.WriteStartElement("e9");
            writer.WriteStringValue("t");
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes a line break in every place a document can hold one.
        /// </summary>
        /// <param name="writer">The writer.</param>
        private static void WriteLineBreaks(Utf8XmlWriter writer)
        {
            writer.WriteStartElement("r");
            writer.WriteAttributeString("a", "x\ny");
            writer.WriteStringValue("one\ntwo");
            writer.WriteComment("c1\nc2");
            writer.WriteProcessingInstruction("pi", "d1\nd2");
            writer.WriteCData("e1\ne2");
            writer.WriteElementString("s", "a & b\nc");
            writer.WriteStartElement("t");
            writer.WriteStringValue("u8\nv"u8);
            writer.WriteEndElement();
            writer.WriteStartElement("u");
            writer.WriteStringValue("w\n]]>x"u8);
            writer.WriteEndElement();
            writer.WriteStartElement("v");
            writer.WriteStringValue("cr\r\nlf");
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes start tags with three attributes, two, a namespace declaration, <c>xsi:nil</c>, and one.
        /// </summary>
        /// <param name="writer">The writer.</param>
        private static void WriteOrder(Utf8XmlWriter writer)
        {
            writer.WriteStartElement("order");
            writer.WriteAttributeString("a", "1");
            writer.WriteAttributeString("b", "2");
            writer.WriteAttributeString("c", "3");
            writer.WriteStartElement("line");
            writer.WriteAttributeString("x", "1");
            writer.WriteAttributeString("y", "2");
            writer.WriteEndElement();
            writer.WriteStartElement("p", "note", "urn:p");
            writer.WriteAttributeString("lang", "en");
            writer.WriteStringValue("n");
            writer.WriteEndElement();
            writer.WriteStartElement("nil");
            writer.WriteNilValue();
            writer.WriteEndElement();
            writer.WriteStartElement("one");
            writer.WriteAttributeString("z", "1");
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        #endregion

    }

}
