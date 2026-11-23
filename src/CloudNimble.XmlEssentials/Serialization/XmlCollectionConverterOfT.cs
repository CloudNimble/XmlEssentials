using System;
using System.Diagnostics.CodeAnalysis;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Reads and writes a collection as the document element, one child element per item.
    /// </summary>
    /// <typeparam name="TCollection">The collection type.</typeparam>
    internal sealed class XmlCollectionConverter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TCollection> : XmlConverter<TCollection>
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
        public XmlCollectionConverter(XmlSerializerOptions options, Type elementType)
        {
            _items = XmlCollectionBinding.Create(typeof(TCollection), elementType, wrapped: true, [], options.GetTypeInfo(elementType), mixed: false);
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override TCollection? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            var builder = _items.CreateBuilder();
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
                        _items.ReadRootItem(ref reader, builder);
                    }
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
