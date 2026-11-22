using System;
using System.Text;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Cached escaped UTF-8 for hot element and attribute names.
    /// </summary>
    public readonly struct XmlEncodedText : IEquatable<XmlEncodedText>
    {

        #region Fields

        private readonly byte[] _utf8;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the encoded UTF-8 bytes.
        /// </summary>
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
        /// Encodes <paramref name="value"/> as UTF-8 name bytes.
        /// </summary>
        /// <param name="value">The name.</param>
        /// <returns>The encoded name.</returns>
        public static XmlEncodedText Encode(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return Encode(value.AsSpan());
        }

        /// <summary>
        /// Encodes <paramref name="value"/> as UTF-8 name bytes.
        /// </summary>
        /// <param name="value">The name.</param>
        /// <returns>The encoded name.</returns>
        public static XmlEncodedText Encode(ReadOnlySpan<char> value)
        {
            var utf8 = new byte[Encoding.UTF8.GetByteCount(value)];
            Encoding.UTF8.GetBytes(value, utf8);
            return new XmlEncodedText(utf8);
        }

        /// <summary>
        /// Encodes <paramref name="utf8Value"/> as UTF-8 name bytes.
        /// </summary>
        /// <param name="utf8Value">The UTF-8 name.</param>
        /// <returns>The encoded name.</returns>
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
        /// Returns true when both values have the same UTF-8 bytes.
        /// </summary>
        /// <param name="left">The first value.</param>
        /// <param name="right">The second value.</param>
        /// <returns><see langword="true"/> if the values are equal.</returns>
        public static bool operator ==(XmlEncodedText left, XmlEncodedText right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns true when the values differ.
        /// </summary>
        /// <param name="left">The first value.</param>
        /// <param name="right">The second value.</param>
        /// <returns><see langword="true"/> if the values are not equal.</returns>
        public static bool operator !=(XmlEncodedText left, XmlEncodedText right)
        {
            return !left.Equals(right);
        }

        #endregion

    }

}
