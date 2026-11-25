namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Lowercases the first character of a name.
    /// </summary>
    internal sealed class XmlCamelCaseNamingPolicy : XmlNamingPolicy
    {

        #region Public Methods

        /// <inheritdoc />
        public override string ConvertName(string name)
        {
            return ToCamel(name);
        }

        #endregion

    }

}
