namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Joins words with <c>_</c> in uppercase.
    /// </summary>
    internal sealed class XmlSnakeCaseUpperNamingPolicy : XmlNamingPolicy
    {

        #region Public Methods

        /// <inheritdoc />
        public override string ConvertName(string name)
        {
            return ToSeparated(name, '_', upper: true);
        }

        #endregion

    }

}
