using System;
using System.Diagnostics.CodeAnalysis;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// How a collection is read and written: its item name, item type, and whether it has a wrapper element.
    /// </summary>
    /// <remarks>
    /// The members that touch items are implemented once per item type by <see cref="XmlCollectionBinding{TElement}"/>,
    /// so items are read, held, and written without boxing.
    /// </remarks>
    internal abstract class XmlCollectionBinding
    {

        #region Fields

        /// <summary>
        /// The type info used for each item.
        /// </summary>
        internal readonly XmlTypeInfo ItemTypeInfo;

        /// <summary>
        /// The whole end tag of an item inside a wrapper, when it can be written from bytes built ahead of time, or null.
        /// </summary>
        internal readonly byte[]? ItemLeafCloseTag;

        /// <summary>
        /// The whole start tag of an item inside a wrapper, when the item name is an ASCII NCName, or null.
        /// </summary>
        internal readonly byte[]? ItemLeafOpenTag;

        /// <summary>
        /// Whether the collection holds mixed text and child elements.
        /// </summary>
        internal readonly bool Mixed;

        /// <summary>
        /// The UTF-8 local name of each item element inside a wrapper.
        /// </summary>
        internal readonly byte[] Utf8ItemName;

        /// <summary>
        /// Whether a wrapper element surrounds the items.
        /// </summary>
        internal readonly bool Wrapped;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the mapping.
        /// </summary>
        /// <param name="wrapped">Whether a wrapper element surrounds the items.</param>
        /// <param name="utf8ItemName">The UTF-8 local name of each item element inside a wrapper.</param>
        /// <param name="itemTypeInfo">The type info used for each item.</param>
        /// <param name="mixed">Whether the collection holds mixed text and child elements.</param>
        private protected XmlCollectionBinding(bool wrapped, byte[] utf8ItemName, XmlTypeInfo itemTypeInfo, bool mixed)
        {
            Wrapped = wrapped;
            Utf8ItemName = utf8ItemName;
            ItemTypeInfo = itemTypeInfo;
            Mixed = mixed;
            if (wrapped && XmlChars.IsShortAsciiNcName(utf8ItemName, utf8ItemName.Length))
            {
                ItemLeafOpenTag = [(byte)'<', .. utf8ItemName, (byte)'>'];
                ItemLeafCloseTag = [(byte)'<', (byte)'/', .. utf8ItemName, (byte)'>'];
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates the mapping for a collection of <paramref name="elementType"/>.
        /// </summary>
        /// <param name="collectionType">The declared collection type.</param>
        /// <param name="elementType">The CLR type the collection holds.</param>
        /// <param name="wrapped">Whether a wrapper element surrounds the items.</param>
        /// <param name="utf8ItemName">The UTF-8 local name of each item element inside a wrapper.</param>
        /// <param name="itemTypeInfo">The type info used for each item.</param>
        /// <param name="mixed">Whether the collection holds mixed text and child elements.</param>
        /// <returns>The mapping.</returns>
        [UnconditionalSuppressMessage("Trimming", "IL2055")]
        [UnconditionalSuppressMessage("Trimming", "IL2067")]
        [UnconditionalSuppressMessage("Trimming", "IL2072")]
        [UnconditionalSuppressMessage("AOT", "IL3050")]
        internal static XmlCollectionBinding Create(Type collectionType, Type elementType, bool wrapped, byte[] utf8ItemName, XmlTypeInfo itemTypeInfo, bool mixed)
        {
            var bindingType = typeof(XmlCollectionBinding<>).MakeGenericType(elementType);
            return (XmlCollectionBinding)Activator.CreateInstance(bindingType, collectionType, wrapped, utf8ItemName, itemTypeInfo, mixed)!;
        }

        /// <summary>
        /// Adds the items of a builder to the collection a member already holds.
        /// </summary>
        /// <param name="builder">The builder from <see cref="CreateBuilder"/>.</param>
        /// <param name="target">The collection the member holds.</param>
        /// <returns><see langword="true"/> when the items were added; <see langword="false"/> when the collection cannot take items.</returns>
        internal abstract bool AddItemsTo(object builder, object target);

        /// <summary>
        /// Returns whether a member declared as <paramref name="collectionType"/> can have items added to the collection it holds.
        /// </summary>
        /// <param name="collectionType">The declared collection type.</param>
        /// <returns><see langword="true"/> when the declared type takes items.</returns>
        internal abstract bool CanPopulate(Type collectionType);

        /// <summary>
        /// Adds <paramref name="item"/> to a builder.
        /// </summary>
        /// <param name="builder">The builder from <see cref="CreateBuilder"/>.</param>
        /// <param name="item">The item.</param>
        internal abstract void AddItem(object builder, object? item);

        /// <summary>
        /// Creates an empty builder that items are added to while the collection is read.
        /// </summary>
        /// <returns>The builder.</returns>
        internal abstract object CreateBuilder();

        /// <summary>
        /// Converts a builder to the declared collection type.
        /// </summary>
        /// <param name="builder">The builder from <see cref="CreateBuilder"/>.</param>
        /// <returns>The collection.</returns>
        internal abstract object Finish(object builder);

        /// <summary>
        /// Reads the element the reader is on as one item of the declared item type and adds it to a builder.
        /// </summary>
        /// <param name="reader">The reader, on the item start tag.</param>
        /// <param name="builder">The builder from <see cref="CreateBuilder"/>.</param>
        internal abstract void ReadItem(ref Utf8XmlReader reader, object builder);

        /// <summary>
        /// Reads the element the reader is on as a document element of the item type and adds it to a builder.
        /// </summary>
        /// <param name="reader">The reader, on the item start tag.</param>
        /// <param name="builder">The builder from <see cref="CreateBuilder"/>.</param>
        internal abstract void ReadRootItem(ref Utf8XmlReader reader, object builder);

        /// <summary>
        /// Writes each item of <paramref name="collection"/> as an element of <paramref name="property"/>.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="collection">The collection.</param>
        /// <param name="property">The member that holds the collection.</param>
        /// <param name="options">The serializer options.</param>
        internal abstract void WriteItems(Utf8XmlWriter writer, object collection, XmlBoundProperty property, XmlSerializerOptions options);

        /// <summary>
        /// Writes each item of <paramref name="collection"/> as a document element of the item type.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="collection">The collection.</param>
        internal abstract void WriteRootItems(Utf8XmlWriter writer, object collection);

        #endregion

    }

}
