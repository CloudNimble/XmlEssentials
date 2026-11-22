namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Well-known XML attribute local names used by XmlEssentials.
    /// </summary>
    /// <remarks>
    /// These are local names only. Bind them to <see cref="XmlNamespaces.Xsi"/> when writing
    /// <c>xsi:nil</c> or <c>xsi:type</c>.
    /// </remarks>
    public static class XmlAttributes
    {

        #region Fields

        /// <summary>
        /// Local name of the XML Schema instance nil attribute (<c>xsi:nil</c>).
        /// </summary>
        public const string Nil = "nil";

        /// <summary>
        /// Local name of the XML Schema instance type attribute (<c>xsi:type</c>).
        /// </summary>
        public const string Type = "type";

        #endregion

    }

}
