namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies the built-in <see cref="XmlNamingPolicy"/> that a source-generated context uses at run time.
    /// </summary>
    /// <remarks>
    /// The values are selected through the naming policy properties of <see cref="XmlSourceGenerationOptionsAttribute"/>.
    /// </remarks>
    public enum XmlKnownNamingPolicy
    {

        /// <summary>
        /// No naming policy. Names are not converted.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// The built-in <see cref="XmlNamingPolicy.CamelCase"/> naming policy.
        /// </summary>
        CamelCase = 1,

        /// <summary>
        /// The built-in <see cref="XmlNamingPolicy.SnakeCaseLower"/> naming policy.
        /// </summary>
        SnakeCaseLower = 2,

        /// <summary>
        /// The built-in <see cref="XmlNamingPolicy.SnakeCaseUpper"/> naming policy.
        /// </summary>
        SnakeCaseUpper = 3,

        /// <summary>
        /// The built-in <see cref="XmlNamingPolicy.KebabCaseLower"/> naming policy.
        /// </summary>
        KebabCaseLower = 4,

        /// <summary>
        /// The built-in <see cref="XmlNamingPolicy.KebabCaseUpper"/> naming policy.
        /// </summary>
        KebabCaseUpper = 5,

        /// <summary>
        /// The built-in <see cref="XmlNamingPolicy.PascalCase"/> naming policy.
        /// </summary>
        PascalCase = 6,

    }

}
