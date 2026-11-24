using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
        private XmlHeldBox<TValue>? _held;
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
            if (NeverStored && !Populates)
            {
                // A member with no setter, or one that is never read, takes nothing from the document. The element
                // is passed over without being read, so what it holds does not have to be a value of the member's type.
                reader.Skip();
                return;
            }

            TValue? value;
            if (_valueConverter is not null && !reader.HasAttributes)
            {
                // A built-in scalar with no attributes cannot be nil and has no derived types: read it directly.
                Store(instance, values, _valueConverter.Read(ref reader, typeof(TValue), _typeInfo.Options));
                return;
            }

            if (Collection is { } collection)
            {
                if (reader.HasAttributes && reader.GetNil())
                {
                    // A wrapper marked nil is a null collection, which is not the same as an empty one.
                    reader.Skip();
                    value = default;
                }
                else
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

                    if (Populates)
                    {
                        PopulateCollection(builder, instance!);
                        return;
                    }

                    value = (TValue)collection.Finish(builder);
                }
            }
            else if (Populates && !(reader.HasAttributes && reader.GetNil()) && (object?)_get(instance!) is { } existing)
            {
                // A value type is populated in its box, and the box is then copied back through the setter the
                // binder required. A nil element and a member that holds nothing are read as any member is.
                _typeInfo.Converter.Populate(ref reader, existing, _typeInfo.Options);
                if (typeof(TValue).IsValueType)
                {
                    _set!(instance!, (TValue)existing);
                }

                return;
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

            if (NullReadError is not null && XmlConverterHelpers.IsNull(value))
            {
                throw new XmlException(NullReadError);
            }

            if (Populates && _set is null && XmlConverterHelpers.IsNull(value))
            {
                // The document asks for null, and a member with no setter cannot be given it.
                throw new InvalidOperationException($"Unable to assign 'null' to the property or field of type '{typeof(TValue)}'.");
            }

            Store(instance, values, value);
        }

        /// <inheritdoc />
        internal override void ReadAttribute(ref Utf8XmlReader reader, object? instance, object?[]? values)
        {
            if (NeverStored)
            {
                return;
            }

            Store(instance, values, _converter!.ReadAsAttribute(ref reader, typeof(TValue), _typeInfo.Options));
        }

        /// <inheritdoc />
        internal override void ReadText(ReadOnlySpan<byte> utf8, object? instance, object?[]? values)
        {
            if (NeverStored)
            {
                return;
            }

            Store(instance, values, _converter!.ReadLexical(utf8, _typeInfo.Options));
        }

        /// <inheritdoc />
        internal override object? CreateHeldBox()
        {
            return _held?.Create();
        }

        /// <inheritdoc />
        internal override void HoldDefault(object?[] values, object? value)
        {
            if (_held is { } held)
            {
                held.Set(values[Index]!, value is null ? default! : (TValue)value);
            }
            else
            {
                values[Index] = value;
            }
        }

        /// <inheritdoc />
        [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "The held box has one constructor and no members found by name.")]
        [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "A value type always has its parameterless constructor.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The reflection resolver already closes this member's own generic type over the same value type. A generated context supplies the box through XmlMetadataServices.CreateValuePropertyInfo, so this is not reached under Native AOT.")]
        internal override void UseHeldBox(ReadOnlySpan<Type> parameterTypes)
        {
            // A box is written again for the next object, so it may only ever be handed to something that
            // copies the value out of it: a parameter of exactly this type, or the member's own setter. A
            // parameter typed as object or as an interface would keep the box itself. Nullable<T> boxes as
            // T or as null, so it has no box of its own to reuse, and a collection member's slot holds its
            // builder while the element is read.
            var type = typeof(TValue);
            if (!type.IsValueType || Nullable.GetUnderlyingType(type) is not null || Collection is not null || (HeldBoxFactory is null && !ReflectsHeldBox))
            {
                return;
            }

            foreach (var parameterType in parameterTypes)
            {
                if (parameterType != type)
                {
                    return;
                }
            }

            _held = HeldBoxFactory is not null
                ? (XmlHeldBox<TValue>)HeldBoxFactory()
                : (XmlHeldBox<TValue>)Activator.CreateInstance(typeof(XmlStructHeldBox<>).MakeGenericType(type))!;
        }

        /// <inheritdoc />
        internal override void SetHeldValue(object instance, object? value)
        {
            _set!(instance, value is null ? default! : (TValue)value);
        }

        /// <summary>
        /// Adds the items that were read to the collection the member holds, or gives the member a new collection
        /// when it holds none.
        /// </summary>
        /// <param name="builder">The builder that holds the items.</param>
        /// <param name="instance">The instance the member belongs to.</param>
        /// <exception cref="NotSupportedException">The collection the member holds cannot take items.</exception>
        /// <remarks>
        /// A member that holds no collection and has no setter stays as it is: there is nothing to populate and no
        /// way to give it a new collection.
        /// </remarks>
        internal void PopulateCollection(object builder, object instance)
        {
            var existing = _get(instance);
            if (XmlConverterHelpers.IsNull(existing))
            {
                _set?.Invoke(instance, (TValue)Collection!.Finish(builder));
                return;
            }

            if (!Collection!.AddItemsTo(builder, existing))
            {
                throw new NotSupportedException($"The collection of type '{existing.GetType()}' held by the member '{Info.Name.LocalName}' is read-only and cannot be populated.");
            }
        }

        /// <inheritdoc />
        internal override string? PopulateRefusal()
        {
            const string cannot = "but it doesn't support populating. This can be either because the property type is immutable or it could use a custom converter.";
            if (_ignore is XmlIgnoreCondition.WhenReading)
            {
                // A member that is never read is not populated either.
                return cannot;
            }

            if (Collection is { } collection)
            {
                return collection.CanPopulate(typeof(TValue)) ? null : cannot;
            }

            if (Polymorphism.IsActive)
            {
                return "but its type allows polymorphic deserialization.";
            }

            if (!_typeInfo.Converter.CanPopulate)
            {
                return cannot;
            }

            return typeof(TValue).IsValueType && _set is null
                ? "but is a value type that doesn't have a setter."
                : null;
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
                if (_held is { } held)
                {
                    held.Set(values[Index]!, value!);
                }
                else
                {
                    values[Index] = value;
                }

                return;
            }

            _set?.Invoke(instance!, value!);
        }

        /// <inheritdoc />
        internal override void StoreCollection(object builder, object? instance, object?[]? values)
        {
            if (Populates)
            {
                PopulateCollection(builder, instance!);
                return;
            }

            Store(instance, values, (TValue)Collection!.Finish(builder));
        }

        /// <inheritdoc />
        internal override void Write(Utf8XmlWriter writer, object instance, XmlSerializerOptions options)
        {
            if (_ignore is XmlIgnoreCondition.WhenWriting)
            {
                return;
            }

            var value = _get(instance);
            if (NullWriteError is not null && XmlConverterHelpers.IsNull(value) && _ignore is not (XmlIgnoreCondition.WhenWritingNull or XmlIgnoreCondition.WhenWritingDefault))
            {
                // A null the ignore condition leaves out is not written, so it is not refused either.
                throw new XmlException(NullWriteError);
            }

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
                    XmlConverterHelpers.WriteCDataSections(writer, value as string ?? value.ToString()!);
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
                // A null wrapped collection is left out under the same conditions as any other null member.
                if (!collection.Wrapped || _ignore is XmlIgnoreCondition.WhenWritingNull or XmlIgnoreCondition.WhenWritingDefault)
                {
                    return;
                }

                WriteStart(writer);
                writer.WriteNilValue();
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
            if (IsAttribute || IsText || _ignore is XmlIgnoreCondition.WhenWritingNull or XmlIgnoreCondition.WhenWritingDefault)
            {
                return;
            }

            WriteStart(writer);
            writer.WriteNilValue();
            writer.WriteEndElement();
        }

        #endregion

    }

}
