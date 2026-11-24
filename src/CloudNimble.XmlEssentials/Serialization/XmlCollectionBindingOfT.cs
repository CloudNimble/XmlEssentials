using System;
using System.Collections.Generic;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Reads and writes the items of a collection of <typeparamref name="TElement"/>.
    /// </summary>
    /// <typeparam name="TElement">The CLR type the collection holds.</typeparam>
    internal sealed class XmlCollectionBinding<TElement> : XmlCollectionBinding
    {

        #region Fields

        private readonly bool _acceptsList;

        /// <summary>
        /// Creates the declared collection from the items read, or null when the list or an array of them is the collection.
        /// </summary>
        private readonly Func<List<TElement>, object>? _create;

        private readonly bool _isArray;
        private readonly XmlTypeInfo<TElement>? _itemInfo;
        private readonly XmlValueConverter<TElement>? _itemValueConverter;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the mapping.
        /// </summary>
        /// <param name="collectionType">The declared collection type.</param>
        /// <param name="wrapped">Whether a wrapper element surrounds the items.</param>
        /// <param name="utf8ItemName">The UTF-8 local name of each item element inside a wrapper.</param>
        /// <param name="itemTypeInfo">The type info used for each item.</param>
        /// <param name="mixed">Whether the collection holds mixed text and child elements.</param>
        public XmlCollectionBinding(Type collectionType, bool wrapped, byte[] utf8ItemName, XmlTypeInfo itemTypeInfo, bool mixed)
            : base(wrapped, utf8ItemName, itemTypeInfo, mixed)
        {
            _isArray = collectionType.IsArray;
            _acceptsList = collectionType.IsAssignableFrom(typeof(List<TElement>));
            _create = _isArray || _acceptsList ? null : XmlCollectionShape.CreateCollectionFactory<TElement>(collectionType);
            _itemInfo = itemTypeInfo as XmlTypeInfo<TElement>;
            _itemValueConverter = _itemInfo?.Converter as XmlValueConverter<TElement>;
        }

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override void AddItem(object builder, object? item)
        {
            ((List<TElement>)builder).Add((TElement)item!);
        }

        /// <inheritdoc />
        internal override bool AddItemsTo(object builder, object target)
        {
            return XmlCollectionShape.AddItems((List<TElement>)builder, target);
        }

        /// <inheritdoc />
        internal override bool CanPopulate(Type collectionType)
        {
            return !Mixed && XmlCollectionShape.CanPopulate<TElement>(collectionType);
        }

        /// <inheritdoc />
        internal override object CreateBuilder()
        {
            return new List<TElement>();
        }

        /// <inheritdoc />
        internal override object Finish(object builder)
        {
            var list = (List<TElement>)builder;
            if (_isArray)
            {
                return list.ToArray();
            }

            return _create is null ? list : _create(list);
        }

        /// <inheritdoc />
        internal override void ReadItem(ref Utf8XmlReader reader, object builder)
        {
            ((List<TElement>)builder).Add(_itemInfo!.ReadElement(ref reader)!);
        }

        /// <inheritdoc />
        internal override void ReadRootItem(ref Utf8XmlReader reader, object builder)
        {
            ((List<TElement>)builder).Add(_itemInfo!.ReadRoot(ref reader)!);
        }

        /// <summary>
        /// Writes one item as an element of <paramref name="property"/>.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="item">The item.</param>
        /// <param name="property">The member that holds the collection.</param>
        /// <param name="options">The serializer options.</param>
        internal void WriteItem(Utf8XmlWriter writer, TElement item, XmlBoundProperty property, XmlSerializerOptions options)
        {
            if (!XmlConverterHelpers.IsNull(item) && _itemValueConverter is not null && writer.CurrentDepth > 0)
            {
                var openTag = Wrapped ? ItemLeafOpenTag : property.LeafOpenTag;
                if (openTag is not null)
                {
                    writer.WriteLeafStart(openTag);
                    _itemValueConverter.WriteValue(writer, item, options);
                    writer.WriteLeafEnd(Wrapped ? ItemLeafCloseTag : property.LeafCloseTag);
                    return;
                }
            }

            var polymorphism = property.Polymorphism;
            if (polymorphism.Derived.Length > 0 && !XmlConverterHelpers.IsNull(item) && polymorphism.TryGetDerivedWrite(item.GetType()) is { } derived)
            {
                if (polymorphism.Placement is XmlDiscriminatorPlacement.ElementName)
                {
                    writer.WriteStartElement(derived.Utf8Name);
                }
                else
                {
                    WriteItemStart(writer, property);
                    XmlConverterHelpers.WriteDiscriminator(writer, polymorphism.Placement, polymorphism.Utf8DiscriminatorAttributeName, derived.Discriminator);
                }

                derived.Info.WriteContentAsObject(writer, item);
                writer.WriteEndElement();
                return;
            }

            WriteItemStart(writer, property);
            if (!XmlConverterHelpers.IsNull(item))
            {
                _itemInfo!.Converter.Write(writer, item, _itemInfo.Options);
            }
            else
            {
                writer.WriteNilValue();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Writes the start tag of one item: the item name inside a wrapper, the member name otherwise.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="property">The member that holds the collection.</param>
        internal void WriteItemStart(Utf8XmlWriter writer, XmlBoundProperty property)
        {
            if (Wrapped)
            {
                writer.WriteStartElement(Utf8ItemName);
                return;
            }

            property.WriteStart(writer);
        }

        /// <inheritdoc />
        internal override void WriteItems(Utf8XmlWriter writer, object collection, XmlBoundProperty property, XmlSerializerOptions options)
        {
            if (collection is List<TElement> list)
            {
                foreach (var item in list)
                {
                    WriteItem(writer, item, property, options);
                }

                return;
            }

            if (collection is TElement[] array)
            {
                foreach (var item in array)
                {
                    WriteItem(writer, item, property, options);
                }

                return;
            }

            foreach (var item in (IEnumerable<TElement>)collection)
            {
                WriteItem(writer, item, property, options);
            }
        }

        /// <inheritdoc />
        internal override void WriteRootItems(Utf8XmlWriter writer, object collection)
        {
            if (collection is List<TElement> list)
            {
                foreach (var item in list)
                {
                    _itemInfo!.WriteRoot(writer, item);
                }

                return;
            }

            if (collection is TElement[] array)
            {
                foreach (var item in array)
                {
                    _itemInfo!.WriteRoot(writer, item);
                }

                return;
            }

            foreach (var item in (IEnumerable<TElement>)collection)
            {
                _itemInfo!.WriteRoot(writer, item);
            }
        }

        #endregion

    }

}
