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
        private readonly Func<TDeclaring, TValue> _get;
        private XmlHeldBox<TValue>? _held;
        private readonly XmlIgnoreCondition _ignore;
        private readonly bool _isScalar;
        private readonly XmlPropertySetter<TDeclaring, TValue>? _set;
        private readonly XmlUnbox<TDeclaring>? _unbox;
        private readonly XmlTypeInfo<TValue>? _typed;
        private readonly XmlTypeInfo _typeInfo;
        private readonly XmlValueConverter<TValue>? _valueConverter;

        /// <summary>
        /// The type of the member. A converter is handed it for every value it reads, and in code that is shared
        /// between reference types <c>typeof</c> is a lookup at run time where this is a field.
        /// </summary>
        private readonly Type _valueType = typeof(TValue);

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
        /// <param name="get">Reads the member.</param>
        /// <param name="set">Writes the member, or null when it is read-only after construction.</param>
        /// <param name="unbox">Reaches the value inside a box, when <typeparamref name="TDeclaring"/> is a value type; otherwise null.</param>
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
            Func<TDeclaring, TValue> get,
            XmlPropertySetter<TDeclaring, TValue>? set,
            XmlUnbox<TDeclaring>? unbox,
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
            _unbox = unbox;
            _ignore = ignore;
            _isScalar = XmlBuiltInConverters.IsScalar(typeof(TValue));
            ReadsWholeLeaf = _valueConverter is not null && LeafOpenTag is not null && collection is null;
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
                Store(instance, values, _valueConverter.Read(ref reader, _valueType, _typeInfo.Options));
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
                        try
                        {
                            // An item of a derived type is told apart by its element, so every item is looked at.
                            var openTag = Polymorphism.IsActive ? null : collection.ItemLeafOpenTag;
                            while (true)
                            {
                                collection.ReadWholeItems(ref reader, builder, openTag, collection.ItemLeafCloseTag);
                                if (!reader.Read() || reader.TokenType is XmlTokenType.EndElement)
                                {
                                    break;
                                }

                                if (reader.TokenType is XmlTokenType.StartElement)
                                {
                                    ReadItem(ref reader, builder);
                                }
                            }
                        }
                        catch (XmlException ex)
                        {
                            // An item could not be read. Its position is the number of items read before it.
                            reader.Locate(ex);
                            ex.AddItem(System.Text.Encoding.UTF8.GetString(collection.Utf8ItemName), ((System.Collections.ICollection)builder).Count);
                            throw;
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
            else if (Populates && !(reader.HasAttributes && reader.GetNil()) && (object?)_get((TDeclaring)instance!) is { } existing)
            {
                // A value type is populated in its box, and the box is then copied back through the setter the
                // binder required. A nil element and a member that holds nothing are read as any member is.
                _typeInfo.Converter.Populate(ref reader, existing, _typeInfo.Options);
                if (typeof(TValue).IsValueType)
                {
                    XmlMemberAccess.Set(_set!, _unbox, instance!, (TValue)existing);
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

            StoreChecked(instance, values, value);
        }

        /// <inheritdoc />
        internal override bool HasValueConverter => _valueConverter is not null;

        /// <inheritdoc />
        internal override XmlTypeInfo ValueTypeInfo => _typeInfo;

        /// <inheritdoc />
        internal override void ReadNil(object? instance, object?[]? values)
        {
            StoreChecked(instance, values, default);
        }

        /// <inheritdoc />
        internal override void StoreValue(object? instance, object?[]? values, object? value)
        {
            StoreChecked(instance, values, (TValue?)value);
        }

        /// <summary>
        /// Stores a value that was read from the member's element, after the checks a null has to pass.
        /// </summary>
        /// <param name="instance">The instance to set the member on, or null when values are held.</param>
        /// <param name="values">The held values, or null when the instance exists.</param>
        /// <param name="value">The value, which is null for an element marked nil.</param>
        /// <exception cref="XmlException">The member is required, or does not accept null, and the value is null.</exception>
        /// <exception cref="InvalidOperationException">The member is populated, has no setter, and the value is null.</exception>
        internal void StoreChecked(object? instance, object?[]? values, TValue? value)
        {
            if (IsRequired && XmlConverterHelpers.IsNull(value))
            {
                throw XmlException.Create($"The required member '{Info.Name.LocalName}' cannot be nil.");
            }

            if (NullReadError is not null && XmlConverterHelpers.IsNull(value))
            {
                throw XmlException.Create(NullReadError);
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

            // A built-in scalar is read from the bytes of the value. That is what its converter does with the
            // reader, one call further in.
            Store(instance, values, _valueConverter is not null
                ? _valueConverter.Parse(reader.ValueSpan, _typeInfo.Options)
                : _converter!.ReadAsAttribute(ref reader, _valueType, _typeInfo.Options));
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
        internal override void UseHeldBox(ReadOnlySpan<Type> parameterTypes)
        {
            // A box is written again for the next object, so it may only ever be handed to something that
            // copies the value out of it: a parameter of exactly this type, or the member's own setter. A
            // parameter typed as object or as an interface would keep the box itself. Nullable<T> boxes as
            // T or as null, so it has no box of its own to reuse, and a collection member's slot holds its
            // builder while the element is read.
            var type = typeof(TValue);
            if (!type.IsValueType || Nullable.GetUnderlyingType(type) is not null || Collection is not null || HeldBoxFactory is null)
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

            _held = (XmlHeldBox<TValue>)HeldBoxFactory();
        }

        /// <inheritdoc />
        internal override void SetHeldValue(object instance, object? value)
        {
            XmlMemberAccess.Set(_set!, _unbox, instance, value is null ? default! : (TValue)value);
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
            var existing = _get((TDeclaring)instance);
            if (XmlConverterHelpers.IsNull(existing))
            {
                if (_set is not null)
                {
                    XmlMemberAccess.Set(_set, _unbox, instance, (TValue)Collection!.Finish(builder));
                }

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
                return collection.CanPopulate() ? null : cannot;
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

            if (_set is not null)
            {
                XmlMemberAccess.Set(_set, _unbox, instance!, value!);
            }
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

            var value = _get((TDeclaring)instance);
            if (NullWriteError is not null && XmlConverterHelpers.IsNull(value) && _ignore is not (XmlIgnoreCondition.WhenWritingNull or XmlIgnoreCondition.WhenWritingDefault))
            {
                // A null the ignore condition leaves out is not written, so it is not refused either.
                throw XmlException.Create(NullWriteError);
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
                        _valueConverter.WriteAttribute(writer, Utf8LocalName, AttributeHead, value, options);
                        return;
                    }

                    writer.WriteStartAttributeUtf8(Utf8LocalName, [], Utf8NamespaceUri);
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
                _valueConverter.WriteLeaf(writer, LeafOpenTag, LeafCloseTag, value, options);
                return;
            }

            if (Polymorphism.Derived.Length > 0 && !XmlConverterHelpers.IsNull(value))
            {
                if (Polymorphism.IsNamed)
                {
                    // The value names its element. Every other value finds its element by its type.
                    Polymorphism.WriteNamed(writer, (XmlNamedValue)(object)value);
                    return;
                }

                if (Polymorphism.TryGetDerivedWrite(value.GetType()) is { } derived)
                {
                    if (Polymorphism.Placement is XmlDiscriminatorPlacement.ElementName)
                    {
                        derived.WriteStart(writer);
                    }
                    else
                    {
                        WriteStart(writer);
                        XmlConverterHelpers.WriteDiscriminator(writer, Polymorphism.Placement, Polymorphism.Utf8DiscriminatorAttributeName, derived.Discriminator);
                    }

                    derived.WriteContent(writer, value);
                    writer.WriteEndElement();
                    return;
                }
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
