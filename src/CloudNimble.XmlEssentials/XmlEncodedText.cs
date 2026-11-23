using System;
using System.Text;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Represents an XML name that is already encoded as UTF-8, so that a <see cref="Utf8XmlWriter"/> can write it
    /// many times without encoding it again.
    /// </summary>
    /// <remarks>
    /// Create a value once with <see cref="Encode(string)"/> and reuse it for a name that is written often.
    /// Encoding converts the name to UTF-8. It does not escape or check the name. The writer checks the name
    /// when it writes it.
    /// </remarks>
    public readonly struct XmlEncodedText : IEquatable<XmlEncodedText>
    {

        #region Fields

        private readonly byte[] _utf8;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the UTF-8 encoded representation of the name.
        /// </summary>
        /// <value>
        /// The UTF-8 bytes of the name. The span is empty for a default <see cref="XmlEncodedText"/>.
        /// </value>
        public ReadOnlySpan<byte> EncodedUtf8Bytes => _utf8;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an encoded value from UTF-8 name bytes that are already a valid XML Name.
        /// </summary>
        /// <param name="utf8">The UTF-8 name bytes.</param>
        internal XmlEncodedText(byte[] utf8)
        {
            _utf8 = utf8;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Encodes a name specified as a string to UTF-8.
        /// </summary>
        /// <param name="value">The name to encode.</param>
        /// <returns>
        /// The UTF-8 encoded name.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public static XmlEncodedText Encode(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return Encode(value.AsSpan());
        }

        /// <summary>
        /// Encodes a name specified as a read-only span of characters to UTF-8.
        /// </summary>
        /// <param name="value">The name to encode.</param>
        /// <returns>
        /// The UTF-8 encoded name.
        /// </returns>
        public static XmlEncodedText Encode(ReadOnlySpan<char> value)
        {
            var utf8 = new byte[Encoding.UTF8.GetByteCount(value)];
            Encoding.UTF8.GetBytes(value, utf8);
            return new XmlEncodedText(utf8);
        }

        /// <summary>
        /// Creates an encoded name from a name specified as a read-only span of UTF-8 bytes.
        /// </summary>
        /// <param name="utf8Value">The UTF-8 encoded name. The bytes are copied.</param>
        /// <returns>
        /// The UTF-8 encoded name.
        /// </returns>
        public static XmlEncodedText Encode(ReadOnlySpan<byte> utf8Value)
        {
            return new XmlEncodedText(utf8Value.ToArray());
        }

        /// <inheritdoc />
        public bool Equals(XmlEncodedText other)
        {
            return EncodedUtf8Bytes.SequenceEqual(other.EncodedUtf8Bytes);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is XmlEncodedText other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.AddBytes(EncodedUtf8Bytes);
            return hash.ToHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return _utf8 is null ? string.Empty : Encoding.UTF8.GetString(_utf8);
        }

        /// <summary>
        /// Determines whether two <see cref="XmlEncodedText"/> values have the same UTF-8 bytes.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> have the same UTF-8 bytes;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator ==(XmlEncodedText left, XmlEncodedText right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="XmlEncodedText"/> values have different UTF-8 bytes.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> have different UTF-8 bytes;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator !=(XmlEncodedText left, XmlEncodedText right)
        {
            return !left.Equals(right);
        }

        #endregion

    }

}
