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
        /// The derived-type choices.
        /// </summary>
        internal readonly XmlDerivedBinding[] Derived;

        /// <summary>
        /// Whether a derived type is written under its own element name.
        /// </summary>
        internal readonly bool HasElementNames;

        /// <summary>
        /// Whether a value read here has to be checked for a derived type.
        /// </summary>
        internal readonly bool IsActive;

        /// <summary>
        /// Where the discriminator is written.
        /// </summary>
        internal readonly XmlDiscriminatorPlacement Placement;

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
        internal XmlPolymorphism(Type declaredType, XmlDerivedBinding[]? derived, XmlDiscriminatorPlacement placement, byte[]? utf8DiscriminatorAttributeName, XmlUnknownDerivedTypeHandling unknownDerivedTypeHandling)
        {
            DeclaredType = declaredType;
            Derived = derived ?? [];
            Placement = placement;
            Utf8DiscriminatorAttributeName = utf8DiscriminatorAttributeName ?? [];
            UnknownDerivedTypeHandling = unknownDerivedTypeHandling;
            IsActive = Derived.Length > 0 || placement is not XmlDiscriminatorPlacement.ElementName;
            HasElementNames = Derived.Length > 0 && placement is XmlDiscriminatorPlacement.ElementName;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns the derived binding whose discriminator equals <paramref name="discriminator"/>.
        /// </summary>
        /// <param name="discriminator">The discriminator value.</param>
        /// <returns>The binding, or null.</returns>
        internal XmlDerivedBinding? FindByDiscriminator(string discriminator)
        {
            foreach (var derived in Derived)
            {
                if (derived.Discriminator == discriminator)
                {
                    return derived;
                }
            }

            // The value of xsi:type is a QName. A document that binds a prefix to the namespace of its types
            // writes "p:Circle", and the prefix is whatever that document chose, so the type is found by the
            // local part. A discriminator that was declared with a colon has been matched above, as it is written.
            if (Placement is XmlDiscriminatorPlacement.XsiType && discriminator.IndexOf(':') is > 0 and var colon)
            {
                var local = discriminator.AsSpan(colon + 1);
                foreach (var derived in Derived)
                {
                    if (local.SequenceEqual(derived.Discriminator))
                    {
                        return derived;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the derived binding whose element name matches the reader.
        /// </summary>
        /// <param name="reader">The reader, on a start tag.</param>
        /// <returns>The binding, or null.</returns>
        internal XmlDerivedBinding? FindByElementName(ref Utf8XmlReader reader)
        {
            foreach (var derived in Derived)
            {
                if (derived.Matches(ref reader, caseInsensitive: true))
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
            if (!DeclaredType.IsAssignableFrom(derived.Type))
            {
                throw XmlException.Create($"The element identifies the type '{derived.Type}', which is not a '{DeclaredType}'.");
            }

            if (Placement is not XmlDiscriminatorPlacement.ElementName)
            {
                return derived.Info.ReadElementAsObject(ref reader);
            }

            try
            {
                return derived.Info.ReadElementAsObject(ref reader);
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
                var discriminator = XmlConverterHelpers.ReadDiscriminator(ref reader, Placement, Utf8DiscriminatorAttributeName);
                if (discriminator is { Length: > 0 })
                {
                    if (FindByDiscriminator(discriminator) is { } typed)
                    {
                        return ReadDerived(ref reader, typed);
                    }

                    if (UnknownDerivedTypeHandling is XmlUnknownDerivedTypeHandling.FailSerialization)
                    {
                        throw XmlException.Create($"Unknown type discriminator '{discriminator}'.");
                    }
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

            if (runtime == DeclaredType)
            {
                return null;
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

        #endregion

    }

}
