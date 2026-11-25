using System;
using System.Text;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// The namespace declarations that are written on the document element of a type, and how that element is
    /// started so that all of them can be written on it.
    /// </summary>
    /// <remarks>
    /// The writer declares the namespace of an element itself when it starts the element, and refuses the same
    /// declaration a second time. So the declaration that names the element's own namespace is not written
    /// again. It decides how the element is started instead: with no prefix when the default namespace is
    /// declared as that namespace, and otherwise with the prefix declared for it, the last one when there are
    /// several, which is the one the writer uses for the elements inside.
    /// </remarks>
    internal sealed class XmlDocumentElementPlan
    {

        #region Fields

        private readonly byte[][] _declarations;
        private readonly Type _type;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a plan.
        /// </summary>
        /// <param name="type">The type that is serialized as a document, for messages.</param>
        /// <param name="declarations">
        /// The declarations in the order they are written: a prefix and a namespace URI in turn, as UTF-8. An
        /// empty prefix is the default namespace.
        /// </param>
        internal XmlDocumentElementPlan(Type type, byte[][] declarations)
        {
            _type = type;
            _declarations = declarations;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the document element and writes the declarations on it.
        /// </summary>
        /// <param name="writer">The writer, at the document level.</param>
        /// <param name="utf8LocalName">The local name of the element.</param>
        /// <param name="utf8NamespaceUri">The namespace of the element, or empty.</param>
        /// <exception cref="InvalidOperationException">
        /// The element is in a namespace that has no declaration, and the default namespace is declared as
        /// another: both would be the default namespace of this element.
        /// </exception>
        internal void WriteStart(Utf8XmlWriter writer, ReadOnlySpan<byte> utf8LocalName, ReadOnlySpan<byte> utf8NamespaceUri)
        {
            var own = FindOwn(utf8NamespaceUri);
            if (own >= 0 && _declarations[own].Length > 0)
            {
                writer.WriteStartElement(_declarations[own], utf8LocalName, utf8NamespaceUri);
            }
            else if (utf8NamespaceUri.IsEmpty)
            {
                writer.WriteStartElement(utf8LocalName);
            }
            else
            {
                writer.WriteStartElement(utf8LocalName, utf8NamespaceUri);
            }

            for (var i = 0; i < _declarations.Length; i += 2)
            {
                if (i != own)
                {
                    writer.WriteNamespaceDeclaration(_declarations[i], _declarations[i + 1]);
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns the index of the declaration that names the element's own namespace, which the writer writes
        /// when it starts the element.
        /// </summary>
        /// <param name="utf8NamespaceUri">The namespace of the element, or empty.</param>
        /// <returns>The index of the prefix in the declarations, or -1 when the element has no namespace.</returns>
        /// <exception cref="InvalidOperationException">The declarations cannot be written on this element.</exception>
        private int FindOwn(ReadOnlySpan<byte> utf8NamespaceUri)
        {
            if (utf8NamespaceUri.IsEmpty)
            {
                return -1;
            }

            var prefixed = -1;
            var defaultNamespace = -1;
            for (var i = 0; i < _declarations.Length; i += 2)
            {
                var names = utf8NamespaceUri.SequenceEqual(_declarations[i + 1]);
                if (_declarations[i].Length == 0)
                {
                    if (names)
                    {
                        return i;
                    }

                    defaultNamespace = i;
                }
                else if (names)
                {
                    prefixed = i;
                }
            }

            if (prefixed < 0 && defaultNamespace >= 0)
            {
                throw new InvalidOperationException(
                    $"The document element of '{_type}' is in the namespace '{Encoding.UTF8.GetString(utf8NamespaceUri)}', and no prefix is declared for that namespace, so the default namespace cannot be declared as '{Encoding.UTF8.GetString(_declarations[defaultNamespace + 1])}'.");
            }

            return prefixed;
        }

        #endregion

    }

}
