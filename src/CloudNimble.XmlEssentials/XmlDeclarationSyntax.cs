using System;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Parses the pseudo-attributes of an XML declaration (XML 1.0 and 1.1, section 2.8, production 23).
    /// </summary>
    /// <remarks>
    /// <c>XMLDecl ::= '&lt;?xml' VersionInfo EncodingDecl? SDDecl? S? '?&gt;'</c>. The version is required,
    /// the three pseudo-attributes appear in that order, and nothing else is allowed. The grammar is
    /// ASCII only, which is also why a NEL or LS inside the declaration is a fatal error in XML 1.1
    /// (section 2.11).
    /// </remarks>
    internal static class XmlDeclarationSyntax
    {

        #region Public Methods

        /// <summary>
        /// Parses the text between <c>&lt;?xml</c> and <c>?&gt;</c>.
        /// </summary>
        /// <param name="utf8">The declaration body, without the target.</param>
        /// <param name="version">When this method returns true, the declared version.</param>
        /// <param name="encoding">When this method returns true, the declared encoding name, or empty when omitted.</param>
        /// <param name="standalone">When this method returns true, the standalone flag, or <see langword="null"/> when omitted.</param>
        /// <returns><see langword="true"/> if the declaration matches the production.</returns>
        /// <remarks>
        /// <c>1.1</c> selects XML 1.1. Any other <c>1.x</c> is read as XML 1.0, as the fifth edition asks
        /// of a 1.0 processor.
        /// </remarks>
        public static bool TryParse(ReadOnlySpan<byte> utf8, out XmlVersion version, out ReadOnlySpan<byte> encoding, out bool? standalone)
        {
            version = XmlVersion.Version10;
            encoding = default;
            standalone = null;
            if (!TryReadPseudoAttribute(utf8, "version"u8, requireLeadingSpace: false, out var versionText, out utf8)
                || versionText.Length < 3
                || versionText[0] is not (byte)'1'
                || versionText[1] is not (byte)'.'
                || versionText[2..].IndexOfAnyExceptInRange((byte)'0', (byte)'9') >= 0)
            {
                return false;
            }

            version = versionText.SequenceEqual("1.1"u8) ? XmlVersion.Version11 : XmlVersion.Version10;
            if (TryReadPseudoAttribute(utf8, "encoding"u8, requireLeadingSpace: true, out var encodingText, out utf8))
            {
                if (!IsEncodingName(encodingText))
                {
                    return false;
                }

                encoding = encodingText;
            }

            if (TryReadPseudoAttribute(utf8, "standalone"u8, requireLeadingSpace: true, out var standaloneText, out utf8))
            {
                if (standaloneText.SequenceEqual("yes"u8))
                {
                    standalone = true;
                }
                else if (standaloneText.SequenceEqual("no"u8))
                {
                    standalone = false;
                }
                else
                {
                    return false;
                }
            }

            return XmlLexical.Collapse(utf8).IsEmpty;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks <c>EncName ::= [A-Za-z] ([A-Za-z0-9._] | '-')*</c>.
        /// </summary>
        /// <param name="name">The encoding name.</param>
        /// <returns><see langword="true"/> if the name matches the production.</returns>
        internal static bool IsEncodingName(ReadOnlySpan<byte> name)
        {
            if (name.IsEmpty || !IsAsciiLetter(name[0]))
            {
                return false;
            }

            foreach (var b in name[1..])
            {
                if (!IsAsciiLetter(b) && b is not ((>= (byte)'0' and <= (byte)'9') or (byte)'.' or (byte)'_' or (byte)'-'))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns whether <paramref name="b"/> is an ASCII letter.
        /// </summary>
        /// <param name="b">The byte.</param>
        /// <returns><see langword="true"/> for <c>A-Z</c> and <c>a-z</c>.</returns>
        internal static bool IsAsciiLetter(byte b)
        {
            return (uint)((b | 0x20) - 'a') <= 'z' - 'a';
        }

        /// <summary>
        /// Reads <c>S? name Eq quoted-value</c> from the front of <paramref name="utf8"/>.
        /// </summary>
        /// <param name="utf8">The remaining declaration body.</param>
        /// <param name="name">The pseudo-attribute name.</param>
        /// <param name="requireLeadingSpace">True when whitespace must separate it from what came before.</param>
        /// <param name="value">When this method returns true, the unquoted value.</param>
        /// <param name="remainder">The body after the pseudo-attribute, or all of <paramref name="utf8"/> when it is absent.</param>
        /// <returns><see langword="true"/> if the named pseudo-attribute is next and is well formed.</returns>
        internal static bool TryReadPseudoAttribute(ReadOnlySpan<byte> utf8, scoped ReadOnlySpan<byte> name, bool requireLeadingSpace, out ReadOnlySpan<byte> value, out ReadOnlySpan<byte> remainder)
        {
            value = default;
            remainder = utf8;
            var rest = SkipWhitespace(utf8);
            if ((requireLeadingSpace && rest.Length == utf8.Length) || !rest.StartsWith(name))
            {
                return false;
            }

            rest = SkipWhitespace(rest[name.Length..]);
            if (rest.IsEmpty || rest[0] is not (byte)'=')
            {
                return false;
            }

            rest = SkipWhitespace(rest[1..]);
            if (rest.IsEmpty || rest[0] is not ((byte)'"' or (byte)'\''))
            {
                return false;
            }

            var end = rest[1..].IndexOf(rest[0]);
            if (end < 0)
            {
                return false;
            }

            value = rest.Slice(1, end);
            remainder = rest[(end + 2)..];
            return true;
        }

        /// <summary>
        /// Removes leading XML whitespace.
        /// </summary>
        /// <param name="utf8">The bytes.</param>
        /// <returns>The bytes from the first non-whitespace byte.</returns>
        internal static ReadOnlySpan<byte> SkipWhitespace(ReadOnlySpan<byte> utf8)
        {
            var i = 0;
            while (i < utf8.Length && XmlChars.IsWhiteSpace(utf8[i]))
            {
                i++;
            }

            return utf8[i..];
        }

        #endregion

    }

}
