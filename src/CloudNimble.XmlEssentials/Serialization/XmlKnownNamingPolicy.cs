namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Built-in naming policies selectable from source-generation options.
    /// </summary>
    public enum XmlKnownNamingPolicy
    {

        /// <summary>
        /// Leave names unchanged.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// <see cref="XmlNamingPolicy.CamelCase"/>.
        /// </summary>
        CamelCase = 1,

        /// <summary>
        /// <see cref="XmlNamingPolicy.SnakeCaseLower"/>.
        /// </summary>
        SnakeCaseLower = 2,

        /// <summary>
        /// <see cref="XmlNamingPolicy.SnakeCaseUpper"/>.
        /// </summary>
        SnakeCaseUpper = 3,

        /// <summary>
        /// <see cref="XmlNamingPolicy.KebabCaseLower"/>.
        /// </summary>
        KebabCaseLower = 4,

        /// <summary>
        /// <see cref="XmlNamingPolicy.KebabCaseUpper"/>.
        /// </summary>
        KebabCaseUpper = 5,

        /// <summary>
        /// <see cref="XmlNamingPolicy.PascalCase"/>.
        /// </summary>
        PascalCase = 6,

    }

}
