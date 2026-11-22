namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Joins words with <c>-</c> in uppercase.
    /// </summary>
    internal sealed class XmlKebabCaseUpperNamingPolicy : XmlNamingPolicy
    {

        #region Public Methods

        /// <inheritdoc />
        public override string ConvertName(string name)
        {
            return ToSeparated(name, '-', upper: true);
        }

        #endregion

    }

}
