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
    /// <see cref="IDictionary{TKey, TValue}"/> is read into a <see cref="Dictionary{TKey, TValue}"/>. The key
    /// <c>Key</c> itself is written in the pair form, because an element named <c>Key</c> starts a pair when
    /// the document is read.
    /// </remarks>
    internal sealed class XmlDictionaryConverter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TDictionary, TKey, TValue> : XmlConverter<TDictionary>
        where TDictionary : IDictionary<TKey, TValue>
        where TKey : notnull
    {

        #region Fields

        private readonly bool _isInterface;
        private readonly XmlTypeInfo<TKey> _keyInfo;
        private readonly XmlTypeInfo<TValue> _valueInfo;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the converter.
        /// </summary>
        /// <param name="options">The serializer options.</param>
        public XmlDictionaryConverter(XmlSerializerOptions options)
        {
            _isInterface = typeof(TDictionary).IsInterface;
            _keyInfo = options.GetTypeInfo<TKey>();
            _valueInfo = options.GetTypeInfo<TValue>();
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override TDictionary? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            var map = _isInterface ? (TDictionary)(object)new Dictionary<TKey, TValue>() : Activator.CreateInstance<TDictionary>();
            if (!reader.IsEmptyElement)
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
                        var key = _keyInfo.ReadElement(ref reader);
                        TValue? value = default;
                        if (reader.Read() && reader.TokenType is XmlTokenType.StartElement && reader.LocalNameEquals("Value"u8))
                        {
                            value = _valueInfo.ReadElement(ref reader);
                        }

                        if (key is not null)
                        {
                            map[key] = value!;
                        }

                        continue;
                    }

                    var name = Encoding.UTF8.GetString(reader.LocalNameSpan);
                    if (name is not TKey named)
                    {
                        throw new XmlException($"The element '{name}' is not an entry of a dictionary keyed by '{typeof(TKey)}'.");
                    }

                    map[named] = _valueInfo.ReadElement(ref reader)!;
                }
            }

            return map;
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

        /// <summary>
        /// Writes one entry: an element named for the key, or a <c>Key</c> and <c>Value</c> pair.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="policy">The naming policy applied to string keys, or null.</param>
        /// <param name="options">The serializer options.</param>
        internal void WriteEntry(Utf8XmlWriter writer, TKey key, TValue value, XmlNamingPolicy? policy, XmlSerializerOptions options)
        {
            if (key is string text)
            {
                var mapped = policy is null ? text : policy.ConvertName(text);
                if (mapped != "Key" && XmlCollectionShape.IsNcName(mapped))
                {
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
        /// Writes a value and closes the element that holds it. A null value leaves the element empty.
        /// </summary>
        /// <param name="writer">The writer, inside the start tag of the element that holds the value.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The serializer options.</param>
        internal void WriteValue(Utf8XmlWriter writer, TValue value, XmlSerializerOptions options)
        {
            if (!XmlConverterHelpers.IsNull(value))
            {
                _valueInfo.Converter.Write(writer, value, options);
            }

            writer.WriteEndElement();
        }

        #endregion

    }

}
