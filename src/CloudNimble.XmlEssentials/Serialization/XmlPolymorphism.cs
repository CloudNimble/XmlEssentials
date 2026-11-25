using System;
using System.Text;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// The derived types a member or collection item may hold, and how they are told apart in the document.
    /// </summary>
    internal sealed class XmlPolymorphism
    {

        #region Fields

        /// <summary>
        /// The mapping of a type that declares no derived types and no discriminator.
        /// </summary>
        internal static readonly XmlPolymorphism None = new(typeof(object), null, XmlDiscriminatorPlacement.ElementName, null, XmlUnknownDerivedTypeHandling.FailSerialization);

        /// <summary>
        /// The declared CLR type of the member or item.
        /// </summary>
        internal readonly Type DeclaredType;

        /// <summary>
        /// The types a value of the declared type may be written as: the types that derive from the declared type.
        /// The declared type is never one of them, so a value of the declared type is written as itself, under the
        /// name of the member and with no discriminator.
        /// </summary>
        internal readonly XmlDerivedBinding[] Derived;

        /// <summary>
        /// Every type a document may identify here: <see cref="Derived"/>, and the rest of the list the declared
        /// type inherits, which holds the declared type itself and the types beside it. A document that identifies
        /// the declared type is read as it; one that identifies a type beside it is refused.
        /// </summary>
        internal readonly XmlDerivedBinding[] Known;

        /// <summary>
        /// Whether a derived type is written under its own element name.
        /// </summary>
        internal readonly bool HasElementNames;

        /// <summary>
        /// Whether the member holds <see cref="XmlNamedValue"/>: each value names its element, so two elements may
        /// hold one type.
        /// </summary>
        internal bool IsNamed { get; init; }

        /// <summary>
        /// Whether a value read here has to be checked for a derived type.
        /// </summary>
        internal readonly bool IsActive;

        /// <summary>
        /// Where the discriminator is written.
        /// </summary>
        internal readonly XmlDiscriminatorPlacement Placement;

        /// <summary>
        /// Whether the element name of a derived type is matched without regard to ASCII case, as
        /// <see cref="XmlSerializerOptions.PropertyNameCaseInsensitive"/> says for every element a member is read from.
        /// </summary>
        internal bool CaseInsensitive;

        /// <summary>
        /// How a runtime type that is not declared is handled.
        /// </summary>
        internal readonly XmlUnknownDerivedTypeHandling UnknownDerivedTypeHandling;

        /// <summary>
        /// The UTF-8 discriminator attribute local name when <see cref="Placement"/> is
        /// <see cref="XmlDiscriminatorPlacement.Attribute"/>, or empty.
        /// </summary>
        internal readonly byte[] Utf8DiscriminatorAttributeName;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the mapping.
        /// </summary>
        /// <param name="declaredType">The declared CLR type of the member or item.</param>
        /// <param name="derived">The derived-type choices, or null.</param>
        /// <param name="placement">Where the discriminator is written.</param>
        /// <param name="utf8DiscriminatorAttributeName">The discriminator attribute local name, or null.</param>
        /// <param name="unknownDerivedTypeHandling">How a runtime type that is not declared is handled.</param>
        /// <param name="known">Every type a document may identify, or null when it is <paramref name="derived"/>.</param>
        internal XmlPolymorphism(Type declaredType, XmlDerivedBinding[]? derived, XmlDiscriminatorPlacement placement, byte[]? utf8DiscriminatorAttributeName, XmlUnknownDerivedTypeHandling unknownDerivedTypeHandling, XmlDerivedBinding[]? known = null)
        {
            DeclaredType = declaredType;
            Derived = derived ?? [];
            Known = known ?? Derived;
            Placement = placement;
            Utf8DiscriminatorAttributeName = utf8DiscriminatorAttributeName ?? [];
            UnknownDerivedTypeHandling = unknownDerivedTypeHandling;
            IsActive = Known.Length > 0 || placement is not XmlDiscriminatorPlacement.ElementName;
            HasElementNames = Derived.Length > 0 && placement is XmlDiscriminatorPlacement.ElementName;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns the derived binding whose discriminator equals <paramref name="discriminator"/>.
        /// </summary>
        /// <param name="discriminator">The UTF-8 discriminator value, as the attribute holds it.</param>
        /// <returns>The binding, or null.</returns>
        /// <remarks>
        /// The value is compared as the bytes it is, with the bytes each binding keeps, so a value that names a
        /// known type is never made into a string. System.Text.Json compares a <c>$type</c> the same way.
        /// </remarks>
        internal XmlDerivedBinding? FindByDiscriminator(ReadOnlySpan<byte> discriminator)
        {
            foreach (var derived in Known)
            {
                if (discriminator.SequenceEqual(derived.Utf8Discriminator))
                {
                    return derived;
                }
            }

            // The value of xsi:type is a QName. A document that binds a prefix to the namespace of its types
            // writes "p:Circle", and the prefix is whatever that document chose, so the type is found by the
            // local part. A discriminator that was declared with a colon has been matched above, as it is written.
            if (Placement is XmlDiscriminatorPlacement.XsiType && discriminator.IndexOf((byte)':') is > 0 and var colon)
            {
                var local = discriminator[(colon + 1)..];
                foreach (var derived in Known)
                {
                    if (local.SequenceEqual(derived.Utf8Discriminator))
                    {
                        return derived;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Reads the discriminator of the start tag the reader is on.
        /// </summary>
        /// <param name="reader">The reader, on a start tag. It is on the start tag again when this method returns.</param>
        /// <param name="unknown">
        /// When this method returns null, the discriminator the start tag has that names no known type, or null
        /// when the start tag has none or an empty one.
        /// </param>
        /// <returns>The binding of the type the discriminator names, or null.</returns>
        internal XmlDerivedBinding? ReadDiscriminator(ref Utf8XmlReader reader, out string? unknown)
        {
            unknown = null;
            if (!reader.HasAttributes)
            {
                return null;
            }

            XmlDerivedBinding? known = null;
            while (reader.MoveToNextAttribute())
            {
                var isDiscriminator = Placement is XmlDiscriminatorPlacement.XsiType
                    ? reader.LocalNameEquals(XmlAttributes.Type) && reader.NamespaceUriEquals(XmlNamespaces.Xsi)
                    : Utf8DiscriminatorAttributeName.Length > 0 && reader.LocalNameEquals(Utf8DiscriminatorAttributeName) && reader.PrefixSpan.IsEmpty;
                if (!isDiscriminator)
                {
                    continue;
                }

                // The last one counts, as it did when each was read as a string.
                var value = reader.ValueSpan;
                known = value.IsEmpty ? null : FindByDiscriminator(value);
                unknown = known is null && !value.IsEmpty ? reader.GetString() : null;
            }

            reader.MoveToElement();
            return known;
        }

        /// <summary>
        /// Returns the derived binding whose element name matches the reader.
        /// </summary>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <returns>The binding, or null.</returns>
        internal XmlDerivedBinding? FindByElementName(ref Utf8XmlReader reader)
        {
            foreach (var derived in Known)
            {
                if (derived.Matches(ref reader, CaseInsensitive))
                {
                    return derived;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns whether the reader is on an element named for one of the derived types.
        /// </summary>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <param name="caseInsensitive">Whether ASCII case is ignored.</param>
        /// <returns><see langword="true"/> when a derived type is written under this element name.</returns>
        internal bool MatchesElementName(ref Utf8XmlReader reader, bool caseInsensitive)
        {
            foreach (var derived in Derived)
            {
                if (derived.Matches(ref reader, caseInsensitive))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Reads the element the reader is on as the derived type the document identified.
        /// </summary>
        /// <param name="reader">The reader, on the start tag.</param>
        /// <param name="derived">The derived type the element name or discriminator identified.</param>
        /// <returns>The value.</returns>
        /// <exception cref="XmlException">The identified type is not the declared type or one derived from it.</exception>
        /// <remarks>
        /// A derived type inherits the list its base declares, so the list of a derived type names its siblings
        /// too. A sibling is a known type and the wrong one for the declared type.
        /// </remarks>
        internal object? ReadDerived(ref Utf8XmlReader reader, XmlDerivedBinding derived)
        {
            if (!IsNamed && !derived.IsOfDeclaredType)
            {
                throw XmlException.Create($"The element identifies the type '{derived.Type}', which is not a '{DeclaredType}'.");
            }

            if (Placement is not XmlDiscriminatorPlacement.ElementName)
            {
                return derived.Info.ReadElementAsObject(ref reader);
            }

            try
            {
                // An alternative of a choice may be of a type whose own derived types an attribute identifies.
                var value = derived.Nested is { } nested
                    ? nested.ReadValue(ref reader, derived.Info)
                    : derived.Info.ReadElementAsObject(ref reader);

                // A named value remembers which element it was, since its type does not say.
                return IsNamed ? new XmlNamedValue(derived.Discriminator, value) { Namespace = derived.Namespace } : value;
            }
            catch (XmlException ex)
            {
                // The element is named for the derived type, not for the member or the item it is the value of.
                reader.Locate(ex);
                ex.NameElement(Encoding.UTF8.GetString(derived.Utf8Name));
                throw;
            }
        }

        /// <summary>
        /// Reads the element the reader is on as the declared type or one of its derived types.
        /// </summary>
        /// <param name="reader">The reader, on the start tag.</param>
        /// <param name="fallback">Type info used when no derived type matches.</param>
        /// <returns>The value.</returns>
        internal object? ReadValue(ref Utf8XmlReader reader, XmlTypeInfo fallback)
        {
            if (Placement is XmlDiscriminatorPlacement.Attribute or XmlDiscriminatorPlacement.XsiType)
            {
                if (ReadDiscriminator(ref reader, out var unknown) is { } typed)
                {
                    return ReadDerived(ref reader, typed);
                }

                if (unknown is not null && UnknownDerivedTypeHandling is XmlUnknownDerivedTypeHandling.FailSerialization)
                {
                    throw XmlException.Create($"Unknown type discriminator '{unknown}'.");
                }

                return fallback.ReadElementAsObject(ref reader);
            }

            if (FindByElementName(ref reader) is { } derived)
            {
                return ReadDerived(ref reader, derived);
            }

            return fallback.ReadElementAsObject(ref reader);
        }

        /// <summary>
        /// Returns the derived binding to write for <paramref name="runtime"/>, or null to write the declared type.
        /// </summary>
        /// <param name="runtime">The runtime type of the value.</param>
        /// <returns>The binding, or null.</returns>
        /// <exception cref="NotSupportedException">The runtime type is not declared and unknown types fail.</exception>
        internal XmlDerivedBinding? TryGetDerivedWrite(Type runtime)
        {
            foreach (var derived in Derived)
            {
                if (derived.Type == runtime)
                {
                    return derived;
                }
            }

            // A value of the declared type is the usual miss, and no base type of it can be an alternative.
            if (runtime == DeclaredType)
            {
                return null;
            }

            // A value of a type that derives from an alternative of a choice is written under the element of that
            // alternative, with the discriminator the alternative's own type gives it.
            for (var type = runtime.BaseType; type is not null; type = type.BaseType)
            {
                foreach (var derived in Derived)
                {
                    if (derived.Type == type && derived.Nested is not null)
                    {
                        return derived;
                    }
                }
            }

            if (UnknownDerivedTypeHandling is XmlUnknownDerivedTypeHandling.FallBackToBaseType)
            {
                return null;
            }

            if (UnknownDerivedTypeHandling is XmlUnknownDerivedTypeHandling.FallBackToNearestAncestor)
            {
                for (var type = runtime.BaseType; type is not null && type != DeclaredType && type != typeof(object); type = type.BaseType)
                {
                    foreach (var derived in Derived)
                    {
                        if (derived.Type == type)
                        {
                            return derived;
                        }
                    }
                }

                return null;
            }

            throw new NotSupportedException($"The type '{runtime}' is not a declared derived type of '{DeclaredType}'.");
        }

        /// <summary>
        /// Writes a value that names its element: the element, and the value it holds inside it.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="named">The value of the member or the item.</param>
        /// <exception cref="NotSupportedException">
        /// The value names an element the member does not have, or holds a value that is not of the type of its
        /// element.
        /// </exception>
        /// <remarks>
        /// Only a member that holds <see cref="XmlNamedValue"/> comes here (<see cref="IsNamed"/>). Every other
        /// value finds its element by its type, with <see cref="TryGetDerivedWrite"/>.
        /// </remarks>
        internal void WriteNamed(Utf8XmlWriter writer, XmlNamedValue named)
        {
            foreach (var derived in Derived)
            {
                if (derived.Discriminator != named.Name || (named.Namespace is not null && named.Namespace != derived.Namespace))
                {
                    continue;
                }

                var content = named.Value;
                if (content is not null && content.GetType() != derived.Type && (derived.Nested is null || !derived.Type.IsInstanceOfType(content)))
                {
                    throw new NotSupportedException($"The element '{named.Name}' holds a value of type '{derived.Type}', and the value given for it is of type '{content.GetType()}'.");
                }

                derived.WriteStart(writer);
                derived.WriteContent(writer, content);
                writer.WriteEndElement();
                return;
            }

            throw new NotSupportedException($"The member has no element named '{named.Name}' for a {nameof(XmlNamedValue)}.");
        }

        #endregion

    }

}
