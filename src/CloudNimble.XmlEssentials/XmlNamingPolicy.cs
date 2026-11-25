using System;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Defines the core behavior of policies that convert CLR type and member names to XML local names and provides a base for derived classes.
    /// </summary>
    /// <remarks>
    /// There is no identity policy. A <see langword="null"/> policy on <see cref="XmlSerializerOptions"/> leaves
    /// names unchanged, matching <c>System.Text.Json.JsonNamingPolicy</c>.
    /// </remarks>
    public abstract class XmlNamingPolicy
    {

        #region Public Properties

        /// <summary>
        /// Gets the naming policy for camel-casing.
        /// </summary>
        /// <value>
        /// A naming policy that converts the first character of a name to lowercase.
        /// </value>
        public static XmlNamingPolicy CamelCase { get; } = new XmlCamelCaseNamingPolicy();

        /// <summary>
        /// Gets the naming policy for lowercase kebab-casing.
        /// </summary>
        /// <value>
        /// A naming policy that separates the words of a name with <c>-</c> and converts every character to lowercase.
        /// </value>
        public static XmlNamingPolicy KebabCaseLower { get; } = new XmlKebabCaseLowerNamingPolicy();

        /// <summary>
        /// Gets the naming policy for uppercase kebab-casing.
        /// </summary>
        /// <value>
        /// A naming policy that separates the words of a name with <c>-</c> and converts every character to uppercase.
        /// </value>
        public static XmlNamingPolicy KebabCaseUpper { get; } = new XmlKebabCaseUpperNamingPolicy();

        /// <summary>
        /// Gets the naming policy for Pascal-casing.
        /// </summary>
        /// <value>
        /// A naming policy that converts the first character of a name to uppercase.
        /// </value>
        public static XmlNamingPolicy PascalCase { get; } = new XmlPascalCaseNamingPolicy();

        /// <summary>
        /// Gets the naming policy for lowercase snake-casing.
        /// </summary>
        /// <value>
        /// A naming policy that separates the words of a name with <c>_</c> and converts every character to lowercase.
        /// </value>
        public static XmlNamingPolicy SnakeCaseLower { get; } = new XmlSnakeCaseLowerNamingPolicy();

        /// <summary>
        /// Gets the naming policy for uppercase snake-casing.
        /// </summary>
        /// <value>
        /// A naming policy that separates the words of a name with <c>_</c> and converts every character to uppercase.
        /// </value>
        public static XmlNamingPolicy SnakeCaseUpper { get; } = new XmlSnakeCaseUpperNamingPolicy();

        #endregion

        #region Constructors

        /// <summary>
        /// Called from constructors in derived classes to initialize the <see cref="XmlNamingPolicy"/> class.
        /// </summary>
        protected XmlNamingPolicy()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// When overridden in a derived class, converts the specified CLR name to an XML local name according to the policy.
        /// </summary>
        /// <param name="name">The CLR name to convert.</param>
        /// <returns>
        /// The converted XML local name.
        /// </returns>
        public abstract string ConvertName(string name);

        #endregion

        #region Private Methods

        /// <summary>
        /// Lowercases the first character of <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The source name.</param>
        /// <returns>The camel-cased name.</returns>
        internal static string ToCamel(string name)
        {
            ArgumentNullException.ThrowIfNull(name);
            if (name.Length == 0 || char.IsLower(name[0]))
            {
                return name;
            }

            return string.Create(name.Length, name, static (span, value) =>
            {
                value.AsSpan().CopyTo(span);
                span[0] = char.ToLowerInvariant(span[0]);
            });
        }

        /// <summary>
        /// Uppercases the first character of <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The source name.</param>
        /// <returns>The Pascal-cased name.</returns>
        internal static string ToPascal(string name)
        {
            ArgumentNullException.ThrowIfNull(name);
            if (name.Length == 0 || char.IsUpper(name[0]))
            {
                return name;
            }

            return string.Create(name.Length, name, static (span, value) =>
            {
                value.AsSpan().CopyTo(span);
                span[0] = char.ToUpperInvariant(span[0]);
            });
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
            ArgumentNullException.ThrowIfNull(name);
            if (name.Length == 0)
            {
                return name;
            }

            Span<char> buffer = name.Length <= 64 ? stackalloc char[128] : new char[name.Length * 2];
            var written = 0;
            var previous = '\0';
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && NeedsSeparator(previous, c, i + 1 < name.Length ? name[i + 1] : '\0'))
                {
                    buffer[written++] = separator;
                }

                buffer[written++] = upper ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c);
                previous = c;
            }

            return new string(buffer[..written]);
        }

        /// <summary>
        /// Returns whether a separator belongs between <paramref name="previous"/> and <paramref name="current"/>.
        /// </summary>
        /// <param name="previous">The previous character.</param>
        /// <param name="current">The current character.</param>
        /// <param name="next">The next character, or NUL at the end.</param>
        /// <returns><see langword="true"/> when a word boundary is here.</returns>
        internal static bool NeedsSeparator(char previous, char current, char next)
        {
            var prevLower = char.IsLower(previous);
            var curUpper = char.IsUpper(current);
            var nextLower = char.IsLower(next);
            return (prevLower && curUpper)
                || (char.IsUpper(previous) && curUpper && nextLower);
        }

        #endregion

    }

}
