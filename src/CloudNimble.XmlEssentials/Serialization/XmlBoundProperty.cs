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
        /// Whether an element under the member's parent that has the name of one of the member's derived types
        /// belongs to the member.
        /// </summary>
        /// <remarks>
        /// It does for a member that holds one value and for a collection with no wrapper, whose values are
        /// children of the parent. It does not for a collection in a wrapper: its items are children of the
        /// wrapper, so their names say nothing about the children of the parent.
        /// </remarks>
        internal readonly bool ClaimsDerivedElementNames;

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
        /// Whether nothing read for the member can be kept: it has no setter, or is never read
        /// (<see cref="XmlIgnoreCondition.WhenReading"/>), and fills no constructor parameter. Such a member is
        /// read only when the value it holds is populated.
        /// </summary>
        internal readonly bool NeverStored;

        /// <summary>
        /// The message of the exception thrown when the document gives null for the member, or null when the member
        /// accepts null. Set once, while the type is bound, when
        /// <see cref="XmlSerializerOptions.RespectNullableAnnotations"/> is on and the member, or the constructor
        /// parameter it fills, is annotated non-nullable.
        /// </summary>
        internal string? NullReadError;

        /// <summary>
        /// Creates the reusable box that holds the member's value for a constructor, for a member whose facts were
        /// generated; or null. It returns an <c>XmlHeldBox&lt;TValue&gt;</c>.
        /// </summary>
        internal Func<object>? HeldBoxFactory;

        /// <summary>
        /// Whether the reusable box may be closed over the member's type at run time, which only the reflection
        /// resolver may do: Native AOT cannot close a generic type over a value type that way.
        /// </summary>
        internal bool ReflectsHeldBox;

        /// <summary>
        /// For an element member with no namespace: the namespaces of the other element members that have its
        /// local name, or null when there are none. An element of that name in one of them belongs to that member.
        /// Set once, while the type is bound.
        /// </summary>
        internal byte[][]? SiblingNamespaces;

        /// <summary>
        /// The message of the exception thrown when the member holds null as it is written, or null when the member
        /// may hold null. Set once, while the type is bound, when
        /// <see cref="XmlSerializerOptions.RespectNullableAnnotations"/> is on and the member is annotated as never
        /// reading null.
        /// </summary>
        internal string? NullWriteError;

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
        /// Whether the value the member already holds is populated when the member is read: items are added to a
        /// collection, entries to a dictionary, and the members of an object are set on it. Set once, while the type
        /// is bound, from <see cref="XmlObjectCreationHandlingAttribute"/> and
        /// <see cref="XmlSerializerOptions.PreferredObjectCreationHandling"/>.
        /// </summary>
        internal bool Populates;

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
            NeverStored = !canSet && constructorIndex < 0;
            ConstructorIndex = constructorIndex;
            IsCData = isCData;
            Collection = collection;
            IsUnwrappedCollection = collection is { Wrapped: false };
            Polymorphism = polymorphism;
            ClaimsDerivedElementNames = polymorphism.HasElementNames && collection is not { Wrapped: true };
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

            return (NameEquals(ref reader, Utf8LocalName, caseInsensitive) && !IsInSiblingNamespace(ref reader))
                || (ClaimsDerivedElementNames && Polymorphism.MatchesElementName(ref reader, caseInsensitive));
        }

        /// <summary>
        /// Returns whether the element the reader is on is in a namespace that another member of this local name
        /// has, and so is that member's.
        /// </summary>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <returns><see langword="true"/> when the element belongs to another member.</returns>
        internal bool IsInSiblingNamespace(ref Utf8XmlReader reader)
        {
            if (SiblingNamespaces is not { } taken)
            {
                return false;
            }

            foreach (var namespaceUri in taken)
            {
                if (reader.NamespaceUriEquals(namespaceUri))
                {
                    return true;
                }
            }

            return false;
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
            // The exact comparison comes first: the ASCII one answers false for any name that holds a
            // character outside ASCII, even when the two names are the same bytes.
            return reader.LocalNameEquals(utf8Name)
                || (caseInsensitive && System.Text.Ascii.EqualsIgnoreCase(reader.LocalNameSpan, utf8Name));
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
        /// Returns why the value this member holds cannot be populated.
        /// </summary>
        /// <returns>
        /// The end of the sentence "Property 'X' on type 'T' is marked with XmlObjectCreationHandling.Populate …", or
        /// null when the member can be populated.
        /// </returns>
        internal abstract string? PopulateRefusal();

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
