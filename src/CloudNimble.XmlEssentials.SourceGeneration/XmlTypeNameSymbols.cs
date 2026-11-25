using System.Text;
using Microsoft.CodeAnalysis;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// The XML name of a type, worked out from its symbol by the rules the runtime library applies to a
    /// <see cref="System.Type"/> (<c>XmlTypeNames</c>): XML Schema names for the built-in types, <c>ArrayOf</c>
    /// and the item name for a collection, and the CLR name for anything else.
    /// </summary>
    /// <remarks>
    /// The generated write path writes these names itself, so the two sets of rules must give the same name
    /// for every type. <c>XmlSerializerGeneratedParityTests</c> compares what each writes.
    /// </remarks>
    internal static class XmlTypeNameSymbols
    {

        #region Public Methods

        /// <summary>
        /// Returns the CLR name of a type as it appears in the name of a generic type: <c>Int32</c>,
        /// <c>ListOfInt32</c>, <c>ArrayOfInt32</c>, <c>NullableOfInt32</c>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The name.</returns>
        internal static string ClrStyle(ITypeSymbol type)
        {
            if (type is IArrayTypeSymbol array)
            {
                return "ArrayOf" + ClrStyle(array.ElementType);
            }

            if (type is not INamedTypeSymbol { IsGenericType: true } named)
            {
                return type.Name;
            }

            // A type nested in a generic type is generic through it, and the arguments of the containing
            // types come first, as System.Type.GetGenericArguments lists them.
            var builder = new StringBuilder(named.Name).Append("Of");
            AppendArguments(builder, named);
            return builder.ToString();
        }

        /// <summary>
        /// Returns the element name of a type under a naming policy.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="policy">The naming policy for elements.</param>
        /// <returns>The name <c>[XmlRoot]</c> gives the type, or the type's name after the policy.</returns>
        internal static string ElementName(ITypeSymbol type, int policy)
        {
            var name = Of(type, out var isFromAttribute);
            return isFromAttribute ? name : Naming.Convert(name, policy);
        }

        /// <summary>
        /// Returns a name with a capital first letter and without the characters a C# identifier cannot hold.
        /// </summary>
        /// <param name="name">An XML name.</param>
        /// <returns>The name, such as <c>Int</c> for <c>int</c>.</returns>
        internal static string MakePascal(string name)
        {
            var builder = new StringBuilder(name.Length);
            foreach (var character in name)
            {
                if (char.IsLetterOrDigit(character) || character == '_')
                {
                    builder.Append(builder.Length == 0 ? char.ToUpperInvariant(character) : character);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Returns the XML name of a type, before any naming policy.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="isFromAttribute">
        /// When this method returns, <see langword="true"/> if the name is the one <c>[XmlRoot]</c> gives the
        /// type, which no naming policy changes.
        /// </param>
        /// <returns>The name.</returns>
        internal static string Of(ITypeSymbol type, out bool isFromAttribute)
        {
            var root = SymbolHelpers.UnwrapNullable(type);
            if (SymbolHelpers.GetAttribute(root, "CloudNimble.Text.Xml.Serialization.XmlRootAttribute") is { } attribute
                && (SymbolHelpers.GetString(attribute, "ElementName") ?? SymbolHelpers.GetConstructorString(attribute, 0)) is { Length: > 0 } name)
            {
                isFromAttribute = true;
                return name;
            }

            isFromAttribute = false;
            if (root is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
            {
                return "base64Binary";
            }

            if (SchemaName(root) is { } schemaName)
            {
                return schemaName;
            }

            return TypeGraphWalker.TryCollectionElement(root, out var item)
                ? "ArrayOf" + MakePascal(Of(item, out _))
                : ClrStyle(root);
        }

        #endregion

        #region Private Methods

        private static void AppendArguments(StringBuilder builder, INamedTypeSymbol type)
        {
            if (type.ContainingType is { } containing)
            {
                AppendArguments(builder, containing);
            }

            foreach (var argument in type.TypeArguments)
            {
                builder.Append(ClrStyle(argument));
            }
        }

        private static string? SchemaName(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return "boolean";
                case SpecialType.System_Byte:
                    return "unsignedByte";
                case SpecialType.System_Char:
                    return "char";
                case SpecialType.System_DateTime:
                    return "dateTime";
                case SpecialType.System_Decimal:
                    return "decimal";
                case SpecialType.System_Double:
                    return "double";
                case SpecialType.System_Int16:
                    return "short";
                case SpecialType.System_Int32:
                    return "int";
                case SpecialType.System_Int64:
                    return "long";
                case SpecialType.System_Object:
                    return "anyType";
                case SpecialType.System_SByte:
                    return "byte";
                case SpecialType.System_Single:
                    return "float";
                case SpecialType.System_String:
                    return "string";
                case SpecialType.System_UInt16:
                    return "unsignedShort";
                case SpecialType.System_UInt32:
                    return "unsignedInt";
                case SpecialType.System_UInt64:
                    return "unsignedLong";
            }

            switch (SymbolHelpers.FullName(type))
            {
                case "global::System.DateOnly":
                    return "dateOnly";
                case "global::System.DateTimeOffset":
                    return "dateTimeOffset";
                case "global::System.Guid":
                    return "guid";
                case "global::System.TimeOnly":
                    return "timeOnly";
                default:
                    return null;
            }
        }

        #endregion

    }

}
