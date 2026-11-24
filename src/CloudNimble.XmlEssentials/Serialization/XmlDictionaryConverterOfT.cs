using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Reads and writes a dictionary. A string key that is an XML name becomes the name of the element that holds
    /// its value; any other key is written as a <c>Key</c> element followed by a <c>Value</c> element.
    /// </summary>
    /// <typeparam name="TDictionary">The dictionary type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <remarks>
    /// Keys and values are read, held, and written as their own types, so neither is boxed. A member declared as
    /// <see cref="IDictionary{TKey, TValue}"/> or <see cref="IReadOnlyDictionary{TKey, TValue}"/> is read into a
    /// <see cref="Dictionary{TKey, TValue}"/>, and an immutable, frozen, or read-only dictionary is built from one. The key
    /// <c>Key</c> itself is written in the pair form, because an element named <c>Key</c> starts a pair when
    /// the document is read.
    /// </remarks>
    internal sealed class XmlDictionaryConverter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDictionary, TKey, TValue> : XmlConverter<TDictionary>
        where TDictionary : IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : notnull
    {

        #region Fields

        /// <summary>
        /// The longest key, in characters, that is written as a leaf element.
        /// </summary>
        internal const int MaxLeafKeyLength = 32;

        private static readonly bool s_canPopulate = XmlCollectionShape.CanPopulateDictionary<TDictionary, TKey, TValue>();
        private readonly Func<IDictionary<TKey, TValue>> _create;
        private readonly Func<IDictionary<TKey, TValue>, TDictionary> _finish;
        private readonly XmlTypeInfo<TKey> _keyInfo;
        private readonly XmlTypeInfo<TValue> _valueInfo;

        /// <summary>
        /// The value's converter when it is one of the built-in scalar converters, which write one text value and nothing else.
        /// </summary>
        private readonly XmlValueConverter<TValue>? _valueLeafConverter;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the converter.
        /// </summary>
        /// <param name="options">The serializer options.</param>
        public XmlDictionaryConverter(XmlSerializerOptions options)
        {
            _create = XmlCollectionShape.CreateDictionaryFactory<TDictionary, TKey, TValue>(out _finish);
            _keyInfo = options.GetTypeInfo<TKey>();
            _valueInfo = options.GetTypeInfo<TValue>();
            _valueLeafConverter = _valueInfo.Converter as XmlValueConverter<TValue>;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override TDictionary? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            var map = _create();
            ReadEntries(ref reader, map);
            return _finish(map);
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, TDictionary value, XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(value);
            var policy = options.DictionaryKeyPolicy ?? options.PropertyNamingPolicy;
            if (value is Dictionary<TKey, TValue> dictionary)
            {
                foreach (var entry in dictionary)
                {
                    WriteEntry(writer, entry.Key, entry.Value, policy, options);
                }

                return;
            }

            foreach (var entry in value)
            {
                WriteEntry(writer, entry.Key, entry.Value, policy, options);
            }
        }

        #endregion

        #region Private Methods

        /// <inheritdoc />
        /// <remarks>
        /// Entries can be added to a dictionary declared as <see cref="IDictionary{TKey, TValue}"/>, or as a class
        /// that implements it and has a public parameterless constructor. An immutable, frozen or read-only
        /// dictionary cannot take them.
        /// </remarks>
        internal override bool CanPopulate => s_canPopulate;

        /// <inheritdoc />
        internal override void Populate(ref Utf8XmlReader reader, object existing, XmlSerializerOptions options)
        {
            if (existing is not IDictionary<TKey, TValue> { IsReadOnly: false } map)
            {
                throw new NotSupportedException($"The dictionary of type '{existing.GetType()}' is read-only and cannot be populated.");
            }

            ReadEntries(ref reader, map);
        }

        /// <summary>
        /// Reads the entries of the element <paramref name="reader"/> is on into <paramref name="map"/>.
        /// </summary>
        /// <param name="reader">The reader, on the start tag of the dictionary.</param>
        /// <param name="map">The dictionary that takes the entries. An entry whose key it already holds is given the value that was read.</param>
        internal void ReadEntries(ref Utf8XmlReader reader, IDictionary<TKey, TValue> map)
        {
            if (!reader.IsEmptyElement)
            {
                string? entry = null;
                try
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType is XmlTokenType.EndElement)
                        {
                            break;
                        }

                        if (reader.TokenType is not XmlTokenType.StartElement)
                        {
                            continue;
                        }

                        if (reader.LocalNameEquals("Key"u8))
                        {
                            entry = "Key";
                            var key = _keyInfo.ReadElement(ref reader);
                            TValue? value = default;
                            if (reader.Read() && reader.TokenType is XmlTokenType.StartElement && reader.LocalNameEquals("Value"u8))
                            {
                                entry = "Value";
                                value = _valueInfo.ReadElement(ref reader);
                            }

                            entry = null;
                            if (key is not null)
                            {
                                map[key] = value!;
                            }

                            continue;
                        }

                        var name = Encoding.UTF8.GetString(reader.LocalNameSpan);
                        if (name is not TKey named)
                        {
                            throw XmlException.Create($"The element '{name}' is not an entry of a dictionary keyed by '{typeof(TKey)}'.");
                        }

                        entry = name;
                        map[named] = _valueInfo.ReadElement(ref reader)!;
                        entry = null;
                    }
                }
                catch (XmlException ex)
                {
                    // The key or the value of an entry could not be read. An entry is an element named for its key.
                    reader.Locate(ex);
                    if (entry is not null)
                    {
                        ex.AddElement(entry);
                    }

                    throw;
                }
            }
        }

        /// <summary>
        /// Writes one entry: an element named for the key, or a <c>Key</c> and <c>Value</c> pair.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="policy">The naming policy applied to string keys, or null.</param>
        /// <param name="options">The serializer options.</param>
        /// <remarks>
        /// A short key with a scalar value is written as a leaf element, as a scalar member of an object is: the
        /// start and end tags are built on the stack from the key, which <see cref="XmlCollectionShape.IsNcName"/>
        /// has just shown to be an ASCII NCName, and no element frame is pushed for it.
        /// </remarks>
        internal void WriteEntry(Utf8XmlWriter writer, TKey key, TValue value, XmlNamingPolicy? policy, XmlSerializerOptions options)
        {
            if (key is string text)
            {
                var mapped = policy is null ? text : policy.ConvertName(text);
                if (mapped != "Key" && XmlCollectionShape.IsNcName(mapped))
                {
                    if (_valueLeafConverter is not null && mapped.Length <= MaxLeafKeyLength && writer.CurrentDepth > 0 && !XmlConverterHelpers.IsNull(value))
                    {
                        WriteLeafEntry(writer, mapped, value, options);
                        return;
                    }

                    writer.WriteStartElement(mapped);
                    WriteValue(writer, value, options);
                    return;
                }
            }

            writer.WriteStartElement("Key"u8);
            _keyInfo.Converter.Write(writer, key, options);
            writer.WriteEndElement();
            writer.WriteStartElement("Value"u8);
            WriteValue(writer, value, options);
        }

        /// <summary>
        /// Writes <c>&lt;name&gt;value&lt;/name&gt;</c> without an element frame.
        /// </summary>
        /// <param name="writer">The writer, inside an element.</param>
        /// <param name="name">The element name: an ASCII NCName of at most <see cref="MaxLeafKeyLength"/> characters.</param>
        /// <param name="value">The value, which is not null.</param>
        /// <param name="options">The serializer options.</param>
        internal void WriteLeafEntry(Utf8XmlWriter writer, string name, TValue value, XmlSerializerOptions options)
        {
            // The buffer holds "<<name>". The start tag is everything after the first byte; changing the
            // second byte to '/' then makes the whole buffer the end tag.
            Span<byte> tags = stackalloc byte[MaxLeafKeyLength + 3];
            tags = tags[..(name.Length + 3)];
            tags[0] = (byte)'<';
            tags[1] = (byte)'<';
            for (var i = 0; i < name.Length; i++)
            {
                tags[i + 2] = (byte)name[i];
            }

            tags[^1] = (byte)'>';
            writer.WriteLeafStart(tags[1..]);
            _valueLeafConverter!.WriteValue(writer, value, options);
            tags[1] = (byte)'/';
            writer.WriteLeafEnd(tags);
        }

        /// <summary>
        /// Writes a value and closes the element that holds it. A null value leaves the element empty.
        /// </summary>
        /// <param name="writer">The writer, inside the start tag of the element that holds the value.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The serializer options.</param>
        internal void WriteValue(Utf8XmlWriter writer, TValue value, XmlSerializerOptions options)
        {
            if (XmlConverterHelpers.IsNull(value))
            {
                // A null value is an entry marked nil. Without the mark a null string would read back as an
                // empty one, and a null number would not read at all.
                writer.WriteNilValue();
            }
            else
            {
                _valueInfo.Converter.Write(writer, value, options);
            }

            writer.WriteEndElement();
        }

        #endregion

    }

}
