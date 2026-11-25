namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Uppercases the first character of a name.
    /// </summary>
    internal sealed class XmlPascalCaseNamingPolicy : XmlNamingPolicy
    {

        #region Public Methods

        /// <inheritdoc />
        public override string ConvertName(string name)
        {
            return ToPascal(name);
        }

        #endregion

    }

}
