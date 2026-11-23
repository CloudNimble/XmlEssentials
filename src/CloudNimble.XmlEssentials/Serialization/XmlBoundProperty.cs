using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// A member of an object, with cached UTF-8 names and typed get/set.
    /// </summary>
    /// <remarks>
    /// A member is read into one of two places. A type created without arguments exists before its content is read,
    /// so each value is set on the instance as it arrives. A type created through a constructor with parameters does
    /// not exist yet, so values are held in an array, one slot per member, until every argument is known. The read
    /// methods take both: <c>values</c> is null in the first case.
    /// </remarks>
    internal abstract class XmlBoundProperty
    {

        #region Fields

        /// <summary>
        /// Whether the member can be set after construction.
        /// </summary>
        internal readonly bool CanSet;

        /// <summary>
        /// Everything an attribute member writes before its value (a space, the name, <c>="</c>), when the member is
        /// an attribute in no namespace whose name is an ASCII NCName, or null.
        /// </summary>
        internal readonly byte[]? AttributeHead;

        /// <summary>
        /// The collection mapping, or null when the member is not a collection.
        /// </summary>
        internal readonly XmlCollectionBinding? Collection;

        /// <summary>
        /// The constructor parameter index this member fills, or <c>-1</c>.
        /// </summary>
        internal readonly int ConstructorIndex;

        /// <summary>
        /// The slot this member uses while an object is read: where its "has appeared" flag, its held value, and its
        /// collection builder live. A member that fills a constructor parameter uses that parameter's position, so the
        /// held values of the parameters are already in order when the constructor is called; the other members follow.
        /// </summary>
        internal readonly int Index;

        /// <summary>
        /// The public property metadata.
        /// </summary>
        internal readonly XmlPropertyInfo Info;

        /// <summary>
        /// Whether the member is written as an attribute.
        /// </summary>
        internal readonly bool IsAttribute;

        /// <summary>
        /// Whether simple content is written as a CDATA section.
        /// </summary>
        internal readonly bool IsCData;

        /// <summary>
        /// The whole end tag of the member element, when it can be written from bytes built ahead of time, or null.
        /// </summary>
        internal readonly byte[]? LeafCloseTag;

        /// <summary>
        /// The whole start tag of the member element, when the member is an element in no namespace of its own whose
        /// name is an ASCII NCName, or null. Such a tag is valid wherever the member is written, so it is checked once.
        /// </summary>
        internal readonly byte[]? LeafOpenTag;

        /// <summary>
        /// Whether the member must appear when deserializing.
        /// </summary>
        internal readonly bool IsRequired;

        /// <summary>
        /// Whether the member is the simple content of its element.
        /// </summary>
        internal readonly bool IsText;

        /// <summary>
        /// Whether the member is a collection written as repeated elements with no wrapper.
        /// </summary>
        internal readonly bool IsUnwrappedCollection;

        /// <summary>
        /// The derived types the member, or each item of a collection member, may hold.
        /// </summary>
        internal readonly XmlPolymorphism Polymorphism;

        /// <summary>
        /// The UTF-8 local name used to match start tags.
        /// </summary>
        internal readonly byte[] Utf8LocalName;

        /// <summary>
        /// The UTF-8 namespace URI, or empty when the member inherits the parent's namespace.
        /// </summary>
        internal readonly byte[] Utf8NamespaceUri;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a bound member.
        /// </summary>
        /// <param name="info">The public metadata.</param>
        /// <param name="index">The slot this member uses while an object is read.</param>
        /// <param name="utf8LocalName">The UTF-8 local name.</param>
        /// <param name="utf8NamespaceUri">The UTF-8 namespace URI.</param>
        /// <param name="canSet">Whether the member can be set after construction.</param>
        /// <param name="constructorIndex">The constructor parameter index, or <c>-1</c>.</param>
        /// <param name="isCData">Whether simple content is written as CDATA.</param>
        /// <param name="collection">The collection mapping, or null.</param>
        /// <param name="polymorphism">The derived types the member may hold.</param>
        private protected XmlBoundProperty(
            XmlPropertyInfo info,
            int index,
            byte[] utf8LocalName,
            byte[] utf8NamespaceUri,
            bool canSet,
            int constructorIndex,
            bool isCData,
            XmlCollectionBinding? collection,
            XmlPolymorphism polymorphism)
        {
            Info = info;
            Index = index;
            IsAttribute = info.IsAttribute;
            IsText = info.IsText;
            IsRequired = info.IsRequired;
            Utf8LocalName = utf8LocalName;
            Utf8NamespaceUri = utf8NamespaceUri;
            CanSet = canSet;
            ConstructorIndex = constructorIndex;
            IsCData = isCData;
            Collection = collection;
            IsUnwrappedCollection = collection is { Wrapped: false };
            Polymorphism = polymorphism;
            if (IsAttribute && utf8NamespaceUri.Length == 0 && XmlChars.IsShortAsciiNcName(utf8LocalName, utf8LocalName.Length))
            {
                AttributeHead = [(byte)' ', .. utf8LocalName, (byte)'=', (byte)'"'];
            }

            if (!IsAttribute && !IsText && utf8NamespaceUri.Length == 0 && XmlChars.IsShortAsciiNcName(utf8LocalName, utf8LocalName.Length))
            {
                LeafOpenTag = [(byte)'<', .. utf8LocalName, (byte)'>'];
                LeafCloseTag = [(byte)'<', (byte)'/', .. utf8LocalName, (byte)'>'];
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds <paramref name="item"/> to a collection builder.
        /// </summary>
        /// <param name="builder">The builder from <see cref="CreateBuilder"/>.</param>
        /// <param name="item">The item.</param>
        internal void AddItem(object builder, object? item)
        {
            Collection!.AddItem(builder, item);
        }

        /// <summary>
        /// Creates an empty collection builder.
        /// </summary>
        /// <returns>The builder.</returns>
        internal object CreateBuilder()
        {
            return Collection!.CreateBuilder();
        }

        /// <summary>
        /// Returns whether this member's start tag or attribute name matches the reader.
        /// </summary>
        /// <param name="reader">The reader, on a start tag or an attribute.</param>
        /// <param name="caseInsensitive">Whether local names are compared without regard to ASCII case.</param>
        /// <returns><see langword="true"/> when the names match.</returns>
        internal bool Matches(ref Utf8XmlReader reader, bool caseInsensitive)
        {
            if (Utf8NamespaceUri.Length > 0)
            {
                if (!reader.NamespaceUriEquals(Utf8NamespaceUri))
                {
                    return false;
                }
            }
            else if (IsAttribute && !reader.NamespaceUriSpan.IsEmpty)
            {
                return false;
            }

            return NameEquals(ref reader, Utf8LocalName, caseInsensitive)
                || (Polymorphism.HasElementNames && Polymorphism.MatchesElementName(ref reader, caseInsensitive));
        }

        /// <summary>
        /// Returns whether this member's attribute name matches the attribute the reader is on.
        /// </summary>
        /// <param name="reader">The reader, on an attribute.</param>
        /// <param name="caseInsensitive">Whether local names are compared without regard to ASCII case.</param>
        /// <returns><see langword="true"/> when the names match.</returns>
        internal bool MatchesAttribute(ref Utf8XmlReader reader, bool caseInsensitive)
        {
            return Utf8NamespaceUri.Length == 0 && !caseInsensitive
                ? reader.UnqualifiedAttributeNameEquals(Utf8LocalName)
                : Matches(ref reader, caseInsensitive);
        }

        /// <summary>
        /// Compares the current local name to <paramref name="utf8Name"/>.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="utf8Name">The UTF-8 local name.</param>
        /// <param name="caseInsensitive">Whether ASCII case is ignored.</param>
        /// <returns><see langword="true"/> when they match.</returns>
        internal static bool NameEquals(ref Utf8XmlReader reader, byte[] utf8Name, bool caseInsensitive)
        {
            return caseInsensitive
                ? System.Text.Ascii.EqualsIgnoreCase(reader.LocalNameSpan, utf8Name)
                : reader.LocalNameEquals(utf8Name);
        }

        /// <summary>
        /// Reads the element the reader is on as this member's value and stores it.
        /// </summary>
        /// <param name="reader">The reader, on the member's start tag.</param>
        /// <param name="instance">The instance to set the member on, or null when values are held.</param>
        /// <param name="values">The held values, one slot per member, or null when the instance exists.</param>
        /// <exception cref="XmlException">The member is required and the element is nil.</exception>
        internal abstract void Read(ref Utf8XmlReader reader, object? instance, object?[]? values);

        /// <summary>
        /// Reads the attribute the reader is on as this member's value and stores it.
        /// </summary>
        /// <param name="reader">The reader, on the member's attribute.</param>
        /// <param name="instance">The instance to set the member on, or null when values are held.</param>
        /// <param name="values">The held values, one slot per member, or null when the instance exists.</param>
        internal abstract void ReadAttribute(ref Utf8XmlReader reader, object? instance, object?[]? values);

        /// <summary>
        /// Reads one item of an unwrapped collection and adds it to a builder. The reader is on the item start tag.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="builder">The builder from <see cref="CreateBuilder"/>.</param>
        internal void ReadItem(ref Utf8XmlReader reader, object builder)
        {
            if (Polymorphism.IsActive)
            {
                Collection!.AddItem(builder, Polymorphism.ReadValue(ref reader, Collection.ItemTypeInfo));
                return;
            }

            Collection!.ReadItem(ref reader, builder);
        }

        /// <summary>
        /// Parses simple content from its UTF-8 lexical form and stores it.
        /// </summary>
        /// <param name="utf8">The UTF-8 lexical form.</param>
        /// <param name="instance">The instance to set the member on, or null when values are held.</param>
        /// <param name="values">The held values, one slot per member, or null when the instance exists.</param>
        internal abstract void ReadText(ReadOnlySpan<byte> utf8, object? instance, object?[]? values);

        /// <summary>
        /// Creates the box this member's slot holds for as long as its values array is reused.
        /// </summary>
        /// <returns>The box, or null when the member holds its value as an ordinary object.</returns>
        internal abstract object? CreateHeldBox();

        /// <summary>
        /// Holds a constructor parameter's default value in this member's slot.
        /// </summary>
        /// <param name="values">The held values, one slot per member.</param>
        /// <param name="value">The default value, as reflection reports it.</param>
        internal abstract void HoldDefault(object?[] values, object? value);

        /// <summary>
        /// Decides whether this member's values are held in a box that is reused from one object to the next.
        /// </summary>
        /// <param name="parameterTypes">The types of the constructor parameters this member fills, or empty when it is set after construction.</param>
        internal abstract void UseHeldBox(ReadOnlySpan<Type> parameterTypes);

        /// <summary>
        /// Assigns a held value on <paramref name="instance"/> after it was created through its constructor.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="value">The held value.</param>
        internal abstract void SetHeldValue(object instance, object? value);

        /// <summary>
        /// Converts a collection builder to the member type and stores it.
        /// </summary>
        /// <param name="builder">The builder from <see cref="CreateBuilder"/>.</param>
        /// <param name="instance">The instance to set the member on, or null when values are held.</param>
        /// <param name="values">The held values, one slot per member, or null when the instance exists.</param>
        internal abstract void StoreCollection(object builder, object? instance, object?[]? values);

        /// <summary>
        /// Writes this member of <paramref name="instance"/>, unless its value is one the member leaves out.
        /// </summary>
        /// <param name="writer">The writer, inside the parent start tag.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="options">The serializer options.</param>
        internal abstract void Write(Utf8XmlWriter writer, object instance, XmlSerializerOptions options);

        /// <summary>
        /// Writes the member start tag, declaring its namespace when it has one.
        /// </summary>
        /// <param name="writer">The writer.</param>
        internal void WriteStart(Utf8XmlWriter writer)
        {
            if (Utf8NamespaceUri.Length > 0)
            {
                writer.WriteStartElement(Utf8LocalName, Utf8NamespaceUri);
                return;
            }

            writer.WriteStartElement(Utf8LocalName);
        }

        #endregion

    }

}
