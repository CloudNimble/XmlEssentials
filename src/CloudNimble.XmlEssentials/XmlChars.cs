using System;
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
        /// Bytes that force an attribute value to be decoded: an entity start or whitespace that normalizes to a space.
        /// </summary>
        public static readonly SearchValues<byte> AttrDecodeTriggers = SearchValues.Create("&\t\n\r"u8);

        /// <summary>
        /// ASCII attribute-value bytes the writer copies verbatim: printable ASCII except
        /// <c>&lt;</c>, <c>&amp;</c>, and <c>"</c>.
        /// </summary>
        /// <remarks>
        /// TAB, LF, and CR are excluded: a reader normalizes them to a space, so they are written as references.
        /// </remarks>
        public static readonly SearchValues<byte> AsciiAttributeWriteChars = SearchValues.Create(" !#$%'()*+,-./0123456789:;=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"u8);

        /// <summary>
        /// ASCII element-text bytes the writer copies verbatim: TAB, LF, and printable ASCII except
        /// <c>&lt;</c>, <c>&amp;</c>, and <c>]</c> (which may start a <c>]]&gt;</c> that must be escaped).
        /// </summary>
        /// <remarks>
        /// CR is excluded: a reader normalizes it to LF, so it is written as a reference.
        /// </remarks>
        public static readonly SearchValues<byte> AsciiTextWriteChars = SearchValues.Create("\t\n !\"#$%'()*+,-./0123456789:;=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\^_`abcdefghijklmnopqrstuvwxyz{|}~"u8);

        /// <summary>
        /// ASCII bytes that are legal, as literals, in comments, CDATA sections, and processing instruction data
        /// in both XML versions: TAB, LF, CR, and printable ASCII.
        /// </summary>
        public static readonly SearchValues<byte> AsciiContentChars = SearchValues.Create("\t\n\r !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"u8);

        /// <summary>
        /// UTF-16 form of <see cref="AsciiAttributeWriteChars"/>: attribute text made only of these is copied verbatim.
        /// </summary>
        /// <remarks>
        /// An allow-list, not a list of characters to escape: control characters are ASCII too, and they
        /// are illegal in XML 1.0 and must be references in XML 1.1.
        /// </remarks>
        public static readonly SearchValues<char> PlainAttributeChars = SearchValues.Create(" !#$%'()*+,-./0123456789:;=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~");

        /// <summary>
        /// UTF-16 form of <see cref="AsciiTextWriteChars"/>: element text made only of these is copied verbatim.
        /// </summary>
        public static readonly SearchValues<char> PlainTextChars = SearchValues.Create("\t\n !\"#$%'()*+,-./0123456789:;=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\^_`abcdefghijklmnopqrstuvwxyz{|}~");

        /// <summary>
        /// Bytes of a double-quoted attribute value that are the value as written: printable ASCII except
        /// <c>"</c>, <c>&lt;</c>, and <c>&amp;</c>. No reference, no whitespace to normalize, no character to check.
        /// </summary>
        public static readonly SearchValues<byte> AttrPlainDouble = SearchValues.Create(" !#$%'()*+,-./0123456789:;=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"u8);

        /// <summary>
        /// Bytes of a single-quoted attribute value that are the value as written: printable ASCII except
        /// <c>'</c>, <c>&lt;</c>, and <c>&amp;</c>.
        /// </summary>
        public static readonly SearchValues<byte> AttrPlainSingle = SearchValues.Create(" !\"#$%()*+,-./0123456789:;=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"u8);

        /// <summary>
        /// <see cref="AsciiTextChars"/> plus every byte of a multi-byte UTF-8 sequence (0x80 to 0xFF).
        /// </summary>
        /// <remarks>
        /// A run made only of these bytes holds legal characters exactly when it is valid UTF-8 and has
        /// neither U+FFFE nor U+FFFF, which lets the reader check a whole run of non-ASCII text at once.
        /// </remarks>
        public static readonly SearchValues<byte> TextRunChars = SearchValues.Create(CreateTextRunChars());

        /// <summary>
        /// <see cref="PlainAsciiChars"/> without <c>]</c>, which may start the end of a CDATA section.
        /// </summary>
        public static readonly SearchValues<byte> CDataPlainChars = SearchValues.Create("\t !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\^_`abcdefghijklmnopqrstuvwxyz{|}~"u8);

        /// <summary>
        /// The byte a line end that XML 1.0 replaces can start with: CR.
        /// </summary>
        public static readonly SearchValues<byte> DecodeStops10 = SearchValues.Create("\r"u8);

        /// <summary>
        /// <see cref="DecodeStops10"/> and the UTF-8 lead bytes of NEL (U+0085) and LS (U+2028), which XML 1.1 reads as
        /// line ends.
        /// </summary>
        public static readonly SearchValues<byte> DecodeStops11 = SearchValues.Create([(byte)'\r', 0xC2, 0xE2]);

        /// <summary>
        /// <see cref="PlainAsciiChars"/> without <c>-</c>, which may start the end of a comment.
        /// </summary>
        public static readonly SearchValues<byte> CommentPlainChars = SearchValues.Create("\t !\"#$%&'()*+,./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"u8);

        /// <summary>
        /// <see cref="PlainAsciiChars"/> without <c>&lt;</c>, <c>&amp;</c> and <c>&gt;</c>: text that ends at the next tag and
        /// needs no decoding.
        /// </summary>
        public static readonly SearchValues<byte> LeafTextChars = SearchValues.Create("\t !\"#$%'()*+,-./0123456789:;=?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"u8);

        /// <summary>
        /// Tab and every printable ASCII character. A run made only of these bytes holds legal characters in
        /// both XML versions and no line ends, so it needs no per-character check and moves no line counter.
        /// </summary>
        public static readonly SearchValues<byte> PlainAsciiChars = SearchValues.Create("\t !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"u8);

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
        /// Stops an XML 1.0 text scan: markup, entity, end-of-line, or a <c>&gt;</c> that may close a forbidden <c>]]&gt;</c>.
        /// </summary>
        public static readonly SearchValues<byte> TextStops10 = SearchValues.Create("<&>\r\n"u8);

        /// <summary>
        /// Stops an XML 1.1 text scan, including lead bytes of NEL (<c>C2</c>) and LS (<c>E2</c>).
        /// </summary>
        public static readonly SearchValues<byte> TextStops11 = SearchValues.Create([(byte)'<', (byte)'&', (byte)'>', (byte)'\r', (byte)'\n', 0xC2, 0xE2]);

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns true if valid UTF-8 <paramref name="utf8"/> contains U+FFFE or U+FFFF, which are not XML characters.
        /// </summary>
        /// <param name="utf8">Valid UTF-8 bytes.</param>
        /// <returns><see langword="true"/> if either noncharacter is present.</returns>
        public static bool ContainsNonCharacter(ReadOnlySpan<byte> utf8)
        {
            // Both encode as EF BF followed by BE or BF. In valid UTF-8 that pair is always a whole
            // three-byte sequence with its last byte, so the index after it is in range.
            ReadOnlySpan<byte> lead = [0xEF, 0xBF];
            while (true)
            {
                var index = utf8.IndexOf(lead);
                if (index < 0)
                {
                    return false;
                }

                if (utf8[index + 2] >= 0xBE)
                {
                    return true;
                }

                utf8 = utf8[(index + 3)..];
            }
        }

        /// <summary>
        /// Builds the byte set behind <see cref="TextRunChars"/>.
        /// </summary>
        /// <returns>Tab, printable ASCII except <c>&lt;</c> and <c>&amp;</c>, and every byte from 0x80 to 0xFF.</returns>
        public static byte[] CreateTextRunChars()
        {
            var bytes = new byte[1 + (0x7E - 0x20 + 1 - 2) + 0x80];
            var count = 0;
            bytes[count++] = (byte)'\t';
            for (var b = 0x20; b <= 0xFF; b++)
            {
                if (b is not ('<' or '&' or 0x7F))
                {
                    bytes[count++] = (byte)b;
                }
            }

            return bytes;
        }

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
        /// Returns true if <paramref name="name"/> is at most <paramref name="maxLength"/> bytes of ASCII that
        /// form an NCName.
        /// </summary>
        /// <param name="name">The UTF-8 name.</param>
        /// <param name="maxLength">The longest name to check here.</param>
        /// <returns>
        /// <see langword="true"/> if the name is a valid NCName in both XML versions; <see langword="false"/> if
        /// it is not, or if deciding needs more than a look at each byte.
        /// </returns>
        /// <remarks>
        /// Element and attribute names are short, and a byte loop over a short name costs less than starting
        /// a vectorized search.
        /// </remarks>
        public static bool IsShortAsciiNcName(ReadOnlySpan<byte> name, int maxLength)
        {
            if ((uint)(name.Length - 1) >= (uint)maxLength || !IsAsciiNameStart(name[0]) || name[0] is (byte)':')
            {
                return false;
            }

            for (var i = 1; i < name.Length; i++)
            {
                if (!IsAsciiNcNameChar(name[i]))
                {
                    return false;
                }
            }

            return true;
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
