namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Type info for a value that is XML already. As the document element, the value is the document.
    /// </summary>
    /// <typeparam name="T">The type that holds the XML.</typeparam>
    internal sealed class XmlFragmentTypeInfo<T> : XmlTypeInfo<T>
    {

        #region Fields

        private readonly XmlFragmentConverter<T> _fragment;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the type info.
        /// </summary>
        /// <param name="converter">The converter.</param>
        /// <param name="options">The options that own the type info.</param>
        internal XmlFragmentTypeInfo(XmlFragmentConverter<T> converter, XmlSerializerOptions options)
            : base(converter, options, CreateRootName(typeof(T), options))
        {
            _fragment = converter;
        }

        #endregion

        #region Private Methods

        /// <inheritdoc />
        /// <remarks>
        /// The document element is read whatever its name is, because the name belongs to the value.
        /// </remarks>
        internal override T? ReadRoot(ref Utf8XmlReader reader)
        {
            XmlConverterHelpers.MoveToStartElement(ref reader);
            if (IsNillable && reader.HasAttributes && reader.GetNil())
            {
                // A document element marked nil is what a null value is written as, so it reads back as null.
                reader.Skip();
                return default;
            }

            return _fragment.ReadValue(ref reader);
        }

        /// <inheritdoc />
        internal override void WriteRoot(Utf8XmlWriter writer, T value)
        {
            if (XmlConverterHelpers.IsNull(value))
            {
                WriteNullRoot(writer);
                return;
            }

            _fragment.WriteValue(writer, value);
        }

        #endregion

    }

}
