namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts an <see cref="XmlElement"/> as the element it is.
    /// </summary>
    /// <remarks>
    /// An element that is read belongs to a document nobody will dispose, so that document holds no rented memory.
    /// </remarks>
    internal sealed class XmlElementFragmentConverter : XmlFragmentConverter<XmlElement>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlElementFragmentConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override bool IsEmpty(XmlElement value)
        {
            return value.ValueKind is XmlValueKind.Undefined;
        }

        /// <inheritdoc />
        internal override XmlElement ReadValue(ref Utf8XmlReader reader)
        {
            return XmlDocumentFragmentConverter.Instance.ReadValue(ref reader).RootElement;
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, XmlElement value)
        {
            value.WriteTo(writer);
        }

        #endregion

    }

}
