using System;
using System.Buffers;
using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// <see cref="Utf8XmlWriter.WriteRawValue(ReadOnlySpan{byte}, bool)"/> copies its input, and unless the
    /// caller asks it not to, first checks that the input is well formed in the place it is written: element
    /// content, an attribute value, or the document level. Every case runs through the UTF-8, character and
    /// string overloads.
    /// </summary>
    [TestClass]
    public class Utf8XmlWriterRawValueTests
    {

        #region Fields

        private static readonly string[] Forms = ["utf8", "chars", "string"];
        private const string NotWellFormed = "The raw XML is not well formed: ";

        #endregion

        #region Public Methods

        [TestMethod]
        [DataRow("a&amp;b &#65;&#x42; &lt;&gt;&apos;&quot; plain", DisplayName = "references and text")]
        [DataRow("", DisplayName = "empty")]
        [DataRow("tab\there", DisplayName = "a literal tab")]
        [DataRow("]]> is allowed here", DisplayName = "the CDATA end, which only element content forbids")]
        public void AttributeValue_WellFormed_IsCopied(string raw)
        {
            foreach (var form in Forms)
            {
                XmlWriterHarness.ToXml(w =>
                {
                    w.WriteStartElement("e");
                    w.WriteStartAttribute("a");
                    Write(w, form, raw, skipInputValidation: false);
                    w.WriteEndElement();
                }).Should().Be("<e a=\"" + raw + "\"/>", "{0} overload", form);
            }
        }

        [TestMethod]
        [DataRow("say \"hi\"", "An attribute value must not contain a quotation mark or '<'.", DisplayName = "a quotation mark, which would end the value")]
        [DataRow("\" b=\"1", "An attribute value must not contain a quotation mark or '<'.", DisplayName = "a quotation mark that starts another attribute")]
        [DataRow("a<b", "An attribute value must not contain a quotation mark or '<'.", DisplayName = "a less-than sign")]
        [DataRow("a & b", null, DisplayName = "an ampersand that starts no reference")]
        [DataRow("&bogus;", null, DisplayName = "an entity that is not defined")]
        [DataRow("&#0;", null, DisplayName = "a reference to a character XML does not allow")]
        [DataRow("a\u0001b", null, DisplayName = "a control character")]
        public void AttributeValue_NotWellFormed_IsRejected(string raw, string? message)
        {
            foreach (var form in Forms)
            {
                var thrown = XmlWriterHarness.ThrownMessage(w =>
                {
                    w.WriteStartElement("e");
                    w.WriteStartAttribute("a");
                    Write(w, form, raw, skipInputValidation: false);
                });
                thrown.Should().NotBeNull("{0} overload must reject the value", form);
                thrown.Should().Be(NotWellFormed + (message ?? ReaderMessage("<r a=\"" + raw + "\"/>")), "{0} overload", form);
            }
        }

        [TestMethod]
        public void DocumentLevel_OneElement_IsTheRoot()
        {
            foreach (var form in Forms)
            {
                XmlWriterHarness.ToXml(w =>
                {
                    Write(w, form, "<!--before-->", skipInputValidation: false);
                    Write(w, form, "<r><c/></r>", skipInputValidation: false);
                    Write(w, form, "<?after x?>\n", skipInputValidation: false);
                }).Should().Be("<!--before--><r><c/></r><?after x?>\n", "{0} overload", form);
            }
        }

        [TestMethod]
        public void DocumentLevel_Declaration_IsAllowedOnlyAtTheStart()
        {
            var noDeclaration = new XmlWriterOptions { OmitXmlDeclaration = true };
            foreach (var form in Forms)
            {
                XmlWriterHarness.ToXml(w =>
                {
                    Write(w, form, "<?xml version=\"1.0\" encoding=\"utf-8\"?><r/>", skipInputValidation: false);
                }, noDeclaration).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?><r/>", "{0} overload", form);
                XmlWriterHarness.ThrownMessage(w =>
                {
                    w.WriteComment("first");
                    Write(w, form, "<?xml version=\"1.0\"?><r/>", skipInputValidation: false);
                }, noDeclaration).Should().Be(NotWellFormed + "An XML declaration is allowed only at the start of the document.", "{0} overload", form);
            }
        }

        [TestMethod]
        public void DocumentLevel_Xml11_IsCheckedAsTheVersionTheInputDeclares()
        {
            // With no declaration of its own the input is read as the version the writer writes. With one,
            // the declaration decides, as it will for whoever reads the document.
            var xml11 = new XmlWriterOptions { OmitXmlDeclaration = true, Version = XmlVersion.Version11 };
            XmlWriterHarness.ToXml(w => w.WriteRawValue("<r>&#1;</r>"u8), xml11).Should().Be("<r>&#1;</r>");
            XmlWriterHarness.ToXml(w => w.WriteRawValue("<?xml version=\"1.1\"?><r>&#1;</r>"u8), xml11).Should().Be("<?xml version=\"1.1\"?><r>&#1;</r>");
            XmlWriterHarness.ThrownMessage(w => w.WriteRawValue("<?xml version=\"1.0\"?><r>&#1;</r>"u8), xml11)
                .Should().Be(NotWellFormed + ReaderMessage("<?xml version=\"1.0\"?><r>&#1;</r>"));
            XmlWriterHarness.ToXml(w => w.WriteRawValue("<?xml-stylesheet href=\"a\"?><r>&#1;</r>"u8), xml11).Should().Be("<?xml-stylesheet href=\"a\"?><r>&#1;</r>");
        }

        [TestMethod]
        public void ElementContent_InsideAnElementWithTheXmlPrefix_IsChecked()
        {
            // The xml prefix needs no declaration, so the element can be written with a prefix and no namespace.
            foreach (var form in Forms)
            {
                XmlWriterHarness.ToXml(w =>
                {
                    w.WriteStartElement("xml", "e", null);
                    Write(w, form, "<x/>", skipInputValidation: false);
                    w.WriteEndElement();
                }).Should().Be("<xml:e><x/></xml:e>", "{0} overload", form);
                XmlWriterHarness.ThrownMessage(w =>
                {
                    w.WriteStartElement("xml", "e", null);
                    Write(w, form, "<x>", skipInputValidation: false);
                }).Should().Be(NotWellFormed + ReaderMessage("<xml:e><x></xml:e>"), "{0} overload", form);
            }
        }

        [TestMethod]
        public void DocumentLevel_SecondRoot_IsRejected()
        {
            const string message = "Multiple root elements are not allowed.";
            foreach (var form in Forms)
            {
                XmlWriterHarness.ThrownMessage(w => Write(w, form, "<a/><b/>", skipInputValidation: false)).Should().Be(NotWellFormed + message, "{0} overload", form);
                XmlWriterHarness.ThrownMessage(w =>
                {
                    Write(w, form, "<a/>", skipInputValidation: false);
                    Write(w, form, "<b/>", skipInputValidation: false);
                }).Should().Be(NotWellFormed + message, "{0} overload", form);
                XmlWriterHarness.ThrownMessage(w =>
                {
                    w.WriteStartElement("a");
                    w.WriteEndElement();
                    Write(w, form, "<b/>", skipInputValidation: false);
                }).Should().Be(NotWellFormed + message, "{0} overload", form);
                XmlWriterHarness.ThrownMessage(w =>
                {
                    Write(w, form, "<a/>", skipInputValidation: false);
                    w.WriteStartElement("b");
                }).Should().Be(message, "{0} overload", form);
            }
        }

        [TestMethod]
        [DataRow("text", DisplayName = "text")]
        [DataRow("<a>", DisplayName = "an element that is not closed")]
        [DataRow("<!DOCTYPE a>", DisplayName = "a document type declaration")]
        public void DocumentLevel_NotWellFormed_IsRejected(string raw)
        {
            foreach (var form in Forms)
            {
                var thrown = XmlWriterHarness.ThrownMessage(w => Write(w, form, raw, skipInputValidation: false));
                thrown.Should().Be(NotWellFormed + ReaderMessage(raw), "{0} overload", form);
            }
        }

        [TestMethod]
        [DataRow("<x/>", DisplayName = "an empty element")]
        [DataRow("text &amp; <b a='1' c=\"2\">bold</b><!--c--><?p d?><![CDATA[z<&]]> tail", DisplayName = "every kind of content")]
        [DataRow("", DisplayName = "empty")]
        [DataRow("  \n", DisplayName = "white space")]
        [DataRow("<q:x xmlns:q=\"urn:q\"/>", DisplayName = "a prefix the input declares")]
        [DataRow("<p:x p:a=\"1\"/><y/>", DisplayName = "a prefix declared on the element that is open")]
        [DataRow("<o:x/>", DisplayName = "a prefix declared on an ancestor")]
        public void ElementContent_WellFormed_IsCopied(string raw)
        {
            foreach (var form in Forms)
            {
                XmlWriterHarness.ToXml(w =>
                {
                    w.WriteStartElement("o", "root", "urn:o&\"<");
                    w.WriteStartElement("p", "e", "urn:p");
                    Write(w, form, raw, skipInputValidation: false);
                    w.WriteEndElement();
                    w.WriteEndElement();
                }).Should().Be("<o:root xmlns:o=\"urn:o&amp;&quot;&lt;\"><p:e xmlns:p=\"urn:p\">" + raw + "</p:e></o:root>", "{0} overload", form);
            }
        }

        [TestMethod]
        [DataRow("<x>", DisplayName = "an element that is not closed")]
        [DataRow("</x>", DisplayName = "an end tag with no start tag")]
        [DataRow("</e><e>", DisplayName = "an end tag for the element that is open")]
        [DataRow("<x></y>", DisplayName = "an end tag that does not match")]
        [DataRow("a < b", DisplayName = "a less-than sign in text")]
        [DataRow("&bogus;", DisplayName = "an entity that is not defined")]
        [DataRow("<u:x/>", DisplayName = "a prefix nothing declares")]
        [DataRow("<x a='1' a='2'/>", DisplayName = "an attribute written twice")]
        [DataRow("<!DOCTYPE x>", DisplayName = "a document type declaration")]
        [DataRow("a\u0001b", DisplayName = "a control character")]
        [DataRow("a]]>b", DisplayName = "the CDATA end in text")]
        [DataRow("<?xml version=\"1.0\"?>", DisplayName = "an XML declaration")]
        public void ElementContent_NotWellFormed_IsRejected(string raw)
        {
            foreach (var form in Forms)
            {
                var thrown = XmlWriterHarness.ThrownMessage(w =>
                {
                    w.WriteStartElement("e");
                    Write(w, form, raw, skipInputValidation: false);
                });
                thrown.Should().Be(NotWellFormed + ReaderMessage("<e>" + raw + "</e>"), "{0} overload", form);
            }
        }

        [TestMethod]
        public void ElementContent_ShadowedPrefix_UsesTheBindingInEffect()
        {
            // The prefix is bound twice on the way down. The input is checked against the inner binding only,
            // so the two declarations do not collide.
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("p", "a", "urn:1");
                w.WriteStartElement("p", "b", "urn:2");
                w.WriteRawValue("<p:c/>"u8);
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Be("<p:a xmlns:p=\"urn:1\"><p:b xmlns:p=\"urn:2\"><p:c/></p:b></p:a>");
        }

        [TestMethod]
        public void ElementContent_Xml11_IsCheckedAsXml11()
        {
            var xml11 = new XmlWriterOptions { OmitXmlDeclaration = true, Version = XmlVersion.Version11 };
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteRawValue("a&#1;b<x/>"u8);
                w.WriteEndElement();
            }, xml11).Should().Be("<e>a&#1;b<x/></e>");
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("a");
                w.WriteRawValue("&#1;"u8);
                w.WriteEndElement();
            }, xml11).Should().Be("<e a=\"&#1;\"/>");
            XmlWriterHarness.ThrownMessage(w =>
            {
                w.WriteStartElement("e");
                w.WriteRawValue("a&#1;b"u8);
            }).Should().Be(NotWellFormed + ReaderMessage("<e>a&#1;b</e>"));
        }

        [TestMethod]
        public void ElementContent_DeeperThanTheReaderDefault_IsCopied()
        {
            // The check is for well-formedness. The reader's default limits are not the writer's.
            var raw = new StringBuilder();
            for (var i = 0; i < 100; i++)
            {
                raw.Append("<d>");
            }

            for (var i = 0; i < 100; i++)
            {
                raw.Append("</d>");
            }

            var text = raw.ToString();
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteRawValue(text);
                w.WriteEndElement();
            }).Should().Be("<e>" + text + "</e>");
        }

        [TestMethod]
        [DataRow("<x>", DisplayName = "an element that is not closed")]
        [DataRow("a < b &bogus;", DisplayName = "text that is not well formed")]
        public void SkipInputValidation_CopiesWhateverItIsGiven(string raw)
        {
            foreach (var form in Forms)
            {
                var options = new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true };
                XmlWriterHarness.ToXml(w =>
                {
                    w.WriteStartElement("e");
                    Write(w, form, raw, skipInputValidation: true);
                }, options).Should().Be("<e>" + raw, "{0} overload", form);
                XmlWriterHarness.ToXml(w =>
                {
                    w.WriteStartElement("e");
                    w.WriteStartAttribute("a");
                    Write(w, form, "say \"hi\"", skipInputValidation: true);
                }, options).Should().Be("<e a=\"say \"hi\"", "{0} overload", form);
            }
        }

        [TestMethod]
        public void WriterThatSkipsValidation_CopiesWhateverItIsGiven()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteRawValue("<x>"u8);
                w.WriteRawValue("<y>"u8, skipInputValidation: false);
            }, new XmlWriterOptions { OmitXmlDeclaration = true, SkipValidation = true }).Should().Be("<e><x><y>");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns the message the reader gives for <paramref name="xml"/>, which the writer reports for raw
        /// input that makes the same document.
        /// </summary>
        /// <param name="xml">A document that is not well formed.</param>
        /// <returns>The message.</returns>
        private static string ReaderMessage(string xml)
        {
            try
            {
                XmlReaderHarness.Drain(xml);
            }
            catch (XmlException ex)
            {
                return ex.Message;
            }

            throw new InvalidOperationException("The reader accepted: " + xml);
        }

        private static void Write(Utf8XmlWriter writer, string form, string raw, bool skipInputValidation)
        {
            switch (form)
            {
                case "utf8": writer.WriteRawValue(Encoding.UTF8.GetBytes(raw), skipInputValidation); break;
                case "chars": writer.WriteRawValue(raw.AsSpan(), skipInputValidation); break;
                case "string": writer.WriteRawValue(raw, skipInputValidation); break;
                default: throw new ArgumentOutOfRangeException(nameof(form), form, "No such overload.");
            }
        }

        #endregion

    }

}
