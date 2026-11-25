using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts a <see cref="Nullable{T}"/> that has a value with the converter for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <remarks>
    /// <see langword="null"/> never reaches this converter: <see cref="XmlTypeInfo{T}"/> reads and writes
    /// <c>xsi:nil</c> before it calls any converter.
    /// </remarks>
    internal sealed class XmlNullableConverter<T> : XmlConverter<T?>
        where T : struct
    {

        #region Fields

        private readonly XmlConverter<T> _inner;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a nullable converter.
        /// </summary>
        /// <param name="inner">The converter for <typeparamref name="T"/>.</param>
        public XmlNullableConverter(XmlConverter<T> inner)
        {
            _inner = inner;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override T? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            return _inner.Read(ref reader, typeof(T), options);
        }

        /// <inheritdoc />
        public override T? ReadAsAttribute(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            return _inner.ReadAsAttribute(ref reader, typeof(T), options);
        }

        /// <inheritdoc />
        public override T? ReadLexical(ReadOnlySpan<byte> utf8, XmlSerializerOptions options)
        {
            return _inner.ReadLexical(utf8, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, T? value, XmlSerializerOptions options)
        {
            _inner.Write(writer, value!.Value, options);
        }

        /// <inheritdoc />
        public override void WriteAsAttribute(Utf8XmlWriter writer, XmlName name, T? value, XmlSerializerOptions options)
        {
            _inner.WriteAsAttribute(writer, name, value!.Value, options);
        }

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override bool CanPopulate => _inner.CanPopulate;

        /// <inheritdoc />
        /// <remarks>
        /// A nullable value that has a value is boxed as that value, so the box is the one the inner converter populates.
        /// </remarks>
        internal override void Populate(ref Utf8XmlReader reader, object existing, XmlSerializerOptions options)
        {
            _inner.Populate(ref reader, existing, options);
        }

        #endregion

    }

}
