using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// XML Schema 1.0 lexical-to-value mappings shared by <see cref="Utf8XmlReader"/>, <see cref="XmlElement"/>,
    /// and the serializer.
    /// </summary>
    /// <remarks>
    /// Every method applies the <c>whiteSpace = collapse</c> facet first (XML Schema Part 2, section 4.3.6),
    /// which all of these datatypes carry: leading and trailing XML whitespace is removed, and none of
    /// the lexical spaces below allow whitespace inside the value. The raw text is still available,
    /// untrimmed, from <c>GetString</c>.
    /// One implementation is the point: the reader and the DOM must never disagree about what a value means.
    /// </remarks>
    internal static class XmlLexical
    {

        #region Public Methods

        /// <summary>
        /// Removes leading and trailing XML whitespace (space, TAB, LF, CR).
        /// </summary>
        /// <param name="utf8">The raw value.</param>
        /// <returns>The collapsed value.</returns>
        public static ReadOnlySpan<byte> Collapse(ReadOnlySpan<byte> utf8)
        {
            var start = 0;
            var end = utf8.Length;
            while (start < end && XmlChars.IsWhiteSpace(utf8[start]))
            {
                start++;
            }

            while (end > start && XmlChars.IsWhiteSpace(utf8[end - 1]))
            {
                end--;
            }

            return utf8[start..end];
        }

        /// <summary>
        /// Decodes <c>xs:base64Binary</c>.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the decoded bytes.</param>
        /// <returns><see langword="true"/> if the value is valid Base64.</returns>
        public static bool TryDecodeBase64(ReadOnlySpan<byte> utf8, [NotNullWhen(true)] out byte[]? value)
        {
            var s = Collapse(utf8);
            if (s.IsEmpty)
            {
                value = [];
                return true;
            }

            var destination = new byte[Base64.GetMaxDecodedFromUtf8Length(s.Length)];
            if (Base64.DecodeFromUtf8(s, destination, out _, out var written) is not OperationStatus.Done)
            {
                value = null;
                return false;
            }

            value = written == destination.Length ? destination : destination.AsSpan(0, written).ToArray();
            return true;
        }

        /// <summary>
        /// Parses <c>xs:boolean</c>: <c>true</c>, <c>false</c>, <c>1</c>, or <c>0</c>.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is a boolean.</returns>
        public static bool TryParseBoolean(ReadOnlySpan<byte> utf8, out bool value)
        {
            var s = Collapse(utf8);
            value = s.SequenceEqual("true"u8) || s.SequenceEqual("1"u8);
            return value || s.SequenceEqual("false"u8) || s.SequenceEqual("0"u8);
        }

        /// <summary>
        /// Parses <c>xs:unsignedByte</c>.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is in range.</returns>
        public static bool TryParseByte(ReadOnlySpan<byte> utf8, out byte value)
        {
            var parsed = TryParseUInt64(utf8, out var wide) && wide <= byte.MaxValue;
            value = parsed ? (byte)wide : default;
            return parsed;
        }

        /// <summary>
        /// Parses <c>xs:date</c> (<c>YYYY-MM-DD</c> with an optional time zone, which is validated and dropped).
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is a date that <see cref="DateOnly"/> can represent.</returns>
        public static bool TryParseDateOnly(ReadOnlySpan<byte> utf8, out DateOnly value)
        {
            var s = Collapse(utf8);
            if (!TryReadDate(ref s, out var year, out var month, out var day) || !TryReadZone(s, out _, out _))
            {
                value = default;
                return false;
            }

            value = new DateOnly(year, month, day);
            return true;
        }

        /// <summary>
        /// Parses <c>xs:dateTime</c> (<c>YYYY-MM-DDThh:mm:ss[.fff][Z|(+|-)hh:mm]</c>).
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is a dateTime that <see cref="DateTime"/> can represent.</returns>
        /// <remarks>
        /// No time zone yields <see cref="DateTimeKind.Unspecified"/>, <c>Z</c> yields <see cref="DateTimeKind.Utc"/>,
        /// and an explicit offset is converted to local time (<see cref="DateTimeKind.Local"/>), matching
        /// <see cref="System.Globalization.DateTimeStyles.RoundtripKind"/>.
        /// </remarks>
        public static bool TryParseDateTime(ReadOnlySpan<byte> utf8, out DateTime value)
        {
            if (!TryReadDateTime(utf8, out var local, out var hasZone, out var offset))
            {
                value = default;
                return false;
            }

            if (!hasZone)
            {
                value = local;
                return true;
            }

            var utcTicks = local.Ticks - offset.Ticks;
            if (utcTicks < DateTime.MinValue.Ticks || utcTicks > DateTime.MaxValue.Ticks)
            {
                value = default;
                return false;
            }

            var utc = new DateTime(utcTicks, DateTimeKind.Utc);
            value = offset == TimeSpan.Zero ? utc : utc.ToLocalTime();
            return true;
        }

        /// <summary>
        /// Parses <c>xs:dateTime</c> into a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is a dateTime that <see cref="DateTimeOffset"/> can represent.</returns>
        /// <remarks>
        /// A value without a time zone is a valid <c>xs:dateTime</c>; it takes the local offset in force at
        /// that instant, as <c>System.Xml.XmlConvert.ToDateTimeOffset</c> does.
        /// </remarks>
        public static bool TryParseDateTimeOffset(ReadOnlySpan<byte> utf8, out DateTimeOffset value)
        {
            if (!TryReadDateTime(utf8, out var local, out var hasZone, out var offset))
            {
                value = default;
                return false;
            }

            if (!hasZone)
            {
                offset = TimeZoneInfo.Local.GetUtcOffset(local);
            }

            var utcTicks = local.Ticks - offset.Ticks;
            if (utcTicks < DateTime.MinValue.Ticks || utcTicks > DateTime.MaxValue.Ticks)
            {
                value = default;
                return false;
            }

            value = new DateTimeOffset(local.Ticks, offset);
            return true;
        }

        /// <summary>
        /// Parses <c>xs:decimal</c>: an optional sign, digits, and an optional fraction. No exponent.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is a decimal in range.</returns>
        public static bool TryParseDecimal(ReadOnlySpan<byte> utf8, out decimal value)
        {
            var s = Collapse(utf8);
            var body = StripPlus(s);
            var unsigned = body.Length > 0 && body[0] is (byte)'-' && body.Length == s.Length ? body[1..] : body;
            if (ScanDecimal(unsigned) != unsigned.Length || unsigned.IsEmpty)
            {
                value = default;
                return false;
            }

            return Utf8Parser.TryParse(body, out value, out var consumed) && consumed == body.Length;
        }

        /// <summary>
        /// Parses <c>xs:double</c>: a decimal mantissa with an optional exponent, or <c>INF</c>, <c>-INF</c>, <c>NaN</c>.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is a double.</returns>
        /// <remarks>
        /// The named literals are also accepted in any letter case, a documented extension.
        /// </remarks>
        public static bool TryParseDouble(ReadOnlySpan<byte> utf8, out double value)
        {
            var s = Collapse(utf8);
            if (TryParseNamedFloat(s, out value))
            {
                return true;
            }

            var body = StripPlus(s);
            return IsFloatLexical(s, body) && Utf8Parser.TryParse(body, out value, out var consumed) && consumed == body.Length;
        }

        /// <summary>
        /// Parses an <c>xs:duration</c> that has no year or month component.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is a day-time duration that fits in <see cref="TimeSpan"/>.</returns>
        /// <remarks>
        /// <c>[-]P[nD][T[nH][nM][n[.n]S]]</c> with at least one component, components in that order, and
        /// at least one component after <c>T</c>. Years and months have no fixed length, so a duration that
        /// names them (even as zero) is rejected rather than approximated.
        /// </remarks>
        public static bool TryParseDuration(ReadOnlySpan<byte> utf8, out TimeSpan value)
        {
            value = default;
            var s = Collapse(utf8);
            var negative = s.Length > 0 && s[0] is (byte)'-';
            if (negative)
            {
                s = s[1..];
            }

            if (s.Length < 2 || s[0] is not (byte)'P')
            {
                return false;
            }

            s = s[1..];
            decimal ticks = 0;
            var components = 0;
            if (s[0] is not (byte)'T')
            {
                if (!TryReadDigits(ref s, out var days) || s.IsEmpty || s[0] is not (byte)'D')
                {
                    return false;
                }

                ticks += (decimal)days * TimeSpan.TicksPerDay;
                components++;
                s = s[1..];
            }

            if (!s.IsEmpty)
            {
                if (s[0] is not (byte)'T')
                {
                    return false;
                }

                s = s[1..];
                var timeComponents = 0;
                var order = 0;
                while (!s.IsEmpty)
                {
                    if (!TryReadDigits(ref s, out var number) || s.IsEmpty)
                    {
                        return false;
                    }

                    decimal fraction = 0;
                    if (s[0] is (byte)'.')
                    {
                        s = s[1..];
                        if (!TryReadFraction(ref s, out var fractionTicks) || s.IsEmpty || s[0] is not (byte)'S')
                        {
                            return false;
                        }

                        fraction = fractionTicks;
                    }

                    var unit = s[0];
                    s = s[1..];
                    var rank = unit switch
                    {
                        (byte)'H' => 1,
                        (byte)'M' => 2,
                        (byte)'S' => 3,
                        _ => 0,
                    };
                    if (rank <= order)
                    {
                        return false;
                    }

                    order = rank;
                    ticks += rank switch
                    {
                        1 => (decimal)number * TimeSpan.TicksPerHour,
                        2 => (decimal)number * TimeSpan.TicksPerMinute,
                        _ => ((decimal)number * TimeSpan.TicksPerSecond) + fraction,
                    };
                    timeComponents++;
                }

                if (timeComponents == 0)
                {
                    return false;
                }

                components += timeComponents;
            }

            if (components == 0 || ticks > long.MaxValue)
            {
                return false;
            }

            value = new TimeSpan(negative ? -(long)ticks : (long)ticks);
            return true;
        }

        /// <summary>
        /// Parses a GUID in <c>D</c>, <c>N</c>, <c>B</c>, or <c>P</c> format.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is a GUID.</returns>
        public static bool TryParseGuid(ReadOnlySpan<byte> utf8, out Guid value)
        {
            var s = Collapse(utf8);
            var format = s.Length switch
            {
                36 => 'D',
                32 => 'N',
                38 => s[0] is (byte)'{' ? 'B' : 'P',
                _ => '\0',
            };
            if (format is '\0')
            {
                value = default;
                return false;
            }

            return Utf8Parser.TryParse(s, out value, out var consumed, format) && consumed == s.Length;
        }

        /// <summary>
        /// Parses <c>xs:short</c>.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is in range.</returns>
        public static bool TryParseInt16(ReadOnlySpan<byte> utf8, out short value)
        {
            var parsed = TryParseInt64(utf8, out var wide) && wide is >= short.MinValue and <= short.MaxValue;
            value = parsed ? (short)wide : default;
            return parsed;
        }

        /// <summary>
        /// Parses <c>xs:int</c>.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is in range.</returns>
        public static bool TryParseInt32(ReadOnlySpan<byte> utf8, out int value)
        {
            if (!TryParseInteger(utf8, out var negative, out var magnitude)
                || magnitude > (negative ? 2147483648UL : int.MaxValue))
            {
                value = default;
                return false;
            }

            value = negative ? (int)(0L - (long)magnitude) : (int)magnitude;
            return true;
        }

        /// <summary>
        /// Parses <c>xs:long</c>: an optional <c>+</c> or <c>-</c> followed by decimal digits.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is in range.</returns>
        public static bool TryParseInt64(ReadOnlySpan<byte> utf8, out long value)
        {
            if (!TryParseInteger(utf8, out var negative, out var magnitude)
                || magnitude > (negative ? 9223372036854775808UL : long.MaxValue))
            {
                value = default;
                return false;
            }

            value = negative ? unchecked((long)(0UL - magnitude)) : (long)magnitude;
            return true;
        }

        /// <summary>
        /// Parses <c>xs:byte</c>.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is in range.</returns>
        public static bool TryParseSByte(ReadOnlySpan<byte> utf8, out sbyte value)
        {
            var parsed = TryParseInt64(utf8, out var wide) && wide is >= sbyte.MinValue and <= sbyte.MaxValue;
            value = parsed ? (sbyte)wide : default;
            return parsed;
        }

        /// <summary>
        /// Parses <c>xs:float</c>: the <c>xs:double</c> lexical space, rounded to single precision.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is a float.</returns>
        public static bool TryParseSingle(ReadOnlySpan<byte> utf8, out float value)
        {
            var s = Collapse(utf8);
            if (TryParseNamedFloat(s, out var named))
            {
                value = (float)named;
                return true;
            }

            var body = StripPlus(s);
            value = default;
            return IsFloatLexical(s, body) && Utf8Parser.TryParse(body, out value, out var consumed) && consumed == body.Length;
        }

        /// <summary>
        /// Parses <c>xs:time</c> (<c>hh:mm:ss[.fff]</c> with an optional time zone, which is validated and dropped).
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is a time.</returns>
        public static bool TryParseTimeOnly(ReadOnlySpan<byte> utf8, out TimeOnly value)
        {
            var s = Collapse(utf8);
            if (!TryReadTime(ref s, out var ticks, out _) || !TryReadZone(s, out _, out _))
            {
                value = default;
                return false;
            }

            value = new TimeOnly(ticks);
            return true;
        }

        /// <summary>
        /// Parses <c>xs:unsignedShort</c>.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is in range.</returns>
        public static bool TryParseUInt16(ReadOnlySpan<byte> utf8, out ushort value)
        {
            var parsed = TryParseUInt64(utf8, out var wide) && wide <= ushort.MaxValue;
            value = parsed ? (ushort)wide : default;
            return parsed;
        }

        /// <summary>
        /// Parses <c>xs:unsignedInt</c>.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is in range.</returns>
        public static bool TryParseUInt32(ReadOnlySpan<byte> utf8, out uint value)
        {
            var parsed = TryParseUInt64(utf8, out var wide) && wide <= uint.MaxValue;
            value = parsed ? (uint)wide : default;
            return parsed;
        }

        /// <summary>
        /// Parses <c>xs:unsignedLong</c>: an optional <c>+</c> followed by decimal digits. <c>-0</c> is zero.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="value">When this method returns true, the parsed value.</param>
        /// <returns><see langword="true"/> if the value is in range.</returns>
        public static bool TryParseUInt64(ReadOnlySpan<byte> utf8, out ulong value)
        {
            var parsed = TryParseInteger(utf8, out var negative, out value) && (!negative || value == 0);
            value = parsed ? value : default;
            return parsed;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks the <c>xs:double</c> lexical form: a decimal mantissa and an optional integer exponent.
        /// </summary>
        /// <param name="collapsed">The collapsed value.</param>
        /// <param name="body">The value with a leading <c>+</c> removed.</param>
        /// <returns><see langword="true"/> if the form is legal.</returns>
        internal static bool IsFloatLexical(ReadOnlySpan<byte> collapsed, ReadOnlySpan<byte> body)
        {
            var rest = body.Length > 0 && body[0] is (byte)'-' && body.Length == collapsed.Length ? body[1..] : body;
            var mantissa = ScanDecimal(rest);
            if (mantissa <= 0)
            {
                return false;
            }

            rest = rest[mantissa..];
            if (rest.IsEmpty)
            {
                return true;
            }

            if (rest[0] is not ((byte)'e' or (byte)'E'))
            {
                return false;
            }

            rest = rest[1..];
            if (rest.Length > 0 && rest[0] is (byte)'+' or (byte)'-')
            {
                rest = rest[1..];
            }

            return !rest.IsEmpty && rest.IndexOfAnyExceptInRange((byte)'0', (byte)'9') < 0;
        }

        /// <summary>
        /// Returns the length of the leading <c>digits[.digits]</c> or <c>.digits</c> run, or 0 when there is no digit.
        /// </summary>
        /// <param name="utf8">The value after any sign.</param>
        /// <returns>The number of bytes in the unsigned decimal numeral.</returns>
        internal static int ScanDecimal(ReadOnlySpan<byte> utf8)
        {
            var i = 0;
            var digits = 0;
            while (i < utf8.Length && utf8[i] is >= (byte)'0' and <= (byte)'9')
            {
                i++;
                digits++;
            }

            if (i < utf8.Length && utf8[i] is (byte)'.')
            {
                i++;
                while (i < utf8.Length && utf8[i] is >= (byte)'0' and <= (byte)'9')
                {
                    i++;
                    digits++;
                }
            }

            return digits == 0 ? 0 : i;
        }

        /// <summary>
        /// Removes one leading <c>+</c>, which the XSD numeric types allow and the framework parsers do not all accept.
        /// </summary>
        /// <param name="collapsed">The collapsed value.</param>
        /// <returns>The value without a leading plus sign.</returns>
        internal static ReadOnlySpan<byte> StripPlus(ReadOnlySpan<byte> collapsed)
        {
            return collapsed.Length > 0 && collapsed[0] is (byte)'+' ? collapsed[1..] : collapsed;
        }

        /// <summary>
        /// Parses an XSD integer into a sign and magnitude.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="negative">When this method returns true, whether the value had a minus sign.</param>
        /// <param name="magnitude">When this method returns true, the absolute value.</param>
        /// <returns><see langword="true"/> if the value is <c>[+-]?[0-9]+</c> and fits in 64 bits.</returns>
        /// <remarks>
        /// Digits are validated and accumulated in one pass. Up to 19 digits cannot overflow, which covers
        /// every realistic value without an overflow check per digit.
        /// </remarks>
        internal static bool TryParseInteger(ReadOnlySpan<byte> utf8, out bool negative, out ulong magnitude)
        {
            var s = utf8;
            negative = false;
            magnitude = 0;
            if (s.IsEmpty)
            {
                return false;
            }

            if (s.Length <= 19 && (uint)(s[0] - '0') <= 9)
            {
                // The usual case: nothing but digits. Anything else (whitespace to collapse, a sign,
                // a stray character) falls through to the general path below.
                var plain = 0UL;
                var index = 0;
                while (index < s.Length && (uint)(s[index] - '0') <= 9)
                {
                    plain = (plain * 10) + (uint)(s[index] - '0');
                    index++;
                }

                if (index == s.Length)
                {
                    magnitude = plain;
                    return true;
                }
            }

            s = Collapse(s);

            if (s.Length > 0 && s[0] is (byte)'-' or (byte)'+')
            {
                negative = s[0] is (byte)'-';
                s = s[1..];
            }

            if (s.IsEmpty)
            {
                return false;
            }

            if (s.Length > 19)
            {
                s = s.TrimStart((byte)'0');
                if (s.Length > 20)
                {
                    return false;
                }
            }

            var unchecked19 = Math.Min(s.Length, 19);
            for (var i = 0; i < unchecked19; i++)
            {
                var digit = (uint)(s[i] - '0');
                if (digit > 9)
                {
                    return false;
                }

                magnitude = (magnitude * 10) + digit;
            }

            if (s.Length == 20)
            {
                var last = (uint)(s[19] - '0');
                if (last > 9 || magnitude > (ulong.MaxValue - last) / 10)
                {
                    return false;
                }

                magnitude = (magnitude * 10) + last;
            }

            return true;
        }

        /// <summary>
        /// Maps the <c>INF</c>, <c>-INF</c>, and <c>NaN</c> literals, in any letter case.
        /// </summary>
        /// <param name="collapsed">The collapsed value.</param>
        /// <param name="value">When this method returns true, the named value.</param>
        /// <returns><see langword="true"/> if the value is a named literal.</returns>
        internal static bool TryParseNamedFloat(ReadOnlySpan<byte> collapsed, out double value)
        {
            if (Ascii.EqualsIgnoreCase(collapsed, "INF"u8))
            {
                value = double.PositiveInfinity;
                return true;
            }

            if (Ascii.EqualsIgnoreCase(collapsed, "-INF"u8))
            {
                value = double.NegativeInfinity;
                return true;
            }

            value = double.NaN;
            return Ascii.EqualsIgnoreCase(collapsed, "NaN"u8);
        }

        /// <summary>
        /// Reads <c>YYYY-MM-DD</c> from the front of <paramref name="utf8"/>.
        /// </summary>
        /// <param name="utf8">The value. Advanced past the date on success.</param>
        /// <param name="year">The year, 1 to 9999.</param>
        /// <param name="month">The month.</param>
        /// <param name="day">The day, valid for the month.</param>
        /// <returns><see langword="true"/> if a calendar date was read.</returns>
        /// <remarks>
        /// XSD allows more than four year digits and negative years; the framework date types cannot hold them.
        /// </remarks>
        internal static bool TryReadDate(ref ReadOnlySpan<byte> utf8, out int year, out int month, out int day)
        {
            month = 0;
            day = 0;
            if (utf8.Length < 10
                || !TryReadFixed(utf8, 0, 4, out year)
                || utf8[4] is not (byte)'-'
                || !TryReadFixed(utf8, 5, 2, out month)
                || utf8[7] is not (byte)'-'
                || !TryReadFixed(utf8, 8, 2, out day)
                || year == 0
                || month is < 1 or > 12
                || day < 1
                || day > DateTime.DaysInMonth(year, month))
            {
                year = 0;
                return false;
            }

            utf8 = utf8[10..];
            return true;
        }

        /// <summary>
        /// Reads an <c>xs:dateTime</c> into its wall-clock value and time zone.
        /// </summary>
        /// <param name="utf8">The lexical value.</param>
        /// <param name="local">The date and time as written, <see cref="DateTimeKind.Unspecified"/>.</param>
        /// <param name="hasZone">Whether a time zone was present.</param>
        /// <param name="offset">The offset from UTC, or zero.</param>
        /// <returns><see langword="true"/> if the value is a dateTime.</returns>
        internal static bool TryReadDateTime(ReadOnlySpan<byte> utf8, out DateTime local, out bool hasZone, out TimeSpan offset)
        {
            var s = Collapse(utf8);
            local = default;
            hasZone = false;
            offset = default;
            if (!TryReadDate(ref s, out var year, out var month, out var day)
                || s.IsEmpty
                || s[0] is not (byte)'T')
            {
                return false;
            }

            s = s[1..];
            if (!TryReadTime(ref s, out var ticks, out var endOfDay) || !TryReadZone(s, out hasZone, out offset))
            {
                return false;
            }

            var date = new DateTime(year, month, day);
            if (endOfDay)
            {
                if (date.Date == DateTime.MaxValue.Date)
                {
                    return false;
                }

                date = date.AddDays(1);
            }

            local = date.AddTicks(ticks);
            return true;
        }

        /// <summary>
        /// Reads one or more decimal digits from the front of <paramref name="utf8"/>.
        /// </summary>
        /// <param name="utf8">The value. Advanced past the digits on success.</param>
        /// <param name="value">The number read.</param>
        /// <returns><see langword="true"/> if one to fifteen digits were read.</returns>
        internal static bool TryReadDigits(ref ReadOnlySpan<byte> utf8, out long value)
        {
            value = 0;
            var i = 0;
            while (i < utf8.Length && utf8[i] is >= (byte)'0' and <= (byte)'9')
            {
                if (i >= 15)
                {
                    return false;
                }

                value = (value * 10) + (utf8[i] - '0');
                i++;
            }

            utf8 = utf8[i..];
            return i > 0;
        }

        /// <summary>
        /// Reads <paramref name="count"/> decimal digits at <paramref name="start"/>.
        /// </summary>
        /// <param name="utf8">The value.</param>
        /// <param name="start">The first digit.</param>
        /// <param name="count">The number of digits.</param>
        /// <param name="value">The number read.</param>
        /// <returns><see langword="true"/> if every byte was a digit.</returns>
        internal static bool TryReadFixed(ReadOnlySpan<byte> utf8, int start, int count, out int value)
        {
            value = 0;
            for (var i = start; i < start + count; i++)
            {
                var digit = (uint)(utf8[i] - '0');
                if (digit > 9)
                {
                    return false;
                }

                value = (value * 10) + (int)digit;
            }

            return true;
        }

        /// <summary>
        /// Reads fractional-second digits from the front of <paramref name="utf8"/> as 100 ns ticks.
        /// </summary>
        /// <param name="utf8">The value after the decimal point. Advanced past the digits on success.</param>
        /// <param name="ticks">The fraction in ticks. Digits past the seventh are dropped.</param>
        /// <returns><see langword="true"/> if at least one digit was read.</returns>
        internal static bool TryReadFraction(ref ReadOnlySpan<byte> utf8, out long ticks)
        {
            ticks = 0;
            var i = 0;
            var scale = TimeSpan.TicksPerSecond;
            while (i < utf8.Length && utf8[i] is >= (byte)'0' and <= (byte)'9')
            {
                scale /= 10;
                ticks += (utf8[i] - '0') * scale;
                i++;
            }

            utf8 = utf8[i..];
            return i > 0;
        }

        /// <summary>
        /// Reads <c>hh:mm:ss[.fff]</c> from the front of <paramref name="utf8"/>.
        /// </summary>
        /// <param name="utf8">The value. Advanced past the time on success.</param>
        /// <param name="ticks">The time of day in ticks. Zero for <c>24:00:00</c>.</param>
        /// <param name="endOfDay">True for <c>24:00:00</c>, which XSD defines as the first instant of the next day.</param>
        /// <returns><see langword="true"/> if a time was read.</returns>
        internal static bool TryReadTime(ref ReadOnlySpan<byte> utf8, out long ticks, out bool endOfDay)
        {
            ticks = 0;
            endOfDay = false;
            if (utf8.Length < 8
                || !TryReadFixed(utf8, 0, 2, out var hour)
                || utf8[2] is not (byte)':'
                || !TryReadFixed(utf8, 3, 2, out var minute)
                || utf8[5] is not (byte)':'
                || !TryReadFixed(utf8, 6, 2, out var second)
                || minute > 59
                || second > 59)
            {
                return false;
            }

            utf8 = utf8[8..];
            long fraction = 0;
            if (utf8.Length > 0 && utf8[0] is (byte)'.')
            {
                utf8 = utf8[1..];
                if (!TryReadFraction(ref utf8, out fraction))
                {
                    return false;
                }
            }

            if (hour == 24)
            {
                endOfDay = minute == 0 && second == 0 && fraction == 0;
                return endOfDay;
            }

            ticks = (hour * TimeSpan.TicksPerHour) + (minute * TimeSpan.TicksPerMinute) + (second * TimeSpan.TicksPerSecond) + fraction;
            return hour < 24;
        }

        /// <summary>
        /// Reads an optional XSD time zone that must span all of <paramref name="utf8"/>.
        /// </summary>
        /// <param name="utf8">The rest of the value after the date or time.</param>
        /// <param name="hasZone">Whether a time zone was present.</param>
        /// <param name="offset">The offset from UTC: zero for <c>Z</c>, otherwise up to 14 hours either way.</param>
        /// <returns><see langword="true"/> if the rest is empty or a legal time zone.</returns>
        internal static bool TryReadZone(ReadOnlySpan<byte> utf8, out bool hasZone, out TimeSpan offset)
        {
            offset = default;
            hasZone = !utf8.IsEmpty;
            if (!hasZone || utf8.SequenceEqual("Z"u8))
            {
                return true;
            }

            if (utf8.Length != 6
                || utf8[0] is not ((byte)'+' or (byte)'-')
                || !TryReadFixed(utf8, 1, 2, out var hours)
                || utf8[3] is not (byte)':'
                || !TryReadFixed(utf8, 4, 2, out var minutes)
                || minutes > 59
                || hours > 14
                || (hours == 14 && minutes != 0))
            {
                return false;
            }

            offset = new TimeSpan(hours, minutes, 0);
            if (utf8[0] is (byte)'-')
            {
                offset = -offset;
            }

            return true;
        }

        #endregion

    }

}
