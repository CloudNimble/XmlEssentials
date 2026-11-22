using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts an enum as its member name, or as its number when <see cref="XmlSerializerOptions.EnumHandling"/>
    /// is <see cref="XmlEnumHandling.Integer"/>. A <see cref="FlagsAttribute"/> enum is a space-separated list of names.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <remarks>
    /// Names and values are read from the type once. Values are held as the raw bits of the underlying type,
    /// zero-extended to 64 bits, so no value is boxed and <see cref="ulong"/> enums keep their full range.
    /// Reading accepts a name or a number whatever the handling is. A number must fit the underlying type, and
    /// for an enum without <see cref="FlagsAttribute"/> it must be a declared member.
    /// </remarks>
    internal sealed class XmlEnumConverter<T> : XmlValueConverter<T>
        where T : struct, Enum
    {

        #region Fields

        private static readonly bool s_flags = typeof(T).IsDefined(typeof(FlagsAttribute), inherit: false);
        private static readonly byte[][] s_names = Array.ConvertAll(Enum.GetNames<T>(), Encoding.UTF8.GetBytes);
        private static readonly int s_shift = 64 - (8 * Unsafe.SizeOf<T>());
        private static readonly bool s_signed = Type.GetTypeCode(typeof(T)) is TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64;
        private static readonly ulong[] s_values = Array.ConvertAll(Enum.GetValues<T>(), ToBits);

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns the enum value whose underlying bits are the low bits of <paramref name="bits"/>.
        /// </summary>
        /// <param name="bits">The bits.</param>
        /// <returns>The value.</returns>
        internal static T FromBits(ulong bits)
        {
            switch (Unsafe.SizeOf<T>())
            {
                case 1:
                    var one = (byte)bits;
                    return Unsafe.As<byte, T>(ref one);
                case 2:
                    var two = (ushort)bits;
                    return Unsafe.As<ushort, T>(ref two);
                case 4:
                    var four = (uint)bits;
                    return Unsafe.As<uint, T>(ref four);
                default:
                    return Unsafe.As<ulong, T>(ref bits);
            }
        }

        /// <inheritdoc />
        internal override T Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            var text = XmlLexical.Collapse(utf8);
            if (!TryParseNumber(text, out var bits) && !TryParseNames(text, out bits))
            {
                throw new XmlException("The value is not a valid enum member.");
            }

            return FromBits(bits);
        }

        /// <summary>
        /// Returns the underlying bits of <paramref name="value"/>, zero-extended.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The bits.</returns>
        internal static ulong ToBits(T value)
        {
            return Unsafe.SizeOf<T>() switch
            {
                1 => Unsafe.As<T, byte>(ref value),
                2 => Unsafe.As<T, ushort>(ref value),
                4 => Unsafe.As<T, uint>(ref value),
                _ => Unsafe.As<T, ulong>(ref value),
            };
        }

        /// <summary>
        /// Parses one member name, or for a flags enum a space-separated list of them.
        /// </summary>
        /// <param name="text">The collapsed lexical form.</param>
        /// <param name="bits">When this method returns true, the combined bits.</param>
        /// <returns><see langword="true"/> if every name is a member.</returns>
        internal static bool TryParseNames(ReadOnlySpan<byte> text, out ulong bits)
        {
            bits = 0;
            do
            {
                var end = s_flags ? text.IndexOfAny(" \t\r\n"u8) : -1;
                var index = IndexOfName(end < 0 ? text : text[..end]);
                if (index < 0)
                {
                    return false;
                }

                bits |= s_values[index];
                text = end < 0 ? default : text[end..].TrimStart(" \t\r\n"u8);
            }
            while (!text.IsEmpty);

            return true;
        }

        /// <summary>
        /// Parses a number that fits the underlying type. Without <see cref="FlagsAttribute"/> it must also be a member.
        /// </summary>
        /// <param name="text">The collapsed lexical form.</param>
        /// <param name="bits">When this method returns true, the bits.</param>
        /// <returns><see langword="true"/> if the text is an acceptable number.</returns>
        internal static bool TryParseNumber(ReadOnlySpan<byte> text, out ulong bits)
        {
            if (s_signed)
            {
                if (!XmlLexical.TryParseInt64(text, out var signed) || (signed << s_shift) >> s_shift != signed)
                {
                    bits = 0;
                    return false;
                }

                bits = ((ulong)signed << s_shift) >> s_shift;
            }
            else if (!XmlLexical.TryParseUInt64(text, out bits) || (bits << s_shift) >> s_shift != bits)
            {
                return false;
            }

            return s_flags || Array.IndexOf(s_values, bits) >= 0;
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, T value, XmlSerializerOptions options)
        {
            var bits = ToBits(value);
            if (options.EnumHandling is not XmlEnumHandling.Integer)
            {
                var index = Array.IndexOf(s_values, bits);
                if (index >= 0)
                {
                    writer.WriteStringValue(s_names[index]);
                    return;
                }

                if (s_flags && bits != 0 && TryWriteFlags(writer, bits))
                {
                    return;
                }
            }

            if (s_signed)
            {
                writer.WriteNumberValue((long)(bits << s_shift) >> s_shift);
                return;
            }

            writer.WriteNumberValue(bits);
        }

        /// <summary>
        /// Returns the index of the member named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The UTF-8 name.</param>
        /// <returns>The index, or -1.</returns>
        private static int IndexOfName(ReadOnlySpan<byte> name)
        {
            for (var i = 0; i < s_names.Length; i++)
            {
                if (name.SequenceEqual(s_names[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Writes <paramref name="bits"/> as the names of the members that make it up, largest first when
        /// choosing and in declared-value order when writing, as <see cref="Enum.ToString()"/> does.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="bits">The value. It is not zero and is not itself a member.</param>
        /// <returns><see langword="false"/> when some bit belongs to no member; nothing is written.</returns>
        private static bool TryWriteFlags(Utf8XmlWriter writer, ulong bits)
        {
            Span<int> chosen = stackalloc int[64];
            var count = 0;
            for (var i = s_values.Length - 1; i >= 0; i--)
            {
                var member = s_values[i];
                if (member != 0 && (bits & member) == member)
                {
                    chosen[count++] = i;
                    bits &= ~member;
                }
            }

            if (bits != 0)
            {
                return false;
            }

            writer.WriteStringValue(s_names[chosen[--count]]);
            while (count > 0)
            {
                writer.WriteStringValue(" "u8);
                writer.WriteStringValue(s_names[chosen[--count]]);
            }

            return true;
        }

        #endregion

    }

}
