using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Internals coverage for <see cref="XmlDocument"/>.
    /// </summary>
    [TestClass]
    public class XmlDocumentInternalsTests
    {

        #region Public Methods

        [TestMethod]
        public void FillFromReader_EmptyInput_WhenARootIsRequired_Throws()
        {
            using var document = new XmlDocument();
            var reader = new Utf8XmlReader(""u8, new XmlReaderOptions { AllowMultipleRoots = true });
            string? message = null;
            try
            {
                document.FillFromReader(ref reader, requireDocumentRoot: true);
            }
            catch (XmlException ex)
            {
                message = ex.Message;
            }

            message.Should().Be("The document has no root element.");
        }

        [TestMethod]
        public void FillFromReader_TwoRoots_WhenARootIsRequired_Throws()
        {
            using var document = new XmlDocument();
            var reader = new Utf8XmlReader("<a/><b/>"u8, new XmlReaderOptions { AllowMultipleRoots = true });
            string? message = null;
            try
            {
                document.FillFromReader(ref reader, requireDocumentRoot: true);
            }
            catch (XmlException ex)
            {
                message = ex.Message;
            }

            message.Should().Be("Multiple root elements are not allowed.");
        }

        [TestMethod]
        public void TokenType_UnknownKind_IsNone()
        {
            using var document = XmlDocument.Parse("<e/>");
            // 1 is a value the enumeration does not name.
            var index = document.AddRow(new XmlDbRow { Kind = (XmlValueKind)1 });
            new XmlElement(document, index).TokenType.Should().Be(XmlTokenType.None);
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true }))
            {
                document.WriteTo(writer);
            }

            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<e/>");
        }

        [TestMethod]
        public void Locate_PayloadSlice_IsNotCopied()
        {
            using var document = XmlDocument.Parse("<e a=\"1\">&amp;x</e>");
            var root = document.Row(0);
            root.NameStart.Should().Be(1);
            document.Row(1).ValueStart.Should().Be(6);
            document.Row(2).ValueStart.Should().BeGreaterThanOrEqualTo(18);
            document.RootElement.GetString().Should().Be("&x");
        }

        [TestMethod]
        public void LocateNamespace_RepeatedInternedUri_IsCopiedOnce()
        {
            var reader = new Utf8XmlReader("<r xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:a=\"1\" xsi:b=\"2\"><p:c xmlns:p=\"urn:c\"/></r>"u8);
            using var document = XmlDocument.ParseValue(ref reader);
            document.Row(2).NsStart.Should().Be(document.Row(3).NsStart);
            var root = document.RootElement;
            root.GetAttribute("a", XmlNamespaces.Xsi).GetInt32().Should().Be(1);
            root.GetAttribute("b", XmlNamespaces.Xsi).GetInt32().Should().Be(2);
            root.GetChild("c", "urn:c").NamespaceUri.Should().Be("urn:c");
        }

        [TestMethod]
        public void Parse_Sequence()
        {
            var seq = new ReadOnlySequence<byte>("<e/>"u8.ToArray());
            using var document = XmlDocument.Parse(seq);
            document.RootElement.IsEmptyElement.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_Version11()
        {
            using var document = XmlDocument.Parse("<?xml version=\"1.1\" encoding=\"utf-8\"?><e/>");
            document.Declaration!.Value.Version.Should().Be("1.1");
        }

        [TestMethod]
        public void GetAttribute_ByNamespace()
        {
            using var document = XmlDocument.Parse("<e xmlns:p=\"urn:x\" p:a=\"1\"/>");
            document.RootElement.GetAttribute("a", "urn:x").GetString().Should().Be("1");
        }

        [TestMethod]
        public void GetDouble_NamedLiterals()
        {
            using var nan = XmlDocument.Parse("<e>NaN</e>");
            double.IsNaN(nan.RootElement.GetDouble()).Should().BeTrue();
            using var inf = XmlDocument.Parse("<e>INF</e>");
            double.IsPositiveInfinity(inf.RootElement.GetDouble()).Should().BeTrue();
            using var ninf = XmlDocument.Parse("<e>-INF</e>");
            double.IsNegativeInfinity(ninf.RootElement.GetDouble()).Should().BeTrue();
        }

        [TestMethod]
        public void Dispose_Twice_LeavesTheDocumentDisposed()
        {
            var document = XmlDocument.Parse("<e/>");
            document.Dispose();
            document.Dispose();
            FluentActions.Invoking(() => document.RootElement).Should().ThrowExactly<ObjectDisposedException>();
        }

        [TestMethod]
        public void Parse_StreamGrowsAndCaps()
        {
            var xml = "<e>" + new string('x', 20_000) + "</e>";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            using var document = XmlDocument.Parse(stream);
            document.RootElement.GetInnerText().Length.Should().Be(20_000);

            var threw = false;
            try
            {
                using var small = new MemoryStream(Encoding.UTF8.GetBytes(xml));
                XmlDocument.Parse(small, new XmlDocumentOptions { MaxDocumentByteCount = 100 });
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_MultipleRoots_Throws()
        {
            var threw = false;
            try
            {
                XmlDocument.Parse("<a/><b/>");
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void ParseValue_EmptyElement_Stops()
        {
            var reader = new Utf8XmlReader("<e/><x/>"u8);
            using var document = XmlDocument.ParseValue(ref reader);
            document.RootElement.LocalName.Should().Be("e");
        }

        [TestMethod]
        [DataRow("<a>t</a>", 2, DisplayName = "the reader is on the text, so the next element token is an end tag")]
        [DataRow("<a><b/></a>", 3, DisplayName = "the reader is on the end tag itself")]
        public void ParseValue_FromAReaderWhoseElementItNeverSawStart_Throws(string xml, int reads)
        {
            string? message = null;
            try
            {
                var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml));
                for (var i = 0; i < reads; i++)
                {
                    reader.Read();
                }

                XmlDocument.ParseValue(ref reader).Dispose();
            }
            catch (XmlException ex)
            {
                message = ex.Message;
            }

            message.Should().Be("End tag without a matching start tag.");
        }

        [TestMethod]
        public void ParseValue_FromAReaderOnAnElementInsideADocument_TakesThatElementAndStops()
        {
            var reader = new Utf8XmlReader("<a><b x=\"1\">t<c/></b><d/></a>"u8);
            reader.Read();
            reader.Read();
            using var document = XmlDocument.ParseValue(ref reader);
            document.RootElement.GetRawXml().Should().Be("<b x=\"1\">t<c/></b>");
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            reader.LocalNameEquals("b").Should().BeTrue();
            reader.Read().Should().BeTrue();
            reader.LocalNameEquals("d").Should().BeTrue();
        }

        [TestMethod]
        public void ParseValue_Malformed_Disposes()
        {
            var threw = false;
            try
            {
                var reader = new Utf8XmlReader("<e>"u8);
                XmlDocument.ParseValue(ref reader);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_TwoPrologComments()
        {
            using var document = XmlDocument.Parse("<!--a--><!--b--><e/>", new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Allow });
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            document.WriteTo(writer);
            writer.Flush();
            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<!--a--><!--b--><e/>");
        }

        [TestMethod]
        public void AddRow_Grows()
        {
            var xml = new StringBuilder("<r>");
            for (var i = 0; i < 40; i++)
            {
                xml.Append("<e/>");
            }

            xml.Append("</r>");
            using var document = XmlDocument.Parse(xml.ToString());
            var names = new List<string>();
            foreach (var child in document.RootElement.EnumerateChildren())
            {
                names.Add(child.LocalName);
            }

            names.Should().Equal(Enumerable.Repeat("e", 40));
        }

        [TestMethod]
        public void Getters_OnAMissingNodeOrAValueOfTheWrongForm_SayWhatIsWrong()
        {
            using var document = XmlDocument.Parse("<e a=\"x\">!!!!</e>");
            var root = document.RootElement;
            root.AttributeCount.Should().Be(1);
            foreach (var child in root.EnumerateChildren())
            {
                (child.TokenType + ":" + child.AttributeCount).Should().Be("Text:0", "only an element has attributes");
            }

            var messages = new List<string>
            {
                "attribute: " + Message(() => root.GetAttribute("z")),
                "attribute in a namespace: " + Message(() => root.GetAttribute("a", "urn:no")),
                "child in a namespace: " + Message(() => root.GetChild("z", "urn:x")),
                "boolean: " + Message(() => root.GetBoolean()),
                "base64: " + Message(() => root.GetBytesFromBase64()),
                "dateTime: " + Message(() => root.GetDateTime()),
                "dateTimeOffset: " + Message(() => root.GetDateTimeOffset()),
                "decimal: " + Message(() => root.GetDecimal()),
                "double: " + Message(() => root.GetDouble()),
                "guid: " + Message(() => root.GetGuid()),
                "int64: " + Message(() => root.GetInt64()),
            };
            messages.Should().Equal(
                "attribute: Attribute 'z' was not found.",
                "attribute in a namespace: Attribute 'a' was not found.",
                "child in a namespace: Child element 'z' was not found.",
                "boolean: The value is not a boolean.",
                "base64: The value is not valid base64.",
                "dateTime: The value is not a valid dateTime.",
                "dateTimeOffset: The value is not a valid dateTime.",
                "decimal: The value is not a valid decimal.",
                "double: The value is not a valid double.",
                "guid: The value is not a valid GUID.",
                "int64: The value is not a valid integer.");

            static string Message(Action read)
            {
                try
                {
                    read();
                }
                catch (XmlException ex)
                {
                    return ex.Message;
                }

                return "no exception";
            }
        }

        [TestMethod]
        public void WriteTo_NullWriter_Throws()
        {
            using var document = XmlDocument.Parse("<e/>");
            var threw = false;
            try
            {
                document.WriteTo(null!);
            }
            catch (ArgumentNullException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Stream_ExactBufferCap_Throws()
        {
            var payload = "<e>" + new string('x', XmlLimits.DefaultBufferSize) + "</e>";
            var threw = false;
            try
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
                XmlDocument.Parse(stream, new XmlDocumentOptions { MaxDocumentByteCount = XmlLimits.DefaultBufferSize });
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Attribute_GetRawXml_AndBooleanDigits()
        {
            using var document = XmlDocument.Parse("<e a=\"1\">0</e>");
            document.RootElement.GetAttribute("a").GetRawXml().Should().Be("", "an attribute has no markup outside its element");
            document.RootElement.GetAttribute("a").TokenType.Should().Be(XmlTokenType.Attribute);
            document.RootElement.GetBoolean().Should().BeFalse();
            using var one = XmlDocument.Parse("<e>1</e>");
            one.RootElement.GetBoolean().Should().BeTrue();
            using var b64 = XmlDocument.Parse("<e>AQID</e>");
            b64.RootElement.GetBytesFromBase64().Should().Equal(1, 2, 3);
            using var inf = XmlDocument.Parse("<e>inf</e>");
            double.IsPositiveInfinity(inf.RootElement.GetDouble()).Should().BeTrue();
            using var ninf = XmlDocument.Parse("<e>-inf</e>");
            double.IsNegativeInfinity(ninf.RootElement.GetDouble()).Should().BeTrue();
            using var nan = XmlDocument.Parse("<e>nan</e>");
            double.IsNaN(nan.RootElement.GetDouble()).Should().BeTrue();
            foreach (var child in document.RootElement.EnumerateChildren())
            {
                child.TokenType.Should().Be(XmlTokenType.Text);
                child.CloneNode().Should().BeOfType<XmlTextNode>();
            }
        }

        [TestMethod]
        public void Clone_CommentCDataPi()
        {
            using var document = XmlDocument.Parse("<e><![CDATA[x]]><!--c--><?pi d?></e>", new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Allow });
            document.RootElement.GetRawXml().Should().Be("<e><![CDATA[x]]><!--c--><?pi d?></e>");
            var clones = new List<string>();
            foreach (var child in document.RootElement.EnumerateChildren())
            {
                clones.Add(child.TokenType + ":" + child.CloneNode() switch
                {
                    XmlCDataNode cdata => "cdata " + cdata.Value,
                    XmlCommentNode comment => "comment " + comment.Value,
                    XmlProcessingInstructionNode instruction => "instruction " + instruction.Target + " " + instruction.Data,
                    var other => other.GetType().Name,
                });
            }

            clones.Should().Equal("CData:cdata x", "Comment:comment c", "ProcessingInstruction:instruction pi d");
        }

        [TestMethod]
        public void Equals_SameRow()
        {
            using var document = XmlDocument.Parse("<e/>");
            var a = document.RootElement;
            var b = document.RootElement;
            a.Equals(b).Should().BeTrue();
            (a == b).Should().BeTrue();
            (a != default).Should().BeTrue();
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [TestMethod]
        public void EmptyDocument_RootElement_Throws()
        {
            using var document = new XmlDocument();
            var threw = false;
            try
            {
                _ = document.RootElement;
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_DeepNesting_GrowsStack()
        {
            var depth = XmlLimits.DefaultMaxDepth + 4;
            var xml = new StringBuilder();
            for (var i = 0; i < depth; i++)
            {
                xml.Append("<e>");
            }

            for (var i = 0; i < depth; i++)
            {
                xml.Append("</e>");
            }

            using var document = XmlDocument.Parse(xml.ToString(), new XmlDocumentOptions { MaxDepth = depth + 1 });
            document.RootElement.LocalName.Should().Be("e");
        }

        [TestMethod]
        public void Stream_ExactCap_Succeeds()
        {
            var inner = XmlLimits.DefaultBufferSize - 7;
            var xml = "<e>" + new string('x', inner) + "</e>";
            xml.Length.Should().Be(XmlLimits.DefaultBufferSize);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            using var document = XmlDocument.Parse(stream, new XmlDocumentOptions { MaxDocumentByteCount = XmlLimits.DefaultBufferSize });
            document.RootElement.GetInnerText().Length.Should().Be(inner);
        }

        #endregion

    }

}
