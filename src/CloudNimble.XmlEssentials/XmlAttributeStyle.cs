namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Specifies how a <see cref="Utf8XmlWriter"/> lays out the attributes of a start tag.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The layout of attributes means nothing to a reader. The setting makes the output consistent: every
    /// start tag is laid out the same way, whichever code wrote it.
    /// </para>
    /// <para>
    /// <see cref="Inline"/> is the zero value, so it is the default in <see cref="XmlWriterOptions"/>,
    /// created with <see langword="new"/> or as <see langword="default"/>, and in <see cref="XmlSerializerOptions"/>.
    /// </para>
    /// </remarks>
    public enum XmlAttributeStyle : byte
    {

        /// <summary>
        /// Every attribute is written on the line of the start tag, with one space before it. This is the default.
        /// </summary>
        Inline = 0,

        /// <summary>
        /// The attributes are written a fixed number to a line. The first line is the line of the start tag.
        /// Each line after it starts with the white space that starts the line of the tag, and then one space
        /// for each character of the tag up to its first attribute, so for a tag that starts its line the
        /// attributes are under one another. <see cref="XmlWriterOptions.AttributesPerLine"/> is the number,
        /// which is 1 unless it is set. A namespace declaration and <c>xsi:nil</c> are attributes and are counted.
        /// </summary>
        PerLine = 1,

    }

}
