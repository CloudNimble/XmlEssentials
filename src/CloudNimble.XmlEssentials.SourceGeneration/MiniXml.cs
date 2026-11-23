using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// A compile-time XML element used to read XSD AdditionalFiles without System.Xml.Schema.
    /// </summary>
    internal sealed class MiniElement
    {

        #region Public Properties

        /// <summary>
        /// Gets the attributes (unprefixed local name → value). Namespaced attributes use <c>{ns}local</c>.
        /// </summary>
        internal Dictionary<string, string> Attributes { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the child elements.
        /// </summary>
        internal List<MiniElement> Elements { get; } = new List<MiniElement>();

        /// <summary>
        /// Gets the local name.
        /// </summary>
        internal string LocalName { get; }

        /// <summary>
        /// Gets the namespace URI.
        /// </summary>
        internal string NamespaceUri { get; }

        /// <summary>
        /// Gets concatenated text.
        /// </summary>
        internal string Text { get; set; } = "";

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an element.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace URI.</param>
        internal MiniElement(string localName, string namespaceUri)
        {
            LocalName = localName;
            NamespaceUri = namespaceUri;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the attribute value for <paramref name="localName"/>, or null.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <returns>The value, or null.</returns>
        internal string? Attr(string localName)
        {
            return Attributes.TryGetValue(localName, out var value) ? value : null;
        }

        /// <summary>
        /// Parses <paramref name="xml"/> into an element tree.
        /// </summary>
        /// <param name="xml">The XML.</param>
        /// <returns>The document element.</returns>
        /// <exception cref="XmlException">The payload is not well-formed, or it contains a DTD.</exception>
        internal static MiniElement Parse(string xml)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
            };
            using var reader = XmlReader.Create(new StringReader(xml), settings);
            while (reader.Read() && reader.NodeType != XmlNodeType.Element)
            {
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                throw new XmlException("The schema has no root element.");
            }

            return ReadElement(reader);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Reads the element the reader is on, including descendants.
        /// </summary>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <returns>The element.</returns>
        internal static MiniElement ReadElement(XmlReader reader)
        {
            var element = new MiniElement(reader.LocalName, reader.NamespaceURI ?? "");
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    var key = string.IsNullOrEmpty(reader.NamespaceURI)
                        ? reader.LocalName
                        : "{" + reader.NamespaceURI + "}" + reader.LocalName;
                    element.Attributes[key] = reader.Value;
                }

                reader.MoveToElement();
            }

            if (reader.IsEmptyElement)
            {
                return element;
            }

            while (reader.Read())
            {
                if (reader.NodeType is XmlNodeType.EndElement)
                {
                    break;
                }

                if (reader.NodeType is XmlNodeType.Element)
                {
                    element.Elements.Add(ReadElement(reader));
                }
                else if (reader.NodeType is XmlNodeType.Text or XmlNodeType.CDATA)
                {
                    element.Text += reader.Value;
                }
            }

            return element;
        }

        #endregion

    }

}
