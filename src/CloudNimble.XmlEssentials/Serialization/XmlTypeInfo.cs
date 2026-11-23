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

        private static readonly XmlPropertyInfo[] EmptyProperties = [];
        private readonly byte[]? _rootLeafCloseTag;
        private readonly byte[]? _rootLeafOpenTag;
        private readonly byte[] _rootLocalName;
        private readonly byte[] _rootNamespaceUri;

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
        /// Gets a parameterless factory that creates an instance of <see cref="Type"/> on deserialization.
        /// </summary>
        /// <value>
        /// A factory that creates an instance of <see cref="Type"/>, or <see langword="null"/> if the type is not mapped as an
        /// object or is created through a constructor that has parameters.
        /// </value>
        public virtual Func<object>? CreateObject => null;

        /// <summary>
        /// Gets a value that indicates whether a value of <see cref="Type"/> can be <see langword="null"/>.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if <see cref="Type"/> is a reference type or a <see cref="Nullable{T}"/>; otherwise, <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// Only a nillable type can be read from an element marked <c>xsi:nil="true"</c> or written from a <see langword="null"/> value.
        /// How a <see langword="null"/> value is written depends on <see cref="XmlSerializerOptions.NilHandling"/>.
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
        /// The serialized members of an object type, in the order they are written. The list is empty for a type that is not mapped as an object.
        /// </value>
        public virtual IList<XmlPropertyInfo> Properties => EmptyProperties;

        /// <summary>
        /// Gets the name of the document element that a value of <see cref="Type"/> is serialized as.
        /// </summary>
        /// <value>
        /// The name of the document element.
        /// </value>
        public XmlName RootName { get; }

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
            var local = root?.ElementName is { Length: > 0 } name
                ? name
                : options.ConvertElementName(XmlBuiltInConverters.GetXmlTypeName(type));
            return new XmlName(local, root?.Namespace ?? options.DefaultNamespace);
        }

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
        /// Reads the element the reader is on, whatever its name, and returns the value boxed.
        /// </summary>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <returns>The value.</returns>
        internal abstract object? ReadElementAsObject(ref Utf8XmlReader reader);

        /// <summary>
        /// Reads the document element of <see cref="Type"/> and returns the value boxed.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The value.</returns>
        internal abstract object? ReadRootAsObject(ref Utf8XmlReader reader);

        /// <summary>
        /// Starts the element named for <see cref="Type"/> without an element frame, when the writer is inside an
        /// element and the name can be written from bytes built ahead of time.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <returns>The end tag to pass to <see cref="Utf8XmlWriter.WriteLeafEnd"/>, or null when nothing was written.</returns>
        /// <remarks>
        /// For an item of a collection that is the document root. The caller writes exactly one value.
        /// </remarks>
        internal byte[]? TryWriteLeafStart(Utf8XmlWriter writer)
        {
            if (_rootLeafOpenTag is null || writer.CurrentDepth == 0)
            {
                return null;
            }

            writer.WriteLeafStart(_rootLeafOpenTag);
            return _rootLeafCloseTag;
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
