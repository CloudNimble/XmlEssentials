using System;
using System.Text;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// Converts CLR names to XML local names using the source-generation naming policy.
    /// </summary>
    internal static class Naming
    {

        #region Public Methods

        /// <summary>
        /// Applies <paramref name="policy"/> to <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The CLR name.</param>
        /// <param name="policy">The known policy value (matches <c>XmlKnownNamingPolicy</c>).</param>
        /// <returns>The XML local name.</returns>
        internal static string Convert(string name, int policy)
        {
            return policy switch
            {
                1 => ToCamel(name),
                2 => ToSeparated(name, '_', upper: false),
                3 => ToSeparated(name, '_', upper: true),
                4 => ToSeparated(name, '-', upper: false),
                5 => ToSeparated(name, '-', upper: true),
                6 => ToPascal(name),
                _ => name,
            };
        }

        /// <summary>
        /// Returns the XML type name for a CLR type symbol display name.
        /// </summary>
        /// <param name="typeName">The metadata type name (no namespace).</param>
        /// <param name="isArray">Whether the type is an array.</param>
        /// <param name="elementName">The element type name when <paramref name="isArray"/> is true.</param>
        /// <param name="genericArguments">Generic argument XML names, or empty.</param>
        /// <returns>The XML local name.</returns>
        internal static string XmlTypeName(string typeName, bool isArray, string? elementName, string[] genericArguments)
        {
            if (isArray)
            {
                return elementName == "Byte" ? "ByteArray" : elementName + "Array";
            }

            var tick = typeName.IndexOf('`');
            var name = tick >= 0 ? typeName.Substring(0, tick) : typeName;
            if (genericArguments.Length == 0)
            {
                return name;
            }

            var builder = new StringBuilder(name.Length + 2);
            builder.Append(name);
            builder.Append("Of");
            foreach (var argument in genericArguments)
            {
                builder.Append(argument);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Returns whether <paramref name="name"/> is a valid unprefixed XML Name.
        /// </summary>
        /// <param name="name">The local name.</param>
        /// <returns><see langword="true"/> when the name can be an element or attribute.</returns>
        internal static bool IsXmlName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (!IsNameStart(name[0]))
            {
                return false;
            }

            for (var i = 1; i < name.Length; i++)
            {
                if (!IsNameChar(name[i]))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Lowercases the first character.
        /// </summary>
        /// <param name="name">The source name.</param>
        /// <returns>The camel-cased name.</returns>
        internal static string ToCamel(string name)
        {
            if (name.Length == 0 || char.IsLower(name[0]))
            {
                return name;
            }

            var chars = name.ToCharArray();
            chars[0] = char.ToLowerInvariant(chars[0]);
            return new string(chars);
        }

        /// <summary>
        /// Uppercases the first character.
        /// </summary>
        /// <param name="name">The source name.</param>
        /// <returns>The Pascal-cased name.</returns>
        internal static string ToPascal(string name)
        {
            if (name.Length == 0 || char.IsUpper(name[0]))
            {
                return name;
            }

            var chars = name.ToCharArray();
            chars[0] = char.ToUpperInvariant(chars[0]);
            return new string(chars);
        }

        /// <summary>
        /// Inserts <paramref name="separator"/> at word boundaries.
        /// </summary>
        /// <param name="name">The source name.</param>
        /// <param name="separator">The word separator.</param>
        /// <param name="upper">When true, every letter is uppercased.</param>
        /// <returns>The separated name.</returns>
        internal static string ToSeparated(string name, char separator, bool upper)
        {
            if (name.Length == 0)
            {
                return name;
            }

            var builder = new StringBuilder(name.Length * 2);
            var previous = '\0';
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && NeedsSeparator(previous, c, i + 1 < name.Length ? name[i + 1] : '\0'))
                {
                    builder.Append(separator);
                }

                builder.Append(upper ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
                previous = c;
            }

            return builder.ToString();
        }

        /// <summary>
        /// Returns whether a separator belongs between two characters.
        /// </summary>
        /// <param name="previous">The previous character.</param>
        /// <param name="current">The current character.</param>
        /// <param name="next">The next character, or NUL.</param>
        /// <returns><see langword="true"/> when a separator should be inserted.</returns>
        internal static bool NeedsSeparator(char previous, char current, char next)
        {
            if (previous == '\0')
            {
                return false;
            }

            if (char.IsLower(previous) && char.IsUpper(current))
            {
                return true;
            }

            return char.IsUpper(previous) && char.IsUpper(current) && char.IsLower(next);
        }

        /// <summary>
        /// Returns whether <paramref name="name"/> is an ASCII NCName (no colon), suitable for a prebuilt UTF-8 leaf tag.
        /// </summary>
        /// <param name="name">The local name.</param>
        /// <returns><see langword="true"/> when a <c>&lt;name&gt;</c> UTF-8 literal is valid.</returns>
        internal static bool IsAsciiNcName(string name)
        {
            if (name.Length == 0 || name[0] is ':' || !IsNameStart(name[0]) || name[0] > 0x7F)
            {
                return false;
            }

            for (var i = 1; i < name.Length; i++)
            {
                var c = name[i];
                if (c is ':' || c > 0x7F || !IsNameChar(c))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns whether <paramref name="c"/> can start an XML Name.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns><see langword="true"/> for a NameStartChar.</returns>
        internal static bool IsNameStart(char c)
        {
            return c is ':' or '_' || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        /// <summary>
        /// Returns whether <paramref name="c"/> can continue an XML Name.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns><see langword="true"/> for a NameChar.</returns>
        internal static bool IsNameChar(char c)
        {
            return IsNameStart(c) || c is '-' or '.' || (c >= '0' && c <= '9');
        }

        #endregion

    }

}
