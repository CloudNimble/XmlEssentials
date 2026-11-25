using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Public properties whose setters have each accessibility, and one whose private setter is included.
    /// </summary>
    public sealed class SetterAccess
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value with a private setter that is marked to be used.
        /// </summary>
        [XmlInclude]
        public string? Included { get; private set; } = "-";

        /// <summary>
        /// Gets a value with a public init accessor.
        /// </summary>
        public string? Init { get; init; } = "-";

        /// <summary>
        /// Gets or sets a value with an internal setter.
        /// </summary>
        public string? Internal { get; internal set; } = "-";

        /// <summary>
        /// Gets or sets a value with a private setter.
        /// </summary>
        public string? Private { get; private set; } = "-";

        /// <summary>
        /// Gets a value with a private init accessor.
        /// </summary>
        public string? PrivateInit { get; private init; } = "-";

        /// <summary>
        /// Gets or sets a value with a public setter.
        /// </summary>
        public string? Public { get; set; } = "-";

        #endregion

    }

}