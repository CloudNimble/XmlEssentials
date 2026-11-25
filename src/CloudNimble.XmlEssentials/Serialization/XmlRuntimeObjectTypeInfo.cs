namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Type info for a value declared as <see cref="object"/>. As the document element, the value is written
    /// as the type it has, under that type's element name.
    /// </summary>
    internal sealed class XmlRuntimeObjectTypeInfo : XmlTypeInfo<object>
    {

        #region Constructors

        /// <summary>
        /// Creates the type info.
        /// </summary>
        /// <param name="converter">The converter.</param>
        /// <param name="options">The options that own the type info.</param>
        internal XmlRuntimeObjectTypeInfo(XmlRuntimeObjectConverter converter, XmlSerializerOptions options)
            : base(converter, options, CreateRootName(typeof(object), options))
        {
        }

        #endregion

        #region Private Methods

        /// <inheritdoc />
        /// <remarks>
        /// The document element is read whatever its name is, because the name belongs to the type that wrote it.
        /// </remarks>
        internal override object? ReadRoot(ref Utf8XmlReader reader)
        {
            XmlConverterHelpers.MoveToStartElement(ref reader);
            return ReadElement(ref reader);
        }

        /// <inheritdoc />
        internal override void WriteRoot(Utf8XmlWriter writer, object value)
        {
            if (value is null || value.GetType() == typeof(object))
            {
                base.WriteRoot(writer, value!);
                return;
            }

            Options.GetTypeInfo(value.GetType()).WriteRootAsObject(writer, value);
        }

        #endregion

    }

}
