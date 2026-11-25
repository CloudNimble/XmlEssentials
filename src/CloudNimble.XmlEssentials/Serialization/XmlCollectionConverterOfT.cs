using System;
using System.Diagnostics.CodeAnalysis;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Reads and writes a collection as the document element, one child element per item.
    /// </summary>
    /// <typeparam name="TCollection">The collection type.</typeparam>
    internal sealed class XmlCollectionConverter<TCollection> : XmlConverter<TCollection>
    {

        #region Fields

        private readonly XmlCollectionBinding _items;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the converter.
        /// </summary>
        /// <param name="options">The serializer options.</param>
        /// <param name="elementType">The CLR type the collection holds.</param>
        [RequiresUnreferencedCode(XmlSerializer.RefMsg), RequiresDynamicCode(XmlSerializer.DynMsg)]
        public XmlCollectionConverter(XmlSerializerOptions options, Type elementType)
        {
            _items = XmlCollectionBinding.Create(typeof(TCollection), elementType, wrapped: true, [], options.GetTypeInfo(elementType), mixed: false);
        }

        /// <summary>
        /// Creates the converter from a binding whose item type the caller named at compile time.
        /// </summary>
        /// <param name="items">How the items are read, held, and written.</param>
        /// <remarks>
        /// A generated context uses this constructor. It closes no generic type at run time, which Native AOT
        /// cannot do for an item type that is a value type.
        /// </remarks>
        internal XmlCollectionConverter(XmlCollectionBinding items)
        {
            _items = items;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override TCollection? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            var builder = _items.CreateBuilder();
            if (!reader.IsEmptyElement)
            {
                try
                {
                    var openTag = _items.ItemTypeInfo.RootLeafOpenTag;
                    var closeTag = _items.ItemTypeInfo.RootLeafCloseTag;
                    while (true)
                    {
                        _items.ReadWholeItems(ref reader, builder, openTag, closeTag);
                        if (!reader.Read() || reader.TokenType is XmlTokenType.EndElement)
                        {
                            break;
                        }

                        if (reader.TokenType is XmlTokenType.StartElement)
                        {
                            _items.ReadRootItem(ref reader, builder);
                        }
                    }
                }
                catch (XmlException ex)
                {
                    // An item could not be read. Its position is the number of items read before it.
                    reader.Locate(ex);
                    ex.AddItem(_items.ItemTypeInfo.RootName.LocalName, ((System.Collections.ICollection)builder).Count);
                    throw;
                }
            }

            return (TCollection)_items.Finish(builder);
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, TCollection value, XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(value);
            _items.WriteRootItems(writer, value);
        }

        #endregion

    }

}
