using System;
using System.Collections.Generic;
using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// An element is written in the namespace the caller gave, with the prefix the caller gave, whatever is
    /// already in scope. Every case runs through each overload that can express it, as a start tag and as an
    /// empty element, and is then read back.
    /// </summary>
    [TestClass]
    public class Utf8XmlWriterNamespaceTests
    {

        #region Fields

        private const string Xsi = "http://www.w3.org/2001/XMLSchema-instance";

        #endregion

        #region Public Methods

        [TestMethod]
        public void Attribute_InANamespaceWhosePrefixIsShadowed_GetsAPrefixOfItsOwn()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("p", "a", "urn:a");
                w.WriteStartElement("p", "m", "urn:m");
                w.WriteAttributeString(null, "x", "urn:a", "1");
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Be("<p:a xmlns:p=\"urn:a\"><p:m xmlns:p=\"urn:m\" xmlns:ns1=\"urn:a\" ns1:x=\"1\"/></p:a>");
        }

        [TestMethod]
        [DataRow("p", "urn:a", "q", "urn:a", "<p:a xmlns:p=\"urn:a\"><q:b xmlns:q=\"urn:a\"/></p:a>", "urn:a", DisplayName = "a new prefix for a namespace bound to another prefix")]
        [DataRow("p", "urn:a", "p", "urn:b", "<p:a xmlns:p=\"urn:a\"><p:b xmlns:p=\"urn:b\"/></p:a>", "urn:b", DisplayName = "a prefix in scope for another namespace")]
        [DataRow(null, "urn:a", "q", "urn:a", "<a xmlns=\"urn:a\"><q:b xmlns:q=\"urn:a\"/></a>", "urn:a", DisplayName = "a prefix for the default namespace in scope")]
        [DataRow("p", "urn:a", "p", "urn:a", "<p:a xmlns:p=\"urn:a\"><p:b/></p:a>", "urn:a", DisplayName = "the prefix and namespace already in scope")]
        [DataRow("p", "urn:a", null, "urn:a", "<p:a xmlns:p=\"urn:a\"><p:b/></p:a>", "urn:a", DisplayName = "no prefix, a namespace bound to a prefix")]
        [DataRow(null, "urn:a", null, "urn:a", "<a xmlns=\"urn:a\"><b/></a>", "urn:a", DisplayName = "no prefix, the default namespace in scope")]
        [DataRow(null, "urn:a", null, "urn:b", "<a xmlns=\"urn:a\"><b xmlns=\"urn:b\"/></a>", "urn:b", DisplayName = "no prefix, a namespace that is not in scope")]
        [DataRow(null, "urn:a", null, "", "<a xmlns=\"urn:a\"><b xmlns=\"\"/></a>", "", DisplayName = "no namespace under a default namespace")]
        [DataRow(null, "", null, "", "<a><b/></a>", "", DisplayName = "no namespace with no default namespace")]
        [DataRow("p", "urn:a", null, "", "<p:a xmlns:p=\"urn:a\"><b/></p:a>", "", DisplayName = "no namespace under a prefixed parent")]
        public void Child_IsWrittenInTheNamespaceGiven(string? parentPrefix, string parentNamespace, string? prefix, string namespaceUri, string expected, string expectedNamespace)
        {
            foreach (var form in Forms(prefix))
            {
                foreach (var empty in new[] { false, true })
                {
                    var xml = XmlWriterHarness.ToXml(w =>
                    {
                        w.WriteStartElement(parentPrefix, "a", parentNamespace);
                        Write(w, form, prefix, "b", namespaceUri, empty);
                        if (!empty)
                        {
                            w.WriteEndElement();
                        }

                        w.WriteEndElement();
                    });
                    xml.Should().Be(expected, "{0} overload, empty element: {1}", form, empty);
                    NamespaceOf(xml, "b").Should().Be(expectedNamespace, "{0} overload, empty element: {1}", form, empty);
                }
            }
        }

        [TestMethod]
        public void Child_WithANullNamespace_InheritsTheDefaultNamespace()
        {
            // A null namespace says nothing about the namespace, as it does for System.Xml.XmlWriter. Only an
            // empty string says "no namespace".
            const string expected = "<a xmlns=\"urn:a\"><b/></a>";
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("a", "urn:a");
                w.WriteStartElement("b", null);
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Be(expected);
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("a", "urn:a");
                w.WriteStartElement(null, "b", null);
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Be(expected);
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("a", "urn:a");
                w.WriteEmptyElement("b", null);
                w.WriteEndElement();
            }).Should().Be(expected);
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("a", "urn:a");
                w.WriteEmptyElement(null, "b", null);
                w.WriteEndElement();
            }).Should().Be(expected);
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("a", "urn:a");
                w.WriteElementString("b", null, null);
                w.WriteEndElement();
            }).Should().Be(expected);
        }

        [TestMethod]
        public void Child_WithNoNamespaceArgument_InheritsTheDefaultNamespace()
        {
            const string expected = "<a xmlns=\"urn:a\"><b/><b/><b/><b/><b/><b/></a>";
            var xml = XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("a", "urn:a");
                w.WriteStartElement("b");
                w.WriteEndElement();
                w.WriteStartElement("b".AsSpan());
                w.WriteEndElement();
                w.WriteStartElement("b"u8);
                w.WriteEndElement();
                w.WriteEmptyElement("b");
                w.WriteEmptyElement("b".AsSpan());
                w.WriteEmptyElement("b"u8);
                w.WriteEndElement();
            });
            xml.Should().Be(expected);
            NamespaceOf(xml, "b").Should().Be("urn:a");
        }

        [TestMethod]
        public void ElementString_WithAnEmptyNamespace_LeavesTheDefaultNamespace()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("a", "urn:a");
                w.WriteElementString("b", "", "v");
                w.WriteEndElement();
            }).Should().Be("<a xmlns=\"urn:a\"><b xmlns=\"\">v</b></a>");
        }

        [TestMethod]
        public void Grandchild_UnderAnElementInNoNamespace_NeedsNoDeclaration()
        {
            foreach (var form in Forms(null))
            {
                var xml = XmlWriterHarness.ToXml(w =>
                {
                    w.WriteStartElement("a", "urn:a");
                    Write(w, form, null, "b", "", empty: false);
                    Write(w, form, null, "c", "", empty: true);
                    w.WriteEmptyElement("d");
                    w.WriteEndElement();
                    Write(w, form, null, "e", "urn:a", empty: true);
                    w.WriteEndElement();
                });
                xml.Should().Be("<a xmlns=\"urn:a\"><b xmlns=\"\"><c/><d/></b><e/></a>", "{0} overload", form);
                NamespaceOf(xml, "c").Should().Be("", "{0} overload", form);
                NamespaceOf(xml, "d").Should().Be("", "{0} overload", form);
                NamespaceOf(xml, "e").Should().Be("urn:a", "{0} overload", form);
            }
        }

        [TestMethod]
        [DataRow(null, "urn:a", null, "urn:m", "<a xmlns=\"urn:a\"><m xmlns=\"urn:m\"><b xmlns=\"urn:a\"/></m></a>", DisplayName = "the default namespace was replaced in between")]
        [DataRow("p", "urn:a", "p", "urn:m", "<p:a xmlns:p=\"urn:a\"><p:m xmlns:p=\"urn:m\"><b xmlns=\"urn:a\"/></p:m></p:a>", DisplayName = "the prefix was bound to another namespace in between")]
        [DataRow("p", "urn:a", null, "urn:m", "<p:a xmlns:p=\"urn:a\"><m xmlns=\"urn:m\"><p:b/></m></p:a>", DisplayName = "the prefix is still bound under another default namespace")]
        public void Grandchild_WithNoPrefix_UsesOnlyABindingThatIsStillInEffect(string? outerPrefix, string outerNamespace, string? middlePrefix, string middleNamespace, string expected)
        {
            foreach (var form in Forms(null))
            {
                foreach (var empty in new[] { false, true })
                {
                    var xml = XmlWriterHarness.ToXml(w =>
                    {
                        w.WriteStartElement(outerPrefix, "a", outerNamespace);
                        w.WriteStartElement(middlePrefix, "m", middleNamespace);
                        Write(w, form, null, "b", "urn:a", empty);
                        if (!empty)
                        {
                            w.WriteEndElement();
                        }

                        w.WriteEndElement();
                        w.WriteEndElement();
                    });
                    xml.Should().Be(expected, "{0} overload, empty element: {1}", form, empty);
                    NamespaceOf(xml, "b").Should().Be("urn:a", "{0} overload, empty element: {1}", form, empty);
                }
            }
        }

        [TestMethod]
        public void NilValue_WhenTheSchemaInstanceNamespaceHasAnotherPrefix_DeclaresXsi()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("a");
                w.WriteNamespaceDeclaration("i", Xsi);
                w.WriteStartElement("b");
                w.WriteNilValue();
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Be("<a xmlns:i=\"" + Xsi + "\"><b xmlns:xsi=\"" + Xsi + "\" xsi:nil=\"true\"/></a>");
        }

        [TestMethod]
        public void NilValue_WhenXsiIsBoundToAnotherNamespace_DeclaresXsiAgain()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("a");
                w.WriteNamespaceDeclaration("xsi", "urn:other");
                w.WriteStartElement("b");
                w.WriteNilValue();
                w.WriteEndElement();
                w.WriteEndElement();
            }).Should().Be("<a xmlns:xsi=\"urn:other\"><b xmlns:xsi=\"" + Xsi + "\" xsi:nil=\"true\"/></a>");
        }

        [TestMethod]
        public void Root_WithAnEmptyNamespace_NeedsNoDeclaration()
        {
            foreach (var form in Forms(null))
            {
                XmlWriterHarness.ToXml(w => Write(w, form, null, "r", "", empty: true)).Should().Be("<r/>", "{0} overload", form);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns the overload families that can write a name with <paramref name="prefix"/>.
        /// </summary>
        /// <param name="prefix">The prefix, or <see langword="null"/> for none.</param>
        /// <returns>The families. The character-span overloads take no prefix.</returns>
        private static string[] Forms(string? prefix)
        {
            return prefix is null ? ["string", "string with prefix", "chars", "utf8", "utf8 with prefix"] : ["string with prefix", "utf8 with prefix"];
        }

        /// <summary>
        /// Returns the namespace the reader reports for the first element named <paramref name="localName"/>.
        /// </summary>
        /// <param name="xml">The document.</param>
        /// <param name="localName">The local name.</param>
        /// <returns>The namespace URI, or an empty string for none.</returns>
        private static string NamespaceOf(string xml, string localName)
        {
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml));
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.StartElement && reader.LocalNameEquals(localName))
                {
                    return Encoding.UTF8.GetString(reader.NamespaceUriSpan);
                }
            }

            throw new InvalidOperationException($"The document has no element named '{localName}'.");
        }

        private static void Write(Utf8XmlWriter writer, string form, string? prefix, string localName, string namespaceUri, bool empty)
        {
            var utf8Prefix = Encoding.UTF8.GetBytes(prefix ?? "");
            var utf8Local = Encoding.UTF8.GetBytes(localName);
            var utf8Namespace = Encoding.UTF8.GetBytes(namespaceUri);
            switch (form, empty)
            {
                case ("string", false): writer.WriteStartElement(localName, namespaceUri); break;
                case ("string", true): writer.WriteEmptyElement(localName, namespaceUri); break;
                case ("string with prefix", false): writer.WriteStartElement(prefix, localName, namespaceUri); break;
                case ("string with prefix", true): writer.WriteEmptyElement(prefix, localName, namespaceUri); break;
                case ("chars", false): writer.WriteStartElement(localName.AsSpan(), namespaceUri.AsSpan()); break;
                case ("chars", true): writer.WriteEmptyElement(localName.AsSpan(), namespaceUri.AsSpan()); break;
                case ("utf8", false): writer.WriteStartElement(utf8Local, utf8Namespace); break;
                case ("utf8", true): writer.WriteEmptyElement(utf8Local, utf8Namespace); break;
                case ("utf8 with prefix", false): writer.WriteStartElement(utf8Prefix, utf8Local, utf8Namespace); break;
                case ("utf8 with prefix", true): writer.WriteEmptyElement(utf8Prefix, utf8Local, utf8Namespace); break;
                default: throw new ArgumentOutOfRangeException(nameof(form), form, "No such overload family.");
            }
        }

        #endregion

    }

}
