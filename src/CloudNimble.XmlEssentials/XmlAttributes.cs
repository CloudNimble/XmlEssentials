namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Provides the local names of the XML Schema instance attributes that XmlEssentials reads and writes.
    /// This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// The constants are local names only. Bind them to the <see cref="XmlNamespaces.Xsi"/> namespace
    /// when you write <c>xsi:nil</c> or <c>xsi:type</c>.
    /// </remarks>
    public static class XmlAttributes
    {

        #region Fields

        /// <summary>
        /// Represents the local name of the XML Schema instance nil attribute (<c>xsi:nil</c>).
        /// </summary>
        public const string Nil = "nil";

        /// <summary>
        /// Represents the local name of the XML Schema instance type attribute (<c>xsi:type</c>).
        /// </summary>
        public const string Type = "type";

        #endregion

    }

}
