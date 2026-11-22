namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// How a nillable null value is written.
    /// </summary>
    public enum XmlNilHandling : byte
    {

        /// <summary>
        /// Write <c>xsi:nil="true"</c> on an empty element.
        /// </summary>
        WriteXsiNil = 0,

        /// <summary>
        /// Write an empty element without <c>xsi:nil</c>.
        /// </summary>
        WriteEmptyElement = 1,

        /// <summary>
        /// Omit the member. A null document root throws.
        /// </summary>
        Omit = 2,

    }

}
