namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Joins words with <c>-</c> in lowercase.
    /// </summary>
    internal sealed class XmlKebabCaseLowerNamingPolicy : XmlNamingPolicy
    {

        #region Public Methods

        /// <inheritdoc />
        public override string ConvertName(string name)
        {
            return ToSeparated(name, '-', upper: false);
        }

        #endregion

    }

}
