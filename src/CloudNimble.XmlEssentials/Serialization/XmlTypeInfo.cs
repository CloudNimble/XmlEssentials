using System;
using System.Collections.Generic;
using System.Text;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Provides XML serialization-related metadata about a type.
    /// </summary>
    /// <remarks>
    /// Instances are created by the library. This class cannot be derived from outside the library.
    /// </remarks>
    public abstract class XmlTypeInfo
    {

        #region Fields

        private readonly byte[]? _rootLeafCloseTag;
        private readonly byte[]? _rootLeafOpenTag;
        private readonly byte[] _rootLocalName;
        private readonly byte[] _rootNamespaceUri;
        private readonly XmlDocumentElementPlan? _documentElementPlan;
        private byte[]? _sequenceElementName;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the <see cref="XmlConverter"/> associated with the current type.
        /// </summary>
        /// <value>
        /// The converter that reads and writes values of <see cref="Type"/>.
        /// </value>
        public XmlConverter Converter { get; }

        /// <summary>
        /// Gets a value that indicates whether a value of <see cref="Type"/> can be <see langword="null"/>.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if <see cref="Type"/> is a reference type or a <see cref="Nullable{T}"/>; otherwise, <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// Only a nillable type can be read from an element marked <c>xsi:nil="true"</c> or written from a <see langword="null"/> value.
        /// A <see langword="null"/> value that is written is an element marked <c>xsi:nil="true"</c>.
        /// </remarks>
        public bool IsNillable { get; }

        /// <summary>
        /// Gets the <see cref="XmlSerializerOptions"/> value associated with the current <see cref="XmlTypeInfo"/> instance.
        /// </summary>
        /// <value>
        /// The options that the metadata was created for.
        /// </value>
        public XmlSerializerOptions Options { get; }

        /// <summary>
        /// Gets the list of <see cref="XmlPropertyInfo"/> metadata corresponding to the current type.
        /// </summary>
        /// <value>
        /// The serialized members of an object type, in the order they are written. The list is empty for a type that is not mapped as an object,
        /// and for a type that a context generated in <see cref="XmlSourceGenerationMode.Serialization"/> mode.
        /// </value>
        public virtual IList<XmlPropertyInfo> Properties => XmlPropertyInfoList.Empty;

        /// <summary>
        /// Gets the name of the document element that a value of <see cref="Type"/> is serialized as.
        /// </summary>
        /// <value>
        /// The name of the document element.
        /// </value>
        public XmlName RootName { get; }

        /// <summary>
        /// Gets the UTF-8 name of the element that holds a sequence of values of <see cref="Type"/>. It is found
        /// through the attributes of the type, so it is found once and kept.
        /// </summary>
        internal byte[] SequenceElementName => _sequenceElementName ??= Encoding.UTF8.GetBytes(XmlTypeNames.SequenceElementName(Type, Options));

        /// <summary>
        /// Gets the <see cref="System.Type"/> for which the XML serialization contract is defined.
        /// </summary>
        /// <value>
        /// The type that the metadata describes.
        /// </value>
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
            _documentElementPlan = XmlNamespaceDeclarations.Plan(type, options);
            if (_rootNamespaceUri.Length == 0 && XmlChars.IsShortAsciiNcName(_rootLocalName, _rootLocalName.Length))
            {
                _rootLeafOpenTag = [(byte)'<', .. _rootLocalName, (byte)'>'];
                _rootLeafCloseTag = [(byte)'<', (byte)'/', .. _rootLocalName, (byte)'>'];
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Builds the document element name of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>
        /// The local name from <see cref="XmlRootAttribute.ElementName"/>, or the XML name of the type under the
        /// element naming policy when the attribute gives none; in the namespace from
        /// <see cref="XmlRootAttribute.Namespace"/>, or <see cref="XmlSerializerOptions.DefaultNamespace"/> when
        /// the attribute gives none.
        /// </returns>
        /// <remarks>
        /// Every kind of type info uses this one rule, so a type has the same document element whether the
        /// reflection resolver, a generated context, or a converter of the caller's reads and writes it.
        /// </remarks>
        internal static XmlName CreateRootName(Type type, XmlSerializerOptions options)
        {
            var root = (XmlRootAttribute?)Attribute.GetCustomAttribute(type, typeof(XmlRootAttribute), inherit: true);
            return new XmlName(XmlTypeNames.ElementName(type, options), root?.Namespace ?? options.DefaultNamespace);
        }

        /// <summary>
        /// Checks that the start tag <paramref name="reader"/> is on is the document element
        /// of <see cref="Type"/>.
        /// </summary>
        /// <param name="reader">The reader.</param>
        internal void CheckRoot(ref Utf8XmlReader reader)
        {
            if (!XmlBoundProperty.NameEquals(ref reader, _rootLocalName, Options.PropertyNameCaseInsensitive))
            {
                throw XmlException.Create($"The root element does not match '{RootName.LocalName}'.");
            }

            if (_rootNamespaceUri.Length > 0 && !reader.NamespaceUriEquals(_rootNamespaceUri))
            {
                throw XmlException.Create($"The root element is not in namespace '{RootName.NamespaceUri}'.");
            }
        }

        /// <summary>
        /// Reads the element the reader is on, whatever its name, and returns the value boxed.
        /// </summary>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <returns>The value.</returns>
        internal abstract object? ReadElementAsObject(ref Utf8XmlReader reader);

        /// <summary>
        /// Returns whether the start tag the reader is on is marked <c>xsi:nil</c>, so that the value is null.
        /// </summary>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <returns><see langword="true"/> when the element stands for null.</returns>
        /// <exception cref="XmlException">The element is marked nil and the type cannot be null.</exception>
        internal abstract bool IsNilElement(ref Utf8XmlReader reader);

        /// <summary>
        /// Gets the binding of an object that the built-in object converter reads, which is what a read from a
        /// stream can stop in the middle of and go on with; otherwise <see langword="null"/>.
        /// </summary>
        internal virtual XmlObjectBinding? ResumableBinding => null;

        /// <summary>
        /// Reads the document element of <see cref="Type"/> and returns the value boxed.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The value.</returns>
        internal abstract object? ReadRootAsObject(ref Utf8XmlReader reader);

        /// <summary>
        /// Gets the whole end tag of the element named for <see cref="Type"/>, <c>&lt;/name&gt;</c>, or null
        /// when the name has a namespace or is not a short ASCII name.
        /// </summary>
        internal byte[]? RootLeafCloseTag => _rootLeafCloseTag;

        /// <summary>
        /// Gets the whole start tag of the element named for <see cref="Type"/>, <c>&lt;name&gt;</c>, or null
        /// when the name has a namespace or is not a short ASCII name.
        /// </summary>
        internal byte[]? RootLeafOpenTag => _rootLeafOpenTag;

        /// <summary>
        /// Gets the tags of the element named for <see cref="Type"/>, built ahead of time, for a value that is
        /// written without an element frame.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="closeTag">The whole end tag, <c>&lt;/name&gt;</c>.</param>
        /// <returns>
        /// The whole start tag, <c>&lt;name&gt;</c>, or null when the writer is not inside an element or the
        /// name cannot be written from bytes built ahead of time.
        /// </returns>
        /// <remarks>
        /// For an item of a collection that is the document root. The caller writes exactly one value.
        /// </remarks>
        internal byte[]? RootLeafTags(Utf8XmlWriter writer, out byte[]? closeTag)
        {
            closeTag = _rootLeafCloseTag;
            return writer.CurrentDepth == 0 ? null : _rootLeafOpenTag;
        }

        /// <summary>
        /// Starts an element as the document element of <see cref="Type"/>, with the namespaces of
        /// <see cref="XmlSerializerOptions.Namespaces"/> and of the type's
        /// <see cref="XmlNamespaceDeclarationAttribute"/>s declared on it, when there are any and the writer is
        /// at the document level.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="utf8LocalName">The local name of the element.</param>
        /// <param name="utf8NamespaceUri">The namespace of the element, or empty.</param>
        /// <returns><see langword="false"/> when nothing was written, and the caller starts the element itself.</returns>
        /// <exception cref="InvalidOperationException">The declared namespaces cannot be written on the element.</exception>
        /// <remarks>
        /// The name is a parameter because a document element is not always named for the type: a value of a
        /// derived type is written under the derived type's name, and a sequence under a name of its own.
        /// </remarks>
        internal bool TryWriteStartDocumentElement(Utf8XmlWriter writer, ReadOnlySpan<byte> utf8LocalName, ReadOnlySpan<byte> utf8NamespaceUri)
        {
            if (_documentElementPlan is null || writer.CurrentDepth != 0)
            {
                return false;
            }

            _documentElementPlan.WriteStart(writer, utf8LocalName, utf8NamespaceUri);
            return true;
        }

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

            WriteStartRoot(writer);
            writer.WriteNilValue();
            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes a boxed value as element or attribute content, without a wrapping start tag.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        internal abstract void WriteContentAsObject(Utf8XmlWriter writer, object value);

        /// <summary>
        /// Writes a boxed value as the document element.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        internal abstract void WriteRootAsObject(Utf8XmlWriter writer, object? value);

        /// <summary>
        /// Writes the start tag of the element named for <see cref="Type"/>, declaring its namespace when it has
        /// one. When it is the document element, the namespaces of
        /// <see cref="XmlSerializerOptions.Namespaces"/> and of the type's
        /// <see cref="XmlNamespaceDeclarationAttribute"/>s are declared on it.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <exception cref="InvalidOperationException">The declared namespaces cannot be written on the document element.</exception>
        internal void WriteStartRoot(Utf8XmlWriter writer)
        {
            WriteStartRoot(writer, _rootLocalName);
        }

        /// <summary>
        /// Writes the start tag of an element in the namespace of <see cref="Type"/> under another local name:
        /// the element that holds a sequence of values of the type.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="utf8LocalName">The local name of the element.</param>
        /// <exception cref="InvalidOperationException">The declared namespaces cannot be written on the document element.</exception>
        internal void WriteStartRoot(Utf8XmlWriter writer, ReadOnlySpan<byte> utf8LocalName)
        {
            if (TryWriteStartDocumentElement(writer, utf8LocalName, _rootNamespaceUri))
            {
                return;
            }

            if (_rootNamespaceUri.Length > 0)
            {
                writer.WriteStartElement(utf8LocalName, _rootNamespaceUri);
                return;
            }

            writer.WriteStartElement(utf8LocalName);
        }

        #endregion

    }

}
