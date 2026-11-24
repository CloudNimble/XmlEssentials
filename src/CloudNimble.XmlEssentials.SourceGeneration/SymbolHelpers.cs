using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// Attribute and type helpers for the generator.
    /// </summary>
    internal static class SymbolHelpers
    {

        #region Fields

        internal const string AttributeAttribute = "CloudNimble.Text.Xml.Serialization.XmlAttributeAttribute";
        internal const string ArrayAttribute = "CloudNimble.Text.Xml.Serialization.XmlArrayAttribute";
        internal const string ArrayItemAttribute = "CloudNimble.Text.Xml.Serialization.XmlArrayItemAttribute";
        internal const string CDataAttribute = "CloudNimble.Text.Xml.Serialization.XmlCDataAttribute";
        internal const string ConstructorAttribute = "CloudNimble.Text.Xml.Serialization.XmlConstructorAttribute";
        internal const string ConverterAttribute = "CloudNimble.Text.Xml.Serialization.XmlConverterAttribute";
        internal const string ContextType = "CloudNimble.Text.Xml.Serialization.XmlSerializerContext";
        internal const string DerivedTypeAttribute = "CloudNimble.Text.Xml.Serialization.XmlDerivedTypeAttribute";
        internal const string ElementAttribute = "CloudNimble.Text.Xml.Serialization.XmlElementAttribute";
        internal const string ExtensionDataAttribute = "CloudNimble.Text.Xml.Serialization.XmlExtensionDataAttribute";
        internal const string IgnoreAttribute = "CloudNimble.Text.Xml.Serialization.XmlIgnoreAttribute";
        internal const string IncludeAttribute = "CloudNimble.Text.Xml.Serialization.XmlIncludeAttribute";
        internal const string MixedAttribute = "CloudNimble.Text.Xml.Serialization.XmlMixedAttribute";
        internal const string ObjectCreationHandlingAttribute = "CloudNimble.Text.Xml.Serialization.XmlObjectCreationHandlingAttribute";
        internal const string PropertyNameAttribute = "CloudNimble.Text.Xml.Serialization.XmlPropertyNameAttribute";
        internal const string PropertyOrderAttribute = "CloudNimble.Text.Xml.Serialization.XmlPropertyOrderAttribute";
        internal const string RequiredAttribute = "CloudNimble.Text.Xml.Serialization.XmlRequiredAttribute";
        internal const string RootAttribute = "CloudNimble.Text.Xml.Serialization.XmlRootAttribute";
        internal const string SerializableAttribute = "CloudNimble.Text.Xml.Serialization.XmlSerializableAttribute<T>";
        internal const string SerializableAttributeMetadataName = "CloudNimble.Text.Xml.Serialization.XmlSerializableAttribute`1";
        internal const string SourceGenerationOptionsAttribute = "CloudNimble.Text.Xml.Serialization.XmlSourceGenerationOptionsAttribute";
        internal const string TextAttribute = "CloudNimble.Text.Xml.Serialization.XmlTextAttribute";
        internal const string BclXmlInclude = "System.Xml.Serialization.XmlIncludeAttribute";

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns whether <paramref name="data"/> is an instance of <paramref name="metadataName"/> or a type derived from it.
        /// </summary>
        /// <param name="data">The attribute.</param>
        /// <param name="metadataName">The fully qualified attribute type name.</param>
        /// <returns><see langword="true"/> when the attribute matches.</returns>
        internal static bool Is(AttributeData data, string metadataName)
        {
            for (var type = data.AttributeClass; type is not null; type = type.BaseType)
            {
                if (type.ToDisplayString() == metadataName || type.OriginalDefinition.ToDisplayString() == metadataName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the first attribute of type <paramref name="metadataName"/> on <paramref name="symbol"/>.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="metadataName">The fully qualified attribute type name.</param>
        /// <returns>The attribute, or null.</returns>
        internal static AttributeData? GetAttribute(ISymbol symbol, string metadataName)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (Is(attribute, metadataName))
                {
                    return attribute;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns every attribute of type <paramref name="metadataName"/> on <paramref name="symbol"/>.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="metadataName">The fully qualified attribute type name.</param>
        /// <returns>The attributes.</returns>
        internal static List<AttributeData> GetAttributes(ISymbol symbol, string metadataName)
        {
            var list = new List<AttributeData>();
            foreach (var attribute in symbol.GetAttributes())
            {
                if (Is(attribute, metadataName))
                {
                    list.Add(attribute);
                }
            }

            return list;
        }

        /// <summary>
        /// Returns a named or constructor argument as a string.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="name">The argument name.</param>
        /// <returns>The string, or null.</returns>
        internal static string? GetString(AttributeData attribute, string name)
        {
            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key == name && argument.Value.Value is string text)
                {
                    return text;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a named argument as an int, or <paramref name="fallback"/>.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="name">The argument name.</param>
        /// <param name="fallback">The value when the argument is absent.</param>
        /// <returns>The integer.</returns>
        internal static int GetInt(AttributeData attribute, string name, int fallback)
        {
            foreach (var argument in attribute.NamedArguments)
            {
                // An enumeration is given as its underlying type, which is a byte for several of the option enumerations.
                if (argument.Key == name && argument.Value.Value is int or byte or sbyte or short or ushort)
                {
                    return System.Convert.ToInt32(argument.Value.Value, System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            return fallback;
        }

        /// <summary>
        /// Returns a named argument as a bool.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="name">The argument name.</param>
        /// <returns>The flag.</returns>
        internal static bool GetBool(AttributeData attribute, string name)
        {
            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key == name && argument.Value.Value is bool value)
                {
                    return value;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the fully qualified C# type name for use in generated source.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The name, prefixed with <c>global::</c>.</returns>
        internal static string FullName(ITypeSymbol type)
        {
            return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> is a built-in scalar (or nullable scalar or enum).
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see langword="true"/> for primitives, enums, strings, and the other built-in value types.</returns>
        internal static bool IsScalar(ITypeSymbol type)
        {
            var root = UnwrapNullable(type);
            if (root.TypeKind is TypeKind.Enum)
            {
                return true;
            }

            return root.SpecialType is SpecialType.System_Boolean
                or SpecialType.System_Byte
                or SpecialType.System_SByte
                or SpecialType.System_Int16
                or SpecialType.System_UInt16
                or SpecialType.System_Int32
                or SpecialType.System_UInt32
                or SpecialType.System_Int64
                or SpecialType.System_UInt64
                or SpecialType.System_Single
                or SpecialType.System_Double
                or SpecialType.System_Decimal
                or SpecialType.System_String
                or SpecialType.System_Char
                or SpecialType.System_DateTime
                || FullName(root) is "global::System.Guid"
                    or "global::System.DateTimeOffset"
                    or "global::System.DateOnly"
                    or "global::System.TimeOnly"
                    or "global::System.TimeSpan"
                    or "global::System.Uri"
                    or "global::System.Version"
                    or "global::System.Byte[]"
                    or "global::System.Int128"
                    or "global::System.UInt128"
                    or "global::System.Half"
                    or "global::System.Numerics.BigInteger"
                    or "global::System.Memory<byte>"
                    or "global::System.ReadOnlyMemory<byte>"
                || root is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte };
        }

        /// <summary>
        /// Returns whether a member declared as <paramref name="type"/> can have items added to the collection it holds.
        /// </summary>
        /// <param name="type">The declared collection type.</param>
        /// <param name="elementType">The item type.</param>
        /// <returns>
        /// <see langword="true"/> for <c>ICollection&lt;T&gt;</c> and the interfaces that extend it, for a class that
        /// implements it and has a public parameterless constructor, and for <c>Stack&lt;T&gt;</c> and <c>Queue&lt;T&gt;</c>.
        /// </returns>
        /// <remarks>
        /// The same test the reflection resolver makes on the declared type, so both populate the same members.
        /// </remarks>
        internal static bool CanPopulateCollection(ITypeSymbol type, ITypeSymbol elementType)
        {
            if (type.IsValueType || type is not INamedTypeSymbol named)
            {
                return false;
            }

            if (named.OriginalDefinition.ToDisplayString() is "System.Collections.Generic.Stack<T>" or "System.Collections.Generic.Queue<T>")
            {
                return true;
            }

            return IsOrImplements(named, "System.Collections.Generic.ICollection<T>", elementType)
                && (named.TypeKind is TypeKind.Interface || HasPublicParameterlessConstructor(named));
        }

        /// <summary>
        /// Returns whether a member declared as <paramref name="type"/> can have entries added to the dictionary it holds.
        /// </summary>
        /// <param name="type">The declared dictionary type.</param>
        /// <param name="keyType">The key type.</param>
        /// <returns>
        /// <see langword="true"/> for <c>IDictionary&lt;TKey, TValue&gt;</c> and for a class that implements it and has a
        /// public parameterless constructor.
        /// </returns>
        internal static bool CanPopulateDictionary(ITypeSymbol type, ITypeSymbol keyType)
        {
            return !type.IsValueType
                && type is INamedTypeSymbol named
                && IsOrImplements(named, "System.Collections.Generic.IDictionary<TKey, TValue>", keyType)
                && (named.TypeKind is TypeKind.Interface || HasPublicParameterlessConstructor(named));
        }

        /// <summary>
        /// Returns what <c>[XmlObjectCreationHandling]</c> on <paramref name="symbol"/> says.
        /// </summary>
        /// <param name="symbol">A member or a type.</param>
        /// <returns>1 for populate, 0 for replace, and -1 when the attribute is absent.</returns>
        internal static int CreationHandlingOf(ISymbol symbol)
        {
            return GetAttribute(symbol, ObjectCreationHandlingAttribute) is { ConstructorArguments: { Length: > 0 } arguments } && arguments[0].Value is { } value
                ? System.Convert.ToInt32(value)
                : -1;
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> has a public constructor with no parameters.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see langword="true"/> when it has one.</returns>
        internal static bool HasPublicParameterlessConstructor(INamedTypeSymbol type)
        {
            foreach (var constructor in type.InstanceConstructors)
            {
                if (constructor.Parameters.Length == 0 && constructor.DeclaredAccessibility is Accessibility.Public)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> is, or implements, the generic interface
        /// <paramref name="definition"/> closed over <paramref name="firstArgument"/>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="definition">The display name of the interface definition.</param>
        /// <param name="firstArgument">The first type argument the interface must have.</param>
        /// <returns><see langword="true"/> when it does.</returns>
        internal static bool IsOrImplements(INamedTypeSymbol type, string definition, ITypeSymbol firstArgument)
        {
            if (Matches(type))
            {
                return true;
            }

            foreach (var implemented in type.AllInterfaces)
            {
                if (Matches(implemented))
                {
                    return true;
                }
            }

            return false;

            bool Matches(INamedTypeSymbol candidate)
            {
                return candidate.OriginalDefinition.ToDisplayString() == definition
                    && SymbolEqualityComparer.Default.Equals(candidate.TypeArguments[0], firstArgument);
            }
        }

        /// <summary>
        /// Returns whether the annotations on a property, field or parameter say it never holds null.
        /// </summary>
        /// <param name="symbol">The property, field or parameter.</param>
        /// <param name="type">Its type, as the type that is serialized has it.</param>
        /// <param name="originalType">Its type in the declaration, where a type parameter is still a type parameter.</param>
        /// <param name="forRead">
        /// <see langword="true"/> for what the symbol returns, which <c>[MaybeNull]</c> and <c>[NotNull]</c> change;
        /// <see langword="false"/> for what it accepts, which <c>[AllowNull]</c> and <c>[DisallowNull]</c> change.
        /// </param>
        /// <returns><see langword="true"/> when null is refused.</returns>
        /// <remarks>
        /// The answer is the one <c>System.Reflection.NullabilityInfoContext</c> gives at run time, which is what
        /// the reflection resolver reads. Code compiled without annotations says nothing, and neither does a type
        /// parameter with no <see langword="class"/>, <see langword="struct"/> or <see langword="notnull"/> constraint.
        /// A value type that is not <see cref="System.Nullable{T}"/> is never null, so it has nothing to refuse.
        /// </remarks>
        internal static bool RefusesNull(ISymbol symbol, ITypeSymbol type, ITypeSymbol originalType, bool forRead)
        {
            if (type.IsValueType && type.OriginalDefinition.SpecialType is not SpecialType.System_Nullable_T)
            {
                return false;
            }

            var refuses = originalType is ITypeParameterSymbol parameter
                ? originalType.NullableAnnotation is NullableAnnotation.NotAnnotated
                    && (parameter.HasNotNullConstraint
                        || parameter.HasValueTypeConstraint
                        || (parameter.HasReferenceTypeConstraint && parameter.ReferenceTypeConstraintNullableAnnotation is NullableAnnotation.NotAnnotated))
                : !type.IsValueType && type.NullableAnnotation is NullableAnnotation.NotAnnotated;
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ContainingNamespace?.ToDisplayString() != "System.Diagnostics.CodeAnalysis")
                {
                    continue;
                }

                switch (attribute.AttributeClass.Name)
                {
                    case "MaybeNullAttribute" when forRead:
                    case "AllowNullAttribute" when !forRead:
                        refuses = false;
                        break;
                    case "NotNullAttribute" when forRead:
                    case "DisallowNullAttribute" when !forRead:
                        refuses = true;
                        break;
                }
            }

            return refuses;
        }

        /// <summary>
        /// Returns the underlying type of a nullable value type, or <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The non-nullable type.</returns>
        internal static ITypeSymbol UnwrapNullable(ITypeSymbol type)
        {
            return type is INamedTypeSymbol named
                && named.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T
                && named.TypeArguments.Length == 1
                ? named.TypeArguments[0]
                : type;
        }

        /// <summary>
        /// Returns whether <paramref name="type"/> inherits <paramref name="metadataName"/>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="metadataName">The fully qualified base type name.</param>
        /// <returns><see langword="true"/> when it inherits.</returns>
        internal static bool Inherits(INamedTypeSymbol type, string metadataName)
        {
            for (var current = type.BaseType; current is not null; current = current.BaseType)
            {
                if (current.ToDisplayString() == metadataName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the constructor argument at <paramref name="index"/> as a string.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="index">The argument index.</param>
        /// <returns>The string, or null.</returns>
        internal static string? GetConstructorString(AttributeData attribute, int index)
        {
            if (index >= attribute.ConstructorArguments.Length)
            {
                return null;
            }

            return attribute.ConstructorArguments[index].Value as string;
        }

        #endregion

    }

}
