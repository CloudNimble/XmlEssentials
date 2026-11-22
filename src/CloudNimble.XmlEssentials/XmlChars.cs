using System.Buffers;
using System.Runtime.CompilerServices;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// XML 1.0 Fifth Edition and XML 1.1 Second Edition Char and Name productions.
    /// </summary>
    /// <remarks>
    /// These helpers take a Unicode code point, not a UTF-8 byte, except the ASCII/byte helpers.
    /// XML 1.0 5th edition names cover XML 1.1 NameStartChar / NameChar, so <see cref="XmlVersion"/>
    /// is reserved for Char and RestrictedChar, not names.
    /// ASCII scanners follow BCL <c>XmlCharType</c>: a cheap byte test on the hot path, full
    /// productions only for non-ASCII.
    /// </remarks>
    internal static class XmlChars
    {

        #region Fields

        /// <summary>
        /// ASCII NCName characters: letters, digits, <c>_</c>, <c>-</c>, <c>.</c>. Excludes colon.
        /// </summary>
        public static readonly SearchValues<byte> AsciiNcNameChars = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_.-"u8);

        /// <summary>
        /// ASCII text that needs no EOL, entity, or Char check: tab and printable ASCII except <c>&lt;</c> and <c>&amp;</c>.
        /// </summary>
        public static readonly SearchValues<byte> AsciiTextChars = SearchValues.Create("\t !\"#$%'()*+,-./0123456789:;=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"u8);

        /// <summary>
        /// Stops an end-tag name scan: colon, closer, or whitespace.
        /// </summary>
        public static readonly SearchValues<byte> EndTagNameStops = SearchValues.Create(":> \t\r\n"u8);

        /// <summary>
        /// Stops a double-quoted attribute value scan.
        /// </summary>
        public static readonly SearchValues<byte> AttrStopsDouble = SearchValues.Create("\"<&\r\n"u8);

        /// <summary>
        /// Stops a single-quoted attribute value scan.
        /// </summary>
        public static readonly SearchValues<byte> AttrStopsSingle = SearchValues.Create("'<&\r\n"u8);

        /// <summary>
        /// Stops an XML 1.0 text scan: markup, entity, or end-of-line.
        /// </summary>
        public static readonly SearchValues<byte> TextStops10 = SearchValues.Create("<&\r\n"u8);

        /// <summary>
        /// Stops an XML 1.1 text scan, including lead bytes of NEL (<c>C2</c>) and LS (<c>E2</c>).
        /// </summary>
        public static readonly SearchValues<byte> TextStops11 = SearchValues.Create([(byte)'<', (byte)'&', (byte)'\r', (byte)'\n', 0xC2, 0xE2]);

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns true if <paramref name="b"/> is an ASCII NameStartChar (<c>A-Z_a-z:</c>).
        /// </summary>
        /// <param name="b">The UTF-8 byte.</param>
        /// <returns><see langword="true"/> if the byte may start a name without decoding.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAsciiNameStart(byte b)
        {
            return b is (>= (byte)'A' and <= (byte)'Z') or (>= (byte)'a' and <= (byte)'z') or (byte)'_' or (byte)':';
        }

        /// <summary>
        /// Returns true if <paramref name="b"/> is an ASCII NCName character (no colon).
        /// </summary>
        /// <param name="b">The UTF-8 byte.</param>
        /// <returns><see langword="true"/> if the byte may continue a name without decoding.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAsciiNcNameChar(byte b)
        {
            return b is (>= (byte)'A' and <= (byte)'Z')
                or (>= (byte)'a' and <= (byte)'z')
                or (>= (byte)'0' and <= (byte)'9')
                or (byte)'_' or (byte)'-' or (byte)'.';
        }

        /// <summary>
        /// Returns true if <paramref name="cp"/> is a NameChar for <paramref name="version"/>.
        /// </summary>
        /// <param name="cp">The Unicode code point.</param>
        /// <param name="version">The document version.</param>
        /// <returns><see langword="true"/> if the code point may appear after the first character of a name.</returns>
        public static bool IsNameChar(int cp, XmlVersion version)
        {
            return IsNameStartChar(cp, version)
                || cp is '-' or '.' or (>= '0' and <= '9') or 0xB7
                || (cp is >= 0x0300 and <= 0x036F)
                || (cp is >= 0x203F and <= 0x2040);
        }

        /// <summary>
        /// Returns true if <paramref name="cp"/> is a NameStartChar for <paramref name="version"/>.
        /// </summary>
        /// <param name="cp">The Unicode code point.</param>
        /// <param name="version">The document version. XML 1.0 5th edition names cover XML 1.1 NameStartChar.</param>
        /// <returns><see langword="true"/> if the code point may start a name.</returns>
        public static bool IsNameStartChar(int cp, XmlVersion version)
        {
            _ = version;
            return cp is ':' or (>= 'A' and <= 'Z') or '_' or (>= 'a' and <= 'z')
                || (cp is >= 0xC0 and <= 0xD6)
                || (cp is >= 0xD8 and <= 0xF6)
                || (cp is >= 0xF8 and <= 0x2FF)
                || (cp is >= 0x370 and <= 0x37D)
                || (cp is >= 0x37F and <= 0x1FFF)
                || (cp is >= 0x200C and <= 0x200D)
                || (cp is >= 0x2070 and <= 0x218F)
                || (cp is >= 0x2C00 and <= 0x2FEF)
                || (cp is >= 0x3001 and <= 0xD7FF)
                || (cp is >= 0xF900 and <= 0xFDCF)
                || (cp is >= 0xFDF0 and <= 0xFFFD)
                || (cp is >= 0x10000 and <= 0xEFFFF);
        }

        /// <summary>
        /// Returns true if <paramref name="b"/> is XML whitespace (space, tab, LF, CR).
        /// </summary>
        /// <param name="b">The byte.</param>
        /// <returns><see langword="true"/> if the byte is whitespace.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWhiteSpace(byte b)
        {
            return b is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r';
        }

        /// <summary>
        /// Returns true if <paramref name="cp"/> is allowed in XML 1.0 character data.
        /// </summary>
        /// <param name="cp">The Unicode code point.</param>
        /// <returns><see langword="true"/> if the code point is an XML 1.0 Char.</returns>
        public static bool IsXml10Char(int cp)
        {
            return cp is 0x9 or 0xA or 0xD
                || (cp is >= 0x20 and <= 0xD7FF)
                || (cp is >= 0xE000 and <= 0xFFFD)
                || (cp is >= 0x10000 and <= 0x10FFFF);
        }

        /// <summary>
        /// Returns true if <paramref name="cp"/> is allowed in XML 1.1 character data (everything except NUL).
        /// </summary>
        /// <param name="cp">The Unicode code point.</param>
        /// <returns><see langword="true"/> if the code point is an XML 1.1 Char.</returns>
        public static bool IsXml11Char(int cp)
        {
            return cp is not 0
                && (cp is >= 0x1 and <= 0xD7FF or >= 0xE000 and <= 0xFFFD or >= 0x10000 and <= 0x10FFFF);
        }

        /// <summary>
        /// Returns true if <paramref name="cp"/> is an XML 1.1 RestrictedChar.
        /// RestrictedChar must appear as a numeric character reference, never as a literal.
        /// </summary>
        /// <param name="cp">The Unicode code point.</param>
        /// <returns><see langword="true"/> if the code point is RestrictedChar.</returns>
        public static bool IsXml11RestrictedChar(int cp)
        {
            return (cp is >= 0x1 and <= 0x8)
                || cp is 0xB or 0xC
                || (cp is >= 0xE and <= 0x1F)
                || (cp is >= 0x7F and <= 0x84)
                || (cp is >= 0x86 and <= 0x9F);
        }

        #endregion

    }

}
