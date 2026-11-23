namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Specifies how a nillable <see langword="null"/> value is written.
    /// </summary>
    public enum XmlNilHandling : byte
    {

        /// <summary>
        /// An empty element that carries <c>xsi:nil="true"</c>.
        /// </summary>
        WriteXsiNil = 0,

        /// <summary>
        /// An empty element without <c>xsi:nil</c>.
        /// </summary>
        WriteEmptyElement = 1,

        /// <summary>
        /// No output: the member is omitted. A <see langword="null"/> document root cannot be omitted, so
        /// serializing one throws an <see cref="System.InvalidOperationException"/>.
        /// </summary>
        Omit = 2,

    }

}
