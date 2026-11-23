using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Reads and writes a <see cref="Nullable{T}"/> whose value has a built-in converter.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <remarks>
    /// The serializer handles null itself (a nil element, or an attribute that is left out), so this converter only
    /// ever sees a value. Being a value converter, it gets the same direct read and write paths as
    /// <typeparamref name="T"/> does. A custom converter for <typeparamref name="T"/> is wrapped by
    /// <see cref="XmlNullableConverter{T}"/> instead.
    /// </remarks>
    internal sealed class XmlNullableValueConverter<T> : XmlValueConverter<T?>
        where T : struct
    {

        #region Fields

        private readonly XmlValueConverter<T> _inner;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the converter.
        /// </summary>
        /// <param name="inner">The built-in converter for <typeparamref name="T"/>.</param>
        public XmlNullableValueConverter(XmlValueConverter<T> inner)
        {
            _inner = inner;
        }

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override T? Parse(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return _inner.Parse(utf8, options);
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, T? value, XmlSerializerOptions options)
        {
            _inner.WriteValue(writer, value.GetValueOrDefault(), options);
        }

        #endregion

    }

}
