using System;
using System.Text;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Metadata for serializing a CLR type.
    /// </summary>
    public abstract class XmlTypeInfo
    {

        #region Fields

        private readonly byte[] _rootLocalName;
        private readonly byte[] _rootNamespaceUri;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the converter used to read and write values of <see cref="Type"/>.
        /// </summary>
        public XmlConverter Converter { get; }

        /// <summary>
        /// Gets whether a null value is written as a nillable element.
        /// </summary>
        public bool IsNillable { get; }

        /// <summary>
        /// Gets the options that produced this metadata.
        /// </summary>
        public XmlSerializerOptions Options { get; }

        /// <summary>
        /// Gets the document element name.
        /// </summary>
        public XmlName RootName { get; }

        /// <summary>
        /// Gets the CLR type.
        /// </summary>
        public Type Type { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates type info.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="converter">The converter.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="rootName">The document element name.</param>
        /// <param name="isNillable">Whether null is representable.</param>
        private protected XmlTypeInfo(Type type, XmlConverter converter, XmlSerializerOptions options, XmlName rootName, bool isNillable)
        {
            Type = type;
            Converter = converter;
            Options = options;
            RootName = rootName;
            IsNillable = isNillable;
            _rootLocalName = Encoding.UTF8.GetBytes(rootName.LocalName);
            _rootNamespaceUri = Encoding.UTF8.GetBytes(rootName.NamespaceUri);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks that the start tag <paramref name="reader"/> is on is the document element
        /// of <see cref="Type"/>.
        /// </summary>
        /// <param name="reader">The reader.</param>
        internal void CheckRoot(ref Utf8XmlReader reader)
        {
            if (!reader.LocalNameEquals(_rootLocalName))
            {
                throw new XmlException($"The root element does not match '{RootName.LocalName}'.");
            }

            if (_rootNamespaceUri.Length > 0 && !reader.NamespaceUriEquals(_rootNamespaceUri))
            {
                throw new XmlException($"The root element is not in namespace '{RootName.NamespaceUri}'.");
            }
        }

        /// <summary>
        /// Reads the document element of <see cref="Type"/> and returns the value boxed.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The value.</returns>
        internal abstract object? ReadRootAsObject(ref Utf8XmlReader reader);

        /// <summary>
        /// Writes a null value as the document element.
        /// </summary>
        /// <param name="writer">The writer.</param>
        internal void WriteNullRoot(Utf8XmlWriter writer)
        {
            if (!IsNillable)
            {
                throw new InvalidOperationException($"Type '{Type}' cannot be null.");
            }

            if (Options.NilHandling is XmlNilHandling.Omit)
            {
                throw new InvalidOperationException("A null document root cannot be omitted.");
            }

            WriteStartRoot(writer);
            if (Options.NilHandling is XmlNilHandling.WriteXsiNil)
            {
                writer.WriteNilValue();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes a boxed value as the document element.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        internal abstract void WriteRootAsObject(Utf8XmlWriter writer, object? value);

        /// <summary>
        /// Writes the start tag of the document element, declaring its namespace when it has one.
        /// </summary>
        /// <param name="writer">The writer.</param>
        internal void WriteStartRoot(Utf8XmlWriter writer)
        {
            if (_rootNamespaceUri.Length > 0)
            {
                writer.WriteStartElement(_rootLocalName, _rootNamespaceUri);
                return;
            }

            writer.WriteStartElement(_rootLocalName);
        }

        #endregion

    }

}
