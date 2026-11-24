using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// The XML name of a CLR type: the name of the document element when a value of the type is a document, and
    /// of the items of a collection of the type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The names are those <c>System.Xml.Serialization.XmlSerializer</c> writes, so a document that either
    /// serializer wrote for a value or a collection has the same names:
    /// </para>
    /// <list type="bullet">
    /// <item><description>A built-in type has the name of its XML Schema type: <c>int</c>, <c>string</c>, <c>boolean</c>, <c>dateTime</c>, <c>base64Binary</c>. The types XML Schema has no name for follow the same pattern (<c>guid</c>, <c>dateOnly</c>, <c>dateTimeOffset</c>).</description></item>
    /// <item><description>A collection is <c>ArrayOf</c> followed by the name of its items with a capital first letter, whatever the collection type is: <c>ArrayOfInt</c> for <c>int[]</c>, <c>List&lt;int&gt;</c> and <c>HashSet&lt;int&gt;</c> alike.</description></item>
    /// <item><description>Any other type has its CLR name, and a generic type adds <c>Of</c> and the CLR names of its type arguments: <c>BoxOfInt32</c>, <c>DictionaryOfStringInt32</c>.</description></item>
    /// </list>
    /// <para>
    /// <see cref="XmlRootAttribute"/> names a type wherever the type names an element, so a collection of a type
    /// marked <c>[XmlRoot("rss")]</c> is <c>ArrayOfRss</c> with <c>rss</c> items.
    /// </para>
    /// </remarks>
    internal static class XmlTypeNames
    {

        #region Fields

        private static readonly FrozenDictionary<Type, string> s_schemaNames = new Dictionary<Type, string>
        {
            [typeof(bool)] = "boolean",
            [typeof(byte)] = "unsignedByte",
            [typeof(byte[])] = "base64Binary",
            [typeof(char)] = "char",
            [typeof(DateOnly)] = "dateOnly",
            [typeof(DateTime)] = "dateTime",
            [typeof(DateTimeOffset)] = "dateTimeOffset",
            [typeof(decimal)] = "decimal",
            [typeof(double)] = "double",
            [typeof(float)] = "float",
            [typeof(Guid)] = "guid",
            [typeof(int)] = "int",
            [typeof(long)] = "long",
            [typeof(object)] = "anyType",
            [typeof(sbyte)] = "byte",
            [typeof(short)] = "short",
            [typeof(string)] = "string",
            [typeof(TimeOnly)] = "timeOnly",
            [typeof(uint)] = "unsignedInt",
            [typeof(ulong)] = "unsignedLong",
            [typeof(ushort)] = "unsignedShort",
        }.ToFrozenDictionary();

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the CLR name of a type as it appears in the name of a generic type: <c>Int32</c>,
        /// <c>ListOfInt32</c>, <c>ArrayOfInt32</c>, <c>NullableOfInt32</c>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The name.</returns>
        internal static string ClrStyle(Type type)
        {
            if (type.IsArray)
            {
                return "ArrayOf" + ClrStyle(type.GetElementType()!);
            }

            if (!type.IsGenericType)
            {
                return type.Name;
            }

            var name = type.Name;
            var tick = name.IndexOf('`');
            var builder = new StringBuilder(tick < 0 ? name : name[..tick]).Append("Of");
            foreach (var argument in type.GetGenericArguments())
            {
                builder.Append(ClrStyle(argument));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Returns the element name of a type under a naming policy.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The name <see cref="XmlRootAttribute"/> gives the type, or the type's name after the policy.</returns>
        internal static string ElementName(Type type, XmlSerializerOptions options)
        {
            var name = Of(type, out var isFromAttribute);
            return isFromAttribute ? name : options.ConvertElementName(name);
        }

        /// <summary>
        /// Returns a name with a capital first letter and without the characters a C# identifier cannot hold,
        /// which is how the name of an item is made part of the name of its collection.
        /// </summary>
        /// <param name="name">An XML name.</param>
        /// <returns>The name, such as <c>Int</c> for <c>int</c> and <c>Typename</c> for <c>type-name</c>.</returns>
        internal static string MakePascal(string name)
        {
            var builder = new StringBuilder(name.Length);
            foreach (var character in name)
            {
                if (char.IsLetterOrDigit(character) || character is '_')
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
        /// When this method returns, <see langword="true"/> if the name is the one <see cref="XmlRootAttribute"/>
        /// gives the type, which no naming policy changes.
        /// </param>
        /// <returns>The name.</returns>
        internal static string Of(Type type, out bool isFromAttribute)
        {
            var root = Nullable.GetUnderlyingType(type) ?? type;
            if (((XmlRootAttribute?)Attribute.GetCustomAttribute(root, typeof(XmlRootAttribute), inherit: true))?.ElementName is { Length: > 0 } name)
            {
                isFromAttribute = true;
                return name;
            }

            isFromAttribute = false;
            if (root.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) && root.Name.Contains("AnonymousType", StringComparison.Ordinal))
            {
                // The compiler names an anonymous type with characters no XML name may hold, and the name
                // changes from one build to the next. Every anonymous type shares this one.
                return "AnonymousType";
            }

            if (s_schemaNames.TryGetValue(root, out var schemaName))
            {
                return schemaName;
            }

            return XmlCollectionShape.IsCollection(root, out var item)
                ? SequenceOf(Of(item, out _))
                : ClrStyle(root);
        }

        /// <summary>
        /// Returns the element name of a sequence of values of a type, under a naming policy: the name a
        /// collection of the type has as a document.
        /// </summary>
        /// <param name="itemType">The type of the values.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The name.</returns>
        internal static string SequenceElementName(Type itemType, XmlSerializerOptions options)
        {
            return options.ConvertElementName(SequenceOf(Of(itemType, out _)));
        }

        /// <summary>
        /// Returns the name of a sequence of items.
        /// </summary>
        /// <param name="itemName">The XML name of an item.</param>
        /// <returns><c>ArrayOf</c> followed by the item name with a capital first letter.</returns>
        internal static string SequenceOf(string itemName)
        {
            return "ArrayOf" + MakePascal(itemName);
        }

        #endregion

    }

}
