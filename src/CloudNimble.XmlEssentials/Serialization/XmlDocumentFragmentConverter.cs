namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts an <see cref="XmlDocument"/> as its document element.
    /// </summary>
    /// <remarks>
    /// The XML declaration and anything outside the document element are not written: the serializer writes
    /// its own declaration, and a member's value sits inside another element, where neither is allowed. A
    /// document that is read holds no rented memory, so it is safe to drop without disposing it.
    /// </remarks>
    internal sealed class XmlDocumentFragmentConverter : XmlFragmentConverter<XmlDocument>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlDocumentFragmentConverter Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override XmlDocument ReadValue(ref Utf8XmlReader reader)
        {
            var document = XmlDocument.ParseValue(ref reader);
            document.MoveOutOfPool();
            return document;
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, XmlDocument value)
        {
            value.RootElement.WriteTo(writer);
        }

        #endregion

    }

}
