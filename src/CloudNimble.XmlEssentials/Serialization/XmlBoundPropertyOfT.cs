using System;
using System.Collections;
using System.Collections.Generic;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// A member of <typeparamref name="TDeclaring"/> whose value is <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TDeclaring">The declaring CLR type.</typeparam>
    /// <typeparam name="TValue">The member CLR type.</typeparam>
    /// <remarks>
    /// The value is read, held, and written as <typeparamref name="TValue"/>, so a value type is boxed only when
    /// it has to wait in the held-values array for a constructor.
    /// </remarks>
    internal sealed class XmlBoundProperty<TDeclaring, TValue> : XmlBoundProperty
    {

        #region Fields

        private readonly XmlConverter<TValue>? _converter;
        private readonly Func<object, TValue> _get;
        private readonly XmlIgnoreCondition _ignore;
        private readonly bool _isScalar;
        private readonly Action<object, TValue>? _set;
        private readonly XmlTypeInfo<TValue>? _typed;
        private readonly XmlTypeInfo _typeInfo;
        private readonly XmlValueConverter<TValue>? _valueConverter;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a bound member.
        /// </summary>
        /// <param name="info">The public metadata.</param>
        /// <param name="index">The slot this member uses while an object is read.</param>
        /// <param name="utf8LocalName">The UTF-8 local name.</param>
        /// <param name="utf8NamespaceUri">The UTF-8 namespace URI.</param>
        /// <param name="typeInfo">The type info the member is read and written with.</param>
        /// <param name="get">Reads the member from a boxed instance.</param>
        /// <param name="set">Writes the member on a boxed instance, or null when it is read-only after construction.</param>
        /// <param name="ignore">When the member is omitted on write.</param>
        /// <param name="constructorIndex">The constructor parameter index, or <c>-1</c>.</param>
        /// <param name="isCData">Whether simple content is written as CDATA.</param>
        /// <param name="collection">The collection mapping, or null.</param>
        /// <param name="polymorphism">The derived types the member may hold.</param>
        internal XmlBoundProperty(
            XmlPropertyInfo info,
            int index,
            byte[] utf8LocalName,
            byte[] utf8NamespaceUri,
            XmlTypeInfo typeInfo,
            Func<object, TValue> get,
            Action<object, TValue>? set,
            XmlIgnoreCondition ignore,
            int constructorIndex,
            bool isCData,
            XmlCollectionBinding? collection,
            XmlPolymorphism polymorphism)
            : base(info, index, utf8LocalName, utf8NamespaceUri, set is not null, constructorIndex, isCData, collection, polymorphism)
        {
            _typeInfo = typeInfo;
            _typed = typeInfo as XmlTypeInfo<TValue>;
            _converter = _typed?.Converter;
            _valueConverter = _converter as XmlValueConverter<TValue>;
            _get = get;
            _set = set;
            _ignore = ignore;
            _isScalar = XmlBuiltInConverters.IsScalar(typeof(TValue));
        }

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override void Read(ref Utf8XmlReader reader, object? instance, object?[]? values)
        {
            TValue? value;
            if (_valueConverter is not null && !reader.HasAttributes)
            {
                // A built-in scalar with no attributes cannot be nil and has no derived types: read it directly.
                Store(instance, values, _valueConverter.Read(ref reader, typeof(TValue), _typeInfo.Options));
                return;
            }

            if (Collection is { } collection)
            {
                var builder = collection.CreateBuilder();
                if (!reader.IsEmptyElement)
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType is XmlTokenType.EndElement)
                        {
                            break;
                        }

                        if (reader.TokenType is XmlTokenType.StartElement)
                        {
                            ReadItem(ref reader, builder);
                        }
                    }
                }

                value = (TValue)collection.Finish(builder);
            }
            else if (Polymorphism.IsActive)
            {
                value = (TValue?)Polymorphism.ReadValue(ref reader, _typeInfo);
            }
            else if (_typed is not null)
            {
                value = _typed.ReadElement(ref reader);
            }
            else
            {
                value = (TValue?)_typeInfo.ReadElementAsObject(ref reader);
            }

            if (IsRequired && XmlConverterHelpers.IsNull(value))
            {
                throw new XmlException($"The required member '{Info.Name.LocalName}' cannot be nil.");
            }

            Store(instance, values, value);
        }

        /// <inheritdoc />
        internal override void ReadAttribute(ref Utf8XmlReader reader, object? instance, object?[]? values)
        {
            Store(instance, values, _converter!.ReadAsAttribute(ref reader, typeof(TValue), _typeInfo.Options));
        }

        /// <inheritdoc />
        internal override void ReadText(ReadOnlySpan<byte> utf8, object? instance, object?[]? values)
        {
            Store(instance, values, _converter!.ReadLexical(utf8, _typeInfo.Options));
        }

        /// <inheritdoc />
        internal override void SetHeldValue(object instance, object? value)
        {
            _set!(instance, value is null ? default! : (TValue)value);
        }

        /// <summary>
        /// Sets <paramref name="value"/> on the instance, or holds it for the constructor.
        /// </summary>
        /// <param name="instance">The instance to set the member on, or null when values are held.</param>
        /// <param name="values">The held values, one slot per member, or null when the instance exists.</param>
        /// <param name="value">The value.</param>
        internal void Store(object? instance, object?[]? values, TValue? value)
        {
            if (values is not null)
            {
                values[Index] = value;
                return;
            }

            _set?.Invoke(instance!, value!);
        }

        /// <inheritdoc />
        internal override void StoreCollection(object builder, object? instance, object?[]? values)
        {
            Store(instance, values, (TValue)Collection!.Finish(builder));
        }

        /// <inheritdoc />
        internal override void Write(Utf8XmlWriter writer, object instance, XmlSerializerOptions options)
        {
            var value = _get(instance);
            if (Collection is not null)
            {
                WriteCollection(writer, value, options);
                return;
            }

            if (XmlConverterHelpers.IsNull(value))
            {
                WriteNull(writer, options);
                return;
            }

            if (_ignore is XmlIgnoreCondition.WhenWritingDefault && EqualityComparer<TValue>.Default.Equals(value, default))
            {
                return;
            }

            if (IsAttribute)
            {
                if (_valueConverter is not null)
                {
                    if (AttributeHead is not null)
                    {
                        writer.WriteStartAttributeTrusted(Utf8LocalName, AttributeHead);
                    }
                    else
                    {
                        writer.WriteStartAttributeUtf8(Utf8LocalName, [], Utf8NamespaceUri);
                    }

                    _valueConverter.WriteValue(writer, value, options);
                    writer.WriteEndAttribute();
                    return;
                }

                _converter!.WriteAsAttribute(writer, Info.Name, value, options);
                return;
            }

            if (IsText)
            {
                if (IsCData)
                {
                    writer.WriteCData(value as string ?? value.ToString()!);
                    return;
                }

                _converter!.Write(writer, value, options);
                return;
            }

            if (_valueConverter is not null && LeafOpenTag is not null && writer.CurrentDepth > 0)
            {
                writer.WriteLeafStart(LeafOpenTag);
                _valueConverter.WriteValue(writer, value, options);
                writer.WriteLeafEnd(LeafCloseTag);
                return;
            }

            if (Polymorphism.Derived.Length > 0 && Polymorphism.TryGetDerivedWrite(value.GetType()) is { } derived)
            {
                if (Polymorphism.Placement is XmlDiscriminatorPlacement.ElementName)
                {
                    writer.WriteStartElement(derived.Utf8Name);
                }
                else
                {
                    WriteStart(writer);
                    XmlConverterHelpers.WriteDiscriminator(writer, Polymorphism.Placement, Polymorphism.Utf8DiscriminatorAttributeName, derived.Discriminator);
                }

                derived.Info.WriteContentAsObject(writer, value);
                writer.WriteEndElement();
                return;
            }

            WriteStart(writer);
            if (!_isScalar && options.ReferenceHandler is { } handler && XmlReferenceScope.Current is { } resolver)
            {
                var id = resolver.GetReference(value, out var alreadyExists);
                if (alreadyExists)
                {
                    WriteReference(writer, handler, id);
                    writer.WriteEndElement();
                    return;
                }
            }

            if (_converter is not null)
            {
                _converter.Write(writer, value, options);
            }
            else
            {
                _typeInfo.WriteContentAsObject(writer, value);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes a collection member as repeated elements, or as a wrapper plus items.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The collection, or null.</param>
        /// <param name="options">The serializer options.</param>
        internal void WriteCollection(Utf8XmlWriter writer, TValue? value, XmlSerializerOptions options)
        {
            var collection = Collection!;
            if (XmlConverterHelpers.IsNull(value))
            {
                if (!collection.Wrapped || _ignore is XmlIgnoreCondition.WhenWritingNull)
                {
                    return;
                }

                WriteStart(writer);
                if (options.NilHandling is XmlNilHandling.WriteXsiNil)
                {
                    writer.WriteNilValue();
                }

                writer.WriteEndElement();
                return;
            }

            if (!collection.Wrapped && value is ICollection { Count: 0 })
            {
                return;
            }

            if (collection.Mixed)
            {
                foreach (var item in (IEnumerable)value)
                {
                    if (item is XmlMixedText text)
                    {
                        writer.WriteStringValue(text.Text);
                        continue;
                    }

                    if (item is XmlMixedElement { Element: { } element })
                    {
                        element.WriteTo(writer);
                    }
                }

                return;
            }

            if (collection.Wrapped)
            {
                WriteStart(writer);
            }

            collection.WriteItems(writer, value, this, options);
            if (collection.Wrapped)
            {
                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Writes a member whose value is null: nothing, an empty element, or an element marked <c>xsi:nil</c>.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="options">The serializer options.</param>
        internal void WriteNull(Utf8XmlWriter writer, XmlSerializerOptions options)
        {
            if (IsAttribute || IsText || _ignore is XmlIgnoreCondition.WhenWritingNull or XmlIgnoreCondition.WhenWritingDefault || options.NilHandling is XmlNilHandling.Omit)
            {
                return;
            }

            WriteStart(writer);
            if (options.NilHandling is XmlNilHandling.WriteXsiNil)
            {
                writer.WriteNilValue();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes the content of an element whose value was already written: a reference to it, or nil.
        /// </summary>
        /// <param name="writer">The writer, inside the member start tag.</param>
        /// <param name="handler">The reference handler in use.</param>
        /// <param name="id">The identifier the value was written under.</param>
        internal static void WriteReference(Utf8XmlWriter writer, XmlReferenceHandler handler, string id)
        {
            if (handler == XmlReferenceHandler.Preserve && id.Length > 0)
            {
                writer.WriteStartAttribute(null, "ref", XmlNamespaces.Refs);
                writer.WriteStringValue(id);
                writer.WriteEndAttribute();
                return;
            }

            writer.WriteNilValue();
        }

        #endregion

    }

}
