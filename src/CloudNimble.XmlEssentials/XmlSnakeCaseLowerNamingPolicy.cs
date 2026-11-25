namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Joins words with <c>_</c> in lowercase.
    /// </summary>
    internal sealed class XmlSnakeCaseLowerNamingPolicy : XmlNamingPolicy
    {

        #region Public Methods

        /// <inheritdoc />
        public override string ConvertName(string name)
        {
            return ToSeparated(name, '_', upper: false);
        }

        #endregion

    }

}
