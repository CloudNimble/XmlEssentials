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
        internal const string ContextType = "CloudNimble.Text.Xml.Serialization.XmlSerializerContext";
        internal const string DerivedTypeAttribute = "CloudNimble.Text.Xml.Serialization.XmlDerivedTypeAttribute";
        internal const string ElementAttribute = "CloudNimble.Text.Xml.Serialization.XmlElementAttribute";
        internal const string ExtensionDataAttribute = "CloudNimble.Text.Xml.Serialization.XmlExtensionDataAttribute";
        internal const string IgnoreAttribute = "CloudNimble.Text.Xml.Serialization.XmlIgnoreAttribute";
        internal const string IncludeAttribute = "CloudNimble.Text.Xml.Serialization.XmlIncludeAttribute";
        internal const string MixedAttribute = "CloudNimble.Text.Xml.Serialization.XmlMixedAttribute";
        internal const string PropertyNameAttribute = "CloudNimble.Text.Xml.Serialization.XmlPropertyNameAttribute";
        internal const string PropertyOrderAttribute = "CloudNimble.Text.Xml.Serialization.XmlPropertyOrderAttribute";
        internal const string RequiredAttribute = "CloudNimble.Text.Xml.Serialization.XmlRequiredAttribute";
        internal const string RootAttribute = "CloudNimble.Text.Xml.Serialization.XmlRootAttribute";
        internal const string SerializableAttribute = "CloudNimble.Text.Xml.Serialization.XmlSerializableAttribute";
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
                if (argument.Key == name && argument.Value.Value is int value)
                {
                    return value;
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
                    or "global::System.Byte[]";
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
        /// Returns the constructor argument at <paramref name="index"/> as a type.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="index">The argument index.</param>
        /// <returns>The type, or null.</returns>
        internal static ITypeSymbol? GetConstructorType(AttributeData attribute, int index)
        {
            if (index >= attribute.ConstructorArguments.Length)
            {
                return null;
            }

            return attribute.ConstructorArguments[index].Value as ITypeSymbol;
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
